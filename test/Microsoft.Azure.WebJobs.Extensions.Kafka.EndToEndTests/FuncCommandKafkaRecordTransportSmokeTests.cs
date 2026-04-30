// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    [Trait("Category", "E2E")]
    public class FuncCommandKafkaRecordTransportSmokeTests
    {
        private const string SmokeTopic = "test-topic";

        [Fact]
        public async Task KafkaRecordTransportSmokeSample_FuncStart_ReceivesParameterBindingDataProtobuf()
        {
            var repositoryRoot = FindRepositoryRoot();
            var samplePath = Path.Combine(repositoryRoot, "samples", "dotnet", "KafkaRecordTransportSmoke");
            var settingsPath = Path.Combine(samplePath, "local.settings.json");
            var consumerGroup = "kafkarecord-transport-smoke-" + Guid.NewGuid().ToString("N");
            var port = GetFreeTcpPort();
            var message = "func-smoke-" + Guid.NewGuid().ToString("N");
            var existingFuncProcessIds = GetFuncProcessIds();

            await File.WriteAllTextAsync(settingsPath, CreateLocalSettingsJson(consumerGroup));

            Process funcProcess = null;
            var funcOutput = new ConcurrentQueue<string>();

            try
            {
                await RunProcessAsync(
                    "dotnet",
                    new[] { "build", "--configuration", "Release", "-p:IsLocalBuild=False" },
                    samplePath,
                    TimeSpan.FromMinutes(2));

                funcProcess = StartProcess(
                    "func",
                    new[] { "start", "--port", port.ToString(), "--verbose" },
                    samplePath,
                    funcOutput,
                    redirectOutput: false);

                using var httpClient = new HttpClient
                {
                    BaseAddress = new Uri($"http://localhost:{port}"),
                    Timeout = TimeSpan.FromSeconds(5)
                };

                await WaitUntilAsync(
                    async () => await TryGetStatusAsync(httpClient, message) != null,
                    TimeSpan.FromMinutes(2),
                    TimeSpan.FromSeconds(2),
                    () => "Function host did not become ready. Output:" + Environment.NewLine + string.Join(Environment.NewLine, funcOutput));

                await ProduceSmokeMessageAsync(message);

                JsonDocument status = null;
                await WaitUntilAsync(
                    async () =>
                    {
                        status?.Dispose();
                        status = await TryGetStatusAsync(httpClient, message);
                        return status?.RootElement.GetProperty("matched").GetBoolean() == true;
                    },
                    TimeSpan.FromMinutes(2),
                    TimeSpan.FromSeconds(2),
                    () => "KafkaRecord transport smoke message was not received. Output:" + Environment.NewLine + string.Join(Environment.NewLine, funcOutput));

                using (status)
                {
                    var record = status.RootElement.GetProperty("record");
                    Assert.Equal("1.0", record.GetProperty("version").GetString());
                    Assert.Equal("AzureKafkaRecord", record.GetProperty("source").GetString());
                    Assert.Equal("application/x-protobuf", record.GetProperty("contentType").GetString());
                    Assert.Equal(SmokeTopic, record.GetProperty("topic").GetString());
                    Assert.Equal(message, record.GetProperty("value").GetString());
                    Assert.True(record.GetProperty("partition").GetInt32() >= 0);
                    Assert.True(record.GetProperty("offset").GetInt64() >= 0);
                    Assert.True(record.GetProperty("timestampUnixMs").GetInt64() > 0);
                }
            }
            finally
            {
                if (funcProcess != null && !funcProcess.HasExited)
                {
                    funcProcess.Kill(entireProcessTree: true);
                    await funcProcess.WaitForExitAsync();
                }

                KillNewFuncProcesses(existingFuncProcessIds);

                if (File.Exists(settingsPath))
                {
                    File.Delete(settingsPath);
                }
            }
        }

        private static string CreateLocalSettingsJson(string consumerGroup)
        {
            return $$"""
            {
              "IsEncrypted": false,
              "Values": {
                "AzureWebJobsStorage": "None",
                "FUNCTIONS_WORKER_RUNTIME": "dotnet",
                "FUNCTIONS_INPROC_NET8_ENABLED": "1",
                "LocalBroker": "localhost:9092",
                                "KafkaRecordSmokeTopic": "{{SmokeTopic}}",
                "KafkaRecordSmokeConsumerGroup": "{{consumerGroup}}"
              }
            }
            """;
        }

        private static Task ProduceSmokeMessageAsync(string message)
        {
            using var producer = new ProducerBuilder<Null, string>(new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
            }).Build();

            producer.Produce(SmokeTopic, new Message<Null, string>
            {
                Value = message
            });

            var remainingMessages = producer.Flush(TimeSpan.FromSeconds(10));
            if (remainingMessages != 0)
            {
                throw new TimeoutException($"Kafka producer did not flush within timeout. Remaining messages: {remainingMessages}");
            }

            return Task.CompletedTask;
        }

        private static async Task<JsonDocument> TryGetStatusAsync(HttpClient httpClient, string message)
        {
            try
            {
                var response = await httpClient.GetAsync("/api/status?message=" + Uri.EscapeDataString(message));
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                return JsonDocument.Parse(json);
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }

        private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout, TimeSpan pollingInterval, Func<string> failureMessage)
        {
            var startTime = DateTime.UtcNow;
            while (DateTime.UtcNow - startTime < timeout)
            {
                if (await condition())
                {
                    return;
                }

                await Task.Delay(pollingInterval);
            }

            throw new TimeoutException(failureMessage());
        }

        private static async Task RunProcessAsync(string fileName, string[] arguments, string workingDirectory, TimeSpan timeout)
        {
            var output = new ConcurrentQueue<string>();
            using var process = StartProcess(fileName, arguments, workingDirectory, output);
            var exitTask = process.WaitForExitAsync();
            var completedTask = await Task.WhenAny(exitTask, Task.Delay(timeout));
            if (completedTask != exitTask)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException($"Process timed out: {fileName} {string.Join(" ", arguments)}" + Environment.NewLine + string.Join(Environment.NewLine, output));
            }

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Process failed with exit code {process.ExitCode}: {fileName} {string.Join(" ", arguments)}" + Environment.NewLine + string.Join(Environment.NewLine, output));
            }
        }

        private static Process StartProcess(string fileName, string[] arguments, string workingDirectory, ConcurrentQueue<string> output, bool redirectOutput = true)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ResolveCommand(fileName),
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = redirectOutput,
                    RedirectStandardError = redirectOutput,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            process.StartInfo.Environment["FUNCTIONS_INPROC_NET8_ENABLED"] = "1";

            if (redirectOutput)
            {
                process.OutputDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        output.Enqueue(args.Data);
                    }
                };
                process.ErrorDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        output.Enqueue(args.Data);
                    }
                };
            }

            process.Start();
            if (redirectOutput)
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            return process;
        }

        private static HashSet<int> GetFuncProcessIds()
        {
            return Process.GetProcessesByName("func").Select(process => process.Id).ToHashSet();
        }

        private static void KillNewFuncProcesses(HashSet<int> existingFuncProcessIds)
        {
            foreach (var process in Process.GetProcessesByName("func"))
            {
                try
                {
                    if (!existingFuncProcessIds.Contains(process.Id) && !process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        private static string ResolveCommand(string fileName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && fileName.Equals("func", StringComparison.OrdinalIgnoreCase))
            {
                return "func.cmd";
            }

            return fileName;
        }

        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "azure-pipelines.yaml")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root from " + AppContext.BaseDirectory);
        }
    }
}

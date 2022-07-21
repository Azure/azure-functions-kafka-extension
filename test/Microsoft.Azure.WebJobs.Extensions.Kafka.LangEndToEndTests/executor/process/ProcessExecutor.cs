// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.process
{
    /* Executes string requests/commands as Processes.
    */
    public class ProcessExecutor : IExecutor<string, Process>
    {
        public ProcessExecutor() { }

        public async Task<Process> ExecuteAsync(string request)
        {
            if (string.IsNullOrEmpty(request)) 
            { 
                throw new ArgumentNullException(nameof(request));
            }

            var requestProcess = CreateProcess(request);
            await Task.Run(() => requestProcess.Start());

            return requestProcess;
        }

        private Process CreateProcess(string request)
        {
            Process process = new Process();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.ArgumentList.Add("/C");
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.ArgumentList.Add(request);
            }
            else
            {
                process.StartInfo.Arguments = $"-c \"{request}\"";
                process.StartInfo.FileName = "/bin/bash";
            }

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = false;

            return process;
        }
    }
}

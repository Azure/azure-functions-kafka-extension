using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.process
{
    public class ProcessExecutor : IExecutor<string, Process>
    {
        public ProcessExecutor() { }

        public async Task<Process> ExecuteAsync(string request)
        {
            Process process = new Process();
            string shell = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shell = "cmd.exe";
                process.StartInfo.ArgumentList.Add("/C");
                process.StartInfo.FileName = shell;
                process.StartInfo.ArgumentList.Add(request);
            }
            else
            {
                shell = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"{request}\"";
                process.StartInfo.FileName = shell;
                //process.StartInfo.ArgumentList.Add(request);
                Console.WriteLine($"Process Arguments: {$"-c \"{request}\""}");
            }

			process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = false;

            await Task.Run(() => process.Start());

            return process;
        }
    }
}

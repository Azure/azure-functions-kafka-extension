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
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shell = "cmd.exe";
            } else
            {
                shell = "/bin/bash";
            }

            // build process command
            process.StartInfo.FileName = shell;
            process.StartInfo.ArgumentList.Add("/C");
            
            //process.StartInfo.Arguments = "/C docker run -p 7072:7071 -e ConfluentCloudUsername -e ConfluentCloudPassword -e ConfluentBrokerList -e AzureWebJobsStorage -e FUNCTIONS_WORKER_RUNTIME='python' azure-functions-kafka-python-confluent";
            //process.StartInfo.Arguments = "/C docker run -p 7072:7071 -e ConfluentCloudUsername -e ConfluentCloudPassword -e ConfluentBrokerList -e AzureWebJobsStorage azure-functions-kafka-python-confluent";
            process.StartInfo.ArgumentList.Add(request);

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = false;

            await Task.Run(() => process.Start());

            return process;
        }
    }
}

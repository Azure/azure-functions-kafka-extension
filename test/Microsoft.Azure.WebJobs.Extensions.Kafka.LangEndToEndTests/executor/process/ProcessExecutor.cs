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
        public ProcessExecutor()
        {
        }
        public async Task<Process> ExecuteAsync(string request)
        {
            Process process = new Process();
            string shell = null;
            /*if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shell = "cmd.exe";
            } else
            {
                shell = "/bin/bash";
            }*/

            //Process(./path_to_script) -- Try
            //How do we run scripts here? - Implementation Detail
            //What will we do with this shell?


            // build process command
            //process.Start();
            return process;
            // TODO execute Process Object
        }
    }
}

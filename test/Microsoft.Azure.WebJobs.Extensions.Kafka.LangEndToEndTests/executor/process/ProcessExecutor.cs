using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.executor.process
{
    public class ProcessExecutor : IExecutor<string, Process>
    {
        public ProcessExecutor()
        {
        }
        public Process Execute(string request)
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
            //process.Start();
            return process;
            // TODO execute Process Object
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.process
{
	public class ProcessManager: IDisposable
	{
		private static ProcessManager instance = new ProcessManager();
		private static List<Process> processList;
		public static ProcessManager GetInstance()
		{
			return instance;
		}

		public void Dispose()
		{
			foreach (Process process in processList)
			{
				process.Kill();
			}
		}

		private ProcessManager()
		{
			processList = new List<Process>();
		}

		public void AddProcess(Process process) 
		{ 
			processList.Add(process);
		}

	}
}

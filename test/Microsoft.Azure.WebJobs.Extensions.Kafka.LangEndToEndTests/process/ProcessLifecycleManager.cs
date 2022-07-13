using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.process
{
	/* Responsible for keeping a list of all created processes 
	 * and killing them during cleanup phase 
	*/
	public class ProcessLifecycleManager: IDisposable
	{
		private static ProcessLifecycleManager instance = new ProcessLifecycleManager();
		private static List<Process> processList;
		public static ProcessLifecycleManager GetInstance()
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

		private ProcessLifecycleManager()
		{
			processList = new List<Process>();
		}

		public void AddProcess(Process process) 
		{ 
			processList.Add(process);
		}

	}
}

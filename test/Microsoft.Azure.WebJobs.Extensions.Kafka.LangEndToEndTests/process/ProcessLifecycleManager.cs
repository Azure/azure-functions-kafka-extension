using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
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
		private static Dictionary<Tuple<BrokerType, Language>, List<Process>> processDictionary;
		public static ProcessLifecycleManager GetInstance()
		{
			return instance;
		}

		public void Dispose()
		{
			foreach (var keyValuePair in processDictionary) 
			{
				foreach (Process process in keyValuePair.Value)
				{
					process.Kill(true);
				}
			}
		}

		public void Dispose(Language language, BrokerType brokerType)
		{
			var brokerLangTuple = new Tuple<BrokerType, Language>(brokerType, language);

			processDictionary.TryGetValue(brokerLangTuple, out var processList);
			
			foreach (Process process in processList)
			{
				process.Kill(true);
			}

		}

		private ProcessLifecycleManager()
		{
			processDictionary = new Dictionary<Tuple<BrokerType, Language>, List<Process>>();
		}

		public void AddProcess(Language language, BrokerType brokerType, Process process) 
		{
			var brokerLangTuple = new Tuple<BrokerType, Language>(brokerType, language);
			if (processDictionary.ContainsKey(brokerLangTuple))
			{
				var processList = processDictionary[brokerLangTuple];
				processList.Add(process);
			}
			processDictionary.Add(brokerLangTuple, new List<Process>() { process });
		}

	}
}

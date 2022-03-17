using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util
{
	public static class Utils
	{
		public static string GetEnvVariable(string varName) { return Environment.GetEnvironmentVariable(varName); }
		public static string BuildCloudBrokerName(QueueType queueType, AppType appType, Language language)
		{
			//return Constants.
			return Constants.E2E + Constants.HIPHEN + Constants.KAFKA + Constants.HIPHEN + language.ToString().ToLower() + Constants.HIPHEN + GiveAppTypeInString(appType) + Constants.HIPHEN + queueType.ToString().ToLower();
		}

		public static string BuildStorageQueueName(BrokerType brokerType, AppType appType, Language language)
		{
			return Constants.E2E + Constants.HIPHEN + language.ToString().ToLower() + Constants.HIPHEN + GiveAppTypeInString(appType) + Constants.HIPHEN + brokerType.ToString().ToLower();
		}

		public static string GiveAppTypeInString(AppType appType)
		{
			return appType == AppType.SINGLE_EVENT ? Constants.SINGLE : Constants.MULTI;
		}

	}
}

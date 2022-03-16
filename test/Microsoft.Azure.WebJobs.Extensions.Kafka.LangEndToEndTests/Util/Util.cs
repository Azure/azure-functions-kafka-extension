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
			return Constants.E2E + Constants.HIPHEN + Constants.KAFKA + Constants.HIPHEN + language.ToString() + Constants.HIPHEN + GiveAppTypeInString(appType) + Constants.HIPHEN + queueType.ToString();
		}

		public static string BuildStorageQueueName(QueueType queueType, AppType appType, Language language)
		{
			return Constants.E2E + Constants.HIPHEN + language.ToString() + Constants.HIPHEN + GiveAppTypeInString(appType) + Constants.HIPHEN + queueType.ToString();
		}

		public static string GiveAppTypeInString(AppType appType)
		{
			return appType == AppType.SINGLE_EVENT ? "single" : "multi";
		}

	}
}

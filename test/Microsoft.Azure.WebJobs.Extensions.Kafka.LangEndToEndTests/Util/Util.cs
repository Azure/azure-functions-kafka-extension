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
			return Constants.E2E + Constants.HIPHEN + Constants.KAFKA + Constants.HIPHEN + LanguageToLower(language) + Constants.HIPHEN + GiveAppTypeInString(appType) + Constants.HIPHEN + queueType.ToString().ToLower();
		}

		public static string BuildStorageQueueName(BrokerType brokerType, AppType appType, Language language)
		{
			return Constants.E2E + Constants.HIPHEN + LanguageToLower(language) + Constants.HIPHEN + GiveAppTypeInString(appType) + Constants.HIPHEN + brokerType.ToString().ToLower();
		}

		public static string LanguageToLower(Language language)
		{
			if (language == Language.DOTNETISOLATED)
				return Constants.DOTNETISOLATED;
			return language.ToString().ToLower();
		}

		public static string GiveAppTypeInString(AppType appType)
		{
			return appType == AppType.SINGLE_EVENT ? Constants.SINGLE : Constants.MULTI;
		}

		public static string Base64Decode(string base64EncodedData)
		{
			var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
			return Encoding.UTF8.GetString(base64EncodedBytes);
		}
	}
}

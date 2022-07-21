// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.brokers;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.type;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.entity;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util
{
	/* Collection of Utility functions
	*/
	public static class Utils
	{
		public static string GetEnvVariable(string varName) { return Environment.GetEnvironmentVariable(varName); }
		public static string BuildCloudBrokerName(QueueType queueType, AppType appType, Language language)
		{
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
			if (string.IsNullOrEmpty(base64EncodedData))
			{
				throw new ArgumentNullException(base64EncodedData);
			}
			var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
			return Encoding.UTF8.GetString(base64EncodedBytes);
		}

		public static List<string> GenerateRandomMsgs(AppType appType)
		{
			var randomStrings = new List<string>();
			
			int numMsgs = (appType == AppType.SINGLE_EVENT ? Constants.SINGLE_MESSAGE_COUNT : Constants.BATCH_MESSAGE_COUNT);
			for (int i = 0; i < numMsgs; i++)
			{
				randomStrings.Add(Guid.NewGuid().ToString());
			}

			return randomStrings;
		}

		private static string GenerateTriggerUrl(string portNum, string appName)
		{
			return "http://localhost:" + portNum + "/api/" + appName;
		}

		public static HttpRequestEntity GenerateTestHttpRequestEntity(string portNum, string appName, List<string> reqMsgs)
		{
			//Generate Trigger Url
			string triggerUrl = Utils.GenerateTriggerUrl(portNum, appName);

			//Generate Request Query Params
			Dictionary<string, string> reqParms = new Dictionary<string, string>();
			for (int i = 0; i < reqMsgs.Count; i++)
			{
				reqParms.TryAdd(Constants.IndexQueryParamMapping[i], reqMsgs[i]);
			}

			return new HttpRequestEntity(triggerUrl, HttpMethods.Get,
			   null, reqParms, null);
		}
	}
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests
{
	public class JavaConfluentAppTest : BaseE2E, IClassFixture<KafkaE2EFixture>
	{
		private readonly KafkaE2EFixture _kafkaE2EFixture;
		readonly ITestOutputHelper _output;

		public JavaConfluentAppTest(KafkaE2EFixture kafkaE2EFixture, ITestOutputHelper output) : base(kafkaE2EFixture, Language.JAVA, BrokerType.CONFLUENT, output)
		{
			_kafkaE2EFixture = kafkaE2EFixture;
			_output = output;
		}

		[Fact]
		public async Task Java_App_Test_Single_Event_Confluent()
		{
			//Generate Random Guids
			List<string> reqMsgs = Utils.GenerateRandomMsgs(AppType.SINGLE_EVENT);

			//Create HttpRequestEntity with url and query parameters
			HttpRequestEntity httpRequestEntity = Utils.GenerateTestHttpRequestEntity(Constants.JAVAAPP_CONFLUENT_PORT, Constants.JAVA_SINGLE_APP_NAME, reqMsgs);

			//Test e2e flow with trigger httpRequestEntity and expectedOutcome
			await Test(AppType.SINGLE_EVENT, InvokeType.HTTP, httpRequestEntity, null, reqMsgs);

		}

		[Fact]
		public async Task Java_App_Test_Multi_Event_Confluent()
		{
			//Generate Random Guids
			List<string> reqMsgs = Utils.GenerateRandomMsgs(AppType.BATCH_EVENT);

			//Create HttpRequestEntity with url and query parameters
			var httpRequestEntity = Utils.GenerateTestHttpRequestEntity(Constants.JAVAAPP_CONFLUENT_PORT, Constants.JAVA_MULTI_APP_NAME, reqMsgs);

			//Test e2e flow with trigger httpRequestEntity and expectedOutcome
			await Test(AppType.BATCH_EVENT, InvokeType.HTTP, httpRequestEntity, null, reqMsgs);
		}

	}
}

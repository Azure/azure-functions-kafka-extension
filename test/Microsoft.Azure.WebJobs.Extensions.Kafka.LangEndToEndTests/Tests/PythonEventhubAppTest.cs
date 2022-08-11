// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests;

public class PythonEventhubAppTest : BaseE2E, IClassFixture<KafkaE2EFixture>
{
	private readonly KafkaE2EFixture _kafkaE2EFixture;
	private readonly ITestOutputHelper _output;

	public PythonEventhubAppTest(KafkaE2EFixture kafkaE2EFixture, ITestOutputHelper output) : base(kafkaE2EFixture,
		Language.PYTHON, BrokerType.EVENTHUB, output)
	{
		_kafkaE2EFixture = kafkaE2EFixture;
		_output = output;
	}

	[Fact]
	public async Task Python_App_Test_Single_Event_Eventhub()
	{
		//Generate Random Guids
		var reqMsgs = Utils.GenerateRandomMsgs(AppType.SINGLE_EVENT);

		//Create HttpRequestEntity with url and query parameters
		var httpRequestEntity = Utils.GenerateTestHttpRequestEntity(Constants.PYTHONAPP_EVENTHUB_PORT,
			Constants.PYTHON_SINGLE_APP_NAME, reqMsgs);

		//Test e2e flow with trigger httpRequestEntity and expectedOutcome
		await Test(AppType.SINGLE_EVENT, InvokeType.HTTP, httpRequestEntity, null, reqMsgs);
	}


	[Fact]
	public async Task Python_App_Test_Multi_Event_Confluent()
	{
		//Generate Random Guids
		var reqMsgs = Utils.GenerateRandomMsgs(AppType.BATCH_EVENT);

		//Create HttpRequestEntity with url and query parameters
		var httpRequestEntity = Utils.GenerateTestHttpRequestEntity(Constants.PYTHONAPP_EVENTHUB_PORT,
			Constants.PYTHON_MULTI_APP_NAME, reqMsgs);

		//Test e2e flow with trigger httpRequestEntity and expectedOutcome
		await Test(AppType.BATCH_EVENT, InvokeType.HTTP, httpRequestEntity, null, reqMsgs);
	}
}
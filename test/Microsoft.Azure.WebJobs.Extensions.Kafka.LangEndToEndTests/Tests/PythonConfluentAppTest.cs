// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests;

public class PythonConfluentAppTest : BaseE2E, IClassFixture<PythonConfluentE2EFixture>
{
	private readonly ITestOutputHelper _output;

	public PythonConfluentAppTest(ITestOutputHelper output) : base(Language.PYTHON, BrokerType.CONFLUENT, output)
	{
		_output = output;
	}

	[Fact(Skip = "Not currently working due to Confluent Infra")]
	public async Task Python_App_Test_Single_Event_Confluent()
	{
		//Generate Random Guids
		var reqMsgs = Utils.GenerateRandomMsgs(AppType.SINGLE_EVENT);

		//Create HttpRequestEntity with url and query parameters
		var httpRequestEntity = Utils.GenerateTestHttpRequestEntity(Constants.PYTHONAPP_CONFLUENT_PORT,
			Constants.PYTHON_SINGLE_APP_NAME, reqMsgs);

		//Test e2e flow with trigger httpRequestEntity and expectedOutcome
		await Test(AppType.SINGLE_EVENT, InvokeType.HTTP, httpRequestEntity, null, reqMsgs);
	}

	[Fact(Skip = "Not currently working due to Confluent Infra")]
	public async Task Python_App_Test_Multi_Event_Confluent()
	{
		//Generate Random Guids
		var reqMsgs = Utils.GenerateRandomMsgs(AppType.BATCH_EVENT);

		//Create HttpRequestEntity with url and query parameters
		var httpRequestEntity = Utils.GenerateTestHttpRequestEntity(Constants.PYTHONAPP_CONFLUENT_PORT,
			Constants.PYTHON_MULTI_APP_NAME, reqMsgs);

		//Test e2e flow with trigger httpRequestEntity and expectedOutcome
		await Test(AppType.BATCH_EVENT, InvokeType.HTTP, httpRequestEntity, null, reqMsgs);
	}
}
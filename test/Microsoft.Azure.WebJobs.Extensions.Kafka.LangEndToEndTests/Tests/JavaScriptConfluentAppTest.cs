// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests;

public class JavaScriptConfluentAppTest : BaseE2E, IClassFixture<JavaScriptConfluentE2EFixture>
{
	private readonly ITestOutputHelper _output;

	public JavaScriptConfluentAppTest(ITestOutputHelper output) : base(Language.JAVASCRIPT, BrokerType.CONFLUENT, output)
	{
		_output = output;
	}

	[IgnoreOnDisableConfluentTestsFlagFact]
	public async Task JavaScript_App_Test_Single_Event_Confluent()
	{
		var reqMsgs = Utils.GenerateRandomMsgs(AppType.SINGLE_EVENT);

		var httpRequestEntity = Utils.GenerateTestHttpRequestEntity(Constants.JSAPP_CONFLUENT_PORT,
			Constants.JS_SINGLE_APP_NAME, reqMsgs);

		await Test(AppType.SINGLE_EVENT, InvokeType.HTTP, httpRequestEntity, null, reqMsgs);
	}

	[IgnoreOnDisableConfluentTestsFlagFact]
	public async Task JavaScript_App_Test_Multi_Event_Confluent()
	{
		var reqMsgs = Utils.GenerateRandomMsgs(AppType.BATCH_EVENT);

		var httpRequestEntity = Utils.GenerateTestHttpRequestEntity(Constants.JSAPP_CONFLUENT_PORT,
			Constants.JS_MULTI_APP_NAME, reqMsgs);

		await Test(AppType.BATCH_EVENT, InvokeType.HTTP, httpRequestEntity, null, reqMsgs);
	}
}
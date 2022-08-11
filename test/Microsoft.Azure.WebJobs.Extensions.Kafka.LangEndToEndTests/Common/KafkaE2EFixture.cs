// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

/* Common fixture for all language test case classes which does -
* Azure Infra setup and Func Apps Startup
* Stopping Func Apps and Azure Infra cleanup
*/
public abstract class KafkaE2EFixture : IAsyncLifetime
{
	private readonly ILogger _logger = TestLogger.GetTestLogger();
	private BrokerType _brokerType;
	private Language _language;

	public KafkaE2EFixture(BrokerType brokerType, Language language)
	{
		_logger.LogInformation($"Kafkae2efixture for {_language} {_brokerType}");
		_brokerType = brokerType;
		_language = language;
	}

	async Task IAsyncLifetime.DisposeAsync()
	{
		//Stopping Func Apps and Azure Infra cleanup
		var testSuitCleaner = new TestSuiteCleaner();
		await testSuitCleaner.CleanupTestSuiteAsync(_language, _brokerType);
		_logger.LogInformation("DisposeAsync");
	}

	async Task IAsyncLifetime.InitializeAsync()
	{
		_logger.LogInformation("InitializeAsync");
		
		//Azure Infra setup and Func Apps Startup
		TestSuitInitializer testSuitInitializer = new();
		await testSuitInitializer.InitializeTestSuitAsync(_language, _brokerType);
	}
}
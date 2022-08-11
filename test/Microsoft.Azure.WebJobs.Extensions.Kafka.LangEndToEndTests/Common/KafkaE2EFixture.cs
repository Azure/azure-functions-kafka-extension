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
public class KafkaE2EFixture : IAsyncLifetime
{
	private readonly ILogger _logger = TestLogger.GetTestLogger();
	private AppType _appType;
	private BrokerType _brokerType;
	private Language _language;
	protected bool isInitialized;

	public KafkaE2EFixture()
	{
		_logger.LogInformation($"Kafkae2efixture for {_language} {_appType} {_brokerType}");
	}

	async Task IAsyncLifetime.DisposeAsync()
	{
		//Stopping Func Apps and Azure Infra cleanup
		var testSuitCleaner = new TestSuiteCleaner();
		await testSuitCleaner.CleanupTestSuiteAsync(_language, _brokerType);
		_logger.LogInformation("DisposeAsync");
	}

	Task IAsyncLifetime.InitializeAsync()
	{
		_logger.LogInformation("InitializeAsync");
		return Task.CompletedTask;
	}

	public void OrchestrateInitialization()
	{
		if (isInitialized)
		{
			return;
		}

		//Azure Infra setup and Func Apps Startup
		TestSuitInitializer testSuitInitializer = new();
		testSuitInitializer.InitializeTestSuit(_language, _brokerType);

		isInitialized = true;
	}

	public void SetLanguage(Language language)
	{
		_language = language;
	}

	public void SetAppType(AppType appType)
	{
		_appType = appType;
	}

	public void SetBrokerType(BrokerType brokerType)
	{
		_brokerType = brokerType;
	}
}
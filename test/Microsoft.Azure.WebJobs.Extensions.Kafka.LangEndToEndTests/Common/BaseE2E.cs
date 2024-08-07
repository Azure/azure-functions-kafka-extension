﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.


using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xunit.Abstractions;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

/* This class acts as the base class for all the language test case classes.
* Takes care of Initial Orchestration and actual flow of the test.
*/
public class BaseE2E
{
	private readonly BrokerType _brokerType;
	private readonly E2ETestInvoker _invoker;
	private readonly Language _language;
	private readonly ILogger _logger = TestLogger.GetTestLogger();

	protected BaseE2E(Language language, BrokerType brokerType,
		ITestOutputHelper output)
	{
		_language = language;
		_brokerType = brokerType;
		_invoker = new E2ETestInvoker();
	}

	public async Task Test(AppType appType, InvokeType invokeType, HttpRequestEntity httpRequestEntity,
		KafkaEntity queueEntity, List<string> expectedOutput)
	{
		//Send invocation Http request to the function apps 
		await InvokeE2ETest(appType, invokeType, httpRequestEntity, queueEntity);

		// wait for the function completion
		await Task.Delay(60000);

		// invokation for read from storage
		await VerifyQueueMsgsAsync(expectedOutput, appType);
	}

	private async Task VerifyQueueMsgsAsync(List<string> expectedOutput, AppType appType)
	{
		var storageQueueName = Utils.BuildStorageQueueName(_brokerType,
			appType, _language);

		IExecutableCommand<QueueResponse> readQueue;
		if (AppType.BATCH_EVENT == appType)
		{
			readQueue = new QueueCommand(QueueType.AzureStorageQueue,
				QueueOperation.READMANY, storageQueueName);
		}
		else
		{
			readQueue = new QueueCommand(QueueType.AzureStorageQueue,
				QueueOperation.READ, storageQueueName);
		}

		var queueMsgs = await readQueue.ExecuteCommandAsync();

		CollectionAssert.AreEquivalent(expectedOutput, queueMsgs.ResponseList);
	}

	private async Task InvokeE2ETest(AppType appType, InvokeType invokeType, HttpRequestEntity httpRequestEntity,
		KafkaEntity queueEntity)
	{
		if (httpRequestEntity != null && InvokeType.HTTP == invokeType)
		{
			try
			{
				IInvokeRequestStrategy<HttpResponseMessage> invokerHttpReqStrategy =
					new InvokeHttpRequestStrategy(httpRequestEntity);
				await _invoker.Invoke(invokerHttpReqStrategy);
			}
			catch (Exception ex)
			{
				_logger.LogError(
					$"Unable to invoke functions for language:{_language} broker:{_brokerType} with exception {ex}");
				throw;
			}
		}
		else
		{
			IInvokeRequestStrategy<string> invokerKafkaReqStrategy = new InvokeKafkaRequestStrategy("");
			_ = _invoker.Invoke(invokerKafkaReqStrategy);
		}
	}
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Placeholder class for Kafka requests
public class InvokeKafkaRequestStrategy : IInvokeRequestStrategy<string>
{
	private readonly IExecutor<IExecutableCommand<string>, string> _kafkaCommandExecutor;

	public InvokeKafkaRequestStrategy(string kafkaProducerRequestEntity)
	{
	}

	public async Task<string> InvokeRequestAsync()
	{
		throw new NotImplementedException();
	}
}
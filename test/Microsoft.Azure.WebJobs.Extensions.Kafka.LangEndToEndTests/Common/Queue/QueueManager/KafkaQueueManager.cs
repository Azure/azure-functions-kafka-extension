// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// TODO for future refernce
public class KafkaQueueManager : IQueueManager<string, string>
{
	private static readonly int _MAX_RETRY_COUNT = 3;

	//private readonly string username;
	//private readonly string apiKey;
	private static readonly KafkaQueueManager instance = new();

	public async Task CreateAsync(string queueName)
	{
		throw new NotImplementedException();
	}

	public async Task ClearAsync(string queueName)
	{
		throw new NotImplementedException();
	}

	public async Task DeleteAsync(string queueName)
	{
		throw new NotImplementedException();
	}

	public async Task<string> ReadAsync(int batchSize, string queueName)
	{
		throw new NotImplementedException();
	}

	public async Task<string> WriteAsync(string messageEntity, string queueName)
	{
		throw new NotImplementedException();
	}

	public KafkaQueueManager GetInstance()
	{
		return instance;
	}
}
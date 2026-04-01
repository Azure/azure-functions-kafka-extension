// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// TODO for future refernce
public class KafkaQueueManager : IQueueManager<string, string>
{
	//private readonly string username;
	//private readonly string apiKey;
	private static readonly KafkaQueueManager instance = new();

	public Task CreateAsync(string queueName)
	{
		throw new NotImplementedException();
	}

	public Task ClearAsync(string queueName)
	{
		throw new NotImplementedException();
	}

	public Task DeleteAsync(string queueName)
	{
		throw new NotImplementedException();
	}

	public Task<string> ReadAsync(int batchSize, string queueName)
	{
		throw new NotImplementedException();
	}

	public Task<string> WriteAsync(string messageEntity, string queueName)
	{
		throw new NotImplementedException();
	}

	public KafkaQueueManager GetInstance()
	{
		return instance;
	}
}
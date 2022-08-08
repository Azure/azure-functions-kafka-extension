// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Interface for Management of External Resources required for testing.
public interface IQueueManager<Request, Response>
{
	Task<Response> ReadAsync(int batchSize, string queueName);
	Task<Response> WriteAsync(Request messageEntity, string queueName);
	Task CreateAsync(string queueName);
	Task DeleteAsync(string queueName);
	Task ClearAsync(string queueName);
}
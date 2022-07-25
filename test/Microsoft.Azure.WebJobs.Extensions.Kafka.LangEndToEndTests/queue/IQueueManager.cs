// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue
{
    /* Interface for Management of External Resources required for testing.
    */
    public interface IQueueManager<Request, Response>
    {
        public Task<Response> ReadAsync(int batchSize, string queueName);
        public Task<Response> WriteAsync(Request messageEntity, string queueName);
        public Task CreateAsync(string queueName);
        public Task DeleteAsync(string queueName);
        public Task ClearAsync(string queueName);
    }
}

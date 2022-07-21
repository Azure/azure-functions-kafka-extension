// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.queue.kafka
{
    // TODO for future refernce
    public class KafkaQueueManager : IQueueManager<string, string>
    {
        private readonly static int MAX_RETRY_COUNT = 3;
        //private readonly string username;
        //private readonly string apiKey;
        private static KafkaQueueManager instance = new KafkaQueueManager();

        public KafkaQueueManager GetInstance()
        {
            return instance;
        }

        public async Task clearAsync(string queueName)
        {
            throw new NotImplementedException();
        }

        public async Task createAsync(string queueName)
        {
            throw new NotImplementedException();
        }

        public async Task deleteAsync(string queueName)
        {
            throw new NotImplementedException();
        }

        public async Task<string> readAsync(int batchSize, string queueName)
        {
            throw new NotImplementedException();
        }

        public async Task<string> writeAsync(string messageEntity, string queueName)
        {
            throw new NotImplementedException();
        }
    }
}

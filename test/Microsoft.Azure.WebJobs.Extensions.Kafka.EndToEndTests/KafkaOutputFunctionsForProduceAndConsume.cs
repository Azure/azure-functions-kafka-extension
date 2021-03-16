// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    public class KafkaOutputFunctionsForProduceAndConsume<T> where T: IKafkaEventData
    {
        private readonly List<T> testData;

        public KafkaOutputFunctionsForProduceAndConsume(List<T> testData)
        {
            this.testData = testData;
        }
        public async Task Produce(
            [Kafka(BrokerList = "LocalBroker")] IAsyncCollector<T> output)
        {
            foreach (var message in testData)
            {
                await output.AddAsync(message);
            }
        }
    }
}
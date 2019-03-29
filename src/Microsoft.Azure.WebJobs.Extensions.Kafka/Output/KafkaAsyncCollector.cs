// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaAsyncCollector : IAsyncCollector<KafkaEventData>
    {
        private readonly string topic;
        private readonly IKafkaProducer producer;

        public KafkaAsyncCollector(string topic, IKafkaProducer producer)
        {
            this.topic = topic;
            this.producer = producer;
        }

        public async Task AddAsync(KafkaEventData item, CancellationToken cancellationToken = default)
        {
            await this.producer.ProduceAsync(this.topic, item, cancellationToken);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
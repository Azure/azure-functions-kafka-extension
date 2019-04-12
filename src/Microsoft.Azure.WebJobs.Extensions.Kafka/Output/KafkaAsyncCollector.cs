// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public sealed class KafkaAsyncCollector : IAsyncCollector<KafkaEventData>
    {
        private readonly string topic;
        private readonly IKafkaProducer producer;

        public KafkaAsyncCollector(string topic, IKafkaProducer producer)
        {
            if (producer == null)
            {
                throw new ArgumentNullException(nameof(producer));
            }

            this.topic = topic;
            this.producer = producer;
        }

        public async Task AddAsync(KafkaEventData item, CancellationToken cancellationToken = default)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            await this.producer.ProduceAsync(this.topic, item);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            this.producer.Dispose();
            return Task.CompletedTask;
        }
    }
}
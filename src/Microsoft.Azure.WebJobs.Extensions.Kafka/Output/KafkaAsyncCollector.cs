// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaAsyncCollector : IAsyncCollector<KafkaEventData>
    {
        private readonly string topic;
        private readonly IKafkaProducer producer;

        public KafkaAsyncCollector()
        {
        }

        public KafkaAsyncCollector(string topic, IKafkaProducer producer)
        {
            this.topic = topic;
            this.producer = producer;
        }

        public Task AddAsync(KafkaEventData item, CancellationToken cancellationToken = default)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            this.producer.Produce(this.topic, item);
            return Task.CompletedTask;
        }
        
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                this.producer.Flush(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // cancellationToken was cancelled
            }

            return Task.CompletedTask;
        }
    }
}
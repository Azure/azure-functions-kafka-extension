// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaProducerEntity
    {
        public IKafkaProducerFactory KafkaProducerFactory { get; set; }

        public Type KeyType { get; set; }

        public Type ValueType { get; set; }

        public string AvroSchema { get; set; }

        public string Topic { get; set; }

        public KafkaAttribute Attribute { get; set; }

        internal async Task SendAndCreateEntityIfNotExistsAsync<T>(T item, Guid functionInstanceId, CancellationToken cancellationToken)
        {
            var kafkaProducer = this.KafkaProducerFactory.Create(this);
            if (item is ICollection collection)
            {
                foreach (var collectionItem in collection)
                {
                    await kafkaProducer.ProduceAsync(this.Topic, collectionItem);
                }
            }
            else
            {
                await kafkaProducer.ProduceAsync(this.Topic, item);
            }
        }
    }
}
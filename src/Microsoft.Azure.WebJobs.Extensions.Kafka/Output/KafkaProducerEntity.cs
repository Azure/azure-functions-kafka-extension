// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            List<Task> tasks = new List<Task>();
            if (item is ICollection collection)
            {
                foreach (var collectionItem in collection)
                {
                    tasks.Add(kafkaProducer.ProduceAsync(this.Topic, this.GetItemToProduce(collectionItem)));
                }
            }
            else
            {
                tasks.Add(kafkaProducer.ProduceAsync(this.Topic, this.GetItemToProduce(item)));
            }
            await Task.WhenAll(tasks);
        }

        private object GetItemToProduce<T>(T item)
        {
            if (item is IKafkaEventData)
            {
                return item;
            }

            return new KafkaEventData<T>(item);
        }
    }
}
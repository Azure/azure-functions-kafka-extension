﻿// Copyright (c) .NET Foundation. All rights reserved.
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

        public string ValueAvroSchema { get; set; }

        public string KeyAvroSchema { get; set; }

        public string Topic { get; set; }

        public KafkaAttribute Attribute { get; set; }

        internal Task SendAndCreateEntityIfNotExistsAsync<T>(T item, Guid functionInstanceId, CancellationToken cancellationToken)
        {
            var kafkaProducer = this.KafkaProducerFactory.Create(this);

            if (item is ICollection)
            {
                ProduceEvents((ICollection)item, kafkaProducer);
                return Task.CompletedTask;
            }
            //await kafkaProducer.ProduceAsync(this.Topic, this.GetItemToProduce(item));
            kafkaProducer.Produce(this.Topic, this.GetItemToProduce(item));
            return Task.CompletedTask;
        }

        private void ProduceEvents(ICollection collection, IKafkaProducer kafkaProducer)
        {
            foreach (var collectionItem in collection)
            {
                kafkaProducer.Produce(this.Topic, this.GetItemToProduce(collectionItem));
            }
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
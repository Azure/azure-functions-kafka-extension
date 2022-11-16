// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

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

        internal Task SendAndCreateEntityIfNotExistsAsync<T>(T item, Guid functionInstanceId, CancellationToken cancellationToken)
        {
            var kafkaProducer = this.KafkaProducerFactory.Create(this);

            if (!(item is ICollection))
            {
                //await kafkaProducer.ProduceAsync(this.Topic, this.GetItemToProduce(item));
                kafkaProducer.Produce(this.Topic, this.GetItemToProduce(item));
                kafkaProducer.Flush();
                return Task.CompletedTask;
            }
            ICollection collection = (ICollection)item;
            /*if (EventsOrderType.KEY.Equals(this.Attribute.OrderType))
            {
                ProduceKafkaEventsByKey(collection, kafkaProducer);
            }*/

            //await ProduceEvents(collection, kafkaProducer);
            //ProduceEvents(collection, kafkaProducer);
            ProduceEvent(collection, kafkaProducer);
            return Task.CompletedTask;
        }

        /*private async void ProduceKafkaEventsByKey(ICollection items, IKafkaProducer kafkaProducer)
        {
            IDictionary<object, List<object>> eventMap = BuildKeyDictionary(items);
            if (eventMap.Count == 0)
            {
                return;
            }
            List<Task> taskList = new List<Task>();
            foreach (KeyValuePair<object, List<object>> entry in eventMap)
            {
                taskList.Add(ProduceEvents(entry.Value, kafkaProducer));
            }
            await Task.WhenAll(taskList);
        }

        private static IDictionary<object, List<object>> BuildKeyDictionary(ICollection items)
        {
            IDictionary<object, List<object>> eventMap = new Dictionary<object, List<object>>();
            foreach (var item in items)
            {
                object key = item.GetType().GetProperty("Key").GetValue(item);
                List<object> eventDataList = eventMap.TryGetValue(key, out eventDataList) ? eventDataList : new List<object>();
                eventDataList.Add(item);
                eventMap[key] = eventDataList;
            }

            return eventMap;
        }

        private void ProduceEvents(ICollection collection, IKafkaProducer kafkaProducer)
        {
            if (collection == null || collection.Count == 0)
            {
                return;
            }
            if (EventsOrderType.NONE.Equals(this.Attribute.OrderType))
            {
                await ProduceEventsAsync(collection, kafkaProducer);
                return;
            }
            ProduceEvent(collection, kafkaProducer);
        }*/

        private void ProduceEvent(ICollection collection, IKafkaProducer kafkaProducer)
        {
            foreach (var collectionItem in collection)
            {
                kafkaProducer.Produce(this.Topic, this.GetItemToProduce(collectionItem));
            }
            kafkaProducer.Flush();
        }

        /*private async Task ProduceEventsAsync(ICollection collection, IKafkaProducer kafkaProducer)
        {
            List<Task> tasks = new List<Task>();
            foreach (var collectionItem in collection)
            {
                tasks.Add(kafkaProducer.ProduceAsync(this.Topic, this.GetItemToProduce(collectionItem)));
            }
            await Task.WhenAll(tasks);
        }*/

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
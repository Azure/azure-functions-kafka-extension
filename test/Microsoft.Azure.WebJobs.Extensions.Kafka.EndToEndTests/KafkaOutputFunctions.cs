// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    public class KafkaOutputFunctions
    {
        public static async Task SendToStringTopic(
            string topic,
            IEnumerable<string> content,
            [Kafka(BrokerList = "LocalBroker", ValueType = typeof(string))] IAsyncCollector<KafkaEventData> output)
        {
            foreach (var c in content)
            {
                var message = new KafkaEventData()
                {
                    Topic = topic,
                    Value = c,
                };

                await output.AddAsync(message);
            }
        }

        public static async Task SendToStringWithLongKeyTopic(
            string topic,
            IEnumerable<long> keys,
            IEnumerable<string> content,
            [Kafka(BrokerList = "LocalBroker", KeyType = typeof(long), ValueType = typeof(string))] IAsyncCollector<KafkaEventData> output)
        {

            var keysEnumerator = keys.GetEnumerator();
            foreach (var c in content)
            {
                keysEnumerator.MoveNext();
                var message = new KafkaEventData()
                {
                    Key = keysEnumerator.Current,
                    Topic = topic,
                    Value = c,
                };

                await output.AddAsync(message);
            }
        }

        public static async Task SendAvroWithStringKeyTopic(
            string topic,
            IEnumerable<string> keys,
            IEnumerable<string> content,
            [Kafka(BrokerList = "LocalBroker", KeyType = typeof(string), ValueType = typeof(MyAvroRecord))] IAsyncCollector<KafkaEventData> output)
        {

            var keysEnumerator = keys.GetEnumerator();
            foreach (var c in content)
            {
                keysEnumerator.MoveNext();

                var message = new KafkaEventData()
                {
                    Key = keysEnumerator.Current,
                    Topic = topic,
                    Value = new MyAvroRecord()
                    {
                        ID = c,
                        Ticks = DateTime.UtcNow.Ticks,
                    },
                };

                await output.AddAsync(message);
            }
        }

        public static async Task SendProtobufWithStringKeyTopic(
            string topic,
            IEnumerable<string> keys,
            IEnumerable<string> content,
            [Kafka(BrokerList = "LocalBroker", KeyType = typeof(string), ValueType = typeof(ProtoUser))] IAsyncCollector<KafkaEventData> output)
        {
            var colors = new[] { "red", "blue", "green" };

            var keysEnumerator = keys.GetEnumerator();
            var i = 0;
            foreach (var c in content)
            {
                keysEnumerator.MoveNext();

                var message = new KafkaEventData()
                {
                    Key = keysEnumerator.Current,
                    Topic = topic,
                    Value = new ProtoUser()
                    {
                        Name = c,
                        FavoriteColor = colors[i % colors.Length],
                        FavoriteNumber = i,
                    },
                };

                await output.AddAsync(message);
                i++;
            }
        }
    }
}
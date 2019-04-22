// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    public class KafkaOutputFunctions
    {
        public static async Task Produce_AsyncCollector_String_Without_Key(
            string topic,
            IEnumerable<string> content,
            [Kafka(BrokerList = "LocalBroker")] IAsyncCollector<KafkaEventData<string>> output)
        {
            foreach (var c in content)
            {
                var message = new KafkaEventData<string>()
                {
                    Topic = topic,
                    Value = c,
                };

                await output.AddAsync(message);
            }
        }

        public static async Task Produce_AsyncColletor_Raw_ByteArray_Without_Key(
            string topic,
            IEnumerable<string> content,
            [Kafka("LocalBroker", Constants.StringTopicWithTenPartitionsName)] IAsyncCollector<byte[]> output)
        {
            foreach (var c in content)
            {
                await output.AddAsync(Encoding.UTF8.GetBytes(c));
            }
        }

        public static void Produce_Out_Parameter_KafkaEventData_Array_String_With_String_Key(
            string topic,
            IEnumerable<string> content,
            IEnumerable<string> keys,
            [Kafka(BrokerList = "LocalBroker")] out KafkaEventData<string, string>[] output)
        {
            var list = new List<KafkaEventData<string, string>>();
            var keysEnumerator = keys.GetEnumerator();
            foreach (var c in content)
            {
                keysEnumerator.MoveNext();
                list.Add(new KafkaEventData<string, string>()
                {
                    Topic = topic,
                    Value = c,
                    Key = keysEnumerator.Current,
                });
            }

            output = list.ToArray();
        }

        [return: Kafka(BrokerList = "LocalBroker")]
        public static KafkaEventData<string, string>[] Produce_Return_Parameter_KafkaEventData_Array_String_With_String_Key(
            string topic,
            IEnumerable<string> content,
            IEnumerable<string> keys)
        {
            var list = new List<KafkaEventData<string, string>>();
            var keysEnumerator = keys.GetEnumerator();
            foreach (var c in content)
            {
                keysEnumerator.MoveNext();
                list.Add(new KafkaEventData<string, string>()
                {
                    Topic = topic,
                    Value = c,
                    Key = keysEnumerator.Current,
                });
            }

            return list.ToArray();
        }

        
        [return: Kafka("LocalBroker", Constants.StringTopicWithTenPartitionsName)]
        public static string[] Produce_Return_Parameter_Raw_String_Array(
            string topic,
            IEnumerable<string> content)
        {
            var list = new List<string>();
            foreach (var c in content)
            {
                list.Add(c);
            }

            return list.ToArray();
        }

        public static void Produce_Out_Parameter_KafkaEventData_Array_String_Without_Key(
            string topic,
            IEnumerable<string> content,
            [Kafka(BrokerList = "LocalBroker")] out KafkaEventData<string>[] output)
        {
            var list = new List<KafkaEventData<string>>();
            foreach (var c in content)
            {
                list.Add(new KafkaEventData<string>()
                {
                    Topic = topic,
                    Value = c,
                });
            }

            output = list.ToArray();
        }

        public static async Task Produce_AsyncColletor_Raw_String_Without_Key(
            string topic,
            IEnumerable<string> content,
            [Kafka("LocalBroker", Constants.StringTopicWithTenPartitionsName)] IAsyncCollector<string> output)
        {
            foreach (var c in content)
            {
                await output.AddAsync(c);
            }
        }

        public static async Task Produce_AsyncCollector_String_With_Long_Key(
            string topic,
            IEnumerable<long> keys,
            IEnumerable<string> content,
            [Kafka(BrokerList = "LocalBroker")] IAsyncCollector<KafkaEventData<long, string>> output)
        {

            var keysEnumerator = keys.GetEnumerator();
            foreach (var c in content)
            {
                keysEnumerator.MoveNext();
                var message = new KafkaEventData<long, string>()
                {
                    Key = keysEnumerator.Current,
                    Topic = topic,
                    Value = c,
                };

                await output.AddAsync(message);
            }
        }

        public static async Task Produce_AsyncCollector_Avro_With_String_key(
            string topic,
            IEnumerable<string> keys,
            IEnumerable<string> content,
            [Kafka(BrokerList = "LocalBroker")] IAsyncCollector<KafkaEventData<string, MyAvroRecord>> output)
        {

            var keysEnumerator = keys.GetEnumerator();
            foreach (var c in content)
            {
                keysEnumerator.MoveNext();

                var message = new KafkaEventData<string, MyAvroRecord>()
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

        public static async Task Produce_AsyncCollector_Raw_SpecificAvro(
            string topic,
            IEnumerable<string> content,
            [Kafka("LocalBroker", Constants.MyAvroRecordTopicName)] IAsyncCollector<MyAvroRecord> output)
        {

            foreach (var c in content)
            {                
                await output.AddAsync(new MyAvroRecord()
                {
                    ID = c,
                    Ticks = DateTime.UtcNow.Ticks,
                });
            }
        }

        [return: Kafka("LocalBroker", Constants.MyProtobufTopicName)]
        public static ProtoUser[] Produce_Return_Parameter_Raw_Protobuf_Without_Key(
            string topic,
            IEnumerable<string> content)
        {
            var colors = new[] { "red", "blue", "green" };

            var list = new List<ProtoUser>();
            var i = 0;
            foreach (var c in content)
            {
                list.Add(new ProtoUser()
                {
                    Name = c,
                    FavoriteColor = colors[i % colors.Length],
                    FavoriteNumber = i,
                });

                i++;
            }

            return list.ToArray();
        }

        public static async Task Produce_AsyncCollector_Protobuf_With_String_Key(
            string topic,
            IEnumerable<string> keys,
            IEnumerable<string> content,
            [Kafka(BrokerList = "LocalBroker")] IAsyncCollector<KafkaEventData<string, ProtoUser>> output)
        {
            var colors = new[] { "red", "blue", "green" };

            var keysEnumerator = keys.GetEnumerator();
            var i = 0;
            foreach (var c in content)
            {
                keysEnumerator.MoveNext();

                var message = new KafkaEventData<string, ProtoUser>()
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
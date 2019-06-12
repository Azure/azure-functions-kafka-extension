// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Avro.Generic;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    internal static class MultiItem_KafkaEventData_String_Without_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithOnePartitionName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<string>[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                log.LogInformation(kafkaEvent.Value.ToString());
            }
        }
    }

    internal static class MultiItem_KafkaEventData_String_With_Ignore_Key_Trigger
    {        public static void Trigger(
               [KafkaTrigger("LocalBroker", Constants.StringTopicWithTenPartitionsName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<Ignore, string>[] kafkaEvents,
               ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                log.LogInformation(kafkaEvent.Value.ToString());
            }
        }

    }

    internal static class SingleItem_Raw_String_Without_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithTenPartitionsName, ConsumerGroup = Constants.ConsumerGroupID)] string kafkaEvent,
            ILogger log)
        {
            log.LogInformation(kafkaEvent);
        }
    }

    internal static class MultiItem_Raw_StringArray_Without_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithTenPartitionsName, ConsumerGroup = Constants.ConsumerGroupID)] string[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
                log.LogInformation(kafkaEvent);
        }
    }

    internal static class MultiItem_KafkaEventData_String_With_String_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithTenPartitionsName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<string, string>[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                log.LogInformation("{key}:{value}", kafkaEvent.Key, kafkaEvent.Value);
            }
        }
    }
    

    internal static class MultiItem_String_With_Long_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithOnePartitionName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<long, string>[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                log.LogInformation("{key}:{value}", kafkaEvent.Key, kafkaEvent.Value);
            }
        }
    }

    internal static class MultiItem_RawByteArray_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithOnePartitionName, ConsumerGroup = Constants.ConsumerGroupID)] byte[][] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                log.LogInformation($@"Byte data received. Length: {kafkaEvent.Length}, Content: ""{Encoding.UTF8.GetString(kafkaEvent)}""");
            }
        }
    }

    internal static class MultiItem_RawStringArray_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithTenPartitionsName, ConsumerGroup = Constants.ConsumerGroupID)] string[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                log.LogInformation(kafkaEvent);
            }
        }
    }

    internal static class SingleItem_SinglePartition_RawByteArray_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithOnePartitionName, ConsumerGroup = Constants.ConsumerGroupID)] byte[] kafkaEvent,
            ILogger log)
        {
            log.LogInformation($@"Byte data received. Length: {kafkaEvent.Length}, Content: ""{Encoding.UTF8.GetString(kafkaEvent)}""");
        }
    }

    internal static class SingleItem_RawByteArray_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithTenPartitionsName, ConsumerGroup = Constants.ConsumerGroupID)] byte[] kafkaEvent,
            ILogger log)
        {
            log.LogInformation($@"Byte data received. Length: {kafkaEvent.Length}, Content: ""{Encoding.UTF8.GetString(kafkaEvent)}""");
        }
    }



    internal static class MultiItem_KafkaEventData_String_With_Long_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithLongKeyAndTenPartitionsName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<long, string>[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                log.LogInformation("{key}: {value}", kafkaEvent.Key, kafkaEvent.Value.ToString());
            }
        }
    }

    internal static class SingleItem_KafkaEventData_String_Without_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithTenPartitionsName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<string> kafkaEvent,
            ILogger log)
        {
            log.LogInformation(kafkaEvent.Value.ToString());
        }
    }

    internal static class SingleItem_Single_Partition_Raw_String_Without_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithOnePartitionName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<string> kafkaEvent,
            ILogger log)
        {
            log.LogInformation(kafkaEvent.Value.ToString());
        }
    }

    internal static class MultiItem_SpecificAvro_With_String_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.MyAvroRecordTopicName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<string, MyAvroRecord>[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                var myRecord = kafkaEvent.Value;
                if (myRecord == null)
                {
                    throw new Exception("MyAvro record is null");
                }

                log.LogInformation("{key}:{ticks}:{value}", kafkaEvent.Key, myRecord.Ticks, myRecord.ID);
            }
        }
    }

    internal static class MultiItem_GenericAvro_With_String_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.MyAvroRecordTopicName, ConsumerGroup = Constants.ConsumerGroupID, AvroSchema = MyAvroRecord.SchemaText)] KafkaEventData<string, GenericRecord>[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                var myRecord = kafkaEvent.Value;
                if (myRecord == null)
                {
                    throw new Exception("MyAvro record is null");
                }

                if (!myRecord.TryGetValue("ticks", out var ticksValue))
                {
                    throw new Exception("MyAvro record does not have 'ticks' property");
                }

                if (!myRecord.TryGetValue("id", out var idValue))
                {
                    throw new Exception("MyAvro record does not have 'id' property");
                }

                log.LogInformation("{key}:{ticks}:{value}", kafkaEvent.Key, ticksValue, idValue);
            }
        }
    }

    internal static class MultiItem_Raw_SpecificAvro_Without_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.MyAvroRecordTopicName, ConsumerGroup = Constants.ConsumerGroupID)] MyAvroRecord[] kafkaEvents,
            ILogger log)
        {
            foreach (var myRecord in kafkaEvents)
            {
                if (myRecord == null)
                {
                    throw new Exception("MyAvro record is null");
                }

                log.LogInformation("{ticks}:{value}", myRecord.Ticks, myRecord.ID);
            }
        }
    }

    internal static class MultiItem_Raw_Protobuf_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.MyProtobufTopicName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<string, ProtoUser>[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                var user = kafkaEvent.Value;
                log.LogInformation("{key}:{favoriteColor}:{name}", kafkaEvent.Key, user.FavoriteColor, user.Name);
            }
        }
    }

    internal static class MultiItem_Protobuf_With_String_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.MyProtobufTopicName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<string, ProtoUser>[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                var user = kafkaEvent.Value;
                log.LogInformation("{key}:{favoriteColor}:{name}", kafkaEvent.Key, user.FavoriteColor, user.Name);
            }
        }
    }

    internal static class MultiItem_String_Without_Key_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithTenPartitionsName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<string>[] kafkaEvents,
            ILogger log)
        {
            foreach (var kafkaEvent in kafkaEvents)
            {
                log.LogInformation(kafkaEvent.Value.ToString());
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Avro.Generic;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Bindings;
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
    {
        public static void Trigger(
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

    internal static class SingleItem_Single_Partition_Raw_String_Without_Key_Trigger_Retry
    {
        [FixedDelayRetry(5, "00:00:01")]
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithOnePartitionName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<string> kafkaEvent,
            ILogger log)
        {
            log.LogInformation(kafkaEvent.Value.ToString());
            throw new Exception("unhandled error");
        }
    }

    internal static class SingleEventTrigger_With_Activity
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithOnePartitionName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<string> kafkaEvent,
            ILogger log)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                log.LogInformation("TraceId:" + activity.TraceId);
            }
        }
    }

    internal static class BatchEvenTrigger_With_Activity
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithOnePartitionName, ConsumerGroup = Constants.ConsumerGroupID)] KafkaEventData<string>[] kafkaEvents,
            ILogger log)
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                log.LogInformation("TraceId:" + activity.TraceId);
                var links = activity.Links;
                log.LogInformation("ActivityLinks: {numlink}", links.Count());
                foreach (var link in links)
                {
                    log.LogInformation("LinkedActivity: 00-" + link.Context.TraceId + "-" + link.Context.SpanId + "-01");
                }
            }
        }
    }

    internal static class SingleItem_With_Schema_Registry
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.SchemaRegistryTopicName, ConsumerGroup = Constants.ConsumerGroupID, SchemaRegistryUrl = Constants.SchemaRegistryUrl)] KafkaEventData<string, GenericRecord> kafkaEvent,
            ILogger log)
        {
            log.LogInformation(kafkaEvent.Value.ToString());
        }
    }

    internal static class SingleItem_With_Schema_Registry_No_Key
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.SchemaRegistryNoKeyTopicName, ConsumerGroup = Constants.ConsumerGroupID, SchemaRegistryUrl = Constants.SchemaRegistryUrl)] KafkaEventData<GenericRecord> kafkaEvent,
            ILogger log)
        {
            log.LogInformation(kafkaEvent.Value.ToString());
        }
    }

    // Tests for key avro schema
    internal static class SingleItem_GenericAvroValue_With_GenericAvroKey_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.MyKeyAvroRecordTopicName, ConsumerGroup = Constants.ConsumerGroupID, AvroSchema = MyAvroRecord.SchemaText, KeyAvroSchema = MyKeyAvroRecord.SchemaText)] KafkaEventData<GenericRecord, GenericRecord> kafkaEvent,
            ILogger log)
        {
            var myRecord = kafkaEvent.Value;
            var myKey = kafkaEvent.Key;
            if (myRecord == null)
            {
                throw new Exception("MyAvro record is null");
            }
            log.LogInformation($"Value: {kafkaEvent.Value.ToString()}");
            if (myKey == null)
            {
                throw new Exception("MyAvro key is null");
            }  
            log.BeginScope($"Key: {myKey.ToString()}");
        }
    }

    internal static class SingleItem_GenericAvroValue_With_GenericAvroKey_SchemaRegistryURL
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.MyKeyAvroRecordTopicName, ConsumerGroup = Constants.ConsumerGroupID, SchemaRegistryUrl = Constants.SchemaRegistryUrl)] KafkaEventData<GenericRecord, GenericRecord> kafkaEvent,
            ILogger log)
        {
            var myRecord = kafkaEvent.Value;
            var myKey = kafkaEvent.Key;
            if (myRecord == null)
            {
                throw new Exception("MyAvro record is null");
            }
            log.LogInformation($"Value: {kafkaEvent.Value.ToString()}");
            if (myKey == null)
            {
                throw new Exception("MyAvro key is null");
            }
            log.BeginScope($"Key: {myKey.ToString()}");
        }
    }

    /// <summary>
    /// Trigger function that fails on the first attempt for each message (tracked by a static counter per message prefix).
    /// On the second attempt (redelivery), it succeeds. Used to validate at-least-once delivery.
    /// </summary>
    internal static class SingleItem_AtLeastOnce_FailThenSucceed_Trigger
    {
        private static readonly ConcurrentDictionary<string, int> AttemptCounts = new ConcurrentDictionary<string, int>();

        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.AtLeastOnceTopicName, ConsumerGroup = Constants.AtLeastOnceConsumerGroupID)] KafkaEventData<string> kafkaEvent,
            ILogger log)
        {
            var value = kafkaEvent.Value.ToString();
            var attempt = AttemptCounts.AddOrUpdate(value, 1, (_, c) => c + 1);
            log.LogInformation("AtLeastOnce attempt {attempt} for {value}", attempt, value);

            if (attempt == 1)
            {
                throw new InvalidOperationException($"Simulated failure on first attempt for: {value}");
            }

            // Second attempt: succeed
            log.LogInformation("AtLeastOnce SUCCESS for {value}", value);
        }
    }

    /// <summary>
    /// Trigger function that always fails. Used to validate maxRetries force-commit behavior.
    /// After maxRetries, the extension should skip the message and process the next one.
    /// </summary>
    internal static class SingleItem_AtLeastOnce_AlwaysFail_Trigger
    {
        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.AtLeastOnceMaxRetriesTopicName, ConsumerGroup = Constants.AtLeastOnceMaxRetriesConsumerGroupID)] KafkaEventData<string> kafkaEvent,
            ILogger log)
        {
            var value = kafkaEvent.Value.ToString();
            log.LogInformation("AtLeastOnce_AlwaysFail for {value}", value);
            throw new InvalidOperationException($"Permanent failure for: {value}");
        }
    }

    /// <summary>
    /// Trigger function that binds to ParameterBindingData — simulates the host-side
    /// parameter type seen during .NET isolated deferred binding for KafkaRecord.
    /// Without the ParameterBindingData → byte[] fix in SerializationHelper,
    /// the host fails at startup with "no default deserializer for ParameterBindingData".
    /// </summary>
    internal static class SingleItem_ParameterBindingData_Trigger
    {
        public static ConcurrentBag<ParameterBindingData> Received = new ConcurrentBag<ParameterBindingData>();

        public static void Trigger(
            [KafkaTrigger("LocalBroker", Constants.StringTopicWithTenPartitionsName, ConsumerGroup = Constants.ConsumerGroupID)] ParameterBindingData bindingData,
            ILogger log)
        {
            Received.Add(bindingData);
            log.LogInformation("ParameterBindingData received: source={source}", bindingData.Source);
        }
    }
}

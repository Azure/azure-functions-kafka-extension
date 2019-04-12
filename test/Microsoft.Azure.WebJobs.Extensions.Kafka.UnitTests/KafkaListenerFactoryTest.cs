// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Avro.Generic;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaListenerFactoryTest
    {
        [Fact]
        public void When_No_Type_Is_Set_Should_Create_ByteArray_Listener()
        {
            var attribute = new KafkaTriggerAttribute("brokers:9092", "myTopic")
            {
            };

            var executor = new Mock<ITriggeredFunctionExecutor>();
            var listenerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = attribute.BrokerList,
                Topic = attribute.Topic,
                ConsumerGroup = "group1",
            };

            var listener = KafkaListenerFactory.CreateFor(
                attribute,
                typeof(KafkaEventData),
                executor.Object,
                true,
                new KafkaOptions(),
                listenerConfig,
                NullLogger.Instance);

            Assert.NotNull(listener);
            Assert.IsType<KafkaListener<Ignore, byte[]>>(listener);
            var typedListener = (KafkaListener<Ignore, byte[]>)listener;
            Assert.Null(typedListener.ValueDeserializer);
        }

        [Fact]
        public void When_String_Value_Type_Is_Set_Should_Create_String_Listener()
        {
            var attribute = new KafkaTriggerAttribute("brokers:9092", "myTopic")
            {
                ValueType = typeof(string),
            };

            var executor = new Mock<ITriggeredFunctionExecutor>();
            var listenerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = attribute.BrokerList,
                Topic = attribute.Topic,
                ConsumerGroup = "group1",
            };

            var listener = KafkaListenerFactory.CreateFor(
                attribute,
                typeof(KafkaEventData),
                executor.Object,
                true,
                new KafkaOptions(),
                listenerConfig,
                NullLogger.Instance);

            Assert.NotNull(listener);
            Assert.IsType<KafkaListener<Ignore, string>>(listener);
            var typedListener = (KafkaListener<Ignore, string>)listener;
            Assert.Null(typedListener.ValueDeserializer);
        }

        [Fact]
        public void When_Avro_Schema_Is_Provided_Should_Create_GenericRecord_Listener()
        {
            var attribute = new KafkaTriggerAttribute("brokers:9092", "myTopic")
            {
                AvroSchema = "fakeAvroSchema"
            };

            var executor = new Mock<ITriggeredFunctionExecutor>();
            var listenerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = attribute.BrokerList,
                Topic = attribute.Topic,
                ConsumerGroup = "group1",
            };

            var listener = KafkaListenerFactory.CreateFor(
                attribute,
                typeof(KafkaEventData),
                executor.Object,
                true,
                new KafkaOptions(),
                listenerConfig,
                NullLogger.Instance);

            Assert.NotNull(listener);
            Assert.IsType<KafkaListener<Ignore, GenericRecord>>(listener);
            var typedListener = (KafkaListener<Ignore, GenericRecord>)listener;
            Assert.NotNull(typedListener.ValueDeserializer);
            Assert.IsType<AvroDeserializer<GenericRecord>>(typedListener.ValueDeserializer);
        }


        [Fact]
        public void When_Value_Type_Is_Specific_Record_Should_Create_SpecificRecord_Listener()
        {
            var attribute = new KafkaTriggerAttribute("brokers:9092", "myTopic")
            {
                ValueType = typeof(MyAvroRecord)
            };

            var executor = new Mock<ITriggeredFunctionExecutor>();
            var listenerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = attribute.BrokerList,
                Topic = attribute.Topic,
                ConsumerGroup = "group1",
            };
            var listener = KafkaListenerFactory.CreateFor(
                attribute,
                typeof(KafkaEventData),
                executor.Object,
                true,
                new KafkaOptions(),
                listenerConfig,
                NullLogger.Instance);

            Assert.NotNull(listener);
            Assert.IsType<KafkaListener<Ignore, MyAvroRecord>>(listener);
            var typedListener = (KafkaListener<Ignore, MyAvroRecord>)listener;
            Assert.NotNull(typedListener.ValueDeserializer);
            Assert.IsType<AvroDeserializer<MyAvroRecord>>(typedListener.ValueDeserializer);
        }

        [Fact]
        public void When_Value_Type_Is_Protobuf_Should_Create_Protobuf_Listener()
        {
            var attribute = new KafkaTriggerAttribute("brokers:9092", "myTopic")
            {
                ValueType = typeof(ProtoUser)
            };

            var executor = new Mock<ITriggeredFunctionExecutor>();
            var listenerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = attribute.BrokerList,
                Topic = attribute.Topic,
                ConsumerGroup = "group1",
            };
            var listener = KafkaListenerFactory.CreateFor(
                attribute,
                typeof(KafkaEventData),
                executor.Object,
                true,
                new KafkaOptions(),
                listenerConfig,
                NullLogger.Instance);

            Assert.NotNull(listener);
            Assert.IsType<KafkaListener<Ignore, ProtoUser>>(listener);
            var typedListener = (KafkaListener<Ignore, ProtoUser>)listener;
            Assert.NotNull(typedListener.ValueDeserializer);
            Assert.IsType<ProtobufDeserializer<ProtoUser>>(typedListener.ValueDeserializer);
        }
    }
}
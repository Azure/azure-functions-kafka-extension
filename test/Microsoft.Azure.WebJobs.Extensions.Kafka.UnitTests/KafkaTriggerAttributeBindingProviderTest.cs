﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Avro.Generic;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests.Helpers;
using Microsoft.Azure.WebJobs.Host;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaTriggerAttributeBindingProviderTest : IDisposable
    {
        private List<FileInfo> createdFiles = new List<FileInfo>();
        private IConfigurationRoot emptyConfiguration;
        private IDrainModeManager drainModeManager = new Mock<IDrainModeManager>().Object;

        public KafkaTriggerAttributeBindingProviderTest()
        {
            this.emptyConfiguration = new ConfigurationBuilder()
                .Build();
        }

        public void Dispose()
        {
            foreach (var fi in this.createdFiles)
            {
                if (fi.Exists)
                {
                    fi.Delete();
                }
            }

            this.createdFiles.Clear();
        }

        private FileInfo CreateFile(string fileName)
        {
            File.WriteAllText(fileName, "dummy contents");
            var file = new FileInfo(fileName);
            this.createdFiles.Add(file);

            return file;
        }

        static void RawByteArray_Fn([KafkaTrigger("brokers:9092", "myTopic")] byte[] data) { }
        static void ByteArray_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<Null, byte[]> data) { }
        static void ByteArrayWithoutKey_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<byte[]> data) { }
        static void ByteArrayWithLongKey_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<long, byte[]> data) { }
        static void ByteArrayWithStringKey_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<string, byte[]> data) { }

        static void RawString_Fn([KafkaTrigger("brokers:9092", "myTopic")] string data) { }
        static void String_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<Null, string> data) { }
        static void StringWithoutKey_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<string> data) { }
        static void String_With_LongKey_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<long, string> data) { }
        static void String_With_StringKey_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<string, string> data) { }

        static void RawGenericAvro_Fn([KafkaTrigger("brokers:9092", "myTopic", AvroSchema = "fake")] GenericRecord genericRecord) { }
        static void GenericAvro_Fn([KafkaTrigger("brokers:9092", "myTopic", AvroSchema = "fake")] KafkaEventData<Null, GenericRecord> genericRecord) { }
        static void GenericAvroWithoutKey_Fn([KafkaTrigger("brokers:9092", "myTopic", AvroSchema = "fake")] KafkaEventData<GenericRecord> genericRecord) { }
        static void GenericAvro_WithLongKey_Fn([KafkaTrigger("brokers:9092", "myTopic", AvroSchema = "fake")] KafkaEventData<long, GenericRecord> genericRecord) { }
        static void GenericAvro_WithStringKey_Fn([KafkaTrigger("brokers:9092", "myTopic", AvroSchema = "fake")] KafkaEventData<string, GenericRecord> genericRecord) { }
        static void GenericWithSchemaRegistry_Fn([KafkaTrigger("brokers:9092", "myTopic", SchemaRegistryUrl = "localhost:8081")] KafkaEventData<GenericRecord> genericRecord) { }
        static void GenericKeyValueAvro_Fn([KafkaTrigger("brokers:9092", "myTopic", AvroSchema = "fake", KeyAvroSchema = "fake")] KafkaEventData<GenericRecord, GenericRecord> genericRecord) { }


        static void RawSpecificAvro_Fn([KafkaTrigger("brokers:9092", "myTopic")] MyAvroRecord myAvroRecord) { }
        static void SpecificAvro_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<Null, MyAvroRecord> myAvroRecord) { }
        static void SpecificAvroWithoutKey_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<MyAvroRecord> myAvroRecord) { }
        static void SpecificAvro_WithLongKey_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<long, MyAvroRecord> myAvroRecord) { }
        static void SpecificAvro_WithStringKey_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<string, MyAvroRecord> myAvroRecord) { }
        static void SpecificKeyValueAvro_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<MyAvroRecord, MyAvroRecord> myAvroRecord) { }

        static void RawProtobuf_Fn([KafkaTrigger("brokers:9092", "myTopic")] ProtoUser protoUser) { }
        static void Protobuf_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<Null, ProtoUser> protoUser) { }
        static void ProtobufWithoutKey_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<ProtoUser> protoUser) { }
        static void Protobuf_WithLongKey_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<long, ProtoUser> protoUser) { }
        static void Protobuf_WithStringKey_Fn([KafkaTrigger("brokers:9092", "myTopic")] KafkaEventData<string, ProtoUser> protoUser) { }

        ParameterInfo GetParameterInfo(string methodName)
        {
            var method = this.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            return method.GetParameters().First();
        }

        private void AssertIsCorrectKafkaListener(IListener listener, Type expectedKeyType, Type expectedValueType, Type expectedDeserializerType)
        {
            var method = this.GetType().GetMethod(nameof(AssertIsCorrectKafkaListenerFor), BindingFlags.NonPublic | BindingFlags.Instance);
            var genericMethod = method.MakeGenericMethod(expectedKeyType, expectedValueType);
            genericMethod.Invoke(this, new object[] { listener, expectedDeserializerType });
        }

        private void AssertIsCorrectKafkaListenerFor<TKey, TValue>(IListener listener, Type expectedDeserializerType)
        {
            Assert.IsType<KafkaListener<TKey, TValue>>(listener);
            var typedListener = (KafkaListener<TKey, TValue>)listener;

            if (expectedDeserializerType == null)
            {
                Assert.Null(typedListener.ValueDeserializer);
            }
            else
            {
                Assert.NotNull(typedListener.ValueDeserializer);
                Assert.IsType(expectedDeserializerType, typedListener.ValueDeserializer);
            }
        }

        [Theory]
        [InlineData(nameof(ByteArray_Fn), typeof(Null))]
        [InlineData(nameof(RawByteArray_Fn), typeof(string))]
        [InlineData(nameof(ByteArrayWithoutKey_Fn), typeof(string))]
        public async Task When_No_Type_Is_Set_Should_Create_ByteArray_Listener(string functionName, Type expectedKeyType)
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

            var config = this.emptyConfiguration;

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance,
                drainModeManager);

            var parameterInfo = new TriggerBindingProviderContext(this.GetParameterInfo(functionName), default);

            var triggerBinding = await bindingProvider.TryCreateAsync(parameterInfo);
            var listener = await triggerBinding.CreateListenerAsync(new ListenerFactoryContext(new FunctionDescriptor(), new Mock<ITriggeredFunctionExecutor>().Object, default));

            Assert.NotNull(listener);
            AssertIsCorrectKafkaListener(listener, expectedKeyType, typeof(byte[]), null);
        }

        [Theory]
        [InlineData(nameof(String_Fn), typeof(Null))]
        [InlineData(nameof(RawString_Fn), typeof(string))]
        [InlineData(nameof(StringWithoutKey_Fn), typeof(string))]
        public async Task When_String_Value_Type_Is_Set_Should_Create_String_Listener(string functionName, Type expectedKeyType)
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

            var config = this.emptyConfiguration;

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance,
                drainModeManager);

            var parameterInfo = new TriggerBindingProviderContext(this.GetParameterInfo(functionName), default);

            var triggerBinding = await bindingProvider.TryCreateAsync(parameterInfo);
            var listener = await triggerBinding.CreateListenerAsync(new ListenerFactoryContext(new FunctionDescriptor(), new Mock<ITriggeredFunctionExecutor>().Object, default));


            Assert.NotNull(listener);
            AssertIsCorrectKafkaListener(listener, expectedKeyType, typeof(string), null);
        }

        [Theory]
        [InlineData(nameof(GenericAvro_Fn), typeof(Null))]
        [InlineData(nameof(GenericAvroWithoutKey_Fn), typeof(string))]
        [InlineData(nameof(RawGenericAvro_Fn), typeof(string))]
        [InlineData(nameof(GenericWithSchemaRegistry_Fn), typeof(string))]
        [InlineData(nameof(GenericKeyValueAvro_Fn), typeof(GenericRecord))]
        public async Task When_Avro_Schema_Is_Provided_Should_Create_GenericRecord_Listener(string functionName, Type expectedKeyType)
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

            var config = this.emptyConfiguration;

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance, 
                drainModeManager);

            var parameterInfo = new TriggerBindingProviderContext(this.GetParameterInfo(functionName), default);

            var triggerBinding = await bindingProvider.TryCreateAsync(parameterInfo);
            var listener = await triggerBinding.CreateListenerAsync(new ListenerFactoryContext(new FunctionDescriptor(), new Mock<ITriggeredFunctionExecutor>().Object, default));


            Assert.NotNull(listener);
            AssertIsCorrectKafkaListener(listener, expectedKeyType, typeof(GenericRecord), typeof(SyncOverAsyncDeserializer<GenericRecord>));
        }

        [Theory]
        [InlineData(nameof(SpecificAvro_Fn), typeof(Null))]
        [InlineData(nameof(RawSpecificAvro_Fn), typeof(string))]
        [InlineData(nameof(SpecificAvroWithoutKey_Fn), typeof(string))]
        [InlineData(nameof(SpecificKeyValueAvro_Fn), typeof(MyAvroRecord))]
        public async Task When_Value_Type_Is_Specific_Record_Should_Create_SpecificRecord_Listener(string functionName, Type expectedKeyType)
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

            var config = this.emptyConfiguration;

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance,
                drainModeManager);

            var parameterInfo = new TriggerBindingProviderContext(this.GetParameterInfo(functionName), default);

            var triggerBinding = await bindingProvider.TryCreateAsync(parameterInfo);
            var listener = await triggerBinding.CreateListenerAsync(new ListenerFactoryContext(new FunctionDescriptor(), new Mock<ITriggeredFunctionExecutor>().Object, default));


            Assert.NotNull(listener);
            AssertIsCorrectKafkaListener(listener, expectedKeyType, typeof(MyAvroRecord), typeof(SyncOverAsyncDeserializer<MyAvroRecord>));
        }

        [Theory]
        [InlineData(nameof(Protobuf_Fn), typeof(Null))]
        [InlineData(nameof(RawProtobuf_Fn), typeof(string))]
        [InlineData(nameof(ProtobufWithoutKey_Fn), typeof(string))]
        public async Task When_Value_Type_Is_Protobuf_Should_Create_Protobuf_Listener(string functionName, Type expectedKeyType)
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

            var config = this.emptyConfiguration;

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance,
                drainModeManager);

            var parameterInfo = new TriggerBindingProviderContext(this.GetParameterInfo(functionName), default);

            var triggerBinding = await bindingProvider.TryCreateAsync(parameterInfo);
            var listener = await triggerBinding.CreateListenerAsync(new ListenerFactoryContext(new FunctionDescriptor(), new Mock<ITriggeredFunctionExecutor>().Object, default));


            Assert.NotNull(listener);
            AssertIsCorrectKafkaListener(listener, expectedKeyType, typeof(ProtoUser), typeof(ProtobufDeserializer<ProtoUser>));
        }


        [Theory]
        [InlineData(nameof(String_With_LongKey_Fn), typeof(long), typeof(string))]
        [InlineData(nameof(String_With_StringKey_Fn), typeof(string), typeof(string))]

        [InlineData(nameof(GenericAvro_WithLongKey_Fn), typeof(long), typeof(GenericRecord))]
        [InlineData(nameof(GenericAvro_WithStringKey_Fn), typeof(string), typeof(GenericRecord))]

        [InlineData(nameof(ByteArrayWithLongKey_Fn), typeof(long), typeof(byte[]))]
        [InlineData(nameof(ByteArrayWithStringKey_Fn), typeof(string), typeof(byte[]))]

        [InlineData(nameof(SpecificAvro_WithLongKey_Fn), typeof(long), typeof(MyAvroRecord))]
        [InlineData(nameof(SpecificAvro_WithStringKey_Fn), typeof(string), typeof(MyAvroRecord))]

        [InlineData(nameof(Protobuf_WithLongKey_Fn), typeof(long), typeof(ProtoUser))]
        [InlineData(nameof(Protobuf_WithStringKey_Fn), typeof(string), typeof(ProtoUser))]
        public async Task When_Value_Is_KafkaEventData_With_Key_Should_Create_Listener_With_Key(string functionName, Type keyType, Type valueType)
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

            var config = this.emptyConfiguration;

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance,
                drainModeManager);

            var parameterInfo = new TriggerBindingProviderContext(this.GetParameterInfo(functionName), default);

            var triggerBinding = await bindingProvider.TryCreateAsync(parameterInfo);
            var listener = await triggerBinding.CreateListenerAsync(new ListenerFactoryContext(new FunctionDescriptor(), new Mock<ITriggeredFunctionExecutor>().Object, default));


            Assert.NotNull(listener);
            Assert.True(listener.GetType().IsGenericType);
            var genericTypes = listener.GetType().GetGenericArguments();
            Assert.Equal(keyType, genericTypes[0]);
            Assert.Equal(valueType, genericTypes[1]);
        }

        [Fact]
        public void GetConsumerConfig_When_Ssl_Locations_Resolve_Should_Contain_Full_Path()
        {
            var sslCertificate = this.CreateFile("sslCertificate.pem");
            var sslCa = this.CreateFile("sslCa.pem");
            var sslKeyLocation = this.CreateFile("sslKey.pem");

            var attribute = new KafkaTriggerAttribute("brokers:9092", "myTopic")
            {
                Protocol = BrokerProtocol.Ssl,
                SslKeyPassword = "password1",
                SslCertificateLocation = sslCertificate.FullName,
                SslCaLocation = sslCa.FullName,
                SslKeyLocation = sslKeyLocation.FullName
            };

            var config = this.emptyConfiguration;

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance,
                drainModeManager);

            MethodInfo consumerConfigMethod = typeof(KafkaTriggerAttributeBindingProvider).GetMethod("CreateConsumerConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);

            KafkaListenerConfiguration result = (KafkaListenerConfiguration)consumerConfigMethod.Invoke(bindingProvider, new object[] { attribute });

            Assert.Equal("password1", result.SslKeyPassword);
            Assert.Equal(sslCertificate.FullName, result.SslCertificateLocation);
            Assert.Equal(sslCa.FullName, result.SslCaLocation);
            Assert.Equal(sslKeyLocation.FullName, result.SslKeyLocation);
        }

        [Fact]
        public void GetConsumerConfig_When_Ssl_Locations_Resolve_InAzure_Should_Contain_Full_Path()
        {
            AzureEnvironment.SetRunningInAzureEnvVars();

            var currentFolder = Directory.GetCurrentDirectory();
            var folder1 = Directory.CreateDirectory(Path.Combine(currentFolder, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart1));
            Directory.CreateDirectory(Path.Combine(folder1.FullName, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart2));

            AzureEnvironment.SetEnvironmentVariable(AzureFunctionsFileHelper.AzureHomeEnvVarName, currentFolder);

            var sslCertificate = this.CreateFile(Path.Combine(currentFolder, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart1, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart2, "sslCertificate.pem"));
            var sslCa = this.CreateFile(Path.Combine(currentFolder, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart1, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart2, "sslCa.pem"));
            var sslKeyLocation = this.CreateFile(Path.Combine(currentFolder, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart1, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart2, "sslKey.pem"));

            var attribute = new KafkaTriggerAttribute("brokers:9092", "myTopic")
            {
                Protocol = BrokerProtocol.Ssl,
                SslKeyPassword = "password1",
                SslCertificateLocation = sslCertificate.FullName,
                SslCaLocation = sslCa.FullName,
                SslKeyLocation = sslKeyLocation.FullName
            };

            var config = this.emptyConfiguration;

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance,
                drainModeManager);

            MethodInfo consumerConfigMethod = typeof(KafkaTriggerAttributeBindingProvider).GetMethod("CreateConsumerConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);

            KafkaListenerConfiguration result = (KafkaListenerConfiguration)consumerConfigMethod.Invoke(bindingProvider, new object[] { attribute });

            Assert.Equal("password1", result.SslKeyPassword);
            Assert.Equal(sslCertificate.FullName, result.SslCertificateLocation);
            Assert.Equal(sslCa.FullName, result.SslCaLocation);
            Assert.Equal(sslKeyLocation.FullName, result.SslKeyLocation);
        }

        [Fact]
        public void GetConsumerConfig_When_Ssl_Locations_Resolve_From_AppSetting_InAzure_Should_Contain_Full_Path()
        {
            AzureEnvironment.SetRunningInAzureEnvVars();

            var currentFolder = Directory.GetCurrentDirectory();
            var folder1 = Directory.CreateDirectory(Path.Combine(currentFolder, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart1));
            Directory.CreateDirectory(Path.Combine(folder1.FullName, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart2));

            AzureEnvironment.SetEnvironmentVariable(AzureFunctionsFileHelper.AzureHomeEnvVarName, currentFolder);

            var sslCertificate = this.CreateFile(Path.Combine(currentFolder, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart1, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart2, "sslCertificate.pem"));
            var sslCa = this.CreateFile(Path.Combine(currentFolder, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart1, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart2, "sslCa.pem"));
            var sslKeyLocation = this.CreateFile(Path.Combine(currentFolder, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart1, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart2, "sslKey.pem"));

            var attribute = new KafkaTriggerAttribute("brokers:9092", "myTopic")
            {
                Protocol = BrokerProtocol.Ssl,
                SslKeyPassword = "password1",
                SslCertificateLocation = "%SslCertificateLocation%",
                SslCaLocation = "%SslCaLocation%",
                SslKeyLocation = "%SslKeyLocation%"
            };

            var configSslLocations = new Dictionary<string, string>
            {
                {"SslCertificateLocation", "sslCertificate.pem"},
                {"SslCaLocation", "sslCa.pem"},
                {"SslKeyLocation", "sslKey.pem"}
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(configSslLocations).Build();

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance,
                drainModeManager);

            MethodInfo consumerConfigMethod = typeof(KafkaTriggerAttributeBindingProvider).GetMethod("CreateConsumerConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);

            KafkaListenerConfiguration result = (KafkaListenerConfiguration)consumerConfigMethod.Invoke(bindingProvider, new object[] { attribute });

            Assert.Equal("password1", result.SslKeyPassword);
            Assert.Equal(sslCertificate.FullName, result.SslCertificateLocation);
            Assert.Equal(sslCa.FullName, result.SslCaLocation);
            Assert.Equal(sslKeyLocation.FullName, result.SslKeyLocation);
        }


        [Fact]
        public void GetConsumerConfig_When_Protocol_is_Not_SSL()
        {
            var attribute = new KafkaTriggerAttribute("brokers:9092", "myTopic")
            {
                Protocol = BrokerProtocol.SaslSsl,
                Username = "myusername",
                Password = "mypassword",
            };

            var config = this.emptyConfiguration;

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance,
                drainModeManager);

            MethodInfo consumerConfigMethod = typeof(KafkaTriggerAttributeBindingProvider).GetMethod("CreateConsumerConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);

            KafkaListenerConfiguration result = (KafkaListenerConfiguration)consumerConfigMethod.Invoke(bindingProvider, new object[] { attribute });
            Assert.Equal(result.SaslUsername, "myusername");
            Assert.Equal(result.SaslPassword, "mypassword");
            Assert.Equal(result.SslKeyLocation, null);
            Assert.Equal(result.SslCaLocation, null);
            Assert.Equal(result.SslCertificateLocation, null);
        }

        [Fact]
        public void GetConsumerConfig_When_Protocol_is_SSL_With_Certificate()
        {
            var dummySslCaCert = "-----BEGIN CERTIFICATE-----\n" +
                "dummycacert\n" +
                "-----END CERTIFICATE-----";
            var dummySslCert = "-----BEGIN CERTIFICATE-----\n" +
                "dummyclientcert\n" +
                "-----END CERTIFICATE-----";
            var dummySslKey = "-----BEGIN PRIVATE KEY-----\n" +
                "dummyclientkey\n" +
                "-----END PRIVATE KEY-----";

            var attribute = new KafkaTriggerAttribute("brokers:9092", "myTopic")
            {
                Protocol = BrokerProtocol.Ssl,
                SslCaPEM = dummySslCaCert,
                SslCertificateandKeyPEM = dummySslCert + "\n" + dummySslKey,
                SslKeyPEM = dummySslKey,
            };

            var config = this.emptyConfiguration;

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance,
                drainModeManager);

            MethodInfo consumerConfigMethod = typeof(KafkaTriggerAttributeBindingProvider).GetMethod("CreateConsumerConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);

            KafkaListenerConfiguration result = (KafkaListenerConfiguration)consumerConfigMethod.Invoke(bindingProvider, new object[] { attribute });
            Assert.Equal(result.SslCaPEM, dummySslCaCert);
            Assert.Equal(result.SslCertificatePEM, dummySslCert);
            Assert.Equal(result.SslKeyPEM, dummySslKey);
        }

        [Fact]
        public void GetConsumerConfig_When_OAuthBearer_Auth_Defined_Should_Contain_Them()
        {
            var attribute = new KafkaTriggerAttribute("brokers:9092", "myTopic")
            {
                AuthenticationMode = BrokerAuthenticationMode.OAuthBearer,
                Protocol = BrokerProtocol.SaslSsl,
                OAuthBearerClientId = "clientId",
                OAuthBearerClientSecret = "secret",
                OAuthBearerMethod = Config.OAuthBearerMethod.Oidc,
                OAuthBearerScope = "scope",
                OAuthBearerExtensions = "key=value",
                OAuthBearerTokenEndpointUrl = "endpointUrl",
            };

            var config = this.emptyConfiguration;

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance,
                drainModeManager);

            MethodInfo consumerConfigMethod = typeof(KafkaTriggerAttributeBindingProvider).GetMethod("CreateConsumerConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);

            KafkaListenerConfiguration result = (KafkaListenerConfiguration)consumerConfigMethod.Invoke(bindingProvider, new object[] { attribute });
            Assert.Equal("brokers:9092", result.BrokerList);
            Assert.Equal(SecurityProtocol.SaslSsl, result.SecurityProtocol);
            Assert.Equal(SaslMechanism.OAuthBearer, result.SaslMechanism);
            Assert.Equal("secret", result.SaslOAuthBearerClientSecret);
            Assert.Equal("clientId", result.SaslOAuthBearerClientId);
            Assert.Equal(SaslOauthbearerMethod.Oidc, result.SaslOAuthBearerMethod);
            Assert.Equal("scope", result.SaslOAuthBearerScope);
            Assert.Equal("key=value", result.SaslOAuthBearerExtensions);
            Assert.Equal("endpointUrl", result.SaslOAuthBearerTokenEndpointUrl);
        }

        [Fact]
        public void GetConsumerConfig_When_OauthBearer_Settings_Resolve_From_AppSetting_InAzure()
        {
            AzureEnvironment.SetRunningInAzureEnvVars();

            var attribute = new KafkaTriggerAttribute("brokers:9092", "myTopic")
            {
                AuthenticationMode = BrokerAuthenticationMode.OAuthBearer,
                Protocol = BrokerProtocol.SaslSsl,
                OAuthBearerClientId = "OAuthBearerClientId",
                OAuthBearerClientSecret = "OAuthBearerClientSecret",
                OAuthBearerMethod = Config.OAuthBearerMethod.Oidc,
                OAuthBearerScope = "OAuthBearerScope",
                OAuthBearerExtensions = "OAuthBearerExtensions",
                OAuthBearerTokenEndpointUrl = "OAuthBearerTokenEndpointUrl",
            };

            var configSslLocations = new Dictionary<string, string>
            {
                {"OAuthBearerClientId", "clientId"},
                {"OAuthBearerClientSecret", "secret"},
                {"OAuthBearerScope", "scope"},
                {"OAuthBearerExtensions", "key=value"},
                {"OAuthBearerTokenEndpointUrl", "endpointUrl"},
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(configSslLocations).Build();

            var bindingProvider = new KafkaTriggerAttributeBindingProvider(
                config,
                Options.Create(new KafkaOptions()),
                new KafkaEventDataConvertManager(NullLogger.Instance),
                new DefaultNameResolver(config),
                NullLoggerFactory.Instance,
                drainModeManager);

            MethodInfo consumerConfigMethod = typeof(KafkaTriggerAttributeBindingProvider).GetMethod("CreateConsumerConfiguration", BindingFlags.NonPublic | BindingFlags.Instance);

            KafkaListenerConfiguration result = (KafkaListenerConfiguration)consumerConfigMethod.Invoke(bindingProvider, new object[] { attribute });

            Assert.Equal("brokers:9092", result.BrokerList);
            Assert.Equal(SecurityProtocol.SaslSsl, result.SecurityProtocol);
            Assert.Equal(SaslMechanism.OAuthBearer, result.SaslMechanism);
            Assert.Equal("secret", result.SaslOAuthBearerClientSecret);
            Assert.Equal("clientId", result.SaslOAuthBearerClientId);
            Assert.Equal(SaslOauthbearerMethod.Oidc, result.SaslOAuthBearerMethod);
            Assert.Equal("scope", result.SaslOAuthBearerScope);
            Assert.Equal("key=value", result.SaslOAuthBearerExtensions);
            Assert.Equal("endpointUrl", result.SaslOAuthBearerTokenEndpointUrl);
        }
    }
}
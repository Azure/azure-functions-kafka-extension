// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Avro.Generic;
using Confluent.Kafka;
using Confluent.SchemaRegistry.Serdes;

using Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaProducerFactoryTest : IDisposable
    {
        private List<FileInfo> createdFiles = new List<FileInfo>();
        private readonly IConfigurationRoot emptyConfiguration;

        public KafkaProducerFactoryTest()
        {
            this.emptyConfiguration = new ConfigurationBuilder().Build();
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

        [Fact]
        public void When_No_Type_Is_Set_Should_Create_ByteArray_Producer()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var producer = factory.Create(entity);

            Assert.NotNull(producer);
            Assert.IsType<KafkaProducer<Null, byte[]>>(producer);
            var typedProducer = (KafkaProducer<Null, byte[]>)producer;
            Assert.Null(typedProducer.ValueSerializer);
        }

        [Fact]
        public void When_String_Value_Type_Is_Set_Should_Create_String_Listener()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(string),
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var producer = factory.Create(entity);

            Assert.NotNull(producer);
            Assert.IsType<KafkaProducer<Null, string>>(producer);
            var typedProducer = (KafkaProducer<Null, string>)producer;
            Assert.Null(typedProducer.ValueSerializer);
        }

        [Fact]
        public void When_Avro_Schema_Is_Provided_Should_Create_GenericRecord_Listener()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
                AvroSchema = "fakeAvroSchema"
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(GenericRecord),
                AvroSchema = attribute.AvroSchema,
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var producer = factory.Create(entity);

            Assert.NotNull(producer);
            Assert.IsType<KafkaProducer<Null, GenericRecord>>(producer);
            var typedProducer = (KafkaProducer<Null, GenericRecord>)producer;
            Assert.NotNull(typedProducer.ValueSerializer);
            Assert.IsType<AvroSerializer<GenericRecord>>(typedProducer.ValueSerializer);
        }


        [Fact]
        public void When_Value_Type_Is_Specific_Record_Should_Create_SpecificRecord_Listener()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
            };


            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(MyAvroRecord),
                AvroSchema = MyAvroRecord.SchemaText,
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var producer = factory.Create(entity);

            Assert.NotNull(producer);
            Assert.IsType<KafkaProducer<Null, MyAvroRecord>>(producer);
            var typedProducer = (KafkaProducer<Null, MyAvroRecord>)producer;
            Assert.NotNull(typedProducer.ValueSerializer);
            Assert.IsType<AvroSerializer<MyAvroRecord>>(typedProducer.ValueSerializer);
        }

        [Fact]
        public void When_Value_Type_Is_Protobuf_Should_Create_Protobuf_Listener()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(ProtoUser),
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var producer = factory.Create(entity);

            Assert.NotNull(producer);
            Assert.IsType<KafkaProducer<Null, ProtoUser>>(producer);
            var typedProducer = (KafkaProducer<Null, ProtoUser>)producer;
            Assert.NotNull(typedProducer.ValueSerializer);
            Assert.IsType<ProtobufSerializer<ProtoUser>>(typedProducer.ValueSerializer);
        }

        [Fact]
        public void GetProducerConfig_When_No_Auth_Defined_Should_Contain_Only_BrokerList()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(ProtoUser),
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var config = factory.GetProducerConfig(entity);
            Assert.Single(config);
            Assert.Equal("brokers:9092", config.BootstrapServers);
        }

        [Fact]
        public void GetProducerConfig_When_Auth_Defined_Should_Contain_Them()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
                AuthenticationMode = BrokerAuthenticationMode.Plain,
                Protocol = BrokerProtocol.SaslSsl,
                Username = "myuser",
                Password = "secret",
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(ProtoUser),
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var config = factory.GetProducerConfig(entity);
            Assert.Equal(5, config.Count());
            Assert.Equal("brokers:9092", config.BootstrapServers);
            Assert.Equal(SecurityProtocol.SaslSsl, config.SecurityProtocol);
            Assert.Equal(SaslMechanism.Plain, config.SaslMechanism);
            Assert.Equal("secret", config.SaslPassword);
            Assert.Equal("myuser", config.SaslUsername);
        }

        [Fact]
        public void GetProducerConfig_When_Ssl_Auth_Defined_Should_Contain_Them()
        {
            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
                Protocol = BrokerProtocol.Ssl,
                SslKeyLocation = "path/to/key",
                SslKeyPassword = "password1",
                SslCertificateLocation = "path/to/cert",
                SslCaLocation = "path/to/cacert"
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(ProtoUser),
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var config = factory.GetProducerConfig(entity);
            Assert.Equal(6, config.Count());
            Assert.Equal("brokers:9092", config.BootstrapServers);
            Assert.Equal(SecurityProtocol.Ssl, config.SecurityProtocol);
            Assert.Equal("path/to/key", config.SslKeyLocation);
            Assert.Equal("password1", config.SslKeyPassword);
            Assert.Equal("path/to/cert", config.SslCertificateLocation);
            Assert.Equal("path/to/cacert", config.SslCaLocation);
        }

        [Fact]
        public void GetProducerConfig_When_Ssl_Locations_Resolve_Should_Contain_Full_Path()
        {
            var sslCertificate = this.CreateFile("sslCertificate.pfx");
            var sslCa = this.CreateFile("sslCa.pem");
            var sslKeyLocation = this.CreateFile("sslKey.key");

            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
                SslCertificateLocation = sslCertificate.FullName,
                SslCaLocation = sslCa.FullName,
                SslKeyLocation = sslKeyLocation.FullName
            };

            var entity = new KafkaProducerEntity
            {
                Attribute = attribute,
                ValueType = typeof(ProtoUser)
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var config = factory.GetProducerConfig(entity);
            Assert.Equal(sslCertificate.FullName, config.SslCertificateLocation);
            Assert.Equal(sslCa.FullName, config.SslCaLocation);
            Assert.Equal(sslKeyLocation.FullName, config.SslKeyLocation);
        }

        [Fact]
        public void GetProducerConfig_When_Ssl_Locations_Resolve_InAzure_Should_Contain_Full_Path()
        {
            AzureEnvironment.SetRunningInAzureEnvVars();

            var currentFolder = Directory.GetCurrentDirectory();
            var folder1 = Directory.CreateDirectory(Path.Combine(currentFolder, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart1));
            Directory.CreateDirectory(Path.Combine(folder1.FullName, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart2));

            AzureEnvironment.SetEnvironmentVariable(AzureFunctionsFileHelper.AzureHomeEnvVarName, currentFolder);

            var sslCertificate = this.CreateFile(Path.Combine(currentFolder, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart1, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart2, "sslCertificate.pfx"));
            var sslCa = this.CreateFile(Path.Combine(currentFolder, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart1, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart2, "sslCa.pem")); 
            var sslKeyLocation = this.CreateFile(Path.Combine(currentFolder, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart1, AzureFunctionsFileHelper.AzureDefaultFunctionPathPart2, "sslKey.key"));

            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
                SslCertificateLocation = "sslCertificate.pfx",
                SslCaLocation = "sslCa.pem",
                SslKeyLocation = "sslKey.key"
            };

            var entity = new KafkaProducerEntity
            {
                Attribute = attribute,
                ValueType = typeof(ProtoUser)
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var config = factory.GetProducerConfig(entity);
            Assert.Equal(sslCertificate.FullName, config.SslCertificateLocation);
            Assert.Equal(sslCa.FullName, config.SslCaLocation);
            Assert.Equal(sslKeyLocation.FullName, config.SslKeyLocation);
        }

        [Fact]
        public void GetProducerConfig_When_Ssl_Auth_Defined_With_String_Pem_Cert_Should_Contain_Them()
        {
            var sslCertPem = "MIIF1TCCA72gAwIBAgIUYkuAAje7tDafB3C1k6Ee+2aTVGUwDQYJKoZIhvcNAQELBQAwejELMAkGA1UEBhMCQVUxDDAKBgNVBAgMA05TVzEPMA0GA1UEBwwGU1lETkVZMRAwDgYDVQQKDAdjb21wYW55MQ0wCwYDVQQLDAR0ZXN0MQ0wCwYDVQQDDAR0ZXN0MRwwGgYJKoZIhvcNAQkBFg10ZXN0QHRlc3QuY29tMB4XDTIyMDQwNjIwNDczNFoXDTIzMDQwNjIwNDczNFowejELMAkGA1UEBhMCQVUxDDAKBgNVBAgMA05TVzEPMA0GA1UEBwwGU1lETkVZMRAwDgYDVQQKDAdjb21wYW55MQ0wCwYDVQQLDAR0ZXN0MQ0wCwYDVQQDDAR0ZXN0MRwwGgYJKoZIhvcNAQkBFg10ZXN0QHRlc3QuY29tMIICIjANBgkqhkiG9w0BAQEFAAOCAg8AMIICCgKCAgEArpsYcUxcG0BHn0gQMAHSJYHRScYEczvLLi9t5tPuA7gpAF3oHED0N5Hx/IUa1n5DKSzXvkurpplbooncGBwhHU2hV//muPa+ENzGBsYtyFcEmkAtbwxjVoC9boccIeGhvOawtXfTL2u2se6jrr+eMkDUe/ePKJvt3Ez+86EjZhIBnLOmrpGkzqQPEWnXTaU5Dc0kJ8S/rLYH44eFIgBF8oFUoRPljsiFCWoNgTnfz5T6KPk1CzKpmTEpP1wBWpNfYYd/dyqABVBRso0Vlrjb0u0+1vTf4mS6dKSykkmfj48D8n/Mob6JfJMPY7aggDxY72U2m1gKz02djoDlyctrZC/ri7fNSDPJ/W+rK0ukmmUzKZI1yNMS9iEee16mc/ZpZR5jEDYKBKCWvusdyMcBhHhQiXqw6AxMUrwj6ySIT72NWCOhWhBoq2HkIvm+uh42kkXgRY+mbuj+6OZeANg52wwuoDgcJIhWYOlnD6NiSKDY+DyBlo8fnO+DzAdqekpKOpYiu9I6RJ/8YXlduj3ePwquULtfjFMUgDB9ocQk9yFureP09yhdn6S+HBXspHtI5zThCpxufSadj9T18wfuwbXArnyglJxTYOwMFSrdf3nc7vsK8QYGNCngSSNgYvAN4olMg7tXu4BHCT5SOxeWSmoITyHHTokD4wPL6LRF7fcCAwEAAaNTMFEwHQYDVR0OBBYEFFBJkQfFAZD4fOgmT92H3umlXumKMB8GA1UdIwQYMBaAFFBJkQfFAZD4fOgmT92H3umlXumKMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQELBQADggIBAD3KUNgEYBTtwg3YISnRXOTdKhGaGfHugDbgK+bJjwdebM2TQujSAvWCmVjjrH3j84ywQJr6qoyWCDpUU7CfSqSAJhwv8nF36hcfed7maSEwh6i2LydcezhOTDTvUfgK9NUQiCFu52HTy8fIPfTum2aPxFbpaEZ2mN9fLtaN205jyRKZMB/Ja9hYnMfR8T+sMOginu3jpKfu2ZkuLKTZP6lGVFpktk1cgzZAeljA9AZxXjU6V3ZjRagccglXSGvtMUpQImiEs/+NUQdMNoZfUkv/8MK1Mede1vGCN3YuFRgpEXVKqcdCzy4jR8h8/bpaA9ijDP69pxPq6WDbvSU9pzxpRe17/4scTEyDA80fhWrcrEuSKNGBJAEpk0r6Xycmj2LmuRvPECABu4vBflVkJZ93cTCoa0I4Ae5cCli0963o2g6+7de8zMR4KLGCh9ApbJYQ10sTLl2uPkKCgNyW7F0K26Bb+e/gXhoFht2lSAuGWQy0kaw3C6bL2mOx6P9pO5kG9sVV9M2rHEaA/vcv/iQtQlqISTrL+B72Y2pClr3r6NYvu3HOJKruyCR+ofmUZzfXN1WdDeqOrKF3YKM/e5FnP8N42OyupRXjLlAQC6KCpAtxI4TD2Fv1PIruZaVIDQQlWdQ13fSu/fcaw90jOGHdRJjuWGtwpHxTcd0vxbxQ";
            var sslKeyPem = "MIIJpDBOBgkqhkiG9w0BBQ0wQTApBgkqhkiG9w0BBQwwHAQIoClo9kq6eP4CAggAMAwGCCqGSIb3DQIJBQAwFAYIKoZIhvcNAwcECPhsfpO6Zt7oBIIJULNXpDnJaCSLmBIy9PLc1h/qQtfRisBn0/bNBLV85Hv4pt7we+EgjG7Z3Ix0IzajftmGU1wwlhwwY75nTB+EzobcnMLnhj1F8PhDwQIc4aBJ4bs5mub7Iskfbsvz6Rx1w13uamTXoZ5h4rNIGNFiyaTqQ3BCmCirQxKeX/EP+xAyb6hagKgZd5/vUxUtrCeIBcnzRaJmGD9V";

            var attribute = new KafkaAttribute("brokers:9092", "myTopic")
            {
                Protocol = BrokerProtocol.Ssl,                
                SslCertificatePem = sslCertPem,
                SslKeyPem = sslKeyPem,
                SslCaLocation = "path/to/cacert"
            };

            var entity = new KafkaProducerEntity()
            {
                Attribute = attribute,
                ValueType = typeof(ProtoUser),
            };

            var factory = new KafkaProducerFactory(emptyConfiguration, new DefaultNameResolver(emptyConfiguration), NullLoggerProvider.Instance);
            var config = factory.GetProducerConfig(entity);
            Assert.Equal(5, config.Count());
            Assert.Equal("brokers:9092", config.BootstrapServers);
            Assert.Equal(SecurityProtocol.Ssl, config.SecurityProtocol);
            Assert.Equal(sslKeyPem, config.SslKeyPem);            
            Assert.Equal(sslCertPem, config.SslCertificatePem);
            Assert.Equal("path/to/cacert", config.SslCaLocation);
        }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class KafkaAttributeTests
    {
        [Fact]
        public void When_Nothing_Is_Set_It_Returns_Default_Values()
        {
            var attribute = typeof(TestClass)
                .GetMethod(nameof(TestClass.FunctionWithDefaults))
                .GetParameters()[0]
                .GetCustomAttribute<KafkaAttribute>();

            Assert.NotNull(attribute);

            Assert.Equal(10_000, attribute.BatchSize);
            Assert.Equal(false, attribute.EnableIdempotence);
            Assert.Equal(1_000_000, attribute.MaxMessageBytes);
            Assert.Equal(int.MaxValue, attribute.MaxRetries);
            Assert.Equal(300_000, attribute.MessageTimeoutMs);
            Assert.Equal(5_000, attribute.RequestTimeoutMs);
        }

        [Fact]
        public void When_Everything_Is_Set_It_Returns_Set_Values()
        {
            var attribute = typeof(TestClass)
                .GetMethod(nameof(TestClass.FunctionWithEverythingSet))
                .GetParameters()[0]
                .GetCustomAttribute<KafkaAttribute>();

            Assert.NotNull(attribute);

            Assert.Equal(BrokerAuthenticationMode.ScramSha512, attribute.AuthenticationMode);
            Assert.Equal("AvroSchema", attribute.AvroSchema);
            Assert.Equal("BrokerList", attribute.BrokerList);
            Assert.Equal(1, attribute.BatchSize);
            Assert.Equal(true, attribute.EnableIdempotence);
            Assert.Equal(2, attribute.MaxMessageBytes);
            Assert.Equal(3, attribute.MaxRetries);
            Assert.Equal(4, attribute.MessageTimeoutMs);
            Assert.Equal("Password", attribute.Password);
            Assert.Equal(BrokerProtocol.SaslPlaintext, attribute.Protocol);
            Assert.Equal(5, attribute.RequestTimeoutMs);
            Assert.Equal("SslCaLocation", attribute.SslCaLocation);
            Assert.Equal("SslCertificateLocation", attribute.SslCertificateLocation);
            Assert.Equal("SslKeyLocation", attribute.SslKeyLocation);
            Assert.Equal("SslKeyPassword", attribute.SslKeyPassword);
            Assert.Equal("Username", attribute.Username);
        }

        private class TestClass
        {
            
            public void FunctionWithDefaults(
                [Kafka("brokerList", "topic")]
                string parameterWithDefaults)
            { 
            }

            public void FunctionWithEverythingSet(
                [Kafka(
                AuthenticationMode = BrokerAuthenticationMode.ScramSha512,
                AvroSchema = "AvroSchema",
                BrokerList = "BrokerList",
                BatchSize = 1,
                EnableIdempotence = true,
                MaxMessageBytes = 2,
                MaxRetries = 3,
                MessageTimeoutMs = 4,
                Password = "Password",
                Protocol = BrokerProtocol.SaslPlaintext,
                RequestTimeoutMs = 5,
                SslCaLocation = "SslCaLocation",
                SslCertificateLocation = "SslCertificateLocation",
                SslKeyLocation = "SslKeyLocation",
                SslKeyPassword = "SslKeyPassword",
                Username = "Username"
                    )]
                string parameterWithDefaults)
            {
            }
        }
    }
}

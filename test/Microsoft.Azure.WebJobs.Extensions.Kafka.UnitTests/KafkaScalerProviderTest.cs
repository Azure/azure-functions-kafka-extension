// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{

    public class KafkaScalerProviderTest
    {
        private readonly Mock<IServiceProvider> serviceProvider;
        private readonly Mock<INameResolver> nameResolver;
        private Mock<IConfiguration> config;

        public KafkaScalerProviderTest()
        {
            config = new Mock<IConfiguration>(MockBehavior.Loose);
            config.Setup(p => p.GetSection(It.IsAny<string>())).Returns<IConfigurationSection>(null);
            nameResolver = new Mock<INameResolver>(MockBehavior.Strict);
            serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
            serviceProvider.Setup(p => p.GetService(typeof(INameResolver))).Returns(nameResolver.Object);
            serviceProvider.Setup(p => p.GetService(typeof(IOptions<KafkaOptions>))).Returns(null);
            serviceProvider.Setup(p => p.GetService(typeof(IConfiguration))).Returns(config.Object);
            serviceProvider.Setup(p => p.GetService(typeof(ILoggerFactory))).Returns(new NullLoggerFactory());
        }

        [Fact]
        public void kafkaScalerProvider_With_SaslSSl_Metadata()
        {
            var metadata = new JObject
            {
                { "BrokerList", "brokerList" },
                { "Topic", "topicTest" },
                { "ConsumerGroup", "consumerGroup" },
                { "LagThreshold", 1000 },
                { "AuthenticationMode", "Plain" },
                { "Protocol", "SaslSsl" },
                { "Username", "username" },
                { "Password", "password" },
            };
            var triggerMetadata = new TriggerMetadata(metadata);
            var kafkaScalerProvider = new KafkaScalerProvider(serviceProvider.Object, triggerMetadata);
            Assert.NotNull(kafkaScalerProvider);
            Assert.NotNull(kafkaScalerProvider.GetTargetScaler());
            Assert.NotNull(kafkaScalerProvider.GetMonitor());
        }

        [Fact]
        public void kafkaScaletProvider_With_SSL_Keyvault_Metadata() 
        { 
            var metadata = new JObject
            {
                { "BrokerList", "brokerList" },
                { "Topic", "topicTest" },
                { "ConsumerGroup", "consumerGroup" },
                { "LagThreshold", 1000 },
                { "Protocol", "Ssl" },
                { "SslCaPEM", "dummycapem" },
                { "SslCertificateandKeyPEM", "dummycertificateandkeypem" }
            };
            var triggerMetadata = new TriggerMetadata(metadata);
            var kafkaScalerProvider = new KafkaScalerProvider(serviceProvider.Object, triggerMetadata);
            Assert.NotNull(kafkaScalerProvider);
            Assert.NotNull(kafkaScalerProvider.GetTargetScaler());
            Assert.NotNull(kafkaScalerProvider.GetMonitor());
        }

        [Fact]
        public void KafkaScalerProvider_Implements_IDisposable()
        {
            // Arrange
            var metadata = new JObject
            {
                { "BrokerList", "brokerList" },
                { "Topic", "topicTest" },
                { "ConsumerGroup", "consumerGroup" },
                { "LagThreshold", 1000 },
            };
            var triggerMetadata = new TriggerMetadata(metadata);

            // Act & Assert
            var kafkaScalerProvider = new KafkaScalerProvider(serviceProvider.Object, triggerMetadata);
            Assert.True(kafkaScalerProvider is IDisposable, "KafkaScalerProvider should implement IDisposable");
            
            // Dispose should not throw
            kafkaScalerProvider.Dispose();
            
            // Multiple Dispose calls should be safe
            kafkaScalerProvider.Dispose();
        }
    }
}

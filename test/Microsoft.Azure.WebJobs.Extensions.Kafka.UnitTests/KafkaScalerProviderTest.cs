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
        public void GetConsumerConfiguration_When_Both_HttpsCaSettings_Are_Defined_Should_Throw()
        {
            var metadata = new KafkaScalerProvider.KafkaMetaData
            {
                BrokerList = "brokerList",
                ConsumerGroup = "consumerGroup",
                AuthenticationMode = BrokerAuthenticationMode.OAuthBearer,
                HttpsCaLocation = "httpsCaLocation",
                HttpsCaPem = "httpsCaPem",
            };

            var exception = Assert.Throws<ArgumentException>(
                () => KafkaScalerProvider.GetConsumerConfiguration(metadata, config.Object, nameResolver.Object));

            Assert.Contains("https.ca.location", exception.Message);
            Assert.Contains("https.ca.pem", exception.Message);
        }

        [Fact]
        public void GetConsumerConfiguration_When_HttpsCaLocation_Does_Not_Exist_Should_Throw()
        {
            var metadata = new KafkaScalerProvider.KafkaMetaData
            {
                BrokerList = "brokerList",
                ConsumerGroup = "consumerGroup",
                AuthenticationMode = BrokerAuthenticationMode.OAuthBearer,
                HttpsCaLocation = "relative/does-not-exist.pem",
            };

            var exception = Assert.Throws<ArgumentException>(
                () => KafkaScalerProvider.GetConsumerConfiguration(metadata, config.Object, nameResolver.Object));

            Assert.Contains("https.ca.location", exception.Message);
            Assert.Contains("relative/does-not-exist.pem", exception.Message);
        }

        [Fact]
        public void GetConsumerConfiguration_When_OAuthBearer_HttpsCaSettings_Resolve_From_AppSetting()
        {
            var metadata = new KafkaScalerProvider.KafkaMetaData
            {
                BrokerList = "brokerList",
                ConsumerGroup = "consumerGroup",
                AuthenticationMode = BrokerAuthenticationMode.OAuthBearer,
                Protocol = BrokerProtocol.SaslSsl,
                OAuthBearerClientId = "OAuthBearerClientId",
                OAuthBearerClientSecret = "OAuthBearerClientSecret",
                OAuthBearerMethod = SaslOauthbearerMethod.Oidc,
                OAuthBearerScope = "OAuthBearerScope",
                OAuthBearerExtensions = "OAuthBearerExtensions",
                OAuthBearerTokenEndpointUrl = "OAuthBearerTokenEndpointUrl",
                HttpsCaLocation = "HttpsCaLocation",
            };

            var settings = new Dictionary<string, string>
            {
                {"brokerList", "broker:9092"},
                {"consumerGroup", "group"},
                {"OAuthBearerClientId", "clientId"},
                {"OAuthBearerClientSecret", "secret"},
                {"OAuthBearerScope", "scope"},
                {"OAuthBearerExtensions", "key=value"},
                {"OAuthBearerTokenEndpointUrl", "endpointUrl"},
                {"HttpsCaLocation", "probe"},
            };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            var result = KafkaScalerProvider.GetConsumerConfiguration(metadata, configuration, new DefaultNameResolver(configuration));

            Assert.Equal("broker:9092", result.BootstrapServers);
            Assert.Equal("group", result.GroupId);
            Assert.Equal(SecurityProtocol.SaslSsl, result.SecurityProtocol);
            Assert.Equal(SaslMechanism.OAuthBearer, result.SaslMechanism);
            Assert.Equal("secret", result.SaslOauthbearerClientSecret);
            Assert.Equal("clientId", result.SaslOauthbearerClientId);
            Assert.Equal(SaslOauthbearerMethod.Oidc, result.SaslOauthbearerMethod);
            Assert.Equal("scope", result.SaslOauthbearerScope);
            Assert.Equal("key=value", result.SaslOauthbearerExtensions);
            Assert.Equal("endpointUrl", result.SaslOauthbearerTokenEndpointUrl);
            Assert.Equal("probe", result.Get("https.ca.location"));
            Assert.Null(result.Get("https.ca.pem"));
        }
    }
}

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
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

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
            using var kafkaScalerProvider = new KafkaScalerProvider(serviceProvider.Object, triggerMetadata);
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
            using var kafkaScalerProvider = new KafkaScalerProvider(serviceProvider.Object, triggerMetadata);
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
            using var kafkaScalerProvider = new KafkaScalerProvider(serviceProvider.Object, triggerMetadata);
            Assert.True(kafkaScalerProvider is IDisposable, "KafkaScalerProvider should implement IDisposable");

            // Dispose should not throw
            kafkaScalerProvider.Dispose();

            // Multiple Dispose calls should be safe
            kafkaScalerProvider.Dispose();
        }

        [Fact]
        public void KafkaScalerProvider_DisposesConsumer_WhenConstructionFails()
        {
            var consumer = new Mock<IConsumer<string, string>>();
            var triggerMetadata = CreateTriggerMetadata(string.Empty);

            Assert.Throws<ArgumentException>(() =>
                new KafkaScalerProvider(serviceProvider.Object, triggerMetadata, _ => consumer.Object));

            consumer.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void AddKafkaScaleForTrigger_RegistersDistinctProvidersForEachTrigger_AndDisposesThem()
        {
            var services = new TestServiceCollection();
            services.AddSingleton(config.Object);
            services.AddSingleton(nameResolver.Object);
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton(Options.Create(new KafkaOptions()));

            var builder = new Mock<IWebJobsBuilder>();
            builder.SetupGet(p => p.Services).Returns(services);

            builder.Object.AddKafkaScaleForTrigger(CreateTriggerMetadata("topic-one"));
            builder.Object.AddKafkaScaleForTrigger(CreateTriggerMetadata("topic-two"));

            var provider = services.BuildServiceProvider();
            var monitorProviders = provider.GetServices<IScaleMonitorProvider>().ToArray();
            var targetScalerProviders = provider.GetServices<ITargetScalerProvider>().ToArray();

            Assert.Equal(2, monitorProviders.Length);
            Assert.Equal(2, targetScalerProviders.Length);
            Assert.NotSame(monitorProviders[0], monitorProviders[1]);
            Assert.Same(monitorProviders[0], targetScalerProviders[0]);
            Assert.Same(monitorProviders[1], targetScalerProviders[1]);

            provider.Dispose();

            var disposedField = typeof(KafkaScalerProvider).GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.All(monitorProviders, scalerProvider =>
                Assert.True((bool)disposedField.GetValue(scalerProvider)));
        }

        private static TriggerMetadata CreateTriggerMetadata(string topic)
        {
            var metadata = new JObject
            {
                { "BrokerList", "brokerList" },
                { "Topic", topic },
                { "ConsumerGroup", "consumerGroup" },
                { "LagThreshold", 1000 },
            };

            return new TriggerMetadata(metadata);
        }

        private sealed class TestServiceCollection : List<ServiceDescriptor>, IServiceCollection
        {
        }
    }
}

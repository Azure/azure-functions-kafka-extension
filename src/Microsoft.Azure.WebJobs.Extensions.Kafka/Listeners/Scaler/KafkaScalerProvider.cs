// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static Confluent.Kafka.ConfigPropertyNames;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaScalerProvider : IScaleMonitorProvider, ITargetScalerProvider
    {
        private readonly KafkaObjectTopicScaler _scaleMonitor;
        private readonly KafkaObjectTargetScaler _targetScaler;


        public KafkaScalerProvider(IServiceProvider serviceProvider, TriggerMetadata triggerMetadata)
        {
            var config = serviceProvider.GetService<IConfiguration>();
            var nameResolver = serviceProvider.GetService<INameResolver>();
            KafkaMetaData kafkaMetadata = JsonConvert.DeserializeObject<KafkaMetaData>(triggerMetadata.Metadata.ToString());
            kafkaMetadata.ResolveProperties(config, nameResolver);
            IOptions<KafkaOptions> options = serviceProvider.GetService<IOptions<KafkaOptions>>();
            string topicName = kafkaMetadata.Topic;
            string consumerGroup = kafkaMetadata.ConsumerGroup;
            long lagThreshold = kafkaMetadata.LagThreshold;
            ILoggerFactory loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory("Kafka"));
            var metricsProvider = new KafkaMetricsProvider<Object, Object>(topicName, new AdminClientConfig(GetAdminConfiguration(kafkaMetadata, config, nameResolver)), logger);

            _scaleMonitor = new KafkaObjectTopicScaler(topicName, consumerGroup, metricsProvider, lagThreshold, logger);
            _targetScaler = new KafkaObjectTargetScaler(topicName, consumerGroup, metricsProvider, lagThreshold, logger);
        }

        private ConsumerConfig GetAdminConfiguration(KafkaMetaData kafkaMetaData, IConfiguration config, INameResolver nameResolver)
        {

            var adminConfig = new ConsumerConfig() {
                GroupId = config.ResolveSecureSetting(nameResolver, kafkaMetaData.ConsumerGroup),
                BootstrapServers = config.ResolveSecureSetting(nameResolver, kafkaMetaData.BrokerList),
            };

            if (kafkaMetaData.AuthenticationMode != BrokerAuthenticationMode.NotSet ||
                kafkaMetaData.Protocol != BrokerProtocol.NotSet)
            {
                adminConfig.SaslPassword = config.ResolveSecureSetting(nameResolver, kafkaMetaData.Password);
                adminConfig.SaslUsername = config.ResolveSecureSetting(nameResolver, kafkaMetaData.Username);
                adminConfig.SslKeyPassword = config.ResolveSecureSetting(nameResolver, kafkaMetaData.SslKeyPassword);
                adminConfig.SslCertificatePem = config.ResolveSecureSetting(nameResolver, kafkaMetaData.SslCertificatePEM);
                adminConfig.SslCaPem = ExtractCertificate(config.ResolveSecureSetting(nameResolver, kafkaMetaData.SslCaPEM));
                adminConfig.SslKeyPem = config.ResolveSecureSetting(nameResolver, kafkaMetaData.SslKeyPEM);

                if (!string.IsNullOrEmpty(kafkaMetaData.SslCertificateandKeyPEM))
                {
                    adminConfig.SslCertificatePem = ExtractCertificate(kafkaMetaData.SslCertificateandKeyPEM);
                    adminConfig.SslKeyPem = ExtractPrivateKey(kafkaMetaData.SslCertificateandKeyPEM);
                }

                if (kafkaMetaData.AuthenticationMode != BrokerAuthenticationMode.NotSet)
                {
                    adminConfig.SaslMechanism = (SaslMechanism)kafkaMetaData.AuthenticationMode;
                }

                if (kafkaMetaData.Protocol != BrokerProtocol.NotSet)
                {
                    adminConfig.SecurityProtocol = (SecurityProtocol)kafkaMetaData.Protocol;
                }

                if (kafkaMetaData.AuthenticationMode == BrokerAuthenticationMode.OAuthBearer)
                {
                    adminConfig.SaslOauthbearerMethod = (SaslOauthbearerMethod)kafkaMetaData.OAuthBearerMethod;
                    adminConfig.SaslOauthbearerClientId = config.ResolveSecureSetting(nameResolver, kafkaMetaData.OAuthBearerClientId);
                    adminConfig.SaslOauthbearerClientSecret = config.ResolveSecureSetting(nameResolver, kafkaMetaData.OAuthBearerClientSecret);
                    adminConfig.SaslOauthbearerScope = config.ResolveSecureSetting(nameResolver, kafkaMetaData.OAuthBearerScope);
                    adminConfig.SaslOauthbearerTokenEndpointUrl = config.ResolveSecureSetting(nameResolver, kafkaMetaData.OAuthBearerTokenEndpointUrl);
                    adminConfig.SaslOauthbearerExtensions = config.ResolveSecureSetting(nameResolver, kafkaMetaData.OAuthBearerExtensions);
                }
            }

            return adminConfig;
        }

        public IScaleMonitor GetMonitor()
        {
            return _scaleMonitor;
        }

        public ITargetScaler GetTargetScaler()
        {
            return _targetScaler;
        }

        private string ExtractSection(string pemString, string sectionName)
        {
            if (!string.IsNullOrEmpty(pemString))
            {
                var regex = new Regex($"-----BEGIN {sectionName}-----(.*?)-----END {sectionName}-----", RegexOptions.Singleline);
                var match = regex.Match(pemString);
                if (match.Success)
                {
                    return match.Value;
                }
            }
            return null;
        }

        private string ExtractCertificate(string pemString)
        {
             return ExtractSection(pemString, "CERTIFICATE");
        }

        private string ExtractPrivateKey(string pemString)
        {
            return ExtractSection(pemString, "PRIVATE KEY");
        }

        internal class KafkaMetaData
        {
            [JsonProperty]
            public string Topic { get; set; }

            [JsonProperty]
            public string BrokerList { get; set; }

            [JsonProperty]
            public string ConsumerGroup { get; set; }

            [JsonProperty]
            public long LagThreshold { get; set; }

            [JsonProperty]
            public string Username { get; set; }

            [JsonProperty]
            public string Password { get; set; }

            [JsonProperty]
            public SaslMechanism SaslMechanism { get; set; }

            [JsonProperty]
            public BrokerProtocol Protocol { get; set; } = BrokerProtocol.NotSet;

            [JsonProperty]
            public BrokerAuthenticationMode AuthenticationMode { get; set; } = BrokerAuthenticationMode.NotSet;

            [JsonProperty]
            public string SslCertificatePEM { get; set; }

            [JsonProperty]
            public string SslKeyPEM { get; set; }

            [JsonProperty]
            public string SslKeyPassword { get; set; }

            [JsonProperty]
            public string SslCertificateandKeyPEM { get; set; }

            [JsonProperty]
            public string SslCaPEM { get; set; }

            [JsonProperty]
            public string OAuthBearerClientId { get; set; }

            [JsonProperty]
            public string OAuthBearerClientSecret { get; set; }

            [JsonProperty]
            public string OAuthBearerScope { get; set; }

            [JsonProperty]
            public string OAuthBearerTokenEndpointUrl { get; set; }

            [JsonProperty]
            public string OAuthBearerExtensions { get; set; }

            [JsonProperty]
            public SaslOauthbearerMethod OAuthBearerMethod { get; set; }

            public void ResolveProperties(IConfiguration config, INameResolver resolver)
            {
                if (resolver != null)
                {
                    Topic = resolver.ResolveWholeString(Topic);
                    ConsumerGroup = resolver.ResolveWholeString(ConsumerGroup);
                    BrokerList = resolver.ResolveWholeString(BrokerList);
                    Username = resolver.ResolveWholeString(Username);
                    Password = resolver.ResolveWholeString(Password);
                }
            }
        }
    }
}

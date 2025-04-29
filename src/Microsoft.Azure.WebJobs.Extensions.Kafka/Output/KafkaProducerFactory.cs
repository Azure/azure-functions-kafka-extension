// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Factory for <see cref="IKafkaProducer"/>
    /// Those matching the broker, key type and value type are shared
    /// </summary>
    public class KafkaProducerFactory : IKafkaProducerFactory
    {
        private readonly IConfiguration config;
        private readonly INameResolver nameResolver;
        private readonly ILoggerFactory loggerFactory;
        private readonly ConcurrentDictionary<string, IProducer<byte[], byte[]>> baseProducers = new ConcurrentDictionary<string, IProducer<byte[], byte[]>>();

        public KafkaProducerFactory(
            IConfiguration config,
            INameResolver nameResolver,
            ILoggerFactory loggerFactory)
        {
            this.config = config;
            this.nameResolver = nameResolver;
            this.loggerFactory = loggerFactory;
        }

        public IKafkaProducer Create(KafkaProducerEntity entity)
        {
            AzureFunctionsFileHelper.InitializeLibrdKafka(this.loggerFactory.CreateLogger(typeof(AzureFunctionsFileHelper)));

            // Goal is to create as less producers as possible
            // We can group producers based on following criterias
            // - Broker List
            // - Configuration
            var producerConfig = this.GetProducerConfig(entity);
            var producerKey = CreateKeyForConfig(producerConfig);

            var baseProducer = baseProducers.GetOrAdd(producerKey, (k) => CreateBaseProducer(producerConfig));
            return Create(baseProducer.Handle, entity);
        }

        /// <summary>
        /// Creates a config key by concatenating all property key and values
        /// </summary>
        private string CreateKeyForConfig(ProducerConfig producerConfig)
        {
            var keyBuilder = new StringBuilder();

            foreach (var kv in producerConfig)
            {
                keyBuilder
                    .Append(kv.Key)
                    .Append('=')
                    .Append(kv.Value)
                    .Append(';');
            }

            return keyBuilder.ToString();
        }

        private IProducer<byte[], byte[]> CreateBaseProducer(ProducerConfig producerConfig)
        {
            var builder = new ProducerBuilder<byte[], byte[]>(producerConfig);
            ILogger logger = this.loggerFactory.CreateLogger("Kafka");
            builder.SetLogHandler((_, m) =>
            {
                logger.Log((LogLevel)m.LevelAs(LogLevelType.MicrosoftExtensionsLogging), $"Libkafka: {m?.Message}");
            });

            return builder.Build();
        }

        private IKafkaProducer Create(Handle producerBaseHandle, KafkaProducerEntity entity)
        {
            var valueType = entity.ValueType ?? typeof(byte[]);
            var keyType = entity.KeyType ?? typeof(Null);

            var valueAvroSchema = this.config.ResolveSecureSetting(nameResolver, entity.ValueAvroSchema);
            var keyAvroSchema = this.config.ResolveSecureSetting(nameResolver, entity.KeyAvroSchema);
            var schemaRegistryUrl = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.SchemaRegistryUrl);
            var schemaRegistryUsername = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.SchemaRegistryUsername);
            var schemaRegistryPassword = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.SchemaRegistryPassword);

            var valueSerializer = SerializationHelper.ResolveValueSerializer(valueType, valueAvroSchema, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword);
            var keySerializer = SerializationHelper.ResolveValueSerializer(keyType, keyAvroSchema, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword);

            return (IKafkaProducer)Activator.CreateInstance(
                typeof(KafkaProducer<,>).MakeGenericType(keyType, valueType),
                producerBaseHandle,
                valueSerializer,
                keySerializer,
                loggerFactory.CreateLogger(typeof(KafkaProducer<,>)));
        }

        public ProducerConfig GetProducerConfig(KafkaProducerEntity entity)
        {
            var sslCertificateLocation = config.ResolveSecureSetting(nameResolver, entity.Attribute.SslCertificateLocation);
            if (!AzureFunctionsFileHelper.TryGetValidFilePath(sslCertificateLocation, out var resolvedSslCertificationLocation))
            {
                resolvedSslCertificationLocation = sslCertificateLocation;
            }

            var sslCaLocation = config.ResolveSecureSetting(nameResolver, entity.Attribute.SslCaLocation);
            if (!AzureFunctionsFileHelper.TryGetValidFilePath(sslCaLocation, out var resolvedSslCaLocation))
            {
                resolvedSslCaLocation = sslCaLocation;
            }

            var sslKeyLocation = config.ResolveSecureSetting(nameResolver, entity.Attribute.SslKeyLocation);
            if (!AzureFunctionsFileHelper.TryGetValidFilePath(sslKeyLocation, out var resolvedSslKeyLocation))
            {
                resolvedSslKeyLocation = sslKeyLocation;
            }
            var kafkaOptions = this.config.Get<KafkaOptions>();
            var conf = new ProducerConfig()
            {
                BootstrapServers = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.BrokerList),
                BatchNumMessages = entity.Attribute.BatchSize,
                EnableIdempotence = entity.Attribute.EnableIdempotence,
                MessageSendMaxRetries = entity.Attribute.MaxRetries,
                MessageTimeoutMs = entity.Attribute.MessageTimeoutMs,
                RequestTimeoutMs = entity.Attribute.RequestTimeoutMs,
                MessageMaxBytes = entity.Attribute.MaxMessageBytes,
                SaslPassword = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.Password),
                SaslUsername = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.Username),
                SslKeyLocation = resolvedSslKeyLocation,
                SslKeyPassword = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.SslKeyPassword),
                SslCertificateLocation = resolvedSslCertificationLocation,
                SslCaLocation = resolvedSslCaLocation,
                SslCaPem = Config.PEMExtractor.ExtractAllCertificates(this.config.ResolveSecureSetting(nameResolver, entity.Attribute.SslCaPEM)),
                SslCertificatePem = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.SslCertificatePEM),
                SslKeyPem = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.SslKeyPEM),
                Debug = kafkaOptions?.LibkafkaDebug,
                MetadataMaxAgeMs = kafkaOptions?.MetadataMaxAgeMs,
                SocketKeepaliveEnable = kafkaOptions?.SocketKeepaliveEnable,
                LingerMs = entity.Attribute.LingerMs,
            };

            if (!string.IsNullOrEmpty(entity.Attribute.SslCertificateandKeyPEM))
            {
                var sslCertificateandKeyPEM = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.SslCertificateandKeyPEM);
                conf.SslCertificatePem = Config.PEMExtractor.ExtractCertificate(sslCertificateandKeyPEM);
                conf.SslKeyPem = Config.PEMExtractor.ExtractPrivateKey(sslCertificateandKeyPEM);
            }

            if (entity.Attribute.AuthenticationMode != BrokerAuthenticationMode.NotSet)
            {
                conf.SaslMechanism = (SaslMechanism)entity.Attribute.AuthenticationMode;
            }

            if (entity.Attribute.Protocol != BrokerProtocol.NotSet)
            {
                conf.SecurityProtocol = (SecurityProtocol)entity.Attribute.Protocol;
            }

            if (entity.Attribute.AuthenticationMode == BrokerAuthenticationMode.OAuthBearer)
            {
                conf.SaslOauthbearerMethod = (SaslOauthbearerMethod)entity.Attribute.OAuthBearerMethod;
                conf.SaslOauthbearerClientId = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.OAuthBearerClientId);
                conf.SaslOauthbearerClientSecret = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.OAuthBearerClientSecret);
                conf.SaslOauthbearerScope = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.OAuthBearerScope);
                conf.SaslOauthbearerTokenEndpointUrl = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.OAuthBearerTokenEndpointUrl);
                conf.SaslOauthbearerExtensions = this.config.ResolveSecureSetting(nameResolver, entity.Attribute.OAuthBearerExtensions);
            }

            return conf;
        }
    }
}
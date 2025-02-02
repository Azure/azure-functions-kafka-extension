﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Extensions.Kafka.Config;
using Microsoft.Azure.WebJobs.Extensions.Kafka.Trigger;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private readonly IConfiguration config;
        private readonly IConverterManager converterManager;
        private readonly INameResolver nameResolver;
        private readonly IOptions<KafkaOptions> options;
        private readonly ILogger logger;

        public KafkaTriggerAttributeBindingProvider(
            IConfiguration config,
            IOptions<KafkaOptions> options,
            IConverterManager converterManager,
            INameResolver nameResolver,
            ILoggerFactory loggerFactory)
        {
            this.config = config;
            this.converterManager = converterManager;
            this.nameResolver = nameResolver;
            this.options = options;
            this.logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory("Kafka"));
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var parameter = context.Parameter;
            var attribute = parameter.GetCustomAttribute<KafkaTriggerAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            var keyAndValueTypes = SerializationHelper.GetKeyAndValueTypes(attribute.AvroSchema, parameter.ParameterType, typeof(string));
            var schemaRegistryUrl = this.config.ResolveSecureSetting(nameResolver, attribute.SchemaRegistryUrl);
            var schemaRegistryUsername = this.config.ResolveSecureSetting(nameResolver, attribute.SchemaRegistryUsername);
            var schemaRegistryPassword = this.config.ResolveSecureSetting(nameResolver, attribute.SchemaRegistryPassword);
            var valueDeserializer = SerializationHelper.ResolveValueDeserializer(keyAndValueTypes.ValueType, keyAndValueTypes.AvroSchema, schemaRegistryUrl, schemaRegistryUsername, schemaRegistryPassword);

            var consumerConfig = CreateConsumerConfiguration(attribute);
            var binding = CreateBindingStrategyFor(keyAndValueTypes.KeyType ?? typeof(Ignore), keyAndValueTypes.ValueType, keyAndValueTypes.RequiresKey, valueDeserializer, parameter, consumerConfig);
            return Task.FromResult<ITriggerBinding>(new KafkaTriggerBindingWrapper(binding));
        }

        ITriggerBinding CreateBindingStrategyFor(Type keyType, Type valueType, bool requiresKey, object valueDeserializer, ParameterInfo parameterInfo, KafkaListenerConfiguration listenerConfiguration)
        {
            var genericCreateBindingStrategy = this.GetType().GetMethod(nameof(CreateBindingStrategy), BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(keyType, valueType);
            return (ITriggerBinding)genericCreateBindingStrategy.Invoke(this, new object[] { parameterInfo, listenerConfiguration, requiresKey, valueDeserializer });
        }

        private ITriggerBinding CreateBindingStrategy<TKey, TValue>(ParameterInfo parameter, KafkaListenerConfiguration listenerConfiguration, bool requiresKey, IDeserializer<TValue> valueDeserializer)
        {
            // TODO: reuse connections if they match with others in same function app
            Task<IListener> listenerCreator(ListenerFactoryContext factoryContext, bool singleDispatch)
            {
                var listener = new KafkaListener<TKey, TValue>(
                    factoryContext.Executor,
                    singleDispatch,
                    this.options.Value,
                    listenerConfiguration,
                    requiresKey,
                    valueDeserializer,
                    this.logger,
                    factoryContext.Descriptor.Id);

                return Task.FromResult<IListener>(listener);
            }

            return BindingFactory.GetTriggerBinding(new KafkaTriggerBindingStrategy<TKey, TValue>(), parameter, new KafkaEventDataConvertManager(this.converterManager, this.logger), listenerCreator);
        }

        private KafkaListenerConfiguration CreateConsumerConfiguration(KafkaTriggerAttribute attribute)
        {
            var consumerConfig = new KafkaListenerConfiguration()
            {
                BrokerList = this.config.ResolveSecureSetting(nameResolver, attribute.BrokerList),
                ConsumerGroup = this.config.ResolveSecureSetting(nameResolver, attribute.ConsumerGroup),
                Topic = this.config.ResolveSecureSetting(nameResolver, attribute.Topic),
                EventHubConnectionString = this.config.ResolveSecureSetting(nameResolver, attribute.EventHubConnectionString),
                LagThreshold = attribute.LagThreshold
            };

            if (attribute.AuthenticationMode != BrokerAuthenticationMode.NotSet ||
                attribute.Protocol != BrokerProtocol.NotSet)
            {
                consumerConfig.SaslPassword = this.config.ResolveSecureSetting(nameResolver, attribute.Password);
                consumerConfig.SaslUsername = this.config.ResolveSecureSetting(nameResolver, attribute.Username);
                consumerConfig.SslKeyLocation = GetValidFilePath(attribute.SslKeyLocation);
                consumerConfig.SslKeyPassword = this.config.ResolveSecureSetting(nameResolver, attribute.SslKeyPassword);
                consumerConfig.SslCertificateLocation = GetValidFilePath(attribute.SslCertificateLocation);
                consumerConfig.SslCaLocation = GetValidFilePath(attribute.SslCaLocation);
                consumerConfig.SslCaPEM = ExtractCertificate(this.config.ResolveSecureSetting(nameResolver, attribute.SslCaPEM));
                consumerConfig.SslCertificatePEM = this.config.ResolveSecureSetting(nameResolver, attribute.SslCertificatePEM);
                consumerConfig.SslKeyPEM = this.config.ResolveSecureSetting(nameResolver, attribute.SslKeyPEM);
                consumerConfig.SslCertificateandKeyPEM = this.config.ResolveSecureSetting(nameResolver, attribute.SslCertificateandKeyPEM);

                if (!string.IsNullOrEmpty(consumerConfig.SslCertificateandKeyPEM)) {
                    consumerConfig.SslCertificatePEM = ExtractCertificate(consumerConfig.SslCertificateandKeyPEM);
                    consumerConfig.SslKeyPEM = ExtractPrivateKey(consumerConfig.SslCertificateandKeyPEM);
                }

                if (attribute.AuthenticationMode != BrokerAuthenticationMode.NotSet)
                {
                    consumerConfig.SaslMechanism = (SaslMechanism)attribute.AuthenticationMode;
                }

                if (attribute.Protocol != BrokerProtocol.NotSet)
                {
                    consumerConfig.SecurityProtocol = (SecurityProtocol)attribute.Protocol;
                }

                if (attribute.AuthenticationMode == BrokerAuthenticationMode.OAuthBearer)
                {
                    consumerConfig.SaslOAuthBearerMethod = (SaslOauthbearerMethod)attribute.OAuthBearerMethod;
                    consumerConfig.SaslOAuthBearerClientId = this.config.ResolveSecureSetting(nameResolver, attribute.OAuthBearerClientId);
                    consumerConfig.SaslOAuthBearerClientSecret = this.config.ResolveSecureSetting(nameResolver, attribute.OAuthBearerClientSecret);
                    consumerConfig.SaslOAuthBearerScope = this.config.ResolveSecureSetting(nameResolver, attribute.OAuthBearerScope);
                    consumerConfig.SaslOAuthBearerTokenEndpointUrl = this.config.ResolveSecureSetting(nameResolver, attribute.OAuthBearerTokenEndpointUrl);
                    consumerConfig.SaslOAuthBearerExtensions = this.config.ResolveSecureSetting(nameResolver, attribute.OAuthBearerExtensions);
                }
            }

            return consumerConfig;
        }

        private string GetValidFilePath(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return null;
            }
            var resolvedLocation = this.config.ResolveSecureSetting(nameResolver, location);
            if (!AzureFunctionsFileHelper.TryGetValidFilePath(resolvedLocation, out var validPath))
            {
                throw new Exception($"{location} is not a valid file location");
            }
            return validPath;
        }

        private string ExtractSection(string pemString, string sectionName)
        {
            if (!string.IsNullOrEmpty(pemString))
            {
                var regex = new Regex($"-----BEGIN {sectionName}-----(.*?)-----END {sectionName}-----", RegexOptions.Singleline);
                var match = regex.Match(pemString);
                if (match.Success)
                {
                    return match.Value.Replace("\\n", "\n");
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
    }
}
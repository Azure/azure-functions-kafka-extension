// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Avro.Generic;
using Avro.Specific;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Configuration;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    [Extension("Kafka", configurationSection: "kafka")]
    public class KafkaExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IConfiguration config;
        private readonly IOptions<KafkaOptions> options;
        private readonly ILoggerFactory loggerFactory;
        private readonly IConverterManager converterManager;
        private readonly INameResolver nameResolver;
        private readonly IWebJobsExtensionConfiguration<KafkaExtensionConfigProvider> configuration;
        private readonly IKafkaProducerFactory kafkaProducerManager;
        private readonly ILogger logger;

        public KafkaExtensionConfigProvider(
            IConfiguration config,
            IOptions<KafkaOptions> options,
            ILoggerFactory loggerFactory,
            IConverterManager converterManager,
            INameResolver nameResolver,
            IWebJobsExtensionConfiguration<KafkaExtensionConfigProvider> configuration,
            IKafkaProducerFactory kafkaProducerManager)
        {
            this.config = config;
            this.options = options;
            this.loggerFactory = loggerFactory;
            this.converterManager = converterManager;
            this.nameResolver = nameResolver;
            this.configuration = configuration;
            this.kafkaProducerManager = kafkaProducerManager;
            this.logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory("Kafka"));
        }

        public void Initialize(ExtensionConfigContext context)
        {
            configuration.ConfigurationSection.Bind(options);

            context
               .AddConverter<KafkaEventData, string>(ConvertKafkaEventData2String)
               .AddConverter<KafkaEventData, ISpecificRecord>(ConvertKafkaEventData2AvroSpecific)
               .AddConverter<KafkaEventData, byte[]>(ConvertKafkaEventData2Bytes);

            // register our trigger binding provider
            var triggerBindingProvider = new KafkaTriggerAttributeBindingProvider(config, options, converterManager, nameResolver, loggerFactory);
            context.AddBindingRule<KafkaTriggerAttribute>()
                .BindToTrigger(triggerBindingProvider);

            // register output binding
            context.AddBindingRule<KafkaAttribute>()
                .BindToCollector(BuildCollectorFromAttribute);
        }

        private IAsyncCollector<KafkaEventData> BuildCollectorFromAttribute(KafkaAttribute attribute)
        {
            return new KafkaAsyncCollector(attribute.Topic, kafkaProducerManager.Create(attribute));
        }

        private ISpecificRecord ConvertKafkaEventData2AvroSpecific(KafkaEventData kafkaEventData)
        {
            if (kafkaEventData.Value is ISpecificRecord specRecord)
            {
                return specRecord;
            }
            else
            {
                logger.LogWarning($@"Unable to convert incoming data to Avro format. Expected ISpecificRecord, got {kafkaEventData.Value.GetType()}. Returning [null]");
                return null;
            }
        }

        private string ConvertKafkaEventData2String(KafkaEventData kafkaEventData)
        {
            try
            {
                if (kafkaEventData.Value is GenericRecord genericRecord)
                {
                    return GenericRecord2String(genericRecord);
                }
                else if (kafkaEventData.Value is byte[] binaryContent)
                {
                    if (binaryContent != null)
                    {
                        return Encoding.UTF8.GetString(binaryContent);
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                else
                {
                    return JsonConvert.SerializeObject(kafkaEventData);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $@"Unable to convert incoming data to string.");
                throw;
            }
        }


        private string GenericRecord2String(GenericRecord record)
        {
            var props = new Dictionary<string, object>();
            foreach (var field in record.Schema.Fields)
            {
                if (record.TryGetValue(field.Name, out var value))
                {
                    props[field.Name] = value;
                }
            }

            return JsonConvert.SerializeObject(props);
        }

        private byte[] ConvertKafkaEventData2Bytes(KafkaEventData input)
        {
            if (input.Value is byte[] bytes)
            {
                return bytes;
            }

            logger.LogWarning($@"Unable to convert incoming data to byte[] as underlying data stream was not byte[]. Returning [null]");
            return null;
        }
    }
}
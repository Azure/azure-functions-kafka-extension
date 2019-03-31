// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Avro.Generic;
using Avro.Specific;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Host.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
        private readonly IKafkaProducerProvider kafkaProducerManager;

        public KafkaExtensionConfigProvider(
            IConfiguration config,
            IOptions<KafkaOptions> options,
            ILoggerFactory loggerFactory,
            IConverterManager converterManager,
            INameResolver nameResolver,
            IWebJobsExtensionConfiguration<KafkaExtensionConfigProvider> configuration,
            IKafkaProducerProvider kafkaProducerManager)
        {
            this.config = config;
            this.options = options;
            this.loggerFactory = loggerFactory;
            this.converterManager = converterManager;
            this.nameResolver = nameResolver;
            this.configuration = configuration;
            this.kafkaProducerManager = kafkaProducerManager;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            this.configuration.ConfigurationSection.Bind(this.options);

            context
               .AddConverter<string, KafkaEventData>(ConvertString2KafkaEventData)
               .AddConverter<KafkaEventData, string>(ConvertKafkaEventData2String)
               .AddConverter<KafkaEventData, ISpecificRecord>(ConvertKafkaEventData2AvroSpecific)
               .AddConverter<byte[], KafkaEventData>(ConvertBytes2KafkaEventData)
               .AddConverter<KafkaEventData, byte[]>(ConvertKafkaEventData2Bytes)
               .AddOpenConverter<OpenType.Poco, KafkaEventData>(ConvertPocoToKafkaEventData);

            // register our trigger binding provider
            var triggerBindingProvider = new KafkaTriggerAttributeBindingProvider(this.config, this.options, this.converterManager, this.nameResolver, this.loggerFactory);
            context.AddBindingRule<KafkaTriggerAttribute>()
                .BindToTrigger(triggerBindingProvider);

            // register output binding
            context.AddBindingRule<KafkaAttribute>()
                .BindToCollector(BuildCollectorFromAttribute);
        }

        private IAsyncCollector<KafkaEventData> BuildCollectorFromAttribute(KafkaAttribute attribute)
        {
            return new KafkaAsyncCollector(attribute.Topic, this.kafkaProducerManager.Get(attribute));
        }

        private ISpecificRecord ConvertKafkaEventData2AvroSpecific(KafkaEventData kafkaEventData)
        {
            return (ISpecificRecord)kafkaEventData.Value;
        }

        private static string ConvertKafkaEventData2String(KafkaEventData kafkaEventData)
        {
            try
            {
                if (kafkaEventData.Value is GenericRecord genericRecord)
                {
                    return GenericRecord2String(genericRecord);
                }
                else
                {
                    return JsonConvert.SerializeObject(kafkaEventData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }


        private static string GenericRecord2String(GenericRecord record)
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

        private static KafkaEventData ConvertBytes2KafkaEventData(byte[] input)
            => new KafkaEventData(input);

        private static byte[] ConvertKafkaEventData2Bytes(KafkaEventData input)
            => Encoding.UTF8.GetBytes(input.Value.ToString());

        private static KafkaEventData ConvertString2KafkaEventData(string input)
            => ConvertBytes2KafkaEventData(Encoding.UTF8.GetBytes(input));

        private static Task<object> ConvertPocoToKafkaEventData(object arg, Attribute attrResolved, ValueBindingContext context)
        {
            return Task.FromResult<object>(ConvertString2KafkaEventData(JsonConvert.SerializeObject(arg)));
        }
    }
}
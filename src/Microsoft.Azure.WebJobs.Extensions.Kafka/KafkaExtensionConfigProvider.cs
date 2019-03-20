// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Avro.Specific;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public class KafkaExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IConfiguration config;
        private readonly ILoggerFactory loggerFactory;
        private readonly IConverterManager converterManager;
        private readonly INameResolver nameResolver;

        public KafkaExtensionConfigProvider(
            IConfiguration config,
            ILoggerFactory loggerFactory,
            IConverterManager converterManager,
            INameResolver nameResolver)
        {
            this.config = config;
            this.loggerFactory = loggerFactory;
            this.converterManager = converterManager;
            this.nameResolver = nameResolver;
        }

        public void Initialize(ExtensionConfigContext context)
        {

            context
               .AddConverter<string, KafkaEventData>(ConvertString2KafkaEventData)
               .AddConverter<KafkaEventData, string>(ConvertKafkaEventData2String)
               .AddConverter<KafkaEventData, ISpecificRecord>(ConvertKafkaEventData2AvroSpecific)
               .AddConverter<byte[], KafkaEventData>(ConvertBytes2KafkaEventData)
               .AddConverter<KafkaEventData, byte[]>(ConvertKafkaEventData2Bytes)
               .AddOpenConverter<OpenType.Poco, KafkaEventData>(ConvertPocoToKafkaEventData);

            // register our trigger binding provider
            var triggerBindingProvider = new KafkaTriggerAttributeBindingProvider(this.config, this.loggerFactory, this.converterManager, this.nameResolver);
            context.AddBindingRule<KafkaTriggerAttribute>()
                .BindToTrigger(triggerBindingProvider);
        }

        private ISpecificRecord ConvertKafkaEventData2AvroSpecific(KafkaEventData kafkaEventData)
        {
            return (ISpecificRecord)kafkaEventData.Value;
        }

        private static string ConvertKafkaEventData2String(KafkaEventData kafkaEventData)
           => JsonConvert.SerializeObject(kafkaEventData);

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
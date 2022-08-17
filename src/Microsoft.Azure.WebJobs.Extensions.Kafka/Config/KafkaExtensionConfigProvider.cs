﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
        private readonly IKafkaProducerFactory kafkaProducerFactory;
        private readonly ILogger logger;

        public KafkaExtensionConfigProvider(
            IConfiguration config,
            IOptions<KafkaOptions> options,
            ILoggerFactory loggerFactory,
            IConverterManager converterManager,
            INameResolver nameResolver,
            IWebJobsExtensionConfiguration<KafkaExtensionConfigProvider> configuration,
            IKafkaProducerFactory kafkaProducerFactory)
        {
            this.config = config;
            this.options = options;
            this.loggerFactory = loggerFactory;
            this.converterManager = converterManager;
            this.nameResolver = nameResolver;
            this.configuration = configuration;
            this.kafkaProducerFactory = kafkaProducerFactory;
            this.logger = loggerFactory.CreateLogger(LogCategories.CreateTriggerCategory("Kafka"));
            this.kafkaProducerFactory.SetLogger(logger);
        }

        public void Initialize(ExtensionConfigContext context)
        {
            configuration.ConfigurationSection.Bind(options);

            // register our trigger binding provider
            var triggerBindingProvider = new KafkaTriggerAttributeBindingProvider(config, options, converterManager, nameResolver, loggerFactory);
            context.AddBindingRule<KafkaTriggerAttribute>()
                .BindToTrigger(triggerBindingProvider);

            // register output binding
            context.AddBindingRule<KafkaAttribute>().Bind(new KafkaAttributeBindingProvider(config, nameResolver, this.kafkaProducerFactory));
        }
    }
}
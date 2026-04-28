// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public static class KafkaWebJobsBuilderExtensions
    {
        /// <summary>
        /// Adds the Kafka extensions to the provider <see cref="IWebJobsBuilder"/>.
        /// </summary>
        public static IWebJobsBuilder AddKafka(this IWebJobsBuilder builder) => AddKafka(builder, o => { });

        /// <summary>
        /// Adds the Kafka extensions to the provider <see cref="IWebJobsBuilder"/>.
        /// </summary>
        public static IWebJobsBuilder AddKafka(this IWebJobsBuilder builder, Action<KafkaOptions> configure)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddExtension<KafkaExtensionConfigProvider>()
                .BindOptions<KafkaOptions>();

            builder.Services.Configure<KafkaOptions>(options =>
            {
                configure(options);
            });

            builder.Services.AddSingleton<IKafkaProducerFactory, KafkaProducerFactory>();

            return builder;
        }

        internal static IWebJobsBuilder AddKafkaScaleForTrigger(this IWebJobsBuilder builder, TriggerMetadata triggerMetadata)
        {
            // Register KafkaScalerProvider as a singleton that the DI container manages directly.
            // This ensures that:
            // 1. The same instance is shared between IScaleMonitorProvider and ITargetScalerProvider
            // 2. The DI container will call Dispose() on the instance when the host is disposed
            // 3. This fixes the librdkafka thread leak issue where native threads were not being cleaned up
            //
            // Previous implementation used Lazy<T> which prevented the DI container from managing
            // the lifetime of KafkaScalerProvider, causing thread leaks when consumers were not disposed.
            builder.Services.AddSingleton<KafkaScalerProvider>(serviceProvider => 
                new KafkaScalerProvider(serviceProvider, triggerMetadata));

            // Register the same singleton instance for both interfaces
            builder.Services.AddSingleton<IScaleMonitorProvider>(serviceProvider =>
                serviceProvider.GetRequiredService<KafkaScalerProvider>());
            builder.Services.AddSingleton<ITargetScalerProvider>(serviceProvider =>
                serviceProvider.GetRequiredService<KafkaScalerProvider>());

            return builder;
        }
    }
}
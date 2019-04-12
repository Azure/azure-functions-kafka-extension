// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public static class KafkaWebJobsBuilderExtensions
    {
        /// <summary>
        /// Adds the Kafka extensions to the provider <see cref="IWebJobsBuilder"/>
        /// </summary>
        public static IWebJobsBuilder AddKafka(this IWebJobsBuilder builder) => AddKafka(builder, o => { });

        /// <summary>
        /// Adds the Kafka extensions to the provider <see cref="IWebJobsBuilder"/>
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
    }
}
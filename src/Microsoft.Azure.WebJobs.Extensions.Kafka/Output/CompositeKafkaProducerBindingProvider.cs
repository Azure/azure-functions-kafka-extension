// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Bindings;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class CompositeKafkaProducerBindingProvider : IKafkaProducerBindingProvider
    {
        private readonly IEnumerable<IKafkaProducerBindingProvider> providers;

        public CompositeKafkaProducerBindingProvider(params IKafkaProducerBindingProvider[] providers)
        {
            this.providers = providers;
        }

        public IArgumentBinding<KafkaProducerEntity> TryCreate(ParameterInfo parameter)
        {
            foreach (IKafkaProducerBindingProvider provider in providers)
            {
                var binding = provider.TryCreate(parameter);

                if (binding != null)
                {
                    return binding;
                }
            }

            return null;
        }
    }
}
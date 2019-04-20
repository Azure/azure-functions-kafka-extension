// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class StringArgumentBindingProvider : IKafkaProducerBindingProvider
    {
        public StringArgumentBindingProvider()
        {
        }

        public IArgumentBinding<KafkaProducerEntity> TryCreate(ParameterInfo parameter)
        {         
            if (!parameter.IsOut || parameter.ParameterType != typeof(string).MakeByRefType())
            {
                return null;
            }

            return new StringArgumentBinding();
        }

        private class StringArgumentBinding : IArgumentBinding<KafkaProducerEntity>
        {
            public Type ValueType
            {
                get { return typeof(string); }
            }

            
            public Task<IValueProvider> BindAsync(KafkaProducerEntity value, ValueBindingContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }

                IValueProvider provider = new NonNullConverterValueBinder<string>(value,
                    new StringToKafkaEventDataConverter(), context.FunctionInstanceId);

                return Task.FromResult(provider);
            }
        }
    }
}
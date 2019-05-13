// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Bindings;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class ByteArrayArgumentBindingProvider : IKafkaProducerBindingProvider
    {
        public IArgumentBinding<KafkaProducerEntity> TryCreate(ParameterInfo parameter)
        {
            if (!parameter.IsOut || parameter.ParameterType != typeof(byte[]).MakeByRefType())
            {
                return null;
            }

            return new ByteArrayArgumentBinding();
        }

        private class ByteArrayArgumentBinding : IArgumentBinding<KafkaProducerEntity>
        {
            public Type ValueType
            {
                get { return typeof(byte[]); }
            }

            public Task<IValueProvider> BindAsync(KafkaProducerEntity value, ValueBindingContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }

                IValueProvider provider = new NonNullConverterValueBinder<byte[]>(value,
                    new ByteArrayToKafkaEventDataConverter(), context.FunctionInstanceId);

                return Task.FromResult(provider);
            }
        }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
            if (!parameter.IsOut)
            {
                return null;
            }

            var parameterType = GetNonRefType(parameter.ParameterType);
            if (parameterType != typeof(string) && parameterType != typeof(string[]))
            {
                return null;
            }

            return (parameterType.IsArray) ? 
                (IArgumentBinding<KafkaProducerEntity>)new StringArrayArgumentBinding() : new StringArgumentBinding();
        }

        private Type GetNonRefType(Type type) => type.IsByRef ? type.GetElementType() : type;

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

        private class StringArrayArgumentBinding : IArgumentBinding<KafkaProducerEntity>
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

                IValueProvider provider = new NonNullArrayConverterValueBinder<string[]>(value,
                    new StringArrayToKafkaEventDataConverter(), context.FunctionInstanceId);

                return Task.FromResult(provider);
            }
        }

        internal class StringArrayToKafkaEventDataConverter : IConverter<string[], IKafkaEventData[]>
        {
            public IKafkaEventData[] Convert(string[] input)
            {
                var result = new KafkaEventData<string>[input?.Length ?? 0];
                if (input != null)
                {
                    for (int i = 0; i < input.Length; i++)
                    {
                        result[i] = new KafkaEventData<string>(input[i]);
                    }
                }

                return result;
            }
        }
    }
}
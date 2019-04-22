// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class SerializableTypeArgumentBindingProvider : IKafkaProducerBindingProvider
    {
        public SerializableTypeArgumentBindingProvider()
        {
        }

        public IArgumentBinding<KafkaProducerEntity> TryCreate(ParameterInfo parameter)
        {
            if (!parameter.IsOut)
            {
                return null;
            }

            var parameterType = GetNonRefType(parameter.ParameterType);
            if (!SerializationHelper.IsDesSerType(GetNonArrayType(parameterType)))
            {
                return null;
            }

            if (parameterType.IsArray)
            {
                return (IArgumentBinding<KafkaProducerEntity>)Activator.CreateInstance(typeof(SerializableArrayTypeArgumentBinding<,>).MakeGenericType(parameterType, GetNonArrayType(parameterType)));
            }

            return (IArgumentBinding<KafkaProducerEntity>)Activator.CreateInstance(typeof(SerializableTypeArgumentBinding<>).MakeGenericType(parameterType));
        }

        private Type GetNonRefType(Type type) => type.IsByRef ? type.GetElementType() : type;

        private Type GetNonArrayType(Type type) => type.IsArray ? type.GetElementType() : type;


        private class SerializableTypeArgumentBinding<TValue> : IArgumentBinding<KafkaProducerEntity>
        {
            public Type ValueType
            {
                get { return typeof(TValue); }
            }

            
            public Task<IValueProvider> BindAsync(KafkaProducerEntity value, ValueBindingContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }

                IValueProvider provider = new NonNullConverterValueBinder<TValue>(value,
                    new SerializableTypeToKafkaEventDataConverter<TValue>(), context.FunctionInstanceId);

                return Task.FromResult(provider);
            }
        }

        internal class SerializableTypeToKafkaEventDataConverter<TValue> : IConverter<TValue, IKafkaEventData>
        {
            public IKafkaEventData Convert(TValue input)
            {
                return new KafkaEventData<TValue>()
                {
                    Value = input,
                };
            }
        }

        private class SerializableArrayTypeArgumentBinding<TArray, TItem> : IArgumentBinding<KafkaProducerEntity>
        {
            public Type ValueType
            {
                get { return typeof(TArray); }
            }


            public Task<IValueProvider> BindAsync(KafkaProducerEntity value, ValueBindingContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }

                IValueProvider provider = new NonNullArrayConverterValueBinder<TArray>(value,
                    new SerializableTypeToKafkaEventDataConverter<TArray, TItem>(), context.FunctionInstanceId);

                return Task.FromResult(provider);
            }
        }



        internal class SerializableTypeToKafkaEventDataConverter<TArrayType, TItem> : IConverter<TArrayType, IKafkaEventData[]>
        {
            public IKafkaEventData[] Convert(TArrayType input)
            {
                var list = new List<IKafkaEventData>();
                var enumerator = ((IEnumerable)input).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    list.Add(new KafkaEventData<TItem>()
                    {
                        Value = (TItem)enumerator.Current,
                    });
                }

                return list.ToArray();
            }
        }
    }
}
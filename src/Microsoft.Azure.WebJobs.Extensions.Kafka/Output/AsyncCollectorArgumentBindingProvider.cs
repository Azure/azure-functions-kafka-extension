// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class AsyncCollectorArgumentBindingProvider : IKafkaProducerBindingProvider
    {
        public IArgumentBinding<KafkaProducerEntity> TryCreate(ParameterInfo parameter)
        {
            Type parameterType = parameter.ParameterType;

            if (!parameterType.IsGenericType)
            {
                return null;
            }

            Type genericTypeDefinition = parameterType.GetGenericTypeDefinition();

            if (genericTypeDefinition != typeof(IAsyncCollector<>))
            {
                return null;
            }
            
            var genericArguments = parameterType.GetGenericArguments();
            if (genericArguments.Length == 1)
            {
                var valueType = genericArguments[0];
                
                return CreateBinding(valueType);
            }

            throw new Exception($"Could not create IAsyncCollector binding for {parameterType.Name}");
        }

        private static IArgumentBinding<KafkaProducerEntity> CreateBinding(Type itemType)
        {
            MethodInfo method = typeof(AsyncCollectorArgumentBindingProvider).GetMethod(nameof(CreateBindingGeneric),
                BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(method != null);
            MethodInfo genericMethod = method.MakeGenericMethod(itemType);
            Debug.Assert(genericMethod != null);
            Func<IArgumentBinding<KafkaProducerEntity>> lambda =
                (Func<IArgumentBinding<KafkaProducerEntity>>)Delegate.CreateDelegate(
                typeof(Func<IArgumentBinding<KafkaProducerEntity>>), genericMethod);
            return lambda.Invoke();
        }

        private static IArgumentBinding<KafkaProducerEntity> CreateBindingGeneric<TItem>()
        {
            return new AsyncCollectorArgumentBinding<TItem>();
        }

        private class AsyncCollectorArgumentBinding<TItem> : IArgumentBinding<KafkaProducerEntity>
        {    
            public Type ValueType
            {
                get { return typeof(IAsyncCollector<TItem>); }
            }

            public Task<IValueProvider> BindAsync(KafkaProducerEntity value, ValueBindingContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException("context");
                }

                IAsyncCollector<TItem> collector = new KafkaProducerAsyncCollector<TItem>(value, context.FunctionInstanceId);
                IValueProvider provider = new CollectorValueProvider(value, collector, typeof(IAsyncCollector<TItem>));

                return Task.FromResult(provider);
            }
        }
    }
 }
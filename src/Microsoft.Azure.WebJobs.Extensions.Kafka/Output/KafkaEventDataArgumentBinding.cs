// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Bindings;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaEventDataArgumentBinding : IArgumentBinding<KafkaProducerEntity>
    {
        public Type ValueType
        {
            get;
            private set;
        }      

        public Task<IValueProvider> BindAsync(KafkaProducerEntity value, ValueBindingContext context)
        {            
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            this.ValueType = typeof(KafkaEventData<,>).MakeGenericType(value.KeyType, value.ValueType);

            var provider = (IValueProvider)Activator.CreateInstance(typeof(KafkaEventDataValueBinder<>).MakeGenericType(this.ValueType), value, context.FunctionInstanceId);
            return Task.FromResult(provider);
        }

        private class KafkaEventDataValueBinder<TItem> : IOrderedValueBinder
        {
            private readonly KafkaProducerEntity entity;
            private readonly Guid functionInstanceId;

            public KafkaEventDataValueBinder(KafkaProducerEntity entity, Guid functionInstanceId)
            {
                this.entity = entity;
                this.functionInstanceId = functionInstanceId;
            }

            public BindStepOrder StepOrder
            {
                get { return BindStepOrder.Enqueue; }
            }

            public Type Type
            {
                get { return typeof(TItem); }
            }

            public Task<object> GetValueAsync()
            {
                return Task.FromResult<object>(null);
            }

            public string ToInvokeString()
            {
                return entity.Topic;
            }

            /// <summary>
            /// Send the message to Kafka producer
            /// </summary>
            public async Task SetValueAsync(object value, CancellationToken cancellationToken)
            {
                if (value == null)
                {
                    return;
                }

                await entity.SendAndCreateEntityIfNotExistsAsync(value, functionInstanceId, cancellationToken);
            }
        }
    }
}
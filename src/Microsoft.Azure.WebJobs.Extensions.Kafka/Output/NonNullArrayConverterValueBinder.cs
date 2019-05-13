// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class NonNullArrayConverterValueBinder<T> : IOrderedValueBinder
    {
        private KafkaProducerEntity entity;
        private IConverter<T, IKafkaEventData[]> converter;
        private Guid functionInstanceId;

        public NonNullArrayConverterValueBinder(KafkaProducerEntity entity, IConverter<T, IKafkaEventData[]> converter, Guid functionInstanceId)
        {
            this.entity = entity;
            this.converter = converter;
            this.functionInstanceId = functionInstanceId;
        }



        public BindStepOrder StepOrder
        {
            get { return BindStepOrder.Enqueue; }
        }

        public Type Type
        {
            get { return typeof(T); }
        }

        public Task<object> GetValueAsync()
        {
            return Task.FromResult<object>(null);
        }

        public string ToInvokeString()
        {
            return entity.Topic;
        }

        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            if (value == null)
            {
                return Task.FromResult(0);
            }

            Debug.Assert(value is T);
            var messages = converter.Convert((T)value);
            Debug.Assert(messages != null);

            return entity.SendAndCreateEntityIfNotExistsAsync(messages, functionInstanceId, cancellationToken);
        }
    }
}
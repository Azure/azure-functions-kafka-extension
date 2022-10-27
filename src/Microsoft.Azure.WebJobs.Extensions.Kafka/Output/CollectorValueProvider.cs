// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class CollectorValueProvider : IValueBinder
    {
        private readonly KafkaProducerEntity entity;
        private readonly object value;
        private readonly Type valueType;

        public CollectorValueProvider(KafkaProducerEntity entity, object value, Type valueType)
        {
            if (value != null && !valueType.IsAssignableFrom(value.GetType()))
            {
                throw new InvalidOperationException("value is not of the correct type.");
            }

            this.entity = entity;
            this.value = value;
            this.valueType = valueType;
        }

        public Type Type
        {
            get { return valueType; }
        }

        public Task<object> GetValueAsync()
        {
            return Task.FromResult(value);
        }

        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            return ((KafkaProducerAsyncCollector<string>)this.value).FlushAsync();
        }

        public string ToInvokeString()
        {
            return this.entity.Topic;
        }
    }
}
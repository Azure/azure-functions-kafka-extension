// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaProducerAsyncCollector<T> : IAsyncCollector<T>
    {
        private readonly KafkaProducerEntity entity;
        private readonly Guid functionInstanceId;

        public KafkaProducerAsyncCollector(KafkaProducerEntity entity, Guid functionInstanceId)
        {
            if (entity == null)
            {
                throw new ArgumentNullException("entity");
            }

            this.entity = entity;
            this.functionInstanceId = functionInstanceId;
        }

        public Task AddAsync(T item, CancellationToken cancellationToken)
        {            
            if (item == null)
            {
                throw new InvalidOperationException("Cannot produce a null message instance.");
            }

            object messageToSend = item;

            if (item.GetType() == typeof(string) || item.GetType() == typeof(byte[]))
            {
                messageToSend = new KafkaEventData<T>(item);
            }

            return entity.SendAndCreateEntityIfNotExistsAsync(messageToSend, functionInstanceId, cancellationToken);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Batching not supported. 
            return Task.FromResult(0);
        }
    }
}
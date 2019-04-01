// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public interface IKafkaProducer
    {
        /// <summary>
        /// Produces a Kafka message
        /// The message is only sent once <see cref="Flush"/> is called
        /// </summary>
        void Produce(string topic, KafkaEventData item);

        /// <summary>
        /// Flushes the pending items to Kafka
        /// </summary>
        void Flush(CancellationToken cancellationToken);
    }
}
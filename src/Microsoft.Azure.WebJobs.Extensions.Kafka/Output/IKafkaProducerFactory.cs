// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Provider for <see cref="IKafkaProducer"/>
    /// </summary>
    public interface IKafkaProducerFactory
    {
        IKafkaProducer Create(KafkaProducerEntity entity);
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public interface IKafkaProducer : IDisposable
    {
        /// <summary>
        /// Produces a Kafka message
        /// </summary>
        Task ProduceAsync(string topic, object item);
    }
}
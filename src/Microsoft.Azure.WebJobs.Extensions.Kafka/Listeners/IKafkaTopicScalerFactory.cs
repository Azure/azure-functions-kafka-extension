// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    public interface IKafkaTopicScalerFactory
    {
        KafkaTopicScaler<TKey, TValue> CreateKafkaTopicScaler<TKey, TValue>(string topic, string consumerGroup, string functionId, IConsumer<TKey, TValue> consumer, AdminClientConfig adminClientConfig, ILogger logger);
    }
}
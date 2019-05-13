// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    internal class TestConsumerBuilder<TKey, TValue> : ConsumerBuilder<TKey, TValue>
    {
        private readonly IConsumer<TKey, TValue> consumer;

        public TestConsumerBuilder(ConsumerConfig config, IConsumer<TKey, TValue> consumer) : base(config)
        {
            this.consumer = consumer;
        }

        public override IConsumer<TKey, TValue> Build() => this.consumer;
    }
}
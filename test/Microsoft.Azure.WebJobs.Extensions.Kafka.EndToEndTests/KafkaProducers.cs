// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    internal static class KafkaProducers
    {
        internal static string CreateMessageValue(string prefix, int id) => string.Concat(prefix, id.ToString("00000000000000000000"));

        internal static async Task ProduceStringsAsync(string brokerList, string topic, IEnumerable values, TimeSpan? interval = null)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = brokerList,

            };

            using (var producer = new ProducerBuilder<Null, string>(config).Build())
            {
                foreach (var value in values)
                {
                    var msg = new Message<Null, string>()
                    {
                        Value = value.ToString(),
                    };

                    await producer.ProduceAsync(topic, msg);

                    if (interval.HasValue)
                    {
                        await Task.Delay(interval.Value);
                    }
                }
            }
        }
    }
}
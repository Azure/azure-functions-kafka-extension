// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Converts <see cref="IKafkaEventData"/> instances to Kafka Message instances 
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class KafkaMessageBuilder<TKey, TValue>
    {
        public Message<TKey, TValue> BuildFrom(IKafkaEventData eventData)
        {
            var msg = new Message<TKey, TValue>()
            {
                Value = (TValue)eventData.Value,
            };

            if (eventData.Key != null)
            {
                if (!(eventData.Key is TKey keyValue))
                {
                    throw new ArgumentException($"Key value is not of the expected type. Expected: {typeof(TKey).Name}. Actual: {eventData.Key.GetType().Name}");
                }

                msg.Key = keyValue;
            }

            if (eventData is IKafkaEventDataWithHeaders withHeaders && withHeaders.Headers?.Count > 0)
            {
                msg.Headers = new Headers();
                foreach (var header in withHeaders.Headers)
                {
                    msg.Headers.Add(header.Key, header.Value);
                }
            }

            return msg;
        }
    }
}
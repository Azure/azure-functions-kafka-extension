// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Avro;
using Avro.Generic;
using Confluent.Kafka;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;

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
                try
                {
                    if (eventData.Key is TKey keyValue)
                    {
                        msg.Key = keyValue;
                    }
                    else
                    {
                        // this case is possible in out of proc when KafkaMessageKeyType is set specifically
                        msg.Key = typeof(TKey) switch
                        {
                            // byte[], string are added as data types supported by KafkaMessageKeyType enum
                            Type t when t == typeof(byte[]) => (TKey)(object)System.Text.Encoding.UTF8.GetBytes(eventData.Key.ToString()),
                            Type t when t == typeof(string) => (TKey)(object)eventData.Key.ToString(),
                            _ => (TKey)eventData.Key
                        };
                    }
                }
                catch
                {
                    throw new ArgumentException($"Could not cast actual key value to the expected type. Expected: {typeof(TKey).Name}. Actual: {eventData.Key.GetType().Name}");
                }
            }

            if (eventData.Headers?.Count > 0)
            {
                msg.Headers = new Headers();
                foreach (var header in eventData.Headers)
                {
                    msg.Headers.Add(header.Key, header.Value);
                }
            }

            return msg;
        }
    }
}
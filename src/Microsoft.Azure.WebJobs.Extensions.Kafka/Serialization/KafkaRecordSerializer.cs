// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Serializes <see cref="IKafkaEventData"/> to a JSON byte array
    /// suitable for transmission via ModelBindingData to isolated workers.
    /// </summary>
    internal static class KafkaRecordSerializer
    {
        /// <summary>
        /// The binding source identifier used in ModelBindingData.
        /// </summary>
        public const string BindingSource = "AzureKafkaConsumeResult";

        /// <summary>
        /// The content type for the serialized data.
        /// </summary>
        public const string JsonContentType = "application/json";

        /// <summary>
        /// Serializes an <see cref="IKafkaEventData"/> to a JSON byte array.
        /// </summary>
        public static byte[] Serialize(IKafkaEventData eventData)
        {
            using (var ms = new MemoryStream())
            using (var sw = new StreamWriter(ms, new UTF8Encoding(false)))
            using (var writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();

                writer.WritePropertyName("topic");
                writer.WriteValue(eventData.Topic);

                writer.WritePropertyName("partition");
                writer.WriteValue(eventData.Partition);

                writer.WritePropertyName("offset");
                writer.WriteValue(eventData.Offset);

                writer.WritePropertyName("leaderEpoch");
                if (eventData.LeaderEpoch.HasValue)
                {
                    writer.WriteValue(eventData.LeaderEpoch.Value);
                }
                else
                {
                    writer.WriteNull();
                }

                writer.WritePropertyName("isPartitionEOF");
                writer.WriteValue(eventData.IsPartitionEOF);

                // message
                writer.WritePropertyName("message");
                writer.WriteStartObject();

                // key
                writer.WritePropertyName("key");
                WriteValueOrBase64(writer, eventData.Key);

                // value
                writer.WritePropertyName("value");
                WriteValueOrBase64(writer, eventData.Value);

                // timestamp
                writer.WritePropertyName("timestamp");
                writer.WriteStartObject();
                writer.WritePropertyName("unixTimestampMs");
                writer.WriteValue(new DateTimeOffset(eventData.Timestamp.Kind == DateTimeKind.Utc
                    ? eventData.Timestamp
                    : eventData.Timestamp.ToUniversalTime()).ToUnixTimeMilliseconds());
                writer.WritePropertyName("type");
                // Default to 0 (NotAvailable) since IKafkaEventData doesn't preserve TimestampType
                writer.WriteValue(0);
                writer.WriteEndObject();

                // headers
                writer.WritePropertyName("headers");
                writer.WriteStartArray();
                if (eventData.Headers != null)
                {
                    foreach (var header in eventData.Headers)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("key");
                        writer.WriteValue(header.Key);
                        writer.WritePropertyName("value");
                        if (header.Value != null)
                        {
                            writer.WriteValue(Convert.ToBase64String(header.Value));
                        }
                        else
                        {
                            writer.WriteNull();
                        }
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndArray();

                writer.WriteEndObject(); // end message

                writer.WriteEndObject(); // end root
                writer.Flush();
                return ms.ToArray();
            }
        }

        private static void WriteValueOrBase64(JsonTextWriter writer, object value)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else if (value is byte[] bytes)
            {
                writer.WriteValue(Convert.ToBase64String(bytes));
            }
            else if (value is string str)
            {
                writer.WriteValue(str);
            }
            else
            {
                // For complex types (Avro, Protobuf, POCOs), serialize to JSON string
                writer.WriteValue(JsonConvert.SerializeObject(value));
            }
        }
    }
}

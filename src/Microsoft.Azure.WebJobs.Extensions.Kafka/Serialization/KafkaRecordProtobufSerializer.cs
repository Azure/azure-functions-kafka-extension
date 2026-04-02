// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text;
using Google.Protobuf;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Serialization
{
    /// <summary>
    /// Serializes <see cref="IKafkaEventData"/> to a Protobuf-encoded byte array
    /// using the <see cref="KafkaRecordProto"/> schema. This format is used for
    /// ModelBindingData transport to isolated workers, avoiding Base64 overhead.
    /// </summary>
    internal static class KafkaRecordProtobufSerializer
    {
        /// <summary>
        /// The binding source identifier used in ModelBindingData.
        /// </summary>
        public const string BindingSource = "AzureKafkaRecord";

        /// <summary>
        /// The content type for the serialized data.
        /// </summary>
        public const string ContentType = "application/x-protobuf";

        /// <summary>
        /// Serializes an <see cref="IKafkaEventData"/> to a Protobuf byte array.
        /// </summary>
        public static byte[] Serialize(IKafkaEventData eventData)
        {
            var proto = new KafkaRecordProto
            {
                Topic = eventData.Topic ?? string.Empty,
                Partition = eventData.Partition,
                Offset = eventData.Offset,
                Timestamp = new KafkaTimestampProto
                {
                    UnixTimestampMs = new DateTimeOffset(
                        eventData.Timestamp.Kind == DateTimeKind.Utc
                            ? eventData.Timestamp
                            : eventData.Timestamp.ToUniversalTime()).ToUnixTimeMilliseconds(),
                    Type = 0, // NotAvailable — IKafkaEventData doesn't preserve TimestampType
                },
            };

            if (eventData.Key != null)
            {
                proto.Key = ToByteString(eventData.Key);
            }

            if (eventData.Value != null)
            {
                proto.Value = ToByteString(eventData.Value);
            }

            if (eventData.LeaderEpoch.HasValue)
            {
                proto.LeaderEpoch = eventData.LeaderEpoch.Value;
            }

            if (eventData.Headers != null)
            {
                foreach (var header in eventData.Headers)
                {
                    var headerProto = new KafkaHeaderProto
                    {
                        Key = header.Key ?? string.Empty,
                    };
                    if (header.Value != null)
                    {
                        headerProto.Value = ByteString.CopyFrom(header.Value);
                    }

                    proto.Headers.Add(headerProto);
                }
            }

            return proto.ToByteArray();
        }

        private static ByteString ToByteString(object value)
        {
            if (value == null)
            {
                return ByteString.Empty;
            }

            if (value is byte[] bytes)
            {
                return ByteString.CopyFrom(bytes);
            }

            if (value is string str)
            {
                return ByteString.CopyFrom(str, Encoding.UTF8);
            }

            // For complex types (Avro, Protobuf, POCOs), serialize to JSON bytes
            var json = JsonConvert.SerializeObject(value);
            return ByteString.CopyFrom(json, Encoding.UTF8);
        }
    }
}

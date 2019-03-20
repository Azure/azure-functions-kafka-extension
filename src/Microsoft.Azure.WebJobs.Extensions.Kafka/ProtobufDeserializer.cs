// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Confluent.Kafka;
using Google.Protobuf;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Protobuf deserializer.
    /// </summary>
    public class ProtobufDeserializer<T> : IDeserializer<T> where T : IMessage<T>, new()
    {
        private readonly MessageParser<T> parser;

        public ProtobufDeserializer()
        {
            parser = new MessageParser<T>(() => new T());
        }

        public T Deserialize(ReadOnlySpan<byte> data, bool isNull, bool isKey, MessageMetadata messageMetadata, TopicPartition source)
            => parser.ParseFrom(data.ToArray());
    }
}
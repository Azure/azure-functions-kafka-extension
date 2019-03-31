// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Confluent.Kafka;
using Google.Protobuf;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Protobuf serializer
    /// </summary>
    public class ProtobufSerializer<T> : ISerializer<T> where T : IMessage<T>, new()
    {
        public byte[] Serialize(T data, SerializationContext context)
            => data.ToByteArray();
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaEventDataHeader : IKafkaEventDataHeader
    {
        

        public KafkaEventDataHeader(string key, byte[] value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public byte[] Value { get; }
    }
}
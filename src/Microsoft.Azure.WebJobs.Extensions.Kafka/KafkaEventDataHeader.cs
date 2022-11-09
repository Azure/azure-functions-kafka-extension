// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class KafkaEventDataHeader : IKafkaEventDataHeader
    {
        

        public KafkaEventDataHeader(string key, byte[] value)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value;
            Console.WriteLine("Test");
        }

        public string Key { get; }
        public byte[] Value { get; }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class ByteArrayToKafkaEventDataConverter : IConverter<byte[], IKafkaEventData>
    {
        public IKafkaEventData Convert(byte[] input)
        {
            return new KafkaEventData<byte[]>()
            {
                Value = input,
            };
        }
    }
}
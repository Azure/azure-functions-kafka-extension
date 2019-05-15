// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal class StringToKafkaEventDataConverter : IConverter<string, IKafkaEventData>
    {
        public IKafkaEventData Convert(string input)
        {
            return new KafkaEventData<string>()
            {
                Value = input,
            };
        }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal static class KafkaMessageKeyTypeExtensions
    {
        /// <summary>
        /// Converts the KafkaMessageKeyType enum to the corresponding System.Type.
        /// </summary>
        /// <param name="dataType">The KafkaMessageKeyType value in enum.</param>
        /// <returns>The corresponding System.Type for the KafkaMessageKeyType.</returns>
        internal static Type GetDataType(this KafkaMessageKeyType dataType)
        {
            switch (dataType)
            {
                case KafkaMessageKeyType.Int:
                    return typeof(int);
                case KafkaMessageKeyType.Long:
                    return typeof(long);
                case KafkaMessageKeyType.String:
                    return typeof(string);
                case KafkaMessageKeyType.Binary:
                    return typeof(byte[]);
                default:
                    throw new InvalidOperationException($"Unsupported KafkaMessageKeyType: {dataType}");
            }
        }
    }
}

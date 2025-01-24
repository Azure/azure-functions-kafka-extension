// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Extensions
{
    internal static class KafkaMessageKeyDataTypeExtensions
    {
        /// <summary>
        /// Converts the KafkaMessageKeyDataType enum to the corresponding System.Type.
        /// </summary>
        /// <param name="keyDataType">The KafkaMessageKeyDataType value.</param>
        /// <returns>The corresponding System.Type for the KeyDataType.</returns>
        internal static Type GetKeyDataType(this KafkaMessageKeyDataType keyDataType)
        {
            switch (keyDataType)
            {
                case KafkaMessageKeyDataType.String:
                    return typeof(string);
                case KafkaMessageKeyDataType.Binary:
                    return typeof(byte[]);
                default:
                    throw new InvalidOperationException($"Unsupported KeyDataType: {keyDataType}");
            }
        }
    }
}

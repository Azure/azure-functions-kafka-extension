// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal static class KafkaDataTypeExtensions
    {
        /// <summary>
        /// Converts the KafkaDataType enum to the corresponding System.Type.
        /// </summary>
        /// <param name="dataType">The KafkaDataType value in enum.</param>
        /// <returns>The corresponding System.Type for the KafkaDataType.</returns>
        internal static Type GetDataType(this KafkaDataType dataType)
        {
            switch (dataType)
            {
                case KafkaDataType.Int:
                    return typeof(int);
                case KafkaDataType.Long:
                    return typeof(long);
                case KafkaDataType.String:
                    return typeof(string);
                case KafkaDataType.Float:
                    return typeof(float);
                case KafkaDataType.Double:
                    return typeof(double);
                case KafkaDataType.Binary:
                    return typeof(byte[]);
                default:
                    throw new InvalidOperationException($"Unsupported KafkaDataType: {dataType}");
            }
        }
    }
}

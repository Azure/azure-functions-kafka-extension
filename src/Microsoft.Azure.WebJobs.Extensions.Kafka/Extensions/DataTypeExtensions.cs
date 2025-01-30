// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal static class DataTypeExtensions
    {
        /// <summary>
        /// Converts the DataType enum to the corresponding System.Type.
        /// </summary>
        /// <param name="dataType">The dataType value in enum.</param>
        /// <returns>The corresponding System.Type for the DataType.</returns>
        internal static Type GetDataType(this DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Int:
                    return typeof(int);
                case DataType.Long:
                    return typeof(long);
                case DataType.String:
                    return typeof(string);
                case DataType.Float:
                    return typeof(float);
                case DataType.Double:
                    return typeof(double);
                case DataType.Binary:
                    return typeof(byte[]);
                default:
                    throw new InvalidOperationException($"Unsupported KeyDataType: {dataType}");
            }
        }
    }
}

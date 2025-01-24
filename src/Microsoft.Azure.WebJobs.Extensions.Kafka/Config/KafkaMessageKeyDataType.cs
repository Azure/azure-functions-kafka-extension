// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Defines the data type for message key as set.
    /// </summary>
    public enum KafkaMessageKeyDataType
    {
        Int = 0,
        Long,
        String,
        Float,
        Double,
        Binary
    }
}

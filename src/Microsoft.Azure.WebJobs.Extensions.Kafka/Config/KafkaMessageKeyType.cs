﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Defines the data type used in kafka extension as enum.
    /// </summary>
    public enum KafkaMessageKeyType
    {
        Int = 0,
        Long,
        String,
        Binary
    }
}

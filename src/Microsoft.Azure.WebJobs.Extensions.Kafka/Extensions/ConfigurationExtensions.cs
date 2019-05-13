// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal static class ConfigurationExtensions
    {
        internal static string ResolveSecureSetting(this IConfiguration config, INameResolver nameResolver, string currentValue)
        {
            if (string.IsNullOrWhiteSpace(currentValue))
            {
                return currentValue;
            }

            var resolved = nameResolver.ResolveWholeString(currentValue);
            var resolvedFromConfig = config.GetConnectionStringOrSetting(resolved);
            return !string.IsNullOrEmpty(resolvedFromConfig) ? resolvedFromConfig : resolved;
        }
    }
}

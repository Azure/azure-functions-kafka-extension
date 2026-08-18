// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Confluent.Kafka;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    internal static class ConfigurationExtensions
    {
        internal const string HttpsCaLocationConfigKey = "https.ca.location";
        internal const string HttpsCaPemConfigKey = "https.ca.pem";

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

        internal static string NormalizePem(string value)
        {
            return string.IsNullOrEmpty(value) ? value : value.Replace("\\n", "\n");
        }

        internal static void SetOptionalConfigValue(this ClientConfig config, string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                config.Set(key, value);
            }
        }

        internal static void SetHttpsCaCertificate(this ClientConfig config, string location, string pem)
        {
            ValidateHttpsCaCertificate(location, pem);
            config.SetOptionalConfigValue(HttpsCaLocationConfigKey, location);
            config.SetOptionalConfigValue(HttpsCaPemConfigKey, NormalizePem(pem));
        }

        internal static void ValidateHttpsCaCertificate(string location, string pem)
        {
            if (!string.IsNullOrWhiteSpace(location) && !string.IsNullOrWhiteSpace(pem))
            {
                throw new ArgumentException($"'{HttpsCaLocationConfigKey}' and '{HttpsCaPemConfigKey}' are mutually exclusive.");
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Config
{
    public static class PEMExtractor
    {
        private static string ExtractSection(string pemString, string sectionName)
        {
            if (!string.IsNullOrEmpty(pemString))
            {
                var regex = new Regex($"-----BEGIN {sectionName}-----(.*?)-----END {sectionName}-----", RegexOptions.Singleline);
                var match = regex.Match(pemString);
                if (match.Success)
                {
                    return match.Value.Replace("\\n", "\n");
                }
            }
            return null;
        }

        private static string ExtractAllSections(string pemString, string sectionName)
        {
            if (string.IsNullOrEmpty(pemString))
            {
                return null;
            }

            var regex = new Regex($"-----BEGIN {sectionName}-----(.*?)-----END {sectionName}-----", RegexOptions.Singleline);
            var matches = regex.Matches(pemString);

            if (matches.Count == 0)
            {
                return null;
            }

            var sections = new List<string>(matches.Count);
            foreach (Match match in matches)
            {
                sections.Add(match.Value.Replace("\\n", "\n"));
            }

            return string.Join(Environment.NewLine, sections);
        }

        public static string ExtractCertificate(string pemString)
        {
            return ExtractSection(pemString, "CERTIFICATE");
        }

        public static string ExtractAllCertificates(string pemString)
        {
            return ExtractAllSections(pemString, "CERTIFICATE");
        }

        public static string ExtractPrivateKey(string pemString)
        {
            return ExtractSection(pemString, "PRIVATE KEY");
        }
    }
}

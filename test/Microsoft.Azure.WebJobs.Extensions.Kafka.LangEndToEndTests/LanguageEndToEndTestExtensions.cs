// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests
{
    internal static class LanguageEndToEndTestExtensions
    {
        public static KafkaEventData<string> ToKafkaEventData(this string s)
        {
            return JsonConvert.DeserializeObject<KafkaEventData<string>>(s);
        }
    }
}

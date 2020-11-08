using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests
{
    internal static class LanguageEndToEndTestExtensions
    {
        public static KafkaEventData<string> KafkaEventData(this string s)
        {
            return JsonConvert.DeserializeObject<KafkaEventData<string>>(s);
        }
    }
}

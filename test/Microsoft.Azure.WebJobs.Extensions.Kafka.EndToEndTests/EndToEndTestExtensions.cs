using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    public static class EndToEndTestExtensions
    {
        public static JobHost GetJobHost(this IHost host) => (JobHost)host.Services.GetRequiredService<IJobHost>();

        /// <summary>
        /// Calls the output trigger string
        /// </summary>
        public static async Task CallOutputTriggerStringAsync(this JobHost jobHost, MethodInfo method, string topic, IEnumerable<object> values)
        {
            var allValues = values.Select(x => x.ToString());
            await jobHost.CallAsync(method, new { topic = topic, content = allValues });
        }

        /// <summary>
        /// Calls the output trigger string
        /// </summary>
        public static async Task CallOutputTriggerStringWithKeyAsync(this JobHost jobHost, MethodInfo method, string topic, IEnumerable<object> values, IEnumerable<string> keys, TimeSpan? interval = null)
        {
            object keysValue = keys;
            var keysParameter = method.GetParameters().First(x => x.Name == "keys");
            if (keysParameter.ParameterType == typeof(IEnumerable<long>))
            {
                keysValue = keys.Select(x => long.Parse(x));
            }

            var allValues = values.Select(x => x.ToString());
            await jobHost.CallAsync(method, new { topic = topic, content = allValues, keys = keysValue });
        }

        internal static string CreateMessageValue(string prefix, int id) => string.Concat(prefix, id.ToString("00000000000000000000"));
    }
}

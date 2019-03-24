using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace SampleHost
{
    class Program
    {
        const string Broker = "localhost:9092";
        const string StringTopicWithOnePartition = "stringTopicOnePartition";
        const string StringTopicWithTenPartitions = "stringTopicTenPartitions";

        static async Task Main(string[] args)
        {
            IHost host = null;
            try
            {
                host = await StartHostAsync();
                host.WaitForShutdown();
                host.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }            
        }

        static async Task<IHost> StartHostAsync()
        {
            ExplicitTypeLocator locator = new ExplicitTypeLocator(typeof(MultiItemTriggerTenPartitions));

            IHost host = new HostBuilder()
                .ConfigureWebJobs(builder =>
                {
                    builder
                    .AddAzureStorage()
                    .AddKafka();
                })
                .ConfigureAppConfiguration(c =>
                {
                    //c.AddTestSettings();
                    //c.AddJsonFile("appsettings.tests.json", optional: false);
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<ITypeLocator>(locator);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole((o) =>
                    {                        
                    });
                })
                .Build();

            await host.StartAsync();
            return host;
        }

        private static class MultiItemTriggerTenPartitions
        {
            public static void Trigger(
                [KafkaTrigger(Broker, StringTopicWithTenPartitions, ConsumerGroup = "EndToEndTestClass-Trigger")] KafkaEventData[] events,
                ILogger log)
            {
                foreach (var kafkaEvent in events)
                {
                    log.LogInformation(kafkaEvent.Value.ToString());
                }
            }
        }
    }
}

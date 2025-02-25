using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace ConsoleProducer
{
    public class StringTopicProducer : ITopicProducer
    {
        private readonly string brokerList;
        private readonly string topicName;
        private readonly int totalMessages;

        public StringTopicProducer(string brokerList, string topicName, int totalMessages)
        {
            this.brokerList = brokerList;
            this.topicName = topicName;
            this.totalMessages = totalMessages;
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = brokerList,

            };

            using (var producer = new ProducerBuilder<Null, string>(config).Build())
            {
                for (int i = 1; i <= this.totalMessages && !cancellationToken.IsCancellationRequested; ++i)
                {
                    var msg = new Message<Null, string>()
                    {
                        Value = DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss.ffff"),
                    };

                    var deliveryReport = await producer.ProduceAsync(this.topicName, msg);
                    Console.WriteLine($"Message {i} sent (value: '{msg.Value}'), offset: {deliveryReport.Offset}, partition: {deliveryReport.Partition}");
                }
            }
        }
    }
}

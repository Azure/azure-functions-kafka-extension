using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace ConsoleProducer
{

    public class ProtobufTopicProducer : ITopicProducer
    {
        private readonly string brokerList;
        private readonly string topicName;
        private readonly int totalMessages;

        public ProtobufTopicProducer(string brokerList, string topicName, int totalMessages)
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


            var builder = new ProducerBuilder<string, User>(config)
                .SetValueSerializer(new ProtobufSerializer<User>());

            using (var producer = builder.Build())
            {
                for (int i = 1; i <= this.totalMessages && !cancellationToken.IsCancellationRequested; ++i)
                {
                    var name = GetName();
                    var msg = new Message<string, User>()
                    {
                        Key = name,
                        Value = new User()
                        {
                            FavoriteColor = "Green",
                            FavoriteNumber = randomizer.Next(),
                            Name = name,
                        }
                    };

                    var deliveryReport = await producer.ProduceAsync(this.topicName, msg);
                    Console.WriteLine($"Message {i} sent (value: '{msg.Value}'), offset: {deliveryReport.Offset}, partition: {deliveryReport.Partition}");
                }
            }
        }

        Random randomizer = new Random();

        // Names generated from http://listofrandomnames.com/index.cfm?textarea
        static string[] randomNames = new[]
        {
            "Newton",
            "Magaret",
            "Mana",
            "Francine",
            "Eulah",
            "Arletta",
            "Clyde",
            "Soon",
            "Thalia",
            "Tiffany",
            "Yer",
            "Katelyn",
            "Tiffanie",
            "Ahmad",
            "Ricarda",
            "Sandee",
            "Linnie",
            "Odilia",
            "Marlena",
            "Pasty",
            "Marietta",
            "Majorie",
            "Brent",
            "Shayna",
            "Judie",
            "Ashton",
            "Houston",
            "Despina",
            "Karrie",
            "Keeley",
            "Lucien",
            "Ellen",
            "Noriko",
            "Sachiko",
            "Jerri",
            "Mathilde",
            "America",
            "Gwenda",
            "Micha",
            "Magdalen",
            "Quyen",
            "Chi",
            "Bryon",
            "Suanne",
            "Cynthia",
            "Benedict",
            "Elinor",
            "Trang",
            "Kizzy",
            "Otelia",
        };

        private string GetName()
        {
            return randomNames[randomizer.Next(randomNames.Length)];
        }
    }
}

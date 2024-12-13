using System;
using System.Threading.Tasks;

namespace ConsoleProducer
{
    class Program
    {
        public static async Task Main(string[] args)
        {

            try
            {
                ITopicProducer producer;
                var brokerList = "localhost:9092";
                // brokerList = "broker:9092";

                producer = new StringTopicProducer(brokerList, "stringTopicTenPartitions", 100);
                // producer = new ProtobufTopicProducer(brokerList, "protoUser", 100);
                await producer.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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

                producer = new StringTopicProducer("localhost:9092", "stringTopic", 100);
                // producer = new ProtobufTopicProducer("broker:9092", "protoUser", 100);
                await producer.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
using Confluent.Kafka;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests
{
    public class KafkaLanguageEndToEndTests : IClassFixture<KafkaLanguageEndToEndFixture>
    {
      
        private readonly KafkaLanguageEndToEndFixture endToEndTestFixture;

        public KafkaLanguageEndToEndTests(KafkaLanguageEndToEndFixture endToEndTestFixture)
        {
            this.endToEndTestFixture = endToEndTestFixture;
        }

        [Fact]
        public async Task Java8_Smoke_Test_For_Output_And_SingleTrigger()
        {
            var random = new Random();
            string inputMessage = $"hello.{random.Next(0, 9999)}";

            var consumer = endToEndTestFixture.ConsumerFactory("java8");
            consumer.Subscribe("java8result");

            var response = await endToEndTestFixture.HttpClient.GetAsync($"http://localhost:7071/api/HttpTriggerAndKafkaOutput?message={inputMessage}");
            Assert.True(response.IsSuccessStatusCode);

            var result = consumer.Consume(10 * 1000);

            Assert.Equal(inputMessage, result.Message.Value.ToKafkaEventData().Value);
        }

        [Fact]
        public async Task Python38_Smoke_Test_For_Output_And_SingleTrigger()
        {
            string inputMessage = $"hello:{DateTime.UtcNow}";
            var consumer = endToEndTestFixture.ConsumerFactory("python38");
            consumer.Subscribe("python38result");

            var response = await endToEndTestFixture.HttpClient.PostAsync($"http://localhost:7072/api/kafka_output", new StringContent(inputMessage));
            Assert.True(response.IsSuccessStatusCode);

            var result = consumer.Consume(10 * 1000);
            Assert.Equal(inputMessage, result.Message.Value.ToKafkaEventData().Value);
        }
    }
}

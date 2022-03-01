using Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Interfaces;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Managers
{
    public abstract class BaseKafkaCloudManager : IManager
    {
        public abstract void DisposeAsync();
    }
}

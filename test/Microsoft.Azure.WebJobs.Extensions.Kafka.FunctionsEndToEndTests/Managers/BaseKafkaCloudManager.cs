using Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Interfaces;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Managers
{
    /// <summary>
    /// Abstract implementation of <see cref="IManager"/> for managing connections to various Kafka clouds used in E2E tests
    /// </summary>
    public abstract class BaseKafkaCloudManager : IManager
    {
        public abstract void DisposeAsync();
    }
}

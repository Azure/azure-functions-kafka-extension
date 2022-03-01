using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Managers;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Fixtures
{
    public abstract class BaseFixture : IAsyncLifetime
    {
        public abstract string LanguageType { get; }

        public HttpClientManager? HttpClientManager;

        public Task DisposeAsync()
        {
            // TODO: sample code, implement actual code
            this.HttpClientManager?.DisposeAsync();
            return Task.CompletedTask;
        }

        public Task InitializeAsync()
        {
            // TODO: sample code, implement actual code
            this.HttpClientManager = new HttpClientManager();
            return Task.CompletedTask;
        }
    }
}

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Managers;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Fixtures
{
    /// <summary>
    /// Base fixture used across test suites for initializing and disposing shared test setup
    /// </summary>
    public abstract class BaseFixture : IAsyncLifetime
    {
        public abstract string LanguageType { get; }

        public HttpClientManager? HttpClientManager;

        /// <summary>
        /// Initialize required manager instances and shared test setup before test execution begins
        /// </summary>
        /// <returns></returns>
        public Task InitializeAsync()
        {
            // TODO: sample code, implement actual code
            this.HttpClientManager = new HttpClientManager();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Dispose all initialized manager instances as necessary after test execution completes
        /// </summary>
        /// <returns></returns>
        public Task DisposeAsync()
        {
            // TODO: sample code, implement actual code
            this.HttpClientManager?.DisposeAsync();
            return Task.CompletedTask;
        }
    }
}

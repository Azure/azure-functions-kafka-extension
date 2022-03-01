using System.Net.Http;
using Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Interfaces;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Managers
{
    /// <summary>
    /// Implementation of <see cref="IManager"/> for managing REST API calls used in E2E tests
    /// </summary>
    public class HttpClientManager : IManager
    {
        // TODO: sample code, implement actual code
        private readonly HttpClient? httpClient;

        public HttpClientManager()
        {
            httpClient = new HttpClient();
        }

        public void Post(string URL, string body) 
        {
        }

        public void DisposeAsync()
        {
            httpClient?.Dispose();
        }
    }
}

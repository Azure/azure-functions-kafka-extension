namespace Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Interfaces
{
    /// <summary>
    /// IManager interface to be implemented by each Manager. A Manager instance is responsible to connect to and
    /// manage any external dependency required by E2E tests and provide shared setup/context across test suite
    /// </summary>
    public interface IManager
    {
        /// <summary>
        /// Dispose any resources initialized during the lifetime of a Manager instance.
        /// This method should be called by <see cref="Fixtures.BaseFixture.DisposeAsync"/> 
        /// when all the test executions complete.
        /// </summary>
        public void DisposeAsync();
    }
}

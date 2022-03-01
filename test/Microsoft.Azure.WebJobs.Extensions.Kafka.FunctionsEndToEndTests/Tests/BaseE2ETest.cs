using Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Fixtures;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Tests
{
    public abstract class BaseE2ETest
    {
        public BaseFixture TestFixture;

        public abstract string LanguageType { get; }

        public BaseE2ETest(BaseFixture testFixture)
        {
            this.TestFixture = testFixture;
        }

        // TODO: sample code, implement actual code common across all tests
        public void TestSingleEventHub()
        {
        }

        public void TestMultiEventHub()
        {
        }

        public void TestSingleConfluent()
        {
        }

        public void TestMultiConfluent()
        {
        }
    }
}

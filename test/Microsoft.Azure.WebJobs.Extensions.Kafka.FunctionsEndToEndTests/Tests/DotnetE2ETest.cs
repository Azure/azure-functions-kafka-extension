using Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Fixtures;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Tests
{
    public class DotnetE2ETest : BaseE2ETest, IClassFixture<DotnetFixture>
    {
        public override string LanguageType { get => "dotnet"; }

        public DotnetE2ETest(DotnetFixture testFixture)
            : base(testFixture)
        {
        }

        // TODO: sample code, implement actual code
        [Fact]
        public void DotnetE2ETestSingleEventHub()
        {
            TestSingleEventHub();
        }

        [Fact]
        public void DotnetE2ETestMultiEventHub()
        {
            TestMultiEventHub();
        }

        [Fact]
        public void DotnetE2ETestSingleConfluent()
        {
            TestSingleConfluent();
        }

        [Fact]
        public void DotnetE2ETestMultiConfluent()
        {
            TestMultiConfluent();
        }
    }
}

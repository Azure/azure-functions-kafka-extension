using Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Fixtures;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Tests
{
    /// <summary>
    /// Implementation of <see cref="BaseE2ETest"/> for Java language E2E tests.
    /// </summary>
    public class JavaE2ETest : BaseE2ETest, IClassFixture<JavaFixture>
    {
        public override string LanguageType { get => "java"; }

        public JavaE2ETest(JavaFixture testFixture) 
            : base(testFixture)
        {
        }

        // TODO: sample code, implement actual code
        [Fact]
        public void JavaE2ETestSingleEventHub()
        {
            TestSingleEventHub();
        }

        [Fact]
        public void JavaE2ETestMultiEventHub()
        {
            TestMultiEventHub();
        }

        [Fact]
        public void JavaE2ETestSingleConfluent()
        {
            TestSingleConfluent();
        }

        [Fact]
        public void JavaE2ETestMultiConfluent()
        {
            TestMultiConfluent();
        }
    }
}

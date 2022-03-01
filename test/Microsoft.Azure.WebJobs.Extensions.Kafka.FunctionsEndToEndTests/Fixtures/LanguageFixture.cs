namespace Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Fixtures
{
    public class JavaFixture : BaseFixture
    {
        public override string LanguageType { get => "java"; }
    }

    public class DotnetFixture : BaseFixture
    {
        public override string LanguageType { get => "dotnet"; }
    }
}

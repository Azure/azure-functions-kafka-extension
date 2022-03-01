namespace Microsoft.Azure.WebJobs.Extensions.Kafka.FunctionsEndToEndTests.Fixtures
{
    /// <summary>
    /// Java language fixture used by Java E2E test
    /// </summary>
    public class JavaFixture : BaseFixture
    {
        public override string LanguageType { get => "java"; }
    }

    /// <summary>
    /// .NET language fixture used by .NET E2E test
    /// </summary>
    public class DotnetFixture : BaseFixture
    {
        public override string LanguageType { get => "dotnet"; }
    }
}

using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests
{
    internal sealed class IgnoreOnDisableEventHubsTestsFlagFact : FactAttribute
    {
        public IgnoreOnDisableEventHubsTestsFlagFact()
        {
            if (IgnoreOnDisableEventHubsFlag())
            {
                Skip = "Skipping EventHubs tests as DisableEventHubsTestsFlag is provided";
            }
        }

        private static bool IgnoreOnDisableEventHubsFlag() =>
            Environment.GetEnvironmentVariable("DisableEventHubsTestsFlag") == "true";
    }
}

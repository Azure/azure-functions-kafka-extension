using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Tests
{
    internal sealed class IgnoreOnDisableConfluentTestsFlagFact : FactAttribute
    {
        public IgnoreOnDisableConfluentTestsFlagFact()
        {
            if (IgnoreOnDisableConfluentFlag())
            {
                Skip = "Skipping Confluent tests as DisableConfluentTestsFlag is provided";
            }
        }

        private static bool IgnoreOnDisableConfluentFlag() =>
            Environment.GetEnvironmentVariable("DisableConfluentTestsFlag") != null;
    }
}

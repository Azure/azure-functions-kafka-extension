using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.apps.languages
{
    /* Represents the supported languages for function apps using kafka extension.
    */
    public enum Language
    {
        PYTHON,
        JAVASCRIPT,
        JAVA,
        TYPESCRIPT,
        DOTNETISOLATED,
        POWERSHELL,
		DOTNET
	}
}

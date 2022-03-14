using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.util
{
	public static class Util
	{
		public static string GetEnvVariable(string varName) { return Environment.GetEnvironmentVariable(varName); }
	
	}
}

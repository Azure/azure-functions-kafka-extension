using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.TestLogger 
{
	static class TestLogger
	{
		public static ILoggerFactory loggerFactory = new LoggerFactory();
		public static ILogger logger = GetTestLogger();

		private static ILogger GetTestLogger()
		{
			return loggerFactory.CreateLogger<ConsoleLoggerOptions>();
		}
	}

}


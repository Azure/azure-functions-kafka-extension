using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.TestLogger 
{
	/* Provides a static logger instance for logging throughout the framework.
	 * This is needed as xunit framework does not support dependency injection directly.
	*/
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


// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
		private static ILoggerFactory loggerFactory = new LoggerFactory();
		private static ILogger logger = CreateTestLogger();

		public static ILogger GetTestLogger()
		{
			return logger;
		}
		private static ILogger CreateTestLogger()
		{
			return loggerFactory.CreateLogger<ConsoleLoggerOptions>();
		}
	}

}


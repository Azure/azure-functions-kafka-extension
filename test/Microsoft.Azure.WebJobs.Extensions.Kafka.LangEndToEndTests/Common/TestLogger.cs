// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	/* Provides a static logger instance for logging throughout the framework.
	* This is needed as xunit framework does not support dependency injection directly.
	*/
	static class TestLogger
	{
		private static readonly ILoggerFactory _loggerFactory = new LoggerFactory();
		private static readonly ILogger _logger = CreateTestLogger();

		public static ILogger GetTestLogger()
		{
			return _logger;
		}
		private static ILogger CreateTestLogger()
		{
			return _loggerFactory.CreateLogger<ConsoleLoggerOptions>();
		}
	}
}


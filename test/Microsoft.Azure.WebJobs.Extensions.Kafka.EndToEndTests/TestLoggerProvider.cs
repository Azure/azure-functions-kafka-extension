// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Tests.Common;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    public class TestLoggerProvider : ILoggerProvider
    {
        private const int DefaultLogWaitTimeoutMs = 180 * 1000;
        private readonly LoggerFilterOptions _filter;
        private readonly Action<LogMessage> _logAction;
        private readonly Regex userCategoryRegex = new Regex(@"^Function\.\w+\.User$");
        private readonly Dictionary<string, TestLogger> _loggerCache = new Dictionary<string, TestLogger>();
        private readonly List<LogWaiter> _logWaiters = new List<LogWaiter>();
        private readonly object _syncLock = new object();

        public TestLoggerProvider(Action<LogMessage> logAction = null)
        {
            _filter = new LoggerFilterOptions();
            _logAction = logAction;
        }

        public IList<TestLogger> CreatedLoggers
        {
            get
            {
                lock (_syncLock)
                {
                    return _loggerCache.Values.ToList();
                }
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            lock (_syncLock)
            {
                if (!_loggerCache.TryGetValue(categoryName, out TestLogger logger))
                {
                    logger = new TestLogger(categoryName, OnLogMessage);
                    _loggerCache.Add(categoryName, logger);
                }

                return logger;
            }
        }

        public IEnumerable<LogMessage> GetAllLogMessages() => CreatedLoggers.SelectMany(l => l.GetLogMessages()).OrderBy(p => p.Timestamp);

        public IEnumerable<LogMessage> GetAllUserLogMessages()
        {
            return GetAllLogMessages().Where(p => userCategoryRegex.IsMatch(p.Category));
        }

        public string GetLogString() => string.Join(Environment.NewLine, GetAllLogMessages());

        public async Task WaitForLogMessagesAsync(Func<IReadOnlyList<LogMessage>, bool> condition, int timeout = DefaultLogWaitTimeoutMs)
        {
            if (condition == null)
            {
                throw new ArgumentNullException(nameof(condition));
            }

            LogWaiter logWaiter;
            lock (_syncLock)
            {
                if (condition(GetAllLogMessages().ToList()))
                {
                    return;
                }

                logWaiter = new LogWaiter(condition);
                _logWaiters.Add(logWaiter);
            }

            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(logWaiter.Task, timeoutTask);
            if (completedTask != logWaiter.Task)
            {
                IReadOnlyList<LogMessage> logMessages;
                lock (_syncLock)
                {
                    _logWaiters.Remove(logWaiter);
                    logMessages = GetAllLogMessages().ToList();
                }

                throw new ApplicationException(GetTimeoutMessage(timeout, logMessages));
            }

            await logWaiter.Task;
        }

        public Task WaitForLogMessageAsync(Func<LogMessage, bool> predicate, int timeout = DefaultLogWaitTimeoutMs)
        {
            return WaitForLogMessagesAsync(logMessages => logMessages.Any(predicate), timeout);
        }

        public Task WaitForUserLogMessagesAsync(Func<LogMessage, bool> predicate, int count, int timeout = DefaultLogWaitTimeoutMs)
        {
            return WaitForLogMessagesAsync(logMessages =>
                logMessages.Count(logMessage => userCategoryRegex.IsMatch(logMessage.Category) && predicate(logMessage)) >= count,
                timeout);
        }

        public void ClearAllLogMessages()
        {
            foreach (TestLogger logger in CreatedLoggers)
            {
                logger.ClearLogMessages();
            }
        }

        public void Dispose()
        {
        }

        private string GetTimeoutMessage(int timeout, IReadOnlyList<LogMessage> logMessages)
        {
            var userLogCount = logMessages.Count(logMessage => userCategoryRegex.IsMatch(logMessage.Category));
            var recentLogs = logMessages
                .TakeLast(20)
                .Select(logMessage => logMessage.ToString());

            return $"Condition not reached within {timeout}ms. Total logs: {logMessages.Count}; user logs: {userLogCount}.{Environment.NewLine}" +
                "Recent logs:" + Environment.NewLine +
                string.Join(Environment.NewLine, recentLogs);
        }

        private void OnLogMessage(LogMessage logMessage)
        {
            _logAction?.Invoke(logMessage);

            List<LogWaiter> completedLogWaiters = null;
            lock (_syncLock)
            {
                if (_logWaiters.Count == 0)
                {
                    return;
                }

                var logMessages = GetAllLogMessages().ToList();
                foreach (var logWaiter in _logWaiters.ToArray())
                {
                    if (logWaiter.Condition(logMessages))
                    {
                        completedLogWaiters ??= new List<LogWaiter>();
                        completedLogWaiters.Add(logWaiter);
                        _logWaiters.Remove(logWaiter);
                    }
                }
            }

            if (completedLogWaiters == null)
            {
                return;
            }

            foreach (var logWaiter in completedLogWaiters)
            {
                logWaiter.TrySetResult();
            }
        }

        private class LogWaiter
        {
            private readonly TaskCompletionSource<object> _completionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            public LogWaiter(Func<IReadOnlyList<LogMessage>, bool> condition)
            {
                Condition = condition;
            }

            public Func<IReadOnlyList<LogMessage>, bool> Condition { get; }

            public Task Task => _completionSource.Task;

            public void TrySetResult()
            {
                _completionSource.TrySetResult(null);
            }
        }
    }
}

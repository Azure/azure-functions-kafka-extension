// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Executor for Shell Commands
public class ShellCommandExecutor : IExecutor<IExecutableCommand<Process>, Process>
{
	public Task<Process> ExecuteAsync(IExecutableCommand<Process> request)
	{
		return request.ExecuteCommandAsync();
	}
}
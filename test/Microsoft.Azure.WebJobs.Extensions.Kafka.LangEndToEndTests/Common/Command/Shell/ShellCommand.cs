// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Representation of commands needed to be run on shell. 
public class ShellCommand : IExecutableCommand<Process>
{
	private readonly IExecutor<string, Process> processExecutor;
	protected string cmd;
	private Process process;

	protected ShellCommand()
	{
		processExecutor = new ProcessExecutor();
	}

	public async Task<Process> ExecuteCommandAsync()
	{
		process = await processExecutor.ExecuteAsync(cmd);
		return process;
	}
}
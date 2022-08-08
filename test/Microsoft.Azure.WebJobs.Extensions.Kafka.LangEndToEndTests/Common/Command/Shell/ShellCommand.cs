// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	// Representation of commands needed to be run on shell. 
	public class ShellCommand : IInfraCommand<Process>
	{
		private Process process;
		protected string cmd;
		private readonly IExecutor<string, Process> processExecutor = null;

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
}

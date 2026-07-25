// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Executes string requests/commands as Processes.
public class ProcessExecutor : IExecutor<string, Process>
{
	public async Task<Process> ExecuteAsync(string request)
	{
		if (string.IsNullOrEmpty(request))
		{
			throw new ArgumentNullException(nameof(request));
		}

		var requestProcess = CreateProcess(request);
		await Task.Run(() =>
		{
			requestProcess.Start();
			requestProcess.WaitForExit();
		});

		return requestProcess;
	}

	private Process CreateProcess(string request)
	{
		var process = new Process();

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			process.StartInfo.FileName = "cmd.exe";
			process.StartInfo.Arguments = $"/C {request}";
		}
		else
		{
			process.StartInfo.FileName = "/bin/bash";
			process.StartInfo.ArgumentList.Add("-c");
			process.StartInfo.ArgumentList.Add(request);
		}

		process.StartInfo.UseShellExecute = false;
		process.StartInfo.CreateNoWindow = false;

		return process;
	}
}
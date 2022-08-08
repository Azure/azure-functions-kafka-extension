// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

/* Responsible for keeping a list of all created processes 
* and killing them during cleanup phase 
*/
public class ProcessLifecycleManager : IDisposable
{
	private static readonly ProcessLifecycleManager instance = new();
	private readonly List<Process> processList;

	private ProcessLifecycleManager()
	{
		processList = new List<Process>();
	}

	public void Dispose()
	{
		foreach (var process in processList)
		{
			process.Kill();
		}
	}

	public static ProcessLifecycleManager GetInstance()
	{
		return instance;
	}

	public void AddProcess(Process process)
	{
		processList.Add(process);
	}
}
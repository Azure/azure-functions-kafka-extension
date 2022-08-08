// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	/* Responsible for keeping a list of all created processes 
	* and killing them during cleanup phase 
	*/
	public class ProcessLifecycleManager : IDisposable
	{
		private readonly static ProcessLifecycleManager instance = new();
		private readonly List<Process> processList;
		public static ProcessLifecycleManager GetInstance()
		{
			return instance;
		}

		public void Dispose()
		{
			foreach (Process process in processList)
			{
				process.Kill();
			}
		}

		private ProcessLifecycleManager()
		{
			processList = new List<Process>();
		}

		public void AddProcess(Process process)
		{
			processList.Add(process);
		}
	}
}

﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Common class for responses received via different Queue Types(External Resources).
public class QueueResponse
{
	public QueueResponse()
	{
		ResponseList = new List<string>();
	}

	public List<string> ResponseList { get; }

	public int GetLength()
	{
		return ResponseList.Count;
	}

	public void AddString(string input)
	{
		ResponseList.Add(input);
	}
}
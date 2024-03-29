﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Common class for requests sent different Queue Types(External Resources).
public class QueueRequest : IEnumerable<string>
{
	private readonly List<string> _requestList;

	public QueueRequest()
	{
		_requestList = new List<string>();
	}

	public IEnumerator<string> GetEnumerator()
	{
		return _requestList.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public int GetLength()
	{
		return _requestList.Count;
	}
}
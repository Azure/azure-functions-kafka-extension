// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	// Defines possible operation that can be performed on the Queue abstraction.
	public enum QueueOperation
	{
		READ, READMANY, WRITE, CREATE, DELETE, CLEAR
	}
}

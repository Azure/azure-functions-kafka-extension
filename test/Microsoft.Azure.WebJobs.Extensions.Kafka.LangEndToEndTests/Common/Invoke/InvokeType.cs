// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	// Determines the type of invocation required to trigger the function app
	public enum InvokeType
	{
		HTTP,
		Kafka
	}
}

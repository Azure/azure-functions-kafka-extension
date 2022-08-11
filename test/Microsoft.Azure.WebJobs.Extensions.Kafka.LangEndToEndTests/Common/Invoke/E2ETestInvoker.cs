// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Responsible for actual invocation of function app depending on the passed strategy.
public class E2ETestInvoker
{
	public async Task Invoke(IInvokeRequestStrategy<HttpResponseMessage> invokeStrategy)
	{
		await invokeStrategy.InvokeRequestAsync();
	}

	public async Task Invoke(IInvokeRequestStrategy<string> invokeStrategy)
	{
		await invokeStrategy.InvokeRequestAsync();
	}
}
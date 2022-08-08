// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	// Interface for all Request Executors that return Response async
	public interface IExecutor<Request, Response>
	{
		Task<Response> ExecuteAsync(Request request);
	}
}

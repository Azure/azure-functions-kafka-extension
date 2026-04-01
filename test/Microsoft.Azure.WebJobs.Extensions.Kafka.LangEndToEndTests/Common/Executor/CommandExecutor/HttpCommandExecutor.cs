// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Executor for Http Commands
public class HttpCommandExecutor : IExecutor<IExecutableCommand<HttpResponseMessage>, HttpResponseMessage>
{
	public Task<HttpResponseMessage> ExecuteAsync(IExecutableCommand<HttpResponseMessage> request)
	{
		return request.ExecuteCommandAsync();
	}
}
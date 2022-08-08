// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
	// Invoke Strategy for http triggered function apps
	public class InvokeHttpRequestStrategy : IInvokeRequestStrategy<HttpResponseMessage>
	{
		private readonly IExecutor<IExecutableCommand<HttpResponseMessage>, HttpResponseMessage> _httpCommandExecutor;
		private readonly HttpRequestEntity _httpRequestEntity;

		public InvokeHttpRequestStrategy(HttpRequestEntity httpRequestEntity)
		{
			_httpRequestEntity = httpRequestEntity;
			_httpCommandExecutor = new HttpCommandExecutor();
		}

		public Task<HttpResponseMessage> InvokeRequestAsync()
		{
			IExecutableCommand<HttpResponseMessage> httpCmd = new HttpCommand.HttpCommandBuilder().
				SetHttpRequestEntity(_httpRequestEntity).Build();
			return _httpCommandExecutor.ExecuteAsync(httpCmd);
		}
	}
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Responsible for creation and execution of commands to handle http request response.
public class HttpCommand : IExecutableCommand<HttpResponseMessage>
{
	private readonly HttpClient _httpClient;
	private readonly HttpRequestEntity _httpRequestEntity;
	private readonly ILogger _logger = TestLogger.GetTestLogger();

	private readonly AsyncRetryPolicy retryPolicy = Policy.Handle<HttpRequestException>()
		.WaitAndRetryAsync(
			6,
			_ => TimeSpan.FromSeconds(20)
		);

	private HttpCommand(HttpCommandBuilder httpCommandBuilder)
	{
		_httpRequestEntity = httpCommandBuilder.GetHttpRequestEntity();
		_httpClient = new HttpClient();
	}

	public async Task<HttpResponseMessage> ExecuteCommandAsync()
	{
		var httpMethod = _httpRequestEntity.HttpMethod;

		switch (httpMethod)
		{
			case Constants.HTTP_POST:
				return await _httpClient.PostAsync(_httpRequestEntity.Url, null);

			case Constants.HTTP_PUT:
				return await _httpClient.PutAsync(_httpRequestEntity.Url, null);

			case Constants.HTTP_DELETE:
				return await _httpClient.DeleteAsync(_httpRequestEntity.Url);

			case Constants.HTTP_GET:
				var requestUri = new Uri(_httpRequestEntity.GetUrlWithQuery());
				return await GetAsync(requestUri);
			default:
				throw new NotImplementedException();
		}
	}

	private async Task<HttpResponseMessage> GetAsync(Uri requestUri)
	{
		HttpResponseMessage response = null;

		try
		{
			response = await retryPolicy.ExecuteAsync(async () => await _httpClient.GetAsync(requestUri));
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			_logger.LogInformation($"request:{requestUri.AbsoluteUri} response:{response.StatusCode.ToString()}");
		}
		catch (Exception ex)
		{
			_logger.LogError($"{ex}");
			throw ex;
		}

		return response;
	}

	public sealed class HttpCommandBuilder
	{
		private HttpRequestEntity _httpRequestEntity;

		public HttpCommandBuilder SetHttpRequestEntity(HttpRequestEntity httpRequestEntity)
		{
			_httpRequestEntity = httpRequestEntity;
			return this;
		}

		public HttpCommand Build()
		{
			return new HttpCommand(this);
		}

		public HttpRequestEntity GetHttpRequestEntity()
		{
			return _httpRequestEntity;
		}
	}
}
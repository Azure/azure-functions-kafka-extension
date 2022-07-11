using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.entity;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;
using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Util;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.http
{
    public class HttpCommand : Command<HttpResponseMessage>
    {
        private HttpRequestEntity httpRequestEntity;
        private HttpClient httpClient;
        private readonly ILogger logger = TestLogger.TestLogger.logger;

        private AsyncRetryPolicy retryPolicy = Policy.Handle<HttpRequestException>()
            .WaitAndRetryAsync(
               retryCount: 6,
               sleepDurationProvider: _ => TimeSpan.FromSeconds(20)
            );
               
        private HttpCommand(HttpCommandBuilder httpCommandBuilder)
        {
            this.httpRequestEntity = httpCommandBuilder.GetHttpRequestEntity();
            httpClient = new HttpClient();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }


        public async Task<HttpResponseMessage> ExecuteCommandAsync()
        {
            string httpMethod = httpRequestEntity.GetHttpMethod();

            switch (httpMethod)
            {
				case Constants.HTTP_POST:
                    return await httpClient.PostAsync(httpRequestEntity.GetUrl(), null);

                case Constants.HTTP_PUT:
                    return await httpClient.PutAsync(httpRequestEntity.GetUrl(), null);

                case Constants.HTTP_DELETE:
                    return await httpClient.DeleteAsync(httpRequestEntity.GetUrl());

                case Constants.HTTP_GET:
                    var requestUri = new Uri(httpRequestEntity.GetUrlWithQuery());
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
                response = await retryPolicy.ExecuteAsync(async () => await httpClient.GetAsync(requestUri));
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                logger.LogInformation($"request:{requestUri.AbsoluteUri} response:{response.StatusCode.ToString()}");
            }
            catch (Exception ex)
            {
                logger.LogError($"{ex}");
                throw ex;
            }
            return response;
        }

        public sealed class HttpCommandBuilder
        {
            private HttpRequestEntity httpRequestEntity;

            public HttpCommandBuilder() { }

            public HttpCommandBuilder SetHttpRequestEntity(HttpRequestEntity httpRequestEntity)
            {
                this.httpRequestEntity = httpRequestEntity;
                return this;
            }
            public HttpCommand Build()
            {
                return new HttpCommand(this);
            }
            public HttpRequestEntity GetHttpRequestEntity()
            {
                return this.httpRequestEntity;
            } 
        }
    }
}

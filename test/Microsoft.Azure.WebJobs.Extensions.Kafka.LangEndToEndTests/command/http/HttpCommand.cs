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

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.command.http
{
    public class HttpCommand : Command<HttpResponseMessage>
    {
        private HttpRequestEntity httpRequestEntity;
        private HttpClient httpClient;
        
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

            if(httpMethod.Equals(HttpMethods.Post))
            {
                return await httpClient.PostAsync(httpRequestEntity.GetUrl(), null);
            }
            else if(httpMethod.Equals(HttpMethods.Put))
            {
                return await httpClient.PutAsync(httpRequestEntity.GetUrl(), null);
            }
            else if (httpMethod.Equals(HttpMethods.Delete))
            {
                return await httpClient.DeleteAsync(httpRequestEntity.GetUrl());
            }
            else
            {
                var requestUri = new Uri(httpRequestEntity.GetUrlWithQuery());
                HttpResponseMessage response = null;
                
                try
                {
                    response = await retryPolicy.ExecuteAsync(async () => await httpClient.GetAsync(requestUri));
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Console.WriteLine($"request:{requestUri.AbsoluteUri} response:{response.StatusCode.ToString()}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw ex;
                }
                return response;
            }
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

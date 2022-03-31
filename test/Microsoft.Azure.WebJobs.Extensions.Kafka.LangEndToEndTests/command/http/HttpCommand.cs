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
        //private int MAX_RETRIES = 3;
        private AsyncRetryPolicy retryPolicy = Policy.Handle<HttpRequestException>()
            .WaitAndRetryAsync(
               retryCount: 6,
               sleepDurationProvider: _ => TimeSpan.FromSeconds(20)
            );
               
        //Overkill - Why this structure?
        private HttpCommand(HttpCommandBuilder httpCommandBuilder)
        {
            this.httpRequestEntity = httpCommandBuilder.GetHttpRequestEntity();
            httpClient = new HttpClient();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }


        //TODO Async Await
        public async Task<HttpResponseMessage> ExecuteCommandAsync()
        {
            // TODO execute HTTP request
            string httpMethod = httpRequestEntity.GetHttpMethod();

            if(httpMethod.Equals(HttpMethods.Post))
            {
                return await httpClient.PostAsync(httpRequestEntity.GetUrl(), null);
                //return await httpClient.PostAsync(client, url, data);
                // TODO execute post request
            }
            else if(httpMethod.Equals(HttpMethods.Put))
            {
                //return await PutAsync(client, url, data);
                // TODO execute put request
            }
            else if (httpMethod.Equals(HttpMethods.Delete))
            {
                //return await DeleteAsync(client, url, parameters);
                // TODO execute delete request
            }
            else
            {
                var requestUri = new Uri(httpRequestEntity.GetUrlWithQuery());
                HttpResponseMessage response = null;
                //Console.WriteLine();
                try
                {
                    response = await retryPolicy.ExecuteAsync(async () => await httpClient.GetAsync(requestUri));
                    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                    Console.WriteLine($"request:{requestUri.AbsoluteUri} response:{response.StatusCode.ToString()}");
                    //response = await httpClient.GetAsync(requestUri);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw ex;
                }
                return response;
            }

            return null;
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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.entity
{
    public class HttpRequestEntity
    {
        private string url;
        private string httpMethod;
        private Dictionary<string, string> headers;
        private Dictionary<string, string> requestParams;
        private String requestBody;

        public HttpRequestEntity(string url, string httpMethod, Dictionary<string, string> headers, 
            Dictionary<string, string> requestParams, string requestBody)
        {
            this.url = url;
            this.httpMethod = httpMethod;
            this.headers = headers;
            this.requestParams = requestParams;
            this.requestBody = requestBody;
        }

        public string GetHttpMethod() { return this.httpMethod; }
        public string GetUrl() { return this.url; }
    }
}

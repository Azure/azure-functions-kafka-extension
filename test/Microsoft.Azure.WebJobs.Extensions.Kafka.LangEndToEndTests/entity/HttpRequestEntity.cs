// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.entity
{
    /* Custom representation of Http Requests.
    */
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
        public string GetUrlWithQuery()
        {
            StringBuilder stringBuilder = new StringBuilder(url);
            stringBuilder.Append("?");
            stringBuilder.Append(GetQuery());
            
            return stringBuilder.ToString(); 
        }
        private string GetQuery()
        {
            var query = new List<string>();
            foreach (KeyValuePair<string, string> entry in requestParams)
            {
                if (!string.IsNullOrEmpty(entry.Key) && !string.IsNullOrEmpty(entry.Value)) 
                { 
                    query.Add($"{entry.Key}={entry.Value}");
                }
            }
            return string.Join("&", query.ToArray());
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common
{
    // Custom representation of Http Requests.
    public class HttpRequestEntity
    {
        public string Url { get; private set; }
        public string HttpMethod { get; private set; }
        private readonly Dictionary<string, string> _headers;
        private readonly Dictionary<string, string> _requestParams;
        private readonly string _requestBody;

        public HttpRequestEntity(string url, string httpMethod, Dictionary<string, string> headers, 
            Dictionary<string, string> requestParams, string requestBody)
        {
            Url = url;
            HttpMethod = httpMethod;
            _headers = headers;
            _requestParams = requestParams;
            _requestBody = requestBody;
        }

        public string GetUrlWithQuery()
        {
            StringBuilder stringBuilder = new StringBuilder(Url);
            stringBuilder.Append("?");
            stringBuilder.Append(GetQuery());
            
            return stringBuilder.ToString(); 
        }
        private string GetQuery()
        {
            var query = new List<string>();
            foreach (KeyValuePair<string, string> entry in _requestParams)
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

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.LangEndToEndTests.Common;

// Custom representation of Http Requests.
public class HttpRequestEntity
{
	private readonly Dictionary<string, string> _headers;
	private readonly string _requestBody;
	private readonly Dictionary<string, string> _requestParams;

	public HttpRequestEntity(string url, string httpMethod, Dictionary<string, string> headers,
		Dictionary<string, string> requestParams, string requestBody)
	{
		Url = url;
		HttpMethod = httpMethod;
		_headers = headers;
		_requestParams = requestParams;
		_requestBody = requestBody;
	}

	public string Url { get; }
	public string HttpMethod { get; }

	public string GetUrlWithQuery()
	{
		var stringBuilder = new StringBuilder(Url);
		stringBuilder.Append("?");
		stringBuilder.Append(GetQuery());

		return stringBuilder.ToString();
	}

	private string GetQuery()
	{
		var query = new List<string>();
		foreach (var entry in _requestParams)
		{
			if (!string.IsNullOrEmpty(entry.Key) && !string.IsNullOrEmpty(entry.Value))
			{
				query.Add($"{entry.Key}={entry.Value}");
			}
		}

		return string.Join("&", query.ToArray());
	}
}
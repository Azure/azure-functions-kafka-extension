// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Config
{
    /// <summary>
    /// Performs OAuth2 client-credentials token acquisition in managed .NET code,
    /// bypassing librdkafka's libcurl-based OIDC path. Caches the token in memory
    /// and refreshes a configurable window before expiry.
    /// </summary>
    /// <remarks>
    /// One instance is shared across the Kafka client(s) and the Schema Registry
    /// client so the same access token serves both. Thread-safe.
    /// </remarks>
    internal sealed class OidcTokenProvider
    {
        private static readonly HttpClient SharedHttpClient = new HttpClient();
        private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);

        private readonly string tokenEndpointUrl;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly string scope;
        private readonly IDictionary<string, string> extensions;
        private readonly SemaphoreSlim refreshLock = new SemaphoreSlim(1, 1);

        private string cachedToken;
        private DateTimeOffset cachedExpiresAt = DateTimeOffset.MinValue;

        public OidcTokenProvider(string tokenEndpointUrl, string clientId, string clientSecret, string scope, string extensionsString)
        {
            this.tokenEndpointUrl = tokenEndpointUrl ?? throw new ArgumentNullException(nameof(tokenEndpointUrl));
            this.clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            this.clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
            this.scope = scope;
            this.extensions = ParseExtensions(extensionsString);
        }

        public string PrincipalName => this.clientId;

        public IDictionary<string, string> Extensions => this.extensions;

        public Task<(string Token, long ExpiresAtUnixMs)> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            return GetTokenAsync(null, cancellationToken);
        }

        public async Task<(string Token, long ExpiresAtUnixMs)> GetTokenAsync(ILogger logger, CancellationToken cancellationToken = default)
        {
            if (this.cachedToken != null && DateTimeOffset.UtcNow + RefreshSkew < this.cachedExpiresAt)
            {
                return (this.cachedToken, this.cachedExpiresAt.ToUnixTimeMilliseconds());
            }

            await this.refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (this.cachedToken != null && DateTimeOffset.UtcNow + RefreshSkew < this.cachedExpiresAt)
                {
                    return (this.cachedToken, this.cachedExpiresAt.ToUnixTimeMilliseconds());
                }

                (this.cachedToken, this.cachedExpiresAt) = await FetchTokenAsync(logger, cancellationToken).ConfigureAwait(false);
                return (this.cachedToken, this.cachedExpiresAt.ToUnixTimeMilliseconds());
            }
            finally
            {
                this.refreshLock.Release();
            }
        }

        private async Task<(string Token, DateTimeOffset ExpiresAt)> FetchTokenAsync(ILogger logger, CancellationToken cancellationToken)
        {
            logger?.LogInformation(
                "Fetching OIDC token from {TokenEndpoint} (clientId={ClientId}, scope={Scope})",
                this.tokenEndpointUrl,
                this.clientId,
                string.IsNullOrWhiteSpace(this.scope) ? "<none>" : this.scope);

            var form = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", this.clientId),
                new KeyValuePair<string, string>("client_secret", this.clientSecret),
            };
            if (!string.IsNullOrWhiteSpace(this.scope))
            {
                form.Add(new KeyValuePair<string, string>("scope", this.scope));
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, this.tokenEndpointUrl)
            {
                Content = new FormUrlEncodedContent(form)
            };

            using var response = await SharedHttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"OIDC token endpoint '{this.tokenEndpointUrl}' returned {(int)response.StatusCode}: {body}");
            }

            var json = JObject.Parse(body);
            var token = (string)json["access_token"];
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException(
                    $"OIDC token response from '{this.tokenEndpointUrl}' did not contain access_token.");
            }

            var expiresInSeconds = (int?)json["expires_in"] ?? 3600;
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);
            logger?.LogInformation(
                "OIDC token fetched from {TokenEndpoint}, expires in {ExpiresInSeconds}s at {ExpiresAt:o}",
                this.tokenEndpointUrl,
                expiresInSeconds,
                expiresAt);
            return (token, expiresAt);
        }

        internal static IDictionary<string, string> ParseExtensions(string extensionsString)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(extensionsString))
            {
                return dict;
            }

            foreach (var pair in extensionsString.Split(','))
            {
                var trimmed = pair.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }
                var eq = trimmed.IndexOf('=');
                if (eq <= 0 || eq == trimmed.Length - 1)
                {
                    throw new ArgumentException(
                        $"Invalid OAuthBearerExtensions entry '{trimmed}'. Expected key=value pairs separated by commas.",
                        nameof(extensionsString));
                }
                dict[trimmed.Substring(0, eq)] = trimmed.Substring(eq + 1);
            }
            return dict;
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Config
{
    /// <summary>
    /// Helpers that wire a shared <see cref="OidcTokenProvider"/> into the
    /// various Confluent.Kafka client builders via
    /// <c>SetOAuthBearerTokenRefreshHandler</c>.
    /// </summary>
    internal static class OidcManagedAuth
    {
        /// <summary>
        /// Indicates whether the given method requires managed (.NET-side)
        /// token acquisition.
        /// </summary>
        public static bool IsManaged(OAuthBearerMethod method) => method == OAuthBearerMethod.OidcManaged;

        public static OidcTokenProvider CreateProvider(string tokenEndpointUrl, string clientId, string clientSecret, string scope, string extensions)
        {
            if (string.IsNullOrWhiteSpace(tokenEndpointUrl))
            {
                throw new ArgumentException("OAuthBearerTokenEndpointUrl is required when OAuthBearerMethod is OidcManaged.", nameof(tokenEndpointUrl));
            }
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new ArgumentException("OAuthBearerClientId is required when OAuthBearerMethod is OidcManaged.", nameof(clientId));
            }
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new ArgumentException("OAuthBearerClientSecret is required when OAuthBearerMethod is OidcManaged.", nameof(clientSecret));
            }
            return new OidcTokenProvider(tokenEndpointUrl, clientId, clientSecret, scope, extensions);
        }

        public static void WireConsumer<TKey, TValue>(ConsumerBuilder<TKey, TValue> builder, OidcTokenProvider provider, ILogger logger)
        {
            builder.SetOAuthBearerTokenRefreshHandler((client, _) => RefreshToken(client, provider, logger));
        }

        public static void WireProducer<TKey, TValue>(ProducerBuilder<TKey, TValue> builder, OidcTokenProvider provider, ILogger logger)
        {
            builder.SetOAuthBearerTokenRefreshHandler((client, _) => RefreshToken(client, provider, logger));
        }

        public static void WireAdminClient(AdminClientBuilder builder, OidcTokenProvider provider, ILogger logger)
        {
            builder.SetOAuthBearerTokenRefreshHandler((client, _) => RefreshToken(client, provider, logger));
        }

        /// <summary>
        /// Synchronously sets an initial OAUTHBEARER token on a client. Required for
        /// clients that aren't actively polled (scaler Consumer, single-shot AdminClient)
        /// because librdkafka blocks in TRY_CONNECT until a token is set, and the
        /// refresh-handler callback is only dispatched via Consume()/Poll() activity.
        /// </summary>
        public static void PrimeToken(IClient client, OidcTokenProvider provider, ILogger logger)
        {
            RefreshToken(client, provider, logger);
        }

        private static void RefreshToken(IClient client, OidcTokenProvider provider, ILogger logger)
        {
            logger?.LogInformation("Acquiring managed OIDC token for client {ClientName}", client?.Name ?? "<unknown>");
            try
            {
                var (token, expiresAtUnixMs) = provider.GetTokenAsync(logger).GetAwaiter().GetResult();
                client.OAuthBearerSetToken(token, expiresAtUnixMs, provider.PrincipalName, provider.Extensions);
                logger?.LogInformation(
                    "OIDC token set on client {ClientName}, expires at {ExpiresAt:o} (principal={Principal}, extensions={ExtensionCount})",
                    client?.Name ?? "<unknown>",
                    DateTimeOffset.FromUnixTimeMilliseconds(expiresAtUnixMs),
                    provider.PrincipalName,
                    provider.Extensions?.Count ?? 0);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to acquire managed OIDC token for client {ClientName}", client?.Name ?? "<unknown>");
                client.OAuthBearerSetTokenFailure(ex.ToString());
            }
        }
    }
}

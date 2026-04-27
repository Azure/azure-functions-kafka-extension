// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Net.Http.Headers;
using Confluent.SchemaRegistry;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Config
{
    /// <summary>
    /// Adapts <see cref="OidcTokenProvider"/> to Confluent's Schema Registry
    /// authentication callback so the same managed OIDC token is used for SR
    /// HTTPS calls as for Kafka broker SASL/OAUTHBEARER.
    /// </summary>
    internal sealed class OidcAuthenticationHeaderValueProvider : IAuthenticationHeaderValueProvider
    {
        private readonly OidcTokenProvider tokenProvider;

        public OidcAuthenticationHeaderValueProvider(OidcTokenProvider tokenProvider)
        {
            this.tokenProvider = tokenProvider;
        }

        public AuthenticationHeaderValue GetAuthenticationHeader()
        {
            var (token, _) = this.tokenProvider.GetTokenAsync().GetAwaiter().GetResult();
            return new AuthenticationHeaderValue("Bearer", token);
        }
    }
}

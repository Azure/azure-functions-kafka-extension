// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.Config
{
    /// <summary>
    /// Defines the OAuth bearer method
    /// </summary>
    public enum OAuthBearerMethod
    {
        Default,
        Oidc,

        /// <summary>
        /// OIDC client-credentials flow performed in managed .NET code rather than
        /// delegated to librdkafka's libcurl-based token fetch. Uses HttpClient to
        /// POST to <c>OAuthBearerTokenEndpointUrl</c> and supplies the resulting
        /// token to librdkafka via <c>SetOAuthBearerTokenRefreshHandler</c>. Avoids
        /// the platform-specific CA-bundle issue that affects librdkafka's OIDC
        /// path on some Linux images (e.g. Azure Functions Flex).
        /// </summary>
        OidcManaged
    }
}

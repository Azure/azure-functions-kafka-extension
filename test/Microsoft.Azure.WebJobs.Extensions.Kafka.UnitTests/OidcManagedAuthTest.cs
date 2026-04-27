// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Extensions.Kafka.Config;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class OidcManagedAuthTest
    {
        [Fact]
        public void IsManaged_ReturnsTrue_OnlyForOidcManaged()
        {
            Assert.False(OidcManagedAuth.IsManaged(OAuthBearerMethod.Default));
            Assert.False(OidcManagedAuth.IsManaged(OAuthBearerMethod.Oidc));
            Assert.True(OidcManagedAuth.IsManaged(OAuthBearerMethod.OidcManaged));
        }

        [Fact]
        public void CreateProvider_Throws_WhenRequiredFieldsMissing()
        {
            Assert.Throws<ArgumentException>(() =>
                OidcManagedAuth.CreateProvider(null, "id", "secret", "scope", null));
            Assert.Throws<ArgumentException>(() =>
                OidcManagedAuth.CreateProvider("https://example/token", null, "secret", "scope", null));
            Assert.Throws<ArgumentException>(() =>
                OidcManagedAuth.CreateProvider("https://example/token", "id", null, "scope", null));
        }

        [Fact]
        public void CreateProvider_AcceptsAllRequiredFields()
        {
            var provider = OidcManagedAuth.CreateProvider(
                "https://example/token", "id", "secret", "scope", "a=1,b=2");
            Assert.Equal("id", provider.PrincipalName);
            Assert.Equal(2, provider.Extensions.Count);
            Assert.Equal("1", provider.Extensions["a"]);
            Assert.Equal("2", provider.Extensions["b"]);
        }
    }

    public class OidcTokenProviderParseExtensionsTest
    {
        [Fact]
        public void ParseExtensions_NullOrEmpty_ReturnsEmpty()
        {
            Assert.Empty(OidcTokenProvider.ParseExtensions(null));
            Assert.Empty(OidcTokenProvider.ParseExtensions(string.Empty));
            Assert.Empty(OidcTokenProvider.ParseExtensions("   "));
        }

        [Fact]
        public void ParseExtensions_SinglePair_Parses()
        {
            var result = OidcTokenProvider.ParseExtensions("key=value");
            Assert.Single(result);
            Assert.Equal("value", result["key"]);
        }

        [Fact]
        public void ParseExtensions_ConfluentCloudShape_Parses()
        {
            var result = OidcTokenProvider.ParseExtensions("logicalCluster=lkc-abc,identityPoolId=pool-xyz");
            Assert.Equal(2, result.Count);
            Assert.Equal("lkc-abc", result["logicalCluster"]);
            Assert.Equal("pool-xyz", result["identityPoolId"]);
        }

        [Fact]
        public void ParseExtensions_PreservesEqualsInValue()
        {
            var result = OidcTokenProvider.ParseExtensions("claim=foo=bar");
            Assert.Single(result);
            Assert.Equal("foo=bar", result["claim"]);
        }

        [Fact]
        public void ParseExtensions_IgnoresWhitespaceBetweenEntries()
        {
            var result = OidcTokenProvider.ParseExtensions(" a=1 , b=2 ");
            Assert.Equal(2, result.Count);
            Assert.Equal("1", result["a"]);
            Assert.Equal("2", result["b"]);
        }

        [Theory]
        [InlineData("missingEquals")]
        [InlineData("=valueWithoutKey")]
        [InlineData("keyWithoutValue=")]
        public void ParseExtensions_Throws_OnMalformedEntry(string input)
        {
            Assert.Throws<ArgumentException>(() => OidcTokenProvider.ParseExtensions(input));
        }
    }
}

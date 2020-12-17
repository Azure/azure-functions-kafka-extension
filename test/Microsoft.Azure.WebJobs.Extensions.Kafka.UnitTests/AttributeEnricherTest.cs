// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class AttributeEnricherTest
    {
        private AttributeEnricher enricher;
        private IConfigurationRoot config;
        private DefaultNameResolver nameResolver;

        public AttributeEnricherTest()
        {
            this.enricher = new AttributeEnricher();
            this.config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"Protocol", "SaslPlaintext"}
                }).Build();
            this.nameResolver = new DefaultNameResolver(config);
        }

        [Fact]
        public void When_ConfigurationString_Provided_Populates_Properties()
        {
            var attr = new KafkaTriggerAttribute("", "", 
                @"
                  Username=""test user"";
                  AuthenticationMode=Plain;
                  Protocol=%Protocol%
                "
            );

            enricher.Enrich(attr, config, nameResolver);

            Assert.Equal(attr.Username, "test user");
            Assert.Equal(attr.AuthenticationMode, BrokerAuthenticationMode.Plain);
            Assert.Equal(attr.Protocol, BrokerProtocol.SaslPlaintext);
        }

        [Fact]
        public void When_ConfigurationString_Provided_Populates_NullableProperties()
        {
            var attr = new KafkaAttribute("", "", 
                "MessageTimeoutMs=3000"
            );

            enricher.Enrich(attr, config, nameResolver);

            Assert.Equal(attr.MessageTimeoutMs, 3000);
        }
    }
}

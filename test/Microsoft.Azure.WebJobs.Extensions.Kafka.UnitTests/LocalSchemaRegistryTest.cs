// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Confluent.SchemaRegistry;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.UnitTests
{
    public class LocalSchemaRegistryTest
    {
        private const string AvroSchema = "{\"type\":\"string\"}";

        [Fact]
        public void OfflineClient_HasNoAuthenticationOrProxy()
        {
            var client = new LocalSchemaRegistry(AvroSchema);

            Assert.Null(client.AuthHeaderProvider);
            Assert.Null(client.Proxy);
        }

        [Fact]
        public async Task GetSchemaByGuidAsync_ReturnsLocalSchema()
        {
            var client = new LocalSchemaRegistry(AvroSchema);

            var schema = await client.GetSchemaByGuidAsync("ignored-guid");

            Assert.Equal(AvroSchema, schema.SchemaString);
            Assert.Equal(SchemaType.Avro, schema.SchemaType);
        }

        [Fact]
        public async Task RegisterSchemaWithResponseAsync_ReturnsLocalRegistration()
        {
            var client = new LocalSchemaRegistry(AvroSchema);
            var schema = new Schema(AvroSchema, SchemaType.Avro);

            var registeredSchema = await client.RegisterSchemaWithResponseAsync("topic-value", schema);

            Assert.Equal("topic-value", registeredSchema.Subject);
            Assert.Equal(1, registeredSchema.Version);
            Assert.Equal(1, registeredSchema.Id);
            Assert.Equal(AvroSchema, registeredSchema.SchemaString);
            Assert.Equal(SchemaType.Avro, registeredSchema.SchemaType);
            Assert.Contains("topic-value", await client.GetAllSubjectsAsync());
        }
    }
}

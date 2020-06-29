// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Schema registry client for offline, where only available schema is the one provider by function contract
    /// </summary>
    public class LocalSchemaRegistry : ISchemaRegistryClient
    {
        private readonly string schema;

        public LocalSchemaRegistry(string schema)
        {
            this.schema = schema;
        }

        public int MaxCachedSchemas
        {
            get 
            {
                return 1;
            }
        }

        public string ConstructKeySubjectName(string topic)
        {
            throw new System.NotImplementedException();
        }

        public string ConstructKeySubjectName(string topic, string recordType = null)
        {
            throw new System.NotImplementedException();
        }

        public string ConstructValueSubjectName(string topic) => topic;

        public string ConstructValueSubjectName(string topic, string recordType = null) => topic;

        public void Dispose()
        {
        }

        public Task<List<string>> GetAllSubjectsAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<Schema> GetLatestSchemaAsync(string subject)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetSchemaAsync(int id)
        {
            return Task.FromResult(this.schema);
        }

        public Task<string> GetSchemaAsync(string subject, int version)
        {
            return Task.FromResult(this.schema);
        }

        public Task<int> GetSchemaIdAsync(string subject, string schema)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<int>> GetSubjectVersionsAsync(string subject)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> IsCompatibleAsync(string subject, string schema)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> RegisterSchemaAsync(string subject, string schema) => Task.FromResult(1);
    }
}
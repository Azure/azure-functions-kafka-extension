// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.SchemaRegistry;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka
{
    /// <summary>
    /// Schema registry client for offline, where only available schema is the one provider by function contract.
    /// </summary>
    public class LocalSchemaRegistry : ISchemaRegistryClient
    {
        private readonly string valueSchema;
        private readonly string keySchema;
        private string valueSubjectName;
        private string keySubjectName;
        private List<string> subjects = new List<string>();

        public LocalSchemaRegistry(string valueSchema, string keySchema = null)
        {
            this.valueSchema = valueSchema;
            this.keySchema = keySchema;
        }

        public int MaxCachedSchemas
        {
            get 
            {
                return 2;
            }
        }

        public string ConstructKeySubjectName(string topic, string recordType = null) => keySubjectName = $"{topic}-key";

        public string ConstructValueSubjectName(string topic, string recordType = null) => valueSubjectName = $"{topic}-value";

        public void Dispose()
        {
        }

        public Task<List<string>> GetAllSubjectsAsync()
        {
            return Task.FromResult(this.subjects);
        }

        public Task<Compatibility> GetCompatibilityAsync(string subject = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<RegisteredSchema> GetLatestSchemaAsync(string subject)
        {
            throw new System.NotImplementedException();
        }

        public Task<RegisteredSchema> GetRegisteredSchemaAsync(string subject, int version)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetSchemaAsync(string subject, int version)
        {
            if (subject == keySubjectName)
            {
                return Task.FromResult(this.keySchema);
            }
            else
            {
                return Task.FromResult(this.valueSchema);
            }
        }

        public Task<Schema> GetSchemaAsync(int id, string format = null)
        {
            if (subjects[id] == keySubjectName)
            {
                return Task.FromResult(new Schema(this.keySchema, SchemaType.Avro));
            }
            return Task.FromResult(new Schema(this.valueSchema, SchemaType.Avro));
        }

        public Task<int> GetSchemaIdAsync(string subject, string schema)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> GetSchemaIdAsync(string subject, Schema schema)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> GetSchemaIdAsync(string subject, string avroSchema, bool normalize = false)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> GetSchemaIdAsync(string subject, Schema schema, bool normalize = false)
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

        public Task<bool> IsCompatibleAsync(string subject, Schema schema)
        {
            throw new System.NotImplementedException();
        }

        public Task<RegisteredSchema> LookupSchemaAsync(string subject, Schema schema, bool ignoreDeletedSchemas)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> RegisterSchemaAsync(string subject, string schema)
        {
            subjects.Add(subject);
            return Task.FromResult(1);
        }
        public Task<int> RegisterSchemaAsync(string subject, Schema schema)
        {
            subjects.Add(subject);
            return Task.FromResult(1);
        }

        public Task<RegisteredSchema> LookupSchemaAsync(string subject, Schema schema, bool ignoreDeletedSchemas, bool normalize = false)
        {
            throw new System.NotImplementedException();
        }

        public Task<int> RegisterSchemaAsync(string subject, string schema, bool normalize = false)
        {
            subjects.Add(subject);
            return Task.FromResult(1);
        }

        public Task<int> RegisterSchemaAsync(string subject, Schema schema, bool normalize = false)
        {
            subjects.Add(subject);
            return Task.FromResult(1);
        }

        public Task<Compatibility> UpdateCompatibilityAsync(Compatibility compatibility, string subject = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
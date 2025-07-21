// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Avro;
using Avro.Specific;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests
{
    public class MyKeyAvroRecord : ISpecificRecord
    {
        public const string SchemaText = @"
       {
        ""type"": ""record"",
        ""name"": ""MyKeyAvroRecord"",
        ""namespace"": ""Microsoft.Azure.WebJobs.Extensions.Kafka.EndToEndTests"",
        ""fields"": [
        {
            ""name"": ""id"",
            ""type"": ""int""
        },  
        {
            ""name"": ""type"",
            ""type"": ""string""
        }
        ]
    }";
        public static Schema _SCHEMA = Schema.Parse(SchemaText);

        [JsonIgnore]
        public virtual Schema Schema => _SCHEMA;
        public string ID { get; set; }
        public string Type { get; set; }

        public virtual object Get(int fieldPos)
        {
            switch (fieldPos)
            {
                case 0: return this.ID;
                case 1: return this.Type;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
            }
            ;
        }
        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: this.ID = (string)fieldValue; break;
                case 1: this.Type = (string)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            }
            ;
        }

    }
}
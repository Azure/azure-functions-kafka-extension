using Avro;
using Avro.Specific;
using Newtonsoft.Json;

namespace KafkaFunctionSample
{
    public class PageViewsFemale : ISpecificRecord
    {
       public const string SchemaText = @"{
  ""type"": ""record"",
  ""name"": ""PageViewsFemale"",
  ""namespace"": ""KafkaFunctionSample"",
  ""fields"": [
    {
      ""name"": ""USERID"",
      ""type"": [
        ""null"",
        ""string""
      ],
      ""default"": null
    },
    {
      ""name"": ""PAGEID"",
      ""type"": [
        ""null"",
        ""string""
      ],
      ""default"": null
    },
    {
      ""name"": ""REGIONID"",
      ""type"": [
        ""null"",
        ""string""
      ],
      ""default"": null
    },
    {
      ""name"": ""GENDER"",
      ""type"": [
        ""null"",
        ""string""
      ],
      ""default"": null
    }
  ]
}";
        public static Schema _SCHEMA = Schema.Parse(SchemaText);

        [JsonIgnore]
        public virtual Schema Schema => _SCHEMA;

        public string UserID { get; set; }
        public string PageID { get; set; }
        public string RegionID { get; set; }
        public string Gender { get; set; }

        public virtual object Get(int fieldPos)
        {
            switch (fieldPos)
            {
                case 0: return this.UserID;
                case 1: return this.PageID;
                case 2: return this.RegionID;
                case 3: return this.Gender;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
            };
        }
        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: this.UserID = (string)fieldValue; break;
                case 1: this.PageID = (string)fieldValue; break;
                case 2: this.RegionID = (string)fieldValue; break;
                case 3: this.Gender = (string)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            };
        }

    }
}
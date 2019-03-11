using Avro;
using Avro.Specific;
using Newtonsoft.Json;

namespace KafkaFunctionSample
{
    public class PageViews : ISpecificRecord
    {
       public const string SchemaText = @"
       {
  ""type"": ""record"",
  ""name"": ""PageViews"",
  ""namespace"": ""KafkaFunctionSample"",
  ""fields"": [
    {
      ""name"": ""viewtime"",
      ""type"": ""long""
    },
    {
      ""name"": ""userid"",
      ""type"": ""string""
    },
    {
      ""name"": ""pageid"",
      ""type"": ""string""
    }
  ]
}";
        public static Schema _SCHEMA = Schema.Parse(SchemaText);

        [JsonIgnore]
        public virtual Schema Schema => _SCHEMA;

        public long ViewTime { get; set; }
        public string UserID { get; set; }
        public string PageID { get; set; }

        public virtual object Get(int fieldPos)
        {
            switch (fieldPos)
            {
                case 0: return this.ViewTime;
                case 1: return this.UserID;
                case 2: return this.PageID;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
            };
        }
        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: this.ViewTime = (long)fieldValue; break;
                case 1: this.UserID = (string)fieldValue; break;
                case 2: this.PageID = (string)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            };
        }

    }
}
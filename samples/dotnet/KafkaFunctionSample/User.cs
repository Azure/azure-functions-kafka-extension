using Avro;
using Avro.Specific;
using Newtonsoft.Json;

namespace KafkaFunctionSample
{
    public class UserRecord : ISpecificRecord
    {
       public const string SchemaText = @"
       {
  ""type"": ""record"",
  ""name"": ""UserRecord"",
  ""namespace"": ""KafkaFunctionSample"",
  ""fields"": [
    {
      ""name"": ""registertime"",
      ""type"": ""long""
    },
    {
      ""name"": ""userid"",
      ""type"": ""string""
    },
    {
      ""name"": ""regionid"",
      ""type"": ""string""
    },
    {
      ""name"": ""gender"",
      ""type"": ""string""
    }
  ]
}";
        public static Schema _SCHEMA = Schema.Parse(SchemaText);

        [JsonIgnore]
        public virtual Schema Schema => _SCHEMA;
        public long RegisterTime { get; set; }
        public string UserID { get; set; }
        public string RegionID { get; set; }
        public string Gender { get; set; }

        public virtual object Get(int fieldPos)
        {
            switch (fieldPos)
            {
                case 0: return this.RegisterTime;
                case 1: return this.UserID;
                case 2: return this.RegionID;
                case 3: return this.Gender;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
            };
        }
        public virtual void Put(int fieldPos, object fieldValue)
        {
            switch (fieldPos)
            {
                case 0: this.RegisterTime = (long)fieldValue; break;
                case 1: this.UserID = (string)fieldValue; break;
                case 2: this.RegionID = (string)fieldValue; break;
                case 3: this.Gender = (string)fieldValue; break;
                default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
            };
        }
    }
}
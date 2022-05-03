using Newtonsoft.Json;
using System;

namespace Parser
{
    public class DateTimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(long?));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //if (objectType == typeof(long?))
            //{
            //    long? stamp = serializer.Deserialize<long?>(reader);
            //    if (stamp == null)
            //    {
            //        return null;
            //    }
            //    else
            //    {
            //        return Tools.ConvertFromUnixTimestamp((double)stamp);
            //    }
            //}
            //else
            //{
            //    return null;
            //}
            

            return serializer.Deserialize<long?>(reader);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}

using Newtonsoft.Json;
using System;

namespace Parser
{
    /// <summary>
    /// Конвертер даты
    /// </summary>
    public class DateConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.GetType() == typeof(long?);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if(long.TryParse((reader.Value.ToString()), out long result))
            {
                return Tools.ConvertFromUnixTimestamp(result);
            }
            else
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if(value.GetType() == typeof(DateTime))
            {
                long? timestamp = Tools.ConvertToUnixTimestamp((DateTime)value);
                writer.WriteValue(timestamp);
            }
            else
            {
                long? timestamp = null;
                writer.WriteValue(timestamp);
            }
        }
    }
}

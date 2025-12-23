using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaserLib
{
    public class OnOffBoolConverter : Newtonsoft.Json.JsonConverter<bool>
    {
        public override void WriteJson(JsonWriter writer, bool value, JsonSerializer serializer)
        {
            writer.WriteValue(value ? "On" : "Off");
        }

        public override bool ReadJson(JsonReader reader, Type objectType, bool existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                var value = reader.Value?.ToString();
                if (string.Equals(value, "On", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "High", StringComparison.OrdinalIgnoreCase)
                    )  
                    return true;
                if (string.Equals(value, "Off", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "OFF", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(value, "Low", StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            // Если не распознано — по умолчанию false или бросить исключение
            //throw new JsonSerializationException($"Unexpected value for boolean: {reader.Value}");
            return false;
        }
    }
}

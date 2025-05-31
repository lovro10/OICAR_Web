using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CARSHARE_WEBAPP.Services
{

    public class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        private const string _format = "yyyy-MM-dd";

        public override DateOnly Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            var dateString = reader.GetString();
            if (DateOnly.TryParseExact(dateString, _format, out var parsed))
            {
                return parsed;
            }
            throw new JsonException($"Cannot parse '{dateString}' as DateOnly({_format})");
        }

        public override void Write(
            Utf8JsonWriter writer,
            DateOnly value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_format));
        }
    }
}

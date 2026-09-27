using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VgAuto.Http.Api
{
    /// <summary>
    /// Timestamps are stored in UTC. MySQL returns them without a kind; write those as UTC ("Z")
    /// so PostgreSQL and MySQL deployments produce the same JSON.
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetDateTime();

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value);
        }
    }
}

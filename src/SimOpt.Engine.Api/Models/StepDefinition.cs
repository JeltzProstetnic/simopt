using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimOpt.Engine.Api.Models
{
    /// <summary>
    /// SIM-126 schema: one step of a <c>process</c> node, written flat as
    /// <c>{"type": "seize", "resource": "bays"}</c> or <c>{"id": "washing", "type": "delay", "duration": {...}}</c>.
    /// Every field beside <c>id</c> and <c>type</c> is a number, a string or a distribution, the
    /// <see cref="ParamValue"/> node parameters use; which fields a type reads is in
    /// <see cref="SchemaCatalog.StepTypes"/>, and the builder refuses any other.
    /// </summary>
    [JsonConverter(typeof(StepDefinitionConverter))]
    public sealed class StepDefinition
    {
        /// <summary>The step's id within its process; without one it is <c>&lt;type&gt;#&lt;n&gt;</c>.</summary>
        public string? Id { get; set; }

        public string Type { get; set; } = string.Empty;

        /// <summary>The step's fields, in the order they were written.</summary>
        public Dictionary<string, ParamValue> Fields { get; set; } = new();
    }

    /// <summary>Reads a step as a flat object, and writes it back the same way: id, type, then the fields.</summary>
    public sealed class StepDefinitionConverter : JsonConverter<StepDefinition>
    {
        public override StepDefinition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("A step is an object such as {\"type\": \"delay\", \"duration\": {\"type\": \"constant\", \"value\": 5}}.");
            var step = new StepDefinition();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                string property = reader.GetString() ?? string.Empty;
                reader.Read();
                if (property == "id")
                    step.Id = reader.TokenType == JsonTokenType.String ? reader.GetString()
                        : throw new JsonException("A step's id is a string.");
                else if (property == "type")
                    step.Type = reader.TokenType == JsonTokenType.String ? reader.GetString()!
                        : throw new JsonException("A step's type is a string, such as \"delay\".");
                else
                    step.Fields[property] = JsonSerializer.Deserialize<ParamValue>(ref reader, options)!;
            }
            return step;
        }

        public override void Write(Utf8JsonWriter writer, StepDefinition value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            if (value.Id != null) writer.WriteString("id", value.Id);
            writer.WriteString("type", value.Type);
            foreach (var (name, field) in value.Fields)
            {
                writer.WritePropertyName(name);
                JsonSerializer.Serialize(writer, field, options);
            }
            writer.WriteEndObject();
        }
    }
}

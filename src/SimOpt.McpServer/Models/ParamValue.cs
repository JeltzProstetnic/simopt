using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimOpt.McpServer.Models
{
    /// <summary>What a node parameter holds.</summary>
    public enum ParamValueKind
    {
        Number,
        Text,
        Distribution,
    }

    /// <summary>
    /// SIM-65: one node parameter, a number (<c>"capacity": 15</c>), a string (<c>"discipline": "fifo"</c>)
    /// or a distribution (<c>"service": {"type": "triangular", "min": 20, "mode": 25, "max": 40}</c>).
    /// </summary>
    /// <remarks>
    /// Params were a dictionary of doubles, which refused any topology with a distribution in it. The
    /// implicit conversions from <see cref="double"/>, <see cref="int"/>, <see cref="string"/> and
    /// <see cref="DistributionSpec"/> keep C# that builds topologies (<c>Params = { ["capacity"] = 15 }</c>)
    /// reading as before.
    /// </remarks>
    [JsonConverter(typeof(ParamValueJsonConverter))]
    public sealed class ParamValue
    {
        private readonly double number;

        private ParamValue(ParamValueKind kind, double number, string? text, DistributionSpec? distribution)
        {
            Kind = kind;
            this.number = number;
            Text = text;
            Distribution = distribution;
        }

        public ParamValueKind Kind { get; }

        /// <summary>The number, or null when the parameter is not a number.</summary>
        public double? Number => Kind == ParamValueKind.Number ? number : null;

        /// <summary>The string, or null when the parameter is not a string.</summary>
        public string? Text { get; }

        /// <summary>The distribution, or null when the parameter is not a distribution.</summary>
        public DistributionSpec? Distribution { get; }

        public static implicit operator ParamValue(double value) => new ParamValue(ParamValueKind.Number, value, null, null);

        public static implicit operator ParamValue(int value) => new ParamValue(ParamValueKind.Number, value, null, null);

        public static implicit operator ParamValue(string value)
            => new ParamValue(ParamValueKind.Text, 0d, value ?? throw new ArgumentNullException(nameof(value)), null);

        public static implicit operator ParamValue(DistributionSpec value)
            => new ParamValue(ParamValueKind.Distribution, 0d, null, value ?? throw new ArgumentNullException(nameof(value)));

        /// <summary>As written in a topology, for error messages: <c>15</c>, <c>"fifo"</c> or <c>a triangular distribution</c>.</summary>
        public override string ToString() => Kind switch
        {
            ParamValueKind.Number => number.ToString("R", CultureInfo.InvariantCulture),
            ParamValueKind.Text => "\"" + Text + "\"",
            _ => "a " + (Distribution!.Type ?? "untyped") + " distribution",
        };
    }

    /// <summary>
    /// Reads a JSON number, string or object into a <see cref="ParamValue"/> and writes it back in the
    /// same form. Anything else, null included, is refused; the serializer adds where it was to the
    /// exception's <see cref="JsonException.Path"/>.
    /// </summary>
    public sealed class ParamValueJsonConverter : JsonConverter<ParamValue>
    {
        public override bool HandleNull => true;

        public override ParamValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Number:
                    return reader.GetDouble();
                case JsonTokenType.String:
                    return reader.GetString()!;
                case JsonTokenType.StartObject:
                    return JsonSerializer.Deserialize<DistributionSpec>(ref reader, options)!;
                default:
                    throw new JsonException("A parameter must be a number, a string or a distribution object, not " +
                                            reader.TokenType.ToString().ToLowerInvariant() + ".");
            }
        }

        public override void Write(Utf8JsonWriter writer, ParamValue value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }
            switch (value.Kind)
            {
                case ParamValueKind.Number:
                    writer.WriteNumberValue(value.Number!.Value);
                    break;
                case ParamValueKind.Text:
                    writer.WriteStringValue(value.Text);
                    break;
                default:
                    JsonSerializer.Serialize(writer, value.Distribution, options);
                    break;
            }
        }
    }
}

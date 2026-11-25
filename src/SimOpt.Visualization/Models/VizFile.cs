using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SimOpt.Engine.Api.Models;
using SimOpt.Engine.Api.Simulation;

namespace SimOpt.Visualization.Models;

/// <summary>
/// What File → Open found: the document, the run's length and warm-up when the file declares them (a run
/// record does), its kind ("model" or "run record"), or why it is no model (then the rest is null).
/// </summary>
public sealed record VizOpened(VizDocument? Document, double? Duration, double? Warmup, string Kind, string? Error)
{
    public static VizOpened Failed(string error) => new(null, null, null, string.Empty, error);
}

/// <summary>
/// SIM-110 step 4 (Q-VIEW-PATH): the files the viewer opens and saves. It opens what the MCP head writes, a
/// schema v1 document (<c>save_model</c>, <c>export_model</c>) or a <c>simopt-run/1</c> record
/// (<c>run_experiment</c>, <c>optimize</c> with <c>record</c>), whose call gives the run's length and warm-up. A
/// document may carry a top-level <c>layout</c> block, by node id (<c>x</c>, <c>y</c>, <c>width</c>, <c>height</c>
/// in metres, <c>label</c>, <c>color</c>), which the builder and the MCP head's <c>load_model</c> ignore, so a model
/// saved from the viewer reopens where it was drawn and keeps its <c>model_sha256</c>.
/// </summary>
public static class VizFile
{
    public const string RunRecordFormat = "simopt-run/1";

    /// <summary>As the MCP head reads a document (web defaults: names as written, any case).</summary>
    private static readonly JsonSerializerOptions ReadOptions = new(JsonSerializerDefaults.Web);

    /// <summary>As <see cref="ModelDocument"/> writes one: indented, line feeds only, no empty optional fields.</summary>
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        NewLine = "\n",
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed class LayoutEntry
    {
        [JsonPropertyName("x")] public double? X { get; set; }
        [JsonPropertyName("y")] public double? Y { get; set; }
        [JsonPropertyName("width")] public double? Width { get; set; }
        [JsonPropertyName("height")] public double? Height { get; set; }
        [JsonPropertyName("label")] public string? Label { get; set; }
        [JsonPropertyName("color")] public string? Color { get; set; }
    }

    /// <summary>Reads a file's text; never throws for a file that is no model, it says why in <see cref="VizOpened.Error"/>.</summary>
    public static VizOpened Read(string text)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(text ?? string.Empty);
        }
        catch (JsonException e)
        {
            return VizOpened.Failed($"The file is not JSON: {e.Message}");
        }
        if (root is not JsonObject file) return VizOpened.Failed("The file is not a JSON object, so it is no model.");

        JsonObject model = file;
        string kind = "model";
        double? duration = null, warmup = null;
        if (file.ContainsKey("record"))
        {
            string? format = file["record"] is JsonValue v && v.TryGetValue(out string? f) ? f : null;
            if (format != RunRecordFormat)
                return VizOpened.Failed($"A record of format \"{format}\" cannot be opened; {RunRecordFormat} records can.");
            if (file["model"] is not JsonObject recorded)
                return VizOpened.Failed($"The run record holds no model; a {RunRecordFormat} record has one under \"model\".");
            model = recorded;
            kind = "run record";
            JsonObject? call = file["call"] as JsonObject;
            duration = Number(call?["duration"]);
            warmup = Number(call?["warmup"]);
        }

        if (!model.ContainsKey("nodes"))
            return VizOpened.Failed("The file has no \"nodes\", so it is no model.");

        Dictionary<string, NodeLayout> layout;
        try
        {
            layout = (model["layout"]?.Deserialize<Dictionary<string, LayoutEntry>>(ReadOptions) ?? new())
                .ToDictionary(e => e.Key, e => new NodeLayout(e.Value.X, e.Value.Y, e.Value.Width, e.Value.Height, e.Value.Label, e.Value.Color));
        }
        catch (JsonException e)
        {
            return VizOpened.Failed($"The layout block cannot be read: {e.Message}");
        }

        var topology = (JsonObject)model.DeepClone();
        topology.Remove("layout");
        TopologyDefinition? definition;
        try
        {
            definition = topology.Deserialize<TopologyDefinition>(ReadOptions);
        }
        catch (JsonException e)
        {
            return VizOpened.Failed($"The file is not a topology document: {e.Message}");
        }
        return definition == null
            ? VizOpened.Failed("The file holds no topology.")
            : new VizOpened(new VizDocument(definition, layout), duration, warmup, kind, null);
    }

    /// <summary>
    /// The document as <c>save_model</c> writes it, followed by its layout block when it has one; without a
    /// layout the bytes are <c>save_model</c>'s.
    /// </summary>
    public static string Write(VizDocument document)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        string saved = ModelDocument.Write(document.Topology);
        if (document.Layout.Count == 0) return saved;

        var file = (JsonObject)JsonNode.Parse(saved)!;
        var layout = new JsonObject();
        foreach (var (id, at) in document.Layout)
            layout[id] = JsonSerializer.SerializeToNode(
                new LayoutEntry { X = at.X, Y = at.Y, Width = at.Width, Height = at.Height, Label = at.Label, Color = at.Color }, WriteOptions);
        file["layout"] = layout;
        return file.ToJsonString(WriteOptions);
    }

    private static double? Number(JsonNode? node) =>
        node is JsonValue v && v.TryGetValue(out double d) ? d : null;
}

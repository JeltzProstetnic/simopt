using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using SimOpt.McpServer.Models;

namespace SimOpt.McpServer.Simulation
{
    /// <summary>
    /// SIM-67 and SIM-86: a model version as its schema v1 document, the bytes <c>save_model</c> writes,
    /// with the SHA-256 that names it in a result (UN-034) and the node parameters it leaves to the
    /// catalogue's defaults (UN-035).
    /// </summary>
    public static class ModelDocument
    {
        /// <summary>
        /// Indented, without empty optional fields, and with line feeds only, so the same model gives the
        /// same bytes, and so the same hash, on every operating system (UN-033).
        /// </summary>
        private static readonly JsonSerializerOptions Options = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = true,
            NewLine = "\n",
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>The topology as a schema v1 document, stamped "1" when it carried no version.</summary>
        public static string Write(TopologyDefinition topology)
        {
            TopologyDefinition copy = TopologyPatcher.Copy(topology);
            copy.SchemaVersion ??= SchemaCatalog.SchemaVersion;
            return JsonSerializer.Serialize(copy, Options);
        }

        /// <summary>The SHA-256 of <see cref="Write"/> in UTF-8, as 64 lowercase hex digits: <c>sha256sum</c> of the saved file.</summary>
        public static string Sha256(TopologyDefinition topology) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Write(topology)))).ToLowerInvariant();

        /// <summary>
        /// Every node parameter the model leaves out and the builder fills from the catalogue, by patch path
        /// ("q.capacity"), with the value used. A parameter given by its older alternative counts as given.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Defaults(TopologyDefinition topology)
        {
            foreach (NodeDefinition node in topology.Nodes)
            {
                string? type = SchemaCatalog.Canonical(node.Type);
                NodeTypeSpec? spec = SchemaCatalog.NodeTypes.FirstOrDefault(t => t.Name == type);
                if (spec == null) continue;
                foreach (ParameterSpec p in spec.Parameters.Where(p => p.Default != null))
                    if (!node.Params.ContainsKey(p.Name) && (p.Alternative == null || !node.Params.ContainsKey(p.Alternative)))
                        yield return new KeyValuePair<string, string>($"{node.Id}.{p.Name}", p.Default!);
            }
        }
    }
}

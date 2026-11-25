using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using SimOpt.Engine.Api.Models;

namespace SimOpt.McpServer.Simulation
{
    /// <summary>SIM-86 slice C: what <c>report_run</c> answers.</summary>
    public sealed class RunReportResult
    {
        [JsonPropertyName("language")] public string Language { get; set; } = "en";
        [JsonPropertyName("record_intact")] public bool RecordIntact { get; set; }
        [JsonPropertyName("model_intact")] public bool ModelIntact { get; set; }
        [JsonPropertyName("markdown")] public string Markdown { get; set; } = string.Empty;
    }

    /// <summary>
    /// SIM-86 slice C (UN-034, Q-PROV-REPORT, MG 2026-11-25): a run record as a Markdown appendix for an expert
    /// report, in English or German: the provenance with both hashes checked, the run's settings, the results with
    /// their intervals, what the run assumed, what the model does not represent, and how to repeat the run. It
    /// reads the record and runs nothing; <c>reproduce_run</c> repeats it. The figures are the recorded ones, which
    /// carry four significant digits. Turning the Markdown into PDF or Word is left to the reader's own tools.
    /// </summary>
    public static class RunReport
    {
        public static IReadOnlyList<string> Languages { get; } = new[] { "en", "de" };

        /// <summary>The German text of each limit of schema v1, keyed by its English text (<see cref="SchemaCatalog.Limits"/>).</summary>
        public static IReadOnlyDictionary<string, string> GermanLimits { get; } = new Dictionary<string, string>
        {
            [SchemaCatalog.Limits[0]] = "Einheiten wechseln ohne Zeitverzug zwischen verbundenen Knoten; Wege, Transport und Handhabung brauchen nur dort Zeit, wo ein Verzögerungsknoten oder ein Verzögerungsschritt steht.",
            [SchemaCatalog.Limits[1]] = "Stationen fallen nie aus, und ihre Bedienstellen arbeiten ohne Schichten, Pausen oder Rüstvorgänge.",
            [SchemaCatalog.Limits[2]] = "Ankünfte folgen über den ganzen Lauf einer einzigen Verteilung: kein Tagesgang und keine Spitzenzeiten.",
            [SchemaCatalog.Limits[3]] = "Alle Einheiten sind gleich: keine Typen, Merkmale oder Prioritäten; eine Warteschlange bedient in Ankunftsreihenfolge oder in umgekehrter Reihenfolge, und eine Verzweigung berücksichtigt die Einheit nie.",
            [SchemaCatalog.Limits[4]] = "Die Bedienstellen einer Station sind gleich und bedienen jeweils eine Einheit, und eine Station teilt nichts mit einer anderen; was Tätigkeiten teilen (Personal, Plätze), ist eine Ressource, die Prozessschritte belegen, eine Einheit je Schritt.",
            [SchemaCatalog.Limits[5]] = "Ein Prozess hat keine Schleifen oder Bedingungen, und eine Einheit hält Ressourcen nur innerhalb eines Prozessknotens; beim Verlassen gibt sie alles frei, was sie noch hält.",
            [SchemaCatalog.Limits[6]] = "Eine Einheit wartet in ihrer Warteschlange, bis sie bedient wird, und geht nie vorzeitig, außer an einem Belegungs- oder Entnahmeschritt mit Geduld; eine Einheit, die eine volle Warteschlange abweist, geht verloren und wird gezählt.",
            [SchemaCatalog.Limits[7]] = "Jede Replikation beginnt leer und untätig; nur die Einschwingphase hält diesen Anfang aus den Kennzahlen heraus.",
        };

        private sealed record Words(
            CultureInfo Culture, string Title, string Intro, string Provenance, string Record, string RecordHash,
            string RecordIntegrity, string RecordIntact, string RecordChanged, string Model, string Version, string ModelHash,
            string ModelIntegrity, string ModelIntact, string ModelChanged, string ToolCall, string RecordedWith,
            string Settings, string RunLength, string Warmup, string Replications, string ReplicationsPerPoint, string BaseSeed,
            string Confidence, string MetricsAsked, string AllMetrics, string Budget, string Strategy, string Evaluations,
            string Space, string Stopped, string SeedNote, string Scenario, string MadeBy, string Metric, string Mean,
            string Interval, string Difference, string Significant, string Yes, string No, string NotRun, string Saturated,
            string Refused, string NothingFinished, string NoData, string Objective, string Minimise, string Maximise,
            string SubjectTo, string Variables, string Variable, string Type, string Range, string To, string Best,
            string Value, string ObjectiveValue, string Feasible, string Violations, string Others, string Rank, string Point,
            string Assumed, string AssumedIntro, string TakenAs, string NothingAssumed, string Limits, string LimitsIntro,
            string AsRecordedInEnglish, string Repeat, string RepeatMcp, string RepeatPython, string RepeatModel,
            IReadOnlyDictionary<string, string> Rules);

        private static readonly Words English = new(
            CultureInfo.InvariantCulture,
            "Simulation run report",
            "Rendered from the run record `{0}` by {1} (`report_run`). The figures are the recorded ones, to four significant digits.",
            "Provenance", "Record", "Record hash (SHA-256)", "Record integrity",
            "Intact: the record still hashes to its record_sha256.",
            "**changed after it was written**: the record no longer hashes to its record_sha256, so its figures cannot be relied on.",
            "Model", "version", "Model hash (SHA-256)", "Model integrity",
            "Intact: the model in the record hashes to the model_sha256 the result names.",
            "**not the model the result names**: the model in the record does not hash to model_sha256.",
            "Tool call", "Recorded with",
            "Run settings", "Run length", "Warm-up", "Replications", "Replications per point", "Base seed", "Confidence",
            "Metrics asked for", "all", "Evaluation budget", "Strategy", "Evaluations", "Search space", "Stopped",
            "Replication r runs at a seed derived from the base seed (ReplicationRunner.SeedFor), so the same base seed gives the same replications on any machine.",
            "Scenario", "Made from the model by", "Metric", "Mean", "95 % interval", "Difference from baseline", "Significant",
            "yes", "no", "Not run", "Saturated stations", "Items refused (mean per replication)", "Nothing reached a sink.",
            "No replication measured",
            "Objective", "Minimise", "Maximise", "subject to", "Decision variables", "Variable", "Type", "Range", "to",
            "Best configuration found", "Value", "Objective value", "Feasible", "Constraints not met", "Other configurations, ranked",
            "Rank", "Configuration",
            "What the run assumed", "Values the run used without being given them, as its result lists them (UN-035):",
            "Taken as", "Nothing: every value was given.",
            "What the model does not represent", "The limits of schema v1, as recorded with the run (UN-035):",
            "as recorded, in English",
            "How to repeat it",
            "With the SimOpt MCP server: call `reproduce_run` with the path of this record. It runs the recorded call on the recorded model, from the record alone, and says whether every figure came out the same.",
            "In Python: `simopt.Server().reproduce_run(\"{0}\")`.",
            "The model alone: the record's `model` object is the schema v1 document; saved as a file, `load_model` opens it, and its SHA-256 is the model hash above.",
            new Dictionary<string, string>());

        private static readonly Words German = new(
            CultureInfo.GetCultureInfo("de-AT"),
            "Bericht über einen Simulationslauf",
            "Erstellt aus dem Laufprotokoll `{0}` mit {1} (`report_run`). Die Zahlen sind die protokollierten, auf vier signifikante Stellen.",
            "Herkunft", "Protokoll", "Hashwert des Protokolls (SHA-256)", "Unversehrtheit des Protokolls",
            "Unverändert: das Protokoll ergibt weiterhin seinen record_sha256.",
            "**nach dem Schreiben verändert**: das Protokoll ergibt nicht mehr seinen record_sha256, auf seine Zahlen ist kein Verlass.",
            "Modell", "Version", "Hashwert des Modells (SHA-256)", "Unversehrtheit des Modells",
            "Unverändert: das Modell im Protokoll ergibt den model_sha256, den das Ergebnis nennt.",
            "**nicht das Modell, das das Ergebnis nennt**: das Modell im Protokoll ergibt nicht den model_sha256.",
            "Werkzeugaufruf", "Protokolliert mit",
            "Laufeinstellungen", "Laufzeit", "Einschwingphase", "Replikationen", "Replikationen je Punkt", "Basis-Startwert",
            "Konfidenzniveau", "Abgefragte Kennzahlen", "alle", "Auswertungsbudget", "Strategie", "Auswertungen", "Suchraum",
            "Beendet",
            "Replikation r läuft mit einem aus dem Basis-Startwert abgeleiteten Startwert (ReplicationRunner.SeedFor); derselbe Basis-Startwert ergibt auf jedem Rechner dieselben Replikationen.",
            "Szenario", "Aus dem Modell gebildet durch", "Kennzahl", "Mittelwert", "95-%-Intervall", "Unterschied zur Basis",
            "Signifikant", "ja", "nein", "Nicht gelaufen", "Gesättigte Stationen", "Abgewiesene Einheiten (Mittel je Replikation)",
            "Keine Einheit erreichte eine Senke.", "Von keiner Replikation gemessen",
            "Zielgröße", "Minimiere", "Maximiere", "unter den Bedingungen", "Entscheidungsvariablen", "Variable", "Typ", "Bereich",
            "bis", "Beste gefundene Konfiguration", "Wert", "Wert der Zielgröße", "Zulässig", "Nicht erfüllte Bedingungen",
            "Weitere Konfigurationen, gereiht", "Rang", "Konfiguration",
            "Was der Lauf angenommen hat", "Werte, die der Lauf ohne Angabe verwendet hat, wie sein Ergebnis sie aufführt (UN-035):",
            "Angenommen als", "Nichts: alle Werte waren angegeben.",
            "Was das Modell nicht abbildet", "Die Grenzen von Schema v1, wie mit dem Lauf protokolliert (UN-035):",
            "wie protokolliert, auf Englisch",
            "Wie man den Lauf wiederholt",
            "Mit dem SimOpt-MCP-Server: `reproduce_run` mit dem Pfad dieses Protokolls aufrufen. Es führt den protokollierten Aufruf am protokollierten Modell aus, allein aus dem Protokoll, und sagt, ob jede Zahl gleich herauskam.",
            "In Python: `simopt.Server().reproduce_run(\"{0}\")`.",
            "Das Modell allein: das Objekt `model` im Protokoll ist das Schema-v1-Dokument; als Datei gespeichert, öffnet es `load_model`, und sein SHA-256 ist der oben genannte Hashwert des Modells.",
            new Dictionary<string, string>
            {
                ["a tenth of the duration"] = "ein Zehntel der Laufzeit",
                ["the model's"] = "wie im Modell",
                ["unlimited"] = "unbegrenzt",
                ["the default set; the model declares none"] = "der Standardsatz; das Modell gibt keine an",
            });

        /// <summary>Renders the record at <paramref name="path"/> in <paramref name="language"/> ("en" or "de").</summary>
        /// <exception cref="ArgumentException">An unknown language, no such file, or the file is not a run record.</exception>
        public static RunReportResult Render(string path, string? language = null)
        {
            string lang = (language ?? "en").Trim().ToLowerInvariant();
            if (!Languages.Contains(lang))
                throw new ArgumentException($"language \"{language}\" is not offered; use one of {string.Join(", ", Languages)}.");
            Words w = lang == "de" ? German : English;
            RunRecordFile.Opened o = RunRecordFile.Open(path);
            string file = Path.GetFileName(o.FullPath);
            JsonObject result = o.Result, call = o.Call;
            var md = new StringBuilder();
            void Line(string text = "") => md.Append(text).Append('\n');
            void Row(string a, string b) => Line($"| {a} | {b} |");
            string N(JsonNode? node) => Number(node, w.Culture);

            Line($"# {w.Title}");
            Line();
            Line(string.Format(w.Intro, file, ExperimentRunner.ToolVersion));
            Line();

            Line($"## {w.Provenance}");
            Line();
            Line("| | |");
            Line("|---|---|");
            Row(w.Record, $"`{file}`, {RunRecordFile.Format}");
            Row(w.RecordHash, $"`{Text(o.Record["record_sha256"]) ?? "–"}`");
            Row(w.RecordIntegrity, o.RecordIntact ? w.RecordIntact : w.RecordChanged);
            Row(w.Model, $"{o.Topology.Name}, {w.Version} {N(result["version"])}");
            Row(w.ModelHash, $"`{Text(result["model_sha256"]) ?? "–"}`");
            Row(w.ModelIntegrity, o.ModelIntact ? w.ModelIntact : w.ModelChanged);
            Row(w.ToolCall, $"`{o.ToolCall}`");
            Row(w.RecordedWith, Text(result["tool"]) ?? "–");
            Line();

            bool search = o.ToolCall == "optimize";
            Line($"## {w.Settings}");
            Line();
            Line("| | |");
            Line("|---|---|");
            Row(w.RunLength, N(call["duration"]));
            Row(w.Warmup, N(call["warmup"]));
            if (search)
            {
                Row(w.ReplicationsPerPoint, N(call["replications_per_eval"]));
                Row(w.BaseSeed, N(call["seed"]));
                Row(w.Budget, N(call["max_evaluations"]));
                Row(w.Strategy, Text(result["strategy"]) ?? "–");
                Row(w.Evaluations, N(result["evaluations"]));
                if (result["space"] != null) Row(w.Space, N(result["space"]));
                Row(w.Stopped, Text(result["stopped"]) ?? "–");
            }
            else
            {
                Row(w.Replications, N(call["replications"]));
                Row(w.BaseSeed, N(call["seed"]));
                if (result["confidence"] is JsonValue c && c.TryGetValue(out double confidence))
                    Row(w.Confidence, (confidence * 100).ToString(w.Culture) + " %");
                Row(w.MetricsAsked, call["metrics"] is JsonArray asked ? string.Join(", ", asked.Select(m => Text(m))) : w.AllMetrics);
            }
            Line();
            Line(w.SeedNote);
            Line();

            if (search) RenderSearch(md, w, call, result);
            else RenderScenarios(md, w, result);

            Line($"## {w.Assumed}");
            Line();
            if (result["assumed"] is JsonObject assumed && assumed.Count > 0)
            {
                Line(w.AssumedIntro);
                Line();
                Line($"| {w.Value} | {w.TakenAs} |");
                Line("|---|---|");
                foreach (var (key, node) in assumed)
                {
                    string rule = Text(node) ?? node?.ToJsonString() ?? "–";
                    string taken = w.Rules.GetValueOrDefault(rule, rule);
                    if (key is "warmup" or "seed" && call[key] != null) taken += ": " + N(call[key]);
                    Row(key, taken);
                }
            }
            else Line(w.NothingAssumed);
            Line();

            Line($"## {w.Limits}");
            Line();
            Line(w.LimitsIntro);
            Line();
            foreach (JsonNode? limit in o.Record["limits"] as JsonArray ?? new JsonArray())
            {
                string text = Text(limit) ?? string.Empty;
                if (lang == "de")
                    text = GermanLimits.TryGetValue(text, out string? german) ? german : $"{text} ({w.AsRecordedInEnglish})";
                Line($"- {text}");
            }
            Line();

            Line($"## {w.Repeat}");
            Line();
            Line($"1. {w.RepeatMcp}");
            Line($"2. {string.Format(w.RepeatPython, file)}");
            Line($"3. {w.RepeatModel}");

            return new RunReportResult
            {
                Language = lang,
                RecordIntact = o.RecordIntact,
                ModelIntact = o.ModelIntact,
                Markdown = md.ToString(),
            };
        }

        private static void RenderScenarios(StringBuilder md, Words w, JsonObject result)
        {
            void Line(string text = "") => md.Append(text).Append('\n');
            foreach (JsonObject scenario in (result["scenarios"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
            {
                Line($"## {w.Scenario}: {Text(scenario["name"])}");
                Line();
                if (scenario["patch"] is JsonArray patch && patch.Count > 0)
                {
                    Line($"{w.MadeBy}: {string.Join("; ", patch.Select(op => $"`{Operation(op, w.Culture)}`"))}");
                    Line();
                }
                if (scenario["errors"] is JsonArray errors && errors.Count > 0)
                {
                    Line($"{w.NotRun}: {string.Join(" ", errors.Select(e => Text(e)))}");
                    Line();
                    continue;
                }
                MetricTable(md, w, scenario["metrics"] as JsonObject);
                if (scenario["vs_baseline"] is JsonObject differences && differences.Count > 0)
                {
                    Line($"| {w.Metric} | {w.Difference} | {w.Interval} | {w.Significant} |");
                    Line("|---|---|---|---|");
                    foreach (var (metric, node) in differences)
                        if (node is JsonObject d)
                            Line($"| {metric} | {Number(d["diff"], w.Culture)} | {Interval(d["ci95"], w.Culture)} | " +
                                 $"{(d["significant"] is JsonValue s && s.TryGetValue(out bool yes) && yes ? w.Yes : w.No)} |");
                    Line();
                }
                Flags(md, w, scenario["flags"] as JsonObject);
            }
        }

        private static void RenderSearch(StringBuilder md, Words w, JsonObject call, JsonObject result)
        {
            void Line(string text = "") => md.Append(text).Append('\n');
            CultureInfo c = w.Culture;

            Line($"## {w.Objective}");
            Line();
            if (call["objective"] is JsonObject objective)
            {
                string direction = Text(objective["direction"]) == "max" ? w.Maximise : w.Minimise;
                string text = $"{direction} `{Text(objective["metric"])}`";
                if (objective["constraints"] is JsonArray constraints && constraints.Count > 0)
                    text += $", {w.SubjectTo}: " + string.Join("; ", constraints.OfType<JsonObject>()
                        .Select(k => $"`{Text(k["metric"])} {Text(k["op"])} {Number(k["value"], c)}`"));
                Line(text);
                Line();
            }

            Line($"## {w.Variables}");
            Line();
            Line($"| {w.Variable} | {w.Type} | {w.Range} |");
            Line("|---|---|---|");
            foreach (JsonObject v in (call["variables"] as JsonArray ?? new JsonArray()).OfType<JsonObject>())
                Line($"| {Text(v["path"])} | {Text(v["type"]) ?? "–"} | {Number(v["min"], c)} {w.To} {Number(v["max"], c)} |");
            Line();

            Line($"## {w.Best}");
            Line();
            if (result["best"] is JsonObject best)
            {
                Line($"| {w.Variable} | {w.Value} |");
                Line("|---|---|");
                foreach (var (variable, value) in best["point"] as JsonObject ?? new JsonObject())
                    Line($"| {variable} | {Number(value, c)} |");
                Line();
                Line($"{w.ObjectiveValue}: {Number(best["objective"], c)}; {w.Feasible}: " +
                     $"{(best["feasible"] is JsonValue f && f.TryGetValue(out bool feasible) && feasible ? w.Yes : w.No)}");
                Line();
                MetricTable(md, w, best["metrics"] as JsonObject);
                if (best["violations"] is JsonArray violations && violations.Count > 0)
                {
                    Line($"{w.Violations}: {string.Join("; ", violations.Select(v => Text(v) ?? v?.ToJsonString()))}");
                    Line();
                }
            }

            if (result["top"] is JsonArray top && top.Count > 1)
            {
                Line($"## {w.Others}");
                Line();
                Line($"| {w.Rank} | {w.Point} | {w.ObjectiveValue} | {w.Feasible} |");
                Line("|---|---|---|---|");
                int rank = 0;
                foreach (JsonObject point in top.OfType<JsonObject>())
                {
                    rank++;
                    string at = string.Join(", ", (point["point"] as JsonObject ?? new JsonObject()).Select(p => $"{p.Key} = {Number(p.Value, c)}"));
                    string feasible = point["feasible"] is JsonValue f && f.TryGetValue(out bool ok) && ok ? w.Yes : w.No;
                    Line($"| {rank} | {at} | {Number(point["objective"], c)} | {feasible} |");
                }
                Line();
            }
        }

        private static void MetricTable(StringBuilder md, Words w, JsonObject? metrics)
        {
            if (metrics == null || metrics.Count == 0) return;
            md.Append($"| {w.Metric} | {w.Mean} | {w.Interval} |\n|---|---|---|\n");
            foreach (var (metric, node) in metrics)
                if (node is JsonObject m)
                    md.Append($"| {metric} | {Number(m["mean"], w.Culture)} | {Interval(m["ci95"], w.Culture)} |\n");
            md.Append('\n');
        }

        private static void Flags(StringBuilder md, Words w, JsonObject? flags)
        {
            if (flags == null) return;
            var notes = new List<string>();
            if (flags["saturated"] is JsonArray saturated) notes.Add($"{w.Saturated}: {string.Join(", ", saturated.Select(s => Text(s)))}");
            if (flags["refused"] is JsonObject refused)
                notes.Add($"{w.Refused}: {string.Join(", ", refused.Select(r => $"{r.Key} {Number(r.Value, w.Culture)}"))}");
            if (flags["nothing_finished"] != null) notes.Add(w.NothingFinished);
            if (flags["no_data"] is JsonArray noData) notes.Add($"{w.NoData}: {string.Join(", ", noData.Select(n => Text(n)))}");
            foreach (string note in notes) md.Append($"- {note}\n");
            if (notes.Count > 0) md.Append('\n');
        }

        /// <summary>"set arrivals.arrival.mean = 4" for a set; any other operation as its JSON.</summary>
        private static string Operation(JsonNode? op, CultureInfo culture)
        {
            if (op is JsonObject o && Text(o["op"]) == "set" && Text(o["path"]) is string path)
                return $"set {path} = {(o["value"] is JsonValue v && v.TryGetValue(out double d) ? d.ToString(culture) : o["value"]?.ToJsonString())}";
            return op?.ToJsonString() ?? "–";
        }

        private static string Interval(JsonNode? node, CultureInfo culture)
        {
            if (node is not JsonArray ci || ci.Count != 2) return "–";
            string separator = culture.NumberFormat.NumberDecimalSeparator == "," ? "; " : ", ";
            return $"[{Number(ci[0], culture)}{separator}{Number(ci[1], culture)}]";
        }

        private static string Number(JsonNode? node, CultureInfo culture) =>
            node is JsonValue v && v.TryGetValue(out double d) ? d.ToString(culture) : node == null ? "–" : node.ToJsonString();

        private static string? Text(JsonNode? node) =>
            node is JsonValue value && value.TryGetValue(out string? text) ? text : null;
    }
}

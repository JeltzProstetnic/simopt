using System.Collections.Generic;
using System.Linq;
using SimOpt.Engine.Api.Models;

namespace SimOpt.Visualization.Models;

/// <summary>
/// SIM-110 step 4 (UN-008): what the check found in a model, as the viewer shows it: each problem at the node
/// it names, errors before warnings, and the problems that name no drawn node in a general list.
/// </summary>
public sealed class ModelProblems
{
    private ModelProblems(IReadOnlyDictionary<string, IReadOnlyList<ValidationIssue>> byNode,
                          IReadOnlySet<string> errors, IReadOnlyList<ValidationIssue> general, bool hasErrors)
    {
        ByNode = byNode;
        Errors = errors;
        General = general;
        HasErrors = hasErrors;
    }

    /// <summary>The problems at each drawn node that has any, errors first.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<ValidationIssue>> ByNode { get; }

    /// <summary>The drawn nodes with at least one error.</summary>
    public IReadOnlySet<string> Errors { get; }

    /// <summary>The problems that name no drawn node, errors first.</summary>
    public IReadOnlyList<ValidationIssue> General { get; }

    /// <summary>The model has errors, so it was not built and cannot run.</summary>
    public bool HasErrors { get; }

    public int Count => ByNode.Values.Sum(p => p.Count) + General.Count;

    public static ModelProblems Of(ValidationReport report, VizTopology drawing)
    {
        var drawn = drawing.Nodes.Select(n => n.Id).ToHashSet();
        var byNode = new Dictionary<string, List<ValidationIssue>>();
        var errors = new HashSet<string>();
        var general = new List<ValidationIssue>();
        foreach (var (issue, isError) in report.Errors.Select(e => (e, true)).Concat(report.Warnings.Select(w => (w, false))))
        {
            if (issue.Node != null && drawn.Contains(issue.Node))
            {
                if (!byNode.TryGetValue(issue.Node, out List<ValidationIssue>? at)) byNode[issue.Node] = at = new List<ValidationIssue>();
                at.Add(issue);
                if (isError) errors.Add(issue.Node);
            }
            else general.Add(issue);
        }
        return new ModelProblems(byNode.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<ValidationIssue>)kv.Value), errors, general,
                                 report.Errors.Count > 0);
    }
}

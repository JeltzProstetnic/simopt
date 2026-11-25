using System;
using System.Linq;
using SimOpt.Engine.Api.Models;

namespace SimOpt.Engine.Api.Simulation
{
    /// <summary>
    /// SIM-66 (D1): one fault the builder found, carrying it as a <see cref="ValidationIssue"/>. The
    /// builder catches it per node, connection, router and metric and carries on, so every fault of a
    /// topology is reported at once.
    /// </summary>
    public sealed class TopologyException : InvalidOperationException
    {
        public TopologyException(string code, string? node, string message, string? suggestion = null)
            : this(new ValidationIssue(code, message, node, suggestion))
        {
        }

        public TopologyException(ValidationIssue issue) : base(issue.Line)
        {
            Issue = issue;
        }

        public ValidationIssue Issue { get; }
    }

    /// <summary>
    /// SIM-66 (D1): a topology that cannot be built, with every error found in it. The message lists
    /// them one per line, diagnosis and fix together, so a caller reading only the message loses
    /// nothing.
    /// </summary>
    public sealed class ModelValidationException : InvalidOperationException
    {
        public ModelValidationException(ValidationReport report)
            : base(string.Join("\n", report.Errors.Select(e => e.Line)))
        {
            Report = report;
        }

        public ValidationReport Report { get; }
    }
}

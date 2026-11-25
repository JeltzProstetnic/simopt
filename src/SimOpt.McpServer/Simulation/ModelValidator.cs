using SimOpt.McpServer.Models;

namespace SimOpt.McpServer.Simulation
{
    /// <summary>
    /// SIM-66 (D1-D6): every layer of validation, in order. The builder's checks and the structural
    /// lint (errors stop here), the offered load of every station, then a trial run on a copy. All of
    /// it is collected into one report, so a client fixes a model in one round instead of one fault
    /// per call (UN-008).
    /// </summary>
    public static class ModelValidator
    {
        public static ValidationReport Validate(TopologyDefinition topology) => Validate(topology, out _);

        /// <param name="built">The model built from the topology, untouched by the trial; null when it has errors.</param>
        public static ValidationReport Validate(TopologyDefinition topology, out ActiveModel? built)
        {
            var report = new ValidationReport();
            built = ModelRegistry.TryBuild(topology, report);
            if (built != null) TrialRun.Check(topology, report);
            if (!report.Valid) built = null;
            return report;
        }
    }
}

namespace PrimitiveForager;

// Read-only replay records. None of these fields is controller memory or a
// classifier for native meaning; they are projections of published witnesses.
public record Evidence(string Id, string Label, string BasisLabel, string PublicationText,
    string State, bool Queried, bool Used, string? LossText, double QueryCost, string WitnessRef);
public record PhysicalFrame(int X, int PeakX, int Charge, int Heat, bool Cloud, bool Absent, bool Unavailable);
public record DecisionFrame(string RuleLabel, string[] UsedEvidenceIds, string[] UnresolvedEvidenceIds);
public record ForagerStep(int Tick, PhysicalFrame World, string ActionLabel, string Reason, string ModeLabel,
    int? FastScore, int? SlowScore, string FastState, string SlowState, Evidence[] Evidence,
    DecisionFrame Decision, int QueryCount, double QueryCost, int Action, int Heading, int Mode, int Wait,
    int ComparisonQueries, int SpatialQueries, int FineQueries, string NativeSignature);
public record ForagerSummary(int Ticks, int Cycles, int StarvationTicks, int OverheatTicks, int Movements,
    double MeanCharge, int SpatialQueries, int FineQueries, int ComparisonQueries, double QueryCost,
    double ElapsedMs, long AllocatedBytes, string TrajectoryHash, Dictionary<string, int> CapabilityUses,
    string ArchiveFile, long ArchiveBytes, bool ArchiveVerified);
public record ForagerRun(string Id, string Label, string RemovalLabel, int Seed, string Backend,
    string Encoding, bool Mirrored, ForagerSummary Summary, ForagerStep[] Steps);
public record Diagnostic(string Title, string Capability, bool Passed, string Description, string Full, string Without);
public record Episode(string RunId, int Tick, string Title, string Description);
public record MatchedComparison(string Label, int Runs, int Cycles, int StarvationTicks, int OverheatTicks,
    int SpatialQueries, int FineQueries, int ComparisonQueries, double QueryCost, double MeanCharge);

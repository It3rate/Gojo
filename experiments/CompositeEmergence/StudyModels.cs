using PrimitiveLearning;

namespace CompositeEmergence;

public sealed record StudySettings(int Seeds, int SeedStart, int DiscoveryTrain,
    int ConsumerTrain, int TestEpisodes, int RefreshTrain, int RefreshTest,
    double SwitchProbability, int ProbePrice, bool Pilot)
{
    public static StudySettings Small => new(2, 0, 384, 128, 256, 768, 768, .04, 2, true);
    public static StudySettings Campaign => new(8, 200, 768, 192, 768, 2048, 2048, .04, 2, false);
}

public sealed record StudyResult(string Stage, string Variant, string Condition, int Seed,
    int TrainEpisodes, int TestEpisodes, int Correct, int Answered, int Queries,
    long ReturnSum, double MeanReturn, double Accuracy, double Coverage, double QueryRate,
    string SelectedDescription, string LearnedDigest, GraphCounts Graph,
    int Switches = 0, double MeanRecovery = 0, int Unrecovered = 0,
    int FeatureStates = 0, int CandidateCount = 0);

public sealed record CurvePoint(string Stage, string Variant, int Seed, int Episode,
    double MeanReturn, double Accuracy, double QueryRate);

public sealed record TracePoint(string Stage, int Index, int PrivateRule, int A, int B,
    int Goal, int Wiring, long? Feature, bool FeatureAvailable, int AgeBucket,
    int LastOutcome, bool Probe, int Action, bool Correct, int Resource, bool Switched,
    int EventDelay, string SelectedDescription);

public sealed record ArchiveCheck(string Model, bool SignatureMatch, bool BehaviorMatch,
    long Bytes, string Path);

public sealed record StudyData(StudySettings Settings, List<StudyResult> Results,
    List<CurvePoint> Curves, List<TracePoint> Trace, List<ArchiveCheck> Archives,
    object[] DiscoveryCandidates, string Status, string StartedUtc, string FinishedUtc,
    double ElapsedSeconds);

using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// One pair of independent drives read through two explicit sampling boundaries.
/// Joint samples retain diagonal chords; component samples retain the intermediate
/// corners of a square wave. No transition, emission, reach or start is changed.
/// A third fixture changes the actual root recurrence, rather than its reader.
/// </summary>
public sealed class PrimitiveSamplingStudy
{
    // study = (joint-fixture -> component-sampled-fixture) -> slower-fixture
    // capture = study -> Closed(joint-run, component-run, slower-run)
    // run = (fixture -> trace) -> (analysis -> ((words -> word-reads) -> unresolved))
    // word-read = (actual-sample -> actual-delta-read) -> actual-reduced-word
    // These are observations of separately identified executions. Sharing a
    // source law neither identifies their events nor duplicates material flow.
    public FriezeFixture Combined { get; }
    public FriezeFixture Separated { get; }
    public FriezeFixture Slower { get; }
    public Relation Surface { get; }

    public PrimitiveSamplingStudy(QualitativeMotionFrame? frame = null,
        IElement? horizontalReach = null, IElement? verticalReach = null)
    {
        frame ??= new QualitativeMotionFrame();
        horizontalReach ??= new Placeholder(); verticalReach ??= new Placeholder();
        Combined = FriezeFixture.CombinedDriveZigzag(frame: frame,
            horizontalReach: horizontalReach, verticalReach: verticalReach);
        Separated = Combined.WithSampling(S(E(Combined.Network.Ports)
            .Where(port => !ReferenceEquals(port, Combined.RootPort)).ToArray()),
            "Same drives / separate component samples");
        Slower = FriezeFixture.CombinedDriveZigzag(true, frame, horizontalReach, verticalReach);
        Surface = new(new Relation(Combined.Surface, Separated.Surface), Slower.Surface);
    }

    /// <summary>The host bound selects an observation window, not a stored time
    /// or distance. All event routing and sampling remain in the native fixtures.</summary>
    public PrimitiveSamplingCapture Capture(int rounds, int eventBudget = 64)
    {
        var combined = Observe(Combined, rounds, eventBudget);
        var separated = Observe(Separated, rounds, eventBudget);
        var slower = Observe(Slower, rounds, eventBudget);
        return new(combined, separated, slower,
            new Relation(Surface, S(combined.Surface, separated.Surface, slower.Surface)));
    }

    /// <summary>Retains actual sample-to-delta correspondences. An unresolved
    /// emission yields an open word; an unfinished cascade yields no fabricated
    /// combined sample. The unresolved evidence is retained in either case.</summary>
    public static PrimitiveSamplingRun Observe(FriezeFixture fixture, int rounds, int eventBudget = 64)
    {
        var trace = fixture.Run(rounds, eventBudget);
        var analysis = FriezeDriveAnalysis.Evaluate(fixture, trace);
        if (!FriezeDriveAnalysis.TryReadResult(fixture, analysis, out var clauses, out var unresolved))
            throw new InvalidOperationException("The native drive analysis did not retain its trace contract.");
        var words = new List<IElement>(); var reads = new List<IElement>();
        foreach (var clause in E(clauses).Cast<Relation>())
        {
            if (!FriezeDriveAnalysis.TryReadSample(fixture, clause, out var sample, out _, out _, out _,
                    out var delta, out _, out _) ||
                !fixture.Frame.TryReadResult(delta, out var word, out _, out _))
                throw new InvalidOperationException("An actual drive sample lost its native delta correspondence.");
            words.Add(word); reads.Add(new Relation(new Relation(sample, delta), word));
        }
        var sampleWords = S(words.ToArray()); var wordReads = S(reads.ToArray());
        var surface = new Relation(new Relation(fixture.Surface, trace),
            new Relation(analysis, new Relation(new Relation(sampleWords, wordReads), unresolved)));
        return new(fixture, trace, analysis, sampleWords, wordReads, unresolved, surface);
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
}

/// <summary>Presentation-friendly handles onto the connected native run, not a
/// second motion program or a geometric classification.</summary>
public sealed record PrimitiveSamplingRun(FriezeFixture Fixture, Relation Trace, Relation Analysis,
    Relation SampleWords, Relation WordReads, Relation Unresolved, Relation Surface)
{
    public Relation NativeSource => Surface;
}

public sealed record PrimitiveSamplingCapture(PrimitiveSamplingRun Combined, PrimitiveSamplingRun Separated,
    PrimitiveSamplingRun Slower, Relation Surface)
{
    public Relation NativeSource => Surface;
}

using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Face alignment of actual applications from distinct ports within
/// one selected sample. This is neither net motion nor a magnitude correlation,
/// independence claim or aesthetic ranking. Counts concern the supplied window.</summary>
public sealed class FriezeCoordinationFeatures
{
    // Surface = (fixture-surface -> trace) -> frame-opposites
    // read = Surface -> (drive-read -> (same-face -> (opposed-face -> (conditioned -> unresolved))))
    // pair = ((actual-sample -> actual-axis) -> (first-app -> second-app)) ->
    //        (((first-port -> second-port) -> (first-receipt -> second-receipt)) ->
    //         ((first-reach -> second-reach) -> face-premise))
    // Equal-face premise aliases the same actual direction at both ends.
    // Opposed-face premise is an actual ordered row of Frame.Opposites.
    // Each pair follows its two applications' source order, once only. Different
    // reaches remain connected without becoming a magnitude/equality premise.
    // conditioned = Closed(((actual-axis -> actual-first-direction) ->
    //                       (first-port -> second-port)) -> (same-pairs -> opposed-pairs))
    public FriezeFixture Fixture { get; }
    public Relation Trace { get; }
    public Relation Surface { get; }
    public Relation DriveRead { get; }
    public Relation SameFace { get; }
    public Relation OpposedFace { get; }
    public Relation Conditioned { get; }
    public Relation Unresolved { get; }
    private readonly Relation read;
    public sealed record Summary(int? SameFacePairs, int? OpposedFacePairs, int Unresolved);

    public FriezeCoordinationFeatures(FriezeFixture fixture, Relation trace)
    {
        Fixture = fixture; Trace = trace;
        Surface = new Relation(new Relation(fixture.Surface, trace), fixture.Frame.Opposites);
        DriveRead = FriezeDriveAnalysis.Evaluate(fixture, trace);
        var same = new List<IElement>(); var opposed = new List<IElement>(); var pending = new List<IElement>();
        var groups = new List<Group>();
        var actualSamples = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        if (fixture.TryReadTrace(trace, out _, out _, out _, out _, out var sourceSamples, out _))
            foreach (var sample in E(sourceSamples)) actualSamples.Add(sample);
        if (!FriezeDriveAnalysis.TryReadResult(fixture, DriveRead, out var samples, out var drivePending)) Add(DriveRead);
        else
        {
            // A known opposed-face comparison can leave net extent unsettled
            // while the lower-resolution face question is fully readable.
            foreach (var item in E(drivePending))
                if (ReferenceEquals(item, trace) || ReferenceEquals(item, DriveRead) || !NetComparisonOnly(item)) Add(item);
            foreach (var clause in E(samples).OfType<Relation>())
            {
                if (!FriezeDriveAnalysis.TryReadSample(fixture, clause, out var sample, out _, out var apps, out _, out _, out _, out _))
                { Add(clause); continue; }
                var applications = E(apps); var known = new Application?[applications.Length];
                for (var i = 0; i < applications.Length; i++)
                {
                    if (applications[i] is Relation app && ApplicationRead(app, out var participant)) known[i] = participant;
                    else Add(applications[i]);
                }
                for (var i = 0; i < known.Length; i++)
                    for (var j = i + 1; j < known.Length; j++)
                    {
                        if (known[i] is not { } a || known[j] is not { } b || ReferenceEquals(a.Port, b.Port) ||
                            !ReferenceEquals(a.Axis, b.Axis)) continue;
                        Relation premise; bool aligned;
                        if (ReferenceEquals(a.Move.Left, b.Move.Left)) { aligned = true; premise = new Relation(a.Move.Left, b.Move.Left); }
                        else
                        {
                            var matches = E(fixture.Frame.Opposites).OfType<Relation>().Where(row =>
                                ReferenceEquals(row.Left, a.Move.Left) && ReferenceEquals(row.Right, b.Move.Left)).ToArray();
                            if (matches.Length != 1) { Add(new Relation(a.App, b.App)); continue; }
                            aligned = false; premise = matches[0];
                        }
                        var witness = new Relation(new Relation(new Relation(sample, a.Axis), new Relation(a.App, b.App)),
                            new Relation(new Relation(new Relation(a.Port, b.Port), new Relation(a.Receipt, b.Receipt)),
                                new Relation(new Relation(a.Reach, b.Reach), premise)));
                        (aligned ? same : opposed).Add(witness);
                        var group = groups.SingleOrDefault(existing => ReferenceEquals(existing.Axis, a.Axis) &&
                            ReferenceEquals(existing.Direction, a.Move.Left) && ReferenceEquals(existing.First, a.Port) && ReferenceEquals(existing.Second, b.Port));
                        if (group is null) { group = new Group(a.Axis, a.Move.Left, a.Port, b.Port, [], []); groups.Add(group); }
                        (aligned ? group.Same : group.Opposed).Add(witness);
                    }
            }
        }
        SameFace = S(same.ToArray()); OpposedFace = S(opposed.ToArray()); Unresolved = S(pending.ToArray());
        Conditioned = S(groups.Select(group => (IElement)new Relation(
            new Relation(new Relation(group.Axis, group.Direction), new Relation(group.First, group.Second)),
            new Relation(S(group.Same.ToArray()), S(group.Opposed.ToArray())))).ToArray());
        read = new Relation(Surface, new Relation(DriveRead,
            new Relation(SameFace, new Relation(OpposedFace, new Relation(Conditioned, Unresolved)))));

        void Add(IElement item) { if (!pending.Any(existing => ReferenceEquals(existing, item))) pending.Add(item); }
        bool NetComparisonOnly(IElement item)
        {
            if (item is not Relation pair) return false;
            if (pair.Left is Relation x && pair.Right is Relation y &&
                fixture.Frame.Direction(x, out var ax, out _, out _) && fixture.Frame.Direction(y, out var ay, out _, out _) &&
                ReferenceEquals(ax, ay) && E(fixture.Frame.Opposites).OfType<Relation>().Any(row =>
                    ReferenceEquals(row.Left, x.Left) && ReferenceEquals(row.Right, y.Left))) return true;
            return actualSamples.Contains(pair.Left) && pair.Right is Relation support && Closed(support) &&
                E(support).Length != 0 && E(support).All(NetComparisonOnly);
        }
    }

    public Relation Evaluate() => read;

    public bool TryReadResult(Relation candidate, out Relation sameFace, out Relation opposedFace, out Relation unresolved)
    {
        sameFace = opposedFace = unresolved = null!;
        if (!ReferenceEquals(candidate.Left, Surface) || candidate.Right is not Relation { Left: var drive, Right: Relation result } ||
            !ReferenceEquals(drive, DriveRead) || !ReferenceEquals(result.Left, SameFace) ||
            result.Right is not Relation rest || !ReferenceEquals(rest.Left, OpposedFace) ||
            rest.Right is not Relation conditions || !ReferenceEquals(conditions.Left, Conditioned) || !ReferenceEquals(conditions.Right, Unresolved) ||
            !Closed(SameFace) || !Closed(OpposedFace) || !Closed(Conditioned) || !Closed(Unresolved)) return false;
        foreach (var pair in E(SameFace).Concat(E(OpposedFace)))
            if (pair is not Relation witness || !TryReadPair(witness, out _, out _, out _, out _, out _, out _, out _)) return false;
        var covered = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var item in E(Conditioned))
        {
            if (item is not Relation group || !TryReadConditioned(group, out _, out _, out _, out _, out var a, out var b)) return false;
            foreach (var pair in E(a).Concat(E(b))) if (!covered.Add(pair)) return false;
        }
        if (E(SameFace).Concat(E(OpposedFace)).Any(pair => !covered.Contains(pair))) return false;
        sameFace = SameFace; opposedFace = OpposedFace; unresolved = Unresolved; return true;
    }

    public bool TryReadConditioned(Relation group, out Relation axis, out Relation firstDirection,
        out Relation firstPort, out Relation secondPort, out Relation sameFace, out Relation opposedFace)
    {
        axis = firstDirection = firstPort = secondPort = sameFace = opposedFace = null!;
        if (!E(Conditioned).Any(item => ReferenceEquals(item, group)) ||
            group.Left is not Relation { Left: Relation context, Right: Relation ports } ||
            context.Left is not Relation selectedAxis || context.Right is not Relation direction ||
            ports.Left is not Relation a || ports.Right is not Relation b ||
            group.Right is not Relation { Left: Relation same, Right: Relation opposed } || !Closed(same) || !Closed(opposed) ||
            E(same).Length + E(opposed).Length == 0) return false;
        foreach (var (scope, expected) in new[] { (same, SameFace), (opposed, OpposedFace) })
            foreach (var item in E(scope))
            {
                if (item is not Relation witness || !E(expected).Any(actual => ReferenceEquals(actual, witness)) ||
                    !TryReadPair(witness, out _, out var actualAxis, out var first, out _, out var actualA, out var actualB, out _) ||
                    !ReferenceEquals(actualAxis, selectedAxis) || first.Right is not Relation move || !ReferenceEquals(move.Left, direction) ||
                    !ReferenceEquals(actualA, a) || !ReferenceEquals(actualB, b)) return false;
            }
        axis = selectedAxis; firstDirection = direction; firstPort = a; secondPort = b; sameFace = same; opposedFace = opposed; return true;
    }

    public bool TryReadPair(Relation witness, out Relation sample, out Relation axis,
        out Relation firstApplication, out Relation secondApplication, out Relation firstPort,
        out Relation secondPort, out Relation premise)
    {
        sample = axis = firstApplication = secondApplication = firstPort = secondPort = premise = null!;
        var aligned = E(SameFace).Any(item => ReferenceEquals(item, witness));
        if (!aligned && !E(OpposedFace).Any(item => ReferenceEquals(item, witness)) ||
            witness.Left is not Relation { Left: Relation context, Right: Relation apps } ||
            context.Left is not Relation actualSample || context.Right is not Relation selectedAxis ||
            apps.Left is not Relation first || apps.Right is not Relation second ||
            witness.Right is not Relation { Left: Relation sources, Right: Relation extents } ||
            sources.Left is not Relation ports || sources.Right is not Relation receipts ||
            extents.Left is not Relation reaches || extents.Right is not Relation face ||
            !ApplicationRead(first, out var a) || !ApplicationRead(second, out var b) ||
            ReferenceEquals(a.Port, b.Port) || !ReferenceEquals(a.Axis, b.Axis) || !ReferenceEquals(selectedAxis, a.Axis) ||
            !ReferenceEquals(ports.Left, a.Port) || !ReferenceEquals(ports.Right, b.Port) ||
            !ReferenceEquals(receipts.Left, a.Receipt) || !ReferenceEquals(receipts.Right, b.Receipt) ||
            !Same(reaches.Left, a.Reach) || !Same(reaches.Right, b.Reach) ||
            !ReferenceEquals(face.Left, a.Move.Left) || !ReferenceEquals(face.Right, b.Move.Left) ||
            (aligned ? !ReferenceEquals(face.Left, face.Right) : !E(Fixture.Frame.Opposites).Any(row => ReferenceEquals(row, face))) ||
            !Fixture.TryReadTrace(Trace, out _, out _, out _, out _, out var samples, out _) ||
            !E(samples).Any(item => ReferenceEquals(item, actualSample)) ||
            !FriezeFixture.TryReadSample(actualSample, out _, out _, out _, out _, out var actualApps, out _)) return false;
        var entries = E(actualApps); var i = Array.FindIndex(entries, app => ReferenceEquals(app, first));
        var j = Array.FindIndex(entries, app => ReferenceEquals(app, second));
        if (i < 0 || j <= i) return false;
        sample = actualSample; axis = selectedAxis; firstApplication = first; secondApplication = second;
        firstPort = a.Port; secondPort = b.Port; premise = face; return true;
    }

    /// <summary>Incomplete counts are absent, while known witness scopes remain
    /// available as lower-bound evidence. Unresolved counts are evidence counts,
    /// not probabilities or a universal severity measure.</summary>
    public Summary Describe() => E(Unresolved).Length == 0
        ? new(E(SameFace).Length, E(OpposedFace).Length, 0)
        : new(null, null, E(Unresolved).Length);

    private sealed record Application(Relation App, Relation Move, Relation Axis, IElement Reach, Relation Port, Relation Receipt);
    private sealed record Group(Relation Axis, IElement Direction, Relation First, Relation Second,
        List<IElement> Same, List<IElement> Opposed);
    private bool ApplicationRead(Relation app, out Application application)
    {
        application = null!;
        if (app.Left is not Relation { Left: Relation receipt } || app.Right is not Relation move ||
            !Fixture.Frame.Direction(move, out var axis, out _, out var reach) || receipt.Left is not Relation { Right: var surface }) return false;
        var clocks = Fixture.Components.Where(clock => ReferenceEquals(clock.Surface, surface)).ToArray();
        if (clocks.Length != 1 || !clocks[0].TryReadAccepted(receipt, out var source, out _, out _)) return false;
        var ports = E(Fixture.Network.Ports).OfType<Relation>().Where(port => ReferenceEquals(port.Right, surface) && Same(port.Left, source)).ToArray();
        if (ports.Length != 1) return false;
        application = new(app, move, axis, reach, ports[0], receipt); return true;
    }
    private static bool Same(IElement a, IElement b) => ReferenceEquals(a, b) || a is Placeholder x && b is Placeholder y && x.Equals(y);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
}

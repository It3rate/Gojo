using System.Text.Json.Serialization;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// A small supplied search neighborhood. Candidate laws are held, alternated or
/// comparison-driven responses. Two fresh directional challenges expose whether
/// an initially unequal public relation becomes equal. Native finite gates may
/// retain the actual original candidate archive; no score ranking, image fitting
/// or new response law is manufactured. Retention is scoped to these challenges.
/// </summary>
public static class ExtentRouteRetention
{
    public sealed record ProbeRead(long Goal, byte BeforeMask, byte AfterMask,
        long[] Values, long[] Tensions, bool Qualifies,
        [property: JsonIgnore] Relation Surface,
        [property: JsonIgnore] Relation GateRead,
        [property: JsonIgnore] Relation ResultSupport);
    public sealed record CandidateRead(string Policy, ProbeRead[] Probes, bool Retained,
        string Reason, int ArchiveBytes,
        [property: JsonIgnore] Relation Surface,
        [property: JsonIgnore] Relation CandidateArchive,
        [property: JsonIgnore] Relation RetentionRead,
        [property: JsonIgnore] byte[] Archive);
    public sealed record HeldOutRead(string Policy, long Goal, string Observation,
        long[] Values, long[] Tensions, byte[] Comparisons, bool GoalReached,
        [property: JsonIgnore] Relation Surface);
    public sealed record ReportRead(string Scope, CandidateRead[] Candidates, HeldOutRead[] HeldOut,
        int RetainedCount, [property: JsonIgnore] Relation Surface);

    public static ReportRead Report()
    {
        var candidates = new[] { "hold", "clock", "feedback" }.Select(policy => Evaluate(policy)).ToArray();
        var heldOut = candidates.Where(c => c.Retained).SelectMany(c =>
            new[] { Reuse(c, 0), Reuse(c, 3), Reuse(c, 6), Reuse(c, 9),
                Reuse(c, 5, "missing"), Reuse(c, 5, "ambiguous") }).ToArray();
        return new("Supplied hold/clock/feedback laws; two different directional challenges, not repeated samples of one test. " +
            "A native gate retains a candidate archive only after both public comparisons settle from unequal to equal. " +
            "This selects an existing coupling policy; it does not discover a new law, an ornament topology or its construction history.",
            candidates, heldOut, candidates.Count(c => c.Retained),
            new Relation(S(candidates.Select(c => (IElement)c.Surface).ToArray()), S(heldOut.Select(h => (IElement)h.Surface).ToArray())));
    }

    public static CandidateRead Evaluate(string policy, bool referenceAvailable = true, bool referenceAmbiguous = false)
    {
        var candidate = CoupledExtentMachine.Create(2, 8, policy);
        var archive = candidate.ArchiveSurface();
        var bytes = GojoNumericFormat.Encode(archive);
        var reject = new Placeholder();
        var outcomes = S(reject, archive);
        var probes = new[] { Probe(5), Probe(1) };
        var retainLaw = FiniteLaw.Create(outcomes, outcomes, outcomes,
            FiniteLaw.Row(reject, reject, S(reject)), FiniteLaw.Row(reject, archive, S(reject)),
            FiniteLaw.Row(archive, reject, S(reject)), FiniteLaw.Row(archive, archive, S(archive)));
        var retainedRead = FiniteLaw.Evaluate(retainLaw, probes[0].ResultSupport, probes[1].ResultSupport);
        var retained = SelectsOnly(retainedRead, archive);
        var evidence = new Relation(new Relation(archive, S(probes.Select(p => (IElement)p.Surface).ToArray())),
            new Relation(retainLaw, new Relation(retainedRead, retained ? S(archive) : StructuralConstraint.Open(archive))));
        return new(policy, probes, retained, retained
            ? "Both fresh directional challenges produced resolved unequal-to-equal public comparisons."
            : "At least one challenge remained unequal, missing or ambiguous; no candidate is installed by default.",
            bytes.Length, evidence, archive, retainedRead, bytes);

        ProbeRead Probe(long target)
        {
            var run = CoupledExtentMachine.Decode(bytes);
            var goal = new Carrier(run.StepUnit.UnitShape); goal.Add((ulong)target);
            var goalPort = new Relation(goal.ValueSlot, goal);
            var link = CoupledComparisonLink.Create(goalPort, run);
            var before = link.Observe(referenceAvailable, referenceAmbiguous);
            var values = new List<long> { run.Snapshot().Value };
            var tensions = new List<long> { run.Snapshot().Tension };
            var observations = new List<IElement> { before.Witness };
            for (var pulse = 0; pulse < 8; pulse++)
            {
                var observation = link.Observe(referenceAvailable, referenceAmbiguous);
                observations.Add(observation.Witness);
                var step = run.Step(observation.Packet, compiled: true);
                values.Add(step.After.Value); tensions.Add(step.After.Tension);
            }
            var after = link.Observe(referenceAvailable, referenceAmbiguous); observations.Add(after.Witness);
            QualitativePublication.TryRead(before.Packet, out _, out _, out _, out _, out var domain, out var beforeSupport, out _);
            QualitativePublication.TryRead(after.Packet, out _, out _, out _, out _, out _, out var afterSupport, out _);
            var states = FiniteLaw.Entries(domain);
            var rows = new List<Relation>();
            for (var a = 0; a < 3; a++)
                for (var b = 0; b < 3; b++)
                    rows.Add(FiniteLaw.Row(states[a], states[b], S(a != 1 && b == 1 ? archive : reject)));
            var law = FiniteLaw.Create(domain, domain, outcomes, rows.ToArray());
            var read = FiniteLaw.Evaluate(law, beforeSupport, afterSupport);
            FiniteLaw.TryReadResult(read, out var result, out _, out _);
            // The supplied trial contract requires resolved observations. An
            // alternative set is not a successful concrete reference run even
            // when an existential union happens to include an accepting row.
            var certain = Singleton(beforeSupport) && Singleton(afterSupport);
            var qualified = certain && SelectsOnly(read, archive);
            var support = certain ? result : StructuralConstraint.Open(FiniteLaw.Entries(result));
            var source = new Relation(archive, new Relation(goalPort, link.Surface));
            var witness = new Relation(source, S(run.ArchiveSurface(), S(observations.ToArray()), law, read,
                new Relation(new Relation(before.Packet, after.Packet), support)));
            return new(target, before.Mask, after.Mask, values.ToArray(), tensions.ToArray(), qualified, witness, read, support);
        }
    }

    /// <summary>Runs a frozen retained controller against a new public target.
    /// The evaluator knows the target for reporting; the actuator receives only
    /// the comparison packets produced by the connected sensor.</summary>
    public static HeldOutRead Reuse(CandidateRead selected, long target, string observation = "resolved")
    {
        if (!selected.Retained || !SelectsOnly(selected.RetentionRead, selected.CandidateArchive))
            throw new ArgumentException("The local retention gate has not selected this candidate.", nameof(selected));
        if (target < 0 || observation is not ("resolved" or "missing" or "ambiguous"))
            throw new ArgumentException("Unsupported supplied test condition.");
        // The retained native participant is authoritative. A diagnostic byte
        // buffer or human policy label is never allowed to replace that choice.
        var machine = CoupledExtentMachine.Decode(GojoNumericFormat.Encode(selected.CandidateArchive));
        var goal = new Carrier(machine.StepUnit.UnitShape); goal.Add((ulong)target);
        var goalPort = new Relation(goal.ValueSlot, goal); var link = CoupledComparisonLink.Create(goalPort, machine);
        var values = new List<long> { machine.Snapshot().Value }; var tensions = new List<long> { machine.Snapshot().Tension };
        var comparisons = new List<byte>(); var observations = new List<IElement>();
        for (var pulse = 0; pulse < 10; pulse++)
        {
            var read = link.Observe(observation != "missing", observation == "ambiguous");
            observations.Add(read.Witness); comparisons.Add(read.Mask);
            var step = machine.Step(read.Packet, compiled: true);
            values.Add(step.After.Value); tensions.Add(step.After.Tension);
        }
        var final = link.Observe(observation != "missing", observation == "ambiguous"); observations.Add(final.Witness);
        var reached = final.Mask == 2 && machine.Snapshot().Tension == 0;
        return new(selected.Policy, target, observation, values.ToArray(), tensions.ToArray(), comparisons.ToArray(), reached,
            new Relation(new Relation(selected.Surface, new Relation(goalPort, link.Surface)),
                S(machine.ArchiveSurface(), S(observations.ToArray()))));
    }

    private static bool Singleton(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out var seam, out var tail) &&
        seam.Equals(tail) && entries.Length == 1;
    private static bool SelectsOnly(Relation read, IElement actual) => FiniteLaw.TryReadResult(read, out var result, out var pending, out _) &&
        Singleton(result) && ReferenceEquals(FiniteLaw.Entries(result)[0], actual) && FiniteLaw.Entries(pending).Length == 0;
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}

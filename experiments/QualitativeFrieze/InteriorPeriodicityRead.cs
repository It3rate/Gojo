using System.Numerics;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>
/// Rechecks supplied finite template correspondences in one advertised motion
/// frame. Actual face rows, group rows, ordered contours, translation words and
/// period participants remain connected. This is neither image recognition nor
/// a theorem about every possible infinite continuation of a finite patch.
/// </summary>
public sealed class InteriorPeriodicityRead
{
    // periodicity = (column word -> row word) -> Closed(instance rows)
    // instance = actual group -> (actual canonical group ->
    //                            (translation word -> Closed(atom pairs)))
    // atom pair = actual instance face ROW -> actual canonical face ROW
    // accepted instance = actual instance row -> Closed(actual atom pair ->
    //                      Closed(actual instance binding -> canonical binding))
    // read = source -> (accepted -> (blocked -> open))
    public Relation Source { get; }
    public Relation Accepted { get; }
    public Relation Blocked { get; }
    public Relation Open { get; }
    public Relation Surface { get; }
    public bool IsComplete { get; }
    public bool IsPeriodic { get; }
    public InteriorPeriodicitySummary Summary { get; }

    public static InteriorPeriodicityRead Read(Relation complex, Relation currentGroups,
        Relation positionBindings, QualitativeMotionFrame frame, IElement reach, Relation periodicity) =>
        new(complex, currentGroups, positionBindings, frame, reach, periodicity);

    private InteriorPeriodicityRead(Relation complex, Relation groups, Relation bindings,
        QualitativeMotionFrame frame, IElement reach, Relation periodicity)
    {
        Source = new(complex, new Relation(groups, new Relation(bindings,
            new Relation(new Relation(frame.Surface, reach), periodicity))));
        var accepted = new List<IElement>(); var blocked = new List<IElement>(); var open = new List<IElement>();
        var sites = Array.Empty<IElement>(); var atoms = Array.Empty<IElement>();
        var ready = true;
        if (complex is { Left: Relation siteScope, Right: Relation { Left: Relation edgeScope, Right: Relation faceScope } })
            ready = Closed(siteScope, blocked, open, out sites) & Closed(edgeScope, blocked, open, out _) &
                Closed(faceScope, blocked, open, out atoms);
        else { blocked.Add(complex); ready = false; }
        if (!N.Distinct(sites) || !N.Distinct(atoms)) { blocked.Add(complex); ready = false; }
        ready &= Closed(groups, blocked, open, out var groupEntries);
        ready &= Closed(bindings, blocked, open, out var bindingEntries);
        var groupRows = groupEntries.OfType<Relation>().ToArray();
        if (groupRows.Length != groupEntries.Length || !N.Distinct(groupRows.Select(group => group.Left).ToArray()))
        { blocked.Add(groups); ready = false; }
        var members = new Dictionary<Relation, Relation[]>(ReferenceEqualityComparer.Instance);
        foreach (var group in groupRows)
        {
            if (group.Right is not Relation selected || !Closed(selected, blocked, open, out var rows)) { ready = false; continue; }
            var actual = rows.OfType<Relation>().ToArray();
            if (actual.Length != rows.Length || actual.Length == 0 || !N.Distinct(rows) ||
                rows.Any(row => !atoms.Any(atom => ReferenceEquals(atom, row))))
            { blocked.Add(new Relation(group, complex)); ready = false; continue; }
            if (!members.TryAdd(group, actual)) { blocked.Add(group); ready = false; }
        }
        var siteSet = sites.ToHashSet(N.Participants.Instance);
        var bySite = new Dictionary<IElement, List<Relation>>(N.Participants.Instance);
        foreach (var entry in bindingEntries)
        {
            if (entry is not Relation { Right: Relation } binding || !siteSet.Contains(binding.Left))
            { blocked.Add(new Relation(entry, bindings)); ready = false; continue; }
            if (!bySite.TryGetValue(binding.Left, out var offered)) bySite.Add(binding.Left, offered = []);
            offered.Add(binding);
        }
        var positions = new Dictionary<IElement, Position>(N.Participants.Instance);
        foreach (var site in sites)
        {
            if (!bySite.TryGetValue(site, out var offered)) { open.Add(new Relation(site, bindings)); ready = false; continue; }
            if (offered.Count != 1) { blocked.Add(new Relation(site, N.S(offered.Cast<IElement>().ToArray()))); ready = false; continue; }
            if (Point((Relation)offered[0].Right, out var at)) positions.Add(site, new(offered[0], at));
            else { open.Add(offered[0]); ready = false; }
        }

        var independent = 0; var instanceRows = Array.Empty<IElement>(); var column = default(P); var row = default(P);
        Relation? periodPair = null;
        if (periodicity is { Left: Relation { Left: Relation u, Right: Relation v } periods, Right: Relation instances })
        {
            periodPair = periods;
            ready &= Closed(instances, blocked, open, out instanceRows);
            if (Point(u, out column) && Point(v, out row))
            {
                independent = Cross(column, row) != 0 ? 2 : column != default || row != default ? 1 : 0;
                if (independent == 2) accepted.Add(new Relation(periods, new Relation(frame.Surface, reach)));
                else blocked.Add(new Relation(periods, frame.Surface));
            }
            else open.Add(new Relation(periods, frame.Surface));
        }
        else { blocked.Add(periodicity); ready = false; }

        var acceptedInstances = 0; var matchedPairs = 0;
        var occurrences = new Dictionary<Relation, List<P>>(ReferenceEqualityComparer.Instance);
        var covered = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var exactInstances = instanceRows.OfType<Relation>().ToArray();
        foreach (var entry in instanceRows)
        {
            if (entry is not Relation { Left: Relation instance,
                Right: Relation { Left: Relation canonical, Right: Relation { Left: Relation translation, Right: Relation pairs } } } offered ||
                !members.ContainsKey(instance) || !members.ContainsKey(canonical))
            { blocked.Add(new Relation(entry, groups)); continue; }
            if (exactInstances.Count(other => ReferenceEquals(other.Left, instance)) != 1)
            { blocked.Add(new Relation(offered, periodicity)); continue; }
            if (!Point(translation, out var shift)) { open.Add(offered); continue; }
            if (!Closed(pairs, blocked, open, out var correspondences)) continue;
            var valid = true; var pairWitnesses = new List<IElement>();
            var targets = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
            var sources = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
            foreach (var item in correspondences)
            {
                if (item is not Relation { Left: Relation target, Right: Relation source } pair ||
                    !members[instance].Any(atom => ReferenceEquals(atom, target)) ||
                    !members[canonical].Any(atom => ReferenceEquals(atom, source)) || !targets.Add(target) || !sources.Add(source))
                { blocked.Add(new Relation(item, offered)); valid = false; continue; }
                if (!Vertices(target, out var current) || !Vertices(source, out var basis))
                { open.Add(new Relation(pair, bindings)); valid = false; continue; }
                if (current.Length != basis.Length || current.Where((value, index) => value.At - basis[index].At != shift).Any())
                { blocked.Add(new Relation(offered, new Relation(pair, new Relation(translation, bindings)))); valid = false; continue; }
                pairWitnesses.Add(new Relation(pair, N.S(current.Zip(basis,
                    (a, b) => (IElement)new Relation(a.Binding, b.Binding)).ToArray())));
            }
            if (targets.Count != members[instance].Length || sources.Count != members[canonical].Length)
            { blocked.Add(new Relation(offered, new Relation(instance, canonical))); valid = false; }
            var determinant = Cross(column, row);
            if (determinant == 0 || Cross(shift, row) % determinant != 0 || Cross(column, shift) % determinant != 0)
            { blocked.Add(new Relation(offered, new Relation(translation, periodPair ?? periodicity))); valid = false; }
            if (!valid) continue;
            accepted.Add(new Relation(offered, N.S(pairWitnesses.ToArray()))); covered.Add(instance);
            acceptedInstances++; matchedPairs += correspondences.Length;
            if (!occurrences.TryGetValue(canonical, out var shifts)) occurrences.Add(canonical, shifts = []);
            shifts.Add(shift);
        }
        foreach (var group in groupRows.Where(group => !covered.Contains(group))) open.Add(new Relation(group, periodicity));
        foreach (var (canonical, shifts) in occurrences)
            if (!shifts.Contains(default) || !shifts.Contains(column) || !shifts.Contains(row))
                open.Add(new Relation(canonical, periodicity));

        // This tests the occupied template total against the declared period.
        // It does not silently invent a missing atomic face or repair a gap.
        var canonicalArea2 = BigInteger.Zero; var areaKnown = ready && occurrences.Count > 0;
        foreach (var canonical in occurrences.Keys)
            foreach (var atom in members[canonical])
                if (Vertices(atom, out var polygon)) canonicalArea2 += BigInteger.Abs(Area2(polygon.Select(p => p.At).ToArray()));
                else { open.Add(new Relation(atom, bindings)); areaKnown = false; }
        var periodArea2 = 2 * BigInteger.Abs(Cross(column, row));
        var areaMatches = areaKnown && periodArea2 > 0 && canonicalArea2 == periodArea2;
        var areaWitness = new Relation(periodPair ?? periodicity, N.S(occurrences.Keys.Cast<IElement>().ToArray()));
        if (!areaKnown) open.Add(areaWitness);
        else if (!areaMatches) blocked.Add(areaWitness);
        else accepted.Add(areaWitness);

        Accepted = N.S(accepted.ToArray()); Blocked = N.S(blocked.ToArray()); Open = N.S(open.ToArray());
        IsComplete = ready && blocked.Count == 0 && open.Count == 0 && acceptedInstances == groupRows.Length && groupRows.Length != 0;
        IsPeriodic = IsComplete && independent == 2 && areaMatches;
        Surface = new(Source, new Relation(Accepted, new Relation(Blocked, Open)));
        Summary = new(groupRows.Length, instanceRows.Length, occurrences.Count, acceptedInstances,
            matchedPairs, independent, areaMatches, canonicalArea2.ToString(), periodArea2.ToString(),
            blocked.Count, open.Count, IsComplete, IsPeriodic);

        bool Point(Relation word, out P point)
        {
            point = default;
            if (!N.Read(word, out var terms)) return false;
            foreach (var item in terms)
            {
                if (item is not Relation move || !frame.Direction(move, out var axis, out var forward, out var actualReach) || !N.Same(actualReach, reach)) return false;
                var sign = forward ? 1 : -1;
                point += ReferenceEquals(axis, frame.Horizontal) ? new P(sign, 0) : new P(0, sign);
            }
            return true;
        }
        bool Vertices(Relation face, out Position[] result)
        {
            result = [];
            if (face.Right is not Relation boundary || !N.Read(boundary, out var darts) || darts.Length < 3) return false;
            var output = new List<Position>();
            foreach (var dart in darts)
            {
                if (dart is not Relation { Right: Relation ends } || !positions.TryGetValue(ends.Left, out var at)) return false;
                output.Add(at);
            }
            result = output.ToArray(); return true;
        }
    }

    private readonly record struct P(long X, long Y)
    {
        public static P operator +(P a, P b) => new(checked(a.X + b.X), checked(a.Y + b.Y));
        public static P operator -(P a, P b) => new(checked(a.X - b.X), checked(a.Y - b.Y));
    }
    private sealed record Position(Relation Binding, P At);
    private static BigInteger Cross(P a, P b) => (BigInteger)a.X * b.Y - (BigInteger)a.Y * b.X;
    private static BigInteger Area2(P[] polygon) => Enumerable.Range(0, polygon.Length)
        .Aggregate(BigInteger.Zero, (sum, i) => sum + Cross(polygon[i], polygon[(i + 1) % polygon.Length]));
    private static bool Closed(Relation scope, List<IElement> blocked, List<IElement> open, out IElement[] entries)
    {
        if (!StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail)) { blocked.Add(scope); return false; }
        if (!seam.Equals(tail)) { open.Add(new Relation(scope, tail)); return false; }
        return true;
    }
}

/// <summary>Derived finite checks in the selected common-reach basis.</summary>
public sealed record InteriorPeriodicitySummary(int Groups, int InstanceRows, int CanonicalGroups,
    int AcceptedInstances, int MatchedAtomPairs, int IndependentPeriods, bool CanonicalAreaMatchesPeriod,
    string TwiceCanonicalFormalArea, string TwicePeriodFormalArea, int BlockedWitnesses, int OpenWitnesses,
    bool IsComplete, bool IsPeriodic);

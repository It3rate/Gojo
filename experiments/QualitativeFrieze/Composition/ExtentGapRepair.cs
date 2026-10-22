using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A bounded local response to equal-extent evidence. The receiver's
/// current passages expose which proposed gap intervals participate in each
/// comparison. Native gap words, not source measurements, are adjusted.</summary>
public static class ExtentGapRepair
{
    // receipt = ((equivalence -> sampled role correspondence) -> layout axes)
    //   -> ((before gap bindings -> equal interval participation)
    //     -> (after gap bindings -> (accepted local changes -> open)))
    // gap binding = gap slot -> repeated actual shared-reach moves
    // interval = (ray slot -> sampled edge) ->
    //   (Closed(participating gap slots) -> native expansion witness)
    // constraint = actual equality clause -> Closed(interval occurrences)
    // Counts and interval indexes below compile these finite native surfaces;
    // they are not an additional coordinate or numeric expression language.
    private sealed record Interval(IElement Slot, Relation Edge, int[] Gaps, Relation Surface);
    private sealed record Constraint(Relation Clause, IReadOnlyList<Interval> Intervals, Relation Surface);

    public static bool TryRepair(JointEquivalenceEvidence.Read eq,
        JointShapeReconstruction.Candidate currentSample, JointShapeDescription.Message proposedLayout,
        int[] horizontal, int[] vertical, int step, out int[] newH, out int[] newV, out Relation witness)
    {
        newH = horizontal.ToArray(); newV = vertical.ToArray();
        var source = new Relation(new Relation(eq.Surface, currentSample.RoleMap),
            new Relation(proposedLayout.Horizontal, proposedLayout.Vertical));
        var open = new List<IElement>(); var constraints = new List<Constraint>(); var changes = new List<IElement>();
        var before = S(); var after = S();
        witness = new(source, StructuralConstraint.Open());
        if (step < 0 || !JointEquivalenceEvidence.TryRead(eq.Surface, proposedLayout, out var stream, out var readWitness) ||
            !TryBuckets(proposedLayout.Horizontal, out var h) || !TryBuckets(proposedLayout.Vertical, out var v) ||
            horizontal.Length != E((Relation)proposedLayout.Horizontal.Right).Length - 1 ||
            vertical.Length != E((Relation)proposedLayout.Vertical.Right).Length - 1 ||
            horizontal.Concat(vertical).Any(gap => gap is < 1 or > 6)) return false;
        var frame = proposedLayout.Frame;
        var metric = proposedLayout.Basis.ReadRightAngle(S());
        if (metric.Answer != "Yes")
        { witness = new(source, new Relation(metric.Surface, StructuralConstraint.Open(stream.ExactLengths))); return false; }
        if (!TryCommonReach(currentSample, frame, out var reach))
        { witness = new(source, StructuralConstraint.Open(currentSample.RoleMap)); return false; }
        var roles = E(proposedLayout.Roles);
        var roleRows = E(currentSample.RoleMap).OfType<Relation>().ToArray();
        if (!Closed(currentSample.RoleMap) || roleRows.Length != roles.Length ||
            roleRows.Select(row => row.Left).Distinct(FieldNative.Participants.Instance).Count() != roles.Length ||
            roleRows.Select(row => row.Right).Distinct(FieldNative.Participants.Instance).Count() != roles.Length ||
            roleRows.Any(row => !roles.Contains(row.Left, FieldNative.Participants.Instance))) return false;
        var roleForSite = roleRows.ToDictionary(row => row.Right, row => row.Left, FieldNative.Participants.Instance);
        var gapSlots = Enumerable.Range(0, horizontal.Length + vertical.Length).Select(_ => (IElement)new Placeholder()).ToArray();
        var values = horizontal.Concat(vertical).ToArray();
        before = Bindings(values);
        foreach (var clause in E(stream.ExactLengths).Cast<Relation>())
        {
            var intervals = new List<Interval>(); var unsupported = new List<IElement>();
            foreach (var slot in JointEquivalenceEvidence.Members(clause))
            {
                if (!currentSample.SlotEdges.TryGetValue(slot, out var edge) ||
                    !QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out var word) ||
                    !roleForSite.TryGetValue(a, out var from) || !roleForSite.TryGetValue(b, out var to) ||
                    !h.ContainsKey(from) || !h.ContainsKey(to) || !v.ContainsKey(from) || !v.ContainsKey(to))
                { unsupported.Add(slot); continue; }
                var heading = Heading(frame, word); var directions = E(heading);
                if (!Closed(heading) || directions.Length != 1)
                { unsupported.Add(new Relation(slot, edge)); continue; }
                var onH = ReferenceEquals(directions[0], frame.Right) || ReferenceEquals(directions[0], frame.Left);
                var onV = ReferenceEquals(directions[0], frame.Up) || ReferenceEquals(directions[0], frame.Down);
                if (!onH && !onV || onH && v[from] != v[to] || onV && h[from] != h[to])
                { unsupported.Add(new Relation(slot, edge)); continue; }
                var first = onH ? h[from] : v[from]; var last = onH ? h[to] : v[to];
                if (first == last) { unsupported.Add(new Relation(slot, edge)); continue; }
                var direction = onH ? last > first ? frame.Right : frame.Left : last > first ? frame.Up : frame.Down;
                if (!ReferenceEquals(direction, directions[0])) { unsupported.Add(new Relation(slot, edge)); continue; }
                var offset = onH ? 0 : horizontal.Length;
                var selected = Enumerable.Range(Math.Min(first, last), Math.Abs(last - first)).Select(index => offset + index).ToArray();
                if (selected.Any(index => index < 0 || index >= gapSlots.Length)) { unsupported.Add(new Relation(slot, edge)); continue; }
                var expanded = S(selected.SelectMany(index => Enumerable.Repeat((IElement)QualitativeMotionFrame.Move(direction, reach), values[index])).ToArray());
                var expansion = frame.Reduce(word, Inverse(frame, expanded));
                if (!Zero(frame, expansion)) { unsupported.Add(new Relation(slot, new Relation(edge, expansion))); continue; }
                var participation = new Relation(new Relation(slot, edge), new Relation(S(selected.Select(index => gapSlots[index]).ToArray()), expansion));
                intervals.Add(new(slot, edge, selected, participation));
            }
            // Dropping a diagonal member would silently weaken the advertised
            // group. This tactic keeps the entire unsupported clause open.
            if (unsupported.Count != 0 || intervals.Count < 2)
            { open.Add(new Relation(clause, StructuralConstraint.Open(unsupported.ToArray()))); continue; }
            constraints.Add(new(clause, intervals, new Relation(clause, S(intervals.Select(interval => (IElement)interval.Surface).ToArray()))));
        }
        var original = Score(values); var current = original;
        for (var iteration = 0; iteration < 16 && current != 0; iteration++)
        {
            int[]? selected = null; var best = current; var bestCost = int.MaxValue;
            var order = Enumerable.Range(0, values.Length).OrderBy(index => (index + step + iteration) % Math.Max(1, values.Length)).ToArray();
            foreach (var at in order)
                foreach (var delta in new[] { -1, 1 })
                {
                    Consider([(at, delta)]);
                    var axisStart = at < horizontal.Length ? 0 : horizontal.Length;
                    var axisLength = at < horizontal.Length ? horizontal.Length : vertical.Length;
                    var reflected = axisStart + axisLength - 1 - (at - axisStart);
                    if (reflected != at) Consider([(at, delta), (reflected, delta)]);
                    if (horizontal.Length == vertical.Length)
                    {
                        var across = at < horizontal.Length ? at + horizontal.Length : at - horizontal.Length;
                        Consider([(at, delta), (across, delta)]);
                        if (reflected != at)
                        {
                            var acrossReflection = reflected < horizontal.Length ? reflected + horizontal.Length : reflected - horizontal.Length;
                            Consider([(at, delta), (reflected, delta), (across, delta), (acrossReflection, delta)]);
                        }
                    }
                }
            // A simultaneous exchange can cross a coordinate-wise plateau
            // without paying a temporary violation of another equality group.
            if (selected is null)
                for (var a = 0; a < order.Length; a++) for (var b = a + 1; b < order.Length; b++)
                    foreach (var da in new[] { -1, 1 }) foreach (var db in new[] { -1, 1 })
                        Consider([(order[a], da), (order[b], db)]);
            if (selected is null) break;
            var previous = Bindings(values); var next = Bindings(selected);
            changes.Add(new Relation(new Relation(previous, S(constraints.Select(constraint => (IElement)constraint.Surface).ToArray())), next));
            values = selected; current = best;

            void Consider((int Index, int Delta)[] edits)
            {
                var candidate = values.ToArray();
                foreach (var edit in edits) candidate[edit.Index] += edit.Delta;
                if (candidate.Any(value => value is < 1 or > 6)) return;
                var score = Score(candidate); var cost = candidate.Sum();
                if (score < best || score == best && score < current && cost < bestCost)
                { best = score; selected = candidate; bestCost = cost; }
            }
        }
        after = Bindings(values);
        if (current != 0) open.AddRange(constraints.Where(constraint => Disagreement(constraint, values) != 0)
            .Select(constraint => (IElement)new Relation(constraint.Surface, after)));
        newH = values.Take(horizontal.Length).ToArray(); newV = values.Skip(horizontal.Length).ToArray();
        witness = new(new Relation(source, new Relation(readWitness, metric.Surface)),
            new Relation(new Relation(before, S(constraints.Select(constraint => (IElement)constraint.Surface).ToArray())),
                new Relation(after, new Relation(S(changes.ToArray()), StructuralConstraint.Open(open.ToArray())))));
        return current < original;

        Relation Bindings(int[] gaps) => S(gaps.Select((gap, index) => (IElement)new Relation(gapSlots[index],
            S(Enumerable.Repeat((IElement)QualitativeMotionFrame.Move(index < horizontal.Length ? frame.Right : frame.Up, reach), gap).ToArray()))).ToArray());
        int Score(int[] gaps) => constraints.Sum(constraint => Disagreement(constraint, gaps));
    }

    private static int Disagreement(Constraint constraint, int[] values)
    {
        var extents = constraint.Intervals.Select(interval => interval.Gaps.Sum(index => values[index])).Order().ToArray();
        var median = extents[extents.Length / 2];
        return extents.Sum(extent => Math.Abs(extent - median));
    }

    private static bool TryBuckets(Relation order, out Dictionary<IElement, int> buckets)
    {
        buckets = new(FieldNative.Participants.Instance);
        if (order.Right is not Relation scope || !Closed(scope)) return false;
        var groups = E(scope).OfType<Relation>().ToArray();
        if (groups.Length != E(scope).Length || groups.Any(group => !Closed(group) || E(group).Length == 0)) return false;
        for (var index = 0; index < groups.Length; index++)
            foreach (var role in E(groups[index])) if (!buckets.TryAdd(role, index)) return false;
        return true;
    }

    private static bool TryCommonReach(JointShapeReconstruction.Candidate candidate, QualitativeMotionFrame frame, out IElement reach)
    {
        reach = null!;
        foreach (var word in candidate.Positions.Values)
        {
            if (!Closed(word)) return false;
            foreach (var item in E(word))
            {
                if (item is not Relation move || !frame.Direction(move, out _, out _, out var actual)) return false;
                if (reach is null) reach = actual;
                else if (!ReferenceEquals(reach, actual)) return false;
            }
        }
        return reach is not null;
    }
}

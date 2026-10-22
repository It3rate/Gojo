using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>A bounded proposal experiment. Spatial embeddings, ray pairings and
/// gap recipes are explicitly proposed structures, assessed against independent
/// exposed evidence. No source graph, source coordinates or family identifier
/// enters this search.</summary>
public static class RelationalShapeSearch
{
    public sealed record Focus(string Id, string Name, RelationalSpatialEvidence.Read Spatial,
        JointEquivalenceEvidence.Read? Equivalence, StructuralEvidenceStreams.ClosureSelection Context)
    {
        public Relation Surface => new(new Relation(Spatial.Surface, Equivalence?.Surface ?? StructuralConstraint.Open()), Context.Surface);
    }

    public sealed class Trial
    {
        public int Index { get; }
        public JointShapeReconstruction.Candidate Candidate { get; }
        public JointShapeReconstruction.Feedback Core { get; }
        public JointEquivalenceEvidence.Assessment? Equivalence { get; }
        public RelationalSpatialEvidence.Assessment Spatial { get; }
        public Relation Surface { get; }
        public Relation PairingHypothesis { get; }
        public int[] HorizontalGaps { get; }
        public int[] VerticalGaps { get; }
        public int HardConflicts { get; }
        public int UnpairedRays { get; }
        public int GoalBlocked { get; }
        public int DefaultConflicts { get; }
        public int Assumptions => E(Candidate.AssumedStrokes).Length;
        public int Open => E(Core.Open).Length + (Equivalence is null ? 0 : E(Equivalence.Open).Length) + E(Spatial.Open).Length;
        public int Supported => E(Core.Matched).Length + (Equivalence is null ? 0 : E(Equivalence.Supported).Length) + E(Spatial.Supported).Length;
        public int DescriptionCost => HorizontalGaps.Sum() + VerticalGaps.Sum();
        // Counts rank this authored search. They are not confidence values or
        // posterior probabilities; the independent native witnesses remain.
        internal (int, int, int, int, int, int, int) Rank =>
            (HardConflicts, UnpairedRays, GoalBlocked, DefaultConflicts, Assumptions, DescriptionCost, -Supported);
        internal Trial(int index, JointShapeDescription.Message message, Focus focus,
            JointShapeReconstruction.Candidate candidate, Relation pairing, Relation derivation,
            int[] horizontal, int[] vertical)
        {
            Index = index; Candidate = candidate; PairingHypothesis = pairing;
            HorizontalGaps = horizontal; VerticalGaps = vertical;
            Core = JointShapeReconstruction.Assess(message, candidate); candidate.Assessment = Core;
            Equivalence = focus.Equivalence is null ? null : JointEquivalenceEvidence.Assess(focus.Equivalence, candidate);
            Spatial = RelationalSpatialEvidence.Assess(focus.Spatial, candidate);
            HardConflicts = Core.Claims.Count(claim => claim.State == "Violated" && claim.Kind is not ("closed" or "crossings" or "overlaps" or "assumptions")) +
                (Equivalence is null ? 0 : E(Equivalence.Contradicted).Length) + E(Spatial.Contradicted).Length;
            UnpairedRays = message.JointReads.Sum(joint => E(joint.Rays).Length) - candidate.SlotEdges.Count;
            var closureClaims = Core.Claims.Where(claim => claim.Kind == "closed").ToArray();
            GoalBlocked = focus.Context.ClosureRequired && (closureClaims.Length == 0 || closureClaims.Any(claim => claim.State != "Supported")) ? 1 : 0;
            DefaultConflicts = Core.Claims.Count(claim => claim.State == "Violated" && claim.Kind is "crossings" or "overlaps");
            Surface = new(new Relation(focus.Surface, derivation), new Relation(new Relation(candidate.Surface, Core.Surface),
                new Relation(Equivalence?.Surface ?? StructuralConstraint.Open(), Spatial.Surface)));
        }

        /// <summary>Capture the concrete current sample, deliberately omitting
        /// the earlier policy history. The full retained trial stays separate.</summary>
        public Relation Snapshot => new(new Relation(Candidate.Source.Graph.Surface, Candidate.Source.Bindings), Candidate.RoleMap);
    }

    public sealed record Result(Focus Focus, IReadOnlyList<Trial> Retained, Trial Best, int Requested,
        int Evaluated, int Blocked, int Improved, int DistinctSamples, Relation CapacityRemainder, Relation Surface);

    public static Result Run(JointShapeDescription.Message message, Focus focus, int budget, bool extentRepair = false)
    {
        if (budget < 3) throw new ArgumentOutOfRangeException(nameof(budget));
        var retained = new List<Trial>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        var blockedReads = new List<IElement>(); var evaluated = 0; var blocked = 0; var improved = 0;
        Trial? best = null;
        var initial = RelationalSpatialEvidence.Infer(message, focus.Spatial, S(), 0);
        var needsPairingHypotheses = !initial.Complete || E(initial.Assumptions).Length != 0;
        for (var attempt = 0; attempt < budget; attempt++)
        {
            // Fully observed spatial evidence needs no speculative edge plan.
            // Sparse reads additionally explore candidate ray correspondences.
            var pairs = needsPairingHypotheses && attempt % 4 != 0 ? ProposePairs(message, attempt / 4) : S();
            var inferred = RelationalSpatialEvidence.Infer(message, focus.Spatial, pairs, attempt % 7);
            if (!inferred.Complete || inferred.Layout is null)
            {
                blocked++; if (blockedReads.Count < 3) blockedReads.Add(inferred.Witness);
                continue;
            }
            var layout = inferred.Layout;
            var hCount = E((Relation)layout.Horizontal.Right).Length;
            var vCount = E((Relation)layout.Vertical.Right).Length;
            var random = new Random(1789 + attempt * 7919);
            var h = SampleGaps(hCount - 1, attempt, best?.HorizontalGaps, random);
            var v = SampleGaps(vCount - 1, attempt, best?.VerticalGaps, random);
            // This symmetric default is useful when an action exchanges axes.
            if (h.Length == v.Length && attempt % 4 != 1) v = h.ToArray();
            IElement? selectedReach = null;
            Relation repairWitness = S();
            // Optional receiver-local feedback. A retained sample supplies its
            // own gap words and edge incidence; no teacher dimensions enter.
            // Sparse layouts can change bucket identities, so this tactic is
            // limited to a fully observed ordinal layout for now.
            if (extentRepair && !needsPairingHypotheses && best is not null &&
                focus.Equivalence is not null && attempt % 4 == 3 &&
                ExtentGapRepair.TryRepair(focus.Equivalence, best.Candidate, layout,
                    best.HorizontalGaps, best.VerticalGaps, attempt,
                    out var repairedH, out var repairedV, out repairWitness))
            {
                h = repairedH; v = repairedV;
                selectedReach = best.Candidate.Positions.Values.SelectMany(E).OfType<Relation>()
                    .Select(move => layout.Frame.Direction(move, out _, out _, out var reach) ? reach : null)
                    .First(reach => reach is not null);
            }
            var positions = Placement(layout, h, v, selectedReach);
            var permission = focus.Context.ShouldComplete ? S(message.ClosureDefault) : S();
            var justification = new Relation(focus.Surface, new Relation(inferred.Witness,
                new Relation(best?.Snapshot ?? StructuralConstraint.Open(), repairWitness)));
            var construction = JointShapeReconstruction.Construction(layout, positions, pairs, permission, justification);
            var candidate = JointShapeReconstruction.Propose(layout, attempt % 3, construction: construction);
            var trial = new Trial(attempt + 1, message, focus, candidate, pairs, inferred.Witness, h, v); evaluated++;
            seen.Add(Signature(message, candidate));
            if (best is null || trial.Rank.CompareTo(best.Rank) < 0)
            { if (best is not null) improved++; best = trial; }
            retained.Add(trial);
            // Keep the first attempt and two strong, distinct alternatives.
            // Receipts do not grow a chain through all discarded policy states.
            if (retained.Count > 8)
            {
                var first = retained[0];
                retained = retained.Skip(1).OrderBy(item => item.Rank).GroupBy(item => Signature(message, item.Candidate))
                    .Select(group => group.First()).Take(5).Prepend(first).ToList();
                if (!retained.Contains(best)) retained.Add(best);
            }
        }
        if (best is null)
        {
            // The failure itself is a visible partial candidate, not a secret
            // fallback to source coordinates or an asserted complete layout.
            var empty = JointShapeReconstruction.Construction(message, StructuralConstraint.Open(), S(), S(), focus.Surface);
            var candidate = JointShapeReconstruction.Propose(message, 0, construction: empty);
            best = new Trial(0, message, focus, candidate, S(), S(blockedReads.ToArray()), [], []);
            retained.Add(best);
        }
        var chosen = retained.OrderBy(item => ReferenceEquals(item, best) ? 0 : 1).ThenBy(item => item.Rank)
            .GroupBy(item => Signature(message, item.Candidate)).Select(group => group.First()).Take(3).ToList();
        // All three inspection slots exist even when coarse evidence admits
        // only one distinct sample. Repetition is labelled in the report.
        foreach (var item in retained.OrderBy(item => item.Index))
            if (chosen.Count < 3 && !chosen.Contains(item)) chosen.Add(item);
        while (chosen.Count < 3) chosen.Add(best);
        var remainder = StructuralConstraint.Open(S(blockedReads.ToArray()));
        var surface = new Relation(new Relation(message.Surface, focus.Surface), new Relation(
            S(chosen.Select(item => (IElement)item.Surface).ToArray()), remainder));
        return new(focus, chosen, best, budget, evaluated, blocked, improved, seen.Count, remainder, surface);
    }

    public static Relation Placement(JointShapeDescription.Message layout, int[] hGaps, int[] vGaps, IElement? selectedReach = null)
    {
        var h = Buckets(layout.Horizontal); var v = Buckets(layout.Vertical); var reach = selectedReach ?? new Placeholder();
        Relation At(IElement role) => S(hGaps.Take(h[role]).SelectMany(gap => Enumerable.Repeat((IElement)QualitativeMotionFrame.Move(layout.Frame.Right, reach), gap))
            .Concat(vGaps.Take(v[role]).SelectMany(gap => Enumerable.Repeat((IElement)QualitativeMotionFrame.Move(layout.Frame.Up, reach), gap))).ToArray());
        return S(E(layout.Roles).Select(role => (IElement)new Relation(role, At(role))).ToArray());
    }

    private static int[] SampleGaps(int length, int step, int[]? previous, Random random)
    {
        var gaps = previous is not null && previous.Length == length ? previous.ToArray() : Enumerable.Repeat(1, length).ToArray();
        if (length == 0) return gaps;
        switch (step % 8)
        {
            case 0: return Enumerable.Repeat(1, length).ToArray();
            case 1: return Enumerable.Range(0, length).Select(index => index == 0 || index == length - 1 ? 2 : 1).ToArray();
            case 2: return Enumerable.Range(0, length).Select(index => index == 0 || index == length - 1 ? 1 : 2).ToArray();
            case 3: return Enumerable.Repeat(2, length).ToArray();
            default:
                var at = random.Next(length); var value = random.Next(1, 4); gaps[at] = value;
                if (step % 2 == 0) gaps[length - 1 - at] = value;
                return gaps;
        }
    }

    private static Relation ProposePairs(JointShapeDescription.Message message, int seed)
    {
        var rays = message.JointReads.SelectMany(joint => E(joint.Rays).Cast<Relation>().Select(row => (Role: joint.Role, Slot: row.Left, Heading: (Relation)row.Right))).ToArray();
        var random = new Random(8171 + seed * 3581); var used = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pairs = new List<IElement>();
        foreach (var ray in rays.OrderBy(_ => random.Next()))
        {
            if (used.Contains(ray.Slot) || Sector(message.Frame, ray.Heading) < 0) continue;
            var choices = rays.Where(other => !used.Contains(other.Slot) && !ReferenceEquals(ray.Role, other.Role) &&
                Sector(message.Frame, other.Heading) == (Sector(message.Frame, ray.Heading) + 4) % 8 && CompatibleComponents(ray.Role, other.Role)).ToArray();
            if (choices.Length == 0) continue;
            var chosen = choices[random.Next(choices.Length)]; used.Add(ray.Slot); used.Add(chosen.Slot);
            pairs.Add(new Relation(ray.Slot, chosen.Slot));
        }
        return S(pairs.ToArray());
        bool CompatibleComponents(IElement a, IElement b) => !Closed(message.Components) || E(message.Components).OfType<Relation>()
            .Any(component => E(component).Contains(a, ReferenceEqualityComparer.Instance) && E(component).Contains(b, ReferenceEqualityComparer.Instance));
    }

    private static Dictionary<IElement, int> Buckets(Relation order) => E((Relation)order.Right).Cast<Relation>()
        .SelectMany((bucket, index) => E(bucket).Select(role => (role, index))).ToDictionary(pair => pair.role, pair => pair.index, FieldNative.Participants.Instance);

    public static string Signature(JointShapeDescription.Message message, JointShapeReconstruction.Candidate candidate)
    {
        var roles = E(message.Roles); var indices = roles.Select((role, index) => (site: candidate.Sites[role], index))
            .ToDictionary(pair => pair.site, pair => pair.index, FieldNative.Participants.Instance);
        var edges = E(candidate.Source.Graph.Strokes).Cast<Relation>().Select(edge => {
            QualitativeStrokeGraph.TryReadStroke(edge, out _, out var a, out var b, out _);
            return Math.Min(indices[a], indices[b]) + ":" + Math.Max(indices[a], indices[b]);
        }).Order(StringComparer.Ordinal);
        // This is a derived cache key, never an input claim or classifier.
        var positions = roles.Select(role => candidate.Positions.TryGetValue(role, out var word)
            ? string.Join(',', E(word).Cast<Relation>().GroupBy(move => Sector(message.Frame, S(move.Left))).OrderBy(group => group.Key).Select(group => group.Key + "x" + group.Count())) : "?");
        return string.Join('|', edges) + ";" + string.Join(';', positions);
    }
}

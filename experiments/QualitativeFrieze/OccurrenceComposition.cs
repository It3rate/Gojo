using Gojo.Core;
using Gojo.Patterns;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Finite existential composition of source-free motif roles over an
/// explicitly advertised observation. This is a non-draining inventory read,
/// not a hidden-source supplier reply or a new kernel operation.</summary>
public static class OccurrenceComposition
{
    // role = actual open slot -> source-free CommunicationPattern surface
    // question = Closed(roles) -> (Closed(different edge-footprint role pairs)
    //   -> (Closed(no shared-site pairs) -> (Closed(shared-site pairs)
    //   -> (Closed(shared-edge pairs) -> Closed(no shared-edge pairs)))))
    // Every pair refers to actual slots in this question. Each constraint is
    // scoped to the two selected occurrences, not their surrounding components.
    // Different means different actual edge sets, not different traversal,
    // pattern, centre-role, source recipe, or match-witness identity. Two reads
    // of the same physical edge footprint cannot manufacture two copies.
    //
    // assignment = Closed(slot -> actual occurrence) ->
    //   (Closed(slot -> (Closed(actual sites) -> Closed(actual edge rows))) ->
    //    (Closed(slot -> native frame/site/edge matching correspondence) ->
    //     Closed(constraint -> ((left footprint -> right footprint) ->
    //       (Closed(shared identities) -> (Closed(left only) -> Closed(right only)))))))
    // assessment = ((advertised induction -> question) -> budget) ->
    //   (inventory -> (consumed -> (remaining -> (rejected ->
    //    (supported -> (refuted -> (open -> blocked)))))))
    // Absence requires both a completed relevant motif grammar and exhausted
    // assignment search. A certified positive assignment needs neither.

    public sealed class Role
    {
        public Placeholder Slot { get; }
        public Relation PatternSurface { get; }
        public Relation Surface { get; }

        public Role(CommunicationPattern pattern)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            Slot = new(); PatternSurface = pattern.Surface; Surface = new(Slot, PatternSurface);
        }

        internal Role(Relation surface)
        {
            Surface = surface; Slot = (Placeholder)surface.Left; PatternSurface = (Relation)surface.Right;
        }
    }

    public static Relation Pair(Role first, Role second) => new(first.Slot, second.Slot);

    public sealed class Question
    {
        public string Name { get; }
        public Relation Surface { get; }
        public IReadOnlyList<Role> Roles { get; }
        public Relation DistinctEdgeSets { get; }
        public Relation DisjointSites { get; }
        public Relation SharedSites { get; }
        public Relation SharedEdges { get; }
        public Relation DisjointEdges { get; }
        public Relation Blocked { get; }
        public Relation Unresolved { get; }
        public bool IsValid => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

        public Question(string name, IReadOnlyList<Role> roles, Relation? distinct = null,
            Relation? disjointSites = null, Relation? sharedSites = null,
            Relation? sharedEdges = null, Relation? disjointEdges = null)
            : this(name, new Relation(N.S(roles.Select(role => (IElement)role.Surface).ToArray()),
                new Relation(distinct ?? N.S(), new Relation(disjointSites ?? N.S(),
                    new Relation(sharedSites ?? N.S(), new Relation(sharedEdges ?? N.S(), disjointEdges ?? N.S())))))) { }

        /// <summary>Reconstructs the question solely from its exact native
        /// surface. Missing/open contract sections are never empty defaults.</summary>
        public Question(string name, Relation surface)
        {
            Name = name; Surface = surface;
            var blocked = new List<IElement>(); var open = new List<IElement>(); var roles = new List<Role>();
            DistinctEdgeSets = DisjointSites = SharedSites = SharedEdges = DisjointEdges = N.S();
            if (surface is not { Left: Relation roleScope, Right: Relation { Left: Relation distinct,
                    Right: Relation { Left: Relation disjointSites, Right: Relation { Left: Relation sharedSites,
                    Right: Relation { Left: Relation sharedEdges, Right: Relation disjointEdges } } } } })
                blocked.Add(surface);
            else
            {
                DistinctEdgeSets = distinct; DisjointSites = disjointSites; SharedSites = sharedSites;
                SharedEdges = sharedEdges; DisjointEdges = disjointEdges;
                ReadScope(roleScope, out var rows);
                foreach (var item in rows)
                {
                    if (item is not Relation { Left: Placeholder { IsInverse: false } slot, Right: Relation pattern } row ||
                        !CommunicationShape.TryRead(pattern, out _) || roles.Any(role => N.Same(role.Slot, slot)))
                    { blocked.Add(new Relation(roleScope, item)); continue; }
                    roles.Add(new(row));
                }
                if (rows.Length == 0) blocked.Add(roleScope);
                foreach (var scope in new[] { distinct, disjointSites, sharedSites, sharedEdges, disjointEdges })
                {
                    ReadScope(scope, out var pairs);
                    foreach (var pair in pairs)
                        if (pair is not Relation relation || N.Same(relation.Left, relation.Right) ||
                            !roles.Any(role => N.Same(role.Slot, relation.Left)) ||
                            !roles.Any(role => N.Same(role.Slot, relation.Right))) blocked.Add(new Relation(scope, pair));
                }
            }
            Roles = roles; Blocked = N.S(blocked.ToArray()); Unresolved = N.S(open.ToArray());

            void ReadScope(Relation scope, out IElement[] entries)
            {
                if (!StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail))
                { entries = []; blocked.Add(scope); }
                else if (!seam.Equals(tail)) open.Add(scope);
            }
        }
    }

    public sealed class Witness
    {
        public Relation Bindings { get; }
        public Relation Footprints { get; }
        public Relation Correspondences { get; }
        public Relation Checks { get; }
        public Relation Surface { get; }
        public IReadOnlyList<CommunicationMatch> Matches { get; }

        internal Witness(IReadOnlyList<Role> roles, Option[] options, Relation checks)
        {
            Matches = options.Select(option => option.Match).ToArray();
            Bindings = N.S(roles.Select((role, index) => (IElement)new Relation(role.Slot,
                options[index].Match.Occurrence.Read)).ToArray());
            Footprints = N.S(roles.Select((role, index) => (IElement)new Relation(role.Slot,
                options[index].Surface)).ToArray());
            Correspondences = N.S(roles.Select((role, index) => (IElement)new Relation(role.Slot,
                options[index].Match.Witness)).ToArray());
            Checks = checks; Surface = new(Bindings, new Relation(Footprints, new Relation(Correspondences, Checks)));
        }
    }

    internal sealed class Option(CommunicationMatch match)
    {
        internal CommunicationMatch Match { get; } = match;
        internal IElement[] Sites { get; } = N.E(match.Occurrence.Sites);
        internal IElement[] Edges { get; } = N.E(match.Occurrence.Segments);
        internal Relation Surface { get; } = new(match.Occurrence.Sites, match.Occurrence.Segments);
    }

    public sealed class Assessment
    {
        public QualitativeMotifInduction Output { get; }
        public Question Question { get; }
        public Relation Budget { get; }
        public Relation Inventory { get; }
        public Relation Consumed { get; }
        public Relation Remaining { get; }
        public Relation Rejected { get; }
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Surface { get; }
        public IReadOnlyList<Witness> Witnesses { get; }
        public string Answer => N.E(Blocked).Length != 0 ? "Blocked" : N.E(Supported).Length != 0 ? "Yes" :
            N.E(Refuted).Length != 0 ? "No" : "Open";

        public Assessment(QualitativeMotifInduction output, Question question, int assignmentCapacity = 4096)
        {
            ArgumentNullException.ThrowIfNull(output); ArgumentNullException.ThrowIfNull(question);
            if (assignmentCapacity < 0) throw new ArgumentOutOfRangeException(nameof(assignmentCapacity));
            Output = output; Question = question;
            var slots = Enumerable.Range(0, assignmentCapacity).Select(_ => (IElement)new Placeholder()).ToArray();
            Budget = N.S(slots);
            var inventory = new List<IElement>(); var consumed = new List<IElement>(); var rejected = new List<IElement>();
            var supported = new List<IElement>(); var refuted = new List<IElement>();
            var pending = new List<IElement>(N.E(question.Unresolved)); var blocked = new List<IElement>(N.E(question.Blocked));
            var witnesses = new List<Witness>();
            if (question.IsValid)
            {
                var choices = new List<Option[]>(); var complete = output.IsComplete; var absent = false;
                foreach (var role in question.Roles)
                {
                    var assessment = new CommunicationAssessment(output,
                        new CommunicationMessage(N.S(role.PatternSurface), N.S()));
                    var options = new List<Option>();
                    foreach (var match in assessment.Matches)
                    {
                        var candidate = new Option(match);
                        // Traversal aliases are not fresh physical instances.
                        if (!options.Any(prior => SameSet(prior.Edges, candidate.Edges))) options.Add(candidate);
                    }
                    choices.Add(options.ToArray());
                    inventory.Add(new Relation(role.Slot, new Relation(assessment.Surface,
                        N.S(options.Select(option => (IElement)option.Surface).ToArray()))));
                    absent |= assessment.RefutedCount != 0;
                    complete &= assessment.BlockedCount == 0 && assessment.UnresolvedCount == 0 &&
                        CommunicationShape.TryRead(role.PatternSurface, out var shape) && WithinGrammar(output, shape);
                }
                if (absent) refuted.Add(new Relation(question.Surface, N.S(inventory.ToArray())));
                else if (choices.Any(options => options.Length == 0))
                    pending.Add(new Relation(question.Surface, StructuralConstraint.Open(output.Unsearched, output.Unresolved, output.Budget)));
                else
                {
                    var selected = new Option[question.Roles.Count]; var cut = false;
                    Search(0);
                    if (witnesses.Count != 0) supported.Add(new Relation(question.Surface, witnesses[0].Surface));
                    else if (complete && !cut) refuted.Add(new Relation(question.Surface,
                        new Relation(N.S(inventory.ToArray()), new Relation(Budget, N.S(rejected.ToArray())))));
                    else pending.Add(new Relation(question.Surface, StructuralConstraint.Open(output.Unsearched,
                        output.Unresolved, output.Budget, N.S(consumed.ToArray()), N.S(rejected.ToArray()))));

                    void Search(int depth)
                    {
                        foreach (var option in choices[depth])
                        {
                            if (consumed.Count >= slots.Length) { cut = true; return; }
                            selected[depth] = option;
                            var partial = N.S(question.Roles.Take(depth + 1).Select((role, index) =>
                                (IElement)new Relation(role.Slot, selected[index].Match.Occurrence.Read)).ToArray());
                            consumed.Add(new Relation(slots[consumed.Count], partial));
                            var pass = Check(depth, out var checks, out var failures);
                            if (!pass) { rejected.Add(new Relation(partial, failures)); continue; }
                            if (depth + 1 == choices.Count)
                            { witnesses.Add(new(question.Roles, selected.ToArray(), checks)); return; }
                            Search(depth + 1);
                            if (cut || witnesses.Count != 0) return;
                        }
                    }

                    bool Check(int depth, out Relation checks, out Relation failures)
                    {
                        var accepted = new List<IElement>(); var denied = new List<IElement>();
                        Inspect(question.DistinctEdgeSets, edges: true, requireShared: null);
                        Inspect(question.DisjointSites, edges: false, requireShared: false);
                        Inspect(question.SharedSites, edges: false, requireShared: true);
                        Inspect(question.SharedEdges, edges: true, requireShared: true);
                        Inspect(question.DisjointEdges, edges: true, requireShared: false);
                        checks = N.S(accepted.ToArray()); failures = N.S(denied.ToArray()); return denied.Count == 0;

                        void Inspect(Relation scope, bool edges, bool? requireShared)
                        {
                            foreach (var pair in N.E(scope).Cast<Relation>())
                            {
                                var a = Index(question.Roles, pair.Left); var b = Index(question.Roles, pair.Right);
                                if (a > depth || b > depth) continue;
                                var left = edges ? selected[a].Edges : selected[a].Sites;
                                var right = edges ? selected[b].Edges : selected[b].Sites;
                                var common = left.Where(item => right.Any(other => N.Same(item, other))).ToArray();
                                var onlyLeft = left.Where(item => !right.Any(other => N.Same(item, other))).ToArray();
                                var onlyRight = right.Where(item => !left.Any(other => N.Same(item, other))).ToArray();
                                var proof = new Relation(new Relation(scope, pair), new Relation(
                                    new Relation(selected[a].Surface, selected[b].Surface),
                                    new Relation(N.S(common), new Relation(N.S(onlyLeft), N.S(onlyRight)))));
                                var passes = requireShared.HasValue ? (common.Length != 0) == requireShared.Value :
                                    onlyLeft.Length != 0 || onlyRight.Length != 0;
                                (passes ? accepted : denied).Add(proof);
                            }
                        }
                    }
                }
            }
            Inventory = N.S(inventory.ToArray()); Consumed = N.S(consumed.ToArray());
            Remaining = N.S(slots.Skip(consumed.Count).ToArray()); Rejected = N.S(rejected.ToArray());
            Supported = N.S(supported.ToArray()); Refuted = N.S(refuted.ToArray());
            Open = N.S(pending.ToArray()); Blocked = N.S(blocked.ToArray()); Witnesses = witnesses;
            Surface = new(new Relation(new Relation(output.Surface, question.Surface), Budget),
                new Relation(Inventory, new Relation(Consumed, new Relation(Remaining, new Relation(Rejected,
                    new Relation(Supported, new Relation(Refuted, new Relation(Open, Blocked))))))));
        }
    }

    private static int Index(IReadOnlyList<Role> roles, IElement slot) =>
        Enumerable.Range(0, roles.Count).Single(index => N.Same(roles[index].Slot, slot));

    private static bool SameSet(IElement[] first, IElement[] second) => first.Length == second.Length &&
        first.All(item => second.Any(other => N.Same(item, other)));

    private static bool WithinGrammar(QualitativeMotifInduction output, CommunicationShape shape)
    {
        if (shape.Centres.Length == 1) return true;
        if (output.Budget.Right is not Relation { Right: Relation lengths } ||
            lengths.Left is not Relation cycleScope || lengths.Right is not Relation walkScope ||
            !N.Read(cycleScope, out var cycleSlots) || !N.Read(walkScope, out var walkSlots)) return false;
        return shape.IsCycle ? shape.Edges.Length >= 3 && shape.Edges.Length <= cycleSlots.Length :
            shape.Edges.Length <= walkSlots.Length;
    }
}

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
    // selected-site question = question -> (Closed(shared selection pairs) ->
    //   Closed(disjoint selection pairs))
    // selection = actual role slot -> Closed(actual pattern-site placeholders)
    // Empty closed selections are known empty sets; an open selection is not.
    // Every whole-footprint pair refers to actual slots in this question. Each constraint is
    // scoped to the two selected occurrences, not their surrounding components.
    // Different means different actual edge sets, not different traversal,
    // pattern, centre-role, source recipe, or match-witness identity. Two reads
    // of the same physical edge footprint cannot manufacture two copies.
    //
    // assignment = Closed(slot -> actual occurrence) ->
    //   (Closed(slot -> (Closed(actual sites) -> Closed(actual edge rows))) ->
    //    (Closed(slot -> native frame/site/edge matching correspondence) ->
    //     (Closed(constraint -> ((left participation -> right participation) ->
    //       (Closed(shared identities) -> (Closed(left only) -> Closed(right only)))))
    //       -> Closed(selection -> Closed(actual selected sites)))))
    // assessment = ((advertised induction -> question) -> budget) ->
    //   (inventory -> (consumed -> (remaining -> (rejected ->
    //    (supported -> (refuted -> (open -> (blocked ->
    //      (correspondence budget -> (correspondence consumed -> correspondence remaining))))))))))
    // Absence requires both a completed relevant motif grammar and exhausted
    // correspondence/assignment search. A certified positive assignment needs
    // neither search to be exhaustive. These are independently budgeted stages;
    // old whole-footprint questions allocate no correspondence-search capacity.

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

    /// <summary>An advertised selection inside one role's source-free graph.
    /// The actual pattern-site identities carry the selection, not a site name,
    /// ordinal, coordinate, or endpoint/centre operation tag.</summary>
    public sealed class SiteSelection(Role role, Relation sites)
    {
        public Relation Surface { get; } = new(role.Slot, sites);
    }

    public static SiteSelection Select(Role role, params IElement[] patternSites) => new(role, N.S(patternSites));
    public static Relation Pair(SiteSelection first, SiteSelection second) => new(first.Surface, second.Surface);

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
        public Relation SharedSelectedSites { get; }
        public Relation DisjointSelectedSites { get; }
        public bool HasSelectedSiteConstraints => N.E(SharedSelectedSites).Length != 0 ||
            N.E(DisjointSelectedSites).Length != 0;
        public Relation Blocked { get; }
        public Relation Unresolved { get; }
        public bool IsValid => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

        public Question(string name, IReadOnlyList<Role> roles, Relation? distinct = null,
            Relation? disjointSites = null, Relation? sharedSites = null,
            Relation? sharedEdges = null, Relation? disjointEdges = null,
            Relation? sharedSelectedSites = null, Relation? disjointSelectedSites = null)
            : this(name, Extend(new Relation(N.S(roles.Select(role => (IElement)role.Surface).ToArray()),
                new Relation(distinct ?? N.S(), new Relation(disjointSites ?? N.S(),
                    new Relation(sharedSites ?? N.S(), new Relation(sharedEdges ?? N.S(), disjointEdges ?? N.S()))))),
                sharedSelectedSites, disjointSelectedSites)) { }

        private static Relation Extend(Relation legacy, Relation? shared, Relation? disjoint) =>
            shared is null && disjoint is null ? legacy : new(legacy, new Relation(shared ?? N.S(), disjoint ?? N.S()));

        /// <summary>Reconstructs the question solely from its exact native
        /// surface. Missing/open contract sections are never empty defaults.</summary>
        public Question(string name, Relation surface)
        {
            Name = name; Surface = surface;
            var blocked = new List<IElement>(); var open = new List<IElement>(); var roles = new List<Role>();
            DistinctEdgeSets = DisjointSites = SharedSites = SharedEdges = DisjointEdges = N.S();
            SharedSelectedSites = DisjointSelectedSites = N.S();
            // Extension = exact legacy question -> (Closed(shared selection pairs)
            //   -> Closed(disjoint selection pairs)); selection = actual role
            // slot -> Closed(actual site placeholders in that role's pattern).
            var body = surface;
            if (surface.Left is Relation legacy && legacy.Left is Relation candidateRoles &&
                StructuralConstraint.TryReadScope(candidateRoles, out _, out _, out _))
            {
                body = legacy;
                if (surface.Right is Relation { Left: Relation shared, Right: Relation disjoint })
                { SharedSelectedSites = shared; DisjointSelectedSites = disjoint; }
                else blocked.Add(surface);
            }
            if (body is not { Left: Relation roleScope, Right: Relation { Left: Relation distinct,
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
                foreach (var scope in new[] { SharedSelectedSites, DisjointSelectedSites })
                {
                    ReadScope(scope, out var pairs);
                    foreach (var item in pairs)
                    {
                        if (item is not Relation { Left: Relation first, Right: Relation second })
                        { blocked.Add(new Relation(scope, item)); continue; }
                        ReadSelection(first); ReadSelection(second);
                    }
                }
            }
            Roles = roles; Blocked = N.S(blocked.ToArray()); Unresolved = N.S(open.ToArray());

            void ReadScope(Relation scope, out IElement[] entries)
            {
                if (!StructuralConstraint.TryReadScope(scope, out entries, out var seam, out var tail))
                { entries = []; blocked.Add(scope); }
                else if (!seam.Equals(tail)) open.Add(scope);
            }

            void ReadSelection(Relation selection)
            {
                var role = roles.FirstOrDefault(candidate => N.Same(candidate.Slot, selection.Left));
                if (role is null || selection.Right is not Relation selected ||
                    !CommunicationShape.TryRead(role.PatternSurface, out var shape))
                { blocked.Add(selection); return; }
                ReadScope(selected, out var entries);
                if (!N.Distinct(entries) || entries.Any(site => !shape.Sites.Any(patternSite => N.Same(site, patternSite))))
                    blocked.Add(selection);
            }
        }
    }

    public sealed class Witness
    {
        public Relation Bindings { get; }
        public Relation Footprints { get; }
        public Relation Correspondences { get; }
        public Relation Checks { get; }
        public Relation SelectedSites { get; }
        public Relation Surface { get; }
        public IReadOnlyList<CommunicationMatch> Matches { get; }

        internal Witness(Question question, Option[] options, Relation checks)
        {
            var roles = question.Roles;
            Matches = options.Select(option => option.Match).ToArray();
            Bindings = N.S(roles.Select((role, index) => (IElement)new Relation(role.Slot,
                options[index].Match.Occurrence.Read)).ToArray());
            Footprints = N.S(roles.Select((role, index) => (IElement)new Relation(role.Slot,
                options[index].Surface)).ToArray());
            Correspondences = N.S(roles.Select((role, index) => (IElement)new Relation(role.Slot,
                options[index].Correspondence)).ToArray());
            SelectedSites = N.S(new[] { question.SharedSelectedSites, question.DisjointSelectedSites }
                .SelectMany(scope => N.E(scope).Cast<Relation>()).SelectMany(pair => new[] { (Relation)pair.Left, (Relation)pair.Right })
                .Distinct<Relation>(ReferenceEqualityComparer.Instance).Select(selection => (IElement)new Relation(selection,
                    N.S(options[Index(roles, selection.Left)].Select(selection)))).ToArray());
            Checks = checks; Surface = new(Bindings, new Relation(Footprints,
                new Relation(Correspondences, new Relation(Checks, SelectedSites))));
        }
    }

    internal sealed class Option(CommunicationMatch match, Relation? actualSiteMap = null)
    {
        internal CommunicationMatch Match { get; } = match;
        internal Participation Participation { get; } = new(new(match.Occurrence.Sites, match.Occurrence.Segments), actualSiteMap);
        internal IElement[] Sites => Participation.Sites;
        internal IElement[] Edges => Participation.Edges;
        internal Relation Surface => Participation.Surface;
        internal Relation? SiteMap => Participation.SiteMap;
        internal Relation Correspondence { get; } = actualSiteMap is null ? match.Witness : new(match.Witness, actualSiteMap);
        internal IElement[] Select(Relation selection) => Participation.Select(selection);
    }

    /// <summary>A local read of certified native occurrence participation. Both
    /// fresh search and restored witness validation use the same joint law.</summary>
    internal sealed class Participation(Relation surface, Relation? siteMap)
    {
        internal Relation Surface { get; } = surface;
        internal Relation? SiteMap { get; } = siteMap;
        internal IElement[] Sites { get; } = N.E((Relation)surface.Left);
        internal IElement[] Edges { get; } = N.E((Relation)surface.Right);
        internal IElement[] Select(Relation selection) => N.E((Relation)selection.Right).Select(site =>
            N.E(SiteMap!).Cast<Relation>().Single(binding => N.Same(binding.Left, site)).Right).ToArray();
    }

    public sealed class Assessment
    {
        public QualitativeMotifInduction Output { get; }
        public Question Question { get; }
        public Relation Budget { get; }
        public Relation CorrespondenceBudget { get; }
        public Relation CorrespondenceConsumed { get; }
        public Relation CorrespondenceRemaining { get; }
        public Relation Inventory { get; }
        public Relation Consumed { get; }
        public Relation Remaining { get; }
        public Relation Rejected { get; }
        public Relation Supported { get; }
        public Relation Refuted { get; }
        public Relation Open { get; }
        public Relation Blocked { get; }
        public Relation Stale { get; }
        public RetainedMatch? Retained { get; }
        public Relation Surface { get; }
        public IReadOnlyList<Witness> Witnesses { get; }
        public string Answer => N.E(Blocked).Length != 0 ? "Blocked" : N.E(Supported).Length != 0 ? "Yes" :
            N.E(Refuted).Length != 0 ? "No" : "Open";

        public Assessment(QualitativeMotifInduction output, Question question, int assignmentCapacity = 4096,
            int correspondenceCapacity = 4096, RetainedMatch? retained = null)
        {
            ArgumentNullException.ThrowIfNull(output); ArgumentNullException.ThrowIfNull(question);
            if (assignmentCapacity < 0) throw new ArgumentOutOfRangeException(nameof(assignmentCapacity));
            if (correspondenceCapacity < 0) throw new ArgumentOutOfRangeException(nameof(correspondenceCapacity));
            Output = output; Question = question; Retained = retained;
            var slots = Enumerable.Range(0, assignmentCapacity).Select(_ => (IElement)new Placeholder()).ToArray();
            Budget = N.S(slots);
            var mappingSlots = Enumerable.Range(0, question.HasSelectedSiteConstraints || retained is not null ? correspondenceCapacity : 0)
                .Select(_ => (IElement)new Placeholder()).ToArray();
            CorrespondenceBudget = N.S(mappingSlots);
            var mappingConsumed = new List<IElement>(); var mappingCut = false;
            var inventory = new List<IElement>(); var consumed = new List<IElement>(); var rejected = new List<IElement>();
            var supported = new List<IElement>(); var refuted = new List<IElement>();
            var pending = new List<IElement>(N.E(question.Unresolved)); var blocked = new List<IElement>(N.E(question.Blocked));
            var stale = new List<IElement>();
            if (retained is not null)
            {
                blocked.AddRange(N.E(retained.Blocked)); pending.AddRange(N.E(retained.Open));
                if (retained.IsValid)
                {
                    foreach (var pin in N.E(retained.Pins).Cast<Relation>())
                    {
                        var role = question.Roles.SingleOrDefault(item => N.Same(item.Slot, pin.Left));
                        if (role is null || pin.Right is not Relation row || !ReferenceEquals(role.PatternSurface, row.Left))
                            blocked.Add(new Relation(question.Surface, pin));
                    }
                    if (!ReferenceEquals(retained.Observation, output.Observation.Surface))
                    {
                        var mismatch = new Relation(retained.Surface, new Relation(retained.Observation, output.Observation.Surface));
                        stale.Add(mismatch); pending.Add(mismatch);
                    }
                }
            }
            var witnesses = new List<Witness>();
            if (question.IsValid && blocked.Count == 0 && stale.Count == 0 && (retained?.IsValid ?? true))
            {
                var choices = new List<Option[]>(); var complete = output.IsComplete; var absent = false;
                foreach (var role in question.Roles)
                {
                    var assessment = new CommunicationAssessment(output,
                        new CommunicationMessage(N.S(role.PatternSurface), N.S()));
                    var options = new List<Option>();
                    foreach (var match in assessment.Matches)
                    {
                        if (!question.HasSelectedSiteConstraints && retained is null)
                        {
                            var candidate = new Option(match);
                            // Traversal aliases are not fresh physical instances.
                            if (!options.Any(prior => SameSet(prior.Edges, candidate.Edges))) options.Add(candidate);
                            continue;
                        }
                        CommunicationShape.TryRead(role.PatternSurface, out var sourceShape);
                        CommunicationShape.TryRead(new(output.Observation.Frame.Surface, match.Family.Definition), out var targetShape);
                        foreach (var map in SiteMaps(sourceShape, targetShape, partial =>
                        {
                            if (mappingConsumed.Count >= mappingSlots.Length) { mappingCut = true; return false; }
                            mappingConsumed.Add(new Relation(mappingSlots[mappingConsumed.Count],
                                new Relation(role.Slot, new Relation(match.Occurrence.Read, partial))));
                            return true;
                        }))
                        {
                            var frameMap = N.S(sourceShape.Directions.Select((direction, index) =>
                                (IElement)new Relation(direction, targetShape.Directions[index])).ToArray());
                            var correspondence = new Relation(new Relation(role.PatternSurface, match.Occurrence.Read),
                                new Relation(new Relation(sourceShape.Frame, targetShape.Frame),
                                    new Relation(frameMap, new Relation(map, MapEdges(sourceShape, targetShape, map)))));
                            var mappedMatch = match with { Witness = correspondence };
                            var actual = N.S(N.E(map).Cast<Relation>().Select(binding => (IElement)new Relation(binding.Left,
                                N.E(match.Occurrence.SiteBindings).Cast<Relation>().Single(row => N.Same(row.Left, binding.Right)).Right)).ToArray());
                            var candidate = new Option(mappedMatch, actual);
                            if (retained is not null && !retained.Accepts(role, candidate.Surface, actual)) continue;
                            // Edge aliases are quotiented only when their full query-site
                            // correspondence also agrees. Named landmarks must not vanish.
                            if (!options.Any(prior => SameSet(prior.Edges, candidate.Edges) &&
                                SameMap(prior.SiteMap!, actual))) options.Add(candidate);
                        }
                        if (mappingCut) break;
                    }
                    choices.Add(options.ToArray());
                    inventory.Add(new Relation(role.Slot, new Relation(assessment.Surface,
                        N.S(options.Select(option => (IElement)option.Surface).ToArray()))));
                    absent |= assessment.RefutedCount != 0;
                    complete &= assessment.BlockedCount == 0 && assessment.UnresolvedCount == 0 &&
                        CommunicationShape.TryRead(role.PatternSurface, out var shape) && WithinGrammar(output, shape) && !mappingCut;
                }
                if (absent) refuted.Add(new Relation(question.Surface, N.S(inventory.ToArray())));
                else if (choices.Any(options => options.Length == 0) && complete)
                    refuted.Add(new Relation(question.Surface, N.S(inventory.ToArray())));
                else if (choices.Any(options => options.Length == 0))
                    pending.Add(new Relation(question.Surface, StructuralConstraint.Open(output.Unsearched, output.Unresolved,
                        output.Budget, CorrespondenceBudget, N.S(mappingConsumed.ToArray()))));
                else
                {
                    var selected = new Option[question.Roles.Count]; var cut = false;
                    Search(0);
                    if (witnesses.Count != 0) supported.Add(new Relation(question.Surface, witnesses[0].Surface));
                    else if (complete && !cut) refuted.Add(new Relation(question.Surface,
                        new Relation(N.S(inventory.ToArray()), new Relation(Budget, N.S(rejected.ToArray())))));
                    else pending.Add(new Relation(question.Surface, StructuralConstraint.Open(output.Unsearched,
                        output.Unresolved, output.Budget, N.S(consumed.ToArray()), N.S(rejected.ToArray()),
                        CorrespondenceBudget, N.S(mappingConsumed.ToArray()))));

                    void Search(int depth)
                    {
                        foreach (var option in choices[depth])
                        {
                            if (consumed.Count >= slots.Length) { cut = true; return; }
                            selected[depth] = option;
                            var partial = N.S(question.Roles.Take(depth + 1).Select((role, index) =>
                                (IElement)new Relation(role.Slot, selected[index].Match.Occurrence.Read)).ToArray());
                            consumed.Add(new Relation(slots[consumed.Count], partial));
                            var pass = CheckAssignments(question, selected.Take(depth + 1).Select(option => option.Participation).ToArray(),
                                out var checks, out var failures);
                            if (!pass) { rejected.Add(new Relation(partial, failures)); continue; }
                            if (depth + 1 == choices.Count)
                            { witnesses.Add(new(question, selected.ToArray(), checks)); return; }
                            Search(depth + 1);
                            if (cut || witnesses.Count != 0) return;
                        }
                    }

                }
            }
            Inventory = N.S(inventory.ToArray()); Consumed = N.S(consumed.ToArray());
            Remaining = N.S(slots.Skip(consumed.Count).ToArray()); Rejected = N.S(rejected.ToArray());
            Supported = N.S(supported.ToArray()); Refuted = N.S(refuted.ToArray());
            Open = N.S(pending.ToArray()); Blocked = N.S(blocked.ToArray()); Stale = N.S(stale.ToArray()); Witnesses = witnesses;
            CorrespondenceConsumed = N.S(mappingConsumed.ToArray());
            CorrespondenceRemaining = N.S(mappingSlots.Skip(mappingConsumed.Count).ToArray());
            Surface = new(new Relation(new Relation(output.Surface, question.Surface), Budget),
                new Relation(Inventory, new Relation(Consumed, new Relation(Remaining, new Relation(Rejected,
                    new Relation(Supported, new Relation(Refuted, new Relation(Open, new Relation(Blocked,
                        new Relation(CorrespondenceBudget, new Relation(CorrespondenceConsumed, CorrespondenceRemaining)))))))))));
            // Legacy assessments preserve their exact contract. A continued
            // read exposes memory and stale-event evidence in a separate native
            // envelope; a mismatch never silently searches a replacement event.
            if (retained is not null) Surface = new(Surface, new Relation(retained.Surface, Stale));
        }
    }

    internal static bool CheckAssignments(Question question, IReadOnlyList<Participation> selected,
        out Relation checks, out Relation failures)
    {
        var accepted = new List<IElement>(); var denied = new List<IElement>();
        Inspect(question.DistinctEdgeSets, edges: true, requireShared: null);
        Inspect(question.DisjointSites, edges: false, requireShared: false);
        Inspect(question.SharedSites, edges: false, requireShared: true);
        Inspect(question.SharedEdges, edges: true, requireShared: true);
        Inspect(question.DisjointEdges, edges: true, requireShared: false);
        InspectSelected(question.SharedSelectedSites, requireShared: true);
        InspectSelected(question.DisjointSelectedSites, requireShared: false);
        checks = N.S(accepted.ToArray()); failures = N.S(denied.ToArray()); return denied.Count == 0;

        void Inspect(Relation scope, bool edges, bool? requireShared)
        {
            foreach (var pair in N.E(scope).Cast<Relation>())
            {
                var a = Index(question.Roles, pair.Left); var b = Index(question.Roles, pair.Right);
                if (a >= selected.Count || b >= selected.Count) continue;
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

        void InspectSelected(Relation scope, bool requireShared)
        {
            foreach (var pair in N.E(scope).Cast<Relation>())
            {
                var first = (Relation)pair.Left; var second = (Relation)pair.Right;
                var a = Index(question.Roles, first.Left); var b = Index(question.Roles, second.Left);
                if (a >= selected.Count || b >= selected.Count) continue;
                var left = selected[a].Select(first); var right = selected[b].Select(second);
                var common = left.Where(site => right.Any(other => N.Same(site, other))).ToArray();
                var leftOnly = left.Where(site => !right.Any(other => N.Same(site, other))).ToArray();
                var rightOnly = right.Where(site => !left.Any(other => N.Same(site, other))).ToArray();
                var proof = new Relation(new Relation(scope, pair), new Relation(
                    new Relation(new Relation(first, N.S(left)), new Relation(second, N.S(right))),
                    new Relation(N.S(common), new Relation(N.S(leftOnly), N.S(rightOnly)))));
                ((common.Length != 0) == requireShared ? accepted : denied).Add(proof);
            }
        }
    }

    private static int Index(IReadOnlyList<Role> roles, IElement slot) =>
        Enumerable.Range(0, roles.Count).Single(index => N.Same(roles[index].Slot, slot));

    private static bool SameSet(IElement[] first, IElement[] second) => first.Length == second.Length &&
        first.All(item => second.Any(other => N.Same(item, other)));

    private static bool SameMap(Relation first, Relation second) => N.E(first).Cast<Relation>().All(binding =>
        N.E(second).Cast<Relation>().Any(other => N.Same(binding.Left, other.Left) && N.Same(binding.Right, other.Right)));

    private static Relation MapEdges(CommunicationShape source, CommunicationShape target, Relation siteMap)
    {
        var used = new HashSet<int>(); var bindings = new List<IElement>();
        var sites = N.E(siteMap).Cast<Relation>().ToArray();
        IElement Actual(IElement site) => sites.Single(binding => N.Same(binding.Left, site)).Right;
        for (var i = 0; i < source.Edges.Length; i++)
        {
            var from = Actual(source.Ends[i].Left); var to = Actual(source.Ends[i].Right);
            var j = Enumerable.Range(0, target.Edges.Length).First(index => !used.Contains(index) &&
                (N.Same(from, target.Ends[index].Left) && N.Same(to, target.Ends[index].Right) &&
                    source.Headings[i] == target.Headings[index] ||
                 N.Same(from, target.Ends[index].Right) && N.Same(to, target.Ends[index].Left) &&
                    source.Headings[i] == Opposite(target.Headings[index])));
            used.Add(j); bindings.Add(new Relation(source.Edges[i].Left, target.Edges[j].Left));
        }
        return N.S(bindings.ToArray());
        static int Opposite(int mask) => ((mask & 1) << 1) | ((mask & 2) >> 1) | ((mask & 4) << 1) | ((mask & 8) >> 1);
    }

    /// <summary>Enumerates the site-role correspondences relevant to selected
    /// sites. Distinct permutations of parallel edges at unchanged sites cannot
    /// change any supported question, so they remain a single read. Each star
    /// leaf extension or ordered path/cycle alignment consumes a native slot.
    /// A cut is reported by the caller; it cannot certify negative evidence.</summary>
    private static IEnumerable<Relation> SiteMaps(CommunicationShape source, CommunicationShape target,
        Func<Relation, bool> consume)
    {
        if (source.Sites.Length != target.Sites.Length || source.Edges.Length != target.Edges.Length ||
            source.Centres.Length != target.Centres.Length || source.IsCycle != target.IsCycle) yield break;
        var map = Enumerable.Repeat(-1, source.Sites.Length).ToArray();
        if (source.Centres.Length == 1)
        {
            var a = SiteIndex(source.Sites, source.Centres[0]); var b = SiteIndex(target.Sites, target.Centres[0]);
            map[a] = b;
            if (!consume(ReadMap())) yield break;
            foreach (var result in Leaves(0)) yield return result;
        }
        else
        {
            var count = source.Edges.Length;
            for (var reverse = 0; reverse < 2; reverse++)
            for (var start = 0; start < (source.IsCycle ? count : 1); start++)
            {
                Array.Fill(map, -1); var valid = true;
                for (var i = 0; i < count && valid; i++)
                {
                    var j = source.IsCycle ? (start + (reverse == 0 ? i : -i - 1) + 2 * count) % count :
                        reverse == 0 ? i : count - i - 1;
                    var heading = reverse == 0 ? target.Headings[j] : Opposite(target.Headings[j]);
                    var from = reverse == 0 ? target.Ends[j].Left : target.Ends[j].Right;
                    var to = reverse == 0 ? target.Ends[j].Right : target.Ends[j].Left;
                    valid = source.Headings[i] == heading && Bind(source.Ends[i].Left, from) && Bind(source.Ends[i].Right, to);
                }
                var partial = ReadMap();
                if (!consume(partial)) yield break;
                if (valid && map.All(index => index >= 0)) yield return partial;
            }
        }

        IEnumerable<Relation> Leaves(int index)
        {
            while (index < map.Length && map[index] >= 0) index++;
            if (index == map.Length) { yield return ReadMap(); yield break; }
            for (var selected = 0; selected < target.Sites.Length; selected++)
            {
                if (map.Contains(selected)) continue;
                map[index] = selected;
                if (!consume(ReadMap())) { map[index] = -1; yield break; }
                if (Profile(source, index).SequenceEqual(Profile(target, selected)))
                    foreach (var result in Leaves(index + 1)) yield return result;
                map[index] = -1;
            }
        }
        bool Bind(IElement first, IElement second)
        {
            var a = SiteIndex(source.Sites, first); var b = SiteIndex(target.Sites, second);
            if (map[a] >= 0) return map[a] == b;
            if (map.Contains(b)) return false;
            map[a] = b; return true;
        }
        Relation ReadMap() => N.S(source.Sites.Select((site, index) => (site, index))
            .Where(pair => map[pair.index] >= 0).Select(pair => (IElement)new Relation(pair.site, target.Sites[map[pair.index]])).ToArray());
        static int[] Profile(CommunicationShape shape, int site) => Enumerable.Range(0, shape.Edges.Length)
            .Where(index => N.Same(shape.Ends[index].Right, shape.Sites[site])).Select(index => shape.Headings[index]).Order().ToArray();
        static int SiteIndex(IElement[] sites, IElement site) => Array.FindIndex(sites, item => N.Same(item, site));
        static int Opposite(int mask) => ((mask & 1) << 1) | ((mask & 2) >> 1) | ((mask & 4) << 1) | ((mask & 8) >> 1);
    }

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

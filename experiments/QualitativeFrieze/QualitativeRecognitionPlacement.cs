using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Two ordinal projection observations over actual observed site roles.
/// Equality in one bucket means equal projection, not shared physical identity.
/// Endpoint envelopes are explicit whole-passage constraints, never a conclusion
/// from endpoint positions or coarse headings alone.</summary>
public sealed class QualitativeRecognitionPlacement
{
    // axis order = actual observation axis -> ordered scope of site buckets
    // bucket = Closed(actual observed sites with the same projection)
    // envelope = actual observed segment ->
    //   ((horizontal -> (first endpoint -> second endpoint)) ->
    //    (vertical -> (first endpoint -> second endpoint)))
    // The endpoint pairs denote unordered extents in their selected axis read.
    // Supplying a row declares that the whole segment's support stays in that
    // product of intervals. It does not declare equal lengths or a face.
    // comparison = (axis order -> (first -> second)) ->
    //              (before -> (equal -> (after -> unresolved)))
    public QualitativeRecognitionObservation Observation { get; }
    public Relation HorizontalBuckets { get; }
    public Relation VerticalBuckets { get; }
    public Relation HorizontalOrder { get; }
    public Relation VerticalOrder { get; }
    public Relation Envelopes { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => E(Blocked).Length == 0 && E(Unresolved).Length == 0;
    private readonly Axis horizontal;
    private readonly Axis vertical;

    public QualitativeRecognitionPlacement(QualitativeRecognitionObservation observation,
        Relation horizontalBuckets, Relation verticalBuckets, Relation? passageEnvelopes = null)
    {
        Observation = observation; HorizontalBuckets = horizontalBuckets; VerticalBuckets = verticalBuckets;
        HorizontalOrder = new(observation.Frame.Horizontal, horizontalBuckets);
        VerticalOrder = new(observation.Frame.Vertical, verticalBuckets);
        Envelopes = passageEnvelopes ?? S();
        var blocked = new List<IElement>(); var pending = new List<IElement>(E(observation.Unresolved));
        horizontal = ReadAxis(HorizontalOrder); vertical = ReadAxis(VerticalOrder);
        if (!C(observation.Sites, out var observedSites) || !N.Distinct(observedSites)) pending.Add(observation.Sites);
        if (!C(Envelopes, out var envelopes)) pending.Add(Envelopes);
        foreach (var item in envelopes)
            if (item is not Relation row || !Envelope(row, out _) ||
                envelopes.OfType<Relation>().Count(other => ReferenceEquals(other.Left, row.Left)) != 1)
                blocked.Add(item);
        Blocked = S(blocked.ToArray()); Unresolved = S(pending.ToArray());
        Surface = new(observation.Surface, new Relation(new Relation(HorizontalOrder, VerticalOrder),
            new Relation(Envelopes, new Relation(Blocked, Unresolved))));

        Axis ReadAxis(Relation order)
        {
            var buckets = (Relation)order.Right;
            var valid = C(buckets, out var entries);
            if (!valid) pending.Add(order);
            var membership = new Dictionary<IElement, int>(N.Participants.Instance);
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] is not Relation bucket || !C(bucket, out var sites) || sites.Length == 0)
                { valid = false; pending.Add(new Relation(order, entries[i])); continue; }
                foreach (var site in sites)
                    if (!E(observation.Sites).Any(actual => N.Same(site, actual)) || !membership.TryAdd(site, i))
                    { valid = false; blocked.Add(new Relation(order, site)); }
            }
            foreach (var site in E(observation.Sites).Where(site => !membership.ContainsKey(site)))
                pending.Add(new Relation(order, site));
            foreach (var item in E(observation.Segments))
            {
                if (item is not Relation segment || !observation.TryReadSegment(segment, out _, out var from, out var to, out var heading) ||
                    !C(heading, out var dirs) || !membership.TryGetValue(from, out var a) || !membership.TryGetValue(to, out var b)) continue;
                var forward = ReferenceEquals(order.Left, observation.Frame.Horizontal) ? observation.Frame.Right : observation.Frame.Up;
                var inverse = ReferenceEquals(order.Left, observation.Frame.Horizontal) ? observation.Frame.Left : observation.Frame.Down;
                var expected = dirs.Any(dir => ReferenceEquals(dir, forward)) ? 1 : dirs.Any(dir => ReferenceEquals(dir, inverse)) ? -1 : 0;
                if (Math.Sign(b - a) != expected) { valid = false; blocked.Add(new Relation(order, segment)); }
            }
            return new(order, entries.OfType<Relation>().ToArray(), membership, valid);
        }
    }

    /// <summary>Local order evidence, preserving equality and reversed order as
    /// separate results. An open or contradictory axis does not settle a pair.</summary>
    public Relation Compare(IElement axis, IElement first, IElement second)
    {
        var selected = Select(axis); var scope = selected?.Order ?? new Relation(axis, StructuralConstraint.Open());
        var request = new Relation(scope, new Relation(first, second));
        var before = S(); var equal = S(); var after = S(); var unresolved = S();
        if (selected is null || !selected.Valid || !selected.Membership.TryGetValue(first, out var a) ||
            !selected.Membership.TryGetValue(second, out var b)) unresolved = S(request);
        else if (a == b) equal = S(selected.Buckets[a]);
        else
        {
            var span = S(selected.Buckets.Skip(Math.Min(a, b)).Take(Math.Abs(a - b) + 1).Cast<IElement>().ToArray());
            if (a < b) before = S(new Relation(new Relation(first, second), span));
            else after = S(new Relation(new Relation(second, first), span));
        }
        return new(request, new Relation(before, new Relation(equal, new Relation(after, unresolved))));
    }

    public bool TryReadComparison(Relation read, out Relation before, out Relation equal,
        out Relation after, out Relation unresolved)
    {
        before = equal = after = unresolved = null!;
        if (read.Left is not Relation { Left: Relation order, Right: Relation pair } ||
            !ReferenceEquals(order, HorizontalOrder) && !ReferenceEquals(order, VerticalOrder) ||
            read.Right is not Relation { Left: Relation a, Right: Relation { Left: Relation b,
                Right: Relation { Left: Relation c, Right: Relation d } } } ||
            !C(a, out _) || !C(b, out _) || !C(c, out _) || !C(d, out _)) return false;
        // This read is exact for the selected stable observation, not an
        // arbitrary result-shaped object whose premise happens to look right.
        C(a, out var beforeRows); C(b, out var equalRows); C(c, out var afterRows); C(d, out var pendingRows);
        var selected = Select(order.Left)!;
        if (!selected.Valid || !selected.Membership.TryGetValue(pair.Left, out var first) || !selected.Membership.TryGetValue(pair.Right, out var second))
        {
            if (beforeRows.Length != 0 || equalRows.Length != 0 || afterRows.Length != 0 ||
                pendingRows.Length != 1 || !ReferenceEquals(pendingRows[0], read.Left)) return false;
        }
        else if (first == second)
        {
            if (beforeRows.Length != 0 || afterRows.Length != 0 || pendingRows.Length != 0 ||
                equalRows.Length != 1 || !ReferenceEquals(equalRows[0], selected.Buckets[first])) return false;
        }
        else
        {
            var active = first < second ? beforeRows : afterRows; var absent = first < second ? afterRows : beforeRows;
            var low = first < second ? pair.Left : pair.Right; var high = first < second ? pair.Right : pair.Left;
            var expected = selected.Buckets.Skip(Math.Min(first, second)).Take(Math.Abs(first - second) + 1).ToArray();
            if (absent.Length != 0 || equalRows.Length != 0 || pendingRows.Length != 0 || active is not [Relation span] ||
                span.Left is not Relation ends || !N.Same(ends.Left, low) || !N.Same(ends.Right, high) ||
                span.Right is not Relation scope || !C(scope, out var offered) || offered.Length != expected.Length ||
                offered.Where((bucket, index) => !ReferenceEquals(bucket, expected[index])).Any()) return false;
        }
        before = a; equal = b; after = c; unresolved = d; return true;
    }

    public QualitativeRecognitionContainment ReadContainment(Relation outerSegments, Relation innerSegments)
    {
        var request = new Relation(Surface, new Relation(outerSegments, innerSegments));
        var blocked = new List<IElement>(); var pending = new List<IElement>(); var proofs = new List<IElement>();
        var insideSites = new List<IElement>(); var touchingSites = new List<IElement>(); var outsideSites = new List<IElement>();
        var accepted = new List<IElement>(); Relation rectangle = StructuralConstraint.Open();
        var outer = Segments(outerSegments, "outer"); var inner = Segments(innerSegments, "inner");
        var outerSites = outer.SelectMany(edge => new[] { edge.From, edge.To }).Distinct(N.Participants.Instance).ToArray();
        var innerSites = inner.SelectMany(edge => new[] { edge.From, edge.To }).Distinct(N.Participants.Instance).ToArray();
        var rectangleKnown = true;
        if (outer.Length != 4 || outerSites.Length != 4 || outerSites.Any(site =>
                outer.Count(edge => N.Same(edge.From, site) || N.Same(edge.To, site)) != 2))
        { blocked.Add(new Relation(outerSegments, S(outerSites))); rectangleKnown = false; }
        if (rectangleKnown)
        {
            var connected = new HashSet<IElement>(N.Participants.Instance) { outerSites[0] };
            var changed = true;
            while (changed)
            {
                changed = false;
                foreach (var edge in outer)
                {
                    if (connected.Contains(edge.From)) changed |= connected.Add(edge.To);
                    if (connected.Contains(edge.To)) changed |= connected.Add(edge.From);
                }
            }
            if (connected.Count != 4) { blocked.Add(new Relation(outerSegments, S(connected.ToArray()))); rectangleKnown = false; }
        }
        if (!horizontal.Valid || !vertical.Valid || outerSites.Any(site =>
                !horizontal.Membership.ContainsKey(site) || !vertical.Membership.ContainsKey(site)))
        { pending.Add(new Relation(outerSegments, new Relation(HorizontalOrder, VerticalOrder))); rectangleKnown = false; }
        var minX = 0; var maxX = 0; var minY = 0; var maxY = 0;
        if (rectangleKnown)
        {
            var xs = outerSites.Select(site => horizontal.Membership[site]).Distinct().Order().ToArray();
            var ys = outerSites.Select(site => vertical.Membership[site]).Distinct().Order().ToArray();
            if (xs.Length != 2 || ys.Length != 2 || outerSites.Select(site =>
                    (horizontal.Membership[site], vertical.Membership[site])).Distinct().Count() != 4)
            { blocked.Add(new Relation(outerSegments, S(outerSites))); rectangleKnown = false; }
            else { minX = xs[0]; maxX = xs[1]; minY = ys[0]; maxY = ys[1]; }
        }
        foreach (var edge in outer)
        {
            if (!rectangleKnown) break;
            var x = horizontal.Membership[edge.From]; var xx = horizontal.Membership[edge.To];
            var y = vertical.Membership[edge.From]; var yy = vertical.Membership[edge.To];
            if (!C(edge.Heading, out var dirs)) { pending.Add(edge.Row); rectangleKnown = false; continue; }
            var expected = x == xx && y != yy ? (yy > y ? Observation.Frame.Up : Observation.Frame.Down) :
                y == yy && x != xx ? (xx > x ? Observation.Frame.Right : Observation.Frame.Left) : null;
            if (expected is null || dirs.Length != 1 || !ReferenceEquals(dirs[0], expected))
            { blocked.Add(new Relation(edge.Row, new Relation(HorizontalOrder, VerticalOrder))); rectangleKnown = false; }
        }
        if (rectangleKnown)
        {
            var low = outerSites.Single(site => horizontal.Membership[site] == minX && vertical.Membership[site] == minY);
            var high = outerSites.Single(site => horizontal.Membership[site] == maxX && vertical.Membership[site] == maxY);
            rectangle = new(new Relation(outerSegments, S(outerSites)), new Relation(
                new Relation(HorizontalOrder, new Relation(low, high)), new Relation(VerticalOrder, new Relation(low, high))));
            foreach (var edge in outer.Concat(inner))
                if (!C(Envelopes, out _) || E(Envelopes).OfType<Relation>().Where(row => ReferenceEquals(row.Left, edge.Row)).ToArray() is not [var envelope] ||
                    !Envelope(envelope, out _)) pending.Add(new Relation(edge.Row, Envelopes));
                else proofs.Add(envelope);
            foreach (var site in innerSites)
            {
                if (!horizontal.Membership.TryGetValue(site, out var x) || !vertical.Membership.TryGetValue(site, out var y))
                { pending.Add(new Relation(site, new Relation(HorizontalOrder, VerticalOrder))); continue; }
                var proof = new Relation(site, new Relation(
                    new Relation(Compare(Observation.Frame.Horizontal, low, site), Compare(Observation.Frame.Horizontal, site, high)),
                    new Relation(Compare(Observation.Frame.Vertical, low, site), Compare(Observation.Frame.Vertical, site, high))));
                proofs.Add(proof);
                if (x < minX || x > maxX || y < minY || y > maxY) outsideSites.Add(proof);
                else if (x == minX || x == maxX || y == minY || y == maxY) touchingSites.Add(proof);
                else insideSites.Add(proof);
            }
            if (inner.Length > 0 && insideSites.Count == innerSites.Length && pending.Count == 0 && blocked.Count == 0)
                accepted.Add(new Relation(rectangle, new Relation(innerSegments, S(proofs.ToArray()))));
        }
        var inside = S(accepted.ToArray()); var interior = S(insideSites.ToArray()); var touching = S(touchingSites.ToArray());
        var outside = S(outsideSites.ToArray()); var evidence = S(proofs.ToArray()); var rejected = S(blocked.ToArray()); var unresolved = S(pending.ToArray());
        var surface = new Relation(request, new Relation(rectangle, new Relation(inside, new Relation(interior,
            new Relation(touching, new Relation(outside, new Relation(evidence, new Relation(rejected, unresolved))))))));
        return new(surface, outerSegments, innerSegments, rectangle, inside, interior, touching, outside, evidence, rejected, unresolved);

        Edge[] Segments(Relation scope, string role)
        {
            if (!C(scope, out var rows)) pending.Add(scope);
            var result = new List<Edge>();
            foreach (var item in rows)
                if (item is not Relation row || !Observation.TryReadSegment(row, out var identity, out var from, out var to, out var heading) ||
                    result.Any(edge => ReferenceEquals(edge.Row, row))) blocked.Add(item);
                else
                {
                    // A row reference identifies the observed description, but
                    // duplicated participant identity leaves which passage it
                    // describes unsettled. Keep that local ambiguity separate
                    // from unrelated malformed observations elsewhere.
                    var aliases = E(Observation.Segments).OfType<Relation>().Where(other => N.Same(other.Left, identity)).ToArray();
                    if (aliases.Length != 1) pending.Add(new Relation(row, S(aliases)));
                    foreach (var endpoint in new[] { from, to })
                    {
                        var siteAliases = E(Observation.Sites).Where(site => N.Same(site, endpoint)).ToArray();
                        if (siteAliases.Length != 1) pending.Add(new Relation(endpoint, S(siteAliases)));
                    }
                    result.Add(new(row, from, to, heading));
                }
            if (result.Count == 0) pending.Add(new Relation(scope, StructuralConstraint.Open()));
            return result.ToArray();
        }
    }

    private bool Envelope(Relation envelope, out Relation segment)
    {
        segment = null!;
        if (envelope.Left is not Relation row || !Observation.TryReadSegment(row, out _, out var from, out var to, out _) ||
            envelope.Right is not Relation { Left: Relation { Left: var horizontalAxis, Right: Relation horizontalEnds },
                Right: Relation { Left: var verticalAxis, Right: Relation verticalEnds } } ||
            !ReferenceEquals(horizontalAxis, Observation.Frame.Horizontal) || !ReferenceEquals(verticalAxis, Observation.Frame.Vertical) ||
            !Ends(horizontalEnds, from, to) || !Ends(verticalEnds, from, to)) return false;
        segment = row; return true;
    }
    private static bool Ends(Relation pair, IElement from, IElement to) =>
        N.Same(pair.Left, from) && N.Same(pair.Right, to) || N.Same(pair.Left, to) && N.Same(pair.Right, from);
    private Axis? Select(IElement axis) => ReferenceEquals(axis, Observation.Frame.Horizontal) ? horizontal :
        ReferenceEquals(axis, Observation.Frame.Vertical) ? vertical : null;
    private sealed class Axis(Relation order, Relation[] buckets, Dictionary<IElement, int> membership, bool valid)
    { public Relation Order { get; } = order; public Relation[] Buckets { get; } = buckets; public Dictionary<IElement, int> Membership { get; } = membership; public bool Valid { get; } = valid; }
    private sealed record Edge(Relation Row, IElement From, IElement To, Relation Heading);
    private static IElement[] E(Relation scope) => QualitativeRecognitionObservation.Entries(scope);
    private static bool C(Relation scope, out IElement[] rows) => QualitativeRecognitionObservation.Closed(scope, out rows);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}

/// <summary>A local containment read conditional on the published axis orders
/// and whole-passage envelope premises. Touching is projected boundary equality,
/// not a claim of a shared physical site or contact.</summary>
public sealed class QualitativeRecognitionContainment(Relation surface, Relation outerSegments, Relation innerSegments,
    Relation rectangle, Relation inside, Relation interiorSites, Relation touchingSites, Relation outsideSites,
    Relation evidence, Relation blocked, Relation unresolved)
{
    public Relation Surface { get; } = surface;
    public Relation OuterSegments { get; } = outerSegments;
    public Relation InnerSegments { get; } = innerSegments;
    public Relation Rectangle { get; } = rectangle;
    public Relation Inside { get; } = inside;
    public Relation InteriorSites { get; } = interiorSites;
    public Relation TouchingSites { get; } = touchingSites;
    public Relation OutsideSites { get; } = outsideSites;
    public Relation Evidence { get; } = evidence;
    public Relation Blocked { get; } = blocked;
    public Relation Unresolved { get; } = unresolved;
    public bool IsStrictlyInside => N.E(Inside).Length == 1 && N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;
}

/// <summary>External ordinal supplier. Source words and comparison proofs remain
/// outside Placement. Endpoint envelopes require explicit source declarations;
/// knowledge of endpoint words never publishes those promises automatically.</summary>
public sealed class QualitativeRecognitionPlacementPublisher
{
    public QualitativeRecognitionPlacement Placement { get; }
    public Relation Reads { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }

    public QualitativeRecognitionPlacementPublisher(QualitativeMotionFrame sourceFrame, Relation sourceBindings,
        QualitativeRecognitionPublisher publication, Relation? declaredEnvelopes = null)
    {
        var reads = new List<IElement>(); var pending = new List<IElement>(); var blocked = new List<IElement>();
        var sites = E(publication.Observation.Sites); var words = new Relation?[sites.Length];
        var bindingClosed = C(sourceBindings, out var bindings);
        for (var i = 0; i < sites.Length; i++)
        {
            var sources = E(publication.SiteMap).Cast<Relation>().Where(row => N.Same(row.Right, sites[i])).ToArray();
            var values = sources.Length == 1 ? bindings.OfType<Relation>().Where(row => N.Same(row.Left, sources[0].Left)).ToArray() : [];
            if (bindingClosed && values is [var binding] && binding.Right is Relation word && ValidWord(word)) words[i] = word;
            else pending.Add(new Relation(sites[i], sourceBindings));
        }
        var horizontal = Order(sourceFrame.Horizontal); var vertical = Order(sourceFrame.Vertical);
        var sourceEnvelopes = declaredEnvelopes ?? S(); var freshEnvelopes = new List<IElement>();
        var envelopesClosed = C(sourceEnvelopes, out var envelopeRows);
        if (!envelopesClosed) pending.Add(sourceEnvelopes);
        foreach (var item in envelopeRows)
        {
            if (item is not Relation row || !publication.TryMapSegment(row.Left, out var observed) ||
                row.Right is not Relation { Left: Relation { Left: var horizontalAxis, Right: Relation horizontalEnds },
                    Right: Relation { Left: var verticalAxis, Right: Relation verticalEnds } } ||
                !ReferenceEquals(horizontalAxis, sourceFrame.Horizontal) || !ReferenceEquals(verticalAxis, sourceFrame.Vertical) ||
                !publication.TryMapSite(horizontalEnds.Left, out var hf) || !publication.TryMapSite(horizontalEnds.Right, out var ht) ||
                !publication.TryMapSite(verticalEnds.Left, out var vf) || !publication.TryMapSite(verticalEnds.Right, out var vt) ||
                !publication.Observation.TryReadSegment(observed, out _, out var from, out var to, out _) ||
                !Pair(hf, ht, from, to) || !Pair(vf, vt, from, to))
            { blocked.Add(item); continue; }
            var fresh = new Relation(observed, new Relation(new Relation(publication.Observation.Frame.Horizontal, new Relation(hf, ht)),
                new Relation(publication.Observation.Frame.Vertical, new Relation(vf, vt))));
            freshEnvelopes.Add(fresh); reads.Add(new Relation(row, fresh));
        }
        Placement = new(publication.Observation, horizontal, vertical,
            envelopesClosed ? S(freshEnvelopes.ToArray()) : StructuralConstraint.Open(freshEnvelopes.ToArray()));
        Reads = S(reads.ToArray()); Blocked = S(blocked.ToArray()); Unresolved = S(pending.ToArray());
        Surface = new(new Relation(publication.Surface, new Relation(sourceFrame.Surface, sourceBindings)),
            new Relation(Placement.Surface, new Relation(Reads, new Relation(Blocked, Unresolved))));

        Relation Order(Relation axis)
        {
            var projections = words.Select(word => word is null ? null : S(E(word).Cast<Relation>().Where(move =>
                sourceFrame.Direction(move, out var selected, out _, out _) && ReferenceEquals(axis, selected)).Cast<IElement>().ToArray())).ToArray();
            var comparisons = new int?[sites.Length, sites.Length]; var complete = words.All(word => word is not null);
            for (var i = 0; i < sites.Length; i++)
            {
                comparisons[i, i] = 0;
                for (var j = i + 1; j < sites.Length; j++)
                {
                    if (projections[i] is not Relation a || projections[j] is not Relation b) { complete = false; continue; }
                    var difference = sourceFrame.Reduce(b, Inverse(a));
                    reads.Add(new Relation(new Relation(axis, new Relation(sites[i], sites[j])), difference));
                    if (!sourceFrame.TryReadResult(difference, out var remainder, out _, out var unknown) ||
                        !C(remainder, out var terms) || E(unknown).Length != 0) { complete = false; continue; }
                    int? comparison = terms.Length == 0 ? 0 : terms.All(term => term is Relation move &&
                        sourceFrame.Direction(move, out _, out var forward, out _) && forward) ? -1 :
                        terms.All(term => term is Relation move && sourceFrame.Direction(move, out _, out var forward, out _) && !forward) ? 1 : null;
                    if (comparison is null) { complete = false; continue; }
                    comparisons[i, j] = comparison; comparisons[j, i] = -comparison;
                }
            }
            if (!complete)
            {
                var open = StructuralConstraint.Open(); pending.Add(new Relation(axis, open)); return open;
            }
            var classes = new List<List<int>>();
            for (var i = 0; i < sites.Length; i++)
            {
                var group = classes.FirstOrDefault(existing => comparisons[existing[0], i] == 0);
                if (group is null) classes.Add([i]); else group.Add(i);
            }
            classes.Sort((a, b) => comparisons[a[0], b[0]]!.Value);
            return S(classes.Select(group => (IElement)S(group.Select(index => sites[index]).ToArray())).ToArray());
        }
        bool ValidWord(Relation word) => C(word, out var terms) && terms.All(term => term is Relation move && sourceFrame.Direction(move, out _, out _, out _));
        Relation Inverse(Relation word) => S(E(word).Reverse().Cast<Relation>().Select(move =>
            (IElement)QualitativeMotionFrame.Move(E(sourceFrame.Opposites).Cast<Relation>().Single(pair => ReferenceEquals(pair.Left, move.Left)).Right, move.Right)).ToArray());
    }

    /// <summary>Construct an explicit source premise. Calling this helper does
    /// not prove its physical truth; the supplier selects this line-support
    /// model and retains the declaration as provenance.</summary>
    public static Relation Envelope(QualitativeMotionFrame frame, IElement edge, IElement from, IElement to) =>
        new(edge, new Relation(new Relation(frame.Horizontal, new Relation(from, to)),
            new Relation(frame.Vertical, new Relation(from, to))));
    private static bool Pair(IElement a, IElement b, IElement from, IElement to) =>
        N.Same(a, from) && N.Same(b, to) || N.Same(a, to) && N.Same(b, from);
    private static IElement[] E(Relation scope) => QualitativeRecognitionObservation.Entries(scope);
    private static bool C(Relation scope, out IElement[] rows) => QualitativeRecognitionObservation.Closed(scope, out rows);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}

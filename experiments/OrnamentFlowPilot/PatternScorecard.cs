using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentTension;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.OrnamentFlowPilot;

internal sealed record SupportStroke(int Slot, int From, int To, int Owner, int Beat, bool Participates);
internal sealed record PatternCount(int Met, int Checked)
{
    public int Misses => Checked - Met;
}
internal sealed record PatternCheck(string Key, string Claim, int Met, int Checked, string? Limit = null)
{
    public int Misses => Checked - Met;
    public bool? Passed => Checked == 0 ? null : Met == Checked;
}
internal sealed record PatternScorecardRead(PathSite[] Sites, SupportStroke[] Strokes,
    int NativeComponents, int NativeEnds, int CollinearOverlaps, int InteriorCrossings,
    int UnjoinedContacts, PatternCount Handoffs, bool? PeriodicSeam,
    CourseSignature? Course, FretSymmetryRead? Windows, GapRead? Gaps, string? DescriptorLimit)
{
    public int StrokeSlots => Strokes.Length;
    public int DepositedStrokes => Strokes.Count(s => s.Participates);
    public int AbsentStrokes => StrokeSlots - DepositedStrokes;
}

// An external observer of a declared drawing surface, not a generator or a
// learned quality law. A deposit selection keeps native sites and stroke slots:
// an absent mark cannot move its later neighbors or connect across its gap.
internal static class PatternScorecard
{
    internal static PatternScorecardRead Read(PathTrace source, ParticipationStrip? deposit = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (deposit is not null && deposit.Count != source.Edges.Length)
            throw new ArgumentException("The deposition surface needs one slot per source stroke.", nameof(deposit));
        Validate(source);
        bool[] selected = deposit is null ? source.Edges.Select(_ => true).ToArray() :
            new GateWalk(deposit).Run().Select(visit => visit.Marked).ToArray();
        var strokes = source.Edges.Select((edge, i) =>
            new SupportStroke(i, edge.From, edge.To, edge.Owner, edge.Beat, selected[i])).ToArray();
        var edges = source.Edges.Where((_, i) => selected[i]).ToArray();
        var material = source with { Edges = edges };
        var joints = JointCensus.Read(material);

        var parent = Enumerable.Range(0, source.Sites.Length).ToArray();
        var degree = new int[parent.Length];
        int Root(int i)
        {
            while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; }
            return i;
        }
        foreach (var edge in edges)
        {
            parent[Root(edge.To)] = Root(edge.From);
            degree[edge.From]++;
            degree[edge.To]++;
        }
        int components = Enumerable.Range(0, parent.Length).Where(i => degree[i] != 0).Select(Root).Distinct().Count();
        int links = 0, linked = 0;
        var previous = new SupportStroke?[source.Owners];
        foreach (var stroke in strokes)
        {
            if (previous[stroke.Owner] is { } last)
            {
                links++;
                if (last.Participates && stroke.Participates && last.To == stroke.From) linked++;
            }
            previous[stroke.Owner] = stroke;
        }

        int contacts = 0;
        for (int i = 0; i < edges.Length; i++)
        for (int j = i + 1; j < edges.Length; j++)
        {
            var a = edges[i]; var b = edges[j];
            if (a.From == b.From || a.From == b.To || a.To == b.From || a.To == b.To) continue;
            var p = source.Sites[a.From]; var q = source.Sites[a.To];
            var r = source.Sites[b.From]; var s = source.Sites[b.To];
            bool collinear = Cross(p, q, r) == 0 && Cross(p, q, s) == 0;
            if (collinear && PositiveOverlap(p, q, r, s)) continue;
            if (On(p, q, r) || On(p, q, s) || On(r, s, p) || On(r, s, q)) contacts++;
        }
        return new(source.Sites.ToArray(), strokes, components, degree.Count(d => d == 1),
            joints.CollinearOverlaps, joints.InteriorCrossings, contacts, new(linked, links), null,
            null, null, null, "This finite material read has no selected periodic course.");
    }

    // The supplied vectors are a realized ordered course. The n+1 vertices are
    // a read of its sequential joins, not newly inferred joints from coordinates.
    // Across bays, the selected continuation identifies last with translated first.
    internal static PatternScorecardRead ReadCourse(IReadOnlyList<PathSite> segments, ParticipationStrip? deposit = null)
    {
        var trace = CourseTrace(segments, 1);
        var sites = trace.Sites;
        var read = Read(trace, deposit);
        bool seam = sites[^1].X > 0 && sites[^1].Y == 0 && read.Strokes[0].Participates && read.Strokes[^1].Participates;
        read = read with { PeriodicSeam = seam };
        if (read.AbsentStrokes != 0)
            return read with { DescriptorLimit = "Material is missing. Whole-course symmetry and white-region readers require every stroke; their old answers are not reused and the gap is not bridged." };
        if (segments.Count is < 3 or > 16 || segments.Any(s => (s.X == 0) == (s.Y == 0)) || sites[^1].X <= 0 || sites[^1].Y != 0)
            return read with { DescriptorLimit = "Periodic descriptors currently require 3–16 nonzero cardinal strokes, positive horizontal pitch and zero net rise." };
        var windows = FretSymmetry.Read(segments);
        var gaps = GapSignature.Read(segments);
        bool allCorners = Enumerable.Range(0, segments.Count).All(i =>
        {
            var a = segments[i]; var b = segments[(i + 1) % segments.Count];
            return (long)a.X * b.Y - (long)a.Y * b.X != 0;
        });
        return read with { Course = allCorners ? CourseSignature.Read(segments) : null, Windows = windows, Gaps = gaps,
            DescriptorLimit = allCorners ? null : "The turn-course reader requires a genuine corner at every stored join; window and white-region reads remain available." };
    }

    // A bounded neighbor check. Site n is the actual end of the first course
    // and start of the second, rather than a coincident replacement site.
    // The strip, when supplied, selects deposits across the entire finite run.
    // Two copies do not prove absence of interactions with every distant copy.
    internal static PatternScorecardRead ReadRepeatedCourse(IReadOnlyList<PathSite> segments,
        int copies = 2, ParticipationStrip? deposit = null) => Read(CourseTrace(segments, copies), deposit);

    private static PathTrace CourseTrace(IReadOnlyList<PathSite> segments, int copies)
    {
        ArgumentNullException.ThrowIfNull(segments);
        if (segments.Count == 0) throw new ArgumentException("Supply at least one stroke.", nameof(segments));
        if (copies is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(copies));
        int count = checked(segments.Count * copies);
        var sites = new PathSite[count + 1];
        var edges = new PathEdge[count];
        for (int i = 0; i < count; i++)
        {
            int slot = i % segments.Count;
            var step = segments[slot];
            sites[i + 1] = new(checked(sites[i].X + step.X), checked(sites[i].Y + step.Y));
            edges[i] = new(i, i + 1, 0, i, i / segments.Count, slot, Heading(step), 0);
        }
        return new(sites, edges, [], sites[segments.Count].X, copies, 1);
    }

    // The caller chooses which obligations matter for the present purpose.
    // Crossing, asymmetry and disconnected decoration are not universal defects.
    internal static PatternCheck[] Evaluate(PatternScorecardRead read, params string[] goals) =>
        goals.Select(key => Goal(read, key)).ToArray();

    private static PatternCheck Goal(PatternScorecardRead read, string key)
    {
        PatternCheck Boolean(string claim, bool? passed, string? limit = null) =>
            new(key, claim, passed == true ? 1 : 0, passed.HasValue ? 1 : 0, passed.HasValue ? null : limit);
        switch (key)
        {
            case "participation": return new(key, "Every selected stroke slot deposits material.", read.DepositedStrokes, read.StrokeSlots);
            case "handoffs": return new(key, "Successive recorded strokes per owner share their native site and both participate.", read.Handoffs.Met, read.Handoffs.Checked);
            case "seam": return Boolean("Last and first strokes participate at the selected horizontal continuation.", read.PeriodicSeam,
                "No periodic continuation was selected for this finite trace.");
            case "no-overdraw":
                int pairs = checked(read.DepositedStrokes * (read.DepositedStrokes - 1) / 2);
                return new(key, "No tested pair deposits the same positive-length collinear support.", pairs - read.CollinearOverlaps, pairs);
            case "common-nearest-gap":
                if (read.Gaps is not { } gaps) return Boolean("Each stroke has the same nearest parallel clearance.", null, read.DescriptorLimit);
                var nearest = gaps.Nearest.Select(n => n.Width).ToArray();
                long? first = nearest.FirstOrDefault(width => width.HasValue);
                return new(key, "Each stroke has the same nearest parallel clearance as the first measured stroke.",
                    first.HasValue ? nearest.Count(width => width == first) : 0, first.HasValue ? nearest.Length : 0,
                    first.HasValue ? null : "No parallel clearance was available.");
            case "alternating-mouths": return Boolean("Open white mouths alternate between the top and bottom banks.", read.Gaps?.AlternatingMouths, read.DescriptorLimit);
            case "no-sealed-regions":
                if (read.Gaps is not { } regions) return Boolean("Every white region reaches a bank.", null, read.DescriptorLimit);
                return new(key, "Every white region reaches a bank.", regions.Regions.Count(r => r.Top || r.Bottom), regions.Regions.Length);
            case "glide": return Boolean("An exact whole-course glide reflection exists.", read.Windows is { } w ? w.Band.Any(b => b.Kind == "glide reflection") : null, read.DescriptorLimit);
            default: throw new ArgumentException($"Unknown observer obligation '{key}'.", nameof(key));
        }
    }

    private static void Validate(PathTrace trace)
    {
        if (trace.Owners <= 0 || trace.Sites.Any(p => Math.Abs((long)p.X) > 1_000_000 || Math.Abs((long)p.Y) > 1_000_000))
            throw new ArgumentException("The finite observer requires owners and sites within its exact integer working range.", nameof(trace));
        foreach (var edge in trace.Edges)
        {
            if ((uint)edge.From >= trace.Sites.Length || (uint)edge.To >= trace.Sites.Length || (uint)edge.Owner >= trace.Owners)
                throw new ArgumentException("A stroke refers outside its native site or owner table.", nameof(trace));
            var a = trace.Sites[edge.From]; var b = trace.Sites[edge.To];
            if (Heading(new(b.X - a.X, b.Y - a.Y)) != edge.Direction)
                throw new ArgumentException("Stored heading disagrees with the realized stroke.", nameof(trace));
        }
    }

    private static int Heading(PathSite v)
    {
        if (v.X == 0 && v.Y == 0 || v.X != 0 && v.Y != 0 && Math.Abs((long)v.X) != Math.Abs((long)v.Y))
            throw new ArgumentException("The support observer requires a nonzero cardinal or 45-degree stroke.");
        return (Math.Sign(v.X), Math.Sign(v.Y)) switch
        {
            (1, 0) => 0, (1, 1) => 1, (0, 1) => 2, (-1, 1) => 3,
            (-1, 0) => 4, (-1, -1) => 5, (0, -1) => 6, _ => 7
        };
    }

    private static long Cross(PathSite a, PathSite b, PathSite c) =>
        ((long)b.X - a.X) * ((long)c.Y - a.Y) - ((long)b.Y - a.Y) * ((long)c.X - a.X);
    private static bool On(PathSite a, PathSite b, PathSite p) => Cross(a, b, p) == 0 &&
        Math.Min(a.X, b.X) <= p.X && p.X <= Math.Max(a.X, b.X) && Math.Min(a.Y, b.Y) <= p.Y && p.Y <= Math.Max(a.Y, b.Y);
    private static bool PositiveOverlap(PathSite a, PathSite b, PathSite c, PathSite d) => a.X != b.X
        ? Math.Min(Math.Max(a.X, b.X), Math.Max(c.X, d.X)) > Math.Max(Math.Min(a.X, b.X), Math.Min(c.X, d.X))
        : Math.Min(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y)) > Math.Max(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y));
}

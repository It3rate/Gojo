using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Experiments.QuickDraw;

/// <summary>Non-draining many-to-many source correspondence for authored
/// simplification passes. Acquired sample identity/order, actual target plans,
/// explicit unmatched source and active residue curves survive transport.
/// Proposed joins do not identify acquired events; no exact fit is asserted.</summary>
public static class QuickDrawGestaltEvidence
{
    // graph = group(acquisition, program, calibration, source partition, passes)
    // interval = actual stroke row -> group(actual consecutive sample rows)
    // pass = ReadWitness(source partition, group(calibration, actual rung plan),
    //                    group(link reads, inferred actual edges),
    //                    group(unmatched intervals, active residue guide edges),
    //                    group(acquisition, program))
    // link = ReadWitness(group(actual intervals), actual pass basis,
    //                    group(actual target edge rows), same selected source,
    //                    same pass provenance)
    // Overlapping source reads are observations. They do not allocate material.
    public sealed record Interval(int Stroke, int Start, int End, Relation Selection);
    public sealed record Link(Interval[] Source, ConstructionLadder.Edge[] Target, Relation Witness);
    public sealed record Pass(int Rung, Link[] Links, Interval[] Unmatched,
        ConstructionLadder.Edge[] Inferred, ConstructionLadder.Edge[] Residue, Relation Witness);
    public sealed record Read(NativeAcquisition.Read Acquisition, ConstructionLadder.Execution Construction,
        Interval[] Intervals, Pass[] Passes, Relation Source, Relation Program, Relation Calibration, Relation Surface);

    public static Relation Import(JsonElement packet)
    {
        var original = packet.TryGetProperty("sourceOriginalJson", out var raw) && raw.ValueKind == JsonValueKind.String
            ? raw.GetString()! : packet.GetProperty("source").GetRawText();
        var acquired = new NativeAcquisition(QuickDrawRecord.Parse(original));
        var recipe = packet.GetProperty("program"); var mapping = recipe.GetProperty("sourceMapping");
        var observation = packet.GetProperty("observation");
        if (mapping.GetProperty("sourceId").GetString() != acquired.Source.KeyId || observation.GetProperty("sourceId").GetString() != acquired.Source.KeyId)
            throw new InvalidDataException("Simplification correspondence changed source identity.");
        var program = ConstructionLadder.Import(recipe);
        if (!ConstructionLadder.TryExecute(program, out var execution) || !Inventory(acquired.Surface, out var strokes, out var samples))
            throw new InvalidDataException("Unreadable source or construction program.");
        var sites = recipe.GetProperty("sites").EnumerateArray().Select((row, i) => (row.GetProperty("id").GetString()!, i)).ToDictionary(row => row.Item1, row => row.i);
        var edges = recipe.GetProperty("edges").EnumerateArray().Select((row, i) => (row.GetProperty("id").GetString()!, i)).ToDictionary(row => row.Item1, row => execution.Edges[row.i]);
        var envelope = mapping.GetProperty("envelopeSiteIds").EnumerateArray().Select(id => (IElement)execution.Sites[sites[id.GetString()!]].Surface).ToArray();
        var calibration = G(acquired.Basis, execution.Frame.Surface, Bounds(samples, acquired.X, acquired.Y), G(envelope));
        var intervals = new Dictionary<string, Relation>(StringComparer.Ordinal);
        foreach (var span in observation.GetProperty("spans").EnumerateArray())
        {
            var stroke = span.GetProperty("strokeIndex").GetInt32(); var start = span.GetProperty("startIndex").GetInt32(); var end = span.GetProperty("endIndex").GetInt32();
            if (stroke < 0 || stroke >= samples.Length || start < 0 || end < start || end >= samples[stroke].Length)
                throw new InvalidDataException("Source interval is outside its acquisition.");
            intervals.Add(span.GetProperty("id").GetString()!, new Relation(strokes[stroke], G(samples[stroke][start..(end + 1)])));
        }
        var partition = G(intervals.Values.Cast<IElement>().ToArray()); var plans = Entries(Entries(program)[5]);
        var history = G(acquired.Surface, program); var passes = new List<IElement>();
        foreach (var pass in mapping.GetProperty("passes").EnumerateArray())
        {
            var index = pass.GetProperty("rung").GetInt32();
            if (index != passes.Count || index >= plans.Length) throw new InvalidDataException("Pass correspondence must follow authored rung order.");
            var basis = G(calibration, plans[index]);
            var links = pass.GetProperty("links").EnumerateArray().Select(link => {
                var selected = G(link.GetProperty("spanIds").EnumerateArray().Select(id => (IElement)intervals[id.GetString()!]).ToArray());
                var target = G(link.GetProperty("edgeIds").EnumerateArray().Select(id => (IElement)edges[id.GetString()!].Surface).ToArray());
                return (IElement)ReadWitness.Create(new Carrier(), selected, basis, target, selected, history);
            }).ToArray();
            var unmatched = G(pass.GetProperty("unmatchedSpanIds").EnumerateArray().Select(id => (IElement)intervals[id.GetString()!]).ToArray());
            Relation Targets(string key) => G(pass.GetProperty(key).EnumerateArray().Select(id => (IElement)edges[id.GetString()!].Surface).ToArray());
            passes.Add(ReadWitness.Create(new Carrier(), partition, basis, G(G(links), Targets("inferredEdgeIds")),
                G(unmatched, Targets("residueEdgeIds")), history));
        }
        var result = G(acquired.Surface, program, calibration, partition, G(passes.ToArray()));
        if (!TryRead(result, out _)) throw new InvalidDataException("Source coverage, target aliases, causal construction or pass accounting is inconsistent.");
        return result;
    }

    public static bool TryRead(Relation surface, out Read read)
    {
        read = null!;
        if (!Fields(surface, 5, out var root) || root is not [Relation source, Relation program, Relation calibration, Relation partition, Relation passScope] ||
            !NativeAcquisition.TryRead(source, out var acquisition) || !ConstructionLadder.TryExecute(program, out var construction) ||
            !Inventory(source, out var strokes, out var samples) || !Fields(calibration, 4, out var basis) ||
            source.Left is not Relation header || !ReferenceEquals(basis[0], header.Right) || !ReferenceEquals(basis[1], construction.Frame.Surface) ||
            !Elements(basis[2], out var extrema) || !Same(extrema, Entries(Bounds(samples, acquisition.X, acquisition.Y))) ||
            !Fields(basis[3], 2, out var envelope) ||
            envelope.Any(site => !construction.Sites.Any(s => ReferenceEquals(s.Surface, site) && s.At == 0)) ||
            envelope.Any(site => !construction.Rungs[0].Visible.Concat(construction.Rungs[0].Guides).Any(edge => edge.Controls.Any(s => ReferenceEquals(s.Surface, site)))) ||
            !Elements(partition, out var intervalRows) || !Elements(passScope, out var passRows) || passRows.Length != construction.Rungs.Length) return false;
        var low = construction.Sites.Single(s => ReferenceEquals(s.Surface, envelope[0])).Position;
        var high = construction.Sites.Single(s => ReferenceEquals(s.Surface, envelope[1])).Position;
        if (ReferenceEquals(envelope[0], envelope[1]) || high.X <= low.X || high.Y <= low.Y) return false;
        var acquiredEvents = new HashSet<IElement>(samples.SelectMany(stroke => stroke).Cast<Relation>().Select(sample => sample.Left), ReferenceEqualityComparer.Instance);
        if (construction.Sites.Any(site => acquiredEvents.Contains(site.Event)) || !Causal(construction)) return false;
        var lookup = new Dictionary<IElement, (int Stroke, int Index)>(ReferenceEqualityComparer.Instance);
        for (var s = 0; s < samples.Length; s++) for (var i = 0; i < samples[s].Length; i++) lookup.Add(samples[s][i], (s, i));
        var intervals = new List<Interval>(); var intervalMap = new Dictionary<IElement, Interval>(ReferenceEqualityComparer.Instance);
        var nextStroke = 0; var nextSample = 0;
        void SkipEmpty() { while (nextStroke < samples.Length && samples[nextStroke].Length == 0) nextStroke++; }
        SkipEmpty();
        foreach (var row in intervalRows)
        {
            if (row is not Relation selection || !Elements(selection.Right, out var covered) || covered.Length == 0 ||
                !lookup.TryGetValue(covered[0], out var start) || start.Stroke != nextStroke || start.Index != nextSample ||
                !ReferenceEquals(selection.Left, strokes[start.Stroke]) ||
                covered.Where((sample, i) => !lookup.TryGetValue(sample, out var entry) || entry.Stroke != start.Stroke || entry.Index != start.Index + i).Any() ||
                covered.Length == 1 && samples[start.Stroke].Length != 1 || intervalMap.ContainsKey(selection)) return false;
            var end = start.Index + covered.Length - 1; var interval = new Interval(start.Stroke, start.Index, end, selection);
            intervals.Add(interval); intervalMap.Add(selection, interval);
            if (end == samples[start.Stroke].Length - 1) { nextStroke++; nextSample = 0; SkipEmpty(); } else nextSample = end;
        }
        if (nextStroke != samples.Length) return false;
        var edgeMap = new Dictionary<IElement, ConstructionLadder.Edge>(ReferenceEqualityComparer.Instance);
        foreach (var edge in construction.Edges) edgeMap.Add(edge.Surface, edge);
        var plans = Entries(Entries(program)[5]); var passes = new List<Pass>();
        bool PickEdges(IElement scope, out ConstructionLadder.Edge[] selected)
        {
            selected = [];
            if (!Elements(scope, out var rows) || rows.Any(row => !edgeMap.ContainsKey(row)) || rows.Distinct(ReferenceEqualityComparer.Instance).Count() != rows.Length) return false;
            selected = rows.Select(row => edgeMap[row]).ToArray(); return true;
        }
        for (var index = 0; index < passRows.Length; index++)
        {
            var rung = construction.Rungs[index];
            if (!Witness(passRows[index], out var selected, out var passBasis, out var result, out var residue, out var history) ||
                !ReferenceEquals(selected, partition) || !Fields(passBasis, 2, out var pb) || !ReferenceEquals(pb[0], calibration) || !ReferenceEquals(pb[1], plans[index]) ||
                !Fields(history, 2, out var provenance) || !ReferenceEquals(provenance[0], source) || !ReferenceEquals(provenance[1], program) ||
                !Fields(result, 2, out var output) || !Elements(output[0], out var linkRows) || !PickEdges(output[1], out var inferred) ||
                !Fields(residue, 2, out var remainder) || !Elements(remainder[0], out var unmatchedRows) ||
                unmatchedRows.Any(row => !intervalMap.ContainsKey(row)) || unmatchedRows.Distinct(ReferenceEqualityComparer.Instance).Count() != unmatchedRows.Length ||
                !PickEdges(remainder[1], out var activeResidue) || activeResidue.Any(edge => !rung.Guides.Contains(edge, ReferenceEqualityComparer.Instance))) return false;
            var activeTargets = new HashSet<ConstructionLadder.Edge>(rung.Visible.Concat(activeResidue), ReferenceEqualityComparer.Instance);
            if (inferred.Any(edge => !activeTargets.Contains(edge))) return false;
            var representedSource = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
            var representedTarget = new HashSet<ConstructionLadder.Edge>(ReferenceEqualityComparer.Instance); var links = new List<Link>();
            foreach (var link in linkRows)
            {
                if (!Witness(link, out var chosen, out var lb, out var target, out var loss, out var lp) ||
                    !ReferenceEquals(lb, passBasis) || !ReferenceEquals(lp, history) || !ReferenceEquals(loss, chosen) ||
                    !Elements(chosen, out var sourceRows) || sourceRows.Length == 0 || sourceRows.Any(row => !intervalMap.ContainsKey(row)) ||
                    sourceRows.Distinct(ReferenceEqualityComparer.Instance).Count() != sourceRows.Length ||
                    !PickEdges(target, out var targets) || targets.Length == 0 || targets.Any(edge => !activeTargets.Contains(edge))) return false;
                representedSource.UnionWith(sourceRows); representedTarget.UnionWith(targets);
                links.Add(new(sourceRows.Select(row => intervalMap[row]).ToArray(), targets, (Relation)link));
            }
            if (unmatchedRows.Any(representedSource.Contains) || representedSource.Count + unmatchedRows.Length != intervals.Count ||
                inferred.Any(representedTarget.Contains)) return false;
            representedTarget.UnionWith(inferred); if (!representedTarget.SetEquals(activeTargets)) return false;
            passes.Add(new(index, links.ToArray(), unmatchedRows.Select(row => intervalMap[row]).ToArray(), inferred, activeResidue, (Relation)passRows[index]));
        }
        read = new(acquisition, construction, intervals.ToArray(), passes.ToArray(), source, program, calibration, surface); return true;
    }

    private static bool Causal(ConstructionLadder.Execution execution)
    {
        var ancestry = new List<HashSet<int>>();
        foreach (var site in execution.Sites)
        {
            var ancestors = new HashSet<int>(site.Dependencies);
            foreach (var dependency in site.Dependencies) ancestors.UnionWith(ancestry[dependency]);
            ancestry.Add(ancestors);
            if (site.At == 0) continue;
            if (site.At >= execution.Rungs.Length || site.Surface.Right is not Relation xy || xy.Left is CarrierLocation || xy.Right is CarrierLocation) return false;
            var earlier = execution.Rungs.Take(site.At).SelectMany(r => r.Visible.Concat(r.Guides)).SelectMany(edge => edge.Controls).Select(control => control.Index);
            if (!ancestors.Overlaps(earlier)) return false;
        }
        return true;
    }
    private static bool Inventory(Relation source, out IElement[] strokes, out IElement[][] samples)
    {
        strokes = []; samples = [];
        if (source.Right is not Relation scope || !NativeAcquisition.Closed(scope, out strokes)) return false;
        samples = new IElement[strokes.Length][];
        for (var i = 0; i < strokes.Length; i++)
            if (strokes[i] is not Relation { Right: Relation { Right: Relation { Left: Relation sampleScope } } } || !NativeAcquisition.Closed(sampleScope, out samples[i])) return false;
        return true;
    }
    private static Relation Bounds(IElement[][] samples, Carrier x, Carrier y)
    {
        var coordinates = samples.SelectMany(stroke => stroke).Cast<Relation>().Select(sample => (Relation)((Relation)((Relation)sample.Right).Right).Left).ToArray();
        if (coordinates.Length == 0) return G();
        IElement Extreme(Carrier axis, bool horizontal, bool maximum)
        {
            var chosen = horizontal ? coordinates[0].Left : coordinates[0].Right;
            foreach (var coordinate in coordinates.Skip(1))
            {
                var candidate = horizontal ? coordinate.Left : coordinate.Right;
                NativeAcquisition.TryReadScalar(chosen, axis, out var a, out var ad); NativeAcquisition.TryReadScalar(candidate, axis, out var b, out var bd);
                var comparison = (b * ad).CompareTo(a * bd); if (maximum ? comparison > 0 : comparison < 0) chosen = candidate;
            }
            return chosen;
        }
        return G(Extreme(x, true, false), Extreme(y, false, false), Extreme(x, true, true), Extreme(y, false, true));
    }
    private static bool Witness(IElement element, out IElement selected, out IElement basis, out IElement result, out IElement residue, out IElement provenance)
    { selected = basis = result = residue = provenance = null!; return element is Relation row && Fields(row, 5, out _) && ReadWitness.TryRead(row, out selected, out basis, out result, out residue, out provenance); }
    private static bool Same(IElement[] first, IElement[] second) => first.Length == second.Length && first.Where((element, i) => !ReferenceEquals(element, second[i])).Any() == false;
    private static Relation G(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private static bool Elements(IElement element, out IElement[] fields) { if (element is Relation row && row.TryReadOrderedGroup(out fields)) return true; fields = []; return false; }
    private static bool Fields(IElement element, int count, out IElement[] fields) => Elements(element, out fields) && fields.Length == count;
    private static IElement[] Entries(IElement element) => Elements(element, out var fields) ? fields : throw new InvalidDataException("Expected native ordered scope.");
}

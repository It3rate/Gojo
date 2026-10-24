using System.Text.Json;
using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Experiments.QuickDraw;

/// <summary>Non-draining correspondence from exact acquisition intervals to a
/// finite construction proposal. Full source intervals remain unaccounted read
/// residue. This is not an intent classifier, geometric equality certificate or
/// material transfer; the target program remains independently executable.</summary>
public static class QuickDrawInterpretationEvidence
{
    // evidence = group(acquisition, construction program, calibration, witnesses)
    // calibration = group(actual source basis, actual target basis,
    //                     group(actual minX,minY,maxX,maxY scalar surfaces),
    //                     group(actual envelope-a site row, envelope-b site row))
    // selection = actual stroke row -> group(actual ordered source sample rows)
    // witness = ReadWitness(selection, calibration, actual target edge row,
    //                       selection, group(acquisition, program, endpoint map))
    // endpoint map = group(actual first sample row -> actual first control row,
    //                      actual last sample row -> actual last control row)
    public sealed record SpanInput(int Stroke, int Start, int End, int Edge);
    public sealed record Span(int Stroke, int Start, int End, ConstructionLadder.Edge Edge,
        Relation Selection, Relation Witness);
    public sealed record Read(NativeAcquisition.Read Acquisition, ConstructionLadder.Execution Construction,
        Relation Source, Relation Program, Relation Calibration, IReadOnlyList<Span> Spans, Relation Surface);

    public static Relation Import(JsonElement packet)
    {
        var original = packet.TryGetProperty("sourceOriginalJson", out var raw) && raw.ValueKind == JsonValueKind.String
            ? raw.GetString()! : packet.GetProperty("source").GetRawText();
        var acquisition = new NativeAcquisition(QuickDrawRecord.Parse(original));
        var proposal = packet.GetProperty("program");
        var mapping = proposal.GetProperty("sourceMapping");
        if (mapping.GetProperty("sourceId").GetString() != acquisition.Source.KeyId ||
            packet.GetProperty("observation").GetProperty("sourceId").GetString() != acquisition.Source.KeyId)
            throw new InvalidDataException("The measured proposal must retain its acquisition source identity.");
        var edgeIndexes = proposal.GetProperty("edges").EnumerateArray().Select((edge, i) => (Id: edge.GetProperty("id").GetString()!, Index: i))
            .ToDictionary(item => item.Id, item => item.Index, StringComparer.Ordinal);
        var siteIndexes = proposal.GetProperty("sites").EnumerateArray().Select((site, i) => (Id: site.GetProperty("id").GetString()!, Index: i))
            .ToDictionary(item => item.Id, item => item.Index, StringComparer.Ordinal);
        var envelope = mapping.GetProperty("envelopeSiteIds").EnumerateArray().Select(id => siteIndexes[id.GetString()!]).ToArray();
        var spans = packet.GetProperty("observation").GetProperty("spans").EnumerateArray().Select(span => new SpanInput(
            span.GetProperty("strokeIndex").GetInt32(), span.GetProperty("startIndex").GetInt32(), span.GetProperty("endIndex").GetInt32(),
            edgeIndexes[mapping.GetProperty("spanMap").GetProperty(span.GetProperty("id").GetString()!).GetString()!])).ToArray();
        return Create(acquisition, ConstructionLadder.Import(proposal), envelope, spans);
    }

    public static Relation Create(NativeAcquisition acquisition, Relation program, int[] envelopeSites, IReadOnlyList<SpanInput> spans)
    {
        if (!ConstructionLadder.TryExecute(program, out var construction) || envelopeSites.Length != 2 ||
            envelopeSites.Any(i => i < 0 || i >= construction.Sites.Length))
            throw new InvalidDataException("A correspondence needs a readable target and two actual envelope sites.");
        Inventory(acquisition.Surface, out var strokeRows, out var sampleRows);
        var calibration = G(acquisition.Basis, construction.Frame.Surface, Bounds(sampleRows, acquisition.X, acquisition.Y),
            G(envelopeSites.Select(i => (IElement)construction.Sites[i].Surface).ToArray()));
        var witnesses = new List<IElement>();
        foreach (var span in spans)
        {
            if (span.Stroke < 0 || span.Stroke >= sampleRows.Length || span.Start < 0 || span.End < span.Start ||
                span.End >= sampleRows[span.Stroke].Length || span.Edge < 0 || span.Edge >= construction.Edges.Length)
                throw new InvalidDataException("A measured interval lies outside its source or target.");
            var samples = sampleRows[span.Stroke]; var edge = construction.Edges[span.Edge];
            var selection = new Relation(strokeRows[span.Stroke], G(samples[span.Start..(span.End + 1)]));
            var endpoints = G(new Relation(samples[span.Start], edge.Controls[0].Surface), new Relation(samples[span.End], edge.Controls[^1].Surface));
            witnesses.Add(ReadWitness.Create(new Carrier(), selection, calibration, edge.Surface, selection,
                G(acquisition.Surface, program, endpoints)));
        }
        var result = G(acquisition.Surface, program, calibration, G(witnesses.ToArray()));
        if (!TryRead(result, out _)) throw new InvalidDataException("Source coverage, target aliases or measured correspondence is inconsistent.");
        return result;
    }

    public static bool TryRead(Relation surface, out Read read)
    {
        read = null!;
        if (!Fields(surface, 4, out var fields) || fields is not [Relation source, Relation program, Relation calibration, Relation witnesses] ||
            !NativeAcquisition.TryRead(source, out var acquisition) || !ConstructionLadder.TryExecute(program, out var construction) ||
            !Inventory(source, out var strokes, out var samples) || !Fields(calibration, 4, out var basis) ||
            source.Left is not Relation sourceHeader || !ReferenceEquals(basis[0], sourceHeader.Right) ||
            !ReferenceEquals(basis[1], construction.Frame.Surface) || !Elements(basis[2], out var bounds) ||
            !Elements(Bounds(samples, acquisition.X, acquisition.Y), out var expectedBounds) ||
            !Same(bounds, expectedBounds) || !Fields(basis[3], 2, out var envelope) ||
            envelope.Any(site => !construction.Sites.Any(s => ReferenceEquals(s.Surface, site) && s.At == 0)) ||
            envelope.Any(site => !construction.Rungs[0].Visible.Concat(construction.Rungs[0].Guides)
                .Any(edge => edge.Controls.Any(control => ReferenceEquals(control.Surface, site)))) ||
            !Elements(witnesses, out var rows)) return false;
        var low = construction.Sites.Single(site => ReferenceEquals(site.Surface, envelope[0])).Position;
        var high = construction.Sites.Single(site => ReferenceEquals(site.Surface, envelope[1])).Position;
        if (ReferenceEquals(envelope[0], envelope[1]) || high.X <= low.X || high.Y <= low.Y) return false;
        var edgeMap = new Dictionary<Relation, ConstructionLadder.Edge>(ReferenceEqualityComparer.Instance);
        foreach (var edge in construction.Edges) edgeMap.Add(edge.Surface, edge);
        var sampleLookup = new Dictionary<IElement, (int Stroke, int Index)>(ReferenceEqualityComparer.Instance);
        for (var s = 0; s < samples.Length; s++)
            for (var i = 0; i < samples[s].Length; i++) sampleLookup.Add(samples[s][i], (s, i));
        var sourceTargets = new Dictionary<IElement, Relation>(ReferenceEqualityComparer.Instance);
        var targetSources = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        bool Endpoint(IElement mapping, IElement sample, Relation target)
        {
            if (mapping is not Relation pair || !ReferenceEquals(pair.Left, sample) || !ReferenceEquals(pair.Right, target) ||
                sourceTargets.TryGetValue(sample, out var priorTarget) && !ReferenceEquals(priorTarget, target) ||
                targetSources.TryGetValue(target, out var priorSample) && !ReferenceEquals(priorSample, sample)) return false;
            sourceTargets[sample] = target; targetSources[target] = sample; return true;
        }
        var spans = new List<Span>(); var used = new HashSet<ConstructionLadder.Edge>(ReferenceEqualityComparer.Instance);
        var nextStroke = 0; var nextSample = 0;
        void SkipEmpty() { while (nextStroke < samples.Length && samples[nextStroke].Length == 0) nextStroke++; }
        SkipEmpty();
        foreach (var item in rows)
        {
            if (item is not Relation witness || !ReadWitness.TryRead(witness, out var selected, out var readBasis, out var target, out var residue, out var provenance) ||
                selected is not Relation selection || !ReferenceEquals(readBasis, calibration) || !ReferenceEquals(residue, selection) ||
                !Elements(selection.Right, out var covered) || covered.Length == 0 ||
                !sampleLookup.TryGetValue(covered[0], out var start) || start.Stroke != nextStroke || start.Index != nextSample ||
                !ReferenceEquals(selection.Left, strokes[start.Stroke]) ||
                covered.Where((sample, i) => !sampleLookup.TryGetValue(sample, out var entry) || entry.Stroke != start.Stroke || entry.Index != start.Index + i).Any() ||
                target is not Relation edgeRow || !edgeMap.TryGetValue(edgeRow, out var edge) || !used.Add(edge) ||
                edge.At != start.Stroke + 2 || !Fields(provenance, 3, out var history) || !ReferenceEquals(history[0], source) || !ReferenceEquals(history[1], program) ||
                !Fields(history[2], 2, out var endpoints) ||
                !Endpoint(endpoints[0], covered[0], edge.Controls[0].Surface) || !Endpoint(endpoints[1], covered[^1], edge.Controls[^1].Surface)) return false;
            var end = start.Index + covered.Length - 1;
            if (covered.Length == 1 && samples[start.Stroke].Length != 1) return false;
            spans.Add(new(start.Stroke, start.Index, end, edge, selection, witness));
            if (end == samples[start.Stroke].Length - 1) { nextStroke++; nextSample = 0; SkipEmpty(); }
            else nextSample = end;
        }
        if (nextStroke != samples.Length || !used.SetEquals(construction.Rungs[^1].Visible)) return false;
        read = new(acquisition, construction, source, program, calibration, spans.AsReadOnly(), surface); return true;
    }

    private static bool Inventory(Relation source, out IElement[] strokes, out IElement[][] samples)
    {
        strokes = []; samples = [];
        if (source.Right is not Relation strokeScope || !NativeAcquisition.Closed(strokeScope, out strokes)) return false;
        samples = new IElement[strokes.Length][];
        for (var i = 0; i < strokes.Length; i++)
            if (strokes[i] is not Relation { Right: Relation { Right: Relation { Left: Relation sampleScope } } } ||
                !NativeAcquisition.Closed(sampleScope, out samples[i])) return false;
        return true;
    }

    private static Relation Bounds(IElement[][] samples, Carrier x, Carrier y)
    {
        var coordinates = samples.SelectMany(stroke => stroke).Cast<Relation>()
            .Select(sample => (Relation)((Relation)((Relation)sample.Right).Right).Left).ToArray();
        if (coordinates.Length == 0) return G();
        IElement Extreme(Carrier axis, bool horizontal, bool maximum)
        {
            var chosen = horizontal ? coordinates[0].Left : coordinates[0].Right;
            foreach (var coordinate in coordinates.Skip(1))
            {
                var candidate = horizontal ? coordinate.Left : coordinate.Right;
                NativeAcquisition.TryReadScalar(chosen, axis, out var an, out var ad);
                NativeAcquisition.TryReadScalar(candidate, axis, out var bn, out var bd);
                var comparison = (bn * ad).CompareTo(an * bd);
                if (maximum ? comparison > 0 : comparison < 0) chosen = candidate;
            }
            return chosen;
        }
        return G(Extreme(x, true, false), Extreme(y, false, false), Extreme(x, true, true), Extreme(y, false, true));
    }
    private static bool Same(IElement[] a, IElement[] b) => a.Length == b.Length && a.Where((element, i) => !ReferenceEquals(element, b[i])).Any() == false;
    private static Relation G(params IElement[] elements) => Relation.OrderedGroup(new Carrier(), elements);
    private static bool Elements(IElement element, out IElement[] fields)
    { if (element is Relation relation && relation.TryReadOrderedGroup(out fields)) return true; fields = []; return false; }
    private static bool Fields(IElement element, int count, out IElement[] fields) => Elements(element, out fields) && fields.Length == count;
}

using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A non-draining layered read of an existing path program, with
/// optional native additions attached through its actual public landmarks.
/// Colour, visual weight and filled-area interpretation belong to a renderer.
/// Selection does not consume paths, and equal positions do not join sites.</summary>
public sealed class OrnamentEnrichment
{
    // definition = identity -> (selected actual source paths ->
    //              (Closed(native addition programs) -> Closed(attachments)))
    // attachment = actual source port -> (offset word -> actual added path)
    // admitted = definition -> (selected paths -> (carried complement ->
    //             (added paths -> Closed(attachment -> (anchor read -> equality)))))
    // Surface = (source program -> definitions) ->
    //           (admitted layers -> (result program -> (blocked -> unresolved)))
    public UnitArrangementProgram Source { get; }
    public Relation SourceSurface { get; }
    public Relation Definitions { get; }
    public Relation Layers { get; }
    public UnitArrangementProgram Result { get; }
    public Relation ResultSurface { get; }
    public Relation Blocked { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Blocked).Length == 0 && N.E(Unresolved).Length == 0;

    public OrnamentEnrichment(UnitArrangementProgram source, Relation definitions) : this(source, source.Surface, definitions) { }

    /// <summary>Keeps the actual imported declaration identity rather than
    /// manufacturing another wrapper surface with the same child references.</summary>
    public OrnamentEnrichment(QualitativeMotionFrame frame, Relation actualSourceSurface, Relation definitions)
        : this(ReadProgram(frame, actualSourceSurface), actualSourceSurface, definitions) { }

    private OrnamentEnrichment(UnitArrangementProgram source, Relation sourceSurface, Relation definitions)
    {
        Source = source; SourceSurface = sourceSurface; Definitions = definitions;
        var accepted = new List<IElement>(); var blocked = new List<IElement>(); var pending = new List<IElement>();
        var paths = N.E(source.Paths).ToList(); var ports = N.E(source.Ports).ToList(); var closed = N.E(source.ClosedPaths).ToList();
        if (!source.Complete()) blocked.Add(source.Surface);
        if (!N.Read(definitions, out var rows)) pending.Add(definitions);
        if (rows.OfType<Relation>().Select(row => row.Left).Distinct(N.Participants.Instance).Count() != rows.Length)
            blocked.Add(definitions);
        else foreach (var row in rows)
        {
            if (!source.Complete()) { pending.Add(new Relation(row, source.Surface)); continue; }
            if (row is not Relation { Right: Relation { Left: Relation selected, Right: Relation { Left: Relation additions, Right: Relation attachments } } } definition)
            { blocked.Add(row); continue; }
            if (!N.Read(selected, out var selectedPaths) || !N.Read(additions, out var additionRows) || !N.Read(attachments, out var anchorRows))
            { pending.Add(definition); continue; }
            if (!N.Distinct(selectedPaths) || selectedPaths.Any(path => !Contains(source.Paths, path)))
            { blocked.Add(new Relation(definition, source.Paths)); continue; }
            var programs = new List<UnitArrangementProgram>(); var invalid = false;
            foreach (var item in additionRows)
            {
                if (item is not Relation actual || !TryProgram(source.Frame, actual, out var program)) { invalid = true; break; }
                programs.Add(program);
            }
            var added = programs.SelectMany(program => N.E(program.Paths)).ToArray();
            if (invalid || !N.Distinct(added) || added.Any(path => paths.Any(actual => ReferenceEquals(actual, path))))
            { blocked.Add(new Relation(definition, additions)); continue; }
            var witnesses = new List<IElement>();
            foreach (var item in anchorRows)
            {
                if (item is not Relation { Left: Relation port, Right: Relation { Left: Relation offset, Right: Relation path } } attachment ||
                    !added.Any(actual => ReferenceEquals(actual, path)) || !Word(source.Frame, offset) ||
                    !TryAnchorWord(source, port, out var anchor, out var anchorRead)) { invalid = true; break; }
                var compare = source.Frame.Reduce(N.S(N.E(anchor).Concat(N.E(offset)).ToArray()),
                    UnitArrangementRead.Inverse(source.Frame, (Relation)path.Left));
                if (!UnitArrangementRead.IsZero(source.Frame, compare)) { invalid = true; break; }
                witnesses.Add(new Relation(attachment, new Relation(anchorRead, compare)));
            }
            if (invalid || added.Any(path => !anchorRows.OfType<Relation>().Any(row => row.Right is Relation pick && ReferenceEquals(pick.Right, path))))
            { blocked.Add(new Relation(definition, attachments)); continue; }
            var candidatePaths = N.S(paths.Concat(added).ToArray());
            var candidatePorts = N.S(ports.Concat(programs.SelectMany(program => N.E(program.Ports))).ToArray());
            var candidateClosed = N.S(closed.Concat(programs.SelectMany(program => N.E(program.ClosedPaths))).ToArray());
            var candidate = new UnitArrangementProgram(source.Frame, "layered native paths", candidatePaths, candidatePorts, candidateClosed);
            if (!candidate.Complete()) { blocked.Add(new Relation(definition, candidate.Surface)); continue; }
            paths.AddRange(added); ports.AddRange(programs.SelectMany(program => N.E(program.Ports))); closed.AddRange(programs.SelectMany(program => N.E(program.ClosedPaths)));
            var complement = N.S(N.E(source.Paths).Where(path => !selectedPaths.Any(actual => ReferenceEquals(actual, path))).ToArray());
            accepted.Add(new Relation(definition, new Relation(selected, new Relation(complement,
                new Relation(N.S(added), N.S(witnesses.ToArray()))))));
        }
        Layers = N.S(accepted.ToArray()); Blocked = N.S(blocked.ToArray()); Unresolved = N.S(pending.ToArray());
        Result = new(source.Frame, "enriched native ornament", N.S(paths.ToArray()), N.S(ports.ToArray()), N.S(closed.ToArray()),
            new Relation(SourceSurface, new Relation(definitions, new Relation(Layers, new Relation(Blocked, Unresolved)))));
        ResultSurface = Result.Surface;
        Surface = new(new Relation(SourceSurface, definitions), new Relation(Layers, new Relation(ResultSurface, new Relation(Blocked, Unresolved))));
    }

    public static Relation Layer(IElement identity, Relation selectedPaths, Relation additionPrograms, Relation attachments) =>
        new(identity, new Relation(selectedPaths, new Relation(additionPrograms, attachments)));
    public static Relation Selection(IElement identity, params Relation[] selectedPaths) => Layer(identity, N.S(selectedPaths), N.S(), N.S());
    public static Relation Attach(Relation actualSourcePort, Relation offsetWord, Relation actualAddedPath) =>
        new(actualSourcePort, new Relation(offsetWord, actualAddedPath));

    /// <summary>Reads the published path cursor at a chord's start, or at the
    /// terminal cursor selected by the actual path. A copied port is refused.</summary>
    public static bool TryAnchorWord(UnitArrangementProgram source, Relation actualPort, out Relation word, out Relation read)
    {
        word = read = null!;
        if (!source.Complete() || !Contains(source.Ports, actualPort) ||
            actualPort.Right is not Relation { Left: Relation path } pick) return false;
        var chords = N.E((Relation)path.Right); var index = ReferenceEquals(pick.Right, path)
            ? chords.Length : Array.FindIndex(chords, actual => ReferenceEquals(actual, pick.Right));
        if (index < 0) return false;
        var prefix = N.S(chords.Take(index).Cast<Relation>().SelectMany(N.E).ToArray());
        read = source.Frame.Reduce((Relation)path.Left, prefix);
        return source.Frame.TryReadResult(read, out word, out _, out var unresolved) && N.Read(word, out _) && N.E(unresolved).Length == 0;
    }

    /// <summary>Selects one admitted layer while carrying every sibling. This
    /// changes emphasis only; it neither removes nor redraws the source.</summary>
    public OrnamentEnrichmentFocus Focus(IElement actualLayerIdentity) => new(this, actualLayerIdentity);

    /// <summary>A narrow complete-contract reader for a transported enriched
    /// motif. Rechecks actual source membership, additions, public anchors and
    /// retained native reductions. It does not search arbitrary ancestry for
    /// rows that happen to resemble a layer. Blocked/open contracts remain
    /// retained evidence but do not acquire a complete enrichment read.</summary>
    public static bool TryReadEnrichment(QualitativeMotionFrame frame, Relation actualResultSurface, out OrnamentEnrichment enrichment)
    {
        enrichment = null!;
        if (!TryProgram(frame, actualResultSurface, out var result) ||
            result.Evidence is not Relation { Left: Relation sourceSurface,
                Right: Relation { Left: Relation definitions, Right: Relation { Left: Relation layers,
                    Right: Relation { Left: Relation blocked, Right: Relation unresolved } } } } ||
            !TryProgram(frame, sourceSurface, out var source) || !N.Read(blocked, out var blockedRows) || blockedRows.Length != 0 ||
            !N.Read(unresolved, out var openRows) || openRows.Length != 0 || !N.Read(layers, out var actualLayers)) return false;
        var expected = new OrnamentEnrichment(source, sourceSurface, definitions);
        if (!expected.IsComplete || !SameEntries(result.Paths, expected.Result.Paths) || !SameEntries(result.Ports, expected.Result.Ports) ||
            !SameEntries(result.ClosedPaths, expected.Result.ClosedPaths) || actualLayers.Length != N.E(expected.Layers).Length) return false;
        for (var index = 0; index < actualLayers.Length; index++)
        {
            var wanted = (Relation)N.E(expected.Layers)[index];
            if (actualLayers[index] is not Relation actual || !ReadLayer(actual, out var definition, out var selected, out var complement, out var added, out var anchors) ||
                !ReadLayer(wanted, out var expectedDefinition, out var expectedSelected, out var expectedComplement, out var expectedAdded, out var expectedAnchors) ||
                !ReferenceEquals(definition, expectedDefinition) || !ReferenceEquals(selected, expectedSelected) ||
                !SameEntries(complement, expectedComplement) || !SameEntries(added, expectedAdded) ||
                !N.Read(anchors, out var anchorRows) || anchorRows.Length != N.E(expectedAnchors).Length) return false;
            for (var anchorIndex = 0; anchorIndex < anchorRows.Length; anchorIndex++)
            {
                var wantedAnchor = (Relation)N.E(expectedAnchors)[anchorIndex];
                if (anchorRows[anchorIndex] is not Relation { Right: Relation { Left: Relation anchorRead, Right: Relation equality } } receipt ||
                    !ReferenceEquals(receipt.Left, wantedAnchor.Left) || wantedAnchor.Right is not Relation { Left: Relation expectedRead, Right: Relation expectedEquality } ||
                    !SameReduction(frame, anchorRead, expectedRead) || !SameReduction(frame, equality, expectedEquality) ||
                    !UnitArrangementRead.IsZero(frame, equality)) return false;
            }
        }
        enrichment = new(source, sourceSurface, definitions, layers, result, actualResultSurface, blocked, unresolved); return true;
    }

    private OrnamentEnrichment(UnitArrangementProgram source, Relation sourceSurface, Relation definitions, Relation layers,
        UnitArrangementProgram result, Relation resultSurface, Relation blocked, Relation unresolved)
    {
        Source = source; SourceSurface = sourceSurface; Definitions = definitions; Layers = layers; Result = result;
        ResultSurface = resultSurface; Blocked = blocked; Unresolved = unresolved;
        Surface = new(new Relation(SourceSurface, definitions), new Relation(Layers, new Relation(ResultSurface, new Relation(Blocked, Unresolved))));
    }

    public bool TryReadLayer(Relation actualRead, out Relation definition, out Relation selected, out Relation complement, out Relation added, out Relation attachmentReads)
    {
        definition = selected = complement = added = attachmentReads = null!;
        return Contains(Layers, actualRead) && ReadLayer(actualRead, out definition, out selected, out complement, out added, out attachmentReads);
    }

    private static bool ReadLayer(Relation actualRead, out Relation definition, out Relation selected, out Relation complement, out Relation added, out Relation attachmentReads)
    {
        definition = selected = complement = added = attachmentReads = null!;
        if (actualRead is not { Left: Relation request, Right: Relation { Left: Relation chosen, Right: Relation { Left: Relation carried,
                Right: Relation { Left: Relation additions, Right: Relation anchors } } } }) return false;
        definition = request; selected = chosen; complement = carried; added = additions; attachmentReads = anchors; return true;
    }

    private static bool SameReduction(QualitativeMotionFrame frame, Relation actual, Relation expected) =>
        actual.Left is Relation { Left: var actualFrame, Right: Relation { Left: Relation before, Right: Relation incoming } } &&
        expected.Left is Relation { Right: Relation { Left: Relation expectedBefore, Right: Relation expectedIncoming } } &&
        ReferenceEquals(actualFrame, frame.Surface) && SameWord(frame, before, expectedBefore) && SameWord(frame, incoming, expectedIncoming) &&
        frame.TryReadResult(actual, out var result, out _, out var unresolved) && N.E(unresolved).Length == 0 &&
        frame.TryReadResult(expected, out var expectedResult, out _, out _) && SameWord(frame, result, expectedResult);
    private static bool SameWord(QualitativeMotionFrame frame, Relation actual, Relation expected) =>
        N.Read(actual, out var a) && N.Read(expected, out var b) && a.Length == b.Length && a.Zip(b).All(pair =>
            pair.First is Relation x && pair.Second is Relation y && ReferenceEquals(x.Left, y.Left) &&
            frame.Direction(x, out _, out _, out var xr) && frame.Direction(y, out _, out _, out var yr) && N.Same(xr, yr));
    private static bool SameEntries(Relation actual, Relation expected) => N.Read(actual, out var a) && N.Read(expected, out var b) &&
        a.Length == b.Length && a.Zip(b).All(pair => ReferenceEquals(pair.First, pair.Second));
    private static UnitArrangementProgram ReadProgram(QualitativeMotionFrame frame, Relation surface) =>
        TryProgram(frame, surface, out var program) ? program : throw new ArgumentException("The source must be a complete native path declaration in the selected frame.", nameof(surface));

    private static bool TryProgram(QualitativeMotionFrame frame, Relation surface, out UnitArrangementProgram program)
    {
        program = null!;
        if (!ReferenceEquals(surface.Left, frame.Surface) || surface.Right is not Relation { Left: Relation { Left: Relation paths, Right: Relation ports },
                Right: Relation { Left: Relation closed, Right: var evidence } }) return false;
        program = new(frame, "retained native addition", paths, ports, closed, evidence); return program.Complete();
    }
    private static bool Contains(Relation scope, IElement item) => N.E(scope).Any(actual => ReferenceEquals(actual, item));
    private static bool Word(QualitativeMotionFrame frame, Relation word) => N.Read(word, out var entries) &&
        entries.All(item => item is Relation move && frame.Direction(move, out _, out _, out _));
}

/// <summary>A decoded structural observation. Its source and selected/carried
/// layer membership live in Surface, independently of any style assignment.</summary>
public sealed class OrnamentEnrichmentFocus
{
    // Surface = (enrichment -> question identity) ->
    //           (selected layer reads -> (carried siblings -> unresolved))
    public Relation Selected { get; }
    public Relation Complement { get; }
    public Relation Unresolved { get; }
    public Relation Surface { get; }
    public bool IsComplete => N.E(Unresolved).Length == 0;
    internal OrnamentEnrichmentFocus(OrnamentEnrichment source, IElement identity)
    {
        var layers = N.E(source.Layers).Cast<Relation>().ToArray();
        Selected = N.S(layers.Where(layer => layer.Left is Relation definition && N.Same(definition.Left, identity)).Cast<IElement>().ToArray());
        Complement = N.S(layers.Where(layer => !N.E(Selected).Any(actual => ReferenceEquals(actual, layer))).Cast<IElement>().ToArray());
        Unresolved = N.E(Selected).Length == 1 ? N.S() : N.S(new Relation(identity, source.Definitions));
        Surface = new(new Relation(source.Surface, identity), new Relation(Selected, new Relation(Complement, Unresolved)));
    }
}

using Gojo.Core;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Authors a finer ornament grid using ordinary native word
/// substitution. One former move is represented by several participations of
/// the same unmeasured reach. Source paths, substitution receipts and actual
/// path/port correspondence remain connected. Dividing the optional display
/// basis is a presentation correspondence, not an analytic calibration law.</summary>
public sealed class BourgoinPatternRefinement
{
    public BourgoinPatternLibrary.Recipe Source { get; }
    public BourgoinPatternLibrary.Recipe Result { get; }
    public QualitativePatternTransform Transform { get; }
    public Relation Correspondence { get; }
    public Relation Surface { get; }

    public BourgoinPatternRefinement(BourgoinPatternLibrary.Recipe source, int subdivisions)
    {
        ValidateSubdivisions(subdivisions);
        Source = source;
        var motifs = Read(source.Motifs).Cast<Relation>().ToArray();
        if (source.Program.Layout is not { Left: Relation { Left: Relation uCycle, Right: Relation vCycle },
                Right: Relation { Left: Relation pitchSlots } })
            throw new ArgumentException("The source must expose its actual two layout cycles and pitch slots.", nameof(source));
        var parameters = Read(source.Program.Parameters).Cast<Relation>().ToArray();
        var bindings = Read(source.Bindings).Cast<Relation>().ToArray();
        var pitchParameters = parameters.Where(row => ReferenceEquals(row.Left, pitchSlots.Left) ||
            ReferenceEquals(row.Left, pitchSlots.Right)).ToArray();
        if (pitchParameters.Length != 2)
            throw new ArgumentException("Both actual pitch slots need explicit native domains.", nameof(source));

        // Only the public start/chord and layout-pitch word roles supply this
        // substitution's domain. Retained ancestry is not mined for lookalikes.
        var sourceWords = new List<Relation> { source.ColumnPitch, source.RowPitch };
        foreach (var motif in motifs)
        {
            var unit = Parse(motif);
            foreach (var path in Read(unit.Paths).Cast<Relation>())
            {
                sourceWords.Add((Relation)path.Left);
                sourceWords.AddRange(Read((Relation)path.Right).Cast<Relation>());
            }
        }
        foreach (var row in pitchParameters) sourceWords.AddRange(Read((Relation)row.Right).Cast<Relation>());
        foreach (var row in bindings.Where(row => ReferenceEquals(row.Left, pitchSlots.Left) ||
            ReferenceEquals(row.Left, pitchSlots.Right))) sourceWords.Add((Relation)row.Right);
        var reaches = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        foreach (var word in sourceWords) foreach (var item in Read(word))
        {
            if (item is not Relation move || !source.Frame.Direction(move, out _, out _, out var reach))
                throw new ArgumentException("Refinement only substitutes complete words in the source's exposed frame.", nameof(source));
            reaches.Add(reach);
        }
        Transform = new(source.Frame, source.Frame, N.S(reaches.SelectMany(reach =>
            new[] { source.Frame.Right, source.Frame.Left, source.Frame.Up, source.Frame.Down }.Select(direction =>
            {
                var move = QualitativeMotionFrame.Move(direction, reach);
                return (IElement)new Relation(move, N.S(Enumerable.Repeat<IElement>(move, subdivisions).ToArray()));
            })).ToArray()));

        if (subdivisions == 1)
        {
            Result = source; Correspondence = new(source.Program.Surface, source.Program.Surface);
            Surface = new(new Relation(source.Program.Surface, source.SeamContract),
                new Relation(Transform.Surface, Correspondence));
            return;
        }

        var words = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var receipts = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var maps = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        foreach (var motif in motifs)
        {
            var unit = Parse(motif);
            var actualPaths = Read(unit.Paths).Cast<Relation>().ToArray();
            var selectedWords = actualPaths.SelectMany(path =>
                new[] { (Relation)path.Left }.Concat(Read((Relation)path.Right).Cast<Relation>()))
                .Distinct(ReferenceComparer<Relation>.Instance).ToArray();
            foreach (var path in actualPaths)
                maps.Add(path, new Relation(MapWord((Relation)path.Left),
                    N.S(Read((Relation)path.Right).Cast<Relation>().Select(word => (IElement)MapWord(word)).ToArray())));
            var pathPairs = N.S(actualPaths.Select(path => (IElement)new Relation(path, maps[path])).ToArray());
            var wordPairs = N.S(selectedWords.Select(word => (IElement)new Relation(word, MapWord(word))).ToArray());
            var newPorts = N.S(Read(unit.Ports).Cast<Relation>().Select(port =>
            {
                if (port.Right is not Relation pick || !maps.TryGetValue(pick.Left, out var mappedPath))
                    throw new ArgumentException("A source port must select an actual source path.", nameof(source));
                var mappedPick = ReferenceEquals(pick.Left, pick.Right) ? mappedPath : MapWord((Relation)pick.Right);
                var result = new Relation(port.Left, new Relation(mappedPath, mappedPick));
                maps.Add(port, result); return (IElement)result;
            }).ToArray());
            var evidence = new Relation(motif, new Relation(Transform.Surface,
                new Relation(new Relation(pathPairs, wordPairs),
                    N.S(selectedWords.Select(word => (IElement)receipts[word]).ToArray()))));
            var mapped = new UnitArrangementProgram(source.Frame, unit.Name,
                N.S(actualPaths.Select(path => maps[path]).ToArray()), newPorts,
                N.S(Read(unit.ClosedPaths).Select(path => maps[path]).ToArray()), evidence);
            if (!mapped.Complete()) throw new InvalidOperationException("Native word refinement did not preserve the source path/port contract.");
            maps.Add(motif, mapped.Surface);
        }
        foreach (var word in sourceWords) MapWord(word);
        var domains = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var parameterRows = N.S(parameters.Select(row =>
        {
            var domain = (Relation)row.Right;
            if (!domains.TryGetValue(domain, out var mapped))
            {
                var entries = Read(domain);
                mapped = entries.Any(maps.ContainsKey) ? N.S(entries.Select(Map).ToArray()) : domain;
                domains.Add(domain, mapped);
            }
            var replacement = ReferenceEquals(mapped, domain) ? row : new Relation(row.Left, mapped);
            maps.Add(row, replacement); return (IElement)replacement;
        }).ToArray());
        var program = new OrnamentProgram(source.Frame, N.S(motifs.Select(Map).ToArray()),
            source.Transforms, parameterRows, source.Program.Layout);
        var mappedBindings = N.S(bindings.Select(row =>
        {
            var mapped = Map(row.Right);
            var replacement = ReferenceEquals(mapped, row.Right) ? row : new Relation(row.Left, mapped);
            maps.Add(row, replacement); return (IElement)replacement;
        }).ToArray());
        maps.Add(source.Program.Surface, program.Surface);
        var seamSteps = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        var selectors = new Dictionary<Relation, Relation>(ReferenceEqualityComparer.Instance);
        if (source.SeamContract is not { Left: Relation { Left: var owner, Right: Relation axes }, Right: Relation rules } ||
            !ReferenceEquals(owner, source.Program.Surface) || !ReferenceEquals(axes.Left, uCycle) || !ReferenceEquals(axes.Right, vCycle))
            throw new ArgumentException("The source seam law must expose this program's actual layout cycles.", nameof(source));
        var mappedRules = Read(rules).Cast<Relation>().Select(rule =>
        {
            if (rule is not { Left: Relation { Left: Relation from, Right: Relation to },
                    Right: Relation { Left: Relation offset, Right: Relation roles } })
                throw new ArgumentException("The source must expose native seam selectors, offset and port roles.", nameof(source));
            var steps = Read(offset).Cast<Relation>().Select(step =>
            {
                if (!seamSteps.TryGetValue(step, out var mapped))
                {
                    if (ReferenceEquals(step.Left, source.Program.Surface) &&
                        (ReferenceEquals(step.Right, uCycle) || ReferenceEquals(step.Right, vCycle))) mapped = new(program.Surface, step.Right);
                    else if (ReferenceEquals(step.Right, source.Program.Surface) &&
                        (ReferenceEquals(step.Left, uCycle) || ReferenceEquals(step.Left, vCycle))) mapped = new(step.Left, program.Surface);
                    else throw new ArgumentException("A seam offset must retain the actual source program and cycle.", nameof(source));
                    seamSteps.Add(step, mapped); maps.Add(step, mapped);
                }
                return (IElement)mapped;
            }).ToArray();
            var replacement = new Relation(new Relation(MapSelector(from), MapSelector(to)), new Relation(N.S(steps), roles));
            maps.Add(rule, replacement); return replacement;
        }).ToArray();
        var contract = OrnamentProgramField.Contract(program, mappedRules);
        var display = source.DisplayBasis;
        Result = source with { Program = program, Bindings = mappedBindings,
            ColumnPitch = MapWord(source.ColumnPitch), RowPitch = MapWord(source.RowPitch), SeamContract = contract,
            DisplayBasis = new(display.RX / subdivisions, display.RY / subdivisions, display.UX / subdivisions, display.UY / subdivisions) };
        Correspondence = new(new Relation(source.Program.Surface, program.Surface),
            new Relation(N.S(maps.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray()),
                N.S(receipts.Values.Cast<IElement>().ToArray())));
        Surface = new(new Relation(new Relation(source.Program.Surface, source.Bindings), source.SeamContract),
            new Relation(Transform.Surface, new Relation(new Relation(program.Surface, mappedBindings), new Relation(contract, Correspondence))));

        IElement Map(IElement item) => maps.TryGetValue(item, out var mapped) ? mapped : item;
        Relation MapWord(Relation word)
        {
            if (words.TryGetValue(word, out var retained)) return retained;
            var read = Transform.Apply(word);
            if (!Transform.TryReadComplete(read, out var result))
                throw new ArgumentException("The selected word has unresolved participation in the native substitution.", nameof(source));
            words.Add(word, result); receipts.Add(word, read); maps.Add(word, result); return result;
        }
        Relation MapSelector(Relation selector)
        {
            if (!selectors.TryGetValue(selector, out var mapped))
            {
                mapped = new(Map(selector.Left), selector.Right); selectors.Add(selector, mapped); maps.Add(selector, mapped);
            }
            return mapped;
        }
        UnitArrangementProgram Parse(Relation motif)
        {
            if (!ReferenceEquals(motif.Left, source.Frame.Surface) ||
                motif.Right is not Relation { Left: Relation { Left: Relation paths, Right: Relation ports },
                    Right: Relation { Left: Relation closed, Right: var evidence } })
                throw new ArgumentException("Refinement requires an exact native motif declaration.", nameof(source));
            var result = new UnitArrangementProgram(source.Frame, source.Name, paths, ports, closed, evidence);
            if (!result.Complete()) throw new ArgumentException("Refinement requires a complete source path/port contract.", nameof(source));
            return result;
        }
    }

    /// <summary>The atlas authoring adapter bounds deliberate expansion; this
    /// is a host allocation contract, not a limit of the native word algebra.</summary>
    internal static void ValidateSubdivisions(int subdivisions)
    {
        if (subdivisions is < 1 or > 16)
            throw new ArgumentOutOfRangeException(nameof(subdivisions), "Choose one to sixteen unmeasured participations per source move.");
    }
    private static IElement[] Read(Relation scope) => N.Read(scope, out var entries) ? entries :
        throw new ArgumentException("Refinement requires closed native declaration scopes.", nameof(scope));
    private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceComparer<T> Instance = new();
        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);
        public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}

using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A source-free copy of an advertised observation. The correspondence
/// remains sender-side; the packet contains no source event, positions, recipe
/// or assessment history. Omitted relation scopes stay open.</summary>
public sealed class OrnamentReconstructionPacket
{
    public QualitativeRecognitionObservation Observation { get; }
    public Relation Surface => Observation.Surface;
    public Relation Correspondence { get; }

    public OrnamentReconstructionPacket(QualitativeRecognitionObservation source, bool includeRelations)
    {
        var frame = new QualitativeMotionFrame();
        var sites = E(source.Sites).ToDictionary(site => site, _ => (IElement)new Placeholder(),
            FieldNative.Participants.Instance);
        var segments = new Dictionary<IElement, IElement>(ReferenceEqualityComparer.Instance);
        foreach (var item in E(source.Segments))
        {
            if (item is not Relation edge || !source.TryReadSegment(edge, out _, out var from, out var to, out var headings))
                throw new ArgumentException("A packet copies an advertised segment observation.", nameof(source));
            var heading = Scope(headings, E(headings).Select(Direction).ToArray());
            segments.Add(edge, new Relation(new Placeholder(), new Relation(new Relation(sites[from], sites[to]), heading)));
        }
        var continuations = new List<IElement>(); var crossings = new List<IElement>();
        if (includeRelations)
        {
            foreach (var item in E(source.Continuations))
            {
                if (item is not Relation row || !source.TryReadContinuation(row, out var site, out var first, out var second))
                    throw new ArgumentException("A published continuation needs its actual participating segments.");
                continuations.Add(new Relation(sites[site], new Relation(segments[first], segments[second])));
            }
            foreach (var item in E(source.Crossings))
            {
                if (item is not Relation row || !source.TryReadCrossing(row, out var first, out var second))
                    throw new ArgumentException("A published crossing needs its actual participating segments.");
                crossings.Add(new Relation(segments[first], segments[second]));
            }
        }
        Observation = new(frame, Scope(source.Sites, sites.Values.ToArray()), Scope(source.Segments, segments.Values.ToArray()),
            includeRelations ? Scope(source.Continuations, continuations.ToArray()) : StructuralConstraint.Open(),
            includeRelations ? Scope(source.Crossings, crossings.ToArray()) : StructuralConstraint.Open());
        Correspondence = new(new Relation(source.Surface, Surface), new Relation(
            S(sites.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray()),
            S(segments.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray())));

        IElement Direction(IElement direction) => ReferenceEquals(direction, source.Frame.Right) ? frame.Right :
            ReferenceEquals(direction, source.Frame.Left) ? frame.Left : ReferenceEquals(direction, source.Frame.Up) ? frame.Up :
            ReferenceEquals(direction, source.Frame.Down) ? frame.Down : throw new ArgumentException("Unknown heading frame.");
    }

    public static HashSet<IElement> Participants(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item)) if (seen.Add(item) && item is Relation relation)
        { pending.Push(relation.Left); pending.Push(relation.Right); }
        return seen;
    }

    private static Relation Scope(Relation original, IElement[] items) =>
        QualitativeRecognitionObservation.Closed(original, out _) ? S(items) : StructuralConstraint.Open(items);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
}

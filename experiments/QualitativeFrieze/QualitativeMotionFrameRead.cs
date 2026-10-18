using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>A narrow compiled read of the current, complete unmeasured motion
/// frame law. It checks ordered structure and all participant aliases against
/// the standard constructor, then retains an explicit change of identities.
/// It does not infer a frame from headings or supply missing laws.</summary>
public static class QualitativeMotionFrameRead
{
    public static bool TryReadStandard(Relation sourceSurface, out QualitativeMotionFrameRebinding read) =>
        TryReadStandard(sourceSurface, out read, out _);

    public static bool TryReadStandard(Relation sourceSurface, out QualitativeMotionFrameRebinding read,
        out Relation witness)
    {
        var target = new QualitativeMotionFrame();
        var forward = new Dictionary<IElement, IElement>(N.Participants.Instance);
        var reverse = new Dictionary<IElement, IElement>(N.Participants.Instance);
        var pending = new Stack<(IElement Source, IElement Target)>();
        pending.Push((sourceSurface, target.Surface));
        var failures = new List<IElement>();
        while (pending.TryPop(out var pair))
        {
            if (forward.TryGetValue(pair.Source, out var prior))
            {
                if (!N.Same(prior, pair.Target)) { failures.Add(new Relation(pair.Source, pair.Target)); break; }
                continue;
            }
            if (reverse.TryGetValue(pair.Target, out var other) && !N.Same(other, pair.Source))
            { failures.Add(new Relation(pair.Source, pair.Target)); break; }
            if (pair.Source is Placeholder a && pair.Target is Placeholder b && a.IsInverse == b.IsInverse)
            {
                forward.Add(a, b); reverse.Add(b, a); continue;
            }
            if (pair.Source is not Relation source || pair.Target is not Relation destination)
            { failures.Add(new Relation(pair.Source, pair.Target)); break; }
            forward.Add(source, destination); reverse.Add(destination, source);
            pending.Push((source.Right, destination.Right)); pending.Push((source.Left, destination.Left));
        }
        var correspondence = N.S(forward.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray());
        witness = new(new Relation(sourceSurface, target.Surface),
            new Relation(correspondence, failures.Count == 0 ? N.S() : StructuralConstraint.Open(failures.ToArray())));
        if (failures.Count != 0) { read = null!; return false; }
        read = new(sourceSurface, target, forward, correspondence, witness); return true;
    }
}

/// <summary>A verified alpha-rebinding into a fresh standard frame. Program
/// relations are copied with one shared memo, so actual path/chord selectors
/// remain aliases. External placeholders are imported unchanged deliberately:
/// this changes a frame, not occurrence ownership or parameter identity.</summary>
public sealed class QualitativeMotionFrameRebinding
{
    public Relation SourceSurface { get; }
    public QualitativeMotionFrame Target { get; }
    public Relation Correspondence { get; }
    public Relation Witness { get; }
    public Relation Surface => Witness;
    private readonly IReadOnlyDictionary<IElement, IElement> frame;
    private readonly Dictionary<IElement, IElement> copies = new(N.Participants.Instance);

    internal QualitativeMotionFrameRebinding(Relation source, QualitativeMotionFrame target,
        IReadOnlyDictionary<IElement, IElement> frame, Relation correspondence, Relation witness)
    { SourceSurface = source; Target = target; this.frame = frame; Correspondence = correspondence; Witness = witness; }

    /// <summary>Reads one acyclic relation/placeholder program fragment. Use
    /// the same rebind instance for Paths, Ports and ClosedPaths. A failed read
    /// leaves the source untouched and commits no partially copied fragments.
    /// Measured participants and cyclic programs need a separate contract.</summary>
    public bool TryRebind(IElement source, out IElement target, out Relation witness)
    {
        var local = new Dictionary<IElement, IElement>(N.Participants.Instance);
        var active = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<(IElement Element, bool Finish)>(); pending.Push((source, false));
        var failures = new List<IElement>();
        while (pending.TryPop(out var item))
        {
            if (local.ContainsKey(item.Element)) continue;
            if (frame.TryGetValue(item.Element, out var mapped) || copies.TryGetValue(item.Element, out mapped))
            { local.Add(item.Element, mapped); continue; }
            if (item.Element is Placeholder slot)
            {
                // The other face need not have been explicitly traversed in
                // the standard law, but it belongs to that same bound slot.
                local.Add(slot, frame.TryGetValue(slot.Dual(), out var opposed) && opposed is Placeholder mappedSlot
                    ? mappedSlot.Dual() : slot);
                continue;
            }
            if (item.Element is not Relation relation)
            { failures.Add(item.Element); break; }
            if (!item.Finish)
            {
                if (!active.Add(relation)) { failures.Add(relation); break; }
                pending.Push((relation, true)); pending.Push((relation.Right, false)); pending.Push((relation.Left, false));
            }
            else
            {
                local.Add(relation, new Relation(local[relation.Left], local[relation.Right])); active.Remove(relation);
            }
        }
        var complete = failures.Count == 0;
        target = complete ? local[source] : source;
        witness = new(new Relation(Witness, source), new Relation(
            complete ? N.S(target) : StructuralConstraint.Open(),
            new Relation(N.S(local.Select(pair => (IElement)new Relation(pair.Key, pair.Value)).ToArray()),
                complete ? N.S() : StructuralConstraint.Open(failures.ToArray()))));
        if (complete) foreach (var pair in local) copies.TryAdd(pair.Key, pair.Value);
        return complete;
    }

    /// <summary>Convenience for an already validated acyclic program.</summary>
    public IElement Rebind(IElement source) => TryRebind(source, out var target, out _) ? target :
        throw new ArgumentException("Frame rebinding requires an acyclic relation/placeholder program.", nameof(source));
}

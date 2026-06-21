using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Encoding;

/// <summary>
/// Relation-shaped observer path over encoded or live Gojo structure. The path
/// is a saved read surface: it records where an observer started, which local
/// steps were followed, what was read, and what residue or tension remained.
/// </summary>
public static class GojoObserverPathSurface
{
    public readonly record struct PathRead(
        IElement SelectedSource,
        IElement ObserverSurface,
        Relation[] Steps,
        IElement Result,
        IElement ResidueOrTension,
        IElement Provenance);

    public readonly record struct StepRead(
        IElement From,
        IElement Through,
        IElement To,
        IElement Evidence,
        IElement ResidueOrTension);

    public static Relation Create(
        Carrier root,
        IElement selectedSource,
        IElement observerSurface,
        Relation stepFrame,
        IElement result,
        IElement? residueOrTension = null,
        IElement? provenance = null) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            selectedSource ?? EncodedTextSurface.UnknownPoint(),
            observerSurface ?? EncodedTextSurface.UnknownPoint(),
            stepFrame ?? StepFrame(new Carrier()),
            result ?? EncodedTextSurface.UnknownPoint(),
            residueOrTension ?? EncodedTextSurface.UnknownPoint(),
            provenance ?? EncodedTextSurface.UnknownPoint());

    public static Relation Step(
        Carrier root,
        IElement from,
        IElement through,
        IElement to,
        IElement? evidence = null,
        IElement? residueOrTension = null) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            from ?? EncodedTextSurface.UnknownPoint(),
            through ?? EncodedTextSurface.UnknownPoint(),
            to ?? EncodedTextSurface.UnknownPoint(),
            evidence ?? EncodedTextSurface.UnknownPoint(),
            residueOrTension ?? EncodedTextSurface.UnknownPoint());

    public static Relation StepFrame(Carrier root, params Relation[] steps) =>
        Relation.OrderedGroup(EncodedTextSurface.RootOrNew(root), steps ?? []);

    public static Relation ObserverSurface(
        Carrier root,
        IElement anchor,
        IElement basis,
        IElement? resolution = null,
        IElement? intent = null) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            anchor ?? EncodedTextSurface.UnknownPoint(),
            basis ?? EncodedTextSurface.UnknownPoint(),
            resolution ?? EncodedTextSurface.UnknownPoint(),
            intent ?? EncodedTextSurface.UnknownPoint());

    public static bool TryRead(IElement element, out PathRead read)
    {
        if (element is Relation path &&
            EncodedTextSurface.TryReadRootElements(path, out var elements) &&
            elements.Length == 6 &&
            TryReadStepFrame(elements[2], out var steps))
        {
            read = new PathRead(
                elements[0],
                elements[1],
                steps,
                elements[3],
                elements[4],
                elements[5]);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadStep(IElement element, out StepRead read)
    {
        if (element is Relation step &&
            EncodedTextSurface.TryReadRootElements(step, out var elements) &&
            elements.Length == 5)
        {
            read = new StepRead(
                elements[0],
                elements[1],
                elements[2],
                elements[3],
                elements[4]);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadStepFrame(IElement element, out Relation[] steps)
    {
        if (element is Relation frame &&
            EncodedTextSurface.TryReadRootElements(frame, out var elements) &&
            elements.All(item => item is Relation step && TryReadStep(step, out _)))
        {
            steps = [.. elements.Cast<Relation>()];
            return true;
        }

        steps = [];
        return false;
    }

    public static bool TryCreateWitness(Carrier root, Relation path, out Relation witness)
    {
        if (TryRead(path, out var read))
        {
            witness = ReadWitness.Create(
                EncodedTextSurface.RootOrNew(root),
                read.SelectedSource,
                path,
                read.Result,
                read.ResidueOrTension,
                read.Provenance);
            return true;
        }

        witness = null!;
        return false;
    }
}

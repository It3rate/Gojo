using Gojo.Core;

namespace Gojo.Encoding;

/// <summary>
/// Compact relation-shaped read frame for scene mechanic reads. The helper
/// owns no word meaning; it only keeps the six proposed read ingredients
/// connected and locally discoverable.
/// </summary>
public static class SceneMechanicReadFrameSurface
{
    public readonly record struct ReadFrameRead(
        IElement SelectedSourceFrame,
        IElement FocusWitness,
        IElement BasisWitness,
        IElement TransformWitness,
        IElement GoalOrResidueWitness,
        IElement LexicalDebugAttachments);

    public readonly record struct DefinitionRead(
        ConceptDefinitionDiscoverySurface.DefinitionNode Definition,
        ReadFrameRead Frame,
        string[] LexicalDebugLabels);

    public static Relation Create(
        Carrier root,
        IElement selectedSourceFrame,
        IElement focusWitness,
        IElement basisWitness,
        IElement transformWitness,
        IElement goalOrResidueWitness,
        IElement lexicalDebugAttachments) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            selectedSourceFrame ?? UnknownPoint(),
            focusWitness ?? UnknownPoint(),
            basisWitness ?? UnknownPoint(),
            transformWitness ?? UnknownPoint(),
            goalOrResidueWitness ?? UnknownPoint(),
            lexicalDebugAttachments ?? UnknownPoint());

    public static Relation LexicalDebugFrame(Carrier root, params string[] labels) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            [.. (labels ?? []).Select(label => EncodedTextSurface.Create(new Carrier(), label))]);

    public static bool TryRead(IElement element, out ReadFrameRead read)
    {
        if (element is Relation frame &&
            EncodedTextSurface.TryReadRootElements(frame, out var elements) &&
            elements.Length == 6)
        {
            read = new ReadFrameRead(
                elements[0],
                elements[1],
                elements[2],
                elements[3],
                elements[4],
                elements[5]);
            return true;
        }

        read = default;
        return false;
    }

    public static bool TryReadFrames(IElement ledger, out DefinitionRead[] reads)
    {
        if (!ConceptDefinitionDiscoverySurface.TryReadLedger(ledger, out var graph))
        {
            reads = [];
            return false;
        }

        var found = new List<DefinitionRead>();
        foreach (var node in graph.Nodes)
        {
            if (!TryRead(node.Read.Body, out var frame))
            {
                continue;
            }

            found.Add(new DefinitionRead(
                node,
                frame,
                ReadLexicalDebugLabels(frame.LexicalDebugAttachments)));
        }

        reads = [.. found];
        return reads.Length > 0;
    }

    public static string[] ReadLexicalDebugLabels(IElement lexicalDebugAttachments)
    {
        if (EncodedTextSurface.TryRead(lexicalDebugAttachments, out var single))
        {
            return [single];
        }

        if (lexicalDebugAttachments is Relation frame &&
            EncodedTextSurface.TryReadRootElements(frame, out var elements))
        {
            return [.. elements
                .Select(element => EncodedTextSurface.TryRead(element, out var label) ? label : string.Empty)
                .Where(label => label.Length > 0)];
        }

        return [];
    }

    private static Relation UnknownPoint() =>
        Relation.Point(Carrier.Unknown, 0);
}

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
        IElement lexicalDebugAttachments)
    {
        var slotCarrier = new Carrier();
        return Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            Slot(slotCarrier, 1, selectedSourceFrame),
            Slot(slotCarrier, 2, focusWitness),
            Slot(slotCarrier, 3, basisWitness),
            Slot(slotCarrier, 4, transformWitness),
            Slot(slotCarrier, 5, goalOrResidueWitness),
            Slot(slotCarrier, 6, lexicalDebugAttachments));
    }

    public static Relation LexicalDebugFrame(Carrier root, params string[] labels) =>
        Relation.OrderedGroup(
            EncodedTextSurface.RootOrNew(root),
            [.. (labels ?? []).Select(label => EncodedTextSurface.Create(new Carrier(), label))]);

    public static bool TryRead(IElement element, out ReadFrameRead read)
    {
        if (element is Relation frame &&
            EncodedTextSurface.TryReadRootElements(frame, out var elements) &&
            elements.Length == 6 &&
            TryReadSlots(elements, out var slots))
        {
            read = new ReadFrameRead(
                slots[0],
                slots[1],
                slots[2],
                slots[3],
                slots[4],
                slots[5]);
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
        if (ConceptDefinitionSurface.TryRead(lexicalDebugAttachments, out var definition))
        {
            return ReadLexicalDebugLabels(definition.Body);
        }

        if (lexicalDebugAttachments is Relation frame &&
            EncodedTextSurface.TryReadRootElements(frame, out var elements))
        {
            var labels = elements
                .Select(element => EncodedTextSurface.TryRead(element, out var label) ? label : string.Empty)
                .Where(label => label.Length > 0)
                .ToArray();

            if (labels.Length > 0)
            {
                return labels;
            }
        }

        if (EncodedTextSurface.TryRead(lexicalDebugAttachments, out var single))
        {
            return [single];
        }

        return [];
    }

    private static Relation UnknownPoint() =>
        Relation.Point(Carrier.Unknown, 0);

    private static Relation Slot(Carrier slotCarrier, long ticks, IElement value) =>
        new(Relation.Point(slotCarrier, ticks), value ?? UnknownPoint());

    private static bool TryReadSlots(IElement[] elements, out IElement[] slots)
    {
        slots = new IElement[6];
        Carrier? slotCarrier = null;
        foreach (var element in elements)
        {
            if (element is not Relation binding ||
                !EncodedTextSurface.TryReadPoint(binding.Left, out var slot) ||
                slot.Ticks is < 1 or > 6)
            {
                slots = [];
                return false;
            }

            slotCarrier ??= slot.Carrier;
            var slotIndex = (int)slot.Ticks - 1;
            if (!slot.Carrier.SameIdentityAs(slotCarrier) ||
                slots[slotIndex] is not null)
            {
                slots = [];
                return false;
            }

            slots[slotIndex] = binding.Right;
        }

        if (slots.Any(slot => slot is null))
        {
            slots = [];
            return false;
        }

        return true;
    }
}

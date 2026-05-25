using Gojo.Core;

namespace Gojo.Patterns;

/// <summary>
/// Small helpers for applying slot bindings to ordinary Gojo structure.
/// The helpers produce relation/carrier/location structure; they are not a
/// durable pattern or repeat object.
/// </summary>
public static class StructuralPattern
{
    /// <summary>Creates relation-shaped binding from a placeholder to an element.</summary>
    public static Relation Bind(Placeholder placeholder, IElement element) =>
        new(placeholder, element);

    /// <summary>Creates relation-shaped binding from a unit-shape descriptor slot to an element.</summary>
    public static Relation BindDescriptor(int descriptorIndex, IElement element) =>
        new(UnitShape.DescriptorSlot(descriptorIndex), element);

    /// <summary>Reads relation-shaped binding from a placeholder to an element.</summary>
    public static bool TryReadBinding(Relation relation, out Placeholder placeholder, out IElement element)
    {
        if (relation.Left is Placeholder slot)
        {
            placeholder = slot;
            element = relation.Right;
            return true;
        }

        placeholder = default!;
        element = default!;
        return false;
    }

    /// <summary>Reads relation-shaped binding from a unit-shape descriptor slot to an element.</summary>
    public static bool TryReadDescriptorBinding(Relation relation, out int descriptorIndex, out IElement element)
    {
        if (UnitShape.TryReadDescriptorSlot(relation.Left, out descriptorIndex))
        {
            element = relation.Right;
            return true;
        }

        descriptorIndex = 0;
        element = default!;
        return false;
    }

    /// <summary>Reads an element when it can serve as a structural pattern slot.</summary>
    public static bool TryReadSlot(IElement element, out IElement slot)
    {
        if (element is Placeholder placeholder)
        {
            slot = placeholder;
            return true;
        }

        if (UnitShape.TryReadDescriptorSlot(element, out var descriptorIndex))
        {
            slot = UnitShape.DescriptorSlot(descriptorIndex);
            return true;
        }

        slot = default!;
        return false;
    }

    /// <summary>Reads relation-shaped binding from any supported structural pattern slot.</summary>
    public static bool TryReadSlotBinding(Relation relation, out IElement slot, out IElement element)
    {
        if (TryReadSlot(relation.Left, out slot))
        {
            element = relation.Right;
            return true;
        }

        element = default!;
        return false;
    }

    /// <summary>Reads distinct slot surfaces from a pattern in traversal order.</summary>
    public static IElement[] ReadSlots(IElement pattern)
    {
        var slots = new List<IElement>();
        ReadSlots(pattern, slots);
        return [.. slots];
    }

    /// <summary>Reads distinct slot-shaped bindings in first-seen order.</summary>
    public static Relation[] ReadSlotBindings(params Relation[] bindings) =>
        ReadSlotBindings((IReadOnlyList<Relation>)bindings);

    /// <summary>Reads distinct slot-shaped bindings in first-seen order.</summary>
    public static Relation[] ReadSlotBindings(IReadOnlyList<Relation> bindings)
    {
        var slotBindings = new List<Relation>();
        foreach (var binding in bindings)
        {
            if (TryReadSlotBinding(binding, out var slot, out var element) &&
                !slotBindings.Any(existing => Equals(existing.Left, slot)))
            {
                slotBindings.Add(new Relation(slot, element));
            }
        }

        return [.. slotBindings];
    }

    /// <summary>Reads one bound value for a slot from relation-shaped bindings.</summary>
    public static bool TryReadBoundSlot(
        IElement slot,
        IReadOnlyList<Relation> bindings,
        out IElement bound)
    {
        var slotBindings = ReadSlotBindings(bindings);
        return TryReadBoundSlotCore(slot, slotBindings, out bound);
    }

    /// <summary>Reads the pattern slots that are not supplied by a binding frame.</summary>
    public static IElement[] ReadUnboundSlots(IElement pattern, params Relation[] bindings) =>
        ReadUnboundSlots(pattern, (IReadOnlyList<Relation>)bindings);

    /// <summary>Reads the pattern slots that are not supplied by a binding frame.</summary>
    public static IElement[] ReadUnboundSlots(IElement pattern, IReadOnlyList<Relation> bindings)
    {
        var slotBindings = ReadSlotBindings(bindings);
        return
        [
            .. ReadSlots(pattern)
                .Where(slot => !TryReadBoundSlotCore(slot, slotBindings, out _))
        ];
    }

    /// <summary>Returns true when every slot surface in the pattern has a binding.</summary>
    public static bool IsComplete(IElement pattern, params Relation[] bindings) =>
        IsComplete(pattern, (IReadOnlyList<Relation>)bindings);

    /// <summary>Returns true when every slot surface in the pattern has a binding.</summary>
    public static bool IsComplete(IElement pattern, IReadOnlyList<Relation> bindings) =>
        ReadUnboundSlots(pattern, bindings).Length == 0;

    /// <summary>Applies a frame and reports unresolved slot surfaces without rejecting the structure.</summary>
    public static bool TryApplyComplete(
        IElement pattern,
        IReadOnlyList<Relation> bindings,
        out IElement applied,
        out IElement[] unboundSlots)
    {
        applied = Apply(pattern, bindings);
        unboundSlots = ReadUnboundSlots(pattern, bindings);
        return unboundSlots.Length == 0;
    }

    /// <summary>Applies a frame and reports unresolved slot surfaces without rejecting the structure.</summary>
    public static bool TryApplyComplete(
        IElement pattern,
        out IElement applied,
        out IElement[] unboundSlots,
        params Relation[] bindings) =>
        TryApplyComplete(pattern, bindings, out applied, out unboundSlots);

    /// <summary>Applies bindings to every matching placeholder or descriptor slot inside a pattern.</summary>
    public static IElement Apply(IElement pattern, params Relation[] bindings) =>
        Apply(pattern, (IReadOnlyList<Relation>)bindings);

    /// <summary>Applies bindings to every matching placeholder or descriptor slot inside a pattern.</summary>
    public static IElement Apply(IElement pattern, IReadOnlyList<Relation> bindings)
    {
        var slotBindings = ReadSlotBindings(bindings);
        return ApplyCore(pattern, slotBindings);
    }

    /// <summary>Instantiates one pattern once per binding frame.</summary>
    public static IElement[] Repeat(IElement pattern, params Relation[][] frames) =>
        Repeat(pattern, (IReadOnlyList<IReadOnlyList<Relation>>)frames);

    /// <summary>Instantiates one pattern once per binding frame.</summary>
    public static IElement[] Repeat(IElement pattern, IReadOnlyList<IReadOnlyList<Relation>> frames)
    {
        var copies = new IElement[frames.Count];
        for (var index = 0; index < frames.Count; index++)
        {
            copies[index] = Apply(pattern, frames[index]);
        }

        return copies;
    }

    /// <summary>
    /// Computes the next binding frame by applying the current frame to
    /// relation-shaped transition bindings.
    /// </summary>
    public static Relation[] Advance(IReadOnlyList<Relation> currentFrame, params Relation[] transitions) =>
        Advance(currentFrame, (IReadOnlyList<Relation>)transitions);

    /// <summary>
    /// Computes the next binding frame by applying the current frame to
    /// relation-shaped transition bindings.
    /// </summary>
    public static Relation[] Advance(
        IReadOnlyList<Relation> currentFrame,
        IReadOnlyList<Relation> transitions)
    {
        var current = ReadSlotBindings(currentFrame);
        var next = new List<Relation>();
        var seen = new List<IElement>();
        foreach (var transition in transitions)
        {
            if (TryReadSlotBinding(transition, out var slot, out var element) &&
                !seen.Any(existing => Equals(existing, slot)))
            {
                seen.Add(slot);
                next.Add(new Relation(slot, ApplyCore(element, current)));
            }
        }

        return [.. next];
    }

    /// <summary>Chains repeated copies into one relation-shaped surface.</summary>
    public static IElement Chain(IReadOnlyList<IElement> elements)
    {
        if (elements.Count == 0)
        {
            return new Placeholder();
        }

        var surface = elements[0];
        for (var index = 1; index < elements.Count; index++)
        {
            surface = new Relation(surface, elements[index]);
        }

        return surface;
    }

    private static void ReadSlots(IElement element, List<IElement> slots)
    {
        if (TryReadSlot(element, out var slot))
        {
            if (!slots.Any(existing => Equals(existing, slot)))
            {
                slots.Add(slot);
            }

            return;
        }

        if (element is Relation relation)
        {
            ReadSlots(relation.Left, slots);
            ReadSlots(relation.Right, slots);
        }
    }

    private static IElement ApplyCore(IElement element, IReadOnlyList<Relation> bindings)
    {
        if (TryReadSlot(element, out var slot) &&
            TryReadBoundSlotCore(slot, bindings, out var slotBound))
        {
            return slotBound;
        }

        return element switch
        {
            Relation relation => new Relation(
                ApplyCore(relation.Left, bindings),
                ApplyCore(relation.Right, bindings)),
            CarrierLocation location => new CarrierLocation(location.Carrier, location.Tick),
            _ => element
        };
    }

    private static bool TryReadBoundSlotCore(
        IElement slot,
        IReadOnlyList<Relation> bindings,
        out IElement bound)
    {
        foreach (var binding in bindings)
        {
            if (Equals(binding.Left, slot))
            {
                bound = binding.Right;
                return true;
            }
        }

        if (slot is not Placeholder placeholder)
        {
            bound = default!;
            return false;
        }

        var dual = placeholder.Dual();
        foreach (var binding in bindings)
        {
            if (Equals(binding.Left, dual))
            {
                bound = binding.Right.Dual();
                return true;
            }
        }

        bound = default!;
        return false;
    }
}

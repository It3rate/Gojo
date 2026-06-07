using Gojo.Core;
using Gojo.Patterns;
using Gojo.Structures;

namespace GojoCAD.Structures;

/// <summary>
/// Relation-shaped selection evidence for graphic/concept structures. A
/// selection is not a classifier result; it carries the searched source, local
/// domain/basis, requested concept pattern, concrete match witnesses, and
/// unresolved residue as ordinary Gojo structure.
/// </summary>
public static class GraphicSelectionSurface
{
    /// <summary>Creates an ordered selection/evidence frame.</summary>
    public static Relation Frame(Carrier root, params IElement[] elements) =>
        Relation.OrderedGroup(RootOrNew(root), elements ?? []);

    /// <summary>Creates an ordered frame of match witnesses.</summary>
    public static Relation MatchFrame(Carrier root, params IElement[] matches) =>
        Frame(root, matches ?? []);

    /// <summary>
    /// Creates a selection query/result:
    /// source, search domain/basis, requested concept pattern, selected
    /// matches, residue/tension.
    /// </summary>
    public static Relation Select(
        Carrier root,
        IElement source,
        IElement searchDomain,
        IElement conceptPattern,
        IElement matches,
        IElement residueOrTension) =>
        Frame(
            root,
            source ?? TensionSlot(),
            searchDomain ?? TensionSlot(),
            conceptPattern ?? TensionSlot(),
            matches ?? MatchFrame(new Carrier()),
            residueOrTension ?? TensionSlot());

    /// <summary>Creates a selection query with no settled matches yet.</summary>
    public static Relation Request(
        Carrier root,
        IElement source,
        IElement searchDomain,
        IElement conceptPattern) =>
        Select(
            root,
            source,
            searchDomain,
            conceptPattern,
            MatchFrame(new Carrier()),
            TensionSlot());

    /// <summary>
    /// Creates a concrete match witness:
    /// candidate relation, requested pattern, pattern-to-candidate bindings,
    /// supporting evidence, residue/tension.
    /// </summary>
    public static Relation Match(
        Carrier root,
        IElement candidate,
        IElement conceptPattern,
        IElement bindings,
        IElement evidence,
        IElement residueOrTension) =>
        Frame(
            root,
            candidate ?? TensionSlot(),
            conceptPattern ?? TensionSlot(),
            bindings ?? FocusSurface.BindingFrame(new Carrier()),
            evidence ?? TensionSlot(),
            residueOrTension ?? TensionSlot());

    /// <summary>
    /// Creates structural pattern evidence:
    /// requested pattern, pattern after supplied bindings, concrete
    /// candidate, residue/tension from unbound pattern slots.
    /// </summary>
    public static Relation PatternEvidence(
        Carrier root,
        IElement conceptPattern,
        IElement appliedPattern,
        IElement candidate,
        IElement residueOrTension) =>
        Frame(
            root,
            conceptPattern ?? TensionSlot(),
            appliedPattern ?? TensionSlot(),
            candidate ?? TensionSlot(),
            residueOrTension ?? TensionSlot());

    /// <summary>
    /// Creates a match witness by applying relation-shaped bindings to a
    /// concept pattern. Incomplete bindings remain visible as residue.
    /// </summary>
    public static bool TryCreatePatternMatch(
        Carrier root,
        IElement candidate,
        IElement conceptPattern,
        Relation bindings,
        out Relation match)
    {
        if (!FocusSurface.TryReadBindingFrame(bindings, out var bindingClauses))
        {
            match = null!;
            return false;
        }

        var pattern = conceptPattern ?? TensionSlot();
        var concreteCandidate = candidate ?? TensionSlot();
        var applied = ApplyCorrespondence(pattern, bindingClauses, out _);
        var unboundSlots = StructuralPattern.ReadSlots(applied);
        var residue = unboundSlots.Length == 0
            ? TensionSlot()
            : Frame(new Carrier(), unboundSlots);
        var evidence = PatternEvidence(
            new Carrier(),
            pattern,
            applied,
            concreteCandidate,
            residue);
        match = Match(
            RootOrNew(root),
            concreteCandidate,
            pattern,
            bindings,
            evidence,
            residue);
        return true;
    }

    /// <summary>Reads a selection query/result.</summary>
    public static bool TryReadSelection(
        Relation selection,
        out IElement source,
        out IElement searchDomain,
        out IElement conceptPattern,
        out IElement matches,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(selection, out var elements) &&
            elements.Length == 5)
        {
            source = elements[0];
            searchDomain = elements[1];
            conceptPattern = elements[2];
            matches = elements[3];
            residueOrTension = elements[4];
            return true;
        }

        source = default!;
        searchDomain = default!;
        conceptPattern = default!;
        matches = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads an ordered frame of match witnesses.</summary>
    public static bool TryReadMatchFrame(Relation matchFrame, out IElement[] matches) =>
        TryReadFrameElements(matchFrame, out matches);

    /// <summary>Reads a concrete match witness.</summary>
    public static bool TryReadMatch(
        Relation match,
        out IElement candidate,
        out IElement conceptPattern,
        out IElement bindings,
        out IElement evidence,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(match, out var elements) &&
            elements.Length == 5)
        {
            candidate = elements[0];
            conceptPattern = elements[1];
            bindings = elements[2];
            evidence = elements[3];
            residueOrTension = elements[4];
            return true;
        }

        candidate = default!;
        conceptPattern = default!;
        bindings = default!;
        evidence = default!;
        residueOrTension = default!;
        return false;
    }

    /// <summary>Reads structural pattern evidence created by <see cref="PatternEvidence"/>.</summary>
    public static bool TryReadPatternEvidence(
        Relation evidence,
        out IElement conceptPattern,
        out IElement appliedPattern,
        out IElement candidate,
        out IElement residueOrTension)
    {
        if (TryReadFrameElements(evidence, out var elements) &&
            elements.Length == 4)
        {
            conceptPattern = elements[0];
            appliedPattern = elements[1];
            candidate = elements[2];
            residueOrTension = elements[3];
            return true;
        }

        conceptPattern = default!;
        appliedPattern = default!;
        candidate = default!;
        residueOrTension = default!;
        return false;
    }

    private static Relation TensionSlot()
    {
        var slot = new Placeholder();
        return new Relation(slot, slot.Dual());
    }

    private static IElement ApplyCorrespondence(
        IElement element,
        IReadOnlyList<Relation> bindings,
        out bool changed)
    {
        foreach (var binding in bindings)
        {
            if (Equals(binding.Left, element))
            {
                changed = true;
                return binding.Right;
            }
        }

        if (element is Relation relation)
        {
            var left = ApplyCorrespondence(relation.Left, bindings, out var leftChanged);
            var right = ApplyCorrespondence(relation.Right, bindings, out var rightChanged);
            changed = leftChanged || rightChanged;
            return changed
                ? new Relation(left, right)
                : relation;
        }

        changed = false;
        return element;
    }

    private static bool TryReadFrameElements(Relation frame, out IElement[] elements)
    {
        if (frame?.Right is Carrier holder &&
            frame.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static Carrier RootOrNew(Carrier root) => root ?? new Carrier();
}

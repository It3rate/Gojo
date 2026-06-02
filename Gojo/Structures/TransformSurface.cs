using Gojo.Core;

namespace Gojo.Structures;

/// <summary>
/// Relation-shaped surface for reusable transforms over selected structure.
/// The transform is the reusable instruction; an application records the
/// focus it was bound to, the result, and the patch witness that settled.
/// </summary>
public static class TransformSurface
{
    /// <summary>Creates an ordered port set. Ports are ordinary Gojo elements.</summary>
    public static Relation Ports(Carrier root, params IElement[] ports) =>
        Relation.OrderedGroup(RootOrNew(root), ports ?? []);

    /// <summary>Creates an ordered frame of bindings or operation evidence.</summary>
    public static Relation Frame(Carrier root, params IElement[] elements) =>
        Relation.OrderedGroup(RootOrNew(root), elements ?? []);

    /// <summary>Creates direct input-to-output operation evidence.</summary>
    public static Relation Map(IElement input, IElement output) =>
        new(input ?? UnknownPoint(), output ?? UnknownPoint());

    /// <summary>Creates carry-forward evidence. Structurally this is input -> output.</summary>
    public static Relation Carry(IElement input, IElement output) =>
        Map(input, output);

    /// <summary>Creates introduction evidence. Structurally this is unresolved input -> output.</summary>
    public static Relation Introduce(IElement output) =>
        Map(UnknownPoint(), output);

    /// <summary>Creates removal/anti-participation evidence. Structurally this is input -> unresolved output.</summary>
    public static Relation Remove(IElement input) =>
        Map(input, UnknownPoint());

    /// <summary>Creates split evidence. Structurally this is input -> output ports.</summary>
    public static Relation Split(IElement input, IElement outputPorts) =>
        Map(input, outputPorts ?? Ports(new Carrier()));

    /// <summary>Creates fold evidence. Structurally this is input ports -> output.</summary>
    public static Relation Fold(IElement inputPorts, IElement output) =>
        Map(inputPorts ?? Ports(new Carrier()), output);

    /// <summary>Creates interpolation evidence. Structurally this is input ports -> output.</summary>
    public static Relation Interpolate(IElement first, IElement second, IElement gradeOrBasis, IElement output) =>
        Fold(Ports(new Carrier(), first ?? UnknownPoint(), second ?? UnknownPoint(), gradeOrBasis ?? UnknownPoint()), output);

    /// <summary>
    /// Creates a reusable transform instruction:
    /// input ports, operation structure, output ports, residue policy,
    /// provenance.
    /// </summary>
    public static Relation Create(
        Carrier root,
        IElement inputPorts,
        IElement operation,
        IElement outputPorts,
        IElement residuePolicy,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            inputPorts ?? Ports(new Carrier()),
            operation ?? UnknownPoint(),
            outputPorts ?? Ports(new Carrier()),
            residuePolicy ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>
    /// Creates a patch witness:
    /// source, transform, focus, selected participation, carried complement,
    /// replacement/introduction, correspondence, result, residue, provenance.
    /// </summary>
    public static Relation Patch(
        Carrier root,
        IElement source,
        IElement transform,
        IElement focus,
        IElement participation,
        IElement complement,
        IElement replacement,
        IElement correspondence,
        IElement result,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            source ?? UnknownPoint(),
            transform ?? UnknownPoint(),
            focus ?? UnknownPoint(),
            participation ?? UnknownPoint(),
            complement ?? UnknownPoint(),
            replacement ?? UnknownPoint(),
            correspondence ?? UnknownPoint(),
            result ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>
    /// Creates a transform application:
    /// transform, focus, parameter bindings, result, patch, read witness,
    /// residue, provenance.
    /// </summary>
    public static Relation Application(
        Carrier root,
        Relation transform,
        Relation focus,
        IElement bindings,
        IElement result,
        Relation patch,
        Relation witness,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            transform ?? UnknownTransform(),
            focus ?? UnknownPoint(),
            bindings ?? UnknownPoint(),
            result ?? UnknownPoint(),
            patch ?? UnknownPoint(),
            witness ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>
    /// Applies a reusable transform to a real focus surface. This records the
    /// concrete patch and read witness; it does not mutate the source focus.
    /// </summary>
    public static bool TryCreateApplication(
        Carrier root,
        Relation transform,
        Relation focus,
        IElement bindings,
        IElement result,
        IElement residueOrTension,
        IElement provenance,
        out Relation application)
    {
        if (!TryRead(transform, out _, out var operation, out _, out _, out _) ||
            !FocusSurface.TryRead(
                focus,
                out _,
                out _,
                out _,
                out _,
                out _,
                out var focusCorrespondence,
                out var focusResidue,
                out var focusProvenance))
        {
            application = null!;
            return false;
        }

        var operationBindings = TryDeriveBindings(operation, out var derivedBindings)
            ? derivedBindings
            : UnknownPoint();
        var effectiveBindings = EffectiveEvidence(bindings, operationBindings, focusCorrespondence);
        var effectiveResidue = EffectiveEvidence(residueOrTension, focusResidue);
        var effectiveProvenance = EffectiveEvidence(provenance, focusProvenance);

        if (!TryCreatePatchFromFocus(
                new Carrier(),
                transform,
                focus,
                effectiveBindings,
                result,
                effectiveResidue,
                effectiveProvenance,
                out var patch))
        {
            application = null!;
            return false;
        }

        var witness = ReadWitness.Create(
            new Carrier(),
            focus,
            transform,
            result ?? UnknownPoint(),
            effectiveResidue,
            effectiveProvenance);

        var candidate = Application(
            RootOrNew(root),
            transform,
            focus,
            effectiveBindings,
            result ?? UnknownPoint(),
            patch,
            witness,
            effectiveResidue,
            effectiveProvenance);

        if (TryReadApplication(candidate, out _, out _, out _, out _, out _, out _, out _, out _))
        {
            application = candidate;
            return true;
        }

        application = null!;
        return false;
    }

    /// <summary>Creates a patch by reading selection roles from a focus surface.</summary>
    public static bool TryCreatePatchFromFocus(
        Carrier root,
        Relation transform,
        Relation focus,
        IElement result,
        IElement residueOrTension,
        IElement provenance,
        out Relation patch)
    {
        return TryCreatePatchFromFocus(
            root,
            transform,
            focus,
            null!,
            result,
            residueOrTension,
            provenance,
            out patch);
    }

    /// <summary>Creates a patch by reading selection roles from a focus surface with explicit correspondence evidence.</summary>
    public static bool TryCreatePatchFromFocus(
        Carrier root,
        Relation transform,
        Relation focus,
        IElement correspondenceEvidence,
        IElement result,
        IElement residueOrTension,
        IElement provenance,
        out Relation patch)
    {
        if (!TryRead(transform, out _, out _, out _, out _, out _) ||
            !FocusSurface.TryRead(
                focus,
                out var source,
                out _,
                out var participation,
                out var complement,
                out var replacement,
                out var focusCorrespondence,
                out var focusResidue,
                out var focusProvenance))
        {
            patch = null!;
            return false;
        }

        patch = Patch(
            RootOrNew(root),
            source,
            transform,
            focus,
            participation,
            complement,
            replacement,
            EffectiveEvidence(correspondenceEvidence, focusCorrespondence),
            result ?? UnknownPoint(),
            EffectiveEvidence(residueOrTension, focusResidue),
            EffectiveEvidence(provenance, focusProvenance));
        return true;
    }

    /// <summary>Reads a reusable transform surface.</summary>
    public static bool TryRead(
        Relation transform,
        out IElement inputPorts,
        out IElement operation,
        out IElement outputPorts,
        out IElement residuePolicy,
        out IElement provenance)
    {
        if (TryReadRootElements(transform, out var elements) &&
            elements.Length == 5)
        {
            inputPorts = elements[0];
            operation = elements[1];
            outputPorts = elements[2];
            residuePolicy = elements[3];
            provenance = elements[4];
            return true;
        }

        inputPorts = default!;
        operation = default!;
        outputPorts = default!;
        residuePolicy = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads an ordered port set.</summary>
    public static bool TryReadPorts(IElement ports, out IElement[] portElements)
    {
        if (ports is Relation relation &&
            TryReadRootElements(relation, out var elements))
        {
            portElements = elements;
            return true;
        }

        portElements = [];
        return false;
    }

    /// <summary>Reads direct input-to-output operation evidence.</summary>
    public static bool TryReadMap(Relation operation, out IElement input, out IElement output)
    {
        if (operation is not null)
        {
            input = operation.Left;
            output = operation.Right;
            return true;
        }

        input = default!;
        output = default!;
        return false;
    }

    /// <summary>Reads introduction evidence as unresolved input -> output.</summary>
    public static bool TryReadIntroduce(Relation operation, out IElement output)
    {
        if (TryReadMap(operation, out var input, out var candidateOutput) &&
            IsUnknownPoint(input) &&
            !IsUnknownPoint(candidateOutput))
        {
            output = candidateOutput;
            return true;
        }

        output = default!;
        return false;
    }

    /// <summary>Reads removal evidence as input -> unresolved output.</summary>
    public static bool TryReadRemove(Relation operation, out IElement input)
    {
        if (TryReadMap(operation, out var candidateInput, out var output) &&
            !IsUnknownPoint(candidateInput) &&
            IsUnknownPoint(output))
        {
            input = candidateInput;
            return true;
        }

        input = default!;
        return false;
    }

    /// <summary>Reads split evidence as input -> output ports.</summary>
    public static bool TryReadSplit(Relation operation, out IElement input, out IElement outputPorts)
    {
        if (TryReadMap(operation, out var candidateInput, out var candidateOutputs) &&
            !IsUnknownPoint(candidateInput) &&
            TryReadPorts(candidateOutputs, out var outputs) &&
            outputs.Length > 1)
        {
            input = candidateInput;
            outputPorts = candidateOutputs;
            return true;
        }

        input = default!;
        outputPorts = default!;
        return false;
    }

    /// <summary>Reads fold evidence as input ports -> output.</summary>
    public static bool TryReadFold(Relation operation, out IElement inputPorts, out IElement output)
    {
        if (TryReadMap(operation, out var candidateInputs, out var candidateOutput) &&
            TryReadPorts(candidateInputs, out var inputs) &&
            inputs.Length > 1 &&
            !IsUnknownPoint(candidateOutput))
        {
            inputPorts = candidateInputs;
            output = candidateOutput;
            return true;
        }

        inputPorts = default!;
        output = default!;
        return false;
    }

    /// <summary>Reads interpolation evidence as first, second, grade/basis -> output.</summary>
    public static bool TryReadInterpolate(
        Relation operation,
        out IElement first,
        out IElement second,
        out IElement gradeOrBasis,
        out IElement output)
    {
        if (TryReadFold(operation, out var inputPorts, out var candidateOutput) &&
            TryReadPorts(inputPorts, out var inputs) &&
            inputs.Length == 3)
        {
            first = inputs[0];
            second = inputs[1];
            gradeOrBasis = inputs[2];
            output = candidateOutput;
            return true;
        }

        first = default!;
        second = default!;
        gradeOrBasis = default!;
        output = default!;
        return false;
    }

    /// <summary>
    /// Derives correspondence bindings from operation evidence. A transform can
    /// still carry explicit bindings; this read only exposes what the operation
    /// itself already implies.
    /// </summary>
    public static bool TryDeriveBindings(IElement operation, out Relation bindings)
    {
        var clauses = ReadOperationClauses(operation);
        var derived = new List<Relation>();
        foreach (var clause in clauses)
        {
            if (TryReadMap(clause, out var input, out var output))
            {
                derived.Add(FocusSurface.Bind(input, output));
            }
        }

        if (derived.Count > 0)
        {
            bindings = FocusSurface.BindingFrame(new Carrier(), [.. derived]);
            return true;
        }

        bindings = null!;
        return false;
    }

    /// <summary>Reads a patch created by <see cref="Patch"/>.</summary>
    public static bool TryReadPatch(
        Relation patch,
        out IElement source,
        out Relation transform,
        out Relation focus,
        out IElement participation,
        out IElement complement,
        out IElement replacement,
        out IElement correspondence,
        out IElement result,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(patch, out var elements) &&
            elements.Length == 10 &&
            elements[1] is Relation transformRelation &&
            TryRead(transformRelation, out _, out _, out _, out _, out _) &&
            elements[2] is Relation focusRelation &&
            FocusSurface.TryRead(focusRelation, out _, out _, out _, out _, out _, out _, out _, out _))
        {
            source = elements[0];
            transform = transformRelation;
            focus = focusRelation;
            participation = elements[3];
            complement = elements[4];
            replacement = elements[5];
            correspondence = elements[6];
            result = elements[7];
            residueOrTension = elements[8];
            provenance = elements[9];
            return true;
        }

        source = default!;
        transform = null!;
        focus = null!;
        participation = default!;
        complement = default!;
        replacement = default!;
        correspondence = default!;
        result = default!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads a transform application created by <see cref="Application"/>.</summary>
    public static bool TryReadApplication(
        Relation application,
        out Relation transform,
        out Relation focus,
        out IElement bindings,
        out IElement result,
        out Relation patch,
        out Relation witness,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(application, out var elements) &&
            elements.Length == 8 &&
            elements[0] is Relation transformRelation &&
            TryRead(transformRelation, out _, out _, out _, out _, out _) &&
            elements[1] is Relation focusRelation &&
            FocusSurface.TryRead(focusRelation, out _, out _, out _, out _, out _, out _, out _, out _) &&
            elements[4] is Relation patchRelation &&
            TryReadPatch(
                patchRelation,
                out _,
                out var patchTransform,
                out var patchFocus,
                out _,
                out _,
                out _,
                out _,
                out var patchResult,
                out _,
                out _) &&
            elements[5] is Relation witnessRelation &&
            ReadWitness.TryRead(
                witnessRelation,
                out var witnessedFocus,
                out var witnessedTransform,
                out var witnessedResult,
                out _,
                out _) &&
            Equals(transformRelation, patchTransform) &&
            Equals(focusRelation, patchFocus) &&
            Equals(elements[3], patchResult) &&
            Equals(focusRelation, witnessedFocus) &&
            Equals(transformRelation, witnessedTransform) &&
            Equals(elements[3], witnessedResult))
        {
            transform = transformRelation;
            focus = focusRelation;
            bindings = elements[2];
            result = elements[3];
            patch = patchRelation;
            witness = witnessRelation;
            residueOrTension = elements[6];
            provenance = elements[7];
            return true;
        }

        transform = null!;
        focus = null!;
        bindings = default!;
        result = default!;
        patch = null!;
        witness = null!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    private static bool TryReadRootElements(Relation structure, out IElement[] elements)
    {
        if (structure?.Right is Carrier holder &&
            structure.TryReadOrderedElementsOn(holder, out elements))
        {
            return true;
        }

        elements = [];
        return false;
    }

    private static Relation[] ReadOperationClauses(IElement operation)
    {
        if (operation is not Relation relation)
        {
            return [];
        }

        if (TryReadRootElements(relation, out var elements) &&
            elements.Length > 0)
        {
            return [.. elements.OfType<Relation>()];
        }

        return [relation];
    }

    private static IElement EffectiveEvidence(IElement primary, IElement fallback)
    {
        if (primary is null || IsUnknownPoint(primary))
        {
            return fallback ?? UnknownPoint();
        }

        return primary;
    }

    private static IElement EffectiveEvidence(params IElement[] evidence)
    {
        foreach (var element in evidence ?? [])
        {
            if (element is not null && !IsUnknownPoint(element))
            {
                return element;
            }
        }

        return UnknownPoint();
    }

    private static Relation UnknownTransform() =>
        Create(new Carrier(), Ports(new Carrier()), UnknownPoint(), Ports(new Carrier()), UnknownPoint(), UnknownPoint());

    private static Relation UnknownPoint() =>
        Relation.Point(Carrier.Unknown, 0);

    private static bool IsUnknownPoint(IElement element)
    {
        return element is Relation relation &&
            relation.Left is CarrierLocation location &&
            relation.Right is Carrier carrier &&
            carrier.IsUnknown &&
            location.Carrier.IsUnknown &&
            location.Ticks == 0;
    }

    private static Carrier RootOrNew(Carrier carrier) =>
        carrier ?? new Carrier();
}

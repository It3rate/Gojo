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
    /// Creates stale patch evidence:
    /// previous application, previous patch, previous focus, new focus,
    /// residue/tension, provenance.
    /// </summary>
    public static Relation StalePatch(
        Carrier root,
        Relation previousApplication,
        Relation previousPatch,
        Relation previousFocus,
        Relation newFocus,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            previousApplication ?? UnknownPoint(),
            previousPatch ?? UnknownPoint(),
            previousFocus ?? UnknownPoint(),
            newFocus ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>
    /// Creates replay evidence:
    /// previous application, new focus, new application, stale patch evidence,
    /// residue/tension, provenance.
    /// </summary>
    public static Relation Replay(
        Carrier root,
        Relation previousApplication,
        Relation newFocus,
        Relation newApplication,
        IElement stalePatchOrResidue,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            previousApplication ?? UnknownPoint(),
            newFocus ?? UnknownPoint(),
            newApplication ?? UnknownPoint(),
            stalePatchOrResidue ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>Creates an ordered frame of transform applications.</summary>
    public static Relation ApplicationFrame(Carrier root, params Relation[] applications) =>
        Relation.OrderedGroup(RootOrNew(root), applications ?? []);

    /// <summary>Creates an ordered frame of replay records.</summary>
    public static Relation ReplayFrame(Carrier root, params Relation[] replays) =>
        Relation.OrderedGroup(RootOrNew(root), replays ?? []);

    /// <summary>
    /// Creates partial replay evidence:
    /// previous applications, carried prefix, replay frame, current
    /// applications, residue/tension, provenance.
    /// </summary>
    public static Relation PartialReplay(
        Carrier root,
        IElement previousApplications,
        IElement carriedPrefix,
        IElement replayFrame,
        IElement currentApplications,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            previousApplications ?? ApplicationFrame(new Carrier()),
            carriedPrefix ?? ApplicationFrame(new Carrier()),
            replayFrame ?? ReplayFrame(new Carrier()),
            currentApplications ?? ApplicationFrame(new Carrier()),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>
    /// Creates focus-change evidence:
    /// previous focus, new focus, correspondence, residue/tension, provenance.
    /// </summary>
    public static Relation FocusChange(
        Carrier root,
        Relation previousFocus,
        Relation newFocus,
        IElement correspondence,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            previousFocus ?? UnknownPoint(),
            newFocus ?? UnknownPoint(),
            correspondence ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>
    /// Creates affected-slice evidence:
    /// previous application frame, focus change, carried prefix, affected
    /// application suffix, residue/tension, provenance.
    /// </summary>
    public static Relation AffectedSlice(
        Carrier root,
        IElement previousApplications,
        Relation focusChange,
        IElement carriedPrefix,
        IElement affectedApplications,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            previousApplications ?? ApplicationFrame(new Carrier()),
            focusChange ?? UnknownPoint(),
            carriedPrefix ?? ApplicationFrame(new Carrier()),
            affectedApplications ?? ApplicationFrame(new Carrier()),
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

    /// <summary>
    /// Replays a previous transform application against a changed focus. The
    /// transform is reused; the resulting application and stale patch evidence
    /// remain inspectable.
    /// </summary>
    public static bool TryReplayApplication(
        Carrier root,
        Relation previousApplication,
        Relation newFocus,
        IElement bindings,
        IElement result,
        IElement residueOrTension,
        IElement provenance,
        out Relation replay)
    {
        if (!TryReadApplication(
                previousApplication,
                out var transform,
                out _,
                out _,
                out _,
                out _,
                out _,
                out var previousResidue,
                out var previousProvenance))
        {
            replay = null!;
            return false;
        }

        var stale = TryCreateStalePatch(
            new Carrier(),
            previousApplication,
            newFocus,
            EffectiveEvidence(residueOrTension, previousResidue),
            EffectiveEvidence(provenance, previousProvenance),
            out var stalePatch)
            ? stalePatch
            : UnknownPoint();
        var replayProvenance = Frame(
            new Carrier(),
            previousApplication,
            stale,
            EffectiveEvidence(provenance, previousProvenance));

        if (!TryCreateApplication(
                new Carrier(),
                transform,
                newFocus,
                bindings,
                result,
                residueOrTension,
                replayProvenance,
                out var newApplication))
        {
            replay = null!;
            return false;
        }

        var candidate = Replay(
            RootOrNew(root),
            previousApplication,
            newFocus,
            newApplication,
            stale,
            EffectiveEvidence(residueOrTension, previousResidue),
            replayProvenance);

        if (TryReadReplay(candidate, out _, out _, out _, out _, out _, out _))
        {
            replay = candidate;
            return true;
        }

        replay = null!;
        return false;
    }

    /// <summary>Creates stale patch evidence when a previous patch is read against a different focus.</summary>
    public static bool TryCreateStalePatch(
        Carrier root,
        Relation previousApplication,
        Relation newFocus,
        IElement residueOrTension,
        IElement provenance,
        out Relation stalePatch)
    {
        if (!TryReadApplication(
                previousApplication,
                out _,
                out var previousFocus,
                out _,
                out _,
                out var previousPatch,
                out _,
                out var applicationResidue,
                out var applicationProvenance) ||
            !FocusSurface.TryRead(newFocus, out _, out _, out _, out _, out _, out _, out _, out _) ||
            Equals(previousFocus, newFocus))
        {
            stalePatch = null!;
            return false;
        }

        stalePatch = StalePatch(
            RootOrNew(root),
            previousApplication,
            previousPatch,
            previousFocus,
            newFocus,
            EffectiveEvidence(residueOrTension, applicationResidue),
            EffectiveEvidence(provenance, applicationProvenance));
        return true;
    }

    /// <summary>Creates focus-change evidence only when both sides are focus surfaces and differ.</summary>
    public static bool TryCreateFocusChange(
        Carrier root,
        Relation previousFocus,
        Relation newFocus,
        IElement correspondence,
        IElement residueOrTension,
        IElement provenance,
        out Relation focusChange)
    {
        if (!FocusSurface.TryRead(previousFocus, out _, out _, out _, out _, out _, out _, out _, out _) ||
            !FocusSurface.TryRead(newFocus, out _, out _, out _, out _, out _, out _, out _, out _) ||
            Equals(previousFocus, newFocus))
        {
            focusChange = null!;
            return false;
        }

        focusChange = FocusChange(
            RootOrNew(root),
            previousFocus,
            newFocus,
            correspondence ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
        return true;
    }

    /// <summary>
    /// Discovers the affected suffix of an application frame from a focus
    /// change. The first application whose focus is the previous focus starts
    /// the affected slice.
    /// </summary>
    public static bool TryCreateAffectedSlice(
        Carrier root,
        IElement previousApplications,
        Relation focusChange,
        IElement residueOrTension,
        IElement provenance,
        out Relation affectedSlice)
    {
        if (!TryReadApplicationFrame(previousApplications, out var applications) ||
            !TryReadFocusChange(
                focusChange,
                out var previousFocus,
                out _,
                out _,
                out var focusResidue,
                out var focusProvenance) ||
            !TryFindApplicationFocusIndex(applications, previousFocus, out var affectedIndex))
        {
            affectedSlice = null!;
            return false;
        }

        var carried = ApplicationFrame(new Carrier(), [.. applications.Take(affectedIndex)]);
        var affected = ApplicationFrame(new Carrier(), [.. applications.Skip(affectedIndex)]);
        var candidate = AffectedSlice(
            RootOrNew(root),
            previousApplications,
            focusChange,
            carried,
            affected,
            EffectiveEvidence(residueOrTension, focusResidue),
            EffectiveEvidence(provenance, focusProvenance));

        if (TryReadAffectedSlice(candidate, out _, out _, out _, out _, out _, out _))
        {
            affectedSlice = candidate;
            return true;
        }

        affectedSlice = null!;
        return false;
    }

    /// <summary>
    /// Creates partial replay from an affected slice. The carried prefix comes
    /// from the affected-slice read, and replay records must replace affected
    /// applications in order.
    /// </summary>
    public static bool TryCreatePartialReplayFromAffectedSlice(
        Carrier root,
        Relation affectedSlice,
        IReadOnlyList<Relation> replays,
        IElement residueOrTension,
        IElement provenance,
        out Relation partialReplay)
    {
        if (!TryReadAffectedSlice(
                affectedSlice,
                out var previousApplications,
                out _,
                out var carriedPrefix,
                out var affectedApplications,
                out var affectedResidue,
                out var affectedProvenance) ||
            !TryReadApplicationFrame(affectedApplications, out var affected) ||
            replays is null ||
            replays.Count == 0 ||
            replays.Count > affected.Length)
        {
            partialReplay = null!;
            return false;
        }

        for (var index = 0; index < replays.Count; index++)
        {
            if (!TryReadReplay(
                    replays[index],
                    out var previousApplication,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _) ||
                !Equals(affected[index], previousApplication))
            {
                partialReplay = null!;
                return false;
            }
        }

        return TryCreatePartialReplay(
            root,
            previousApplications,
            carriedPrefix,
            replays,
            EffectiveEvidence(residueOrTension, affectedResidue),
            EffectiveEvidence(provenance, affectedProvenance),
            out partialReplay);
    }

    /// <summary>
    /// Creates a partial replay run. Carried prefix applications must be the
    /// leading applications from the previous frame; replay records then
    /// replace the following previous applications in order.
    /// </summary>
    public static bool TryCreatePartialReplay(
        Carrier root,
        IElement previousApplications,
        IElement carriedPrefix,
        IReadOnlyList<Relation> replays,
        IElement residueOrTension,
        IElement provenance,
        out Relation partialReplay)
    {
        if (!TryReadApplicationFrame(previousApplications, out var previous) ||
            !TryReadApplicationFrame(carriedPrefix, out var carried) ||
            replays is null ||
            replays.Count == 0 ||
            carried.Length + replays.Count > previous.Length ||
            !IsPrefix(previous, carried))
        {
            partialReplay = null!;
            return false;
        }

        var cleanReplays = new List<Relation>();
        var current = new List<Relation>(carried);
        for (var index = 0; index < replays.Count; index++)
        {
            var replay = replays[index];
            if (!TryReadReplay(
                    replay,
                    out var previousApplication,
                    out _,
                    out var newApplication,
                    out _,
                    out _,
                    out _) ||
                !Equals(previous[carried.Length + index], previousApplication))
            {
                partialReplay = null!;
                return false;
            }

            cleanReplays.Add(replay);
            current.Add(newApplication);
        }

        var replayFrame = ReplayFrame(new Carrier(), [.. cleanReplays]);
        var currentFrame = ApplicationFrame(new Carrier(), [.. current]);
        var candidate = PartialReplay(
            RootOrNew(root),
            previousApplications,
            carriedPrefix,
            replayFrame,
            currentFrame,
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());

        if (TryReadPartialReplay(candidate, out _, out _, out _, out _, out _, out _))
        {
            partialReplay = candidate;
            return true;
        }

        partialReplay = null!;
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

    /// <summary>Reads an ordered frame of transform applications.</summary>
    public static bool TryReadApplicationFrame(IElement frame, out Relation[] applications)
    {
        if (frame is Relation relation &&
            TryReadRootElements(relation, out var elements))
        {
            var read = new List<Relation>();
            foreach (var element in elements)
            {
                if (element is not Relation application ||
                    !TryReadApplication(application, out _, out _, out _, out _, out _, out _, out _, out _))
                {
                    applications = [];
                    return false;
                }

                read.Add(application);
            }

            applications = [.. read];
            return true;
        }

        applications = [];
        return false;
    }

    /// <summary>Reads an ordered frame of replay records.</summary>
    public static bool TryReadReplayFrame(IElement frame, out Relation[] replays)
    {
        if (frame is Relation relation &&
            TryReadRootElements(relation, out var elements))
        {
            var read = new List<Relation>();
            foreach (var element in elements)
            {
                if (element is not Relation replay ||
                    !TryReadReplay(replay, out _, out _, out _, out _, out _, out _))
                {
                    replays = [];
                    return false;
                }

                read.Add(replay);
            }

            replays = [.. read];
            return true;
        }

        replays = [];
        return false;
    }

    /// <summary>Reads focus-change evidence created by <see cref="FocusChange"/>.</summary>
    public static bool TryReadFocusChange(
        Relation focusChange,
        out Relation previousFocus,
        out Relation newFocus,
        out IElement correspondence,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(focusChange, out var elements) &&
            elements.Length == 5 &&
            elements[0] is Relation previousFocusRelation &&
            elements[1] is Relation newFocusRelation &&
            FocusSurface.TryRead(previousFocusRelation, out _, out _, out _, out _, out _, out _, out _, out _) &&
            FocusSurface.TryRead(newFocusRelation, out _, out _, out _, out _, out _, out _, out _, out _) &&
            !Equals(previousFocusRelation, newFocusRelation))
        {
            previousFocus = previousFocusRelation;
            newFocus = newFocusRelation;
            correspondence = elements[2];
            residueOrTension = elements[3];
            provenance = elements[4];
            return true;
        }

        previousFocus = null!;
        newFocus = null!;
        correspondence = default!;
        residueOrTension = default!;
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

    /// <summary>Reads stale patch evidence created by <see cref="StalePatch"/>.</summary>
    public static bool TryReadStalePatch(
        Relation stalePatch,
        out Relation previousApplication,
        out Relation previousPatch,
        out Relation previousFocus,
        out Relation newFocus,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(stalePatch, out var elements) &&
            elements.Length == 6 &&
            elements[0] is Relation applicationRelation &&
            TryReadApplication(
                applicationRelation,
                out _,
                out var applicationFocus,
                out _,
                out _,
                out var applicationPatch,
                out _,
                out _,
                out _) &&
            elements[1] is Relation patchRelation &&
            TryReadPatch(patchRelation, out _, out _, out _, out _, out _, out _, out _, out _, out _, out _) &&
            elements[2] is Relation previousFocusRelation &&
            FocusSurface.TryRead(previousFocusRelation, out _, out _, out _, out _, out _, out _, out _, out _) &&
            elements[3] is Relation newFocusRelation &&
            FocusSurface.TryRead(newFocusRelation, out _, out _, out _, out _, out _, out _, out _, out _) &&
            Equals(applicationPatch, patchRelation) &&
            Equals(applicationFocus, previousFocusRelation))
        {
            previousApplication = applicationRelation;
            previousPatch = patchRelation;
            previousFocus = previousFocusRelation;
            newFocus = newFocusRelation;
            residueOrTension = elements[4];
            provenance = elements[5];
            return true;
        }

        previousApplication = null!;
        previousPatch = null!;
        previousFocus = null!;
        newFocus = null!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads replay evidence created by <see cref="Replay"/>.</summary>
    public static bool TryReadReplay(
        Relation replay,
        out Relation previousApplication,
        out Relation newFocus,
        out Relation newApplication,
        out IElement stalePatchOrResidue,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(replay, out var elements) &&
            elements.Length == 6 &&
            elements[0] is Relation previousApplicationRelation &&
            TryReadApplication(
                previousApplicationRelation,
                out var previousTransform,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _) &&
            elements[1] is Relation newFocusRelation &&
            FocusSurface.TryRead(newFocusRelation, out _, out _, out _, out _, out _, out _, out _, out _) &&
            elements[2] is Relation newApplicationRelation &&
            TryReadApplication(
                newApplicationRelation,
                out var replayTransform,
                out var replayFocus,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _) &&
            Equals(previousTransform, replayTransform) &&
            Equals(newFocusRelation, replayFocus) &&
            IsReadableStaleOrUnknown(elements[3]))
        {
            previousApplication = previousApplicationRelation;
            newFocus = newFocusRelation;
            newApplication = newApplicationRelation;
            stalePatchOrResidue = elements[3];
            residueOrTension = elements[4];
            provenance = elements[5];
            return true;
        }

        previousApplication = null!;
        newFocus = null!;
        newApplication = null!;
        stalePatchOrResidue = default!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads partial replay evidence created by <see cref="PartialReplay"/>.</summary>
    public static bool TryReadPartialReplay(
        Relation partialReplay,
        out IElement previousApplications,
        out IElement carriedPrefix,
        out IElement replayFrame,
        out IElement currentApplications,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(partialReplay, out var elements) &&
            elements.Length == 6 &&
            TryReadApplicationFrame(elements[0], out var previous) &&
            TryReadApplicationFrame(elements[1], out var carried) &&
            TryReadReplayFrame(elements[2], out var replays) &&
            TryReadApplicationFrame(elements[3], out var current) &&
            replays.Length > 0 &&
            carried.Length + replays.Length <= previous.Length &&
            current.Length == carried.Length + replays.Length &&
            IsPrefix(previous, carried) &&
            CurrentMatchesReplay(carried, replays, current) &&
            ReplaysMatchPrevious(previous, carried.Length, replays))
        {
            previousApplications = elements[0];
            carriedPrefix = elements[1];
            replayFrame = elements[2];
            currentApplications = elements[3];
            residueOrTension = elements[4];
            provenance = elements[5];
            return true;
        }

        previousApplications = default!;
        carriedPrefix = default!;
        replayFrame = default!;
        currentApplications = default!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads affected-slice evidence created by <see cref="AffectedSlice"/>.</summary>
    public static bool TryReadAffectedSlice(
        Relation affectedSlice,
        out IElement previousApplications,
        out Relation focusChange,
        out IElement carriedPrefix,
        out IElement affectedApplications,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(affectedSlice, out var elements) &&
            elements.Length == 6 &&
            TryReadApplicationFrame(elements[0], out var previous) &&
            elements[1] is Relation focusChangeRelation &&
            TryReadFocusChange(
                focusChangeRelation,
                out var previousFocus,
                out _,
                out _,
                out _,
                out _) &&
            TryReadApplicationFrame(elements[2], out var carried) &&
            TryReadApplicationFrame(elements[3], out var affected) &&
            affected.Length > 0 &&
            carried.Length + affected.Length == previous.Length &&
            IsPrefix(previous, carried) &&
            IsSuffixFrom(previous, carried.Length, affected) &&
            TryReadApplication(
                affected[0],
                out _,
                out var affectedFocus,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _) &&
            Equals(previousFocus, affectedFocus))
        {
            previousApplications = elements[0];
            focusChange = focusChangeRelation;
            carriedPrefix = elements[2];
            affectedApplications = elements[3];
            residueOrTension = elements[4];
            provenance = elements[5];
            return true;
        }

        previousApplications = default!;
        focusChange = null!;
        carriedPrefix = default!;
        affectedApplications = default!;
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

    private static bool IsReadableStaleOrUnknown(IElement element) =>
        IsUnknownPoint(element) ||
        element is Relation relation &&
        TryReadStalePatch(relation, out _, out _, out _, out _, out _, out _);

    private static bool IsPrefix(IReadOnlyList<Relation> full, IReadOnlyList<Relation> prefix)
    {
        if (prefix.Count > full.Count)
        {
            return false;
        }

        for (var index = 0; index < prefix.Count; index++)
        {
            if (!Equals(full[index], prefix[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ReplaysMatchPrevious(
        IReadOnlyList<Relation> previous,
        int startIndex,
        IReadOnlyList<Relation> replays)
    {
        if (startIndex + replays.Count > previous.Count)
        {
            return false;
        }

        for (var index = 0; index < replays.Count; index++)
        {
            if (!TryReadReplay(
                    replays[index],
                    out var previousApplication,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _) ||
                !Equals(previous[startIndex + index], previousApplication))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryFindApplicationFocusIndex(
        IReadOnlyList<Relation> applications,
        Relation focus,
        out int index)
    {
        for (var candidateIndex = 0; candidateIndex < applications.Count; candidateIndex++)
        {
            if (TryReadApplication(
                    applications[candidateIndex],
                    out _,
                    out var applicationFocus,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _) &&
                Equals(applicationFocus, focus))
            {
                index = candidateIndex;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private static bool IsSuffixFrom(
        IReadOnlyList<Relation> full,
        int startIndex,
        IReadOnlyList<Relation> suffix)
    {
        if (startIndex < 0 ||
            startIndex + suffix.Count != full.Count)
        {
            return false;
        }

        for (var index = 0; index < suffix.Count; index++)
        {
            if (!Equals(full[startIndex + index], suffix[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CurrentMatchesReplay(
        IReadOnlyList<Relation> carried,
        IReadOnlyList<Relation> replays,
        IReadOnlyList<Relation> current)
    {
        if (current.Count != carried.Count + replays.Count)
        {
            return false;
        }

        for (var index = 0; index < carried.Count; index++)
        {
            if (!Equals(carried[index], current[index]))
            {
                return false;
            }
        }

        for (var index = 0; index < replays.Count; index++)
        {
            if (!TryReadReplay(
                    replays[index],
                    out _,
                    out _,
                    out var newApplication,
                    out _,
                    out _,
                    out _) ||
                !Equals(newApplication, current[carried.Count + index]))
            {
                return false;
            }
        }

        return true;
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

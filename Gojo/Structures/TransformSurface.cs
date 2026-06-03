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

    /// <summary>Creates an ordered frame of application dependencies.</summary>
    public static Relation ApplicationDependencyFrame(Carrier root, params Relation[] dependencies) =>
        Relation.OrderedGroup(RootOrNew(root), dependencies ?? []);

    /// <summary>Creates an ordered frame of stale dependency witnesses.</summary>
    public static Relation StaleDependencyFrame(Carrier root, params Relation[] staleDependencies) =>
        Relation.OrderedGroup(RootOrNew(root), staleDependencies ?? []);

    /// <summary>Creates an ordered frame of replay records.</summary>
    public static Relation ReplayFrame(Carrier root, params Relation[] replays) =>
        Relation.OrderedGroup(RootOrNew(root), replays ?? []);

    /// <summary>
    /// Creates partial replay evidence:
    /// previous applications, carried applications, replay frame, current
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
    /// Creates dependency-aware affected-slice evidence:
    /// previous application frame, focus change, dependency frame, carried
    /// applications, affected applications, residue/tension, provenance.
    /// </summary>
    public static Relation DependencyAffectedSlice(
        Carrier root,
        IElement previousApplications,
        Relation focusChange,
        IElement dependencyFrame,
        IElement carriedApplications,
        IElement affectedApplications,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            previousApplications ?? ApplicationFrame(new Carrier()),
            focusChange ?? UnknownPoint(),
            dependencyFrame ?? ApplicationDependencyFrame(new Carrier()),
            carriedApplications ?? ApplicationFrame(new Carrier()),
            affectedApplications ?? ApplicationFrame(new Carrier()),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>
    /// Creates application dependency evidence:
    /// producer application, consumer application, dependency bindings,
    /// residue/tension, provenance.
    /// </summary>
    public static Relation ApplicationDependency(
        Carrier root,
        Relation producerApplication,
        Relation consumerApplication,
        IElement dependencyEvidence,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            producerApplication ?? UnknownPoint(),
            consumerApplication ?? UnknownPoint(),
            dependencyEvidence ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>
    /// Creates stale dependency evidence:
    /// dependency, producer replay, stale consumer application, residue/tension,
    /// provenance.
    /// </summary>
    public static Relation StaleDependency(
        Carrier root,
        Relation dependency,
        Relation producerReplay,
        Relation staleConsumerApplication,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            dependency ?? UnknownPoint(),
            producerReplay ?? UnknownPoint(),
            staleConsumerApplication ?? UnknownPoint(),
            residueOrTension ?? UnknownPoint(),
            provenance ?? UnknownPoint());
    }

    /// <summary>
    /// Creates operation-output evidence:
    /// application, operation, bound input, operation read, output target surface,
    /// result, residue/tension, provenance.
    /// </summary>
    public static Relation OperationOutput(
        Carrier root,
        Relation application,
        IElement operation,
        IElement inputEvidence,
        IElement operationRead,
        IElement outputTarget,
        IElement result,
        IElement residueOrTension,
        IElement provenance)
    {
        return Relation.OrderedGroup(
            RootOrNew(root),
            application ?? UnknownPoint(),
            operation ?? UnknownPoint(),
            inputEvidence ?? UnknownPoint(),
            operationRead ?? UnknownPoint(),
            outputTarget ?? UnknownPoint(),
            result ?? UnknownPoint(),
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
    /// Discovers direct dependency evidence when a consumer application
    /// references output structure from a producer application.
    /// </summary>
    public static bool TryCreateApplicationDependency(
        Carrier root,
        Relation producerApplication,
        Relation consumerApplication,
        IElement residueOrTension,
        IElement provenance,
        out Relation dependency)
    {
        if (!TryReadApplication(producerApplication, out _, out _, out _, out _, out _, out _, out var producerResidue, out var producerProvenance) ||
            !TryReadApplication(consumerApplication, out _, out _, out _, out _, out _, out _, out var consumerResidue, out var consumerProvenance) ||
            Equals(producerApplication, consumerApplication) ||
            !TryCreateDependencyEvidence(producerApplication, consumerApplication, out var evidence))
        {
            dependency = null!;
            return false;
        }

        var candidate = ApplicationDependency(
            RootOrNew(root),
            producerApplication,
            consumerApplication,
            evidence,
            EffectiveEvidence(residueOrTension, consumerResidue, producerResidue),
            EffectiveEvidence(provenance, consumerProvenance, producerProvenance));

        if (TryReadApplicationDependency(candidate, out _, out _, out _, out _, out _))
        {
            dependency = candidate;
            return true;
        }

        dependency = null!;
        return false;
    }

    /// <summary>
    /// Creates stale dependency evidence when a producer has been replayed but
    /// a dependent consumer is still represented by its previous application.
    /// </summary>
    public static bool TryCreateStaleDependency(
        Carrier root,
        Relation dependency,
        Relation producerReplay,
        Relation staleConsumerApplication,
        IElement residueOrTension,
        IElement provenance,
        out Relation staleDependency)
    {
        if (!TryReadApplicationDependency(
                dependency,
                out var producerApplication,
                out var consumerApplication,
                out _,
                out var dependencyResidue,
                out var dependencyProvenance) ||
            !TryReadReplay(
                producerReplay,
                out var replayedPreviousApplication,
                out _,
                out _,
                out _,
                out var replayResidue,
                out var replayProvenance) ||
            !Equals(producerApplication, replayedPreviousApplication) ||
            !Equals(consumerApplication, staleConsumerApplication))
        {
            staleDependency = null!;
            return false;
        }

        var candidate = StaleDependency(
            RootOrNew(root),
            dependency,
            producerReplay,
            staleConsumerApplication,
            EffectiveEvidence(residueOrTension, replayResidue, dependencyResidue),
            EffectiveEvidence(provenance, replayProvenance, dependencyProvenance));

        if (TryReadStaleDependency(candidate, out _, out _, out _, out _, out _))
        {
            staleDependency = candidate;
            return true;
        }

        staleDependency = null!;
        return false;
    }

    /// <summary>
    /// Reads a transform application's executable operation through its
    /// bindings and records the settled output target surface.
    /// </summary>
    public static bool TryCreateOperationOutputRead(
        Carrier root,
        Relation application,
        IElement residueOrTension,
        IElement provenance,
        out Relation operationOutput)
    {
        if (!TryReadApplication(
                application,
                out var transform,
                out _,
                out var bindings,
                out var result,
                out _,
                out _,
                out var applicationResidue,
                out var applicationProvenance) ||
            !TryRead(transform, out _, out var operation, out var outputPorts, out _, out _) ||
            operation is not Relation operationRelation ||
            !TryBuildOperationInput(operationRelation.Left, bindings, out var inputEvidence) ||
            !TryReadOperationReader(outputPorts, operationRelation.Right, result, out var reader) ||
            !operationRelation.TryReadOperationFrom(inputEvidence, reader, out var operationRead) ||
            !TryReadOperationOutputTargets(operationRead, out var outputTarget))
        {
            operationOutput = null!;
            return false;
        }

        var candidate = OperationOutput(
            RootOrNew(root),
            application,
            operation,
            inputEvidence,
            operationRead,
            outputTarget,
            result,
            EffectiveEvidence(residueOrTension, applicationResidue),
            EffectiveEvidence(provenance, applicationProvenance));

        if (TryReadOperationOutput(candidate, out _, out _, out _, out _, out _, out _, out _, out _))
        {
            operationOutput = candidate;
            return true;
        }

        operationOutput = null!;
        return false;
    }

    /// <summary>
    /// Discovers affected applications from a focus change and dependency
    /// frame. The changed application and transitive consumers are affected;
    /// other applications remain carried.
    /// </summary>
    public static bool TryCreateDependencyAffectedSlice(
        Carrier root,
        IElement previousApplications,
        IElement dependencyFrame,
        Relation focusChange,
        IElement residueOrTension,
        IElement provenance,
        out Relation affectedSlice)
    {
        if (!TryReadApplicationFrame(previousApplications, out var applications) ||
            !TryReadApplicationDependencyFrame(dependencyFrame, out var dependencies) ||
            !TryReadFocusChange(
                focusChange,
                out var previousFocus,
                out _,
                out _,
                out var focusResidue,
                out var focusProvenance) ||
            !TryFindApplicationFocusIndex(applications, previousFocus, out var affectedIndex) ||
            !TryComputeDependencyAffectedApplications(
                applications,
                applications[affectedIndex],
                dependencies,
                out var carried,
                out var affected))
        {
            affectedSlice = null!;
            return false;
        }

        var candidate = DependencyAffectedSlice(
            RootOrNew(root),
            previousApplications,
            focusChange,
            dependencyFrame,
            ApplicationFrame(new Carrier(), carried),
            ApplicationFrame(new Carrier(), affected),
            EffectiveEvidence(residueOrTension, focusResidue),
            EffectiveEvidence(provenance, focusProvenance));

        if (TryReadDependencyAffectedSlice(candidate, out _, out _, out _, out _, out _, out _, out _))
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
    /// Creates partial replay from a dependency-aware affected slice. Replay
    /// records must replace applications in the affected frame; carried
    /// independent applications remain in the current frame.
    /// </summary>
    public static bool TryCreatePartialReplayFromDependencyAffectedSlice(
        Carrier root,
        Relation affectedSlice,
        IReadOnlyList<Relation> replays,
        IElement residueOrTension,
        IElement provenance,
        out Relation partialReplay)
    {
        if (!TryReadDependencyAffectedSlice(
                affectedSlice,
                out var previousApplications,
                out _,
                out var dependencyFrame,
                out var carriedApplications,
                out var affectedApplications,
                out var affectedResidue,
                out var affectedProvenance) ||
            !TryReadApplicationDependencyFrame(dependencyFrame, out var dependencies) ||
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

        var replayResidue = TryCreateStaleDependencyResidue(
            dependencies,
            affected,
            replays,
            residueOrTension,
            provenance,
            out var staleDependencyResidue)
            ? CombineEvidence(residueOrTension, staleDependencyResidue, affectedResidue)
            : EffectiveEvidence(residueOrTension, affectedResidue);

        return TryCreatePartialReplay(
            root,
            previousApplications,
            carriedApplications,
            replays,
            replayResidue,
            EffectiveEvidence(provenance, affectedProvenance),
            out partialReplay);
    }

    /// <summary>
    /// Resumes a partial replay from stale dependency residue. Existing replay
    /// records are preserved; new replay records must replace stale consumers.
    /// </summary>
    public static bool TryCreatePartialReplayFromStaleDependencyResidue(
        Carrier root,
        Relation partialReplay,
        IReadOnlyList<Relation> replays,
        IElement residueOrTension,
        IElement provenance,
        out Relation resumedReplay)
    {
        if (!TryReadPartialReplay(
                partialReplay,
                out var previousApplications,
                out var carriedApplications,
                out var replayFrame,
                out _,
                out var partialResidue,
                out var partialProvenance) ||
            !TryReadReplayFrame(replayFrame, out var existingReplays) ||
            !TryReadStaleDependencyResidue(partialResidue, out var staleDependencies) ||
            replays is null ||
            replays.Count == 0 ||
            !TryReadRemainingStaleDependencies(staleDependencies, replays, out var remainingStaleDependencies))
        {
            resumedReplay = null!;
            return false;
        }

        var remainingStaleResidue = StaleDependencyResidueOf(remainingStaleDependencies);
        return TryCreatePartialReplay(
            root,
            previousApplications,
            carriedApplications,
            [.. existingReplays, .. replays],
            CombineEvidence(residueOrTension, remainingStaleResidue),
            EffectiveEvidence(provenance, partialProvenance),
            out resumedReplay);
    }

    /// <summary>
    /// Creates a partial replay run. Carried applications remain unchanged;
    /// replay records replace their previous applications in frame order.
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
            !TryBuildCurrentApplications(previous, carried, replays, out var current))
        {
            partialReplay = null!;
            return false;
        }

        var replayFrame = ReplayFrame(new Carrier(), [.. replays]);
        var currentFrame = ApplicationFrame(new Carrier(), current);
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

    /// <summary>Reads an ordered frame of application dependencies.</summary>
    public static bool TryReadApplicationDependencyFrame(IElement frame, out Relation[] dependencies)
    {
        if (frame is Relation relation &&
            TryReadRootElements(relation, out var elements))
        {
            var read = new List<Relation>();
            foreach (var element in elements)
            {
                if (element is not Relation dependency ||
                    !TryReadApplicationDependency(dependency, out _, out _, out _, out _, out _))
                {
                    dependencies = [];
                    return false;
                }

                read.Add(dependency);
            }

            dependencies = [.. read];
            return true;
        }

        dependencies = [];
        return false;
    }

    /// <summary>Reads an ordered frame of stale dependency witnesses.</summary>
    public static bool TryReadStaleDependencyFrame(IElement frame, out Relation[] staleDependencies)
    {
        if (frame is Relation relation &&
            TryReadRootElements(relation, out var elements))
        {
            var read = new List<Relation>();
            foreach (var element in elements)
            {
                if (element is not Relation staleDependency ||
                    !TryReadStaleDependency(staleDependency, out _, out _, out _, out _, out _))
                {
                    staleDependencies = [];
                    return false;
                }

                read.Add(staleDependency);
            }

            staleDependencies = [.. read];
            return true;
        }

        staleDependencies = [];
        return false;
    }

    /// <summary>Reads stale dependency witnesses from residue evidence.</summary>
    public static bool TryReadStaleDependencyResidue(IElement residue, out Relation[] staleDependencies)
    {
        var read = new List<Relation>();
        CollectStaleDependencyResidue(residue, read);
        staleDependencies = [.. read];
        return staleDependencies.Length > 0;
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
            TryBuildCurrentApplications(previous, carried, replays, out var expectedCurrent) &&
            ApplicationsMatch(expectedCurrent, current))
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

    /// <summary>Reads dependency-aware affected-slice evidence created by <see cref="DependencyAffectedSlice"/>.</summary>
    public static bool TryReadDependencyAffectedSlice(
        Relation affectedSlice,
        out IElement previousApplications,
        out Relation focusChange,
        out IElement dependencyFrame,
        out IElement carriedApplications,
        out IElement affectedApplications,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(affectedSlice, out var elements) &&
            elements.Length == 7 &&
            TryReadApplicationFrame(elements[0], out var previous) &&
            elements[1] is Relation focusChangeRelation &&
            TryReadFocusChange(
                focusChangeRelation,
                out var previousFocus,
                out _,
                out _,
                out _,
                out _) &&
            TryReadApplicationDependencyFrame(elements[2], out var dependencies) &&
            TryReadApplicationFrame(elements[3], out var carried) &&
            TryReadApplicationFrame(elements[4], out var affected) &&
            TryFindApplicationFocusIndex(previous, previousFocus, out var affectedIndex) &&
            TryComputeDependencyAffectedApplications(
                previous,
                previous[affectedIndex],
                dependencies,
                out var expectedCarried,
                out var expectedAffected) &&
            ApplicationsMatch(carried, expectedCarried) &&
            ApplicationsMatch(affected, expectedAffected))
        {
            previousApplications = elements[0];
            focusChange = focusChangeRelation;
            dependencyFrame = elements[2];
            carriedApplications = elements[3];
            affectedApplications = elements[4];
            residueOrTension = elements[5];
            provenance = elements[6];
            return true;
        }

        previousApplications = default!;
        focusChange = null!;
        dependencyFrame = default!;
        carriedApplications = default!;
        affectedApplications = default!;
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

    /// <summary>Reads application dependency evidence created by <see cref="ApplicationDependency"/>.</summary>
    public static bool TryReadApplicationDependency(
        Relation dependency,
        out Relation producerApplication,
        out Relation consumerApplication,
        out IElement dependencyEvidence,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(dependency, out var elements) &&
            elements.Length == 5 &&
            elements[0] is Relation producer &&
            TryReadApplication(producer, out _, out _, out _, out _, out _, out _, out _, out _) &&
            elements[1] is Relation consumer &&
            TryReadApplication(consumer, out _, out _, out _, out _, out _, out _, out _, out _) &&
            !Equals(producer, consumer) &&
            DependencyEvidenceMatches(producer, consumer, elements[2]))
        {
            producerApplication = producer;
            consumerApplication = consumer;
            dependencyEvidence = elements[2];
            residueOrTension = elements[3];
            provenance = elements[4];
            return true;
        }

        producerApplication = null!;
        consumerApplication = null!;
        dependencyEvidence = default!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads stale dependency evidence created by <see cref="StaleDependency"/>.</summary>
    public static bool TryReadStaleDependency(
        Relation staleDependency,
        out Relation dependency,
        out Relation producerReplay,
        out Relation staleConsumerApplication,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(staleDependency, out var elements) &&
            elements.Length == 5 &&
            elements[0] is Relation dependencyRelation &&
            TryReadApplicationDependency(
                dependencyRelation,
                out var producerApplication,
                out var consumerApplication,
                out _,
                out _,
                out _) &&
            elements[1] is Relation replayRelation &&
            TryReadReplay(
                replayRelation,
                out var replayedPreviousApplication,
                out _,
                out _,
                out _,
                out _,
                out _) &&
            Equals(producerApplication, replayedPreviousApplication) &&
            elements[2] is Relation consumerRelation &&
            Equals(consumerApplication, consumerRelation))
        {
            dependency = dependencyRelation;
            producerReplay = replayRelation;
            staleConsumerApplication = consumerRelation;
            residueOrTension = elements[3];
            provenance = elements[4];
            return true;
        }

        dependency = null!;
        producerReplay = null!;
        staleConsumerApplication = null!;
        residueOrTension = default!;
        provenance = default!;
        return false;
    }

    /// <summary>Reads operation-output evidence created by <see cref="OperationOutput"/>.</summary>
    public static bool TryReadOperationOutput(
        Relation operationOutput,
        out Relation application,
        out IElement operation,
        out IElement inputEvidence,
        out IElement operationRead,
        out IElement outputTarget,
        out IElement result,
        out IElement residueOrTension,
        out IElement provenance)
    {
        if (TryReadRootElements(operationOutput, out var elements) &&
            elements.Length == 8 &&
            elements[0] is Relation applicationRelation &&
            TryReadApplication(
                applicationRelation,
                out var transform,
                out _,
                out _,
                out var applicationResult,
                out _,
                out _,
                out _,
                out _) &&
            TryRead(transform, out _, out var transformOperation, out var outputPorts, out _, out _) &&
            Equals(transformOperation, elements[1]) &&
            elements[1] is Relation operationRelation &&
            TryReadOperationReader(outputPorts, operationRelation.Right, applicationResult, out var reader) &&
            operationRelation.TryReadOperationFrom(elements[2], reader, out var expectedRead) &&
            ElementStructureMatches(expectedRead, elements[3]) &&
            TryReadOperationOutputTargets(elements[3], out var expectedTarget) &&
            ElementStructureMatches(expectedTarget, elements[4]) &&
            Equals(applicationResult, elements[5]))
        {
            application = applicationRelation;
            operation = elements[1];
            inputEvidence = elements[2];
            operationRead = elements[3];
            outputTarget = elements[4];
            result = elements[5];
            residueOrTension = elements[6];
            provenance = elements[7];
            return true;
        }

        application = null!;
        operation = default!;
        inputEvidence = default!;
        operationRead = default!;
        outputTarget = default!;
        result = default!;
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
        var clauses = new List<Relation>();
        CollectOperationClauses(operation, clauses);
        return [.. clauses];
    }

    private static void CollectOperationClauses(IElement element, List<Relation> clauses)
    {
        if (element is not Relation relation)
        {
            return;
        }

        if (TryReadRootElements(relation, out var elements) &&
            elements.Length > 0)
        {
            foreach (var child in elements)
            {
                CollectOperationClauses(child, clauses);
            }

            return;
        }

        clauses.Add(relation);
    }

    private static bool TryBuildOperationInput(IElement portSurface, IElement bindings, out IElement inputEvidence)
    {
        var bindingClauses = ReadBindingClauses(bindings);
        if (bindingClauses.Length == 0)
        {
            inputEvidence = default!;
            return false;
        }

        inputEvidence = ApplyBindings(portSurface, bindingClauses, out var changed);
        return changed;
    }

    private static IElement ApplyBindings(IElement element, IReadOnlyList<Relation> bindings, out bool changed)
    {
        foreach (var binding in bindings)
        {
            if (Equals(element, binding.Left))
            {
                changed = true;
                return binding.Right;
            }
        }

        if (element is Relation relation)
        {
            var left = ApplyBindings(relation.Left, bindings, out var leftChanged);
            var right = ApplyBindings(relation.Right, bindings, out var rightChanged);
            changed = leftChanged || rightChanged;
            return changed ? new Relation(left, right) : element;
        }

        changed = false;
        return element;
    }

    private static bool TryReadOperationReader(
        IElement outputPorts,
        IElement operationOutputSurface,
        IElement result,
        out Carrier reader)
    {
        if (TryReadConcreteCarrier(outputPorts, out reader) ||
            TryReadConcreteCarrier(operationOutputSurface, out reader) ||
            TryReadConcreteCarrier(result, out reader))
        {
            return true;
        }

        reader = null!;
        return false;
    }

    private static bool TryReadOperationReader(IElement outputTarget, out Carrier reader) =>
        TryReadConcreteCarrier(outputTarget, out reader);

    private static bool TryReadConcreteCarrier(IElement element, out Carrier carrier)
    {
        switch (element)
        {
            case Carrier candidate when !candidate.IsUnknown:
                carrier = candidate;
                return true;
            case CarrierLocation location when !location.Carrier.IsUnknown:
                carrier = location.Carrier;
                return true;
            case Relation relation:
                if (TryReadRootElements(relation, out var elements) &&
                    elements.Length > 0)
                {
                    foreach (var child in elements)
                    {
                        if (TryReadConcreteCarrier(child, out carrier))
                        {
                            return true;
                        }
                    }

                    break;
                }

                if (TryReadConcreteCarrier(relation.Left, out carrier) ||
                    TryReadConcreteCarrier(relation.Right, out carrier))
                {
                    return true;
                }

                break;
        }

        carrier = null!;
        return false;
    }

    private static bool TryReadOperationOutputTargets(IElement operationRead, out IElement outputTargets)
    {
        if (operationRead is Relation read)
        {
            if (TryReadOperationTermTargets(read.Left, out var targets))
            {
                outputTargets = targets.Length == 1
                    ? targets[0]
                    : Frame(new Carrier(), targets);
                return true;
            }

            return TryReadOperationTargetElement(read.Left, out outputTargets);
        }

        outputTargets = default!;
        return false;
    }

    private static bool TryReadOperationTermTargets(IElement element, out IElement[] targets)
    {
        if (element is Relation group &&
            TryReadRootElements(group, out var terms) &&
            terms.Length > 0)
        {
            var collected = new List<IElement>();
            foreach (var term in terms)
            {
                if (!TryReadOperationTermTarget(term, out var target))
                {
                    targets = [];
                    return false;
                }

                collected.Add(target);
            }

            targets = [.. collected];
            return true;
        }

        targets = [];
        return false;
    }

    private static bool TryReadOperationTermTarget(IElement term, out IElement target)
    {
        if (term is Relation { Left: CarrierLocation location })
        {
            target = Relation.Point(location.Carrier, location.Ticks);
            return true;
        }

        return TryReadOperationTargetElement(term, out target);
    }

    private static bool TryReadOperationTargetElement(IElement element, out IElement target)
    {
        switch (element)
        {
            case CarrierLocation location:
                target = Relation.Point(location.Carrier, location.Ticks);
                return true;
            case Relation relation:
                target = relation;
                return true;
            default:
                target = element;
                return element is not null;
        }
    }

    private static bool ElementStructureMatches(IElement expected, IElement actual)
    {
        if (Equals(expected, actual))
        {
            return true;
        }

        if (TryReadTargetSurfaceElements(expected, out var expectedElements) &&
            TryReadTargetSurfaceElements(actual, out var actualElements))
        {
            return expectedElements.Length == actualElements.Length &&
                expectedElements
                    .Zip(actualElements)
                    .All(pair => ElementStructureMatches(pair.First, pair.Second));
        }

        if (expected is Relation expectedRelation &&
            actual is Relation actualRelation)
        {
            return ElementStructureMatches(expectedRelation.Left, actualRelation.Left) &&
                ElementStructureMatches(expectedRelation.Right, actualRelation.Right);
        }

        return false;
    }

    private static bool TryReadTargetSurfaceElements(IElement element, out IElement[] elements)
    {
        if (element is Relation relation &&
            TryReadRootElements(relation, out var readElements) &&
            readElements.Length > 0)
        {
            elements = readElements;
            return true;
        }

        elements = [];
        return false;
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

    private static bool TryComputeDependencyAffectedApplications(
        IReadOnlyList<Relation> applications,
        Relation changedApplication,
        IReadOnlyList<Relation> dependencies,
        out Relation[] carried,
        out Relation[] affected)
    {
        if (!ContainsRelation(applications, changedApplication))
        {
            carried = [];
            affected = [];
            return false;
        }

        var affectedSet = new List<Relation> { changedApplication };
        var expanded = true;
        while (expanded)
        {
            expanded = false;
            foreach (var dependency in dependencies)
            {
                if (!TryReadApplicationDependency(
                        dependency,
                        out var producer,
                        out var consumer,
                        out _,
                        out _,
                        out _) ||
                    !ContainsRelation(applications, producer) ||
                    !ContainsRelation(applications, consumer) ||
                    !ContainsRelation(affectedSet, producer) ||
                    ContainsRelation(affectedSet, consumer))
                {
                    continue;
                }

                affectedSet.Add(consumer);
                expanded = true;
            }
        }

        var carriedList = new List<Relation>();
        var affectedList = new List<Relation>();
        foreach (var application in applications)
        {
            if (ContainsRelation(affectedSet, application))
            {
                affectedList.Add(application);
            }
            else
            {
                carriedList.Add(application);
            }
        }

        carried = [.. carriedList];
        affected = [.. affectedList];
        return affected.Length > 0;
    }

    private static bool TryCreateStaleDependencyResidue(
        IReadOnlyList<Relation> dependencies,
        IReadOnlyList<Relation> affected,
        IReadOnlyList<Relation> replays,
        IElement residueOrTension,
        IElement provenance,
        out IElement staleDependencyResidue)
    {
        var staleDependencies = new List<Relation>();
        foreach (var dependency in dependencies)
        {
            if (!TryReadApplicationDependency(
                    dependency,
                    out var producerApplication,
                    out var consumerApplication,
                    out _,
                    out _,
                    out _) ||
                !ContainsRelation(affected, consumerApplication) ||
                TryFindReplayForPrevious(replays, consumerApplication, out _) ||
                !TryFindReplayForPrevious(replays, producerApplication, out var producerReplay) ||
                !TryCreateStaleDependency(
                    new Carrier(),
                    dependency,
                    producerReplay,
                    consumerApplication,
                    residueOrTension,
                    provenance,
                    out var staleDependency))
            {
                continue;
            }

            staleDependencies.Add(staleDependency);
        }

        staleDependencyResidue = staleDependencies.Count switch
        {
            0 => UnknownPoint(),
            1 => staleDependencies[0],
            _ => StaleDependencyFrame(new Carrier(), [.. staleDependencies])
        };
        return staleDependencies.Count > 0;
    }

    private static IElement StaleDependencyResidueOf(IEnumerable<Relation> staleDependencies)
    {
        var read = staleDependencies.ToArray();
        return read.Length switch
        {
            0 => UnknownPoint(),
            1 => read[0],
            _ => StaleDependencyFrame(new Carrier(), read)
        };
    }

    private static void CollectStaleDependencyResidue(IElement residue, List<Relation> staleDependencies)
    {
        if (residue is not Relation relation ||
            IsUnknownPoint(relation))
        {
            return;
        }

        if (TryReadStaleDependency(relation, out _, out _, out _, out _, out _))
        {
            if (!ContainsRelation(staleDependencies, relation))
            {
                staleDependencies.Add(relation);
            }

            return;
        }

        if (TryReadRootElements(relation, out var elements))
        {
            foreach (var element in elements)
            {
                CollectStaleDependencyResidue(element, staleDependencies);
            }
        }
    }

    private static bool TryReadRemainingStaleDependencies(
        IReadOnlyList<Relation> staleDependencies,
        IReadOnlyList<Relation> replays,
        out Relation[] remainingStaleDependencies)
    {
        var matched = new bool[staleDependencies.Count];
        foreach (var replay in replays)
        {
            if (!TryReadReplay(
                    replay,
                    out var replayPreviousApplication,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _) ||
                !TryFindStaleDependencyForConsumer(staleDependencies, matched, replayPreviousApplication, out var matchIndex))
            {
                remainingStaleDependencies = [];
                return false;
            }

            matched[matchIndex] = true;
        }

        var remaining = new List<Relation>();
        for (var index = 0; index < staleDependencies.Count; index++)
        {
            if (!matched[index])
            {
                remaining.Add(staleDependencies[index]);
            }
        }

        remainingStaleDependencies = [.. remaining];
        return true;
    }

    private static bool TryFindStaleDependencyForConsumer(
        IReadOnlyList<Relation> staleDependencies,
        IReadOnlyList<bool> matched,
        Relation consumerApplication,
        out int index)
    {
        for (var candidateIndex = 0; candidateIndex < staleDependencies.Count; candidateIndex++)
        {
            if (!matched[candidateIndex] &&
                TryReadStaleDependency(
                    staleDependencies[candidateIndex],
                    out _,
                    out _,
                    out var staleConsumerApplication,
                    out _,
                    out _) &&
                Equals(staleConsumerApplication, consumerApplication))
            {
                index = candidateIndex;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private static bool TryBuildCurrentApplications(
        IReadOnlyList<Relation> previous,
        IReadOnlyList<Relation> carried,
        IReadOnlyList<Relation> replays,
        out Relation[] current)
    {
        if (previous.Count == 0 ||
            replays.Count == 0 ||
            !ApplicationsBelongTo(previous, carried) ||
            !ReplayPreviousApplicationsBelongTo(previous, replays) ||
            !ApplicationsAreUnique(carried) ||
            !ReplayPreviousApplicationsAreUnique(replays) ||
            ReplayPreviousApplicationsOverlap(carried, replays))
        {
            current = [];
            return false;
        }

        var read = new List<Relation>();
        foreach (var previousApplication in previous)
        {
            if (ContainsRelation(carried, previousApplication))
            {
                read.Add(previousApplication);
                continue;
            }

            if (TryFindReplayForPrevious(replays, previousApplication, out var replay) &&
                TryReadReplay(
                    replay,
                    out _,
                    out _,
                    out var newApplication,
                    out _,
                    out _,
                    out _))
            {
                read.Add(newApplication);
            }
        }

        current = [.. read];
        return current.Length > 0;
    }

    private static bool ApplicationsBelongTo(
        IReadOnlyList<Relation> previous,
        IReadOnlyList<Relation> applications)
    {
        foreach (var application in applications)
        {
            if (!ContainsRelation(previous, application))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ReplayPreviousApplicationsBelongTo(
        IReadOnlyList<Relation> previous,
        IReadOnlyList<Relation> replays)
    {
        foreach (var replay in replays)
        {
            if (!TryReadReplay(
                    replay,
                    out var previousApplication,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _) ||
                !ContainsRelation(previous, previousApplication))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ApplicationsAreUnique(IReadOnlyList<Relation> applications)
    {
        for (var left = 0; left < applications.Count; left++)
        {
            for (var right = left + 1; right < applications.Count; right++)
            {
                if (Equals(applications[left], applications[right]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ReplayPreviousApplicationsAreUnique(IReadOnlyList<Relation> replays)
    {
        for (var left = 0; left < replays.Count; left++)
        {
            if (!TryReadReplay(
                    replays[left],
                    out var leftPrevious,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _))
            {
                return false;
            }

            for (var right = left + 1; right < replays.Count; right++)
            {
                if (!TryReadReplay(
                        replays[right],
                        out var rightPrevious,
                        out _,
                        out _,
                        out _,
                        out _,
                        out _) ||
                    Equals(leftPrevious, rightPrevious))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool ReplayPreviousApplicationsOverlap(
        IReadOnlyList<Relation> carried,
        IReadOnlyList<Relation> replays)
    {
        foreach (var replay in replays)
        {
            if (!TryReadReplay(
                    replay,
                    out var previousApplication,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _) ||
                ContainsRelation(carried, previousApplication))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryFindReplayForPrevious(
        IReadOnlyList<Relation> replays,
        Relation previousApplication,
        out Relation replay)
    {
        foreach (var candidate in replays)
        {
            if (TryReadReplay(
                    candidate,
                    out var candidatePrevious,
                    out _,
                    out _,
                    out _,
                    out _,
                    out _) &&
                Equals(candidatePrevious, previousApplication))
            {
                replay = candidate;
                return true;
            }
        }

        replay = null!;
        return false;
    }

    private static bool ApplicationsMatch(
        IReadOnlyList<Relation> left,
        IReadOnlyList<Relation> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var index = 0; index < left.Count; index++)
        {
            if (!Equals(left[index], right[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsRelation(IEnumerable<Relation> candidates, Relation relation) =>
        candidates.Any(candidate => Equals(candidate, relation));

    private static bool TryCreateDependencyEvidence(
        Relation producerApplication,
        Relation consumerApplication,
        out Relation evidence)
    {
        var outputCandidates = ReadProducerOutputCandidates(producerApplication);
        var inputCandidates = ReadConsumerInputCandidates(consumerApplication);
        var bindings = new List<Relation>();

        foreach (var output in outputCandidates)
        {
            foreach (var input in inputCandidates)
            {
                if (Equals(output, input) &&
                    !bindings.Any(binding => Equals(binding.Left, output) && Equals(binding.Right, input)))
                {
                    bindings.Add(FocusSurface.Bind(output, input));
                }
            }
        }

        if (bindings.Count > 0)
        {
            evidence = FocusSurface.BindingFrame(new Carrier(), [.. bindings]);
            return true;
        }

        evidence = null!;
        return false;
    }

    private static bool DependencyEvidenceMatches(
        Relation producerApplication,
        Relation consumerApplication,
        IElement dependencyEvidence)
    {
        var bindings = ReadBindingClauses(dependencyEvidence);
        if (bindings.Length == 0)
        {
            return false;
        }

        var outputCandidates = ReadProducerOutputCandidates(producerApplication);
        var inputCandidates = ReadConsumerInputCandidates(consumerApplication);
        foreach (var binding in bindings)
        {
            if (ElementTreeContainsAny(binding.Left, outputCandidates) &&
                ElementTreeContainsAny(binding.Right, inputCandidates))
            {
                return true;
            }
        }

        return false;
    }

    private static IElement[] ReadProducerOutputCandidates(Relation application)
    {
        if (!TryReadApplication(
                application,
                out var transform,
                out _,
                out var bindings,
                out var result,
                out var patch,
                out _,
                out _,
                out _))
        {
            return [];
        }

        var candidates = new List<IElement>();
        AddCandidate(candidates, result);
        if (TryRead(transform, out _, out _, out var outputPorts, out _, out _) &&
            TryReadPorts(outputPorts, out var outputPortElements))
        {
            AddCandidateTrees(candidates, outputPortElements);
            AddBindingTargetsForSources(candidates, bindings, outputPortElements);
        }

        if (TryReadPatch(
                patch,
                out _,
                out _,
                out _,
                out _,
                out _,
                out var replacement,
                out var correspondence,
                out var patchResult,
                out _,
                out _))
        {
            AddCandidateTree(candidates, replacement);
            AddCandidate(candidates, patchResult);
            AddBindingTargets(candidates, correspondence);
        }

        if (TryCreateOperationOutputRead(
                new Carrier(),
                application,
                UnknownPoint(),
                UnknownPoint(),
                out var operationOutput) &&
            TryReadOperationOutput(
                operationOutput,
                out _,
                out _,
                out _,
                out _,
                out var outputTarget,
                out _,
                out _,
                out _))
        {
            AddCandidateTree(candidates, outputTarget);
        }

        return [.. candidates];
    }

    private static IElement[] ReadConsumerInputCandidates(Relation application)
    {
        if (!TryReadApplication(
                application,
                out var transform,
                out var focus,
                out var bindings,
                out _,
                out var patch,
                out _,
                out _,
                out _))
        {
            return [];
        }

        var candidates = new List<IElement>();
        if (TryRead(transform, out var inputPorts, out var operation, out _, out _, out _) &&
            TryReadPorts(inputPorts, out var inputPortElements))
        {
            AddCandidateTrees(candidates, inputPortElements);
            AddBindingTargetsForSources(candidates, bindings, inputPortElements);
            AddOperationInputCandidates(candidates, operation);
        }

        AddBindingSources(candidates, bindings);
        if (FocusSurface.TryRead(
                focus,
                out var source,
                out var focusBasis,
                out var participation,
                out var complement,
                out _,
                out var focusCorrespondence,
                out _,
                out _))
        {
            AddCandidate(candidates, source);
            AddCandidateTree(candidates, focusBasis);
            AddCandidateTree(candidates, participation);
            AddCandidateTree(candidates, complement);
            AddBindingSources(candidates, focusCorrespondence);
        }

        if (TryReadPatch(
                patch,
                out var patchSource,
                out _,
                out _,
                out var patchParticipation,
                out var patchComplement,
                out _,
                out var patchCorrespondence,
                out _,
                out _,
                out _))
        {
            AddCandidate(candidates, patchSource);
            AddCandidateTree(candidates, patchParticipation);
            AddCandidateTree(candidates, patchComplement);
            AddBindingSources(candidates, patchCorrespondence);
        }

        return [.. candidates];
    }

    private static Relation[] ReadBindingClauses(IElement evidence)
    {
        var clauses = new List<Relation>();
        CollectBindingClauses(evidence, clauses);
        return [.. clauses];
    }

    private static void CollectBindingClauses(IElement evidence, List<Relation> clauses)
    {
        if (evidence is not Relation relation)
        {
            return;
        }

        if (TryReadRootElements(relation, out var elements) &&
            elements.Length > 0)
        {
            foreach (var element in elements)
            {
                CollectBindingClauses(element, clauses);
            }

            return;
        }

        clauses.Add(relation);
    }

    private static void AddOperationInputCandidates(List<IElement> candidates, IElement operation)
    {
        foreach (var clause in ReadOperationClauses(operation))
        {
            if (TryReadMap(clause, out var input, out _))
            {
                AddCandidateTree(candidates, input);
            }
        }
    }

    private static void AddBindingTargetsForSources(
        List<IElement> candidates,
        IElement bindings,
        IReadOnlyList<IElement> sources)
    {
        foreach (var binding in ReadBindingClauses(bindings))
        {
            if (ContainsElement(sources, binding.Left))
            {
                AddCandidateTree(candidates, binding.Right);
            }
        }
    }

    private static void AddBindingTargets(List<IElement> candidates, IElement bindings)
    {
        foreach (var binding in ReadBindingClauses(bindings))
        {
            AddCandidateTree(candidates, binding.Right);
        }
    }

    private static void AddBindingSources(List<IElement> candidates, IElement bindings)
    {
        foreach (var binding in ReadBindingClauses(bindings))
        {
            AddCandidateTree(candidates, binding.Left);
        }
    }

    private static void AddCandidateTrees(List<IElement> candidates, IEnumerable<IElement> elements)
    {
        foreach (var element in elements)
        {
            AddCandidateTree(candidates, element);
        }
    }

    private static void AddCandidateTree(List<IElement> candidates, IElement element)
    {
        AddCandidate(candidates, element);

        if (element is not Relation relation)
        {
            return;
        }

        if (TryReadRootElements(relation, out var elements) &&
            elements.Length > 0)
        {
            foreach (var child in elements)
            {
                AddCandidateTree(candidates, child);
            }

            return;
        }

        AddCandidateTree(candidates, relation.Left);
        AddCandidateTree(candidates, relation.Right);
    }

    private static bool ElementTreeContainsAny(IElement element, IReadOnlyList<IElement> candidates)
    {
        if (ContainsElement(candidates, element))
        {
            return true;
        }

        if (element is not Relation relation)
        {
            return false;
        }

        if (TryReadRootElements(relation, out var elements) &&
            elements.Length > 0)
        {
            return elements.Any(child => ElementTreeContainsAny(child, candidates));
        }

        return ElementTreeContainsAny(relation.Left, candidates) ||
            ElementTreeContainsAny(relation.Right, candidates);
    }

    private static void AddCandidate(List<IElement> candidates, IElement element)
    {
        if (element is null ||
            element is Carrier ||
            element is CarrierLocation ||
            IsUnknownPoint(element) ||
            ContainsElement(candidates, element))
        {
            return;
        }

        candidates.Add(element);
    }

    private static bool ContainsElement(IEnumerable<IElement> candidates, IElement element) =>
        candidates.Any(candidate => Equals(candidate, element));

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

    private static IElement CombineEvidence(params IElement[] evidence)
    {
        var meaningful = new List<IElement>();
        foreach (var element in evidence ?? [])
        {
            if (element is not null && !IsUnknownPoint(element))
            {
                meaningful.Add(element);
            }
        }

        return meaningful.Count switch
        {
            0 => UnknownPoint(),
            1 => meaningful[0],
            _ => Frame(new Carrier(), [.. meaningful])
        };
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

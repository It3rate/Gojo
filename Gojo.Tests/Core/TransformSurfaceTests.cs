using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class TransformSurfaceTests
{
    [Fact]
    public void TransformSurfaceReadsReusableInstructionPortsOperationAndPolicy()
    {
        var input = new Placeholder();
        var output = new Placeholder();
        var inputPorts = TransformSurface.Ports(new Carrier(), input);
        var outputPorts = TransformSurface.Ports(new Carrier(), output);
        var operation = FocusSurface.Bind(input, output);
        var residuePolicy = Relation.Point(new Carrier(), 3);
        var provenance = Relation.Point(new Carrier(), 4);

        var transform = TransformSurface.Create(
            new Carrier(),
            inputPorts,
            operation,
            outputPorts,
            residuePolicy,
            provenance);

        Assert.True(TransformSurface.TryRead(
            transform,
            out var readInputPorts,
            out var readOperation,
            out var readOutputPorts,
            out var readPolicy,
            out var readProvenance));
        Assert.Equal(inputPorts, readInputPorts);
        Assert.Equal(operation, readOperation);
        Assert.Equal(outputPorts, readOutputPorts);
        Assert.Equal(residuePolicy, readPolicy);
        Assert.Equal(provenance, readProvenance);
    }

    [Fact]
    public void TransformApplicationBuildsPatchAndReadWitnessFromFocus()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 10);
        var selected = Relation.Segment(sourceCarrier, 4, 6);
        var carry = FocusSurface.Frame(
            new Carrier(),
            Relation.Segment(sourceCarrier, 0, 4),
            Relation.Segment(sourceCarrier, 6, 10));
        var replacement = Relation.Segment(new Carrier(), 0, 3);
        var focusCorrespondence = FocusSurface.Bind(selected, replacement);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 5),
            selected,
            carry,
            replacement,
            focusCorrespondence,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        var inputPort = new Placeholder();
        var outputPort = new Placeholder();
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), inputPort),
            FocusSurface.Bind(inputPort, outputPort),
            TransformSurface.Ports(new Carrier(), outputPort),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(new Carrier(), 1));
        var bindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(inputPort, selected),
            FocusSurface.Bind(outputPort, replacement));
        var result = FocusSurface.Frame(new Carrier(), carry, replacement);
        var residue = new Placeholder();
        var provenance = Relation.Point(new Carrier(), 2);

        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            focus,
            bindings,
            result,
            residue,
            provenance,
            out var application));

        Assert.True(TransformSurface.TryReadApplication(
            application,
            out var readTransform,
            out var readFocus,
            out var readBindings,
            out var readResult,
            out var patch,
            out var witness,
            out var readResidue,
            out var readProvenance));
        Assert.Equal(transform, readTransform);
        Assert.Equal(focus, readFocus);
        Assert.Equal(bindings, readBindings);
        Assert.Equal(result, readResult);
        Assert.Equal(residue, readResidue);
        Assert.Equal(provenance, readProvenance);

        Assert.True(TransformSurface.TryReadPatch(
            patch,
            out var patchSource,
            out var patchTransform,
            out var patchFocus,
            out var patchParticipation,
            out var patchComplement,
            out var patchReplacement,
            out var patchCorrespondence,
            out var patchResult,
            out var patchResidue,
            out var patchProvenance));
        Assert.Equal(source, patchSource);
        Assert.Equal(transform, patchTransform);
        Assert.Equal(focus, patchFocus);
        Assert.Equal(selected, patchParticipation);
        Assert.Equal(carry, patchComplement);
        Assert.Equal(replacement, patchReplacement);
        Assert.Equal(bindings, patchCorrespondence);
        Assert.Equal(result, patchResult);
        Assert.Equal(residue, patchResidue);
        Assert.Equal(provenance, patchProvenance);

        Assert.True(ReadWitness.TryRead(
            witness,
            out var witnessSelected,
            out var witnessSurface,
            out var witnessResult,
            out var witnessResidue,
            out var witnessProvenance));
        Assert.Equal(focus, witnessSelected);
        Assert.Equal(transform, witnessSurface);
        Assert.Equal(result, witnessResult);
        Assert.Equal(residue, witnessResidue);
        Assert.Equal(provenance, witnessProvenance);
    }

    [Fact]
    public void TransformOperationShapesReadCarryIntroduceRemoveSplitFoldAndInterpolate()
    {
        var input = Relation.Point(new Carrier(), 1);
        var output = Relation.Point(new Carrier(), 2);
        var extra = Relation.Point(new Carrier(), 3);
        var grade = Relation.Point(new Carrier(), 4);
        var outputPorts = TransformSurface.Ports(new Carrier(), output, extra);
        var inputPorts = TransformSurface.Ports(new Carrier(), input, extra);

        var carry = TransformSurface.Carry(input, output);
        var introduce = TransformSurface.Introduce(output);
        var remove = TransformSurface.Remove(input);
        var split = TransformSurface.Split(input, outputPorts);
        var fold = TransformSurface.Fold(inputPorts, output);
        var interpolate = TransformSurface.Interpolate(input, extra, grade, output);

        Assert.True(TransformSurface.TryReadMap(carry, out var mapInput, out var mapOutput));
        Assert.Equal(input, mapInput);
        Assert.Equal(output, mapOutput);

        Assert.True(TransformSurface.TryReadIntroduce(introduce, out var introducedOutput));
        Assert.Equal(output, introducedOutput);

        Assert.True(TransformSurface.TryReadRemove(remove, out var removedInput));
        Assert.Equal(input, removedInput);

        Assert.True(TransformSurface.TryReadSplit(split, out var splitInput, out var splitOutputs));
        Assert.Equal(input, splitInput);
        Assert.Equal(outputPorts, splitOutputs);

        Assert.True(TransformSurface.TryReadFold(fold, out var foldInputs, out var foldOutput));
        Assert.Equal(inputPorts, foldInputs);
        Assert.Equal(output, foldOutput);

        Assert.True(TransformSurface.TryReadInterpolate(
            interpolate,
            out var first,
            out var second,
            out var readGrade,
            out var interpolatedOutput));
        Assert.Equal(input, first);
        Assert.Equal(extra, second);
        Assert.Equal(grade, readGrade);
        Assert.Equal(output, interpolatedOutput);
    }

    [Fact]
    public void TransformApplicationCanDeriveBindingsFromOperationEvidence()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 10);
        var selected = Relation.Segment(sourceCarrier, 4, 6);
        var carry = Relation.Segment(sourceCarrier, 0, 4);
        var replacement = Relation.Segment(new Carrier(), 0, 3);
        var introduced = Relation.Point(new Carrier(), 9);
        var operation = TransformSurface.Frame(
            new Carrier(),
            TransformSurface.Remove(selected),
            TransformSurface.Carry(carry, carry),
            TransformSurface.Introduce(introduced),
            TransformSurface.Map(selected, replacement));
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), selected),
            operation,
            TransformSurface.Ports(new Carrier(), replacement, introduced),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 5),
            selected,
            carry,
            replacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var result = FocusSurface.Frame(new Carrier(), carry, replacement, introduced);

        Assert.True(TransformSurface.TryDeriveBindings(operation, out var derived));
        Assert.True(FocusSurface.TryReadBindingFrame(derived, out var derivedBindings));
        Assert.Equal(4, derivedBindings.Length);

        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            focus,
            Relation.Point(Carrier.Unknown, 0),
            result,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var application));
        Assert.True(TransformSurface.TryReadApplication(
            application,
            out _,
            out _,
            out var applicationBindings,
            out _,
            out var patch,
            out _,
            out _,
            out _));
        Assert.True(FocusSurface.TryReadBindingFrame(applicationBindings, out var applicationBindingClauses));
        Assert.Equal(derivedBindings, applicationBindingClauses);

        Assert.True(TransformSurface.TryReadPatch(
            patch,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out var patchCorrespondence,
            out _,
            out _,
            out _));
        Assert.True(FocusSurface.TryReadBindingFrame(patchCorrespondence, out var patchBindingClauses));
        Assert.Equal(derivedBindings, patchBindingClauses);
    }

    [Fact]
    public void TransformApplicationCanReplayAgainstChangedFocusAndPreserveStalePatch()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 12);
        var inputPort = new Placeholder();
        var outputPort = new Placeholder();
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), inputPort),
            TransformSurface.Map(inputPort, outputPort),
            TransformSurface.Ports(new Carrier(), outputPort),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        var oldSelected = Relation.Segment(sourceCarrier, 4, 6);
        var oldCarry = FocusSurface.Frame(
            new Carrier(),
            Relation.Segment(sourceCarrier, 0, 4),
            Relation.Segment(sourceCarrier, 6, 12));
        var oldReplacement = Relation.Segment(new Carrier(), 0, 3);
        var oldFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 5),
            oldSelected,
            oldCarry,
            oldReplacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var oldBindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(inputPort, oldSelected),
            FocusSurface.Bind(outputPort, oldReplacement));
        var oldResult = FocusSurface.Frame(new Carrier(), oldCarry, oldReplacement);

        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            oldFocus,
            oldBindings,
            oldResult,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var oldApplication));
        Assert.True(TransformSurface.TryReadApplication(
            oldApplication,
            out _,
            out _,
            out _,
            out _,
            out var oldPatch,
            out _,
            out _,
            out _));

        var newSelected = Relation.Segment(sourceCarrier, 5, 8);
        var newCarry = FocusSurface.Frame(
            new Carrier(),
            Relation.Segment(sourceCarrier, 0, 5),
            Relation.Segment(sourceCarrier, 8, 12));
        var newReplacement = Relation.Segment(new Carrier(), 0, 5);
        var newFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 6),
            newSelected,
            newCarry,
            newReplacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var newBindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(inputPort, newSelected),
            FocusSurface.Bind(outputPort, newReplacement));
        var newResult = FocusSurface.Frame(new Carrier(), newCarry, newReplacement);
        var replayResidue = new Placeholder();

        Assert.True(TransformSurface.TryReplayApplication(
            new Carrier(),
            oldApplication,
            newFocus,
            newBindings,
            newResult,
            replayResidue,
            Relation.Point(Carrier.Unknown, 0),
            out var replay));

        Assert.True(TransformSurface.TryReadReplay(
            replay,
            out var replayPrevious,
            out var replayFocus,
            out var replayApplication,
            out var stale,
            out var replayReadResidue,
            out var replayProvenance));
        Assert.Equal(oldApplication, replayPrevious);
        Assert.Equal(newFocus, replayFocus);
        Assert.Equal(replayResidue, replayReadResidue);
        Assert.True(FocusSurface.TryReadFrame(replayProvenance, out var replayProvenanceParts));
        Assert.Equal(3, replayProvenanceParts.Length);

        Assert.True(TransformSurface.TryReadApplication(
            replayApplication,
            out var replayTransform,
            out var applicationFocus,
            out var applicationBindings,
            out var applicationResult,
            out _,
            out _,
            out var applicationResidue,
            out _));
        Assert.Equal(transform, replayTransform);
        Assert.Equal(newFocus, applicationFocus);
        Assert.Equal(newBindings, applicationBindings);
        Assert.Equal(newResult, applicationResult);
        Assert.Equal(replayResidue, applicationResidue);

        var stalePatch = Assert.IsType<Relation>(stale);
        Assert.True(TransformSurface.TryReadStalePatch(
            stalePatch,
            out var stalePreviousApplication,
            out var stalePreviousPatch,
            out var stalePreviousFocus,
            out var staleNewFocus,
            out var staleResidue,
            out _));
        Assert.Equal(oldApplication, stalePreviousApplication);
        Assert.Equal(oldPatch, stalePreviousPatch);
        Assert.Equal(oldFocus, stalePreviousFocus);
        Assert.Equal(newFocus, staleNewFocus);
        Assert.Equal(replayResidue, staleResidue);
    }

    [Fact]
    public void StalePatchIsNotCreatedWhenFocusStillMatches()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 10);
        var selected = Relation.Segment(sourceCarrier, 4, 6);
        var replacement = Relation.Segment(new Carrier(), 0, 3);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 5),
            selected,
            Relation.Segment(sourceCarrier, 0, 4),
            replacement,
            FocusSurface.Bind(selected, replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), selected),
            TransformSurface.Map(selected, replacement),
            TransformSurface.Ports(new Carrier(), replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            focus,
            FocusSurface.Bind(selected, replacement),
            FocusSurface.Frame(new Carrier(), source, replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var application));

        Assert.False(TransformSurface.TryCreateStalePatch(
            new Carrier(),
            application,
            focus,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out _));
    }

    [Fact]
    public void StructuralLadderCanUseTransformApplicationAsTimelineTransition()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 10);
        var selected = Relation.Segment(sourceCarrier, 4, 6);
        var replacement = Relation.Segment(new Carrier(), 0, 3);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 5),
            selected,
            Relation.Segment(sourceCarrier, 0, 4),
            replacement,
            FocusSurface.Bind(selected, replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), new Placeholder()),
            FocusSurface.Bind(selected, replacement),
            TransformSurface.Ports(new Carrier(), new Placeholder()),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var result = FocusSurface.Frame(new Carrier(), source, replacement);
        var residue = new Placeholder();

        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            focus,
            FocusSurface.Bind(selected, replacement),
            result,
            residue,
            Relation.Point(Carrier.Unknown, 0),
            out var application));
        Assert.True(StructuralLadderSurface.TryCreateRungFromTransformApplication(
            new Carrier(),
            application,
            out var refinedRung));

        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), source);
        var clock = TimeStepSequence.CreateClock(new Carrier());
        var step = StructuralLadderSurface.TransformStep(
            new Carrier(),
            clock,
            0,
            1,
            baseRung,
            application,
            refinedRung,
            residue);
        var ladder = StructuralLadderSurface.Create(
            new Carrier(),
            baseRung,
            StructuralLadderSurface.RungFrame(new Carrier(), baseRung, refinedRung),
            StructuralLadderSurface.Timeline(new Carrier(), clock, baseRung, step),
            residue,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(StructuralLadderSurface.TryReadTransformStep(
            step,
            out var span,
            out var start,
            out var end,
            out var before,
            out var readApplication,
            out var after,
            out var readResidue));
        Assert.Equal(Relation.Segment(clock, 0, 1), span);
        Assert.Equal(new CarrierLocation(clock, 0), start);
        Assert.Equal(new CarrierLocation(clock, 1), end);
        Assert.Equal(baseRung, before);
        Assert.Equal(application, readApplication);
        Assert.Equal(refinedRung, after);
        Assert.Equal(residue, readResidue);

        Assert.True(StructuralLadderSurface.TryReadRungPath(ladder, out var path));
        Assert.Equal([baseRung, refinedRung], path);
        Assert.True(StructuralLadderSurface.TryReadRung(
            refinedRung,
            out _,
            out _,
            out _,
            out var rungCorrespondence,
            out _,
            out _,
            out _,
            out _));
        Assert.Equal(application, rungCorrespondence);
    }

    [Fact]
    public void RoundedCornerCanBeExpressedAsGenericTransformApplication()
    {
        var horizontal = new Carrier();
        var vertical = new Carrier();
        var arc = new Carrier();
        var incomingEdge = Relation.Segment(horizontal, 0, 10);
        var outgoingEdge = Relation.Segment(vertical, 0, 8);
        var cornerJoin = new Relation(
            Relation.Point(horizontal, 10),
            Relation.Point(vertical, 0));
        var incomingSelected = Relation.Segment(horizontal, 7, 10);
        var outgoingSelected = Relation.Segment(vertical, 0, 3);
        var incomingCarry = Relation.Segment(horizontal, 0, 7);
        var outgoingCarry = Relation.Segment(vertical, 3, 8);
        var roundedTransition = Relation.Segment(arc, 0, 4);
        var source = FocusSurface.Frame(new Carrier(), incomingEdge, cornerJoin, outgoingEdge);
        var participation = FocusSurface.Frame(
            new Carrier(),
            FocusSurface.PartialSpan(new Carrier(), incomingEdge, incomingSelected, incomingCarry, Relation.Point(Carrier.Unknown, 0)),
            FocusSurface.PartialSpan(new Carrier(), outgoingEdge, outgoingSelected, outgoingCarry, Relation.Point(Carrier.Unknown, 0)));
        var carry = FocusSurface.Frame(new Carrier(), incomingCarry, outgoingCarry);
        var correspondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(Relation.Point(horizontal, 7), Relation.Point(arc, 0)),
            FocusSurface.Bind(Relation.Point(vertical, 3), Relation.Point(arc, 4)));
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            cornerJoin,
            participation,
            carry,
            roundedTransition,
            correspondence,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), incomingSelected, outgoingSelected),
            TransformSurface.Frame(new Carrier(), participation, roundedTransition, correspondence),
            TransformSurface.Ports(new Carrier(), incomingCarry, roundedTransition, outgoingCarry),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var result = FocusSurface.Frame(new Carrier(), incomingCarry, roundedTransition, outgoingCarry, correspondence);
        var residue = new Placeholder();

        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            focus,
            correspondence,
            result,
            residue,
            Relation.Point(Carrier.Unknown, 0),
            out var application));
        Assert.True(TransformSurface.TryReadApplication(
            application,
            out _,
            out _,
            out _,
            out _,
            out var patch,
            out _,
            out _,
            out _));
        Assert.True(TransformSurface.TryReadPatch(
            patch,
            out var patchSource,
            out _,
            out _,
            out var patchParticipation,
            out var patchCarry,
            out var patchReplacement,
            out var patchCorrespondence,
            out var patchResult,
            out var patchResidue,
            out _));

        Assert.Equal(source, patchSource);
        Assert.Equal(participation, patchParticipation);
        Assert.Equal(carry, patchCarry);
        Assert.Equal(roundedTransition, patchReplacement);
        Assert.Equal(correspondence, patchCorrespondence);
        Assert.Equal(result, patchResult);
        Assert.Equal(residue, patchResidue);
    }
}

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
    public void OperationOutputReadSettlesExecutableOperationFromBindings()
    {
        var x = new Carrier();
        var y = new Carrier();
        var resultCarrier = new Carrier();
        var xPort = new Relation(Relation.Point(x, 1), Relation.Point(Carrier.Unknown, 1));
        var yPort = new Relation(Relation.Point(y, 1), Relation.Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(xPort, yPort), Relation.Segment(resultCarrier, 0, 1));
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), xPort, yPort),
            operation,
            TransformSurface.Ports(new Carrier(), Relation.Segment(resultCarrier, 0, 1)),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var xInput = Relation.Point(x, 3);
        var yInput = Relation.Point(y, 4);
        var source = FocusSurface.Frame(new Carrier(), xInput, yInput);
        var output = Relation.Point(resultCarrier, 12);
        var result = FocusSurface.Frame(new Carrier(), source, output);
        var application = CreateApplication(
            transform,
            CreateFocus(
                source,
                xInput,
                source,
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0)),
            FocusSurface.BindingFrame(
                new Carrier(),
                FocusSurface.Bind(xPort, xInput),
                FocusSurface.Bind(yPort, yInput)),
            result);

        Assert.True(TransformSurface.TryCreateOperationOutputRead(
            new Carrier(),
            application,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var operationOutput));
        Assert.True(TransformSurface.TryReadOperationOutput(
            operationOutput,
            out var readApplication,
            out var readOperation,
            out var inputEvidence,
            out _,
            out var outputTarget,
            out var readResult,
            out _,
            out _));
        Assert.Equal(application, readApplication);
        Assert.Equal(operation, readOperation);
        Assert.Equal(new Relation(xInput, yInput), inputEvidence);
        Assert.Equal(output, outputTarget);
        Assert.Equal(result, readResult);
    }

    [Fact]
    public void OperationOutputReadPreservesProductExpansionTargets()
    {
        var lengthShape = new Carrier().UnitShape;
        var x = new Carrier(lengthShape);
        var y = new Carrier(lengthShape);
        var area = new Carrier(UnitShape.Product(lengthShape, lengthShape));
        var xPort = new Relation(Relation.Point(x, 1), Relation.Point(Carrier.Unknown, 1));
        var yPort = new Relation(Relation.Point(y, 1), Relation.Point(Carrier.Unknown, 1));
        var outputSurface = Relation.Segment(area, 0, 1);
        var operation = new Relation(new Relation(xPort, yPort), outputSurface);
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), xPort, yPort),
            operation,
            TransformSurface.Ports(new Carrier(), outputSurface),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var xFused = new Relation(Relation.Point(x, 3), Relation.Point(x.Inverse(), 2));
        var yFused = new Relation(
            new CarrierLocation(y.Inverse(), 4),
            new CarrierLocation(y, 5));
        var source = FocusSurface.Frame(new Carrier(), xFused, yFused);
        var result = FocusSurface.Frame(new Carrier(), source);
        var application = CreateApplication(
            transform,
            CreateFocus(
                source,
                xFused,
                source,
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0)),
            FocusSurface.BindingFrame(
                new Carrier(),
                FocusSurface.Bind(xPort, xFused),
                FocusSurface.Bind(yPort, yFused)),
            result);

        Assert.True(TransformSurface.TryCreateOperationOutputRead(
            new Carrier(),
            application,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var operationOutput));
        Assert.True(TransformSurface.TryReadOperationOutput(
            operationOutput,
            out _,
            out _,
            out _,
            out _,
            out var outputTargets,
            out _,
            out _,
            out _));

        var targetFrame = Assert.IsType<Relation>(outputTargets);
        var holder = Assert.IsType<Carrier>(targetFrame.Right);
        Assert.True(targetFrame.TryReadOrderedElementsOn(holder, out var targets));
        Assert.Equal(4, targets.Length);
        AssertPointTarget(targets[0], area.Inverse(), 8);
        AssertPointTarget(targets[1], area.Inverse(), 10);
        AssertPointTarget(targets[2], area.Inverse(), 12);
        AssertPointTarget(targets[3], area, 15);

        static void AssertPointTarget(IElement element, Carrier expectedCarrier, long expectedTicks)
        {
            var point = Assert.IsType<Relation>(element);
            var location = Assert.IsType<CarrierLocation>(point.Left);
            var carrier = Assert.IsType<Carrier>(point.Right);
            Assert.True(location.Carrier.SameIdentityAs(expectedCarrier));
            Assert.True(carrier.SameIdentityAs(expectedCarrier));
            Assert.Equal(expectedTicks, location.Ticks);
        }
    }

    [Fact]
    public void ApplicationDependencyDiscoversSettledOperationOutputInsideProducerResult()
    {
        var x = new Carrier();
        var y = new Carrier();
        var resultCarrier = new Carrier();
        var xPort = new Relation(Relation.Point(x, 1), Relation.Point(Carrier.Unknown, 1));
        var yPort = new Relation(Relation.Point(y, 1), Relation.Point(Carrier.Unknown, 1));
        var operation = new Relation(new Relation(xPort, yPort), Relation.Segment(resultCarrier, 0, 1));
        var producerTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), xPort, yPort),
            operation,
            TransformSurface.Ports(new Carrier(), Relation.Segment(resultCarrier, 0, 1)),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var xInput = Relation.Point(x, 3);
        var yInput = Relation.Point(y, 4);
        var source = FocusSurface.Frame(new Carrier(), xInput, yInput);
        var output = Relation.Point(resultCarrier, 12);
        var producerResult = FocusSurface.Frame(new Carrier(), source, output);
        var producerApplication = CreateApplication(
            producerTransform,
            CreateFocus(
                source,
                xInput,
                source,
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0)),
            FocusSurface.BindingFrame(
                new Carrier(),
                FocusSurface.Bind(xPort, xInput),
                FocusSurface.Bind(yPort, yInput)),
            producerResult);

        var consumerReplacement = Relation.Point(new Carrier(), 1);
        var consumerTransform = CreateMapTransform(new Placeholder(), new Placeholder());
        var consumerApplication = CreateApplication(
            consumerTransform,
            CreateFocus(
                output,
                output,
                output,
                Relation.Point(Carrier.Unknown, 0),
                consumerReplacement,
                FocusSurface.Bind(output, consumerReplacement)),
            FocusSurface.Bind(ReadInputPort(consumerTransform), output),
            FocusSurface.Frame(new Carrier(), output, consumerReplacement));

        Assert.True(TransformSurface.TryCreateApplicationDependency(
            new Carrier(),
            producerApplication,
            consumerApplication,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var dependency));
        Assert.True(TransformSurface.TryReadApplicationDependency(
            dependency,
            out var readProducer,
            out var readConsumer,
            out var evidence,
            out _,
            out _));
        Assert.Equal(producerApplication, readProducer);
        Assert.Equal(consumerApplication, readConsumer);
        Assert.True(FocusSurface.TryReadBindingFrame(evidence, out var bindings));
        Assert.Contains(bindings, binding =>
            Equals(binding.Left, output) &&
            Equals(binding.Right, output));
    }

    [Fact]
    public void ApplicationDependencyDiscoversOneProductExpansionOutputTarget()
    {
        var lengthShape = new Carrier().UnitShape;
        var x = new Carrier(lengthShape);
        var y = new Carrier(lengthShape);
        var area = new Carrier(UnitShape.Product(lengthShape, lengthShape));
        var xPort = new Relation(Relation.Point(x, 1), Relation.Point(Carrier.Unknown, 1));
        var yPort = new Relation(Relation.Point(y, 1), Relation.Point(Carrier.Unknown, 1));
        var outputSurface = Relation.Segment(area, 0, 1);
        var operation = new Relation(new Relation(xPort, yPort), outputSurface);
        var producerTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), xPort, yPort),
            operation,
            TransformSurface.Ports(new Carrier(), outputSurface),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var xFused = new Relation(Relation.Point(x, 3), Relation.Point(x.Inverse(), 2));
        var yFused = new Relation(
            new CarrierLocation(y.Inverse(), 4),
            new CarrierLocation(y, 5));
        var source = FocusSurface.Frame(new Carrier(), xFused, yFused);
        var consumedOutput = Relation.Point(area.Inverse(), 10);
        var producerApplication = CreateApplication(
            producerTransform,
            CreateFocus(
                source,
                xFused,
                source,
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0)),
            FocusSurface.BindingFrame(
                new Carrier(),
                FocusSurface.Bind(xPort, xFused),
                FocusSurface.Bind(yPort, yFused)),
            FocusSurface.Frame(new Carrier(), source, consumedOutput));

        var consumerReplacement = Relation.Point(new Carrier(), 1);
        var consumerTransform = CreateMapTransform(new Placeholder(), new Placeholder());
        var consumerApplication = CreateApplication(
            consumerTransform,
            CreateFocus(
                consumedOutput,
                consumedOutput,
                consumedOutput,
                Relation.Point(Carrier.Unknown, 0),
                consumerReplacement,
                FocusSurface.Bind(consumedOutput, consumerReplacement)),
            FocusSurface.Bind(ReadInputPort(consumerTransform), consumedOutput),
            FocusSurface.Frame(new Carrier(), consumedOutput, consumerReplacement));

        Assert.True(TransformSurface.TryCreateApplicationDependency(
            new Carrier(),
            producerApplication,
            consumerApplication,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var dependency));
        Assert.True(TransformSurface.TryReadApplicationDependency(
            dependency,
            out var readProducer,
            out var readConsumer,
            out var evidence,
            out _,
            out _));
        Assert.Equal(producerApplication, readProducer);
        Assert.Equal(consumerApplication, readConsumer);
        Assert.True(FocusSurface.TryReadBindingFrame(evidence, out var bindings));
        Assert.Contains(bindings, binding =>
            Equals(binding.Left, consumedOutput) &&
            Equals(binding.Right, consumedOutput));
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
    public void PartialReplayCarriesPrefixAndReplaysAffectedApplications()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 20);
        var firstInput = new Placeholder();
        var firstOutput = new Placeholder();
        var secondInput = new Placeholder();
        var secondOutput = new Placeholder();
        var firstTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), firstInput),
            TransformSurface.Map(firstInput, firstOutput),
            TransformSurface.Ports(new Carrier(), firstOutput),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var secondTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), secondInput),
            TransformSurface.Map(secondInput, secondOutput),
            TransformSurface.Ports(new Carrier(), secondOutput),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        var firstSelected = Relation.Segment(sourceCarrier, 2, 4);
        var firstReplacement = Relation.Segment(new Carrier(), 0, 2);
        var firstFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 3),
            firstSelected,
            Relation.Segment(sourceCarrier, 0, 2),
            firstReplacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var firstBindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(firstInput, firstSelected),
            FocusSurface.Bind(firstOutput, firstReplacement));
        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            firstTransform,
            firstFocus,
            firstBindings,
            FocusSurface.Frame(new Carrier(), source, firstReplacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var firstApplication));

        var secondSelected = Relation.Segment(sourceCarrier, 8, 10);
        var secondReplacement = Relation.Segment(new Carrier(), 0, 2);
        var secondFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 9),
            secondSelected,
            Relation.Segment(sourceCarrier, 0, 8),
            secondReplacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var secondBindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(secondInput, secondSelected),
            FocusSurface.Bind(secondOutput, secondReplacement));
        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            secondTransform,
            secondFocus,
            secondBindings,
            FocusSurface.Frame(new Carrier(), source, secondReplacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var secondApplication));

        var previousFrame = TransformSurface.ApplicationFrame(new Carrier(), firstApplication, secondApplication);
        var carriedPrefix = TransformSurface.ApplicationFrame(new Carrier(), firstApplication);
        var changedSecondSelected = Relation.Segment(sourceCarrier, 9, 13);
        var changedSecondReplacement = Relation.Segment(new Carrier(), 0, 4);
        var changedSecondFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 11),
            changedSecondSelected,
            Relation.Segment(sourceCarrier, 0, 9),
            changedSecondReplacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var changedSecondBindings = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(secondInput, changedSecondSelected),
            FocusSurface.Bind(secondOutput, changedSecondReplacement));
        var replayResidue = new Placeholder();
        Assert.True(TransformSurface.TryReplayApplication(
            new Carrier(),
            secondApplication,
            changedSecondFocus,
            changedSecondBindings,
            FocusSurface.Frame(new Carrier(), source, changedSecondReplacement),
            replayResidue,
            Relation.Point(Carrier.Unknown, 0),
            out var secondReplay));

        Assert.True(TransformSurface.TryCreatePartialReplay(
            new Carrier(),
            previousFrame,
            carriedPrefix,
            [secondReplay],
            replayResidue,
            Relation.Point(Carrier.Unknown, 0),
            out var partialReplay));

        Assert.True(TransformSurface.TryReadPartialReplay(
            partialReplay,
            out var readPreviousFrame,
            out var readCarriedPrefix,
            out var readReplayFrame,
            out var readCurrentFrame,
            out var readResidue,
            out _));
        Assert.Equal(previousFrame, readPreviousFrame);
        Assert.Equal(carriedPrefix, readCarriedPrefix);
        Assert.Equal(replayResidue, readResidue);
        Assert.True(TransformSurface.TryReadReplayFrame(readReplayFrame, out var replayRecords));
        Assert.Equal([secondReplay], replayRecords);
        Assert.True(TransformSurface.TryReadApplicationFrame(readCurrentFrame, out var currentApplications));
        Assert.Equal(2, currentApplications.Length);
        Assert.Equal(firstApplication, currentApplications[0]);
        Assert.True(TransformSurface.TryReadReplay(
            secondReplay,
            out _,
            out _,
            out var replayedSecondApplication,
            out _,
            out _,
            out _));
        Assert.Equal(replayedSecondApplication, currentApplications[1]);
    }

    [Fact]
    public void PartialReplayRejectsOutOfOrderReplay()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 20);
        var first = Relation.Segment(sourceCarrier, 2, 4);
        var second = Relation.Segment(sourceCarrier, 8, 10);
        var replacement = Relation.Segment(new Carrier(), 0, 2);
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), first),
            TransformSurface.Map(first, replacement),
            TransformSurface.Ports(new Carrier(), replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var firstFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 3),
            first,
            Relation.Point(Carrier.Unknown, 0),
            replacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var secondFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 9),
            second,
            Relation.Point(Carrier.Unknown, 0),
            replacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            firstFocus,
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Frame(new Carrier(), source, replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var firstApplication));
        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            secondFocus,
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Frame(new Carrier(), source, replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var secondApplication));
        Assert.True(TransformSurface.TryReplayApplication(
            new Carrier(),
            firstApplication,
            secondFocus,
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Frame(new Carrier(), source, replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var outOfOrderReplay));

        var previousFrame = TransformSurface.ApplicationFrame(new Carrier(), firstApplication, secondApplication);
        var carriedPrefix = TransformSurface.ApplicationFrame(new Carrier(), firstApplication);

        Assert.False(TransformSurface.TryCreatePartialReplay(
            new Carrier(),
            previousFrame,
            carriedPrefix,
            [outOfOrderReplay],
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out _));
    }

    [Fact]
    public void FocusChangeDiscoversAffectedApplicationSuffix()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 20);
        var firstInput = new Placeholder();
        var firstOutput = new Placeholder();
        var secondInput = new Placeholder();
        var secondOutput = new Placeholder();
        var firstSelected = Relation.Segment(sourceCarrier, 2, 4);
        var firstReplacement = Relation.Segment(new Carrier(), 0, 2);
        var secondSelected = Relation.Segment(sourceCarrier, 8, 10);
        var secondReplacement = Relation.Segment(new Carrier(), 0, 2);
        var firstFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 3),
            firstSelected,
            Relation.Point(Carrier.Unknown, 0),
            firstReplacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var secondFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 9),
            secondSelected,
            Relation.Point(Carrier.Unknown, 0),
            secondReplacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var firstTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), firstInput),
            TransformSurface.Map(firstInput, firstOutput),
            TransformSurface.Ports(new Carrier(), firstOutput),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var secondTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), secondInput),
            TransformSurface.Map(secondInput, secondOutput),
            TransformSurface.Ports(new Carrier(), secondOutput),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            firstTransform,
            firstFocus,
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Frame(new Carrier(), source, firstReplacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var firstApplication));
        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            secondTransform,
            secondFocus,
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Frame(new Carrier(), source, secondReplacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var secondApplication));

        var changedSecondFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 11),
            Relation.Segment(sourceCarrier, 9, 13),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Segment(new Carrier(), 0, 4),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var focusResidue = new Placeholder();
        Assert.True(TransformSurface.TryCreateFocusChange(
            new Carrier(),
            secondFocus,
            changedSecondFocus,
            FocusSurface.Bind(secondSelected, Relation.Segment(sourceCarrier, 9, 13)),
            focusResidue,
            Relation.Point(Carrier.Unknown, 0),
            out var focusChange));
        var previousFrame = TransformSurface.ApplicationFrame(new Carrier(), firstApplication, secondApplication);

        Assert.True(TransformSurface.TryCreateAffectedSlice(
            new Carrier(),
            previousFrame,
            focusChange,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var affectedSlice));

        Assert.True(TransformSurface.TryReadAffectedSlice(
            affectedSlice,
            out var readPreviousFrame,
            out var readFocusChange,
            out var carriedPrefix,
            out var affectedApplications,
            out var readResidue,
            out _));
        Assert.Equal(previousFrame, readPreviousFrame);
        Assert.Equal(focusChange, readFocusChange);
        Assert.Equal(focusResidue, readResidue);
        Assert.True(TransformSurface.TryReadApplicationFrame(carriedPrefix, out var carried));
        Assert.Equal([firstApplication], carried);
        Assert.True(TransformSurface.TryReadApplicationFrame(affectedApplications, out var affected));
        Assert.Equal([secondApplication], affected);
    }

    [Fact]
    public void AffectedSliceRejectsFocusChangeNotFoundInApplicationFrame()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 10);
        var selected = Relation.Segment(sourceCarrier, 2, 4);
        var replacement = Relation.Segment(new Carrier(), 0, 2);
        var applicationFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 3),
            selected,
            Relation.Point(Carrier.Unknown, 0),
            replacement,
            Relation.Point(Carrier.Unknown, 0),
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
            applicationFocus,
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Frame(new Carrier(), source, replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var application));

        var unrelatedOldFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 7),
            Relation.Segment(sourceCarrier, 6, 8),
            Relation.Point(Carrier.Unknown, 0),
            replacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var unrelatedNewFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 8),
            Relation.Segment(sourceCarrier, 7, 9),
            Relation.Point(Carrier.Unknown, 0),
            replacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        Assert.True(TransformSurface.TryCreateFocusChange(
            new Carrier(),
            unrelatedOldFocus,
            unrelatedNewFocus,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var focusChange));

        Assert.False(TransformSurface.TryCreateAffectedSlice(
            new Carrier(),
            TransformSurface.ApplicationFrame(new Carrier(), application),
            focusChange,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out _));
    }

    [Fact]
    public void PartialReplayCanBeCreatedDirectlyFromAffectedSlice()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 20);
        var firstInput = new Placeholder();
        var firstOutput = new Placeholder();
        var secondInput = new Placeholder();
        var secondOutput = new Placeholder();
        var firstSelected = Relation.Segment(sourceCarrier, 2, 4);
        var firstReplacement = Relation.Segment(new Carrier(), 0, 2);
        var secondSelected = Relation.Segment(sourceCarrier, 8, 10);
        var secondReplacement = Relation.Segment(new Carrier(), 0, 2);
        var firstFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 3),
            firstSelected,
            Relation.Point(Carrier.Unknown, 0),
            firstReplacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var secondFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 9),
            secondSelected,
            Relation.Point(Carrier.Unknown, 0),
            secondReplacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var firstTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), firstInput),
            TransformSurface.Map(firstInput, firstOutput),
            TransformSurface.Ports(new Carrier(), firstOutput),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var secondTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), secondInput),
            TransformSurface.Map(secondInput, secondOutput),
            TransformSurface.Ports(new Carrier(), secondOutput),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            firstTransform,
            firstFocus,
            FocusSurface.Bind(firstInput, firstSelected),
            FocusSurface.Frame(new Carrier(), source, firstReplacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var firstApplication));
        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            secondTransform,
            secondFocus,
            FocusSurface.Bind(secondInput, secondSelected),
            FocusSurface.Frame(new Carrier(), source, secondReplacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var secondApplication));

        var changedSecondSelected = Relation.Segment(sourceCarrier, 9, 13);
        var changedSecondReplacement = Relation.Segment(new Carrier(), 0, 4);
        var changedSecondFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 11),
            changedSecondSelected,
            Relation.Point(Carrier.Unknown, 0),
            changedSecondReplacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        Assert.True(TransformSurface.TryCreateFocusChange(
            new Carrier(),
            secondFocus,
            changedSecondFocus,
            FocusSurface.Bind(secondSelected, changedSecondSelected),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var focusChange));
        var previousFrame = TransformSurface.ApplicationFrame(new Carrier(), firstApplication, secondApplication);
        Assert.True(TransformSurface.TryCreateAffectedSlice(
            new Carrier(),
            previousFrame,
            focusChange,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var affectedSlice));

        var replayResidue = new Placeholder();
        Assert.True(TransformSurface.TryReplayApplication(
            new Carrier(),
            secondApplication,
            changedSecondFocus,
            FocusSurface.Bind(secondInput, changedSecondSelected),
            FocusSurface.Frame(new Carrier(), source, changedSecondReplacement),
            replayResidue,
            Relation.Point(Carrier.Unknown, 0),
            out var secondReplay));

        Assert.True(TransformSurface.TryCreatePartialReplayFromAffectedSlice(
            new Carrier(),
            affectedSlice,
            [secondReplay],
            replayResidue,
            Relation.Point(Carrier.Unknown, 0),
            out var partialReplay));
        Assert.True(TransformSurface.TryReadPartialReplay(
            partialReplay,
            out _,
            out var carriedPrefix,
            out _,
            out var currentFrame,
            out var readResidue,
            out _));
        Assert.Equal(replayResidue, readResidue);
        Assert.True(TransformSurface.TryReadApplicationFrame(carriedPrefix, out var carried));
        Assert.Equal([firstApplication], carried);
        Assert.True(TransformSurface.TryReadApplicationFrame(currentFrame, out var current));
        Assert.Equal(2, current.Length);
        Assert.Equal(firstApplication, current[0]);
        Assert.True(TransformSurface.TryReadReplay(
            secondReplay,
            out _,
            out _,
            out var replayedSecond,
            out _,
            out _,
            out _));
        Assert.Equal(replayedSecond, current[1]);
    }

    [Fact]
    public void PartialReplayFromAffectedSliceRejectsReplayOutsideAffectedOrder()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 20);
        var first = Relation.Segment(sourceCarrier, 2, 4);
        var second = Relation.Segment(sourceCarrier, 8, 10);
        var replacement = Relation.Segment(new Carrier(), 0, 2);
        var transform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), first),
            TransformSurface.Map(first, replacement),
            TransformSurface.Ports(new Carrier(), replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var firstFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 3),
            first,
            Relation.Point(Carrier.Unknown, 0),
            replacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var secondFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(sourceCarrier, 9),
            second,
            Relation.Point(Carrier.Unknown, 0),
            replacement,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            firstFocus,
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Frame(new Carrier(), source, replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var firstApplication));
        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            secondFocus,
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Frame(new Carrier(), source, replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var secondApplication));
        Assert.True(TransformSurface.TryCreateFocusChange(
            new Carrier(),
            secondFocus,
            FocusSurface.Create(
                new Carrier(),
                source,
                Relation.Point(sourceCarrier, 11),
                Relation.Segment(sourceCarrier, 9, 13),
                Relation.Point(Carrier.Unknown, 0),
                replacement,
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0)),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var focusChange));
        Assert.True(TransformSurface.TryCreateAffectedSlice(
            new Carrier(),
            TransformSurface.ApplicationFrame(new Carrier(), firstApplication, secondApplication),
            focusChange,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var affectedSlice));
        Assert.True(TransformSurface.TryReplayApplication(
            new Carrier(),
            firstApplication,
            secondFocus,
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Frame(new Carrier(), source, replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var wrongReplay));

        Assert.False(TransformSurface.TryCreatePartialReplayFromAffectedSlice(
            new Carrier(),
            affectedSlice,
            [wrongReplay],
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out _));
    }

    [Fact]
    public void ApplicationDependencyDiscoversProducerOutputConsumedByFocus()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 20);
        var producerSelected = Relation.Segment(sourceCarrier, 2, 4);
        var producerReplacement = Relation.Segment(new Carrier(), 0, 2);
        var producerFocus = CreateFocus(
            source,
            Relation.Point(sourceCarrier, 3),
            producerSelected,
            Relation.Point(Carrier.Unknown, 0),
            producerReplacement,
            FocusSurface.Bind(producerSelected, producerReplacement));
        var producerTransform = CreateMapTransform(new Placeholder(), new Placeholder());
        var producerResult = FocusSurface.Frame(new Carrier(), source, producerReplacement);
        var producerApplication = CreateApplication(
            producerTransform,
            producerFocus,
            FocusSurface.BindingFrame(
                new Carrier(),
                FocusSurface.Bind(ReadInputPort(producerTransform), producerSelected),
                FocusSurface.Bind(ReadOutputPort(producerTransform), producerReplacement)),
            producerResult);

        var consumerReplacement = Relation.Segment(new Carrier(), 0, 3);
        var consumerFocus = CreateFocus(
            producerResult,
            producerReplacement,
            producerReplacement,
            Relation.Point(Carrier.Unknown, 0),
            consumerReplacement,
            FocusSurface.Bind(producerReplacement, consumerReplacement));
        var consumerTransform = CreateMapTransform(new Placeholder(), new Placeholder());
        var consumerApplication = CreateApplication(
            consumerTransform,
            consumerFocus,
            FocusSurface.Bind(ReadInputPort(consumerTransform), producerReplacement),
            FocusSurface.Frame(new Carrier(), producerResult, consumerReplacement));

        Assert.True(TransformSurface.TryCreateApplicationDependency(
            new Carrier(),
            producerApplication,
            consumerApplication,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var dependency));
        Assert.True(TransformSurface.TryReadApplicationDependency(
            dependency,
            out var readProducer,
            out var readConsumer,
            out var evidence,
            out _,
            out _));
        Assert.Equal(producerApplication, readProducer);
        Assert.Equal(consumerApplication, readConsumer);
        Assert.True(FocusSurface.TryReadBindingFrame(evidence, out var bindings));
        Assert.Contains(bindings, binding =>
            Equals(binding.Left, producerReplacement) &&
            Equals(binding.Right, producerReplacement));
    }

    [Fact]
    public void ApplicationDependencyCanBeExplicitBindingBetweenOutputAndConsumerInput()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 20);
        var producerSelected = Relation.Segment(sourceCarrier, 2, 4);
        var producerReplacement = Relation.Segment(new Carrier(), 0, 2);
        var producerTransform = CreateMapTransform(new Placeholder(), new Placeholder());
        var producerApplication = CreateApplication(
            producerTransform,
            CreateFocus(
                source,
                Relation.Point(sourceCarrier, 3),
                producerSelected,
                Relation.Point(Carrier.Unknown, 0),
                producerReplacement,
                FocusSurface.Bind(producerSelected, producerReplacement)),
            FocusSurface.Bind(ReadOutputPort(producerTransform), producerReplacement),
            FocusSurface.Frame(new Carrier(), source, producerReplacement));

        var consumerSelected = Relation.Segment(sourceCarrier, 8, 10);
        var consumerReplacement = Relation.Segment(new Carrier(), 0, 3);
        var consumerTransform = CreateMapTransform(new Placeholder(), new Placeholder());
        var consumerApplication = CreateApplication(
            consumerTransform,
            CreateFocus(
                source,
                Relation.Point(sourceCarrier, 9),
                consumerSelected,
                Relation.Point(Carrier.Unknown, 0),
                consumerReplacement,
                FocusSurface.Bind(consumerSelected, consumerReplacement)),
            FocusSurface.Bind(ReadInputPort(consumerTransform), consumerSelected),
            FocusSurface.Frame(new Carrier(), source, consumerReplacement));

        var explicitEvidence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(producerReplacement, consumerSelected));
        var dependency = TransformSurface.ApplicationDependency(
            new Carrier(),
            producerApplication,
            consumerApplication,
            explicitEvidence,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(TransformSurface.TryReadApplicationDependency(
            dependency,
            out var readProducer,
            out var readConsumer,
            out var readEvidence,
            out _,
            out _));
        Assert.Equal(producerApplication, readProducer);
        Assert.Equal(consumerApplication, readConsumer);
        Assert.Equal(explicitEvidence, readEvidence);
    }

    [Fact]
    public void ApplicationDependencyDoesNotInferFromSharedUnproducedSource()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 20);
        var firstSelected = Relation.Segment(sourceCarrier, 2, 4);
        var secondSelected = Relation.Segment(sourceCarrier, 8, 10);
        var firstReplacement = Relation.Segment(new Carrier(), 0, 2);
        var secondReplacement = Relation.Segment(new Carrier(), 0, 3);
        var firstTransform = CreateMapTransform(new Placeholder(), new Placeholder());
        var secondTransform = CreateMapTransform(new Placeholder(), new Placeholder());
        var firstApplication = CreateApplication(
            firstTransform,
            CreateFocus(
                source,
                Relation.Point(sourceCarrier, 3),
                firstSelected,
                Relation.Point(Carrier.Unknown, 0),
                firstReplacement,
                FocusSurface.Bind(firstSelected, firstReplacement)),
            FocusSurface.Bind(ReadOutputPort(firstTransform), firstReplacement),
            FocusSurface.Frame(new Carrier(), source, firstReplacement));
        var secondApplication = CreateApplication(
            secondTransform,
            CreateFocus(
                source,
                Relation.Point(sourceCarrier, 9),
                secondSelected,
                Relation.Point(Carrier.Unknown, 0),
                secondReplacement,
                FocusSurface.Bind(secondSelected, secondReplacement)),
            FocusSurface.Bind(ReadInputPort(secondTransform), secondSelected),
            FocusSurface.Frame(new Carrier(), source, secondReplacement));

        Assert.False(TransformSurface.TryCreateApplicationDependency(
            new Carrier(),
            firstApplication,
            secondApplication,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out _));
    }

    [Fact]
    public void ApplicationDependencyDiscoversProducerOutputInsideConsumerOperationInput()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 20);
        var producerSelected = Relation.Segment(sourceCarrier, 2, 4);
        var producerReplacement = Relation.Segment(new Carrier(), 0, 2);
        var producerApplication = CreateSegmentApplication(
            source,
            Relation.Point(sourceCarrier, 3),
            producerSelected,
            producerReplacement);
        var consumerSelected = Relation.Segment(sourceCarrier, 8, 10);
        var consumerReplacement = Relation.Segment(new Carrier(), 0, 3);
        var inputPort = new Placeholder();
        var outputPort = new Placeholder();
        var grade = Relation.Point(new Carrier(), 1);
        var consumerTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), inputPort),
            TransformSurface.Interpolate(producerReplacement, inputPort, grade, outputPort),
            TransformSurface.Ports(new Carrier(), outputPort),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var consumerApplication = CreateApplication(
            consumerTransform,
            CreateFocus(
                source,
                Relation.Point(sourceCarrier, 9),
                consumerSelected,
                Relation.Point(Carrier.Unknown, 0),
                consumerReplacement,
                FocusSurface.Bind(consumerSelected, consumerReplacement)),
            FocusSurface.BindingFrame(
                new Carrier(),
                FocusSurface.Bind(inputPort, consumerSelected),
                FocusSurface.Bind(outputPort, consumerReplacement)),
            FocusSurface.Frame(new Carrier(), source, consumerReplacement));

        Assert.True(TransformSurface.TryCreateApplicationDependency(
            new Carrier(),
            producerApplication,
            consumerApplication,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var dependency));
        Assert.True(TransformSurface.TryReadApplicationDependency(
            dependency,
            out _,
            out _,
            out var evidence,
            out _,
            out _));
        Assert.True(FocusSurface.TryReadBindingFrame(evidence, out var bindings));
        Assert.Contains(bindings, binding =>
            Equals(binding.Left, producerReplacement) &&
            Equals(binding.Right, producerReplacement));
    }

    [Fact]
    public void ApplicationDependencyDiscoversProducerOutputInsideConsumerCorrespondence()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 20);
        var producerSelected = Relation.Segment(sourceCarrier, 2, 4);
        var producerReplacement = Relation.Segment(new Carrier(), 0, 2);
        var producerApplication = CreateSegmentApplication(
            source,
            Relation.Point(sourceCarrier, 3),
            producerSelected,
            producerReplacement);
        var consumerSelected = Relation.Segment(sourceCarrier, 8, 10);
        var consumerReplacement = Relation.Segment(new Carrier(), 0, 3);
        var consumerTransform = CreateMapTransform(new Placeholder(), new Placeholder());
        var correspondenceSource = FocusSurface.Frame(new Carrier(), producerReplacement, consumerSelected);
        var consumerApplication = CreateApplication(
            consumerTransform,
            CreateFocus(
                source,
                Relation.Point(sourceCarrier, 9),
                consumerSelected,
                Relation.Point(Carrier.Unknown, 0),
                consumerReplacement,
                FocusSurface.BindingFrame(
                    new Carrier(),
                    FocusSurface.Bind(correspondenceSource, consumerReplacement))),
            FocusSurface.BindingFrame(
                new Carrier(),
                FocusSurface.Bind(ReadInputPort(consumerTransform), consumerSelected),
                FocusSurface.Bind(ReadOutputPort(consumerTransform), consumerReplacement)),
            FocusSurface.Frame(new Carrier(), source, consumerReplacement));

        Assert.True(TransformSurface.TryCreateApplicationDependency(
            new Carrier(),
            producerApplication,
            consumerApplication,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var dependency));
        Assert.True(TransformSurface.TryReadApplicationDependency(
            dependency,
            out _,
            out _,
            out var evidence,
            out _,
            out _));
        Assert.True(FocusSurface.TryReadBindingFrame(evidence, out var bindings));
        Assert.Contains(bindings, binding =>
            Equals(binding.Left, producerReplacement) &&
            Equals(binding.Right, producerReplacement));
    }

    [Fact]
    public void DependencyAffectedSliceCarriesIndependentLaterApplications()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 30);
        var firstSelected = Relation.Segment(sourceCarrier, 2, 4);
        var secondSelected = Relation.Segment(sourceCarrier, 10, 12);
        var thirdSelected = Relation.Segment(sourceCarrier, 20, 22);
        var secondReplacement = Relation.Segment(new Carrier(), 0, 3);
        var firstApplication = CreateSegmentApplication(
            source,
            Relation.Point(sourceCarrier, 3),
            firstSelected,
            Relation.Segment(new Carrier(), 0, 2));
        var secondApplication = CreateSegmentApplication(
            source,
            Relation.Point(sourceCarrier, 11),
            secondSelected,
            secondReplacement);
        Assert.True(TransformSurface.TryReadApplication(
            secondApplication,
            out _,
            out var secondFocus,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _));
        var thirdApplication = CreateSegmentApplication(
            source,
            Relation.Point(sourceCarrier, 21),
            thirdSelected,
            Relation.Segment(new Carrier(), 0, 4));
        var changedSecondFocus = CreateFocus(
            source,
            Relation.Point(sourceCarrier, 12),
            Relation.Segment(sourceCarrier, 10, 14),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Segment(new Carrier(), 0, 4),
            FocusSurface.Bind(secondSelected, Relation.Segment(sourceCarrier, 10, 14)));
        Assert.True(TransformSurface.TryCreateFocusChange(
            new Carrier(),
            secondFocus,
            changedSecondFocus,
            FocusSurface.Bind(secondSelected, Relation.Segment(sourceCarrier, 10, 14)),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var focusChange));

        var previousFrame = TransformSurface.ApplicationFrame(new Carrier(), firstApplication, secondApplication, thirdApplication);
        var emptyDependencies = TransformSurface.ApplicationDependencyFrame(new Carrier());

        Assert.True(TransformSurface.TryReadApplicationFrame(previousFrame, out var previousApplications));
        Assert.Equal(3, previousApplications.Length);
        Assert.True(TransformSurface.TryReadApplicationDependencyFrame(emptyDependencies, out var dependencies));
        Assert.Empty(dependencies);
        Assert.True(TransformSurface.TryReadFocusChange(
            focusChange,
            out var previousSecondFocus,
            out _,
            out _,
            out _,
            out _));
        Assert.Equal(previousSecondFocus, secondFocus);

        Assert.True(TransformSurface.TryCreateDependencyAffectedSlice(
            new Carrier(),
            previousFrame,
            emptyDependencies,
            focusChange,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var affectedSlice));
        Assert.True(TransformSurface.TryReadDependencyAffectedSlice(
            affectedSlice,
            out var readPrevious,
            out _,
            out var dependencyFrame,
            out var carriedApplications,
            out var affectedApplications,
            out _,
            out _));
        Assert.Equal(previousFrame, readPrevious);
        Assert.Equal(emptyDependencies, dependencyFrame);
        Assert.True(TransformSurface.TryReadApplicationFrame(carriedApplications, out var carried));
        Assert.True(TransformSurface.TryReadApplicationFrame(affectedApplications, out var affected));
        Assert.Equal([firstApplication, thirdApplication], carried);
        Assert.Equal([secondApplication], affected);
    }

    [Fact]
    public void DependencyAffectedSliceIncludesTransitiveConsumers()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 40);
        var firstSelected = Relation.Segment(sourceCarrier, 2, 4);
        var secondSelected = Relation.Segment(sourceCarrier, 12, 14);
        var thirdSelected = Relation.Segment(sourceCarrier, 22, 24);
        var fourthSelected = Relation.Segment(sourceCarrier, 32, 34);
        var firstReplacement = Relation.Segment(new Carrier(), 0, 2);
        var secondReplacement = Relation.Segment(new Carrier(), 0, 3);
        var thirdReplacement = Relation.Segment(new Carrier(), 0, 4);
        var firstApplication = CreateSegmentApplication(source, Relation.Point(sourceCarrier, 3), firstSelected, firstReplacement);
        Assert.True(TransformSurface.TryReadApplication(
            firstApplication,
            out _,
            out var firstFocus,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _));
        var secondApplication = CreateSegmentApplication(source, Relation.Point(sourceCarrier, 13), secondSelected, secondReplacement);
        var thirdApplication = CreateSegmentApplication(source, Relation.Point(sourceCarrier, 23), thirdSelected, thirdReplacement);
        var fourthApplication = CreateSegmentApplication(
            source,
            Relation.Point(sourceCarrier, 33),
            fourthSelected,
            Relation.Segment(new Carrier(), 0, 5));
        var firstToSecond = TransformSurface.ApplicationDependency(
            new Carrier(),
            firstApplication,
            secondApplication,
            FocusSurface.Bind(firstReplacement, secondSelected),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var secondToThird = TransformSurface.ApplicationDependency(
            new Carrier(),
            secondApplication,
            thirdApplication,
            FocusSurface.Bind(secondReplacement, thirdSelected),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var dependencyFrame = TransformSurface.ApplicationDependencyFrame(new Carrier(), firstToSecond, secondToThird);
        var changedFirstFocus = CreateFocus(
            source,
            Relation.Point(sourceCarrier, 4),
            Relation.Segment(sourceCarrier, 2, 6),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Segment(new Carrier(), 0, 4),
            FocusSurface.Bind(firstSelected, Relation.Segment(sourceCarrier, 2, 6)));
        Assert.True(TransformSurface.TryCreateFocusChange(
            new Carrier(),
            firstFocus,
            changedFirstFocus,
            FocusSurface.Bind(firstSelected, Relation.Segment(sourceCarrier, 2, 6)),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var focusChange));

        Assert.True(TransformSurface.TryCreateDependencyAffectedSlice(
            new Carrier(),
            TransformSurface.ApplicationFrame(new Carrier(), firstApplication, secondApplication, thirdApplication, fourthApplication),
            dependencyFrame,
            focusChange,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var affectedSlice));
        Assert.True(TransformSurface.TryReadDependencyAffectedSlice(
            affectedSlice,
            out _,
            out _,
            out _,
            out var carriedApplications,
            out var affectedApplications,
            out _,
            out _));
        Assert.True(TransformSurface.TryReadApplicationFrame(carriedApplications, out var carried));
        Assert.True(TransformSurface.TryReadApplicationFrame(affectedApplications, out var affected));
        Assert.Equal([fourthApplication], carried);
        Assert.Equal([firstApplication, secondApplication, thirdApplication], affected);
    }

    [Fact]
    public void PartialReplayFromDependencyAffectedSliceCarriesIndependentApplications()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 30);
        var firstApplication = CreateSegmentApplication(
            source,
            Relation.Point(sourceCarrier, 3),
            Relation.Segment(sourceCarrier, 2, 4),
            Relation.Segment(new Carrier(), 0, 2));
        var secondSelected = Relation.Segment(sourceCarrier, 10, 12);
        var secondReplacement = Relation.Segment(new Carrier(), 0, 3);
        var secondApplication = CreateSegmentApplication(source, Relation.Point(sourceCarrier, 11), secondSelected, secondReplacement);
        Assert.True(TransformSurface.TryReadApplication(
            secondApplication,
            out _,
            out var secondFocus,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _));
        var thirdApplication = CreateSegmentApplication(
            source,
            Relation.Point(sourceCarrier, 21),
            Relation.Segment(sourceCarrier, 20, 22),
            Relation.Segment(new Carrier(), 0, 4));
        var changedSecondSelected = Relation.Segment(sourceCarrier, 10, 14);
        var changedSecondReplacement = Relation.Segment(new Carrier(), 0, 4);
        var changedSecondFocus = CreateFocus(
            source,
            Relation.Point(sourceCarrier, 12),
            changedSecondSelected,
            Relation.Point(Carrier.Unknown, 0),
            changedSecondReplacement,
            FocusSurface.Bind(secondSelected, changedSecondSelected));
        Assert.True(TransformSurface.TryCreateFocusChange(
            new Carrier(),
            secondFocus,
            changedSecondFocus,
            FocusSurface.Bind(secondSelected, changedSecondSelected),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var focusChange));
        var previousFrame = TransformSurface.ApplicationFrame(new Carrier(), firstApplication, secondApplication, thirdApplication);
        Assert.True(TransformSurface.TryCreateDependencyAffectedSlice(
            new Carrier(),
            previousFrame,
            TransformSurface.ApplicationDependencyFrame(new Carrier()),
            focusChange,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var affectedSlice));
        Assert.True(TransformSurface.TryReplayApplication(
            new Carrier(),
            secondApplication,
            changedSecondFocus,
            FocusSurface.Bind(secondSelected, changedSecondSelected),
            FocusSurface.Frame(new Carrier(), source, changedSecondReplacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var secondReplay));

        Assert.True(TransformSurface.TryCreatePartialReplayFromDependencyAffectedSlice(
            new Carrier(),
            affectedSlice,
            [secondReplay],
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var partialReplay));
        Assert.True(TransformSurface.TryReadPartialReplay(
            partialReplay,
            out _,
            out var carriedApplications,
            out _,
            out var currentApplications,
            out _,
            out _));
        Assert.True(TransformSurface.TryReadApplicationFrame(carriedApplications, out var carried));
        Assert.True(TransformSurface.TryReadApplicationFrame(currentApplications, out var current));
        Assert.True(TransformSurface.TryReadReplay(
            secondReplay,
            out _,
            out _,
            out var replayedSecond,
            out _,
            out _,
            out _));
        Assert.Equal([firstApplication, thirdApplication], carried);
        Assert.Equal([firstApplication, replayedSecond, thirdApplication], current);
    }

    [Fact]
    public void PartialReplayFromDependencyAffectedSliceRejectsReplayOutsideAffectedOrder()
    {
        var sourceCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 20);
        var firstSelected = Relation.Segment(sourceCarrier, 2, 4);
        var secondSelected = Relation.Segment(sourceCarrier, 12, 14);
        var firstReplacement = Relation.Segment(new Carrier(), 0, 2);
        var secondReplacement = Relation.Segment(new Carrier(), 0, 3);
        var firstApplication = CreateSegmentApplication(source, Relation.Point(sourceCarrier, 3), firstSelected, firstReplacement);
        Assert.True(TransformSurface.TryReadApplication(
            firstApplication,
            out _,
            out var firstFocus,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _));
        var secondApplication = CreateSegmentApplication(source, Relation.Point(sourceCarrier, 13), secondSelected, secondReplacement);
        var dependency = TransformSurface.ApplicationDependency(
            new Carrier(),
            firstApplication,
            secondApplication,
            FocusSurface.Bind(firstReplacement, secondSelected),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var changedFirstSelected = Relation.Segment(sourceCarrier, 2, 6);
        var changedFirstFocus = CreateFocus(
            source,
            Relation.Point(sourceCarrier, 4),
            changedFirstSelected,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Segment(new Carrier(), 0, 4),
            FocusSurface.Bind(firstSelected, changedFirstSelected));
        Assert.True(TransformSurface.TryCreateFocusChange(
            new Carrier(),
            firstFocus,
            changedFirstFocus,
            FocusSurface.Bind(firstSelected, changedFirstSelected),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var focusChange));
        Assert.True(TransformSurface.TryCreateDependencyAffectedSlice(
            new Carrier(),
            TransformSurface.ApplicationFrame(new Carrier(), firstApplication, secondApplication),
            TransformSurface.ApplicationDependencyFrame(new Carrier(), dependency),
            focusChange,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var affectedSlice));
        Assert.True(TransformSurface.TryReplayApplication(
            new Carrier(),
            secondApplication,
            CreateFocus(
                source,
                Relation.Point(sourceCarrier, 15),
                Relation.Segment(sourceCarrier, 12, 18),
                Relation.Point(Carrier.Unknown, 0),
                Relation.Segment(new Carrier(), 0, 6),
                FocusSurface.Bind(secondSelected, Relation.Segment(sourceCarrier, 12, 18))),
            FocusSurface.Bind(secondSelected, Relation.Segment(sourceCarrier, 12, 18)),
            FocusSurface.Frame(new Carrier(), source, Relation.Segment(new Carrier(), 0, 6)),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var secondReplay));

        Assert.False(TransformSurface.TryCreatePartialReplayFromDependencyAffectedSlice(
            new Carrier(),
            affectedSlice,
            [secondReplay],
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out _));
    }

    [Fact]
    public void DependencyPartialReplayFollowsOperationOutputProductTermConsumer()
    {
        var lengthShape = new Carrier().UnitShape;
        var x = new Carrier(lengthShape);
        var y = new Carrier(lengthShape);
        var area = new Carrier(UnitShape.Product(lengthShape, lengthShape));
        var xPort = new Relation(Relation.Point(x, 1), Relation.Point(Carrier.Unknown, 1));
        var yPort = new Relation(Relation.Point(y, 1), Relation.Point(Carrier.Unknown, 1));
        var outputSurface = Relation.Segment(area, 0, 1);
        var producerTransform = TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), xPort, yPort),
            new Relation(new Relation(xPort, yPort), outputSurface),
            TransformSurface.Ports(new Carrier(), outputSurface),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var xFused = new Relation(Relation.Point(x, 3), Relation.Point(x.Inverse(), 2));
        var yFused = new Relation(
            new CarrierLocation(y.Inverse(), 4),
            new CarrierLocation(y, 5));
        var producerSource = FocusSurface.Frame(new Carrier(), xFused, yFused);
        var consumedOutput = Relation.Point(area.Inverse(), 12);
        var producerApplication = CreateApplication(
            producerTransform,
            CreateFocus(
                producerSource,
                xFused,
                producerSource,
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0),
                Relation.Point(Carrier.Unknown, 0)),
            FocusSurface.BindingFrame(
                new Carrier(),
                FocusSurface.Bind(xPort, xFused),
                FocusSurface.Bind(yPort, yFused)),
            FocusSurface.Frame(new Carrier(), producerSource, consumedOutput));
        Assert.True(TransformSurface.TryReadApplication(
            producerApplication,
            out _,
            out var producerFocus,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _));

        var consumerInputPort = new Placeholder();
        var consumerOutputPort = new Placeholder();
        var consumerTransform = CreateMapTransform(consumerInputPort, consumerOutputPort);
        var consumerReplacement = Relation.Point(new Carrier(), 1);
        var consumerApplication = CreateApplication(
            consumerTransform,
            CreateFocus(
                consumedOutput,
                consumedOutput,
                consumedOutput,
                Relation.Point(Carrier.Unknown, 0),
                consumerReplacement,
                FocusSurface.Bind(consumedOutput, consumerReplacement)),
            FocusSurface.Bind(consumerInputPort, consumedOutput),
            FocusSurface.Frame(new Carrier(), consumedOutput, consumerReplacement));
        var independentApplication = CreateSegmentApplication(
            Relation.Segment(new Carrier(), 0, 20),
            Relation.Point(new Carrier(), 2),
            Relation.Segment(new Carrier(), 1, 3),
            Relation.Segment(new Carrier(), 0, 2));
        Assert.True(TransformSurface.TryCreateApplicationDependency(
            new Carrier(),
            producerApplication,
            consumerApplication,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var dependency));

        var newXFused = new Relation(Relation.Point(x, 4), Relation.Point(x.Inverse(), 2));
        var newProducerSource = FocusSurface.Frame(new Carrier(), newXFused, yFused);
        var newConsumedOutput = Relation.Point(area.Inverse(), 16);
        var changedProducerFocus = CreateFocus(
            newProducerSource,
            newXFused,
            newProducerSource,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Bind(xFused, newXFused));
        Assert.True(TransformSurface.TryCreateFocusChange(
            new Carrier(),
            producerFocus,
            changedProducerFocus,
            FocusSurface.Bind(xFused, newXFused),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var focusChange));
        var previousFrame = TransformSurface.ApplicationFrame(
            new Carrier(),
            producerApplication,
            consumerApplication,
            independentApplication);
        var dependencyFrame = TransformSurface.ApplicationDependencyFrame(new Carrier(), dependency);

        Assert.True(TransformSurface.TryCreateDependencyAffectedSlice(
            new Carrier(),
            previousFrame,
            dependencyFrame,
            focusChange,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var affectedSlice));
        Assert.True(TransformSurface.TryReadDependencyAffectedSlice(
            affectedSlice,
            out _,
            out _,
            out _,
            out var carriedApplications,
            out var affectedApplications,
            out _,
            out _));
        Assert.True(TransformSurface.TryReadApplicationFrame(carriedApplications, out var carried));
        Assert.True(TransformSurface.TryReadApplicationFrame(affectedApplications, out var affected));
        Assert.Equal([independentApplication], carried);
        Assert.Equal([producerApplication, consumerApplication], affected);

        Assert.True(TransformSurface.TryReplayApplication(
            new Carrier(),
            producerApplication,
            changedProducerFocus,
            FocusSurface.BindingFrame(
                new Carrier(),
                FocusSurface.Bind(xPort, newXFused),
                FocusSurface.Bind(yPort, yFused)),
            FocusSurface.Frame(new Carrier(), newProducerSource, newConsumedOutput),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var producerReplay));
        var newConsumerReplacement = Relation.Point(new Carrier(), 2);
        var changedConsumerFocus = CreateFocus(
            newConsumedOutput,
            newConsumedOutput,
            newConsumedOutput,
            Relation.Point(Carrier.Unknown, 0),
            newConsumerReplacement,
            FocusSurface.Bind(consumedOutput, newConsumedOutput));
        Assert.True(TransformSurface.TryReplayApplication(
            new Carrier(),
            consumerApplication,
            changedConsumerFocus,
            FocusSurface.Bind(consumerInputPort, newConsumedOutput),
            FocusSurface.Frame(new Carrier(), newConsumedOutput, newConsumerReplacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var consumerReplay));

        Assert.True(TransformSurface.TryCreatePartialReplayFromDependencyAffectedSlice(
            new Carrier(),
            affectedSlice,
            [producerReplay, consumerReplay],
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var partialReplay));
        Assert.True(TransformSurface.TryReadPartialReplay(
            partialReplay,
            out _,
            out var replayCarriedApplications,
            out _,
            out var currentApplications,
            out _,
            out _));
        Assert.True(TransformSurface.TryReadApplicationFrame(replayCarriedApplications, out var replayCarried));
        Assert.True(TransformSurface.TryReadApplicationFrame(currentApplications, out var current));
        Assert.True(TransformSurface.TryReadReplay(
            producerReplay,
            out _,
            out _,
            out var replayedProducer,
            out _,
            out _,
            out _));
        Assert.True(TransformSurface.TryReadReplay(
            consumerReplay,
            out _,
            out _,
            out var replayedConsumer,
            out _,
            out _,
            out _));
        Assert.Equal([independentApplication], replayCarried);
        Assert.Equal([replayedProducer, replayedConsumer, independentApplication], current);
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

    private static Relation CreateSegmentApplication(
        IElement source,
        IElement focusBasis,
        IElement selected,
        IElement replacement)
    {
        var transform = CreateMapTransform(new Placeholder(), new Placeholder());
        return CreateApplication(
            transform,
            CreateFocus(
                source,
                focusBasis,
                selected,
                Relation.Point(Carrier.Unknown, 0),
                replacement,
                FocusSurface.Bind(selected, replacement)),
            FocusSurface.BindingFrame(
                new Carrier(),
                FocusSurface.Bind(ReadInputPort(transform), selected),
                FocusSurface.Bind(ReadOutputPort(transform), replacement)),
            FocusSurface.Frame(new Carrier(), source, replacement));
    }

    private static Relation CreateMapTransform(IElement inputPort, IElement outputPort) =>
        TransformSurface.Create(
            new Carrier(),
            TransformSurface.Ports(new Carrier(), inputPort),
            TransformSurface.Map(inputPort, outputPort),
            TransformSurface.Ports(new Carrier(), outputPort),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

    private static Relation CreateFocus(
        IElement source,
        IElement focusBasis,
        IElement participation,
        IElement complement,
        IElement replacement,
        IElement correspondence) =>
        FocusSurface.Create(
            new Carrier(),
            source,
            focusBasis,
            participation,
            complement,
            replacement,
            correspondence,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

    private static Relation CreateApplication(
        Relation transform,
        Relation focus,
        IElement bindings,
        IElement result)
    {
        Assert.True(TransformSurface.TryCreateApplication(
            new Carrier(),
            transform,
            focus,
            bindings,
            result,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var application));
        return application;
    }

    private static IElement ReadInputPort(Relation transform)
    {
        Assert.True(TransformSurface.TryRead(transform, out var inputPorts, out _, out _, out _, out _));
        Assert.True(TransformSurface.TryReadPorts(inputPorts, out var ports));
        return ports[0];
    }

    private static IElement ReadOutputPort(Relation transform)
    {
        Assert.True(TransformSurface.TryRead(transform, out _, out _, out var outputPorts, out _, out _));
        Assert.True(TransformSurface.TryReadPorts(outputPorts, out var ports));
        return ports[0];
    }
}

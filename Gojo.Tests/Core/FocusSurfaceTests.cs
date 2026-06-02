using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class FocusSurfaceTests
{
    [Fact]
    public void PartialSpanReadsWholeSelectedComplementAndResidue()
    {
        var edge = new Carrier();
        var whole = Relation.Segment(edge, 0, 12);
        var selected = Relation.Segment(edge, 8, 12);
        var complement = Relation.Segment(edge, 0, 8);
        var residue = new Placeholder();

        var partial = FocusSurface.PartialSpan(
            new Carrier(),
            whole,
            selected,
            complement,
            residue);

        Assert.True(FocusSurface.TryReadPartialSpan(
            partial,
            out var readWhole,
            out var readSelected,
            out var readComplement,
            out var readResidue));
        Assert.Equal(whole, readWhole);
        Assert.Equal(selected, readSelected);
        Assert.Equal(complement, readComplement);
        Assert.Equal(residue, readResidue);
    }

    [Fact]
    public void FocusSurfaceCanSelectSubspansAcrossACornerAndBindReplacement()
    {
        var horizontal = new Carrier();
        var vertical = new Carrier();
        var arcCarrier = new Carrier();

        var horizontalEdge = Relation.Segment(horizontal, 0, 10);
        var verticalEdge = Relation.Segment(vertical, 0, 8);
        var source = FocusSurface.Frame(new Carrier(), horizontalEdge, verticalEdge);

        var cornerJoin = new Relation(
            Relation.Point(horizontal, 10),
            Relation.Point(vertical, 0));
        var horizontalSelected = FocusSurface.PartialSpan(
            new Carrier(),
            horizontalEdge,
            Relation.Segment(horizontal, 7, 10),
            Relation.Segment(horizontal, 0, 7),
            Relation.Point(Carrier.Unknown, 0));
        var verticalSelected = FocusSurface.PartialSpan(
            new Carrier(),
            verticalEdge,
            Relation.Segment(vertical, 0, 3),
            Relation.Segment(vertical, 3, 8),
            Relation.Point(Carrier.Unknown, 0));
        var participation = FocusSurface.Frame(
            new Carrier(),
            horizontalSelected,
            verticalSelected);
        var complement = FocusSurface.Frame(
            new Carrier(),
            Relation.Segment(horizontal, 0, 7),
            Relation.Segment(vertical, 3, 8));

        var roundedArc = Relation.Segment(arcCarrier, 0, 4);
        var replacementBinding = FocusSurface.Bind(participation, roundedArc);
        var replacement = FocusSurface.BindingFrame(new Carrier(), replacementBinding);
        var firstTangent = FocusSurface.Bind(
            Relation.Point(horizontal, 7),
            Relation.Point(arcCarrier, 0));
        var secondTangent = FocusSurface.Bind(
            Relation.Point(vertical, 3),
            Relation.Point(arcCarrier, 4));
        var correspondence = FocusSurface.BindingFrame(new Carrier(), firstTangent, secondTangent);
        var residue = new Placeholder();

        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            cornerJoin,
            participation,
            complement,
            replacement,
            correspondence,
            residue,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(FocusSurface.TryRead(
            focus,
            out var readSource,
            out var readBasis,
            out var readParticipation,
            out var readComplement,
            out var readReplacement,
            out var readCorrespondence,
            out var readResidue,
            out _));
        Assert.Equal(source, readSource);
        Assert.Equal(cornerJoin, readBasis);
        Assert.Equal(participation, readParticipation);
        Assert.Equal(complement, readComplement);
        Assert.Equal(residue, readResidue);

        Assert.True(FocusSurface.TryReadBindingFrame(readReplacement, out var replacements));
        Assert.Equal([replacementBinding], replacements);
        Assert.True(FocusSurface.TryReadBindingFrame(readCorrespondence, out var correspondences));
        Assert.Equal([firstTangent, secondTangent], correspondences);
    }

    [Fact]
    public void FocusSurfaceReadRejectsOverfullOrderedSurface()
    {
        var source = Relation.Segment(new Carrier(), 0, 10);
        var focusBasis = Relation.Point(new Carrier(), 5);
        var participation = Relation.Segment(new Carrier(), 4, 6);
        var unresolved = Relation.Point(Carrier.Unknown, 0);
        var overfull = Relation.OrderedGroup(
            new Carrier(),
            source,
            focusBasis,
            participation,
            unresolved,
            unresolved,
            unresolved,
            unresolved,
            unresolved,
            Relation.Point(new Carrier(), 1));

        Assert.False(FocusSurface.TryRead(
            overfull,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _));
    }

    [Fact]
    public void FocusStepUsesTimeStepSequenceForOrderedPartialStates()
    {
        var clock = TimeStepSequence.CreateClock(new Carrier());
        var boundary = new Carrier();
        var sharpRectangle = FocusSurface.Frame(
            new Carrier(),
            Relation.Segment(boundary, 0, 10),
            Relation.Segment(boundary, 10, 20),
            Relation.Segment(boundary, 20, 30),
            Relation.Segment(boundary, 30, 40));
        var cornerA = Relation.Point(boundary, 10);
        var cornerB = Relation.Point(boundary, 20);
        var cornerAFocus = FocusSurface.Create(
            new Carrier(),
            sharpRectangle,
            cornerA,
            Relation.Segment(boundary, 8, 12));
        var afterCornerA = FocusSurface.Frame(
            new Carrier(),
            sharpRectangle,
            Relation.Segment(new Carrier(), 0, 4));
        var cornerBFocus = FocusSurface.Create(
            new Carrier(),
            afterCornerA,
            cornerB,
            Relation.Segment(boundary, 18, 22));
        var afterCornerB = FocusSurface.Frame(
            new Carrier(),
            afterCornerA,
            Relation.Segment(new Carrier(), 0, 4));

        var first = FocusSurface.Step(
            new Carrier(),
            clock,
            0,
            1,
            sharpRectangle,
            cornerAFocus,
            afterCornerA,
            Relation.Point(Carrier.Unknown, 0));
        var second = FocusSurface.Step(
            new Carrier(),
            clock,
            1,
            2,
            afterCornerA,
            cornerBFocus,
            afterCornerB,
            Relation.Point(Carrier.Unknown, 0));
        var timeline = FocusSurface.Timeline(new Carrier(), clock, sharpRectangle, first, second);

        Assert.True(TimeStepSequence.TryRead(timeline, out var origin, out var initial, out var steps));
        Assert.Equal(new CarrierLocation(clock, 0), origin);
        Assert.Equal(sharpRectangle, initial);
        Assert.Equal([first, second], steps);
        Assert.True(TimeStepSequence.TryReadStep(
            first,
            out _,
            out _,
            out _,
            out var before,
            out var transition,
            out var after,
            out _));
        Assert.Equal(sharpRectangle, before);
        Assert.Equal(cornerAFocus, transition);
        Assert.Equal(afterCornerA, after);
        Assert.True(FocusSurface.TryReadStep(
            first,
            out var focusSpan,
            out var focusStart,
            out var focusEnd,
            out var focusBefore,
            out var readFocus,
            out var focusAfter,
            out var focusResidue));
        Assert.Equal(Relation.Segment(clock, 0, 1), focusSpan);
        Assert.Equal(new CarrierLocation(clock, 0), focusStart);
        Assert.Equal(new CarrierLocation(clock, 1), focusEnd);
        Assert.Equal(sharpRectangle, focusBefore);
        Assert.Equal(cornerAFocus, readFocus);
        Assert.Equal(afterCornerA, focusAfter);
        Assert.Equal(Relation.Point(Carrier.Unknown, 0), focusResidue);
    }

    [Fact]
    public void FocusStepReadRejectsNonFocusTransition()
    {
        var clock = TimeStepSequence.CreateClock(new Carrier());
        var source = Relation.Segment(new Carrier(), 0, 1);
        var transition = Relation.Segment(new Carrier(), 1, 2);
        var step = TimeStepSequence.Step(
            new Carrier(),
            clock,
            0,
            1,
            source,
            transition,
            Relation.Point(new Carrier(), 2),
            Relation.Point(Carrier.Unknown, 0));

        Assert.False(FocusSurface.TryReadStep(
            step,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _,
            out _));
    }

    [Fact]
    public void FocusSurfaceCanCarryTimelineAndProjectToReadWitness()
    {
        var clock = TimeStepSequence.CreateClock(new Carrier());
        var source = Relation.Segment(new Carrier(), 0, 10);
        var basis = Relation.Point(new Carrier(), 4);
        var participation = Relation.Segment(new Carrier(), 3, 5);
        var residue = new Placeholder();
        var step = FocusSurface.Step(
            new Carrier(),
            clock,
            0,
            1,
            source,
            basis,
            participation,
            residue);
        var timeline = FocusSurface.Timeline(new Carrier(), clock, source, step);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            basis,
            participation,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            residue,
            timeline);

        Assert.True(FocusSurface.TryReadTimeline(focus, out var readTimeline));
        Assert.Equal(timeline, readTimeline);
        Assert.True(FocusSurface.TryReadWitness(focus, new Carrier(), out var witness));
        Assert.True(ReadWitness.TryRead(
            witness,
            out var readSelected,
            out var readSurface,
            out var result,
            out var readResidue,
            out var provenance));
        Assert.Equal(source, readSelected);
        Assert.Equal(basis, readSurface);
        Assert.Equal(participation, result);
        Assert.Equal(residue, readResidue);
        Assert.Equal(timeline, provenance);
    }

    [Fact]
    public void FocusCanProjectAcrossRungsThroughCorrespondence()
    {
        var coarse = new Carrier();
        var detailed = new Carrier();
        var coarseHorizontal = Relation.Segment(coarse, 0, 10);
        var coarseVertical = Relation.Segment(coarse, 10, 20);
        var coarseSource = FocusSurface.Frame(new Carrier(), coarseHorizontal, coarseVertical);
        var coarseCorner = Relation.Point(coarse, 10);
        var coarseSelectedSpan = Relation.Segment(coarse, 8, 12);
        var coarseCarry = FocusSurface.Frame(
            new Carrier(),
            Relation.Segment(coarse, 0, 8),
            Relation.Segment(coarse, 12, 20));
        var coarseFocus = FocusSurface.Create(
            new Carrier(),
            coarseSource,
            coarseCorner,
            coarseSelectedSpan,
            coarseCarry,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        var detailedHorizontal = Relation.Segment(detailed, 0, 30);
        var detailedVertical = Relation.Segment(detailed, 30, 55);
        var detailedSource = FocusSurface.Frame(new Carrier(), detailedHorizontal, detailedVertical);
        var detailedCorner = Relation.Point(detailed, 30);
        var detailedSelectedSpan = Relation.Segment(detailed, 24, 36);
        var detailedCarry = FocusSurface.Frame(
            new Carrier(),
            Relation.Segment(detailed, 0, 24),
            Relation.Segment(detailed, 36, 55));
        var correspondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(coarseCorner, detailedCorner),
            FocusSurface.Bind(coarseSelectedSpan, detailedSelectedSpan),
            FocusSurface.Bind(coarseCarry, detailedCarry));
        var residue = new Placeholder();

        Assert.True(FocusSurface.TryProject(
            coarseFocus,
            new Carrier(),
            detailedSource,
            correspondence,
            residue,
            out var projected,
            out var projection));

        Assert.True(FocusSurface.TryRead(
            projected,
            out var readSource,
            out var readBasis,
            out var readParticipation,
            out var readComplement,
            out _,
            out var readCorrespondence,
            out var readResidue,
            out var readProvenance));
        Assert.Equal(detailedSource, readSource);
        Assert.Equal(detailedCorner, readBasis);
        Assert.Equal(detailedSelectedSpan, readParticipation);
        Assert.Equal(detailedCarry, readComplement);
        Assert.Equal(correspondence, readCorrespondence);
        Assert.Equal(residue, readResidue);
        Assert.True(FocusSurface.TryReadFrame(readProvenance, out var provenanceParts));
        Assert.Equal([coarseFocus, correspondence, Relation.Point(Carrier.Unknown, 0)], provenanceParts);

        Assert.True(FocusSurface.TryReadProjection(
            projection,
            out var sourceFocus,
            out var targetSource,
            out var projectionCorrespondence,
            out var projectedFocus,
            out var projectionResidue));
        Assert.Equal(coarseFocus, sourceFocus);
        Assert.Equal(detailedSource, targetSource);
        Assert.Equal(correspondence, projectionCorrespondence);
        Assert.Equal(projected, projectedFocus);
        Assert.Equal(residue, projectionResidue);
    }

    [Fact]
    public void FocusProjectionPreservesUnmappedParticipationAsResidueBearingStructure()
    {
        var sourceCarrier = new Carrier();
        var targetCarrier = new Carrier();
        var source = Relation.Segment(sourceCarrier, 0, 10);
        var sourceBasis = Relation.Point(sourceCarrier, 5);
        var sourceParticipation = Relation.Segment(sourceCarrier, 4, 6);
        var targetSource = Relation.Segment(targetCarrier, 0, 20);
        var targetBasis = Relation.Point(targetCarrier, 10);
        var correspondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(sourceBasis, targetBasis));
        var unresolved = new Placeholder();
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            sourceBasis,
            sourceParticipation,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            unresolved,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(FocusSurface.TryProject(
            focus,
            new Carrier(),
            targetSource,
            correspondence,
            unresolved,
            out var projected,
            out _));

        Assert.True(FocusSurface.TryRead(
            projected,
            out _,
            out var readBasis,
            out var readParticipation,
            out _,
            out _,
            out _,
            out var readResidue,
            out _));
        Assert.Equal(targetBasis, readBasis);
        Assert.Equal(sourceParticipation, readParticipation);
        Assert.Equal(unresolved, readResidue);
    }

    [Fact]
    public void FocusCanCreateResultWitness()
    {
        var source = Relation.Segment(new Carrier(), 0, 10);
        var focusBasis = Relation.Point(new Carrier(), 5);
        var participation = Relation.Segment(new Carrier(), 4, 6);
        var result = FocusSurface.Frame(new Carrier(), source, Relation.Segment(new Carrier(), 0, 3));
        var residue = new Placeholder();
        var provenance = Relation.Point(new Carrier(), 1);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            focusBasis,
            participation,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            residue,
            provenance);

        Assert.True(FocusSurface.TryCreateResultWitness(
            new Carrier(),
            focus,
            result,
            residue,
            provenance,
            out var witness));
        Assert.True(FocusSurface.TryReadResultWitness(
            witness,
            out var readFocus,
            out var readSource,
            out var readResult,
            out var readResidue,
            out var readProvenance));
        Assert.Equal(focus, readFocus);
        Assert.Equal(source, readSource);
        Assert.Equal(result, readResult);
        Assert.Equal(residue, readResidue);
        Assert.Equal(provenance, readProvenance);
    }

    [Fact]
    public void FocusCanCreateAndReadRefinement()
    {
        var source = Relation.Segment(new Carrier(), 0, 10);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(new Carrier(), 5),
            Relation.Segment(new Carrier(), 4, 6));
        var result = FocusSurface.Frame(new Carrier(), source, Relation.Segment(new Carrier(), 0, 3));
        var residue = new Placeholder();
        var provenance = Relation.Point(new Carrier(), 1);

        Assert.True(FocusSurface.TryCreateRefinementRead(
            new Carrier(),
            focus,
            result,
            residue,
            provenance,
            out var refinement));

        Assert.True(FocusSurface.TryReadRefinement(
            refinement,
            out var readFocus,
            out var readResult,
            out var witness,
            out var readResidue,
            out var readProvenance));
        Assert.Equal(focus, readFocus);
        Assert.Equal(result, readResult);
        Assert.Equal(residue, readResidue);
        Assert.Equal(provenance, readProvenance);
        Assert.True(FocusSurface.TryReadResultWitness(
            witness,
            out var witnessFocus,
            out _,
            out var witnessResult,
            out _,
            out _));
        Assert.Equal(focus, witnessFocus);
        Assert.Equal(result, witnessResult);
    }

    [Fact]
    public void FocusRefinementReadRejectsMismatchedWitnessResult()
    {
        var source = Relation.Segment(new Carrier(), 0, 10);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(new Carrier(), 5),
            Relation.Segment(new Carrier(), 4, 6));
        var result = Relation.Point(new Carrier(), 1);
        var differentResult = Relation.Point(new Carrier(), 2);
        var residue = new Placeholder();
        Assert.True(FocusSurface.TryCreateResultWitness(
            new Carrier(),
            focus,
            differentResult,
            residue,
            Relation.Point(Carrier.Unknown, 0),
            out var witness));
        var refinement = RefinementRead.Create(
            new Carrier(),
            focus,
            result,
            witness,
            residue,
            Relation.Point(Carrier.Unknown, 0));

        Assert.False(FocusSurface.TryReadRefinement(
            refinement,
            out _,
            out _,
            out _,
            out _,
            out _));
    }

    [Fact]
    public void RepeatTemplateCanUseFocusSurfaceAsSelectedStructure()
    {
        var input = new Placeholder();
        var output = new Placeholder();
        var source = Relation.Segment(new Carrier(), 0, 10);
        var selected = Relation.Segment(new Carrier(), 2, 4);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            input,
            selected,
            Relation.Point(Carrier.Unknown, 0),
            FocusSurface.Bind(selected, output),
            FocusSurface.Bind(input, output),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var ports = RepeatSurface.Ports(new Carrier(), input, output);

        var template = RepeatSurface.Template(new Carrier(), ports, focus, ports);

        Assert.True(RepeatSurface.TryReadTemplate(
            template,
            out var readInputs,
            out var readSelected,
            out var readOutputs));
        Assert.Equal(ports, readInputs);
        Assert.Equal(focus, readSelected);
        Assert.Equal(ports, readOutputs);
    }
}

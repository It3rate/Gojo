using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class StructuralLadderSurfaceTests
{
    [Fact]
    public void BaseRungReadsAsCompleteStructureWithoutFocus()
    {
        var rectangle = Relation.OrderedGroup(
            new Carrier(),
            Relation.Segment(new Carrier(), 0, 10),
            Relation.Segment(new Carrier(), 10, 20));

        var rung = StructuralLadderSurface.BaseRung(new Carrier(), rectangle);

        Assert.True(StructuralLadderSurface.TryReadRung(
            rung,
            out var source,
            out _,
            out var result,
            out _,
            out _,
            out var witness,
            out var refinement,
            out _));
        Assert.Equal(rectangle, source);
        Assert.Equal(rectangle, result);
        Assert.False(ReadWitness.TryRead(witness, out _));
        Assert.False(RefinementRead.TryRead(
            Assert.IsType<Relation>(refinement),
            out _,
            out _,
            out _,
            out _,
            out _));
    }

    [Fact]
    public void RefinedRungCarriesFocusWitnessAndRefinementEvidence()
    {
        var source = Relation.Segment(new Carrier(), 0, 10);
        var selected = Relation.Segment(new Carrier(), 4, 6);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(new Carrier(), 5),
            selected);
        var result = Relation.OrderedGroup(
            new Carrier(),
            Relation.Segment(new Carrier(), 0, 4),
            Relation.Segment(new Carrier(), 0, 3),
            Relation.Segment(new Carrier(), 6, 10));
        var correspondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(selected, Relation.Segment(new Carrier(), 0, 3)));
        var residue = new Placeholder();
        var provenance = Relation.Point(new Carrier(), 1);

        var rung = StructuralLadderSurface.Rung(
            new Carrier(),
            source,
            focus,
            result,
            correspondence,
            residue,
            provenance);

        Assert.True(StructuralLadderSurface.TryReadRung(
            rung,
            out var readSource,
            out var readFocus,
            out var readResult,
            out var readCorrespondence,
            out var readResidue,
            out var witness,
            out var refinement,
            out var readProvenance));
        Assert.Equal(source, readSource);
        Assert.Equal(focus, readFocus);
        Assert.Equal(result, readResult);
        Assert.Equal(correspondence, readCorrespondence);
        Assert.Equal(residue, readResidue);
        Assert.Equal(provenance, readProvenance);

        var focusWitness = Assert.IsType<Relation>(witness);
        Assert.True(FocusSurface.TryReadResultWitness(
            focusWitness,
            out var witnessedFocus,
            out var witnessedSource,
            out var witnessedResult,
            out var witnessedResidue,
            out var witnessedProvenance));
        Assert.Equal(focus, witnessedFocus);
        Assert.Equal(source, witnessedSource);
        Assert.Equal(result, witnessedResult);
        Assert.Equal(residue, witnessedResidue);
        Assert.Equal(provenance, witnessedProvenance);

        var focusRefinement = Assert.IsType<Relation>(refinement);
        Assert.True(FocusSurface.TryReadRefinement(
            focusRefinement,
            out var refinedFocus,
            out var refinedResult,
            out _,
            out _,
            out _));
        Assert.Equal(focus, refinedFocus);
        Assert.Equal(result, refinedResult);
    }

    [Fact]
    public void RefinedRungCanBeCreatedDirectlyFromFocusSurface()
    {
        var source = Relation.Segment(new Carrier(), 0, 10);
        var selected = Relation.Segment(new Carrier(), 4, 6);
        var replacement = Relation.Segment(new Carrier(), 0, 3);
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(new Carrier(), 5),
            selected,
            Relation.Segment(new Carrier(), 0, 4),
            replacement,
            FocusSurface.Bind(selected, replacement),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var result = FocusSurface.Frame(new Carrier(), source, replacement);

        Assert.True(StructuralLadderSurface.TryCreateRefinedRungFromFocus(
            new Carrier(),
            focus,
            result,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out var rung));

        Assert.True(StructuralLadderSurface.TryReadRung(
            rung,
            out var readSource,
            out var readFocus,
            out var readResult,
            out _,
            out _,
            out var witness,
            out var refinement,
            out _));
        Assert.Equal(source, readSource);
        Assert.Equal(focus, readFocus);
        Assert.Equal(result, readResult);
        Assert.True(FocusSurface.TryReadResultWitness(
            Assert.IsType<Relation>(witness),
            out var witnessedFocus,
            out var witnessedSource,
            out var witnessedResult,
            out _,
            out _));
        Assert.Equal(focus, witnessedFocus);
        Assert.Equal(source, witnessedSource);
        Assert.Equal(result, witnessedResult);
        Assert.True(FocusSurface.TryReadRefinement(
            Assert.IsType<Relation>(refinement),
            out var refinedFocus,
            out var refinedResult,
            out _,
            out _,
            out _));
        Assert.Equal(focus, refinedFocus);
        Assert.Equal(result, refinedResult);
    }

    [Fact]
    public void RefinedRungBuilderRejectsNonFocusSurface()
    {
        Assert.False(StructuralLadderSurface.TryCreateRefinedRungFromFocus(
            new Carrier(),
            Relation.Segment(new Carrier(), 0, 10),
            Relation.Segment(new Carrier(), 0, 12),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            out _));
    }

    [Fact]
    public void LadderReadsBaseRungRungFrameHistoryResidueAndProvenance()
    {
        var baseStructure = Relation.Segment(new Carrier(), 0, 10);
        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), baseStructure);
        var focus = FocusSurface.Create(
            new Carrier(),
            baseStructure,
            Relation.Point(new Carrier(), 5),
            Relation.Segment(new Carrier(), 4, 6));
        var refined = StructuralLadderSurface.Rung(
            new Carrier(),
            baseStructure,
            focus,
            Relation.Segment(new Carrier(), 0, 12),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var frame = StructuralLadderSurface.RungFrame(new Carrier(), baseRung, refined);
        var residue = new Placeholder();
        var provenance = Relation.Point(new Carrier(), 3);

        var ladder = StructuralLadderSurface.Create(
            new Carrier(),
            baseRung,
            frame,
            Relation.Point(Carrier.Unknown, 0),
            residue,
            provenance);

        Assert.True(StructuralLadderSurface.TryRead(
            ladder,
            out var readBase,
            out var readFrame,
            out var readHistory,
            out var readResidue,
            out var readProvenance));
        Assert.Equal(baseRung, readBase);
        Assert.Equal(Relation.Point(Carrier.Unknown, 0), readHistory);
        Assert.Equal(residue, readResidue);
        Assert.Equal(provenance, readProvenance);
        Assert.True(StructuralLadderSurface.TryReadRungFrame(readFrame, out var rungs));
        Assert.Equal([baseRung, refined], rungs);
    }

    [Fact]
    public void LadderTimelineReadsOrderedFocusTransitionsBetweenRungs()
    {
        var clock = TimeStepSequence.CreateClock(new Carrier());
        var baseStructure = Relation.Segment(new Carrier(), 0, 10);
        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), baseStructure);
        var focus = FocusSurface.Create(
            new Carrier(),
            baseStructure,
            Relation.Point(new Carrier(), 5),
            Relation.Segment(new Carrier(), 4, 6));
        var refinedRung = StructuralLadderSurface.Rung(
            new Carrier(),
            baseStructure,
            focus,
            Relation.OrderedGroup(new Carrier(), baseStructure, Relation.Segment(new Carrier(), 0, 3)),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var residue = new Placeholder();
        var step = StructuralLadderSurface.Step(
            new Carrier(),
            clock,
            0,
            1,
            baseRung,
            focus,
            refinedRung,
            residue);
        var timeline = StructuralLadderSurface.Timeline(new Carrier(), clock, baseRung, step);
        var ladder = StructuralLadderSurface.Create(
            new Carrier(),
            baseRung,
            StructuralLadderSurface.RungFrame(new Carrier(), baseRung, refinedRung),
            timeline,
            residue,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(StructuralLadderSurface.TryReadTimeline(ladder, out var readTimeline));
        Assert.Equal(timeline, readTimeline);
        Assert.True(StructuralLadderSurface.TryReadStep(
            step,
            out var span,
            out var start,
            out var end,
            out var before,
            out var readFocus,
            out var after,
            out var readResidue));
        Assert.Equal(Relation.Segment(clock, 0, 1), span);
        Assert.Equal(new CarrierLocation(clock, 0), start);
        Assert.Equal(new CarrierLocation(clock, 1), end);
        Assert.Equal(baseRung, before);
        Assert.Equal(focus, readFocus);
        Assert.Equal(refinedRung, after);
        Assert.Equal(residue, readResidue);
    }

    [Fact]
    public void RungCanCarryProjectedFocusAsCorrespondenceEvidence()
    {
        var source = Relation.Segment(new Carrier(), 0, 10);
        var sourceFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(new Carrier(), 5),
            Relation.Segment(new Carrier(), 4, 6));
        var target = Relation.Segment(new Carrier(), 0, 20);
        var projectedBasis = Relation.Point(new Carrier(), 10);
        var correspondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(Relation.Point(new Carrier(), 5), projectedBasis));
        Assert.True(FocusSurface.TryProject(
            sourceFocus,
            new Carrier(),
            target,
            correspondence,
            Relation.Point(Carrier.Unknown, 0),
            out var projectedFocus,
            out var projection));

        var rung = StructuralLadderSurface.Rung(
            new Carrier(),
            source,
            projectedFocus,
            target,
            projection,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(StructuralLadderSurface.TryReadRung(
            rung,
            out _,
            out var readFocus,
            out var readResult,
            out var readCorrespondence,
            out _,
            out _,
            out _,
            out _));
        Assert.Equal(projectedFocus, readFocus);
        Assert.Equal(target, readResult);
        Assert.Equal(projection, readCorrespondence);
        Assert.True(FocusSurface.TryReadProjection(
            Assert.IsType<Relation>(readCorrespondence),
            out var readSourceFocus,
            out var readTarget,
            out _,
            out var readProjectedFocus,
            out _));
        Assert.Equal(sourceFocus, readSourceFocus);
        Assert.Equal(target, readTarget);
        Assert.Equal(projectedFocus, readProjectedFocus);
    }

    [Fact]
    public void ChangeReadExposesSelectedCarryReplacementResultAndResidue()
    {
        var source = Relation.Segment(new Carrier(), 0, 10);
        var selected = Relation.Segment(new Carrier(), 4, 6);
        var carry = FocusSurface.Frame(
            new Carrier(),
            Relation.Segment(new Carrier(), 0, 4),
            Relation.Segment(new Carrier(), 6, 10));
        var replacement = Relation.Segment(new Carrier(), 0, 3);
        var focusCorrespondence = FocusSurface.BindingFrame(
            new Carrier(),
            FocusSurface.Bind(selected, replacement));
        var focusResidue = new Placeholder();
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(new Carrier(), 5),
            selected,
            carry,
            replacement,
            focusCorrespondence,
            focusResidue,
            Relation.Point(Carrier.Unknown, 0));
        var result = FocusSurface.Frame(new Carrier(), carry, replacement);
        var rungCorrespondence = Relation.Point(new Carrier(), 8);
        var rungResidue = new Placeholder();
        var rung = StructuralLadderSurface.Rung(
            new Carrier(),
            source,
            focus,
            result,
            rungCorrespondence,
            rungResidue,
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(StructuralLadderSurface.TryCreateChangeRead(new Carrier(), rung, out var change));
        Assert.True(StructuralLadderSurface.TryReadChange(
            change,
            out var readSource,
            out var readParticipation,
            out var readComplement,
            out var readReplacement,
            out var readCorrespondence,
            out var readResult,
            out var readResidue));
        Assert.Equal(source, readSource);
        Assert.Equal(selected, readParticipation);
        Assert.Equal(carry, readComplement);
        Assert.Equal(replacement, readReplacement);
        Assert.Equal(rungCorrespondence, readCorrespondence);
        Assert.Equal(result, readResult);
        Assert.Equal(rungResidue, readResidue);
    }

    [Fact]
    public void ChangeReadFallsBackToFocusEvidenceWhenRungEvidenceIsUnresolved()
    {
        var source = Relation.Segment(new Carrier(), 0, 10);
        var selected = Relation.Segment(new Carrier(), 4, 6);
        var replacement = Relation.Segment(new Carrier(), 0, 3);
        var focusCorrespondence = FocusSurface.Bind(selected, replacement);
        var focusResidue = new Placeholder();
        var focus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(new Carrier(), 5),
            selected,
            Relation.Point(Carrier.Unknown, 0),
            replacement,
            focusCorrespondence,
            focusResidue,
            Relation.Point(Carrier.Unknown, 0));
        var result = FocusSurface.Frame(new Carrier(), source, replacement);
        var rung = StructuralLadderSurface.Rung(
            new Carrier(),
            source,
            focus,
            result,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(StructuralLadderSurface.TryCreateChangeRead(new Carrier(), rung, out var change));
        Assert.True(StructuralLadderSurface.TryReadChange(
            change,
            out _,
            out _,
            out _,
            out _,
            out var readCorrespondence,
            out _,
            out var readResidue));
        Assert.Equal(focusCorrespondence, readCorrespondence);
        Assert.Equal(focusResidue, readResidue);
    }

    [Fact]
    public void BaseRungDoesNotCreateChangeRead()
    {
        var rung = StructuralLadderSurface.BaseRung(new Carrier(), Relation.Segment(new Carrier(), 0, 10));

        Assert.False(StructuralLadderSurface.TryCreateChangeRead(new Carrier(), rung, out _));
    }

    [Fact]
    public void CurrentRungReadsLastTimelineAfterState()
    {
        var clock = TimeStepSequence.CreateClock(new Carrier());
        var source = Relation.Segment(new Carrier(), 0, 10);
        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), source);
        var firstFocus = FocusSurface.Create(
            new Carrier(),
            source,
            Relation.Point(new Carrier(), 3),
            Relation.Segment(new Carrier(), 2, 4));
        var firstRung = StructuralLadderSurface.Rung(
            new Carrier(),
            source,
            firstFocus,
            Relation.Segment(new Carrier(), 0, 11),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var secondFocus = FocusSurface.Create(
            new Carrier(),
            firstRung,
            Relation.Point(new Carrier(), 7),
            Relation.Segment(new Carrier(), 6, 8));
        var secondRung = StructuralLadderSurface.Rung(
            new Carrier(),
            firstRung,
            secondFocus,
            Relation.Segment(new Carrier(), 0, 12),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var firstStep = StructuralLadderSurface.Step(
            new Carrier(),
            clock,
            0,
            1,
            baseRung,
            firstFocus,
            firstRung,
            Relation.Point(Carrier.Unknown, 0));
        var secondStep = StructuralLadderSurface.Step(
            new Carrier(),
            clock,
            1,
            2,
            firstRung,
            secondFocus,
            secondRung,
            Relation.Point(Carrier.Unknown, 0));
        var timeline = StructuralLadderSurface.Timeline(new Carrier(), clock, baseRung, firstStep, secondStep);
        var ladder = StructuralLadderSurface.Create(
            new Carrier(),
            baseRung,
            StructuralLadderSurface.RungFrame(new Carrier(), baseRung, firstRung, secondRung),
            timeline,
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(StructuralLadderSurface.TryReadRungPath(ladder, out var path));
        Assert.Equal([baseRung, firstRung, secondRung], path);
        Assert.True(StructuralLadderSurface.TryReadCurrentRung(ladder, out var current));
        Assert.Equal(secondRung, current);
    }

    [Fact]
    public void CurrentRungFallsBackToLastFramedRung()
    {
        var source = Relation.Segment(new Carrier(), 0, 10);
        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), source);
        var refinedRung = StructuralLadderSurface.Rung(
            new Carrier(),
            source,
            FocusSurface.Create(new Carrier(), source, Relation.Point(new Carrier(), 5), Relation.Segment(new Carrier(), 4, 6)),
            Relation.Segment(new Carrier(), 0, 11),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));
        var ladder = StructuralLadderSurface.CreateWithRungs(new Carrier(), baseRung, refinedRung);

        Assert.True(StructuralLadderSurface.TryReadRungPath(ladder, out var path));
        Assert.Equal([refinedRung], path);
        Assert.True(StructuralLadderSurface.TryReadCurrentRung(ladder, out var current));
        Assert.Equal(refinedRung, current);
    }

    [Fact]
    public void CurrentRungFallsBackToBaseRung()
    {
        var baseRung = StructuralLadderSurface.BaseRung(new Carrier(), Relation.Segment(new Carrier(), 0, 10));
        var ladder = StructuralLadderSurface.Create(
            new Carrier(),
            baseRung,
            StructuralLadderSurface.RungFrame(new Carrier()),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0),
            Relation.Point(Carrier.Unknown, 0));

        Assert.True(StructuralLadderSurface.TryReadRungPath(ladder, out var path));
        Assert.Equal([baseRung], path);
        Assert.True(StructuralLadderSurface.TryReadCurrentRung(ladder, out var current));
        Assert.Equal(baseRung, current);
    }
}

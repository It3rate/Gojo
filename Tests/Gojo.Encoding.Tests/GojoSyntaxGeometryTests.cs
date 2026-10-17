using Gojo.Blocks;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Patterns;
using Gojo.Structures;
using GojoCAD;
using GojoCAD.Structures;

namespace Gojo.Encoding.Tests;

public sealed class GojoSyntaxGeometryTests
{
    [Theory]
    [InlineData("line-line", 0)]
    [InlineData("line-arc", 0)]
    [InlineData("arc-arc", 0)]
    [InlineData("line-line", 1)]
    [InlineData("line-arc", 1)]
    [InlineData("arc-arc", 1)]
    [InlineData("line-line", 2)]
    [InlineData("line-arc", 2)]
    [InlineData("arc-arc", 2)]
    public void BlocksBezierBridgeSharesTheActualIncomingAndOutgoingEndpoints(string row, int restores)
    {
        var specimen = BlocksGlossaryExport.Structures("curves")
            .Single(item => item.Id == $"curves-{row}-bezier").Structure;
        for (var i = 0; i < restores; i++) specimen = GojoNumericFormat.Decode(GojoNumericFormat.Encode(specimen))[0];
        var f = Fields(specimen);
        var incomingEnd = row == "arc-arc" ? R(R(f[0]).Right).Right : R(f[0]).Right;
        var outgoingStart = row == "line-line" ? R(f[1]).Left : R(R(f[1]).Right).Left;
        var bridge = Assert.Single(GraphicReadGraph.From(R(f[2])).ReadsOf<SharedFrameCubicCurveRead>());
        Assert.Same(incomingEnd, bridge.Start.Source);
        Assert.Same(outgoingStart, bridge.End.Source);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void DirectedFacesOrderAndSharedJunctionsSurviveTransport(int restores)
    {
        var f = Fields(Parse("graphic-primitives.gojo", restores)[0]);
        var ordinary = Assert.Single(GraphicReadGraph.From(R(f[1])).ReadsOf<PositiveSegmentRead>());
        var directed = Assert.Single(GraphicReadGraph.From(R(f[2])).ReadsOf<DirectedSegmentRead>());
        var next = Assert.Single(GraphicReadGraph.From(R(f[5])).ReadsOf<DirectedSegmentRead>());
        var lookalike = Assert.Single(GraphicReadGraph.From(R(f[6])).ReadsOf<DirectedSegmentRead>());
        Assert.Same(f[0], ordinary.Start.Location.Carrier);
        Assert.Equal(-4, ordinary.Start.Location.Ticks);
        Assert.True(directed.InverseStart.Location.Carrier.IsInverse);
        Assert.Same(f[0], directed.PositiveEnd.Location.Carrier);
        Assert.True(directed.PositiveEnd.Location.SameTickRefAs(next.InverseStart.Location));
        Assert.False(directed.PositiveEnd.Location.SameTickRefAs(lookalike.InverseStart.Location));
        Assert.Equal(next.InverseStart.Location.Ticks, lookalike.InverseStart.Location.Ticks);
        var reversed = Assert.Single(GraphicReadGraph.From(R(f[3])).ReadsOf<PositiveSegmentRead>());
        Assert.Equal(3, reversed.Start.Location.Ticks);
        Assert.Equal(-4, reversed.End.Location.Ticks);
        Assert.Equal(4, Assert.IsType<CarrierLocation>(f[4]).Ticks);

        directed.PositiveEnd.Location.Tick!.Set(5);
        Assert.Equal(5, next.InverseStart.Location.Ticks);
        Assert.Equal(3, lookalike.InverseStart.Location.Ticks);
        Assert.Equal(0UL, Assert.IsType<Carrier>(f[0]).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void NormalizedParameterKeepsCalibrationAndKnownRefinementResidue(int restores)
    {
        var f = Fields(Parse("graphic-primitives.gojo", restores)[1]);
        Assert.True(GraphicParameterSurface.TryReadNormalizedValue(R(f[0]), out var basis,
            out var domain, out var coarse, out var fine, out var coarseResult,
            out var fineResult, out var refinement, out var calibration));
        Assert.Same(f[1], calibration);
        Assert.Same(domain, R(calibration).Left);
        Assert.Same(basis, R(calibration).Right);
        Assert.Equal(40, coarse.Ticks);
        Assert.Equal(47, fine.Ticks);
        var coarsePoint = Point(coarseResult);
        var finePoint = Point(fineResult);
        Assert.Equal(90, Project(R(calibration), coarse, coarsePoint.Carrier).Ticks);
        Assert.Equal(104, Project(R(calibration), fine, finePoint.Carrier).Ticks);
        Assert.True(ReadWitness.TryRead(R(refinement), out var source, out var operation,
            out var result, out var residue, out _));
        var rounded = R(operation).Project(Assert.IsType<CarrierLocation>(source),
            Assert.IsType<CarrierLocation>(result).Carrier);
        Assert.True(rounded.TryReadProjection(out var actual, out var remainder));
        Assert.Equal(result, actual);
        Assert.Equal(residue, remainder);
        Assert.Equal(7, Assert.IsType<CarrierLocation>(residue).Ticks);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void NativeCarrierPromisesAndPartialPortsNeedNoCoordinateTuple(int restores)
    {
        var roots = Parse("graphic-primitives.gojo", restores);
        var native = Fields(roots[2]);
        var anchor = Point(native[0]);
        var circle = Assert.IsType<Carrier>(native[1]);
        var line = Assert.IsType<Carrier>(native[2]);
        Assert.True(circle.TryReadCircularOrigin(out var circularOrigin));
        Assert.True(line.TryReadLinearOrigin(out var linearOrigin));
        Assert.Equal(5, circularOrigin.Ticks);
        Assert.Equal(5, linearOrigin.Ticks);
        anchor.Tick!.Set(9);
        Assert.True(circle.TryReadCircularOrigin(out circularOrigin));
        Assert.True(line.TryReadLinearOrigin(out linearOrigin));
        Assert.Equal(5, circularOrigin.Ticks);
        Assert.Equal(5, linearOrigin.Ticks);

        var partial = Fields(roots[3]);
        Assert.True(GraphicConceptShape.TryReadSectionConcept(R(partial[0]), out var start,
            out var trace, out var end, out var residue));
        Assert.IsType<Placeholder>(trace);
        Assert.IsType<Placeholder>(residue);
        Assert.True(GraphicConceptShape.TryReadPortConcept(R(start), out var startSite,
            out _, out _, out _, out _));
        Assert.Same(native[0], startSite);
        Assert.True(GraphicConceptShape.TryReadPortConcept(R(end), out var endSite,
            out _, out _, out _, out _));
        Assert.IsType<Placeholder>(endSite);
        var bound = R(StructuralPattern.Apply(partial[0], R(partial[1])));
        Assert.True(GraphicConceptShape.TryReadSectionConcept(bound, out _, out var stillOpen,
            out var boundEnd, out _));
        Assert.Same(trace, stillOpen);
        Assert.Equal(8, Point(Fields(boundEnd)[0]).Ticks);
        Assert.IsType<Placeholder>(Fields(end)[0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ExplicitHolderPreservesIdentityWhileCoordinateOrderDefinesTheRead(int restores)
    {
        var f = Fields(Parse("graphic-primitives.gojo", restores)[4]);
        Assert.Same(f[5], R(f[3]).Right);
        Assert.Same(f[5], R(f[4]).Right);
        var segment = Assert.Single(GraphicReadGraph.From(R(f[0])).ReadsOf<SharedFrameSegmentRead>());
        Assert.Equal(3, segment.Start.Coordinates.Count);
        // The current CAD reader matches ordered coordinate axes, not holder identity.
        var otherHolder = Assert.Single(GraphicReadGraph.From(R(f[1])).ReadsOf<SharedFrameSegmentRead>());
        Assert.NotSame(otherHolder.Start.Holder, otherHolder.End.Holder);
        Assert.Empty(GraphicReadGraph.From(R(f[2])).ReadsOf<SharedFrameSegmentRead>());
        Assert.True(segment.Start.Coordinates[0].Carrier.UnitShape.CompatibleWith(
            segment.Start.Coordinates[1].Carrier.UnitShape));
    }

    [Fact]
    public void ExplicitHolderShorthandExpandsToOrdinaryRelationsIncludingEmptyGroups()
    {
        var roots = GojoSyntax.Parse("""
            gojo 1;
            let $h = carrier();
            let $a = ?a;
            let $b = ?b;
            root group_on($h, $a, $b);
            root ((point($h,0) -> $a) -> (point($h,0) -> $b)) -> $h;
            root group_on($h);
            root point($h,1) -> $h;
            """).Roots;
        Assert.Equal(roots[0], roots[1]);
        Assert.Equal(roots[2], roots[3]);
        Assert.Empty(Fields(roots[2]));
        Assert.Same(R(roots[0]).Right, R(roots[2]).Right);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TemplateInstancesPreserveRepeatedRolesBindingsAndPartialConstruction(int restores)
    {
        var f = Fields(Parse("graphic-primitives.gojo", restores)[5]);
        Assert.True(GraphicParameterSurface.TryReadTemplateInstance(R(f[0]), out var template,
            out var bindings, out var result, out var provenance));
        var expanded = StructuralPattern.Apply(template, Fields(bindings).Cast<Relation>().ToArray());
        Assert.Equal(result, expanded);
        Assert.Same(R(result).Left, R(R(result).Right).Left);
        Assert.Single(GraphicReadGraph.From(R(result)).ReadsOf<SharedFrameCircleRead>());
        Assert.True(GraphicParameterSurface.TryReadTemplateInstance(R(f[1]), out var sameTemplate,
            out var partialBindings, out var partialResult, out var sameProvenance));
        Assert.Same(template, sameTemplate);
        Assert.Same(provenance, sameProvenance);
        Assert.IsType<Placeholder>(partialResult);
        var partial = R(StructuralPattern.Apply(template, Fields(partialBindings).Cast<Relation>().ToArray()));
        Assert.Same(R(partial).Left, R(partial.Right).Left);
        Assert.IsType<Placeholder>(R(partial.Right).Right);
    }

    [Theory]
    [InlineData("group_on()")]
    [InlineData("group_on(?unbound, ?a)")]
    [InlineData("group_on(carrier(), 3)")]
    public void ExplicitHolderReportsInvalidArguments(string expression) =>
        Assert.Throws<GojoSyntaxException>(() => GojoSyntax.Parse($"gojo 1; root {expression};"));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ExistingBezierRecipesRemainReadableWithoutConstructorNames(int restores)
    {
        var f = Fields(Parse("bezier-construction.gojo", restores)[0]);
        var quadratic = Assert.Single(GraphicReadGraph.From(R(f[0])).ReadsOf<SharedFrameQuadraticCurveRead>());
        var cubic = Assert.Single(GraphicReadGraph.From(R(f[1])).ReadsOf<SharedFrameCubicCurveRead>());
        Assert.Same(f[2], quadratic.Start.Source);
        Assert.Same(f[3], quadratic.Control.Source);
        Assert.Same(f[4], quadratic.End.Source);
        Assert.Same(f[2], cubic.Start.Source);
        Assert.Same(f[3], cubic.FirstControl.Source);
        Assert.Same(f[4], cubic.SecondControl.Source);
        Assert.Same(f[5], cubic.End.Source);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void BezierConstructionPreservesSharedGradeIntermediateDependenciesAndOpenControls(int restores)
    {
        var f = Fields(Parse("bezier-construction.gojo", restores)[1]);
        var original = Interpolations(f[0]);
        Assert.All(original, op => Assert.Same(f[3], op.Grade));
        Assert.Same(original[0].Output, original[3].First);
        Assert.Same(original[1].Output, original[3].Second);
        Assert.Same(original[1].Output, original[4].First);
        Assert.Same(original[2].Output, original[4].Second);
        Assert.Same(original[3].Output, original[5].First);
        Assert.Same(original[4].Output, original[5].Second);
        var partial = Interpolations(StructuralPattern.Apply(f[0], Fields(f[1]).Cast<Relation>().ToArray()));
        Assert.IsType<Relation>(partial[0].First);
        Assert.IsType<Relation>(partial[0].Second);
        Assert.IsType<Placeholder>(partial[1].Second);
        Assert.Same(partial[1].Second, partial[2].First);
        var bound = Interpolations(StructuralPattern.Apply(f[0], Fields(f[2]).Cast<Relation>().ToArray()));
        Assert.All(bound.Take(3), op => { Assert.IsType<Relation>(op.First); Assert.IsType<Relation>(op.Second); });
        Assert.Equal(500, Assert.IsType<CarrierLocation>(Fields(bound[0].Grade)[0]).Ticks);
        // Binding control evidence does not pretend to have evaluated the curve.
        Assert.IsType<Placeholder>(bound[5].Output);
        Assert.IsType<Placeholder>(original[0].First);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ExplicitConstructionRetainsClosedAndRepeatedControlsWherePairRecipeIsAmbiguous(int restores)
    {
        var f = Fields(Parse("bezier-construction.gojo", restores)[2]);
        Assert.Empty(GraphicReadGraph.From(R(f[0])).ReadsOf<SharedFrameCubicCurveRead>());
        Assert.Empty(GraphicReadGraph.From(R(f[1])).ReadsOf<SharedFrameCubicCurveRead>());
        Assert.Single(GraphicReadGraph.From(R(f[1])).ReadsOf<SharedFrameCurveTransitionRead>());
        var closed = Interpolations(StructuralPattern.Apply(f[2], Fields(f[3]).Cast<Relation>().ToArray()));
        Assert.Same(closed[0].First, closed[2].Second);
        var repeated = Interpolations(StructuralPattern.Apply(f[2], Fields(f[4]).Cast<Relation>().ToArray()));
        Assert.All(repeated.Take(3), op =>
        {
            Assert.Same(repeated[0].First, op.First);
            Assert.Same(repeated[0].First, op.Second);
        });
        Assert.Equal(6, repeated.Length);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CurveReversalRebindsControlOrderRatherThanReversingTheOuterRelation(int restores)
    {
        var roots = Parse("bezier-construction.gojo", restores);
        var points = Fields(roots[0]);
        var f = Fields(roots[3]);
        var reversed = Interpolations(StructuralPattern.Apply(f[0], Fields(f[1]).Cast<Relation>().ToArray()));
        Assert.Same(points[5], reversed[0].First);
        Assert.Same(points[4], reversed[0].Second);
        Assert.Same(points[3], reversed[1].Second);
        Assert.Same(points[2], reversed[2].Second);
        var outerReverse = Assert.Single(GraphicReadGraph.From(R(f[2])).ReadsOf<SharedFrameCubicCurveRead>());
        Assert.Same(points[3], outerReverse.Start.Source);
        Assert.Same(points[4], outerReverse.End.Source);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void FriezeGeneratorsKeepMotifPortsFeedbackOpenDomainAndObservationWindow(int restores)
    {
        var f = Fields(Parse("frieze-patterns.gojo", restores)[0]);
        Assert.True(RepeatSurface.TryRead(R(f[0]), out var motif, out var domain,
            out var transform, out var feedback, out var residue, out var history));
        Assert.Same(f[8], motif);
        Assert.IsType<Placeholder>(Fields(domain)[1]);
        Assert.IsType<Placeholder>(residue);
        Assert.IsType<Placeholder>(history);
        Assert.False(RepeatSurface.TryReadSpan(domain, out _, out _));
        Assert.True(RepeatSurface.TryReadSpan(f[2], out var start, out var end));
        Assert.Same(start.Carrier, Fields(domain)[0]);
        Assert.Same(domain, Fields(f[1])[1]);
        Assert.Equal(-2, start.Ticks);
        Assert.Equal(2, end.Ticks);
        Assert.True(TransformSurface.TryRead(R(transform), out var inputs, out var operation,
            out var outputs, out _, out var period));
        Assert.Same(f[3], period);
        Assert.Same(f[9], Fields(operation)[0]);
        Assert.IsType<Placeholder>(Fields(operation)[2]);
        var feed = R(Assert.Single(Fields(feedback)));
        Assert.Same(Assert.Single(Fields(outputs)), feed.Left);
        Assert.Same(Assert.Single(Fields(inputs)), feed.Right);
        var edges = Fields(motif).Select(R).ToArray();
        Assert.Same(edges[0].Right, edges[1].Left);
        Assert.Same(edges[1].Right, edges[2].Left);
        Assert.Same(edges[2].Right, edges[0].Left);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TwoGlideStepsEqualOneTranslationInTheSelectedCoordinateRead(int restores)
    {
        var f = Fields(Parse("frieze-patterns.gojo", restores)[0]);
        var x = Point(R(f[3]).Left).Carrier;
        var y = Point(R(R(f[5]).Left).Left).Carrier;
        for (var tick = -17; tick <= 17; tick++)
        {
            var location = new CarrierLocation(x, tick);
            var glide = Project(R(f[6]), location, x);
            var twice = Project(R(f[6]), glide, x);
            Assert.Equal(tick + 12, twice.Ticks);
            Assert.Equal(Project(R(f[4]), location, x), twice);
            var reflected = Project(R(f[7]), new CarrierLocation(y, tick), y);
            Assert.Equal(-tick, reflected.Ticks);
            Assert.Same(y, reflected.Carrier);
            Assert.False(reflected.Carrier.IsInverse);
            Assert.Equal(tick, Project(R(f[7]), reflected, y).Ticks);
        }
        Assert.Equal(0UL, x.Value);
        Assert.Equal(0UL, y.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void UnknownFriezeCalibrationStaysOpenUntilItsBindingArrives(int restores)
    {
        var f = Fields(Parse("frieze-patterns.gojo", restores)[1]);
        var before = Fields(Fields(Fields(f[0])[2])[1]);
        Assert.IsType<Placeholder>(Fields(before[1])[0]);
        var bound = StructuralPattern.Apply(f[0], R(f[1]));
        var after = Fields(Fields(Fields(bound)[2])[1]);
        Assert.IsType<Relation>(Fields(after[1])[0]);
        Assert.IsType<Placeholder>(after[2]);
        Assert.IsType<Placeholder>(Fields(Fields(f[0])[1])[1]);
    }

    private static (IElement First, IElement Second, IElement Grade, IElement Output)[] Interpolations(IElement action)
    {
        Assert.True(TransformSurface.TryRead(R(action), out _, out var operation, out var outputs, out _, out _));
        var operations = Fields(operation);
        Assert.Equal(6, operations.Length);
        var reads = operations.Select(item =>
        {
            Assert.True(TransformSurface.TryReadInterpolate(R(item), out var a, out var b, out var grade, out var output));
            return (First: a, Second: b, Grade: grade, Output: output);
        }).ToArray();
        Assert.Same(reads[^1].Output, Assert.Single(Fields(outputs)));
        return reads;
    }

    private static IElement[] Parse(string fixture, int restores)
    {
        var roots = GojoSyntax.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Fixtures", "Syntax", fixture))).Roots.ToArray();
        for (var i = 0; i < restores; i++) roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(roots));
        return roots;
    }
    private static Relation R(IElement element) => Assert.IsType<Relation>(element);
    private static IElement[] Fields(IElement element)
    {
        Assert.True(R(element).TryReadOrderedGroup(out var fields));
        return fields;
    }
    private static CarrierLocation Point(IElement element) => Assert.IsType<CarrierLocation>(R(element).Left);
    private static CarrierLocation Project(Relation calibration, CarrierLocation sample, Carrier target)
    {
        Assert.True(calibration.Project(sample, target).TryReadProjection(out var result, out var residue));
        Assert.True(ProjectionResidue.TryRead(residue, out _, out var numerator, out _));
        Assert.Equal(0, numerator);
        return result;
    }
}

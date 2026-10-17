using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class StructuralLandmarkTests
{
    [Fact]
    public void SharedUnmeasuredLandmarkGeneratesThreeDistinctRecognizableIncidencePatterns()
    {
        var fixture = new StructuralLandmarkFixture();
        var templates = new[] { fixture.ETemplate, fixture.HTemplate, fixture.BTemplate };
        var generated = templates.Select(template => Instance(fixture.Generate(template))).ToArray();
        var localInteriors = new List<IElement>();
        foreach (var glyph in generated)
        {
            var roles = Instance(fixture.ReadRoles(glyph));
            var mapped = Assert.IsType<Relation>(Assert.IsType<Relation>(roles.Left).Right);
            Assert.Same(fixture.Interior, mapped.Left);
            localInteriors.Add(mapped.Right);
            Assert.True(Contains(glyph, fixture.Frame));
            AssertNoNumbers(glyph);
        }
        Assert.Equal(3, localInteriors.Distinct(ReferenceEqualityComparer.Instance).Count());
        for (var i = 0; i < templates.Length; i++)
            for (var j = 0; j < generated.Length; j++)
            {
                var read = fixture.Recognize(templates[i], StructuralConstraint.Closed(generated[j]));
                Assert.Equal(i == j, StructuralConstraint.TryReadUnique(read, out _, out _));
                Assert.True(Contains(read, generated[j]));
            }
        AssertNoNumbers(fixture.Surface);
    }

    [Fact]
    public void RecognitionAcceptsFreshInstancesAndPreservesDistinctMatches()
    {
        var fixture = new StructuralLandmarkFixture();
        var first = Instance(fixture.Generate(fixture.HTemplate));
        var second = Instance(fixture.Generate(fixture.HTemplate));
        Assert.NotSame(first, second);
        Assert.NotSame(first.Left, second.Left);
        var read = fixture.Recognize(fixture.HTemplate, StructuralConstraint.Closed(first, second));
        Assert.True(StructuralConstraint.TryRead(read, out _, out var candidates, out _, out var unresolved));
        Assert.Equal(2, Entries(candidates).Length);
        Assert.Empty(Entries(unresolved));
        Assert.False(StructuralConstraint.TryReadUnique(read, out _, out _));
    }

    [Fact]
    public void ASelectedLocalSiteIsReusedAcrossEveryOccurrenceWithoutPositionNumbers()
    {
        var fixture = new StructuralLandmarkFixture();
        var selected = new Placeholder();
        var generation = fixture.Generate(fixture.ETemplate, selected);
        var glyph = Instance(generation);
        var roles = Instance(fixture.ReadRoles(glyph));
        var mapped = Assert.IsType<Relation>(Assert.IsType<Relation>(roles.Left).Right);
        Assert.Same(selected, mapped.Right);
        var placement = Assert.IsType<Relation>(Assert.IsType<Relation>(glyph.Right).Left);
        var axis = Assert.IsType<Relation>(Assert.IsType<Relation>(placement.Right).Right);
        Assert.Same(selected, Assert.IsType<Relation>(axis.Left).Right);
        Assert.Same(selected, Assert.IsType<Relation>(axis.Right).Left);
        var strokes = Assert.IsType<Relation>(Assert.IsType<Relation>(glyph.Right).Right);
        var branches = Assert.IsType<Relation>(strokes.Right);
        var interiorBar = Assert.IsType<Relation>(Assert.IsType<Relation>(branches.Right).Left);
        Assert.Same(selected, interiorBar.Left);
        Assert.True(Contains(generation, fixture.ETemplate));
        AssertNoNumbers(generation);
    }

    [Fact]
    public void OneStableUnmeasuredChoiceFeedsEveryTemplateAndConflictingBindingCannotOverwriteIt()
    {
        var fixture = new StructuralLandmarkFixture();
        Assert.IsType<Placeholder>(fixture.LandmarkBinding.Right);
        Assert.Same(fixture.Interior, fixture.LandmarkBinding.Right);
        foreach (var template in new[] { fixture.ETemplate, fixture.HTemplate, fixture.BTemplate })
        {
            var generated = fixture.Generate(template);
            Assert.True(Contains(generated, fixture.LandmarkBinding));
            Assert.True(Contains(Instance(generated), fixture.Interior));
        }
        var rival = new Placeholder();
        var conflict = new Relation(fixture.LandmarkBinding.Left, rival);
        var refused = fixture.Generate(fixture.ETemplate, additionalBindings: [conflict]);
        Assert.False(StructuralConstraint.TryReadUnique(refused, out _, out _));
        Assert.True(StructuralConstraint.TryRead(refused, out _, out var candidates, out _, out var unresolved));
        Assert.Empty(Entries(candidates));
        Assert.NotEmpty(Entries(unresolved));
        Assert.True(Contains(refused, conflict));
        Assert.Same(fixture.Interior, fixture.LandmarkBinding.Right);
        Assert.True(StructuralConstraint.TryReadUnique(fixture.Generate(fixture.ETemplate), out _, out _));
    }

    [Fact]
    public void IndependentLookalikeLandmarkDoesNotDischargeTheSharedChoice()
    {
        var fixture = new StructuralLandmarkFixture();
        var glyph = Instance(fixture.Generate(fixture.ETemplate));
        var body = (Relation)glyph.Right;
        var placement = (Relation)body.Left;
        var mappingAndAxis = (Relation)placement.Right;
        var mappings = (Relation)mappingAndAxis.Left;
        var remainingMaps = (Relation)mappings.Right;
        var middleMap = (Relation)remainingMaps.Left;
        var independent = new Relation(new Placeholder(), middleMap.Right);
        var changedMaps = new Relation(mappings.Left, new Relation(independent, remainingMaps.Right));
        var changed = new Relation(glyph.Left, new Relation(new Relation(fixture.Frame,
            new Relation(changedMaps, mappingAndAxis.Right)), body.Right));
        AssertClosedRejection(fixture.Recognize(fixture.ETemplate, StructuralConstraint.Closed(changed)));
    }

    [Fact]
    public void IdenticalEndpointsDoNotMakeTwoIndependentAxisRelationsOneSharedAxis()
    {
        var fixture = new StructuralLandmarkFixture();
        var glyph = Instance(fixture.Generate(fixture.ETemplate));
        var body = (Relation)glyph.Right;
        var strokes = (Relation)body.Right;
        var axis = (Relation)strokes.Left;
        var lookalikeAxis = new Relation(axis.Left, axis.Right);
        var changed = new Relation(glyph.Left,
            new Relation(body.Left, new Relation(lookalikeAxis, strokes.Right)));
        AssertClosedRejection(fixture.Recognize(fixture.ETemplate, StructuralConstraint.Closed(changed)));
    }

    [Fact]
    public void SharedLandmarkAcrossDifferentFramesDoesNotEstablishPlacementCorrespondence()
    {
        var fixture = new StructuralLandmarkFixture();
        var other = new StructuralLandmarkFixture(interior: fixture.Interior);
        var glyph = Instance(other.Generate(other.HTemplate));
        Assert.Same(fixture.Interior, other.Interior);
        AssertClosedRejection(fixture.Recognize(fixture.HTemplate, StructuralConstraint.Closed(glyph)));
        Assert.True(StructuralConstraint.TryReadUnique(other.ReadRoles(glyph), out _, out _));
    }

    [Fact]
    public void ReversalAndMissingRoleDoNotSilentlyPreserveTheFrame()
    {
        var fixture = new StructuralLandmarkFixture();
        var glyph = Instance(fixture.Generate(fixture.ETemplate));
        var body = (Relation)glyph.Right;
        var placement = (Relation)body.Left;
        var reversed = new Relation(fixture.Frame.Right, fixture.Frame.Left);
        var reversedGlyph = new Relation(glyph.Left,
            new Relation(new Relation(reversed, placement.Right), body.Right));
        var missingRole = new Relation(glyph.Left,
            new Relation(new Relation(fixture.Frame, new Placeholder()), body.Right));
        AssertClosedRejection(fixture.Recognize(fixture.ETemplate, StructuralConstraint.Closed(reversedGlyph)));
        AssertClosedRejection(fixture.Recognize(fixture.ETemplate, StructuralConstraint.Closed(missingRole)));
    }

    [Fact]
    public void AnOpenSearchIsDifferentFromAClosedRejectedPattern()
    {
        var fixture = new StructuralLandmarkFixture();
        var wrong = Instance(fixture.Generate(fixture.BTemplate));
        var closed = fixture.Recognize(fixture.HTemplate, StructuralConstraint.Closed(wrong));
        var open = fixture.Recognize(fixture.HTemplate, StructuralConstraint.Open(wrong));
        AssertClosedRejection(closed);
        Assert.True(StructuralConstraint.TryRead(open, out _, out var candidates, out _, out var unresolved));
        Assert.Empty(Entries(candidates));
        Assert.NotEmpty(Entries(unresolved));
        Assert.True(Contains(unresolved, wrong) || Contains(open, wrong));
    }

    [Fact]
    public void CollapsedAndLoopedFrameRolesStayUnresolvedInsteadOfPromisingStrictInterior()
    {
        var same = new Placeholder();
        var fixtures = new[]
        {
            new StructuralLandmarkFixture(beginning: same, interior: same),
            new StructuralLandmarkFixture(interior: same, end: same),
            new StructuralLandmarkFixture(beginning: same, end: same)
        };
        foreach (var fixture in fixtures)
        {
            foreach (var read in new[]
            {
                fixture.Generate(fixture.ETemplate),
                fixture.Recognize(fixture.ETemplate, StructuralConstraint.Closed(new Placeholder()))
            })
            {
                Assert.False(StructuralConstraint.TryReadUnique(read, out _, out _));
                Assert.True(StructuralConstraint.TryRead(read, out _, out var candidates, out _, out var unresolved));
                Assert.Empty(Entries(candidates));
                Assert.NotEmpty(Entries(unresolved));
                Assert.True(Contains(read, fixture.FrameQualification));
            }
        }
    }

    [Fact]
    public void LocalRoleAliasingIsRejectedEvenWhenGeneralMatchingAndRelationAliasesStillFit()
    {
        var fixture = new StructuralLandmarkFixture();
        Assert.Equal(15, Entries(fixture.LocalRoleConstraints).Length);
        Assert.True(Contains(fixture.Surface, fixture.LocalRoleConstraints));
        foreach (var template in new[] { fixture.ETemplate, fixture.HTemplate, fixture.BTemplate })
        {
            var glyph = Instance(fixture.Generate(template));
            var roles = Instance(fixture.ReadRoles(glyph));
            var first = Assert.IsType<Relation>(roles.Left);
            var middle = Assert.IsType<Relation>(first.Right).Right;
            Check(template, glyph, middle, first.Left);
        }
        var eGlyph = Instance(fixture.Generate(fixture.ETemplate));
        var strokes = (Relation)((Relation)eGlyph.Right).Right;
        var firstBar = (Relation)((Relation)strokes.Right).Left;
        Check(fixture.ETemplate, eGlyph, firstBar.Right, firstBar.Left);

        void Check(Relation template, Relation glyph, IElement from, IElement to)
        {
            var collapsed = (Relation)Replace(glyph, from, to, new(ReferenceEqualityComparer.Instance));
            // The general matcher correctly permits many variables to bind one
            // participant. The template's extra incidence promise forbids it.
            var raw = StructuralConstraint.Match(template, fixture.Variables,
                StructuralConstraint.Closed(collapsed), fixture.LandmarkBinding);
            Assert.True(StructuralConstraint.TryReadUnique(raw, out _, out _));
            var qualified = fixture.Recognize(template, StructuralConstraint.Closed(collapsed));
            AssertClosedRejection(qualified);
            Assert.True(Contains(qualified, collapsed));
            Assert.True(Contains(qualified, fixture.LocalRoleConstraints));
        }
    }

    private static IElement Replace(IElement source, IElement from, IElement to,
        Dictionary<Relation, IElement> memo)
    {
        if (ReferenceEquals(source, from)) return to;
        if (source is not Relation relation) return source;
        if (memo.TryGetValue(relation, out var previous)) return previous;
        var left = Replace(relation.Left, from, to, memo);
        var right = Replace(relation.Right, from, to, memo);
        IElement replaced = ReferenceEquals(left, relation.Left) && ReferenceEquals(right, relation.Right)
            ? relation : new Relation(left, right);
        memo.Add(relation, replaced);
        return replaced;
    }

    private static Relation Instance(Relation read)
    {
        Assert.True(StructuralConstraint.TryReadUnique(read, out var instance, out _));
        return Assert.IsType<Relation>(instance);
    }

    private static void AssertClosedRejection(Relation read)
    {
        Assert.True(StructuralConstraint.TryRead(read, out _, out var candidates, out var blocked, out var unresolved));
        Assert.Empty(Entries(candidates));
        Assert.NotEmpty(Entries(blocked));
        Assert.Empty(Entries(unresolved));
    }

    private static IElement[] Entries(Relation scope)
    {
        Assert.True(StructuralConstraint.TryReadScope(scope, out var entries, out _, out _));
        return entries;
    }

    private static bool Contains(IElement root, IElement target) =>
        ReferenceEquals(root, target) || root is Relation relation &&
        (Contains(relation.Left, target) || Contains(relation.Right, target));

    private static void AssertNoNumbers(IElement root)
    {
        Assert.IsNotType<Carrier>(root);
        Assert.IsNotType<CarrierLocation>(root);
        if (root is not Relation relation) return;
        AssertNoNumbers(relation.Left);
        AssertNoNumbers(relation.Right);
    }
}

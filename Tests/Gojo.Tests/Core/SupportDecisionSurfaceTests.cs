using Gojo.Core;
using Gojo.Structures;

namespace Gojo.Tests.Core;

public sealed class SupportDecisionSurfaceTests
{
    [Fact]
    public void APointCanSelectTheFirstApplicableNativeResponse()
    {
        var fixture = new Fixture();
        var first = fixture.Rule(-10, -1, fixture.First);
        var second = fixture.Rule(0, 10, fixture.Second);
        var last = fixture.Fallback();
        var binding = fixture.Binding(Point(fixture.Axis, 3));
        var selection = fixture.Select([first, second, last], binding);

        AssertSelection(selection, second, fixture.Second, 1, 1);
        var earlier = Earlier(selection);
        AssertRuleFailure(Assert.IsType<Relation>(Assert.Single(earlier)), first, 1, 0);
        Assert.True(ReadWitness.TryRead(selection, out var selected, out var policy, out _, out _, out _));
        Assert.Same(binding, Assert.Single(Group(Assert.IsType<Relation>(selected))));
        Assert.Same(first, Group(Assert.IsType<Relation>(policy))[0]);
    }

    [Fact]
    public void SupportCrossingAConditionBoundaryIsUnresolvedRatherThanFalse()
    {
        var fixture = new Fixture();
        var partition = CoarseReadSurface.Partition(new Carrier(), fixture.Axis, (0, 4), (5, 9), (10, 14));
        var publication = CoarseReadSurface.Publish(new Carrier(), partition, 0b011, new Placeholder());
        var narrow = fixture.Rule(0, 6, fixture.First);
        var selection = fixture.Select([narrow, fixture.Fallback()], fixture.Binding(publication));

        AssertSelection(selection, null, fixture.FallbackResponse, 0, 1);
        AssertRuleFailure(Assert.IsType<Relation>(Assert.Single(Earlier(selection))), narrow, 0, 1);
    }

    [Fact]
    public void AWholeUnionCanBeAdmittedWithoutChoosingOneMember()
    {
        var fixture = new Fixture();
        var partition = CoarseReadSurface.Partition(new Carrier(), fixture.Axis, (-9, -1), (0, 0), (1, 9));
        var publication = CoarseReadSurface.Publish(new Carrier(), partition, 0b111, new Placeholder());
        var tolerant = fixture.Rule(-9, 9, fixture.First);
        var binding = fixture.Binding(publication);

        AssertSelection(fixture.Select([tolerant, fixture.Fallback()], binding), tolerant, fixture.First, 1, 0);
        Assert.True(CoarseReadSurface.TryReadMask(publication, partition, out var mask));
        Assert.Equal(0b111, mask);
    }

    [Fact]
    public void DisconnectedSupportDoesNotGainAnInventedMiddle()
    {
        var fixture = new Fixture();
        var partition = CoarseReadSurface.Partition(new Carrier(), fixture.Axis, (0, 4), (5, 9), (10, 14));
        var publication = CoarseReadSurface.Publish(new Carrier(), partition, 0b101, new Placeholder());
        var middle = fixture.Rule(5, 9, fixture.First);
        var all = fixture.Rule(0, 14, fixture.Second);
        var selection = fixture.Select([middle, all], fixture.Binding(publication));

        AssertSelection(selection, all, fixture.Second, 1, 1);
        AssertRuleFailure(Assert.IsType<Relation>(Assert.Single(Earlier(selection))), middle, 1, 0);
        Assert.True(SupportDecisionSurface.TryReadRuleRead(Assert.IsType<Relation>(Assert.Single(Earlier(selection))),
            out _, out _, out var rejected, out _));
        var conditionRead = Assert.IsType<Relation>(Assert.Single(Group(rejected)));
        Assert.True(ReadWitness.TryRead(conditionRead, out _, out _, out var captured, out _, out _));
        Assert.Equal(2, Group(Assert.IsType<Relation>(captured)).Length);
    }

    [Theory]
    [InlineData(17)]
    [InlineData(-3)]
    public void AClippedOpenEndpointCannotSettleAnyFiniteContainingFrame(int raw)
    {
        var fixture = new Fixture();
        var capacity = FiniteReadSurface.Capacity(new Carrier(), fixture.Axis, 0, 15);
        var publication = FiniteReadSurface.Publish(new Carrier(), capacity, new CarrierLocation(fixture.Axis, raw), new Placeholder());
        var finite = fixture.Rule(-100, 100, fixture.First);
        var selection = fixture.Select([finite, fixture.Fallback()], fixture.Binding(publication));

        AssertSelection(selection, null, fixture.FallbackResponse, 0, 1);
        AssertRuleFailure(Assert.IsType<Relation>(Assert.Single(Earlier(selection))), finite, 0, 1);
    }

    [Fact]
    public void AClosedFiniteCellMustFitAsAWholeDespiteItsNominal()
    {
        var fixture = new Fixture();
        var capacity = FiniteReadSurface.Capacity(new Carrier(), fixture.Axis, -15, 15, 4);
        var publication = FiniteReadSurface.Publish(new Carrier(), capacity, new CarrierLocation(fixture.Axis, 10), new Placeholder());
        var narrow = fixture.Rule(8, 8, fixture.First);
        var cell = fixture.Rule(8, 11, fixture.Second);
        var selection = fixture.Select([narrow, cell, fixture.Fallback()], fixture.Binding(publication));

        AssertSelection(selection, cell, fixture.Second, 1, 1);
        AssertRuleFailure(Assert.IsType<Relation>(Assert.Single(Earlier(selection))), narrow, 0, 1);
        Assert.True(FiniteReadSurface.TryReadValue(publication, out var nominal));
        Assert.Equal(8, nominal.Ticks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnknownAndEmptySupportCannotManufactureZero(bool empty)
    {
        var fixture = new Fixture();
        var partition = CoarseReadSurface.Partition(new Carrier(), fixture.Axis, (-1, -1), (0, 0), (1, 1));
        var unknown = empty ? CoarseReadSurface.Publish(new Carrier(), partition, 0, new Placeholder()) :
            CoarseReadSurface.Unknown(new Carrier(), partition, new Placeholder());
        var zero = fixture.Rule(0, 0, fixture.First);

        AssertSelection(fixture.Select([zero, fixture.Fallback()], fixture.Binding(unknown)), null, fixture.FallbackResponse, 0, 1);
        var knownZero = CoarseReadSurface.Publish(new Carrier(), partition, 0b010, new Placeholder());
        AssertSelection(fixture.Select([zero, fixture.Fallback()], fixture.Binding(knownZero)), zero, fixture.First, 1, 0);
    }

    [Fact]
    public void SourceAndProvenanceCannotSupplyPrivateFinePrecision()
    {
        var fixture = new Fixture();
        var partition = CoarseReadSurface.Partition(new Carrier(), fixture.Axis, (0, 9), (10, 19));
        var privateFine = Point(fixture.Axis, 7);
        var exposed = CoarseReadSurface.Measure(new Carrier(), partition, 7, privateFine);
        var exact = fixture.Rule(7, 7, fixture.First);
        var whole = fixture.Rule(0, 9, fixture.Second);
        var selection = fixture.Select([exact, whole], fixture.Binding(exposed));

        AssertSelection(selection, whole, fixture.Second, 1, 1);
        AssertRuleFailure(Assert.IsType<Relation>(Assert.Single(Earlier(selection))), exact, 0, 1);
    }

    [Fact]
    public void AConditionReadCapturesTheCoordinatesActuallyConsumed()
    {
        var fixture = new Fixture();
        var site = new TickRef(3);
        var publication = ReadWitness.Create(new Carrier(), new Placeholder(), Relation.Segment(fixture.Axis, 0, 1),
            new CarrierLocation(fixture.Axis, site), Group(), new Placeholder());
        var rule = fixture.Rule(0, 5, fixture.First);
        var selection = fixture.Select([rule], fixture.Binding(publication));
        Assert.True(ReadWitness.TryRead(selection, out _, out _, out _, out _, out var provenance));
        var selectedRead = Assert.IsType<Relation>(Group(Assert.IsType<Relation>(provenance))[3]);
        Assert.True(ReadWitness.TryRead(selectedRead, out _, out _, out _, out _, out var ruleEvidence));
        var admittedRead = Assert.IsType<Relation>(Assert.Single(Group(Assert.IsType<Relation>(Group(Assert.IsType<Relation>(ruleEvidence))[1]))));
        Assert.True(ReadWitness.TryRead(admittedRead, out _, out _, out var captured, out _, out _));
        site.Set(9);

        var support = Assert.IsType<Relation>(Assert.Single(Group(Assert.IsType<Relation>(captured))));
        Assert.Equal(3, Assert.IsType<CarrierLocation>(support.Left).Ticks);
        Assert.Equal(3, Assert.IsType<CarrierLocation>(support.Right).Ticks);
        AssertSelection(selection, rule, fixture.First, 1, 0);
        AssertSelection(fixture.Select([rule, fixture.Fallback()], fixture.Binding(publication)), null, fixture.FallbackResponse, 0, 1);
    }

    [Fact]
    public void PriorityRemainsNativeAndLaterRulesAreNotEvaluated()
    {
        var fixture = new Fixture();
        var first = fixture.Rule(0, 10, fixture.First);
        var second = fixture.Rule(0, 10, fixture.Second);
        AssertSelection(fixture.Select([first, second], fixture.Binding(Point(fixture.Axis, 2))), first, fixture.First, 1, 0);
        AssertSelection(fixture.Select([second, first], fixture.Binding(Point(fixture.Axis, 2))), second, fixture.Second, 1, 0);
    }

    [Fact]
    public void AnUnconditionalFallbackRetainsPriorUnresolvedDemands()
    {
        var fixture = new Fixture();
        var conditional = fixture.Rule(1, 10, fixture.First);
        var selection = fixture.Select([conditional, fixture.Fallback()]);
        AssertSelection(selection, null, fixture.FallbackResponse, 0, 1);
        AssertRuleFailure(Assert.IsType<Relation>(Assert.Single(Earlier(selection))), conditional, 0, 1);

        var noFallback = fixture.Select([conditional]);
        Assert.True(SupportDecisionSurface.TryReadSelection(noFallback, out var selectedRule, out var response, out _, out _));
        Assert.IsType<Placeholder>(selectedRule);
        Assert.IsType<Placeholder>(response);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void WrongAxisInvalidCoordinateAndMismatchedDeclaredFrameStayUnresolved(int kind)
    {
        var fixture = new Fixture();
        var foreign = new Carrier();
        var result = kind == 0 ? (IElement)new CarrierLocation(foreign, 2) : kind == 1 ? default(CarrierLocation) : new CarrierLocation(fixture.Axis, 2);
        var publication = ReadWitness.Create(new Carrier(), new Placeholder(),
            Relation.Segment(kind == 2 ? foreign : fixture.Axis, 0, 1), result, Group(), new Placeholder());
        var rule = fixture.Rule(0, 10, fixture.First);
        var selection = fixture.Select([rule, fixture.Fallback()], fixture.Binding(publication));
        AssertSelection(selection, null, fixture.FallbackResponse, 0, 1);
        AssertRuleFailure(Assert.IsType<Relation>(Assert.Single(Earlier(selection))), rule, 0, 1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void InvalidFiniteQualificationsCannotFallBackToTheNominal(int kind)
    {
        var fixture = new Fixture();
        var capacity = FiniteReadSurface.Capacity(new Carrier(), fixture.Axis, 0, 15, kind == 2 ? 0 : 1);
        var low = kind == 0 ? -1 : 6;
        var forged = QualifiedReadSurface.Create(new Carrier(), new Placeholder(), Relation.Segment(fixture.Axis, 0, 1),
            new CarrierLocation(fixture.Axis, 5), new Relation(new CarrierLocation(fixture.Axis, low), new CarrierLocation(fixture.Axis, 9)),
            new Placeholder(), capacity, new Placeholder(), new Placeholder());
        var rule = fixture.Rule(0, 15, fixture.First);
        AssertSelection(fixture.Select([rule, fixture.Fallback()], fixture.Binding(forged)), null, fixture.FallbackResponse, 0, 1);
    }

    [Fact]
    public void PendingSuppliersAndDuplicateRoleBindingsRemainUnresolved()
    {
        var fixture = new Fixture();
        var exact = Point(fixture.Axis, 3);
        var pending = QualifiedReadSurface.Add(new Carrier(), exact, exact);
        var rule = fixture.Rule(0, 10, fixture.First);
        AssertSelection(fixture.Select([rule, fixture.Fallback()], fixture.Binding(pending)), null, fixture.FallbackResponse, 0, 1);
        AssertSelection(fixture.Select([rule, fixture.Fallback()], fixture.Binding(exact), fixture.Binding(Point(fixture.Axis, 4))),
            null, fixture.FallbackResponse, 0, 1);
    }

    [Fact]
    public void AnExplicitProjectionPointUsesItsTargetAxisRatherThanItsSourceAxis()
    {
        var fixture = new Fixture();
        var sourceAxis = new Carrier();
        var law = new Relation(Relation.Segment(sourceAxis, 0, 100), Relation.Segment(fixture.Axis, 10, 0));
        var publication = ReadWitness.Create(new Carrier(), new CarrierLocation(sourceAxis, 70), law,
            new CarrierLocation(fixture.Axis, 3), new CarrierLocation(sourceAxis, 0), law);
        var rule = fixture.Rule(0, 5, fixture.First);
        AssertSelection(fixture.Select([rule], fixture.Binding(publication)), rule, fixture.First, 1, 0);

        var wrongTarget = new Relation(Relation.Segment(sourceAxis, 0, 100), Relation.Segment(sourceAxis, 10, 0));
        var foreign = ReadWitness.Create(new Carrier(), new Placeholder(), wrongTarget,
            new CarrierLocation(fixture.Axis, 3), Group(), wrongTarget);
        AssertSelection(fixture.Select([rule, fixture.Fallback()], fixture.Binding(foreign)), null, fixture.FallbackResponse, 0, 1);
    }

    [Fact]
    public void AMalformedQualifiedShapeCannotDiscardItsVariationContract()
    {
        var fixture = new Fixture();
        var capacity = FiniteReadSurface.Capacity(new Carrier(), fixture.Axis, 0, 15);
        var malformed = ReadWitness.Create(new Carrier(), new Placeholder(),
            new Relation(Relation.Segment(fixture.Axis, 0, 1), capacity),
            new CarrierLocation(fixture.Axis, 5), Group(), new Placeholder());
        var rule = fixture.Rule(0, 15, fixture.First);
        AssertSelection(fixture.Select([rule, fixture.Fallback()], fixture.Binding(malformed)), null, fixture.FallbackResponse, 0, 1);
    }

    [Fact]
    public void APartitionPublicationMustExposeARegionUnionRatherThanAPretendPoint()
    {
        var fixture = new Fixture();
        var partition = CoarseReadSurface.Partition(new Carrier(), fixture.Axis, (0, 9), (10, 19));
        var malformed = ReadWitness.Create(new Carrier(), new Placeholder(), partition,
            new CarrierLocation(fixture.Axis, 7), Group(), new Placeholder());
        var rule = fixture.Rule(7, 7, fixture.First);
        AssertSelection(fixture.Select([rule, fixture.Fallback()], fixture.Binding(malformed)), null, fixture.FallbackResponse, 0, 1);
    }

    [Fact]
    public void MalformedConditionCannotBecomeUnconditional()
    {
        var fixture = new Fixture();
        var malformed = SupportDecisionSurface.Rule(new Carrier(), fixture.First,
            SupportDecisionSurface.Condition(new Carrier(), fixture.Role, Relation.Segment(fixture.Axis, 10, 0)));
        AssertSelection(fixture.Select([malformed, fixture.Fallback()], fixture.Binding(Point(fixture.Axis, 3))), null, fixture.FallbackResponse, 0, 1);
        Assert.False(SupportDecisionSurface.TryReadSelection(Group(), out _, out _, out _, out _));
        Assert.False(SupportDecisionSurface.TryReadRuleRead(Group(), out _, out _, out _, out _));
    }

    [Fact]
    public void EveryConditionMustAdmitItsOwnBoundRole()
    {
        var fixture = new Fixture();
        var other = new Placeholder();
        var both = SupportDecisionSurface.Rule(new Carrier(), fixture.First,
            SupportDecisionSurface.Condition(new Carrier(), fixture.Role, Relation.Segment(fixture.Axis, 0, 5)),
            SupportDecisionSurface.Condition(new Carrier(), other, Relation.Segment(fixture.Axis, 7, 9)));
        var binding = SupportDecisionSurface.Binding(new Carrier(), other, Point(fixture.Axis, 8));
        AssertSelection(fixture.Select([both, fixture.Fallback()], fixture.Binding(Point(fixture.Axis, 3)), binding), both, fixture.First, 2, 0);
        AssertSelection(fixture.Select([both, fixture.Fallback()], fixture.Binding(Point(fixture.Axis, 3))), null, fixture.FallbackResponse, 0, 1);
    }

    private sealed class Fixture
    {
        public Carrier Axis { get; } = new();
        public IElement Role { get; } = new Placeholder();
        public IElement First { get; } = new Placeholder();
        public IElement Second { get; } = new Placeholder();
        public IElement FallbackResponse { get; } = new Placeholder();
        public Relation Rule(long minimum, long maximum, IElement response) => SupportDecisionSurface.Rule(new Carrier(), response,
            SupportDecisionSurface.Condition(new Carrier(), Role, Relation.Segment(Axis, minimum, maximum)));
        public Relation Fallback() => SupportDecisionSurface.Rule(new Carrier(), FallbackResponse);
        public Relation Binding(Relation publication) => SupportDecisionSurface.Binding(new Carrier(), Role, publication);
        public Relation Select(Relation[] rules, params Relation[] bindings) =>
            SupportDecisionSurface.Select(new Carrier(), SupportDecisionSurface.Create(new Carrier(), rules), bindings);
    }

    private static Relation Point(Carrier axis, long value) => ReadWitness.Create(new Carrier(), new Placeholder(),
        Relation.Segment(axis, 0, 1), new CarrierLocation(axis, value), Group(), new Placeholder());
    private static Relation Group(params IElement[] fields) => Relation.OrderedGroup(new Carrier(), fields);
    private static IElement[] Group(Relation group) { Assert.True(group.TryReadOrderedGroup(out var fields)); return fields; }
    private static IElement[] Earlier(Relation selection)
    {
        Assert.True(SupportDecisionSurface.TryReadSelection(selection, out _, out _, out _, out var earlier)); return Group(earlier);
    }
    private static void AssertSelection(Relation selection, Relation? rule, IElement response, int admittedCount, int earlierCount)
    {
        Assert.True(SupportDecisionSurface.TryReadSelection(selection, out var selected, out var actual, out var admitted, out var earlier));
        // Passing null means the explicit unconditional fallback, whose rule
        // identity is available in the selection but was not prebound here.
        if (rule is not null) Assert.Same(rule, selected); else Assert.IsType<Relation>(selected);
        Assert.Same(response, actual); Assert.Equal(admittedCount, Group(admitted).Length); Assert.Equal(earlierCount, Group(earlier).Length);
    }
    private static void AssertRuleFailure(Relation read, Relation expected, int rejectedCount, int unresolvedCount)
    {
        Assert.True(SupportDecisionSurface.TryReadRuleRead(read, out var rule, out _, out var rejected, out var unresolved));
        Assert.Same(expected, rule); Assert.Equal(rejectedCount, Group(rejected).Length); Assert.Equal(unresolvedCount, Group(unresolved).Length);
    }
}

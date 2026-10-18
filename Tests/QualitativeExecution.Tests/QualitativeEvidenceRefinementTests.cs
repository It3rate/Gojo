using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeFrieze;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class QualitativeEvidenceRefinementTests
{
    [Fact]
    public void ActualUnknownObligationQueriesItsPortAndRefinesTheSameHeldOutIdentities()
    {
        var f = new Fixture(); var calls = new List<IElement>();
        var service = f.Service(4, request => { calls.Add(request.Node); return new(request, S(f.B), S(request.Node)); });
        var refinement = new QualitativeEvidenceRefinement(f.Before, f.Ports, service);
        Assert.Single(calls); Assert.Same(f.HeldOut[1], calls[0]); Assert.Single(E(refinement.Accepted)); Assert.Empty(E(refinement.Remaining));
        Assert.True(refinement.IsComplete); Assert.Single(E(refinement.NewFacts)); Assert.Empty(E(refinement.ReusedFacts));
        Assert.True(refinement.After.TryFindNode(f.HeldOut[1], out var actual, out var symbols)); Assert.Same(f.B, Assert.Single(E(symbols)));
        Assert.Same(actual, Assert.Single(E(refinement.NewFacts))); Assert.NotSame(f.Graph, refinement.After);
        Assert.True(f.Graph.TryFindNode(f.HeldOut[1], out var previous, out var oldSymbols)); Assert.False(Closed(oldSymbols));
        var correspondence = Assert.IsType<Relation>(Assert.Single(E(refinement.Correspondence)));
        Assert.Same(previous, correspondence.Left); Assert.Same(actual, correspondence.Right);
        var after = f.Grammar.Evaluate(refinement.After, f.Path); Assert.Single(E(after.Supported));
        Assert.Same(f.Graph.Links, refinement.After.Links); Assert.Single(E(service.Spent)); Assert.Equal(3, E(service.Available).Length);
    }

    [Fact]
    public void ContrarySupplierAnswerIsAcceptedAndRefutesTheExpectationInsteadOfBeingRewritten()
    {
        var f = new Fixture(); var service = f.Service(1, request => new(request, S(f.A), S(request.Node)));
        var refinement = new QualitativeEvidenceRefinement(f.Before, f.Ports, service);
        Assert.Single(E(refinement.Accepted)); Assert.Empty(E(refinement.Rejected));
        Assert.True(refinement.After.TryFindNode(f.HeldOut[1], out _, out var actual)); Assert.Same(f.A, Assert.Single(E(actual)));
        var after = f.Grammar.Evaluate(refinement.After, f.Path); Assert.Empty(E(after.Supported)); Assert.NotEmpty(E(after.Refuted));
        Assert.Empty(E(refinement.Remaining));
    }

    [Fact]
    public void EmptyCapacityLeavesTheHigherQuestionUnknownWithoutCallingTheSupplier()
    {
        var f = new Fixture(); var calls = 0; var service = f.Service(0, request => { calls++; return new(request, S(f.B), S()); });
        var refinement = new QualitativeEvidenceRefinement(f.Before, f.Ports, service);
        Assert.Equal(0, calls); Assert.Single(E(refinement.Blocked)); Assert.Single(E(refinement.Remaining));
        Assert.Empty(E(refinement.Accepted)); Assert.Empty(E(service.Spent)); Assert.Same(f.Graph, refinement.After);
        Assert.NotEmpty(E(f.Grammar.Evaluate(refinement.After, f.Path).Unresolved));
    }

    [Fact]
    public void TwoUnknownsShareOneServiceCapacityAndUnpaidRequestRemainsVisible()
    {
        var f = new Fixture(twoUnknown: true); var service = f.Service(1, request => new(request,
            S(ReferenceEquals(request.Node, f.HeldOut[0]) ? f.A : f.B), S(request.Node)));
        var refinement = new QualitativeEvidenceRefinement(f.Before, f.Ports, service);
        Assert.Single(E(refinement.Accepted)); Assert.Single(E(refinement.Blocked)); Assert.Single(E(refinement.Remaining));
        Assert.Single(E(service.Spent)); Assert.Empty(E(service.Available)); Assert.Equal(2, E(refinement.Requests).Length);
        Assert.NotEmpty(E(f.Grammar.Evaluate(refinement.After, f.Path).Unresolved));
    }

    [Fact]
    public void ReaskingTheSameObservationReusesTheActualPublicationWithoutNewSupportOrCapacity()
    {
        var f = new Fixture(); var calls = 0; var service = f.Service(1, request => { calls++; return new(request, S(f.B), S(request.Node)); });
        var first = new QualitativeEvidenceRefinement(f.Before, f.Ports, service);
        var second = new QualitativeEvidenceRefinement(f.Before, f.Ports, service);
        Assert.Equal(1, calls); Assert.Single(E(service.Spent)); Assert.Empty(E(second.Accepted)); Assert.Single(E(second.Reused));
        Assert.Empty(E(second.NewFacts)); Assert.Single(E(second.ReusedFacts)); Assert.Single(E(second.Facts));
        Assert.Same(Assert.Single(E(first.NewFacts)), Assert.Single(E(second.ReusedFacts)));
        Assert.Equal(4, E(second.After.Nodes).Length);
    }

    [Fact]
    public void CompetingExpectedPhasesShareOneActualSupplierObservationPerNode()
    {
        var f = new Fixture(allUnknown: true); var calls = 0;
        Assert.Equal(8, E(f.Before.Demands).Length);
        var service = f.Service(4, request => { calls++; var index = Array.FindIndex(f.HeldOut, node => ReferenceEquals(node, request.Node)); return new(request, S(index % 2 == 0 ? f.A : f.B), S()); });
        var refinement = new QualitativeEvidenceRefinement(f.Before, f.Ports, service);
        Assert.Equal(4, calls); Assert.Equal(8, E(refinement.Requests).Length); Assert.Equal(4, E(refinement.Accepted).Length);
        Assert.Equal(4, E(refinement.Reused).Length); Assert.Equal(4, E(refinement.Facts).Length); Assert.Equal(4, E(refinement.After.Nodes).Length);
        Assert.Equal(4, E(service.Spent).Length); Assert.Single(E(f.Grammar.Evaluate(refinement.After, f.Path).Supported));
    }

    [Fact]
    public void KnownNegativeOrPositiveHigherReadsDoNotAuthorizeQueriesToChangeTheirAnswers()
    {
        var f = new Fixture();
        foreach (var symbol in new[] { f.A, f.B })
        {
            var rows = E(f.Graph.Nodes).Cast<Relation>().Select(row => ReferenceEquals(row.Left, f.HeldOut[1]) ?
                (IElement)new Relation(row.Left, S(symbol)) : row).ToArray();
            var graph = new QualitativePatternTokenGraph(S(rows), f.Graph.Links); var read = f.Grammar.Evaluate(graph, f.Path);
            Assert.Empty(E(read.Demands)); var calls = 0;
            var refinement = new QualitativeEvidenceRefinement(read, f.Ports, f.Service(2, request => { calls++; return new(request, S(f.B), S()); }));
            Assert.Equal(0, calls); Assert.Empty(E(refinement.Requests)); Assert.Same(graph, refinement.After);
        }
    }

    [Fact]
    public void ClosedEmptyAnswerIsKnownAbsenceRatherThanARefusal()
    {
        var f = new Fixture(); var refinement = new QualitativeEvidenceRefinement(f.Before, f.Ports, f.Service(1, request => new(request, S(), S())));
        Assert.Single(E(refinement.Accepted)); Assert.Empty(E(refinement.Rejected)); Assert.Empty(E(refinement.Remaining));
        Assert.NotEmpty(E(f.Grammar.Evaluate(refinement.After, f.Path).Refuted));
    }

    [Fact]
    public void MissingOrCompetingRoutesDoNotTriggerHiddenFallbackQueries()
    {
        var f = new Fixture(); var calls = 0; var service = f.Service(2, request => { calls++; return new(request, S(f.B), S()); });
        var missing = new QualitativeEvidenceRefinement(f.Before, S(), service);
        Assert.Single(E(missing.Blocked)); Assert.Empty(E(missing.Requests));
        var route = E(f.Ports).Cast<Relation>().Single(row => ReferenceEquals(row.Left, f.HeldOut[1]));
        var competing = new QualitativeEvidenceRefinement(f.Before, S(route, new Relation(route.Left, route.Right)), service);
        Assert.Single(E(competing.Blocked)); Assert.Equal(0, calls); Assert.Empty(E(service.Spent));
    }

    [Fact]
    public void SupplierRefusalMalformedAddressAndStaleRevisionHaveDistinctNativeOutcomes()
    {
        var f = new Fixture();
        var refused = new QualitativeEvidenceRefinement(f.Before, f.Ports, f.Service(1, request => QualitativeEvidenceReply.Refuse(request, S(request.Node))));
        Assert.Single(E(refused.Rejected)); Assert.Empty(E(refused.Stale)); Assert.Same(f.Graph, refused.After);
        var malformed = new QualitativeEvidenceRefinement(f.Before, f.Ports, f.Service(1, request => new(request, request.Port, request.Revision,
            S(new Relation(new Placeholder(), S(f.B))), S())));
        Assert.Single(E(malformed.Rejected)); Assert.Empty(E(malformed.Stale)); Assert.Empty(E(malformed.Facts));
        var stale = new QualitativeEvidenceRefinement(f.Before, f.Ports, f.Service(1, request => new(request, S(f.B), S(), new Placeholder())));
        Assert.Single(E(stale.Stale)); Assert.Empty(E(stale.Rejected)); Assert.Single(E(stale.Remaining)); Assert.Same(f.Graph, stale.After);
    }

    [Fact]
    public void PartialOrAmbiguousReplyDoesNotPretendTheHigherObligationHasSettled()
    {
        var f = new Fixture();
        foreach (var answer in new[] { StructuralConstraint.Open(f.B), S(f.A, f.B) })
        {
            var refinement = new QualitativeEvidenceRefinement(f.Before, f.Ports, f.Service(1, request => new(request, answer, S())));
            Assert.Single(E(refinement.Accepted)); Assert.Single(E(refinement.Remaining)); Assert.False(refinement.IsComplete);
            Assert.NotEmpty(E(f.Grammar.Evaluate(refinement.After, f.Path).Unresolved));
        }
    }

    [Fact]
    public void ClosedAlternativeSupportCanBeNarrowedButCannotAcquireAnExcludedPossibility()
    {
        var f = new Fixture(finiteUnknown: true);
        var narrowed = new QualitativeEvidenceRefinement(f.Before, f.Ports, f.Service(1, request => new(request, S(f.A), S())));
        Assert.Single(E(narrowed.Accepted)); Assert.Empty(E(narrowed.Rejected));
        var foreign = new Relation(new Placeholder(), new Placeholder());
        var contradicted = new QualitativeEvidenceRefinement(f.Before, f.Ports, f.Service(1, request => new(request, S(foreign), S())));
        Assert.Single(E(contradicted.Rejected)); Assert.Empty(E(contradicted.Accepted)); Assert.Same(f.Graph, contradicted.After);
    }

    [Fact]
    public void UnselectedUnknownNodeIsNeverRequestedAndCopiedDemandOrRequestCannotBecomeEvidence()
    {
        var f = new Fixture(extraUnknown: true); var seen = new List<IElement>();
        var refinement = new QualitativeEvidenceRefinement(f.Before, f.Ports, f.Service(2, request => { seen.Add(request.Node); return new(request, S(f.B), S()); }));
        Assert.Single(seen); Assert.Same(f.HeldOut[1], seen[0]);
        var demand = Assert.IsType<Relation>(Assert.Single(E(f.Before.Demands)));
        Assert.False(f.Before.TryReadDemand(new Relation(demand.Left, demand.Right), out _));
        var request = Assert.IsType<Relation>(Assert.Single(E(refinement.Requests)));
        Assert.True(refinement.TryReadRequest(request, out var read)); Assert.Same(demand, read.Obligation);
        Assert.False(refinement.TryReadRequest(new Relation(request.Left, request.Right), out _));
    }

    [Fact]
    public void QualitativeTimelineConnectsEveryBeforeAfterAndPreservesBlockedRemainder()
    {
        var f = new Fixture(twoUnknown: true); var refinement = new QualitativeEvidenceRefinement(f.Before, f.Ports,
            f.Service(1, request => new(request, S(f.A), S())));
        var steps = E((Relation)refinement.Timeline.Left).Cast<Relation>().ToArray(); Assert.Equal(3, steps.Length);
        Assert.Same(f.Graph.Surface, steps[0].Left);
        for (var i = 1; i < steps.Length; i++) Assert.Same(((Relation)((Relation)steps[i - 1].Right).Right).Left, steps[i].Left);
        Assert.Same(refinement.After.Surface, ((Relation)((Relation)steps[^1].Right).Right).Left);
        Assert.Same(refinement.Remaining, ((Relation)((Relation)steps[^1].Right).Right).Right);
        Assert.Equal(steps.Length - 1, E((Relation)refinement.Timeline.Right).Length);
        AssertNoNumbers(refinement.Surface);
    }

    [Fact]
    public void UpperUnknownCanCauseASecondLocalQueryWithoutPublishingThePrivateLowerWorld()
    {
        var lower = new Fixture();
        var rowDefinition = Assert.Single(lower.Before.Matches, match => match.IsUnresolved).FramedSymbol;
        var upper = new Fixture(symbolB: rowDefinition);
        QualitativeEvidenceRefinement? nested = null; QualitativeEvidenceRequest? actualUpper = null;
        var fine = lower.Service(1, request => new(request, S(lower.B), S(request.Node)));
        var coarse = upper.Service(1, request =>
        {
            actualUpper = request;
            nested = new(lower.Before, lower.Ports, request.Read, fine);
            var localRead = lower.Grammar.Evaluate(nested.After, lower.Path);
            var actualRow = Assert.Single(localRead.Matches, match => match.IsSupported).FramedSymbol;
            return new(request, S(actualRow), S(new Relation(request.Node, actualRow)));
        });
        var top = new QualitativeEvidenceRefinement(upper.Before, upper.Ports, coarse);
        Assert.NotNull(nested); Assert.NotNull(actualUpper); Assert.Same(actualUpper.Read, nested.ParentCause);
        Assert.Same(actualUpper.Read, nested.CausalBridge.Left); Assert.Single(E(nested.Accepted)); Assert.Single(E(top.Accepted));
        Assert.Single(E(upper.Grammar.Evaluate(top.After, upper.Path).Supported));
        var publicTop = Reach(top.Surface);
        Assert.DoesNotContain(lower.HeldOut, node => publicTop.Contains(node));
        Assert.DoesNotContain(publicTop, element => ReferenceEquals(element, lower.Before.Surface));
        Assert.Contains(Reach(nested.Surface), element => ReferenceEquals(element, lower.Before.Surface));
    }

    private sealed class Fixture
    {
        internal Relation A { get; } = new(new Placeholder(), new Placeholder());
        internal Relation B { get; } = new(new Placeholder(), new Placeholder());
        internal IElement[] HeldOut { get; } = Enumerable.Range(0, 4).Select(_ => (IElement)new Placeholder()).ToArray();
        internal QualitativePatternGrammar Grammar { get; }
        internal QualitativePatternTokenGraph Graph { get; }
        internal QualitativePatternGrammarEvaluation Before { get; }
        internal Relation Path { get; }
        internal Relation Ports { get; }
        internal Fixture(bool twoUnknown = false, bool finiteUnknown = false, bool extraUnknown = false, bool allUnknown = false, Relation? symbolB = null)
        {
            B = symbolB ?? B;
            var training = Enumerable.Range(0, 6).Select(_ => (IElement)new Placeholder()).ToArray();
            var train = new QualitativePatternTokenGraph(S(training.Select((node, index) => (IElement)new Relation(node, S(index % 2 == 0 ? A : B))).ToArray()), Links(training));
            Grammar = new(train, S(S(training)), maxPeriod: 2); Assert.Single(Grammar.Grammars);
            var rows = HeldOut.Select((node, index) => (IElement)new Relation(node,
                allUnknown ? StructuralConstraint.Open() : index == 1 ? finiteUnknown ? S(A, B) : StructuralConstraint.Open() : index == 0 && twoUnknown ? StructuralConstraint.Open() : S(index % 2 == 0 ? A : B))).ToList();
            if (extraUnknown) rows.Add(new Relation(new Placeholder(), StructuralConstraint.Open()));
            Graph = new(S(rows.ToArray()), Links(HeldOut)); Path = S(S(HeldOut)); Before = Grammar.Evaluate(Graph, Path);
            Ports = S(E(Graph.Nodes).Cast<Relation>().Select(row => (IElement)new Relation(row.Left, new Placeholder())).ToArray());
            Assert.NotEmpty(E(Before.Demands));
        }
        internal QualitativeEvidenceService Service(int capacity, Func<QualitativeEvidenceRequest, QualitativeEvidenceReply> read) =>
            new(new Relation(Ports, S(A, B)), Ports, S(A, B), capacity, read);
        private static Relation Links(IElement[] nodes) => S(nodes.Zip(nodes.Skip(1)).Select(pair => (IElement)new Relation(new Placeholder(), new Relation(pair.First, pair.Second))).ToArray());
    }
    private static void AssertNoNumbers(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = new Stack<IElement>(); work.Push(root);
        while (work.TryPop(out var item)) if (seen.Add(item)) { Assert.IsNotType<Carrier>(item); Assert.IsNotType<CarrierLocation>(item); if (item is Relation relation) { work.Push(relation.Left); work.Push(relation.Right); } }
    }
    private static HashSet<IElement> Reach(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var work = new Stack<IElement>(); work.Push(root);
        while (work.TryPop(out var item)) if (seen.Add(item) && item is Relation row) { work.Push(row.Left); work.Push(row.Right); }
        return seen;
    }
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var seam, out var tail) && seam.Equals(tail);
    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => StructuralConstraint.TryReadScope(scope, out var entries, out _, out _) ? entries : [];
}

using Gojo.Core;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeFrieze;
using Gojo.Patterns;
using D = Gojo.Experiments.QualitativeFrieze.OrnamentDefaultsPolicy;
using E = Gojo.Experiments.QualitativeFrieze.StructuralEvidenceRules;

namespace QualitativeExecution.Tests;

public sealed class StructuralEvidenceServiceTests
{
    [Fact]
    public void Unequal_reach_refuses_a_square_before_any_finer_premise_is_requested()
    {
        var frame = new StructuralPotential.Frame(); var source = frame.Publish(); var calls = new List<Relation>();
        var service = Service(source, S(frame.Leaves.Select(leaf => (IElement)leaf.Predicate).ToArray()), request =>
        {
            calls.Add(request.Predicate); Assert.Same(frame.EqualSpacing, request.Predicate);
            return new(request, Fact(request.Predicate, "No"));
        });
        var read = E.Read(frame.Laws, source, frame.FiniteSquare, service);
        Assert.Equal("No", read.Answer); Assert.Equal(new[] { frame.EqualSpacing }, calls);
        Assert.Equal(new[] { frame.FiniteSquare, frame.EqualSpacing }, Items(read.Visited));
        Assert.Equal(new[] { frame.FiniteRectangle }, Items(read.Unasked));
        Assert.Single(Items(read.ServiceRequests)); Assert.Single(Items(read.ServiceAccepted));
        Assert.Empty(source.Evidence); Assert.Equal("No", Assert.Single(read.ObservedPublication.Evidence).Answer);
        Assert.Same(source, read.Source); Assert.Same(source.Source, read.ObservedPublication.Source);
        Assert.All(Items(read.ServiceRequests), request => Assert.Contains(request, Participants(read.ObservedPublication.Surface)));
        Assert.All(Items(read.ServiceAccepted), accepted => Assert.Contains(accepted, Participants(read.ObservedPublication.Surface)));
    }

    [Fact]
    public void A_rectangle_never_requests_equal_reach_or_unselected_uniformity()
    {
        var frame = new StructuralPotential.Frame(); var source = frame.Publish(); var called = new List<Relation>();
        var service = Service(source, S(frame.Leaves.Select(leaf => (IElement)leaf.Predicate).ToArray()), request =>
        {
            called.Add(request.Predicate); Assert.NotSame(frame.EqualSpacing, request.Predicate);
            Assert.NotSame(frame.UniformA, request.Predicate); Assert.NotSame(frame.UniformB, request.Predicate);
            return new(request, Fact(request.Predicate, "Yes"));
        });
        var read = E.Read(frame.Laws, source, frame.FiniteRectangle, service);
        Assert.Equal("Yes", read.Answer); Assert.Equal(9, called.Count);
        Assert.Equal(called.Count, read.ObservedPublication.Evidence.Count); Assert.Empty(Items(read.ServiceBlocked));
        Assert.Contains(read.ServiceAccepted, Participants(read.ToEvidence().Surface));
    }

    [Fact]
    public void Supporting_line_square_potential_does_not_ask_for_finite_spans()
    {
        var frame = new StructuralPotential.Frame(); var source = frame.Publish();
        var service = Service(source, S(frame.Leaves.Select(leaf => (IElement)leaf.Predicate).ToArray()), request =>
        {
            Assert.DoesNotContain(frame.SpanCoverage, span => ReferenceEquals(span, request.Predicate));
            return new(request, Fact(request.Predicate, "Yes"));
        });
        var read = E.Read(frame.Laws, source, frame.PotentialSquare, service);
        Assert.Equal("Yes", read.Answer); Assert.Equal(6, Items(read.ServiceAccepted).Length);
    }

    [Fact]
    public void Shared_subgoals_call_the_supplier_once_and_capacity_resets_for_each_application()
    {
        var leaf = Predicate(); var left = Predicate(); var right = Predicate(); var goal = Predicate(); var calls = 0;
        var program = new E.Program([new("root", goal, [left, right]), new("left", left, [leaf]), new("right", right, [leaf])]);
        var source = new E.Publication(Predicate(), []);
        var service = Service(source, S(leaf), request => { calls++; return new(request, Fact(leaf, "Yes")); }, 1);
        foreach (var _ in new[] { 0, 1 })
        {
            var read = E.Read(program, source, goal, service);
            Assert.Equal("Yes", read.Answer); Assert.Single(Items(read.ServiceConsumed)); Assert.Single(Items(read.ServiceRequests));
        }
        Assert.Equal(2, calls); Assert.Empty(source.Evidence);
    }

    [Fact]
    public void Existing_open_accounts_and_rule_conclusions_never_invoke_a_supplier()
    {
        var leaf = Predicate(); var goal = Predicate(); var source = new E.Publication(Predicate(), [Fact(leaf, "Open")]);
        var service = Service(source, S(leaf, goal), _ => throw new InvalidOperationException("No new account was requested."));
        var read = E.Read(new([new("root", goal, [leaf])]), source, goal, service);
        Assert.Equal("Open", read.Answer); Assert.Empty(Items(read.ServiceRequests));
        Assert.Same(Assert.Single(source.Evidence).Surface, Assert.Single(read.ObservedPublication.Evidence).Surface);
        Assert.Equal(new[] { leaf }, Items(read.Missing));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Missing_routes_and_zero_service_capacity_remain_open_without_becoming_source_negatives(bool noCapacity)
    {
        var leaf = Predicate(); var source = new E.Publication(Predicate(), []);
        var service = Service(source, noCapacity ? S(leaf) : S(), _ => throw new InvalidOperationException("Unavailable service must not run."), noCapacity ? 0 : 1);
        var read = E.Read(new([]), source, leaf, service);
        Assert.Equal("Open", read.Answer); Assert.Equal(new[] { leaf }, Items(read.Missing));
        Assert.NotEmpty(Items(read.ServiceOpen)); Assert.Empty(Items(read.Refuted)); Assert.Empty(read.ObservedPublication.Evidence);
        Assert.Empty(Items(read.BudgetCuts)); Assert.Empty(Items(read.ServiceConsumed));
    }

    [Theory]
    [InlineData("source")]
    [InlineData("route")]
    [InlineData("request")]
    [InlineData("revision")]
    [InlineData("predicate")]
    public void Foreign_sources_malformed_routes_and_substituted_replies_are_blocked(string failure)
    {
        var leaf = Predicate(); var source = new E.Publication(Predicate(), []); var calls = 0;
        var service = new E.Service(failure == "source" ? Predicate() : source.Source, Predicate(), new Placeholder(),
            failure == "route" ? StructuralConstraint.Open(leaf) : S(leaf), 1, request =>
            {
                calls++;
                if (failure == "predicate") return new(request, Fact(new(leaf.Left, leaf.Right), "Yes"));
                var other = failure == "revision" ? new E.Request(request.Source, request.Port, new Placeholder(), leaf, request.Context) :
                    new E.Request(request.Source, request.Port, request.Revision, leaf, request.Context);
                return new(other, Fact(leaf, "Yes"));
            });
        var read = E.Read(new([]), source, leaf, service);
        Assert.Equal("Blocked", read.Answer); Assert.NotEmpty(Items(read.ServiceBlocked));
        Assert.Empty(read.ObservedPublication.Evidence); Assert.Empty(Items(read.ServiceAccepted));
        Assert.Equal(failure is "source" or "route" ? 0 : 1, calls);
    }

    [Fact]
    public void A_service_reply_can_preserve_opposed_accounts_without_choosing_a_polarity()
    {
        var leaf = Predicate(); var source = new E.Publication(Predicate(), []);
        var opposed = new D.Evidence(leaf, S(Predicate()), S(Predicate()), S(), S());
        var read = E.Read(new([]), source, leaf, Service(source, S(leaf), request => new(request, opposed)));
        Assert.Equal("Conflict", read.Answer); Assert.Single(Items(read.ServiceAccepted));
        var fact = read.ToEvidence(); Assert.NotEmpty(Items(fact.Supported)); Assert.NotEmpty(Items(fact.Refuted));
        Assert.Contains(opposed.Surface, Participants(fact.Surface)); Assert.Contains(read.ServiceRequests, Participants(fact.Surface));
    }

    [Fact]
    public void Traversal_budget_exhaustion_never_calls_even_an_available_service()
    {
        var leaf = Predicate(); var source = new E.Publication(Predicate(), []);
        var read = E.Read(new([]), source, leaf,
            Service(source, S(leaf), _ => throw new InvalidOperationException("No traversal capacity.")), 0);
        Assert.Equal("Open", read.Answer); Assert.Equal(new[] { leaf }, Items(read.BudgetCuts));
        Assert.Empty(Items(read.Missing)); Assert.Empty(Items(read.ServiceRequests));
    }

    [Fact]
    public void Accepted_native_service_evidence_transports_into_a_fresh_eager_reader_with_all_aliases()
    {
        var leaf = Predicate(); var goal = Predicate(); var source = new E.Publication(Predicate(), []);
        var program = new E.Program([new("root", goal, [leaf])]);
        var first = E.Read(program, source, goal, Service(source, S(leaf), request => new(request, Fact(leaf, "Yes"))));
        var world = Restore(new(program.Surface, new Relation(first.ObservedPublication.Surface, new Relation(goal, first.Surface))));
        var body = Assert.IsType<Relation>(world.Right); var goals = Assert.IsType<Relation>(body.Right);
        var importedSource = new E.Publication(Assert.IsType<Relation>(body.Left));
        var second = E.Read(new(Assert.IsType<Relation>(world.Left)), importedSource, Assert.IsType<Relation>(goals.Left));
        Assert.Equal("Yes", second.Answer); Assert.Empty(Items(second.ServiceRequests));
        Assert.NotSame(source.Source, importedSource.Source); Assert.Single(importedSource.Evidence);
        Assert.NotEmpty(Participants(Assert.IsType<Relation>(goals.Right)));
    }

    private static E.Service Service(E.Publication source, Relation predicates, Func<E.Request, E.Reply> handler, int capacity = 32) =>
        new(source.Source, Predicate(), new Placeholder(), predicates, capacity, handler);
    private static Relation Predicate() => new(new Placeholder(), new Placeholder());
    private static D.Evidence Fact(Relation predicate, string answer) => D.Evidence.FromAnswer(predicate, answer, new(predicate, new Placeholder()));
    private static Relation S(params IElement[] rows) => StructuralConstraint.Closed(rows);
    private static IElement[] Items(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static HashSet<IElement> Participants(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance); var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.TryPop(out var item))
        { if (!seen.Add(item) || item is not Relation row) continue; pending.Push(row.Left); pending.Push(row.Right); }
        return seen;
    }
    private static Relation Restore(Relation surface)
    {
        var json = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("service-world", surface));
        Assert.True(GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(json), out var restored));
        return Assert.IsType<Relation>(restored.Root);
    }
}

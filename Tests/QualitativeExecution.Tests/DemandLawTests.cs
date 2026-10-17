using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Experiments.QualitativeExecution.Tests;

public sealed class DemandLawTests
{
    [Fact]
    public void QuestionCanSettleOverAllOutcomesBeforeEitherSupplierRuns()
    {
        var f = new Fixture();
        var sameAnswer = new Placeholder();
        var routes = S(new Relation(f.No, sameAnswer), new Relation(f.Yes, sameAnswer));
        var application = DemandLaw.Create(f.Law, f.Left, f.Right, routes);
        var receipt = DemandLaw.Ask(application);
        AssertAnswer(receipt, sameAnswer);
        Assert.Same(receipt, DemandLaw.Service(receipt, _ => throw new InvalidOperationException("Unneeded supplier.")));
        Assert.True(Contains(receipt, f.LeftPublication));
        Assert.True(Contains(receipt, f.RightPublication));
        AssertNoCarriers(receipt);
    }

    [Fact]
    public void AdvertisedShortCircuitSkipsTheSupplierForADeepBranch()
    {
        var f = new Fixture(leftSupport: false);
        // The second port may represent a large branch. Only its advertised
        // output contract is needed: every admitted right state still says No.
        var receipt = DemandLaw.Ask(f.Application);
        AssertAnswer(receipt, f.Stop);
        DemandLaw.Service(receipt, _ => throw new InvalidOperationException("Nested branch must stay unevaluated."));
        Assert.True(DemandLaw.TryRead(receipt, out _, out _, out var requests, out _, out _));
        Assert.Empty(E(requests));
    }

    [Fact]
    public void FirstRefinementCanSettleTheAnswerAndSkipTheSecondSupplier()
    {
        var f = new Fixture();
        var receipt = DemandLaw.Ask(f.Application);
        Assert.True(DemandLaw.TryRead(receipt, out _, out _, out var requests, out _, out _));
        Assert.Equal(new IElement[] { f.Left, f.Right }, E(requests));
        var offered = Narrow(f.Left, S(f.No));
        var visits = 0;
        var serviced = DemandLaw.Service(receipt, port =>
        {
            visits++;
            Assert.Same(f.Left, port);
            return offered;
        });
        Assert.Equal(1, visits);
        AssertAnswer(serviced, f.Stop);
        Assert.True(Contains(serviced, receipt));
        Assert.True(Contains(serviced, offered));
        Assert.True(Contains(serviced, f.LeftPublication));
        AssertCurrentLeftSupport(serviced, f.No);
        AssertCurrentLeftSupport(receipt, f.No, f.Yes);
        AssertNoCarriers(serviced);
    }

    [Fact]
    public void NecessarySecondSupplierIsQueriedAfterTheFirstInNativePortOrder()
    {
        var f = new Fixture();
        var visited = new List<IElement>();
        var leftOffer = Narrow(f.Left, S(f.Yes));
        var rightOffer = Narrow(f.Right, S(f.No));
        var serviced = DemandLaw.Service(DemandLaw.Ask(f.Application), port =>
        {
            visited.Add(port);
            return ReferenceEquals(port, f.Left) ? leftOffer : rightOffer;
        });
        Assert.Equal(new IElement[] { f.Left, f.Right }, visited);
        AssertAnswer(serviced, f.Stop);
        Assert.True(Contains(serviced, leftOffer)); Assert.True(Contains(serviced, rightOffer));
        AssertCurrentLeftSupport(serviced, f.Yes);
    }

    [Fact]
    public void NestedDemandBranchesAreOnlyEvaluatedWhenTheirPortIsRequested()
    {
        var outer = new Fixture();
        var inner = new Fixture();
        var nestedCalls = 0;
        var result = DemandLaw.Service(DemandLaw.Ask(outer.Application), port =>
        {
            if (ReferenceEquals(port, outer.Left)) return Narrow(outer.Left, S(outer.No));
            nestedCalls++;
            // This is an independently meaningful demand application. Its
            // entire execution would be a supplier for the outer right port.
            DemandLaw.Service(DemandLaw.Ask(inner.Application), _ =>
                throw new InvalidOperationException("Deep supplier was evaluated."));
            return Narrow(outer.Right, S(outer.Yes));
        });
        AssertAnswer(result, outer.Stop);
        Assert.Equal(0, nestedCalls);
    }

    [Fact]
    public void OpenKnownSupportDoesNotCertifyAUniversalAnswer()
    {
        var f = new Fixture();
        var openLeft = DemandLaw.Port(f.Left.Left, Narrow(f.Left, StructuralConstraint.Open(f.No)));
        var application = DemandLaw.Create(f.Law, openLeft, f.Right, f.Routes);
        var initial = DemandLaw.Ask(application);
        Assert.False(DemandLaw.TryReadAnswer(initial, out _));
        Assert.True(DemandLaw.TryRead(initial, out _, out var answers, out _, out _, out _));
        Assert.Same(f.Stop, Assert.Single(E(answers)));
        Assert.True(StructuralConstraint.TryReadScope(answers, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
        var visits = 0;
        var serviced = DemandLaw.Service(initial, port =>
        {
            visits++;
            Assert.Same(openLeft, port);
            return Narrow(openLeft, S(f.No));
        });
        Assert.Equal(1, visits);
        AssertAnswer(serviced, f.Stop);
    }

    [Fact]
    public void ClosedEmptyPremiseIsConflictAndNeverRequestsAFallbackSample()
    {
        var f = new Fixture();
        var impossible = DemandLaw.Port(f.Left.Left, Narrow(f.Left, S()));
        var receipt = DemandLaw.Ask(DemandLaw.Create(f.Law, impossible, f.Right, f.Routes));
        Assert.False(DemandLaw.TryReadAnswer(receipt, out _));
        Assert.True(DemandLaw.TryRead(receipt, out _, out var answers, out var requests, out _, out _));
        Assert.Empty(E(answers)); Assert.Empty(E(requests));
        Assert.True(StructuralConstraint.TryReadScope(answers, out _, out var seam, out var tail));
        Assert.Equal(seam, tail);
        DemandLaw.Service(receipt, _ => throw new InvalidOperationException("Conflict is not an implicit refinement request."));
    }

    [Fact]
    public void MissingPairPreventsUniformKnownOutputsFromPretendingTheAnswerIsComplete()
    {
        var f = new Fixture();
        var incompleteLaw = FiniteLaw.Create(f.Domain, f.Domain, f.Domain,
            FiniteLaw.Row(f.No, f.No, S(f.No)),
            FiniteLaw.Row(f.No, f.Yes, S(f.No)),
            FiniteLaw.Row(f.Yes, f.No, S(f.No)));
        var receipt = DemandLaw.Ask(DemandLaw.Create(incompleteLaw, f.Left, f.Right, f.Routes));
        Assert.False(DemandLaw.TryReadAnswer(receipt, out _));
        Assert.True(DemandLaw.TryRead(receipt, out _, out var answers, out var requests, out _, out var proofs));
        Assert.Same(f.Stop, Assert.Single(E(answers)));
        Assert.Equal(2, E(requests).Length);
        Assert.True(Contains(proofs, incompleteLaw));
        var uncoveredLeft = DemandLaw.Port(f.Left.Left, Narrow(f.Left, S(f.Yes)));
        var uncoveredRight = DemandLaw.Port(f.Right.Left, Narrow(f.Right, S(f.Yes)));
        var uncovered = DemandLaw.Ask(DemandLaw.Create(incompleteLaw, uncoveredLeft, uncoveredRight, f.Routes));
        Assert.False(DemandLaw.TryReadAnswer(uncovered, out _));
        Assert.True(DemandLaw.TryRead(uncovered, out _, out var unknown, out var noUsefulQueries, out _, out _));
        Assert.Empty(E(unknown)); Assert.Empty(E(noUsefulQueries));
        Assert.True(StructuralConstraint.TryReadScope(unknown, out _, out var a, out var b));
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void MissingOrOpenAnswerRoutesDoNotRequestAlreadyExactOperands()
    {
        var f = new Fixture(leftSupport: true, rightSupport: true);
        foreach (var routes in new[] { S(), StructuralConstraint.Open(new Relation(f.Yes, f.Go)) })
        {
            var receipt = DemandLaw.Ask(DemandLaw.Create(f.Law, f.Left, f.Right, routes));
            Assert.False(DemandLaw.TryReadAnswer(receipt, out _));
            Assert.True(DemandLaw.TryRead(receipt, out _, out _, out var requests, out _, out _));
            Assert.Empty(E(requests));
            DemandLaw.Service(receipt, _ => throw new InvalidOperationException("Missing rule is not missing operand detail."));
        }
    }

    [Theory]
    [InlineData(0)] // foreign source
    [InlineData(1)] // foreign subject
    [InlineData(2)] // new observation occurrence
    [InlineData(3)] // changed basis
    [InlineData(4)] // lookalike domain
    [InlineData(5)] // foreign outcome
    [InlineData(6)] // closed empty offer
    [InlineData(7)] // incomplete offer
    public void IncompatibleOffersRetainTheOldClaimAndCounterexample(int mismatch)
    {
        var f = new Fixture();
        Assert.True(QualitativePublication.TryRead(f.LeftPublication, out var source, out var subject,
            out var eventIdentity, out var basis, out var domain, out _, out _));
        var support = mismatch switch
        {
            5 => S(new Placeholder()),
            6 => S(),
            7 => StructuralConstraint.Open(f.No),
            _ => S(f.No)
        };
        var offered = QualitativePublication.Create(mismatch == 0 ? new Placeholder() : source,
            mismatch == 1 ? new Placeholder() : subject,
            mismatch == 2 ? new Placeholder() : eventIdentity,
            mismatch == 3 ? new Placeholder() : basis,
            mismatch == 4 ? S(f.No, f.Yes) : domain, support);
        var receipt = DemandLaw.Ask(f.Application);
        var calls = 0;
        var serviced = DemandLaw.Service(receipt, port =>
        {
            calls++;
            return ReferenceEquals(port, f.Left) ? offered : f.RightPublication;
        });
        Assert.Equal(2, calls);
        Assert.False(DemandLaw.TryReadAnswer(serviced, out _));
        Assert.True(DemandLaw.TryRead(serviced, out _, out _, out _, out var blocked, out _));
        Assert.Single(E(blocked));
        Assert.True(Contains(blocked, offered));
        Assert.True(Contains(serviced, f.LeftPublication));
        AssertCurrentLeftSupport(serviced, f.No, f.Yes);
    }

    [Fact]
    public void NoProgressPublicationIsQueriedAtMostOncePerPortPerExplicitServiceCall()
    {
        var f = new Fixture();
        var calls = 0;
        var serviced = DemandLaw.Service(DemandLaw.Ask(f.Application), port =>
        {
            calls++;
            return ReferenceEquals(port, f.Left) ? f.LeftPublication : f.RightPublication;
        });
        Assert.Equal(2, calls);
        Assert.False(DemandLaw.TryReadAnswer(serviced, out _));
        Assert.True(DemandLaw.TryRead(serviced, out _, out _, out var requests, out var blocked, out _));
        Assert.Equal(2, E(requests).Length); Assert.Empty(E(blocked));
    }

    [Fact]
    public void WrongDomainOrAmbiguousPortIdentityBlocksBeforeSupplierInvocation()
    {
        var f = new Fixture();
        var otherDomain = S(f.No, f.Yes);
        var wrongPublication = QualitativePublication.Create(new Placeholder(), new Placeholder(), new Placeholder(),
            new Placeholder(), otherDomain, otherDomain);
        foreach (var left in new[]
        {
            DemandLaw.Port(new Placeholder(), wrongPublication),
            DemandLaw.Port(f.Right.Left, f.LeftPublication)
        })
        {
            var receipt = DemandLaw.Ask(DemandLaw.Create(f.Law, left, f.Right, f.Routes));
            Assert.False(DemandLaw.TryReadAnswer(receipt, out _));
            Assert.True(DemandLaw.TryRead(receipt, out _, out _, out var requests, out var blocked, out _));
            Assert.Empty(E(requests)); Assert.Single(E(blocked));
            DemandLaw.Service(receipt, _ => throw new InvalidOperationException("Unadmitted contract must not query."));
        }
    }

    [Fact]
    public void BroaderOfferCannotReplaceAnExistingNarrowerObservation()
    {
        var a = new Placeholder(); var b = new Placeholder(); var c = new Placeholder();
        var context = new Placeholder(); var no = new Placeholder(); var yes = new Placeholder();
        var domain = S(a, b, c); var other = S(context); var outcomes = S(no, yes);
        var law = FiniteLaw.Create(domain, other, outcomes,
            FiniteLaw.Row(a, context, S(no)), FiniteLaw.Row(b, context, S(yes)),
            FiniteLaw.Row(c, context, S(no)));
        var publication = QualitativePublication.Create(new Placeholder(), new Placeholder(), new Placeholder(),
            new Placeholder(), domain, S(a, b));
        var left = DemandLaw.Port(new Placeholder(), publication);
        var right = DemandLaw.Port(new Placeholder(), QualitativePublication.Create(new Placeholder(), new Placeholder(),
            new Placeholder(), new Placeholder(), other, other));
        var application = DemandLaw.Create(law, left, right, S(new Relation(no, no), new Relation(yes, yes)));
        var offered = Narrow(left, domain);
        var serviced = DemandLaw.Service(DemandLaw.Ask(application), port =>
        {
            Assert.Same(left, port);
            return offered;
        });
        Assert.False(DemandLaw.TryReadAnswer(serviced, out _));
        AssertCurrentLeftSupport(serviced, a, b);
        Assert.True(DemandLaw.TryRead(serviced, out _, out _, out _, out var blocked, out _));
        Assert.Single(E(blocked)); Assert.True(Contains(blocked, offered));
    }

    [Fact]
    public void SelectingAnActionLeavesPhysicalStateForAnExplicitIndependentTransition()
    {
        var f = new Fixture(leftSupport: true, rightSupport: true);
        var stock = new QualitativeStockFixture();
        var owner = new Placeholder(); var beforeEvent = new Placeholder();
        var before = stock.Initial(owner, beforeEvent, S(stock.Available));
        var receipt = DemandLaw.Ask(f.Application);
        AssertAnswer(receipt, f.Go);
        DemandLaw.Service(receipt, _ => throw new InvalidOperationException("Finer observation is unnecessary."));
        Assert.True(stock.TryReadState(before, out _, out _, out var oldSupport));
        Assert.Same(stock.Available, Assert.Single(E(oldSupport)));
        // Decay is independent physical maintenance, still explicitly offered
        // when this observation question has no remaining demand.
        var offer = stock.Offer(owner, new Placeholder(), beforeEvent, S(stock.Draw));
        var transition = stock.Step(before, offer);
        Assert.True(stock.TryReadStep(transition, out _, out _, out var after, out _, out _));
        Assert.True(stock.TryReadState(after, out _, out _, out var afterSupport));
        Assert.Same(stock.Empty, Assert.Single(E(afterSupport)));
        Assert.True(Contains(transition, before));
    }

    [Fact]
    public void RebuiltLawDoesNotSilentlyRewriteAnEarlierApplication()
    {
        var f = new Fixture(leftSupport: false, rightSupport: true);
        var receipt = DemandLaw.Ask(f.Application);
        AssertAnswer(receipt, f.Stop);
        Assert.True(FiniteLaw.TryRead(f.Law, out _, out _, out _, out var rows));
        rows[1] = FiniteLaw.Row(f.No, f.Yes, S(f.Yes));
        var otherLaw = FiniteLaw.Create(f.Domain, f.Domain, f.Domain, rows);
        var otherApplication = DemandLaw.Create(otherLaw, f.Left, f.Right, f.Routes);
        AssertAnswer(DemandLaw.Ask(otherApplication), f.Go);
        AssertAnswer(receipt, f.Stop);
        Assert.Same(receipt, DemandLaw.Service(receipt, _ => throw new InvalidOperationException("The earlier application remains certified.")));
    }

    [Fact]
    public void RebuiltAnswerRoutesRequireAnExplicitApplication()
    {
        var f = new Fixture(leftSupport: false, rightSupport: true);
        var receipt = DemandLaw.Ask(f.Application);
        var routes = S(new Relation(f.No, f.Go), new Relation(f.Yes, f.Stop));
        var otherApplication = DemandLaw.Create(f.Law, f.Left, f.Right, routes);
        AssertAnswer(DemandLaw.Ask(otherApplication), f.Go);
        AssertAnswer(receipt, f.Stop);
    }

    [Fact]
    public void RebuiltPublishedSupportDoesNotOverwriteThePreviousPublication()
    {
        var f = new Fixture(leftSupport: false, rightSupport: true);
        var receipt = DemandLaw.Ask(f.Application);
        var otherLeft = DemandLaw.Port(f.Left.Left, Narrow(f.Left, S(f.Yes)));
        var otherApplication = DemandLaw.Create(f.Law, otherLeft, f.Right, f.Routes);
        AssertAnswer(DemandLaw.Ask(otherApplication), f.Go);
        AssertAnswer(receipt, f.Stop);
        AssertCurrentLeftSupport(receipt, f.No);
    }

    [Fact]
    public void AnswerCopiedToABroaderApplicationCannotSuppressItsSupplierDemand()
    {
        var f = new Fixture(leftSupport: false, rightSupport: true);
        var receipt = DemandLaw.Ask(f.Application);
        var broadLeft = DemandLaw.Port(f.Left.Left, Narrow(f.Left, f.Domain));
        var broadApplication = DemandLaw.Create(f.Law, broadLeft, f.Right, f.Routes);
        var copied = new Relation(new Relation(broadApplication, new Relation(broadLeft, f.Right)), receipt.Right);
        Assert.False(DemandLaw.TryReadAnswer(copied, out _));
        var visits = 0;
        var refreshed = DemandLaw.Service(copied, port =>
        {
            visits++;
            Assert.Same(broadLeft, port);
            return Narrow(broadLeft, S(f.Yes));
        });
        Assert.Equal(1, visits);
        AssertAnswer(refreshed, f.Go);
        Assert.True(Contains(refreshed, copied));
        AssertAnswer(receipt, f.Stop);
    }

    [Fact]
    public void AnswerCopiedToAnIncompleteLawIsPendingWithoutQueryingExactPorts()
    {
        var f = new Fixture(leftSupport: false, rightSupport: true);
        var receipt = DemandLaw.Ask(f.Application);
        var incomplete = FiniteLaw.Create(f.Domain, f.Domain, f.Domain);
        var application = DemandLaw.Create(incomplete, f.Left, f.Right, f.Routes);
        var copied = new Relation(new Relation(application, new Relation(f.Left, f.Right)), receipt.Right);
        Assert.False(DemandLaw.TryReadAnswer(copied, out _));
        var refreshed = DemandLaw.Service(copied, _ => throw new InvalidOperationException("A missing law is not missing input detail."));
        Assert.False(DemandLaw.TryReadAnswer(refreshed, out _));
        Assert.True(DemandLaw.TryRead(refreshed, out _, out var answers, out var requests, out _, out _));
        Assert.Empty(E(answers)); Assert.Empty(E(requests));
        Assert.True(StructuralConstraint.TryReadScope(answers, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail);
        Assert.True(Contains(refreshed, copied));
    }

    [Fact]
    public void EditingRetainedAnswerFieldsCannotCertifyAnUnsupportedProspectiveAnswer()
    {
        var f = new Fixture(leftSupport: false, rightSupport: true);
        var genuine = DemandLaw.Ask(f.Application);
        var result = Assert.IsType<Relation>(genuine.Right);
        var forged = new Relation(genuine.Left, new Relation(S(f.Go), result.Right));

        AssertRetainedAnswer(forged, f.Go);
        Assert.False(DemandLaw.TryReadAnswer(forged, out _));
        var repaired = DemandLaw.Service(forged, _ => throw new InvalidOperationException("No finer input is needed."));
        AssertAnswer(repaired, f.Stop);
        Assert.True(Contains(repaired, forged));
    }

    [Fact]
    public void EditingAnswerAndRequestFieldsCannotHideNecessarySupplierDemand()
    {
        var f = new Fixture();
        var genuine = DemandLaw.Ask(f.Application);
        var result = Assert.IsType<Relation>(genuine.Right);
        var remaining = Assert.IsType<Relation>(result.Right);
        var forged = new Relation(genuine.Left, new Relation(S(f.Go), new Relation(S(), remaining.Right)));
        AssertRetainedAnswer(forged, f.Go);
        Assert.False(DemandLaw.TryReadAnswer(forged, out _));

        var visits = 0;
        var repaired = DemandLaw.Service(forged, port =>
        {
            visits++;
            Assert.Same(f.Left, port);
            return Narrow(f.Left, S(f.No));
        });
        Assert.Equal(1, visits);
        AssertAnswer(repaired, f.Stop);
        Assert.True(Contains(repaired, forged));
    }

    [Fact]
    public void GuardUsesTheRefinedCurrentPortsRatherThanTheOriginalAdvertisedSupports()
    {
        var f = new Fixture();
        var initial = DemandLaw.Ask(f.Application);
        var refined = DemandLaw.Service(initial, port =>
        {
            Assert.Same(f.Left, port);
            return Narrow(f.Left, S(f.No));
        });
        AssertAnswer(refined, f.Stop);
        AssertCurrentLeftSupport(initial, f.No, f.Yes);
        AssertCurrentLeftSupport(refined, f.No);
        Assert.Same(refined, DemandLaw.Service(refined, _ => throw new InvalidOperationException("Refined current ports already suffice.")));
    }

    [Theory]
    [InlineData(0)] // substituted port identity
    [InlineData(1)] // substituted publication source
    [InlineData(2)] // substituted publication subject
    [InlineData(3)] // substituted observation event
    [InlineData(4)] // substituted basis
    [InlineData(5)] // substituted domain
    [InlineData(6)] // broader support despite an answer unaffected by broadening
    [InlineData(7)] // incompatible support
    [InlineData(8)] // replacement of a closed empty premise
    public void ProspectiveReadAndServiceRejectUnjustifiedCurrentPortSubstitution(int mismatch)
    {
        var f = new Fixture(leftSupport: false, rightSupport: true);
        var originalLeft = mismatch == 8 ? DemandLaw.Port(f.Left.Left, Narrow(f.Left, S())) : f.Left;
        var routes = mismatch == 6 ? S(new Relation(f.No, f.Stop), new Relation(f.Yes, f.Stop)) : f.Routes;
        var application = DemandLaw.Create(f.Law, originalLeft, f.Right, routes);
        var original = DemandLaw.Ask(application);
        Assert.True(QualitativePublication.TryRead(f.LeftPublication, out var source, out var subject,
            out var eventIdentity, out var basis, out _, out _, out _));
        var support = mismatch == 6 ? f.Domain : mismatch == 7 ? S(f.Yes) : S(f.No);
        var publication = QualitativePublication.Create(mismatch == 1 ? new Placeholder() : source,
            mismatch == 2 ? new Placeholder() : subject,
            mismatch == 3 ? new Placeholder() : eventIdentity,
            mismatch == 4 ? new Placeholder() : basis,
            mismatch == 5 ? S(f.No, f.Yes) : f.Domain, support);
        var substituted = DemandLaw.Port(mismatch == 0 ? new Placeholder() : f.Left.Left, publication);
        var body = Assert.IsType<Relation>(original.Right);
        var rest = Assert.IsType<Relation>(body.Right);
        var claimed = mismatch == 7 ? f.Go : f.Stop;
        var forged = new Relation(new Relation(application, new Relation(substituted, f.Right)),
            new Relation(S(claimed), new Relation(S(), rest.Right)));

        AssertRetainedAnswer(forged, claimed);
        Assert.False(DemandLaw.TryReadAnswer(forged, out _));
        var serviced = DemandLaw.Service(forged, _ => throw new InvalidOperationException("A substituted context cannot demand a service."));
        Assert.False(DemandLaw.TryReadAnswer(serviced, out _));
        Assert.True(DemandLaw.TryRead(serviced, out _, out _, out var requests, out var blocked, out _));
        Assert.Empty(E(requests)); Assert.NotEmpty(E(blocked));
        Assert.True(Contains(serviced, forged));
    }

    private sealed class Fixture
    {
        public readonly IElement No = new Placeholder();
        public readonly IElement Yes = new Placeholder();
        public readonly IElement Stop = new Placeholder();
        public readonly IElement Go = new Placeholder();
        public readonly Relation Domain, Law, Routes, LeftPublication, RightPublication, Left, Right, Application;

        public Fixture(bool? leftSupport = null, bool? rightSupport = null)
        {
            Domain = S(No, Yes);
            Law = FiniteLaw.Create(Domain, Domain, Domain,
                FiniteLaw.Row(No, No, S(No)), FiniteLaw.Row(No, Yes, S(No)),
                FiniteLaw.Row(Yes, No, S(No)), FiniteLaw.Row(Yes, Yes, S(Yes)));
            Routes = S(new Relation(No, Stop), new Relation(Yes, Go));
            LeftPublication = Publication(leftSupport); RightPublication = Publication(rightSupport);
            Left = DemandLaw.Port(new Placeholder(), LeftPublication);
            Right = DemandLaw.Port(new Placeholder(), RightPublication);
            Application = DemandLaw.Create(Law, Left, Right, Routes);
        }

        private Relation Publication(bool? value) => QualitativePublication.Create(new Placeholder(), new Placeholder(),
            new Placeholder(), new Placeholder(), Domain, value is null ? Domain : S(value.Value ? Yes : No));
    }

    private static Relation Narrow(Relation port, Relation support)
    {
        Assert.True(DemandLaw.TryReadPort(port, out _, out var publication));
        Assert.True(QualitativePublication.TryRead(publication, out var source, out var subject, out var eventIdentity,
            out var basis, out var domain, out _, out _));
        return QualitativePublication.Create(source, subject, eventIdentity, basis, domain, support);
    }

    private static void AssertCurrentLeftSupport(Relation receipt, params IElement[] expected)
    {
        var header = Assert.IsType<Relation>(receipt.Left);
        var ports = Assert.IsType<Relation>(header.Right);
        var current = Assert.IsType<Relation>(ports.Left);
        Assert.True(DemandLaw.TryReadPort(current, out _, out var publication));
        Assert.True(QualitativePublication.TryRead(publication, out _, out _, out _, out _, out _, out var support, out _));
        Assert.Equal(expected, E(support));
    }

    private static void AssertAnswer(Relation receipt, IElement expected)
    {
        Assert.True(DemandLaw.TryReadAnswer(receipt, out var answer));
        Assert.Same(expected, answer);
    }

    private static void AssertRetainedAnswer(Relation receipt, IElement expected)
    {
        Assert.True(DemandLaw.TryRead(receipt, out _, out var answers, out _, out _, out _));
        Assert.Same(expected, Assert.Single(E(answers)));
    }

    private static Relation S(params IElement[] entries) => StructuralConstraint.Closed(entries);
    private static IElement[] E(Relation scope) => FiniteLaw.Entries(scope);

    private static bool Contains(IElement root, IElement target)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.Count != 0)
        {
            var node = pending.Pop();
            if (ReferenceEquals(node, target)) return true;
            if (!seen.Add(node) || node is not Relation relation) continue;
            pending.Push(relation.Left); pending.Push(relation.Right);
        }
        return false;
    }

    private static void AssertNoCarriers(IElement root)
    {
        var seen = new HashSet<IElement>(ReferenceEqualityComparer.Instance);
        var pending = new Stack<IElement>(); pending.Push(root);
        while (pending.Count != 0)
        {
            var node = pending.Pop();
            if (!seen.Add(node)) continue;
            Assert.IsNotType<Carrier>(node); Assert.IsNotType<CarrierLocation>(node);
            if (node is Relation relation) { pending.Push(relation.Left); pending.Push(relation.Right); }
        }
    }
}

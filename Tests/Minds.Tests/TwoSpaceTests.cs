using System.Numerics;
using Gojo.Core;
using Gojo.Encoding.Syntax;
using Gojo.Structures;
using Minds.TwoSpace;

namespace Minds.Tests;

public sealed class TwoSpaceTests
{
    [Fact]
    public void PairAverageCanStayFixedWhileThePartsKeepMoving()
    {
        var run = Run("full");
        Scalar(run.Rounds[1].Observation, 0, 9);
        Scalar(run.Rounds[1].Observation, 1, 5);
        foreach (var round in run.Rounds.Skip(1))
        {
            Scalar(round.Observation, 2, 7); // Average.
            Scalar(round.Observation, 6, 8); // Sum of absolute local movements.
            Scalar(round.Observation, 7, 0); // Average change.
        }
        Assert.True(TimeStepSequence.TryRead(run.Timeline, out _, out _, out var steps));
        Assert.Equal(run.Events.Count - 1, steps.Length);
        Assert.True(TimeStepSequence.TryReadStep(steps[^1], out _, out _, out _, out _, out _, out _, out var residue));
        // Movement is measured activity, not automatically unsettled material.
        Number(Fields(Fields(residue)[0])[0], 0); Number(Fields(Fields(residue)[1])[0], 0);
        Number(Fields(residue)[2], 0); Number(Fields(residue)[3], 0);
    }

    [Fact]
    public void AgreementDoesNotRemoveMismatchToTheLocalReference()
    {
        var run = Run("half");
        Scalar(run.Rounds[1].Observation, 0, 7); Scalar(run.Rounds[1].Observation, 1, 7);
        Scalar(run.Rounds[1].Observation, 3, 0);
        Scalar(run.Rounds[1].Observation, 8, -5); Scalar(run.Rounds[1].Observation, 9, -5);
        Scalar(run.Rounds[2].Observation, 6, 0);
        // The observation window continues after agreement; zero changes are still recorded.
        Assert.Equal(6, run.Events[^1].AAttempts);
    }

    [Fact]
    public void MessageScheduleChangesOutcomeWithoutChangingResponseStrength()
    {
        var captured = Run("full"); var fresh = Run("fresh");
        Scalar(captured.Rounds[1].Observation, 1, 5);
        Scalar(fresh.Rounds[1].Observation, 0, 9); Scalar(fresh.Rounds[1].Observation, 1, 9);
        Scalar(fresh.Rounds[1].Observation, 7, 2);
        var afterA = captured.Events.Single(e => e.Description == "A applies its response" && e.AAttempts == 1);
        Number(Fields(Fields(afterA.PublishedA!)[0])[2], 5);
        Scalar(afterA.Observation, 0, 9); // Current view differs from the message actually sent.
    }

    [Fact]
    public void SmallResponsesKeepFractionalRequestsWithoutInventingCarrierTension()
    {
        var run = Run("quarter");
        var last = run.Events[^1];
        var plan = Fields(last.Evidence)[1];
        var remainder = Fields(Fields(plan)[3])[0];
        Assert.True(ProjectionResidue.TryRead(remainder, out _, out var n, out var d));
        Assert.NotEqual(0, n); Assert.True(d > 1);
        Number(Fields(last.Evidence)[3], 0);
        Scalar(run.Rounds[^1].Observation, 3, 1, 4);
        Scalar(run.Rounds[^1].Observation, 6, 1, 20); // Observation ends while some movement remains.
    }

    [Fact]
    public void LaterReferenceResponseKeepsEarlierObservationsReadable()
    {
        var run = Run("reference");
        Scalar(run.Rounds[2].Observation, 0, 7); Scalar(run.Rounds[2].Observation, 8, -5);
        Scalar(run.Rounds[3].Observation, 0, 2); Scalar(run.Rounds[3].Observation, 1, 2);
        Scalar(run.Rounds[3].Observation, 8, 0);
        Scalar(run.Rounds[0].Observation, 0, 5); Scalar(run.Rounds[0].Observation, 1, 9);
    }

    [Fact]
    public void ObservingDoesNotActPublishOrRewriteParticipantHistory()
    {
        var roots = GojoSyntax.Parse(FeedbackExperiment.FixtureText()).Roots;
        var a = new Space(roots[0], roots[2], roots[4], roots[5]);
        var b = new Space(roots[1], roots[3], roots[4], roots[5]);
        var aState = a.State; var bState = b.State; var ah = a.History; var bh = b.History;
        var ai = a.Inbox; var bi = b.Inbox;
        var observer = new PairObserver(a.Output);
        var before = observer.Observe(a, b);
        for (var i = 0; i < 5; i++) observer.Observe(a, b, before);
        Assert.Same(aState, a.State); Assert.Same(bState, b.State);
        Assert.Same(ah, a.History); Assert.Same(bh, b.History);
        Assert.Same(ai, a.Inbox); Assert.Same(bi, b.Inbox);
        Assert.Null(a.LastPublishedMessage); Assert.Null(b.LastPublishedMessage);
        Assert.Equal(50UL, a.Actuator.Value); Assert.Equal(180UL, b.Actuator.Value);
        Assert.Equal(0, a.Actuator.Tension); Assert.Equal(0, b.Actuator.Tension);
        var am = a.Publish(1); var bm = b.Publish(101);
        var ar = a.Receive(bm, 2); var br = b.Receive(am, 102);
        var ap = a.Plan(ar, 2); var bp = b.Plan(br, 2);
        Assert.True(a.Apply(ap, ar)); Assert.True(b.Apply(bp, br));
        Scalar(observer.Observe(a, b), 0, 7); Scalar(observer.Observe(a, b), 1, 7);
        Scalar(before, 0, 5); Scalar(before, 1, 9);
        // A duplicate response is refused with retained local state, not silently applied again.
        var after = a.State;
        Assert.False(a.Apply(ap, ar)); Assert.Same(after, a.State);
    }

    private static FeedbackRun Run(string id) => FeedbackExperiment.Run(FeedbackExperiment.Cases.Single(c => c.Id == id));
    private static IElement[] Fields(IElement element)
    {
        Assert.True(Assert.IsType<Relation>(element).TryReadOrderedGroup(out var fields));
        return fields;
    }
    private static void Scalar(Relation observation, int index, long n, long d = 1) =>
        Number(PairObserver.Scalar(Assert.IsType<Relation>(Fields(observation)[index])), n, d);
    private static void Number(IElement element, long n, long d = 1)
    {
        Assert.True(ProjectionResidue.TryRead(element, out _, out var actualN, out var actualD));
        Assert.Equal((BigInteger)n * actualD, (BigInteger)actualN * d);
    }
}

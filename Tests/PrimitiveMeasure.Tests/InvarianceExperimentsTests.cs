using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class InvarianceExperimentsTests
{
    [Fact]
    public void ExplicitExpectedReadExplainsSameLookingCodesThatFailEquivariance()
    {
        var priority = InvarianceExperiments.Run(7, 1);
        Assert.Equal((byte)(1 << 6), priority.BeforeSupport);
        Assert.Equal((byte)(1 << 6), priority.AfterSupport);
        Assert.Equal((byte)(1 << 5), priority.ExpectedAfterSupport);
        Assert.Equal((byte)1, priority.VerdictSupport);
        var ambiguous = InvarianceExperiments.Run(8, 2);
        Assert.Equal(ambiguous.AfterSupport, ambiguous.ExpectedAfterSupport);
        Assert.Equal((byte)3, ambiguous.VerdictSupport);
    }

    [Theory]
    [InlineData(0, 1)] // The replacement loses shared identity.
    [InlineData(1, 1)] // One independent dial changes without the other.
    [InlineData(2, 1)] // The order is actually reversed.
    [InlineData(3, 1)] // The final arrival is actually detached.
    [InlineData(4, 1)] // One reversed edge breaks this ordered walk.
    [InlineData(5, 1)] // A straight passage does not implement a choice.
    [InlineData(6, 1)] // Removing the last participant changes presence.
    [InlineData(7, 1)] // Serial priority defeats exchanged role correspondence.
    [InlineData(8, 1)] // Direction reversal is not frame reorientation.
    [InlineData(9, 3)] // Missing equality evidence does not prove nonrectangularity.
    [InlineData(10, 1)] // These reordered spans actually become disjoint.
    [InlineData(11, 1)] // One arrival leaves the common joint.
    public void PreservingAndChallengingTrialsPublishTheAppropriateKindOfEvidence(int specimen, byte challenge)
    {
        Assert.Equal((byte)2, InvarianceExperiments.Run(specimen, 0).VerdictSupport);
        Assert.Equal(challenge, InvarianceExperiments.Run(specimen, 1).VerdictSupport);
    }

    [Fact]
    public void EveryPublishedComparisonIsReconstructibleFromItsActualPackedLawAndReferences()
    {
        for (var specimen = 0; specimen < InvarianceExperiments.Cases.Count; specimen++)
        for (var variation = 0; variation < 3; variation++)
        {
            var result = InvarianceExperiments.Run(specimen, variation);
            var wiring = ReadRow(result.RowBytes, result.ReferenceBits, 3);
            Assert.Equal(result.Wiring, wiring);
            Assert.Equal(64, result.LawBytes.Length);
            Assert.Equal(0, wiring[0] & 1); // Imported observations.
            Assert.Equal(0, wiring[1] & 1);
            Assert.Equal(1, wiring[2] & 1); // Calculated comparison.

            // Read the advertised tuple bits independently of BitRelation and
            // of the authored case name or explanation.
            byte expected = 0;
            for (var before = 0; before < 8; before++)
            for (var after = 0; after < 8; after++)
            {
                if ((result.BeforeSupport & (1 << before)) == 0 ||
                    (result.AfterSupport & (1 << after)) == 0 ||
                    (wiring[0] == wiring[1] && before != after)) continue;
                for (var answer = 0; answer < 8; answer++)
                {
                    var bit = before + 8 * after + 64 * answer;
                    if ((result.LawBytes[bit / 8] & (1 << (bit % 8))) != 0)
                        expected |= (byte)(1 << answer);
                }
            }
            Assert.Equal(expected, result.VerdictSupport);
            Assert.Equal(expected, result.Port.ReadSupport());
            Assert.Equal(0, expected & ~3);
        }
    }

    [Fact]
    public void HidingTheRelevantObservationCannotCreateAConfirmedCorrespondence()
    {
        for (var specimen = 0; specimen < InvarianceExperiments.Cases.Count; specimen++)
        {
            var result = InvarianceExperiments.Run(specimen, 2);
            Assert.NotEqual((byte)2, result.VerdictSupport);
            Assert.True(result.VerdictSupport is 0 or 3,
                $"Hidden case {specimen} asserted a concrete disagreement instead of retaining uncertainty.");
        }
    }

    [Fact]
    public void FreshApplicationsDoNotRewriteEarlierComparisonEvidence()
    {
        for (var specimen = 0; specimen < InvarianceExperiments.Cases.Count; specimen++)
        {
            var first = InvarianceExperiments.Run(specimen, 0);
            var before = first.BeforeBytes.ToArray();
            var after = first.AfterBytes.ToArray();
            var verdict = first.Port.ReadSupport();

            _ = InvarianceExperiments.Run(specimen, 1);
            _ = InvarianceExperiments.Run(specimen, 2);
            var another = InvarianceExperiments.Run(specimen, 0);

            Assert.Equal(before, first.BeforeBytes);
            Assert.Equal(after, first.AfterBytes);
            Assert.Equal(verdict, first.Port.ReadSupport());
            Assert.Equal(first.BeforeSupport, another.BeforeSupport);
            Assert.Equal(first.AfterSupport, another.AfterSupport);
            Assert.Equal(first.VerdictSupport, another.VerdictSupport);
            Assert.Equal(first.LawBytes, another.LawBytes);
            Assert.Equal(first.RowBytes, another.RowBytes);
            Assert.Equal(before, another.BeforeBytes);
            Assert.Equal(after, another.AfterBytes);
        }
    }

    [Fact]
    public void ASeparateConsumerCanWaitForComparisonEvidenceThenMoveTheSameActivation()
    {
        var unresolved = InvarianceExperiments.Run(3, 2);
        var rejected = InvarianceExperiments.Run(3, 1);
        var established = InvarianceExperiments.Run(3, 0);
        Assert.Equal((byte)3, unresolved.Port.ReadSupport());
        Assert.Equal((byte)1, rejected.Port.ReadSupport());
        Assert.Equal((byte)2, established.Port.ReadSupport());

        var consumer = new PulseCircuit(2);
        var source = consumer.State(true);
        var destination = consumer.State();
        var guard = consumer.Guard();
        consumer.Link(consumer.AddTable(PassageLaw.Controlled()), source, guard, destination);
        consumer.Bind(guard, unresolved.Port);
        var waitingBytes = consumer.ValueBytes();
        Assert.False(consumer.Pulse().Committed);
        Assert.Equal(waitingBytes, consumer.ValueBytes());

        consumer.Bind(guard, rejected.Port);
        var blocked = consumer.Pulse();
        Assert.True(blocked.Committed);
        Assert.False(blocked.Moved);
        Assert.Equal(source, consumer.Active);

        consumer.Bind(guard, established.Port);
        Assert.True(consumer.Pulse().Moved);
        Assert.Equal(destination, consumer.Active);
        Assert.Equal((byte)1, consumer.Expose(source).ReadSupport());
        Assert.Equal((byte)2, consumer.Expose(destination).ReadSupport());
        Assert.False(consumer.Pulse().Moved);
        Assert.Equal(destination, consumer.Active);
    }

    [Fact]
    public void AChangedGeometricGuaranteeAndAForeignSpanBasisAreNotTheSameKindOfFailure()
    {
        var detachedExtents = InvarianceExperiments.Run(9, 1);
        Assert.Equal((byte)2, detachedExtents.BeforeSupport);
        Assert.Equal((byte)3, detachedExtents.AfterSupport);
        Assert.Equal((byte)3, detachedExtents.Port.ReadSupport());
        Assert.Equal(detachedExtents.BeforeLinks, detachedExtents.AfterLinks);
        Assert.NotEqual(detachedExtents.BeforeBytes, detachedExtents.AfterBytes);

        var foreignRail = InvarianceExperiments.Run(10, 2);
        Assert.Equal((byte)2, foreignRail.BeforeSupport);
        Assert.Equal((byte)0, foreignRail.AfterSupport);
        Assert.Equal((byte)0, foreignRail.Port.ReadSupport());
        var separatedSpans = InvarianceExperiments.Run(10, 1);
        Assert.Equal((byte)1, separatedSpans.AfterSupport);
        Assert.Equal((byte)1, separatedSpans.Port.ReadSupport());
    }

    [Fact]
    public void AWholeReverseReadPreservesClosureWhileChangingEveryOrderedEndpoint()
    {
        var result = InvarianceExperiments.Run(4, 0);
        Assert.Equal((byte)2, result.VerdictSupport);
        Assert.Equal(4, result.BeforeLinks.Length);
        for (var i = 0; i < result.BeforeLinks.Length; i++)
        {
            var original = result.BeforeLinks[i];
            var reversed = result.AfterLinks[result.AfterLinks.Length - i - 1];
            Assert.Equal(new[] { original[1], original[0] }, reversed);
        }
        var partialReverse = InvarianceExperiments.Run(4, 1);
        Assert.Equal((byte)1, partialReverse.AfterSupport);
        Assert.Equal((byte)1, partialReverse.VerdictSupport);
    }

    [Fact]
    public void SwitchCorrespondenceCanSucceedWhenTheComparedWordsAreDifferent()
    {
        var mapped = InvarianceExperiments.Run(5, 0);
        Assert.NotEqual(mapped.BeforeSupport, mapped.AfterSupport);
        Assert.Equal((byte)2, mapped.Port.ReadSupport());
        Assert.Equal(1, System.Numerics.BitOperations.PopCount((uint)(mapped.BeforeBytes[0] & 7)));
        Assert.Equal(1, System.Numerics.BitOperations.PopCount((uint)(mapped.AfterBytes[0] & 7)));
        var straight = InvarianceExperiments.Run(5, 1);
        Assert.Equal((byte)1, straight.Port.ReadSupport());
        var concealed = InvarianceExperiments.Run(5, 2);
        Assert.Equal((byte)3, concealed.Port.ReadSupport());
        Assert.Equal(1, concealed.AfterBytes[0] & 7); // Actual T source retained.
    }

    [Fact]
    public void YAndTChoiceLawsCorrespondAcrossBothControlValuesAndOccupiedReceivers()
    {
        static byte Map(byte state) => (byte)((state & 8) | ((state << 1) & 7) | ((state >> 2) & 1));
        static uint MapSupport(uint support)
        {
            uint mapped = 0;
            for (byte state = 0; state < 16; state++)
                if ((support & (1u << state)) != 0) mapped |= 1u << Map(state);
            return mapped;
        }

        for (byte state = 0; state < 16; state++)
        {
            var y = GlyphJunction.YBranch(state);
            var t = GlyphJunction.TBar(Map(state), optionalTurn: true);
            Assert.Equal(MapSupport(y.Flow.Predict(1u << state)), t.Flow.Predict(1u << Map(state)));
            var yAfter = y.Advance();
            var tAfter = t.Advance();
            Assert.Equal(Map(yAfter), tAfter);
            Assert.Equal(System.Numerics.BitOperations.PopCount((uint)(state & 7)),
                System.Numerics.BitOperations.PopCount((uint)(yAfter & 7)));
            Assert.Equal(state & 8, yAfter & 8);
            Assert.Equal(state & 8, tAfter & 8);
        }

        // Whole-state support preserves the shared unknown control across
        // both guarded rows; independent outlet marginals would lose this.
        var uncertainY = GlyphJunction.YBranch(4);
        var uncertainT = GlyphJunction.TBar(1, optionalTurn: true);
        var beforeSupport = (1u << 4) | (1u << 12);
        Assert.Equal(MapSupport(uncertainY.Flow.Predict(beforeSupport)),
            uncertainT.Flow.Predict(MapSupport(beforeSupport)));
        Assert.False(uncertainY.Flow.TryStep(beforeSupport, out _));
        Assert.Equal((byte)4, uncertainY.Flow.Stored);
    }

    [Fact]
    public void CollectionAndPriorityExposeDifferentLimitsOfInputExchange()
    {
        var collected = InvarianceExperiments.Run(6, 0);
        Assert.NotEqual(collected.BeforeBytes[0], collected.AfterBytes[0]);
        Assert.Equal((byte)2, collected.BeforeSupport);
        Assert.Equal((byte)2, collected.AfterSupport);
        Assert.Equal((byte)2, collected.VerdictSupport);

        var priority = InvarianceExperiments.Run(7, 1);
        Assert.Equal(priority.BeforeSupport, priority.AfterSupport);
        Assert.Equal((byte)(1 << 6), priority.BeforeSupport);
        Assert.Equal((byte)1, priority.VerdictSupport);
        // Both outlets remain occupied, but the selected exchange law also
        // asks which contributor was left behind by serial acceptance.
        Assert.Equal(4, priority.BeforeBytes[0] & 4);
        Assert.Equal(4, priority.AfterBytes[0] & 4);
        Assert.Equal(2, System.Numerics.BitOperations.PopCount((uint)(priority.AfterBytes[0] & 7)));
    }

    [Fact]
    public void ExactCorrespondenceOfPossibleAdditionResultsDoesNotSelectAnActualOutcome()
    {
        var result = InvarianceExperiments.Run(8, 2);
        byte transformed = 0;
        foreach (var word in ReadSupport.Words(result.BeforeSupport))
            transformed |= ReadSupport.Of(ThreeBitMeasure.Reorient(word));
        Assert.Equal(transformed, result.AfterSupport);
        Assert.False(ReadSupport.TrySingle(result.BeforeSupport, out _));
        Assert.False(ReadSupport.TrySingle(result.AfterSupport, out _));
        Assert.Equal((byte)3, result.VerdictSupport);
    }

    [Fact]
    public void SharedJointAndLoopReadsSurviveOnlyTheChangesTheyActuallyObserve()
    {
        var joint = InvarianceExperiments.Run(11, 0);
        Assert.All(joint.BeforeLinks, row => Assert.Equal(3, row[1]));
        Assert.All(joint.AfterLinks, row => Assert.Equal(4, row[1]));
        Assert.Equal((byte)2, joint.VerdictSupport);
        var split = InvarianceExperiments.Run(11, 1);
        Assert.Equal(2, split.AfterLinks.Select(row => row[1]).Distinct().Count());
        Assert.Equal((byte)1, split.VerdictSupport);

        var loop = InvarianceExperiments.Run(3, 0);
        Assert.NotEqual(loop.BeforeBytes, loop.AfterBytes);
        Assert.Equal(loop.BeforeLinks, loop.AfterLinks);
        Assert.Equal((byte)2, loop.VerdictSupport);
    }

    [Fact]
    public void AComparisonOfTwoFalseClaimsDoesNotMakeEitherClaimPositive()
    {
        var source = StructuralRoute.Pairs(4, [[0, 1], [1, 2]]);
        source.Inspect(int.MaxValue);
        var before = CapturedRead.Capture(source.Closure);
        source.SetWord(0, 0);
        var after = CapturedRead.Capture(source.Closure);
        var probe = new CorrespondenceProbe(InvarianceExperiments.Equality, before, after);
        Assert.Equal((byte)1, before.ReadSupport());
        Assert.Equal((byte)1, after.ReadSupport());
        Assert.Equal((byte)2, probe.ReadSupport());
        Assert.Equal((byte)1, source.Closure.ReadSupport());
    }

    private static int[] ReadRow(byte[] packed, int bits, int arity)
    {
        var result = new int[arity];
        for (var field = 0; field < arity; field++)
        for (var bit = 0; bit < bits; bit++)
        {
            var offset = field * bits + bit;
            if ((packed[offset / 8] & (1 << (offset % 8))) != 0)
                result[field] |= 1 << bit;
        }
        return result;
    }
}

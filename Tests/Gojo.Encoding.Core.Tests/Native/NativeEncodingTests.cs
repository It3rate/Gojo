using Gojo.Core;
using Gojo.Patterns;

namespace Gojo.Encoding.Core.Native.Tests;

public sealed class NativeEncodingTests
{
    [Fact]
    public void LawAndSharedParticipantsRoundTripWithoutNamesOrOriginalWrappers()
    {
        var a = new Placeholder(); var b = new Placeholder();
        var domain = S(a, b);
        var law = FiniteLaw.Create(domain, domain, domain,
            FiniteLaw.Row(a, a, S(a)), FiniteLaw.Row(a, b, S(b)),
            FiniteLaw.Row(b, a, S(b)), FiniteLaw.Row(b, b, S(a)));
        var restored = GojoNumericFormat.Decode(GojoNumericFormat.Encode(law, S(a, b), S(b)));
        var restoredLaw = Assert.IsType<Relation>(restored[0]);
        var left = Assert.IsType<Relation>(restored[1]);
        var right = Assert.IsType<Relation>(restored[2]);
        Assert.True(FiniteLaw.TryRead(restoredLaw, out var leftDomain, out var rightDomain, out var outputDomain, out _));
        Assert.Same(leftDomain, rightDomain); Assert.Same(leftDomain, outputDomain);
        var participants = FiniteLaw.Entries(leftDomain);
        Assert.NotSame(a, participants[0]);
        Assert.Same(participants[1], Assert.Single(FiniteLaw.Entries(right)));
        Assert.True(FiniteLaw.TryReadResult(FiniteLaw.Evaluate(restoredLaw, left, right),
            out var support, out var unresolved, out var rows));
        Assert.Equal(2, FiniteLaw.Entries(support).Length);
        Assert.Empty(FiniteLaw.Entries(unresolved)); Assert.Equal(2, FiniteLaw.Entries(rows).Length);
    }

    [Fact]
    public void CurveFrameAndEndpointCorrespondenceSurviveNumericRoundTrip()
    {
        var frame = new QualitativeCurve.Frame(new QualitativeMotionFrame());
        var sharedSite = new Placeholder();
        var section = frame.Quadratic(sharedSite, sharedSite, frame.Word(0, 0), frame.Word(2, 3), frame.Word(0, 0));
        var restored = GojoNumericFormat.Decode(GojoNumericFormat.Encode(frame.Surface, section));
        Assert.True(QualitativeCurve.Frame.TryFromSurface(Assert.IsType<Relation>(restored[0]), out var imported));
        var read = imported.Read(Assert.IsType<Relation>(restored[1]));
        Assert.True(read.Complete); Assert.Same(read.Start, read.End);
        Assert.NotSame(sharedSite, read.Start);
        Assert.Equal(frame.Read(section).Controls, read.Controls);
    }

    [Fact]
    public void CapturedPlansRefuseStaleSourceAndPreserveBatchOutputOnRefusal()
    {
        // A reachable tick exercises dependency invalidation. It is not a
        // measurement, truth code or a required part of an ordinary finite law.
        var tick = new TickRef(0); var input = new Relation(tick, new Placeholder());
        var partner = new Placeholder(); var result = new Placeholder();
        var law = FiniteLaw.Create(S(input), S(partner), S(result), FiniteLaw.Row(input, partner, S(result)));
        var scalar = Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(law));
        var batch = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(law));
        ulong[] output = [99];
        Assert.True(batch.TryEvaluateBatch([new FiniteLawMaskPair(1, 1)], output)); Assert.Equal(1UL, output[0]);
        tick.Move(1);
        Assert.False(scalar.IsCurrent); Assert.False(batch.IsCurrent);
        Assert.False(scalar.TryEvaluateMasks(1, 1, out _));
        output[0] = 99;
        Assert.False(batch.TryEvaluateBatch([new FiniteLawMaskPair(1, 1)], output)); Assert.Equal(99UL, output[0]);
        Assert.True(FiniteLaw.TryReadResult(scalar.Evaluate(S(input), S(partner)), out var support, out var residue, out _));
        Assert.True(StructuralConstraint.TryReadScope(support, out _, out var seam, out var tail));
        Assert.NotEqual(seam, tail); Assert.NotEmpty(FiniteLaw.Entries(residue));
        Assert.True(Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(law)).TryEvaluateMasks(1, 1, out _));
    }

    [Fact]
    public void NativeOpenSupportIsNeverCompiledAsTheEmptyMask()
    {
        var a = new Placeholder(); var b = new Placeholder();
        var law = FiniteLaw.Create(S(a), S(b), S(a), FiniteLaw.Row(a, b, S(a)));
        var scalar = Assert.IsType<FiniteLawPlan>(FiniteLawPlan.Capture(law));
        var batch = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(law));
        Assert.True(scalar.TryEncodeLeft(S(), out var empty)); Assert.Equal((byte)0, empty);
        Assert.True(batch.TryEncodeLeft(S(), out var emptyBatch)); Assert.Equal(0UL, emptyBatch);
        Assert.False(scalar.TryEncodeLeft(StructuralConstraint.Open(), out _));
        Assert.False(batch.TryEncodeLeft(StructuralConstraint.Open(), out _));
        Assert.False(scalar.TryEncodeLeft(S(new Placeholder()), out _));
        Assert.False(batch.TryEncodeLeft(S(new Placeholder()), out _));
        ulong[] output = [91];
        Assert.False(batch.TryEvaluateBatch([new FiniteLawMaskPair(2, 1)], output)); Assert.Equal(91UL, output[0]);
    }

    [Fact]
    public void BackendWidthIsNotANativeLawLimit()
    {
        var states = Enumerable.Range(0, 65).Select(_ => (IElement)new Placeholder()).ToArray();
        var partner = new Placeholder(); var domain = S(states);
        var law = FiniteLaw.Create(domain, S(partner), domain,
            states.Select(state => FiniteLaw.Row(state, partner, S(state))).ToArray());
        Assert.Null(FiniteLawPlan.Capture(law)); Assert.Null(FiniteLawBatchPlan.Capture(law));
        Assert.True(FiniteLaw.TryReadResult(FiniteLaw.Evaluate(law, S(states[^1]), S(partner)),
            out var support, out var unresolved, out _));
        Assert.Same(states[^1], Assert.Single(FiniteLaw.Entries(support)));
        Assert.Empty(FiniteLaw.Entries(unresolved));
    }

    [Fact]
    public void BatchLastBitAndInvalidInputKeepTheirOwnMeaning()
    {
        var states = Enumerable.Range(0, 64).Select(_ => (IElement)new Placeholder()).ToArray();
        var partner = new Placeholder(); var domain = S(states);
        var law = FiniteLaw.Create(domain, S(partner), domain,
            states.Select(state => FiniteLaw.Row(state, partner, S(state))).ToArray());
        var batch = Assert.IsType<FiniteLawBatchPlan>(FiniteLawBatchPlan.Capture(law));
        Assert.True(batch.TryEncodeLeft(S(states[^1]), out var last)); Assert.Equal(1UL << 63, last);
        ulong[] output = [91, 92];
        Assert.True(batch.TryEvaluateBatch([new FiniteLawMaskPair(last, 1), new FiniteLawMaskPair(ulong.MaxValue, 1)], output));
        Assert.Equal(last, output[0]); Assert.Equal(ulong.MaxValue, output[1]);
        output = [91, 92];
        Assert.False(batch.TryEvaluateBatch([new FiniteLawMaskPair(last, 1), new FiniteLawMaskPair(1, 2)], output));
        Assert.Equal(new ulong[] { 91, 92 }, output);
    }

    private static Relation S(params IElement[] values) => StructuralConstraint.Closed(values);
}

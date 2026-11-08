using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Experiments.QualitativeExecution;
using Gojo.Experiments.QualitativeFrieze.Composition;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;

namespace QualitativeExecution.Tests;

public sealed class CoupledClaimStudyTests
{
    [Theory]
    [InlineData("cardinal", 0)] [InlineData("cardinal", 1)]
    [InlineData("chevron", 0)] [InlineData("chevron", 1)]
    [InlineData("inset", 0)] [InlineData("inset", 1)]
    public void Two_local_controls_settle_representable_joint_claims(string family, int seed)
    {
        foreach (var mode in new[] { "together", "alternating" })
        {
            var run = CoupledClaimStudy.Execute(family, seed: seed, mode: mode);
            Assert.True(run.Settled); var end = run.Frames[^1];
            Assert.Equal(end.GapGoal, end.Gap); Assert.True(end.EndCoincident);
            Assert.Equal(0, end.WidthTension); Assert.Equal(0, end.HeightTension);
            Assert.All(run.Frames.TakeLast(4), f => Assert.Equal(end.Width, f.Width));
        }
    }

    [Fact]
    public void One_claim_can_succeed_without_the_other()
    {
        var gapOnly = CoupledClaimStudy.Execute(mode: "gap-only");
        var endpointOnly = CoupledClaimStudy.Execute(mode: "endpoint-only");
        Assert.Equal(gapOnly.Frames[^1].GapGoal, gapOnly.Frames[^1].Gap);
        Assert.False(gapOnly.Frames[^1].EndCoincident); Assert.False(gapOnly.Settled);
        Assert.True(endpointOnly.Frames[^1].EndCoincident);
        Assert.NotEqual(endpointOnly.Frames[^1].GapGoal, endpointOnly.Frames[^1].Gap);
        Assert.False(endpointOnly.Settled);
    }

    [Fact]
    public void Each_actuator_accounts_for_its_own_new_tension()
    {
        foreach (var scenario in new[] { "reachable", "height-capacity", "width-capacity", "incompatible" })
        {
            var run = CoupledClaimStudy.Execute(scenario: scenario);
            for (var i = 1; i < run.Frames.Length; i++)
            {
                var before = run.Frames[i - 1]; var after = run.Frames[i];
                Assert.Equal(after.WidthRequest ?? 0,
                    after.Width - before.Width + after.WidthTension - before.WidthTension);
                Assert.Equal(after.HeightRequest ?? 0,
                    after.Height - before.Height + after.HeightTension - before.HeightTension);
            }
        }
    }

    [Theory]
    [InlineData("height-capacity")] [InlineData("width-capacity")]
    public void Capacity_does_not_turn_a_held_request_into_satisfied_geometry(string scenario)
    {
        var run = CoupledClaimStudy.Execute(scenario: scenario);
        Assert.False(run.Settled); var end = run.Frames[^1];
        Assert.Equal(1, scenario == "height-capacity" ? end.HeightTension : end.WidthTension);
        Assert.Contains(run.Frames, f => scenario == "height-capacity" ? f.HeightSuppressed : f.WidthSuppressed);
    }

    [Theory]
    [InlineData("missing-gap")] [InlineData("missing-endpoint")] [InlineData("ambiguous-endpoint")]
    public void Unavailable_or_ambiguous_evidence_does_not_invent_a_move(string scenario)
    {
        var run = CoupledClaimStudy.Execute(scenario: scenario);
        Assert.All(run.Frames.Where(f => f.Beat is >= 1 and <= 8), f =>
        {
            Assert.Null(scenario == "missing-gap" ? f.HeightRequest : f.WidthRequest);
            Assert.Equal(scenario == "ambiguous-endpoint" ? 7 : 0,
                scenario == "missing-gap" ? f.GapInputMask : f.EndpointInputMask);
        });
        Assert.True(run.Settled);
    }

    [Fact]
    public void Resolution_and_geometry_failure_remain_visible()
    {
        var offGrid = CoupledClaimStudy.Execute("inset", "off-grid");
        Assert.False(offGrid.Settled); Assert.DoesNotContain(offGrid.Frames, f => f.Gap == 10);
        Assert.True(offGrid.Frames.TakeLast(8).Select(f => f.Height).Distinct().Count() > 1);
        var impossible = CoupledClaimStudy.Execute(scenario: "incompatible");
        Assert.False(impossible.Settled);
        Assert.Contains(impossible.Frames, f => f.Gap is null || f.WidthTension != 0);
    }

    [Theory]
    [InlineData("chevron", "reachable")] [InlineData("inset", "off-grid")]
    public void Compiled_and_native_laws_produce_the_same_complete_trace(string family, string scenario)
    {
        var native = CoupledClaimStudy.Execute(family, scenario, compiled: false);
        var compiled = CoupledClaimStudy.Execute(family, scenario);
        Assert.Equal(JsonSerializer.Serialize(native.Frames), JsonSerializer.Serialize(compiled.Frames));
    }

    [Fact]
    public void Both_claims_and_controls_resume_from_one_decoded_native_graph()
    {
        var template = CoupledShapeTemplate.ChevronCap();
        var height = CoupledExtentMachine.Create(1, 10); var width = CoupledExtentMachine.Create(8, 12, unit: height.StepUnit.UnitShape);
        var desired = new Carrier(height.StepUnit.UnitShape); desired.Add(6);
        var gap = GeometricGapClaim.Create(template.Frame, new Relation(desired.ValueSlot, desired), height,
            template.Frame.Motion.Down, template.Frame.Word(0, -1));
        var endpoint = GeometricEndpointClaim.Create(template.Frame, width, template.Frame.Motion.Right, template.Frame.Word(1, 0));
        var hg = PendingAdjustmentGate.Create(height); var wg = PendingAdjustmentGate.Create(width);
        var picture = CoupledShapeTemplate.Realize(template, 8, 8, 2, 1);
        var upper = (Relation)FiniteLaw.Entries(picture.Upper.Sections)[1];
        var lower = (Relation)FiniteLaw.Entries(picture.Lower.Sections)[1];
        var roots = GojoNumericFormat.Decode(GojoNumericFormat.Encode(template.Frame.Surface, gap.Surface, endpoint.Surface,
            height.ArchiveSurface(), width.ArchiveSurface(), hg.Surface, wg.Surface,
            upper, lower, picture.Upper.EndPort, picture.Lower.EndPort));
        var frame = Curve.Frame.FromSurface((Relation)roots[0]);
        var copiedGap = GeometricGapClaim.FromSurface(frame, (Relation)roots[1]);
        var copiedEndpoint = GeometricEndpointClaim.FromSurface(frame, (Relation)roots[2]);
        var h = CoupledExtentMachine.Restore((Relation)roots[3]); var w = CoupledExtentMachine.Restore((Relation)roots[4]);
        var copiedHg = PendingAdjustmentGate.FromSurface((Relation)roots[5]); var copiedWg = PendingAdjustmentGate.FromSurface((Relation)roots[6]);
        var hRead = copiedGap.Observe((Relation)roots[7], (Relation)roots[8]);
        var wRead = copiedEndpoint.Observe((Relation)roots[9], (Relation)roots[10]);
        Assert.Equal(3, hRead.Gap); Assert.Equal(-2, wRead.AlongDifference);
        Assert.Equal(1, h.Step(copiedHg.Evaluate(hRead.Packet).Packet).RequestedDelta);
        Assert.Equal(1, w.Step(copiedWg.Evaluate(wRead.Packet).Packet).RequestedDelta);
        Assert.Equal(2, h.Snapshot().Value); Assert.Equal(9, w.Snapshot().Value);
        Assert.Equal(1, height.Snapshot().Value); Assert.Equal(8, width.Snapshot().Value);
    }
}

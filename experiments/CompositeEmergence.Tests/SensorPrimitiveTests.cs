using Gojo.Core;
using Gojo.Structures;
using PrimitiveLearning;

namespace CompositeEmergence.Tests;

public sealed class SensorPrimitiveTests
{
    [Fact]
    public void PrimitiveLearnsOnlyItsOwnChannelAndRetainsOpaqueObservationIdentity()
    {
        var axis = new Carrier(); var primitive = new SensorPrimitive(axis); var eventId = new Carrier();
        var publication = NativeGraph.Publication(axis, 1, eventId, 10); primitive.Accept(publication);
        Assert.Same(eventId, NativeGraph.Identity(primitive.Current)); Assert.True(NativeGraph.TryTimestamp(primitive.Current, out var time)); Assert.Equal(10, time);
        Assert.Equal(0, primitive.Updates); primitive.Accept(publication); Assert.Equal(0, primitive.Updates);
        Assert.False(NativeGraph.TryValue(primitive.Current, axis, out _));
        primitive.Accept(NativeGraph.Publication(new Carrier(), 1, new Carrier(), 11)); Assert.Equal(0, primitive.Updates);
    }
    [Fact]
    public void LocalTransitionLearningCanPredictAConstantChannelWithoutOtherPorts()
    {
        var primitive = new SensorPrimitive(new Carrier());
        for (var i = 0; i < 12; i++) primitive.Accept(NativeGraph.Publication(primitive.Axis, 1, new Carrier(), i));
        Assert.True(OrdinalPredictor.TryReadPrediction(primitive.OwnForecast(), out var forecast)); Assert.Equal(1, forecast.Ticks);
        var before = primitive.Updates; primitive.Accept(NativeGraph.Publication(primitive.Axis, -1, new Carrier(), 20), false); Assert.Equal(before, primitive.Updates);
    }
    [Fact]
    public void RestoredPrimitiveKeepsNativeCountsAndDirectionAxis()
    {
        var primitive = new SensorPrimitive(new Carrier());
        for (var i = 0; i < 12; i++) primitive.Accept(NativeGraph.Publication(primitive.Axis, -1, new Carrier(), i));
        var root = Assert.IsType<Relation>(Assert.Single(GojoNumericFormat.Decode(GojoNumericFormat.Encode(primitive.Root)))); var restored = SensorPrimitive.FromRoot(root);
        Assert.Equal(NativeInspection.Signature(primitive.Root), NativeInspection.Signature(restored.Root));
        Assert.True(OrdinalPredictor.TryReadPrediction(restored.OwnForecast(), out var forecast)); Assert.Equal(-1, forecast.Ticks);
    }
}

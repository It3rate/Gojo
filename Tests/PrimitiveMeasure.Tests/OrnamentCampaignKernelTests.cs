using System.Numerics;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class OrnamentCampaignKernelTests
{
    private static OrnamentProgram CopyMark(byte observation = 1, byte flags = 0) =>
        new([new(observation, observation, 10)], [new(2, 2, 10, 0, 1)], flags);

    private static RailInput Rail(ulong ink = 0) =>
        new(7, ink, ink, 0, 2, 1, 3, 7, 7);

    [Fact]
    public void TransportPreservesPrivateWiringAndRejectsPaddingAndForwardReferences()
    {
        var original = new OrnamentProgram([new(5, 6, 8), new(16, 12, 8)],
            [new(2, 4, 14, 0, 0), new(8, 7, 8, 0, 1)], 3);
        var packet = original.Encode();
        var restored = OrnamentProgram.Decode(packet);
        Assert.Equal(packet, restored.Encode());
        Assert.Equal(original.Sensor.ToArray(), restored.Sensor.ToArray());
        Assert.Equal(original.Writer.ToArray(), restored.Writer.ToArray());
        Assert.Equal(3, restored.Flags);
        Assert.Throws<FormatException>(() => OrnamentProgram.Decode(packet[..^1]));
        var malformed = CopyMark().Encode();
        malformed[^1] |= 0x80;
        Assert.Throws<FormatException>(() => OrnamentProgram.Decode(malformed));
        Assert.Throws<ArgumentException>(() => new OrnamentProgram([new(16, 0, 10)], [new(0, 0, 0, 0, 1)]));
        Assert.Throws<ArgumentException>(() => new OrnamentProgram([new(1, 1, 8)], [new(8, 0, 8, 0, 1)]));
    }

    [Fact]
    public void StockIsConservedAndProtectedAttemptsSpendPermitsWithoutMovingInk()
    {
        var input = Rail() with { Protected = 1, EditBudget = 2 };
        var result = CopyMark().Compile().Run(input);
        Assert.Equal(2UL, result.Ink);
        Assert.Equal(2, result.Stock);
        Assert.Equal(2, result.Attempts);
        Assert.Equal(1, result.Blocked);
        Assert.Equal(1, result.Edits);
        Assert.Equal(5, result.BudgetRefusals);
        Assert.Equal(input.Stock, result.Stock + BitOperations.PopCount(result.Ink));
    }

    [Fact]
    public void ReadingACutOrUncertainPriorityRowCannotCommitTheLowerFallback()
    {
        var machine = new OrnamentProgram([new(1, 1, 10)],
            [new(2, 2, 10, 0, 1), new(1, 1, 15, 0, 3)]).Compile();
        var input = Rail();
        var cut = machine.Run(input, WireMode.Cut);
        var uncertain = machine.Run(input, WireMode.Unknown);
        Assert.Equal(0UL, cut.Ink);
        Assert.Equal(0, cut.Attempts);
        Assert.Equal(input.Beats, cut.Missing);
        Assert.Equal(0, cut.Unresolved);
        Assert.Equal(0UL, uncertain.Ink);
        Assert.Equal(input.Beats, uncertain.Unresolved);
        Assert.Equal(0, uncertain.Missing);
        Assert.Equal(0, machine.Run(input, WireMode.Inverted).Missing);
    }

    [Fact]
    public void AliasOfOneConcealedParticipantStaysCorrelatedAcrossTheRead()
    {
        var input = Rail() with { Hidden = 127 };
        var same = new OrnamentProgram([new(2, 2, 6)], [new(2, 2, 10, 0, 1)]).Compile();
        var independent = new OrnamentProgram([new(3, 4, 6)], [new(2, 2, 10, 0, 1)]).Compile();
        Assert.All(same.Replay(input), frame => Assert.Equal(1, frame.WireSupport));
        Assert.All(independent.Replay(input), frame => Assert.Equal(3, frame.WireSupport));
        Assert.Equal(input.Beats, independent.Run(input).Unresolved);
        Assert.Equal(0, same.Run(input).Unresolved);
    }

    [Fact]
    public void LiveGapFeedbackCanPropagateAndFrozenInitialEvidenceCannot()
    {
        var input = Rail(1) with { Start = 1, Beats = 3 };
        var machine = CopyMark(3).Compile();
        Assert.Equal(0b1111UL, machine.Run(input).Ink);
        Assert.Equal(0b0011UL, machine.Run(input, WireMode.Frozen).Ink);
        // The input goal and original motif are immutable over both runs.
        Assert.Equal(1UL, input.Seed);
        Assert.Equal(1UL, input.Initial);
    }

    [Fact]
    public void DelayedWireWaitsOneBeatAndExternalInterventionIsAccountedSeparately()
    {
        var input = Rail(1) with { Start = 1, Beats = 3, InterventionBeat = 2, InterventionXor = 0b100000UL };
        var machine = CopyMark(1, 2).Compile();
        var frames = machine.Replay(input);
        Assert.Equal(1, frames[0].WireSupport);
        Assert.False(frames[0].Moved);
        Assert.True(frames[1].Moved);
        Assert.Equal(0b100101UL, frames[2].Before);
        var result = machine.Run(input);
        Assert.Equal(1, result.ExternalDelta);
        Assert.Equal(BitOperations.PopCount(input.Initial) + input.Stock + result.ExternalDelta,
            BitOperations.PopCount(result.Ink) + result.Stock);
    }

    [Fact]
    public void ReversedFrameCarriesRelativeTransfersAndOrderWithIt()
    {
        const int width = 9;
        static ulong Reflect(ulong bits) =>
            Enumerable.Range(0, width).Aggregate(0UL, (mask, i) => mask | (((bits >> i) & 1) << (width - 1 - i)));
        var input = new RailInput(width, 0b1001001, 0b1001001, 1, 3, 1, 2, 10, 18, Start:3);
        SensorRow[] sense = [new(5, 7, 14)];
        WriterRow[] write = [new(2, 2, 10, 1, 3)];
        var forward = new OrnamentProgram(sense, write).Compile().Run(input);
        var reversed = new OrnamentProgram(sense, write, 1).Compile().Run(input with
        { Initial = Reflect(input.Initial), Seed = Reflect(input.Seed), Protected = Reflect(input.Protected), Start = width - 1 - input.Start });
        Assert.Equal(Reflect(forward.Ink), reversed.Ink);
        Assert.Equal(forward.Edits, reversed.Edits);
        Assert.Equal(forward.Blocked, reversed.Blocked);
    }

    [Fact]
    public void CompiledReadsAgreeWithGenericGojoRelationsAndDirectRailWalks()
    {
        // Arbitrary small boundary cases for the executor, separate from all
        // scientific train/validation/test/reserve episodes and predictions.
        var random = new Random(3281);
        for (var trial = 0; trial < 60; trial++)
        {
            int width = trial == 0 ? 63 : random.Next(3, 22);
            ulong mask = (1UL << width) - 1;
            ulong Bits() => (ulong)random.NextInt64() & mask;
            var input = new RailInput(width, Bits(), Bits(), Bits(), random.Next(1,width + 1),
                (byte)random.Next(16), random.Next(0,8), random.Next(0,15), 19,
                trial % 3 == 0 ? 0 : Bits(), 7, Bits(), random.Next(width));
            var sensor = Enumerable.Range(0, 4).Select(i => new SensorRow((byte)random.Next(16+i),
                (byte)random.Next(16+i), (byte)random.Next(16))).ToArray();
            var writer = Enumerable.Range(0, 4).Select(i => new WriterRow((byte)random.Next(8+i),
                (byte)random.Next(8+i), (byte)random.Next(16), (byte)random.Next(4), (byte)random.Next(4))).ToArray();
            var machine = OrnamentProgram.Decode(new OrnamentProgram(sensor, writer, (byte)random.Next(4)).Encode()).Compile();
            foreach (var mode in Enum.GetValues<WireMode>())
            {
                var actual = machine.Run(input, mode);
                Assert.Equal(machine.RunReference(input, mode), actual);
                Assert.Equal(actual.Edits + actual.Blocked, actual.Attempts);
                Assert.InRange(actual.Attempts, 0, input.EditBudget);
                Assert.Equal(BitOperations.PopCount(input.Initial) + input.Stock + actual.ExternalDelta,
                    BitOperations.PopCount(actual.Ink) + actual.Stock);
                Assert.Equal(((input.Initial ^ input.InterventionXor) & input.Protected), actual.Ink & input.Protected);
            }
        }
    }
}

using Gojo.Core;
using C = BreakDrivenPrediction.Continuation;
using S = BreakDrivenPrediction.TimingSource;

namespace BreakDrivenPrediction.Tests;

public class TimingSignalCoverageTests
{
    [Fact]
    public void CompleteAbsenceOfReversalsDoesNotEstablishFlatInput()
    {
        var flatClock = new Carrier();
        var risingClock = new Carrier();
        var flat = S.Create(1000, flatClock, sampleStride: 1);
        var rising = S.Create(1000, risingClock, sampleStride: 1);
        long sharedLevel = 1000;
        Relation? flatPacket = null, risingPacket = null;

        // Establish a known positive last difference before one environment
        // becomes flat and the other keeps rising. No turn is hidden at25.
        for (long tick = 1; tick <= 48; tick++)
        {
            if (tick <= 24) sharedLevel += ((tick - 1) / 5) % 2 == 0 ? 1 : -1;
            var flatLevel = sharedLevel;
            var risingLevel = sharedLevel + Math.Max(0, tick - 24);
            Assert.True(S.TryObserve(flat, new CarrierLocation(flatClock, tick),
                new CarrierLocation(S.Sensor(flat), flatLevel), out flat));
            Assert.True(S.TryObserve(rising, new CarrierLocation(risingClock, tick),
                new CarrierLocation(S.Sensor(rising), risingLevel), out rising));
            if (tick % 12 != 0) continue;
            Assert.True(S.Publish(flat, new CarrierLocation(flatClock, tick), out flat, out flatPacket));
            Assert.True(S.Publish(rising, new CarrierLocation(risingClock, tick), out rising, out risingPacket));
            if (tick <= 24) continue;
            AssertCompleteEmptyReversals(flatPacket);
            AssertCompleteEmptyReversals(risingPacket);
        }

        // The endpoint channel still distinguishes these inputs. An event-only
        // consumer cannot assign flatness merely from equal empty event groups.
        Assert.Equal(sharedLevel, Assert.IsType<CarrierLocation>(C.Parts(flatPacket!)[4]).Ticks);
        Assert.Equal(sharedLevel + 24, Assert.IsType<CarrierLocation>(C.Parts(risingPacket!)[4]).Ticks);
    }

    private static void AssertCompleteEmptyReversals(Relation packet)
    {
        var fields = C.Parts(packet);
        Assert.Empty(C.Parts(Assert.IsType<Relation>(fields[3])));
        Assert.Empty(C.Parts(Assert.IsType<Relation>(fields[5])));
    }
}

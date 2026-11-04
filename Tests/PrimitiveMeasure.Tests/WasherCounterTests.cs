using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class WasherCounterTests
{
    [Fact]
    public void An_identical_silent_beat_can_hide_either_a_lost_request_or_a_real_transfer()
    {
        var lostRequest = new WasherCounter(WasherCounter.RememberDelivery);
        var lostReceipt = new WasherCounter(WasherCounter.RememberDelivery);
        var none = lostRequest.Step(false,true);
        var one = lostReceipt.Step(true,false);
        Assert.False(none.Heard);
        Assert.False(one.Heard);
        Assert.Equal((false,false,false),(none.RequestArrived,none.Dropped,none.ReceiptSent));
        Assert.Equal((true,true,true),(one.RequestArrived,one.Dropped,one.ReceiptSent));
        Assert.Equal((3,0),((int)none.StockBits,(int)none.TrayBits));
        Assert.Equal((2,1),((int)one.StockBits,(int)one.TrayBits));
        Assert.False(lostRequest.TrayLamp);
        Assert.True(lostReceipt.TrayLamp);
        var retry = lostReceipt.Step(true,true);
        Assert.Equal((false,true,true),(retry.Dropped,retry.ReceiptSent,retry.Done));
        Assert.Equal((2,2),((int)retry.StockBits,(int)retry.TrayBits));
        var idle = lostReceipt.Step(true,true);
        Assert.Equal((false,false,false,true),(idle.RequestArrived,idle.Dropped,idle.ReceiptSent,idle.Done));
    }

    [Fact]
    public void On_an_empty_bin_the_blind_counter_cannot_acknowledge_a_new_delivery()
    {
        var counter = new WasherCounter(WasherCounter.ServeEveryRequest);
        byte[] actualRows = [4]; // (outlet 0,intake 0) then (outlet 1,intake 0).
        Assert.Equal(actualRows,counter.FeedRows);
        Assert.Equal(PassageLaw.Controlled().PackedBytes(),counter.FeedLaw);
        var three = WasherSurvey.Run(counter,7,4,3);
        Assert.Equal(new WasherHistory(3,0),WasherSurvey.Observe(three));
        Assert.True(three[^1].RequestArrived);
        Assert.False(three[^1].ReceiptSent);
        Assert.Equal(0,counter.StockBits);
        Assert.Equal(3,counter.TrayBits);
        Assert.True(counter.TrayLamp); // Sampling the lamp leaves both occupied sites intact.
        Assert.Equal(3,counter.TrayBits);
    }

    [Theory]
    [InlineData(WasherCounter.RememberDelivery,3367,0,665)]
    [InlineData(WasherCounter.ServeEveryRequest,2928,1824,1104)]
    public void A_bit_read_predicts_the_whole_trace_and_each_clock_preserves_stock(byte law,int confirmed,int doubles,int pending)
    {
        int confirmations = 0, duplicates = 0, waitingWithParts = 0;
        for (int f = 0; f < 64; f++)
            for (int r = 0; r < 64; r++)
            {
                var trace = WasherSurvey.Run(new(law),f,r,6);
                var history = WasherSurvey.Observe(trace);
                Assert.Equal(WasherSurvey.Predict(law,f,r,6),history);
                int inTray = 0;
                foreach (var b in trace)
                {
                    if (b.Dropped) inTray++;
                    Assert.Equal(inTray,BitOperations.PopCount((uint)b.TrayBits));
                    Assert.Equal(2-inTray,BitOperations.PopCount((uint)b.StockBits));
                }
                if (history.Heard != 0) confirmations++;
                if (history.Parts == 2) duplicates++;
                if (history.Heard == 0 && history.Parts != 0) waitingWithParts++;
            }
        Assert.Equal((confirmed,doubles,pending),(confirmations,duplicates,waitingWithParts));
    }

    [Theory]
    [InlineData(1,0,0)]
    [InlineData(2,0,2)]
    [InlineData(3,1,16)]
    [InlineData(6,439,1824)]
    public void Which_rules_can_be_distinguished_depends_on_the_surface_and_the_controlled_schedule(
        int beats,int expectedReceiptDifferences,int expectedReleaseDifferences)
    {
        int receiptDifferences = 0, releaseDifferences = 0;
        var guardedWords = new HashSet<byte>();
        var eagerWords = new HashSet<byte>();
        for (int forward = 0; forward <= WasherSurvey.Mask(beats); forward++)
            for (int back = 0; back <= WasherSurvey.Mask(beats); back++)
            {
                var guarded = WasherSurvey.Predict(WasherCounter.RememberDelivery,forward,back,beats);
                var eager = WasherSurvey.Predict(WasherCounter.ServeEveryRequest,forward,back,beats);
                Assert.Equal(guarded.LampWord(beats),eager.LampWord(beats));
                guardedWords.Add(guarded.Heard);
                eagerWords.Add(eager.Heard);
                if (guarded.Drops != eager.Drops) releaseDifferences++;
                if (guarded.Heard == eager.Heard) continue;
                receiptDifferences++;
                if (beats == 3) Assert.Equal((7,4),(forward,back));
            }
        Assert.True(guardedWords.SetEquals(eagerWords));
        Assert.Equal(beats+1,guardedWords.Count);
        Assert.Equal((expectedReceiptDifferences,expectedReleaseDifferences),(receiptDifferences,releaseDifferences));
    }

    [Theory]
    [InlineData(WasherCounter.RememberDelivery)]
    [InlineData(WasherCounter.ServeEveryRequest)]
    public void Masked_receipts_and_optional_lamp_admit_exactly_the_observed_histories(byte law)
    {
        var worlds = (from f in Enumerable.Range(0,16)
                      from r in Enumerable.Range(0,16)
                      let counter = new WasherCounter(law)
                      let h = WasherSurvey.Observe(WasherSurvey.Run(counter,f,r,4))
                      select (history:h,lamp:counter.TrayLamp)).Distinct().ToArray();
        foreach (bool? lamp in new bool?[]{null,false,true})
            for (int visible = 0; visible < 16; visible++)
                for (int heard = 0; heard < 16; heard++)
                {
                    var actual = worlds.Where(w => (lamp == null || w.lamp == lamp) &&
                        (w.history.Heard & visible) == (heard & visible)).Select(w=>w.history).ToHashSet();
                    Assert.True(actual.SetEquals(WasherSurvey.Possible(law,4,heard,visible,lamp)));
                }
        Assert.Empty(WasherSurvey.Possible(law,2,3,3)); // Two receipts heard on one order.
        Assert.Empty(WasherSurvey.Possible(law,1,1,1,false));
        Assert.Equal([new WasherHistory(0,0)],WasherSurvey.Possible(law,0,0,0));
    }
}

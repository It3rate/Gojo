using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class WasherProbeTests
{
    private static readonly WasherProbeBank Bank = WasherProbeBank.Calibrated;

    [Fact]
    public void Calibration_connects_actual_sampled_and_double_release_sites_to_the_program()
    {
        Assert.Equal(new byte[]{9,73,137,19,83,147,139,39,103,167},
            Enumerable.Range(0,10).Select(i=>Bank.Probe(i).Word));
        Assert.Equal(0x36,Bank.Yes(0));
        Assert.Equal(0xDB6,Bank.Yes(1));
        Assert.Equal(0x820,Bank.Yes(5));
        Assert.Equal(0x006,Bank.Yes(7));
        Assert.Equal(0x800,Bank.Yes(6));
        for (int i = 0; i < WasherProbeBank.Count; i++)
        {
            Assert.Equal(i < 3 ? 0 : i == 6 ? 0x800 : 0x820,Bank.DoubleRelease(i));
            for (int k = 0; k < 12; k++)
            {
                var machine = new WasherTestStand(k);
                byte actual = machine.Sample(Bank.Probe(i));
                Assert.Equal(1 << ((Bank.Yes(i) >> k) & 1),(int)actual);
                Assert.Equal(k%3,BitOperations.PopCount((uint)machine.LastTrace[0].StockBits) +
                    BitOperations.PopCount((uint)machine.LastTrace[0].TrayBits));
            }
        }
    }

    [Fact]
    public void First_response_redirects_credit_and_no_second_question_uses_the_hidden_configuration()
    {
        var paths = new Dictionary<int,int[]>();
        foreach (int fixture in new[]{0,1,2,5,11})
        {
            var stand = new WasherTestStand(fixture);
            var desk = new WasherProbeDesk(7,9);
            var steps = new List<int>();
            while (desk.Step(stand.Sample) is {} step)
            {
                steps.Add(step.Row);
                Assert.Equal(1 << step.Row,step.Offers.Open & -step.Offers.Open);
                Assert.NotEqual(0,desk.Live & (1<<fixture));
            }
            paths[fixture] = steps.ToArray();
        }
        Assert.Equal([0,1],paths[0]);
        Assert.Equal([0,3,5],paths[1]);
        Assert.Equal(paths[1],paths[2]);
        Assert.Equal([0,1,5],paths[11]);
        var direct = new WasherProbeDesk(7,9,Bank.DoubleRelease(5));
        Assert.Equal(5,direct.Step(new WasherTestStand(5).Sample)!.Value.Row);
        Assert.True(direct.Answered);
        Assert.Equal(0x820,direct.Live); // Same material answer for both return leads.
        Assert.Null(direct.Step(_=>throw new Exception("unexpected probe")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Lost_or_unresolved_samples_spend_real_beats_without_manufacturing_silence(byte support)
    {
        var desk = new WasherProbeDesk(7,1);
        var step = desk.Step(_=>support)!.Value;
        Assert.Equal((0,WasherProbeBank.Family,0,support),(step.Row,step.After,desk.BeatsLeft,step.Sample));
        Assert.Equal(1,desk.Tried);
        Assert.Null(desk.Step(_=>throw new Exception()));
        var off = new WasherProbeDesk(7,1).Step(_=>1)!.Value;
        Assert.NotEqual(WasherProbeBank.Family,off.After);
    }

    [Fact]
    public void Stock_protection_depends_on_surviving_evidence_and_a_check_can_reject_every_candidate()
    {
        var safe = new WasherProbeDesk(7,9,protectStock:true);
        Assert.Equal(7,safe.Offers().Safe);
        safe.Step(new WasherTestStand(0).Sample);
        safe.Step(new WasherTestStand(0).Sample);
        Assert.Equal(0x249,safe.Live);
        Assert.Equal(1023,safe.Offers().Safe);
        Assert.False(safe.Answered); // Distinct empty mechanisms remain equivalent.
        var check = new WasherProbeDesk(7,9,checkPromises:true);
        var stand = new WasherTestStand(0);
        // Empty dispenser with a lamp stuck on is outside the calibrated family.
        byte BadLamp(WasherProbe p) { byte actual = stand.Sample(p); return p.Surface == WasherProbe.Lamp ? (byte)2 : actual; }
        var path = new List<ProbeTrial>();
        while (check.Step(BadLamp) is {} trial) path.Add(trial);
        Assert.Equal(0,check.Live);
        Assert.True(path[^1].Offers.Checking);
        Assert.Equal(WasherProbe.Fall,Bank.Probe(path[^1].Row).Surface);
        Assert.All(path.Where(s=>!s.Offers.Checking),s=>Assert.NotEqual(0,s.After));
    }

    [Fact]
    public void Receipt_only_can_guarantee_four_classes_in_five_beats_but_the_local_choice_can_miss()
    {
        Assert.Equal(5,WasherProbeAudit.MinimumGuaranteedBeats(WasherProbeBank.Family,1,null,false));
        Assert.Null(WasherProbeAudit.MinimumGuaranteedBeats(WasherProbeBank.Family,1,Bank.Yes(5),false));
        Assert.Null(WasherProbeAudit.MinimumGuaranteedBeats(WasherProbeBank.Family,7,Bank.Yes(5),true));
        var stand = new WasherTestStand(1);
        var cheap = new WasherProbeDesk(1,5);
        Assert.Equal(0,cheap.Step(stand.Sample)!.Value.Row);
        Assert.Equal(3,cheap.Step(stand.Sample)!.Value.Row);
        Assert.Equal(2,cheap.BeatsLeft);
        Assert.Null(cheap.Step(stand.Sample));
        Assert.False(WasherProbeAudit.Settled(cheap.Live,1,null));
        var balanced = new WasherProbeDesk(1,5,preferBalanced:true);
        Assert.Equal(0,balanced.Step(stand.Sample)!.Value.Row);
        Assert.Equal(7,balanced.Step(stand.Sample)!.Value.Row);
        Assert.True(WasherProbeAudit.Settled(balanced.Live,1,null));
    }

    [Fact]
    public void Opening_the_return_before_sampling_the_chute_separates_the_two_fully_loaded_retry_machines()
    {
        int pair = (1<<5) | (1<<11);
        var original = new WasherProbeDesk(4,9,candidates:pair,sockets:1023 ^ (1<<6));
        Assert.Null(original.Step(_ => throw new Exception()));
        var stand = new WasherTestStand(5);
        var expanded = new WasherProbeDesk(4,9,candidates:pair);
        Assert.Equal(6,expanded.Step(stand.Sample)!.Value.Row);
        Assert.Equal(1<<5,expanded.Live);
        // The sample is the second beat's falling edge. The first receipt stops this desk.
        Assert.True(stand.LastTrace[0].Heard);
        Assert.False(stand.LastTrace[1].RequestArrived);
    }

    [Fact]
    public void Ten_test_sockets_recover_every_distinction_in_full_six_beat_traces()
    {
        int[,] separated = new int[12,12];
        for (int f = 0; f < 64; f++)
        for (int b = 0; b < 64; b++)
        {
            var histories = new WasherHistory[12];
            for (int k = 0; k < 12; k++)
            {
                byte law = k / 3 % 2 == 0 ? WasherCounter.RememberDelivery : WasherCounter.ServeEveryRequest;
                int back = k < 6 ? b : 0;
                var trace = WasherSurvey.Run(new(law,k%3),f,back,6);
                var actual = WasherSurvey.Observe(trace);
                histories[k] = actual;
                Assert.Equal(WasherSurvey.Predict(law,f,back,6,k%3),actual);
                for (int j = 0; j < 6; j++)
                    Assert.Equal(trace[j].TrayBits != 0, (actual.LampWord(6) & (1<<j)) != 0);
                for (int j = 0; j < k; j++)
                {
                    var other = histories[j];
                    if (actual.Heard != other.Heard) separated[j,k] |= 1;
                    if (actual.LampWord(6) != other.LampWord(6)) separated[j,k] |= 2;
                    if (actual.Drops != other.Drops) separated[j,k] |= 4;
                }
            }
        }
        int[] counts = [1,4,2,5,4,6,4,6];
        for (int surfaces = 0; surfaces < 8; surfaces++)
        {
            var groups = new HashSet<int>();
            for (int k = 0; k < 12; k++)
            {
                int equivalent = 0;
                for (int j = 0; j < 12; j++)
                    if ((separated[Math.Min(j,k),Math.Max(j,k)] & surfaces) == 0) equivalent |= 1 << j;
                var desk = new WasherProbeDesk(surfaces,18);
                while (desk.Step(new WasherTestStand(k).Sample) != null) { }
                Assert.Equal(equivalent,desk.Live);
                groups.Add(equivalent);
            }
            Assert.Equal(counts[surfaces],groups.Count);
        }
    }
}

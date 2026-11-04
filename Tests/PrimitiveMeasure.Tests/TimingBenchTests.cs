namespace Gojo.Experiments.PrimitiveMeasure.Tests;

public sealed class TimingBenchTests
{
    private const int Pair = (1 << 5) | (1 << 11), Slot = WasherProbeBank.Feedback;
    private static SocketStudy Finish(SocketStudy s)
    {
        while (s.Step()) { }
        Assert.False(s.Step());
        return s;
    }

    [Theory]
    [InlineData(5,1)]
    [InlineData(11,2)]
    public void Old_factory_rack_narrows_twelve_to_two_before_the_bench_can_bind_a_socket(int ticket,byte expected)
    {
        var s = Finish(SocketSurvey.Start(TimingStands.Factory,0xFFF,ticket,4));
        Assert.Equal(Pair,s.RequestedPair);
        Assert.Equal([2,5,Slot],s.Trials.Select(t=>t.Row));
        Assert.Equal((byte)0x93,s.Bench!.Retained!.Value.Parent.Word);
        Assert.Equal((byte)0x8B,s.Rack.Probe(Slot).Word);
        Assert.Equal(Pair,s.Rack.Scope(Slot));
        Assert.Equal(1<<11,s.Rack.Yes(Slot));
        Assert.Equal(1<<11,s.Rack.DoubleRelease(Slot));
        Assert.Equal(["Repeat requested","Socket retained"],s.Checks.Select(c=>c.Outcome));
        Assert.Equal(new TimingRead(1,1),s.Checks[0].Left!.Value);
        Assert.Equal(new TimingRead(2,2),s.Checks[0].Right!.Value);
        Assert.Equal((40,42),(s.Bench.BeatsLeft,s.Bench.UnitsLeft));
        Assert.Equal(expected,s.Trials[^1].Sample);
        Assert.Equal(1<<ticket,s.Desk.Live);
        var reuse = new WasherProbeDesk(4,2,candidates:Pair,rack:s.Rack);
        Assert.Equal(Slot,reuse.Step(new WasherTestStand(11).Sample)!.Value.Row);
        Assert.Equal(1<<11,reuse.Live);
        var wider = new WasherProbeDesk(4,9,candidates:Pair|2,rack:s.Rack);
        Assert.Equal(0,wider.Offers().Wired & (1 << Slot));
        Assert.Equal(0,TimingStands.Factory.FreshRack().Scope(Slot));
    }

    [Fact]
    public void Two_reference_rechecks_cannot_overwrite_the_first_retained_socket()
    {
        var rack = TimingStands.Factory.FreshRack();
        var desk = new WasherProbeDesk(4,9,candidates:Pair,rack:rack);
        var request = desk.RequestTiming()!;
        TimingBench Bench() => new(request,TimingStands.Factory.Run,TimingStands.Factory.MaximumUnits,8,8);
        var first = Bench(); var second = Bench();
        first.Step(); second.Step();
        Assert.Equal("Socket retained",first.Step()!.Value.Outcome);
        Assert.Equal("Request expired",second.Step()!.Value.Outcome);
        Assert.Null(second.Retained);
        Assert.Null(desk.RequestTiming());
    }

    [Fact]
    public void Both_factory_and_ornament_controls_can_remain_observationally_identical()
    {
        var spare = Finish(SocketSurvey.Start(TimingStands.Factory,6,2,7));
        var mirroredFrame = Finish(SocketSurvey.Start(TimingStands.Border,17,4,7));
        foreach (var s in new[]{spare,mirroredFrame})
        {
            Assert.NotNull(s.Bench);
            Assert.NotEmpty(s.Checks);
            Assert.All(s.Checks,c=>Assert.Equal("References agree",c.Outcome));
            Assert.Equal(s.RequestedPair,s.Desk.Live);
            Assert.Equal(0,s.Rack.Scope(Slot));
        }
    }

    [Theory]
    [InlineData(false,1,0x53,0x4B)]
    [InlineData(true,2,0x4B,0x53)]
    public void A_horizontal_ornament_read_can_be_earned_by_moving_the_accent_in_either_direction(
        bool early,int directions,byte parent,byte child)
    {
        // With early accents installed, the swap already separates at H. Use
        // raw two-reference comparison below to check the reverse edit itself.
        var domain = TimingStands.Border;
        var p = new WasherProbe(parent);
        Assert.Contains(new PulseEdit(p,new(child)),TimingBench.Neighbors([p],directions));
        byte[] Sensor(byte w) => [domain.Run(0,new(w)).Support,domain.Run(1,new(w)).Support];
        Assert.Equal(new byte[]{2,2},Sensor(0x53));
        Assert.Equal(new byte[]{1,2},Sensor(0x4B));
        var s = Finish(SocketSurvey.Start(domain,3,1,2,early:early));
        Assert.Equal(2,s.Desk.Live);
        Assert.Equal(!early,s.Bench?.Retained != null);
        var drawn = TimingStands.Swatch(0,new(0x4B),false);
        Assert.Equal(2,drawn[0].Length);
        Assert.Equal(BorderStudy.Down,Assert.Single(drawn[1]).Move.Bits);
    }

    [Theory]
    [InlineData(SocketMode.MissingReference,"Incomplete readings")]
    [InlineData(SocketMode.AmbiguousReference,"Incomplete readings")]
    [InlineData(SocketMode.ChangedRepeat,"Split did not repeat")]
    [InlineData(SocketMode.BenchLimit,"Bench time limit")]
    [InlineData(SocketMode.MaterialLimit,"Material limit")]
    [InlineData(SocketMode.Protected,"Stock protection")]
    [InlineData(SocketMode.DeskLimit,"Desk time limit")]
    public void Blocked_or_unrepeatable_variants_leave_the_slot_vacant(SocketMode mode,string reason)
    {
        var s = Finish(SocketSurvey.Start(TimingStands.Factory,Pair,5,4,mode));
        Assert.Contains(s.Checks,c=>c.Outcome==reason);
        Assert.Equal(Pair,s.Desk.Live);
        Assert.Equal(0,s.Rack.Scope(Slot));
        Assert.True(s.Bench!.BeatsLeft>=0 && s.Bench.UnitsLeft>=0);
        if (mode is SocketMode.MaterialLimit or SocketMode.Protected or SocketMode.DeskLimit)
            Assert.All(s.Checks,c=>Assert.Null(c.Left));
    }

    [Fact]
    public void Retention_depends_on_same_reference_correspondence_on_two_independent_runs()
    {
        for (int first=0;first<4;first++)
        for (int repeat=0;repeat<4;repeat++)
        {
            var rack = TimingStands.Factory.FreshRack();
            var request = new WasherProbeDesk(4,9,candidates:Pair,rack:rack).RequestTiming()!;
            var tickets = new List<int>();
            TimingRead Read(int ticket,WasherProbe p)
            {
                int joint = tickets.Count<2?first:repeat;
                tickets.Add(ticket);
                return new((byte)(1<<((joint>>(ticket==5?0:1))&1)),0);
            }
            var bench = new TimingBench(request,Read,_=>0,8,0);
            bench.Step();
            if (first is 1 or 2)
            {
                bench.Step();
                Assert.Equal([5,11,11,5],tickets);
            }
            Assert.Equal(first==repeat && first is 1 or 2,bench.Retained.HasValue);
        }
    }

    [Fact]
    public void A_held_score_continues_both_heads_across_repeats_after_the_public_sample()
    {
        var edited=new WasherProbe(0x4B);
        var shortRun=TimingStands.Swatch(1,edited,false);
        var repeated=TimingStands.Swatch(1,edited,false,3);
        Assert.Equal(shortRun.SelectMany(x=>x),repeated.Take(2).SelectMany(x=>x));
        Assert.Equal(BorderStudy.Up,repeated[0][1].Move.Bits);
        Assert.Equal(BorderStudy.Down,repeated[2][1].Move.Bits);
        Assert.Equal(BorderStudy.Up,repeated[4][1].Move.Bits);
        var reoriented=TimingStands.Swatch(5,edited,false,3);
        Assert.NotEqual(repeated[0][0].Move.Bits,reoriented[0][0].Move.Bits);
        Assert.Equal(repeated.SelectMany(x=>x).Select(e=>(e.Move.Axis,e.Move.Sense)),
            reoriented.SelectMany(x=>x).Select(e=>(e.Move.Axis,e.Move.Sense)));
    }

    [Fact]
    public void Clean_pair_episodes_recover_exactly_the_splits_available_with_one_adjacent_edit()
    {
        foreach (var stand in new[]{TimingStands.Factory,TimingStands.Border,TimingStands.ReverseBorder})
        foreach (bool early in new[]{false,true})
        {
            var seed=stand.FreshRack(early);
            var cache=new Dictionary<(int,byte),byte>();
            byte Read(int k,byte w)
            {
                if (cache.TryGetValue((k,w),out byte saved)) return saved;
                byte sample = stand.Run(k,new(w)).Support;
                cache.Add((k,w),sample);
                return sample;
            }
            for(int a=0;a<stand.Types;a++)
            for(int b=a+1;b<stand.Types;b++)
            for(int sensors=0;sensors<8;sensors++)
            {
                var parents=Enumerable.Range(0,10).Where(i=>seed.Scope(i)!=0)
                    .Select(seed.Probe).Where(p=>(sensors & 1<<p.Surface)!=0).ToArray();
                bool installed=parents.Any(p=>Read(a,p.Word)!=Read(b,p.Word));
                // All seed forward contacts are occupied. Independently move
                // the sole accent to every in-window adjacent time position.
                bool usefulEdit=parents.Any(p=>Enumerable.Range(0,p.Beats).Any(t=>
                {
                    int old=System.Numerics.BitOperations.TrailingZeroCount((uint)p.Back);
                    byte word=(byte)((p.Word & ~56) | (1<<t)<<3);
                    return Math.Abs(old-t)==1 && Read(a,word)!=Read(b,word);
                }));
                var s=Finish(SocketSurvey.Start(stand,(1<<a)|(1<<b),a,sensors,early:early));
                Assert.Equal(!installed && usefulEdit,s.Bench?.Retained.HasValue==true);
                Assert.Equal(installed || usefulEdit,s.Desk.Answered);
                if (s.Desk.Answered) Assert.Equal(1<<a,s.Desk.Live);
            }
        }
    }

    [Fact]
    public void Pulse_edit_grammar_matches_independent_adjacent_move_enumeration()
    {
        for (int sensor=0;sensor<3;sensor++)
        for (int f=1;f<=7;f++)
        for (int b=0;b<=7;b++)
        for (int dir=0;dir<=3;dir++)
        {
            var p = new WasherProbe((byte)(sensor<<6|b<<3|f));
            if (b>=1<<p.Beats) continue;
            var expected = new HashSet<byte>();
            for (int lane=0;lane<=3;lane+=3)
            for (int from=0;from<p.Beats;from++)
            foreach (int delta in new[]{-1,1})
            {
                int to=from+delta;
                if (to<0 || to>=p.Beats || (dir & (delta<0?1:2))==0) continue;
                int source=1<<(lane+from),target=1<<(lane+to);
                if ((p.Word & source)==0 || (p.Word & target)!=0) continue;
                var candidate=new WasherProbe((byte)(p.Word-source+target));
                if (candidate.Beats==p.Beats && candidate.Forward!=0) expected.Add(candidate.Word);
            }
            var actual=TimingBench.Neighbors([p,p],dir).Select(e=>e.Child.Word).ToArray();
            Assert.Equal(expected.Count,actual.Length);
            Assert.True(expected.SetEquals(actual));
        }
    }
}

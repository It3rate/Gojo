using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure;

public readonly record struct TimingRead(byte Support, int Units);

/// <summary>Two adapters for the same eight-bit timing cartridge. Reference and
/// unknown runs each start with fresh stock or a pen at its starting phase.</summary>
public sealed class TimingStands
{
    public int Types { get; }
    public int Family => (1 << Types) - 1;
    public Func<int,WasherProbe,TimingRead> Run { get; }
    public Func<WasherProbe,int> MaximumUnits { get; }
    private readonly Lazy<WasherProbeBank>[] templates;
    public WasherProbeBank FreshRack(bool early = false) => templates[early ? 1 : 0].Value.Copy();

    private TimingStands(int types, Func<int,WasherProbe,TimingRead> run, Func<WasherProbe,int> maximum)
    {
        Types = types;
        Run = run;
        MaximumUnits = maximum;
        templates = new[]{false,true}.Select(early=>new Lazy<WasherProbeBank>(
            ()=>WasherProbeBank.Seed(Family,run,early))).ToArray();
    }

    internal static TimingRead Washer(int fixture, WasherProbe program)
    {
        var stand = new WasherTestStand(fixture);
        byte sample = stand.Sample(program);
        return new(sample,WasherSurvey.Observe(stand.LastTrace).Parts);
    }

    public static TimingStands Factory { get; } = new(12,Washer,p=>Math.Min(2,BitOperations.PopCount((uint)p.Forward)));
    public static TimingStands Border { get; } = Printer(false);
    public static TimingStands ReverseBorder { get; } = Printer(true);
    private static TimingStands Printer(bool reverse) => new(8,(k,p)=>
    {
        var trace = Swatch(k,p,reverse);
        var end = new ReachBalance();
        foreach (var e in trace[^1]) end.Include(e.Move);
        byte x = end.SignSupport(0), y = end.SignSupport(1);
        // The supplied scores use one reach on each axis. Zero is sign support 010.
        bool on = p.Surface switch { 0 => x != 2 || y != 2, 1 => x != 2, 2 => y == 4,
            _ => throw new ArgumentOutOfRangeException(nameof(p)) };
        return new(on ? (byte)2 : (byte)1,trace.Count(beat=>beat.Any(e=>e.Move.Sense != 0)));
    },p=>BitOperations.PopCount((uint)((p.Forward | p.Back) & ((1 << p.Beats)-1))));

    public static BorderEmission[][] Swatch(int fixture, WasherProbe probe, bool reverse, int repetitions = 1)
    {
        if ((uint)fixture >= 8 || probe.Forward == 0 || repetitions < 1) throw new ArgumentOutOfRangeException();
        var score = (fixture / 2) switch
        {
            0 or 2 => BorderStudy.Together(),
            1 => BorderStudy.OpposedSiblings(true),
            _ => BorderStudy.LoopWithDrift(true)
        };
        byte[] a = score.VoiceBytes(fixture & 1), b = score.VoiceBytes(1 - (fixture & 1));
        if (fixture / 2 == 2)
        {
            // Same physical movement under the opposite O/D convention.
            for (int i=0;i<a.Length;i++) a[i] ^= ThreeBitMeasure.Orientation | ThreeBitMeasure.Direction;
            for (int i=0;i<b.Length;i++) b[i] ^= ThreeBitMeasure.Orientation | ThreeBitMeasure.Direction;
        }
        byte[] clock = new byte[probe.Beats];
        for (int i=0;i<clock.Length;i++)
            clock[reverse ? clock.Length-1-i : i] = (byte)(((probe.Forward>>i)&1) | (((probe.Back>>i)&1)<<1));
        return BorderStudy.Trace(new BorderScore(a,b,clock),checked(clock.Length*repetitions),reverse);
    }
}

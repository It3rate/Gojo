using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>One single-order test cartridge. Low three bits drive the request wire,
/// next three drive the receipt wire, and the top two bind the sampled outlet.
/// The last driven request clocks the final sample.</summary>
public readonly record struct WasherProbe(byte Word)
{
    public const int Receipt = 0, Lamp = 1, Fall = 2;
    public int Forward => Word & 7;
    public int Back => (Word >> 3) & 7;
    public int Surface => Word >> 6;
    public int Beats => BitOperations.Log2((uint)Forward) + 1;
}

/// <summary>Test fixture. A socket starts a separately supplied, equally loaded
/// cartridge and publishes one final reading. Only the lab reads the audit trace.</summary>
public sealed class WasherTestStand
{
    public const int Types = 12;
    private readonly byte law;
    private readonly int stock;
    private readonly bool returnConnected;
    public WasherBeat[] LastTrace { get; private set; } = [];

    public WasherTestStand(int fixture)
    {
        if ((uint)fixture >= Types) throw new ArgumentOutOfRangeException(nameof(fixture));
        stock = fixture % 3;
        law = (fixture / 3 % 2) == 0 ? WasherCounter.RememberDelivery : WasherCounter.ServeEveryRequest;
        returnConnected = fixture < 6;
    }

    internal WasherCounter Drive(int forward, int back, int beats)
    {
        var machine = new WasherCounter(law,stock);
        LastTrace = WasherSurvey.Run(machine,forward,returnConnected ? back : 0,beats);
        return machine;
    }

    public byte Sample(WasherProbe probe)
    {
        if (probe.Forward == 0 || probe.Surface > WasherProbe.Fall)
            throw new ArgumentException("A test needs a clock and a connected sensor.");
        var machine = Drive(probe.Forward,probe.Back,probe.Beats);
        bool on = probe.Surface switch
        {
            WasherProbe.Receipt => LastTrace[^1].Heard,
            WasherProbe.Lamp => machine.TrayLamp,
            _ => LastTrace[^1].Dropped
        };
        return on ? (byte)2 : (byte)1;
    }
}

/// <summary>Each four-byte socket holds the program, twelve measured yes contacts,
/// and twelve contacts for runs that spent a second washer. Position sets priority.</summary>
public sealed class WasherProbeBank
{
    public const ushort Family = (1 << WasherTestStand.Types) - 1;
    public const int Feedback = 6, LateReceipt = 7, Count = 10;
    public const int AllSockets = (1 << Count) - 1;
    private readonly uint[] rows = new uint[Count];
    public uint Row(int index) => rows[index];
    public WasherProbe Probe(int index) => new((byte)rows[index]);
    public int Yes(int index) => (int)(rows[index] >> 8) & Family;
    public int DoubleRelease(int index) => (int)(rows[index] >> 20);
    public static WasherProbeBank Calibrated { get; } = new();

    private WasherProbeBank()
    {
        for (int i = 0; i < Count; i++)
        {
            int delayedIndex = i > Feedback ? i - 1 : i;
            int beats = delayedIndex / 3 + 1, surface = delayedIndex % 3;
            int forward = (1 << beats) - 1, back = 1 << (beats - 1);
            var probe = new WasherProbe((byte)(forward | (back << 3) | (surface << 6)));
            // A returned receipt can stop the next request. This extra socket
            // listens to the chute one beat AFTER opening the return wire.
            if (i == Feedback) probe = new(0b10_001_011);
            uint yes = 0, extra = 0;
            for (int k = 0; k < WasherTestStand.Types; k++)
            {
                var reference = new WasherTestStand(k);
                if (reference.Sample(probe) == 2) yes |= 1u << k;
                if (WasherSurvey.Observe(reference.LastTrace).Parts > 1) extra |= 1u << k;
            }
            rows[i] = probe.Word | (yes << 8) | (extra << 20);
        }
    }
}

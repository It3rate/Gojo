using System.Diagnostics;
using Gojo.Core;
using static Phototaxis.Native;

namespace Phototaxis;

/// <summary>External observations only. No creature policy or resource accounting lives here.</summary>
public sealed class MachineResources
{
    private readonly double? fixedDuty;
    private readonly long? fixedWork;
    private long windowStart = Stopwatch.GetTimestamp();
    private long windowWork;
    public double Duty { get; private set; }
    public bool Measured => fixedDuty is null;
    public MachineResources() { }
    private MachineResources(double duty, long microseconds)
    { fixedDuty = Duty = Math.Clamp(duty, 0, 1); fixedWork = microseconds * Stopwatch.Frequency / 1_000_000; }
    public static MachineResources Fixed(double duty = 0, long microsecondsPerBodyTick = 10) => new(duty, microsecondsPerBodyTick);
    public long Begin() => Stopwatch.GetTimestamp();
    public long BodyWorkSince(long start) => fixedWork ?? Math.Max(0, Stopwatch.GetTimestamp() - start);
    public void ObserveStep(long start)
    {
        if (fixedDuty is not null) return;
        var now = Stopwatch.GetTimestamp();
        windowWork += now - start;
        var wall = now - windowStart;
        if (wall < Stopwatch.Frequency / 10) return;
        Duty = Math.Clamp(windowWork / (double)wall, 0, 1);
        windowStart = now; windowWork = 0;
    }
}

/// <summary>Measured work becomes native calorie demand through a retained calibration.</summary>
public sealed class Computation
{
    public const long CaloriesPerRuntimeSecond = 500;
    public Carrier Runtime { get; }
    public Carrier Billed { get; }
    public Carrier Rate { get; }
    public Relation Demand { get; }
    private readonly Relation unpaid;
    public Relation LastBill { get; private set; } = G(new Placeholder());
    public Relation Root => G(Runtime, Billed, Demand, LastBill, unpaid);
    public Computation(Units units, long caloriesPerRuntimeSecond = CaloriesPerRuntimeSecond)
    {
        Runtime = new Carrier(units.Work.UnitShape);
        Billed = new Carrier(units.Calorie.UnitShape);
        Rate = new Carrier(units.Count.UnitShape); Aim(Rate, caloriesPerRuntimeSecond);
        // The common rate calibrates measured work to calorie demand. Fractional
        // demand stays in the cumulative calibration until a later whole calorie.
        Demand = Scale(Product(Runtime.ValueSlot, Rate.ValueSlot), 1, Stopwatch.Frequency, new Carrier(units.Calorie.UnitShape));
        unpaid = SumTo(new Carrier(units.Calorie.UnitShape), (Demand, 1), (Billed.ValueSlot, -1));
    }
    public void Bill(Carrier energy, Carrier sink)
    {
        var read = ReadMany(unpaid)[0];
        if (!TryValue(read, out var due)) { LastBill = read; return; }
        if (due <= 0) return;
        var payment = Transfer(energy, sink, due);
        Billed.Add((ulong)due);
        LastBill = G(read, payment, L(Runtime, (long)Runtime.Value), L(Billed, (long)Billed.Value));
    }
}

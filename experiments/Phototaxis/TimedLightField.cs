namespace Phototaxis;

// External photometry. Schedules and positions are unavailable to controller ports.
public readonly record struct LightPatch(double X, double Y, double Radius, double RemainingSeconds, int Source = 0);
public readonly record struct LightSchedule(double X, double Y, double Radius, int PeriodTicks, int OnTicks, int PhaseTicks,
    double VelocityX = 0, double VelocityY = 0);

public sealed class TimedLightField
{
    public LightSchedule[] Schedules { get; }
    private readonly int seed;
    private readonly long motionStartTick;
    private readonly double arenaWidth, arenaHeight;
    public TimedLightField(int seed, EcologySettings? ecology = null, long motionStartTick = 0,
        (double X, double Y)[]? origins = null, double arenaWidth = Bath.Width, double arenaHeight = Bath.Height)
    {
        ecology ??= EcologySettings.Original;
        ecology.Validate();
        this.seed = seed; this.motionStartTick = motionStartTick;
        this.arenaWidth = arenaWidth; this.arenaHeight = arenaHeight;
        var random = new Random(seed ^ 0x35d7ad);
        var motion = new Random(seed ^ 0x65b58a);
        Schedules = Enumerable.Range(0, 5).Select(i =>
        {
            var period = random.Next(90, 141) * Bath.TicksPerSecond;
            var on = random.Next(45, 81) * Bath.TicksPerSecond;
            var x = 45 + random.NextDouble() * (arenaWidth - 90);
            var y = 45 + random.NextDouble() * (arenaHeight - 90);
            var phase = random.Next(period);
            var angle = motion.NextDouble() * Math.Tau;
            return new LightSchedule(origins?[i].X ?? x, origins?[i].Y ?? y, ecology.LightRadius, period,
                Math.Max(1, (int)Math.Round(on * ecology.LightOnScale)), phase,
                ecology.LightDriftSpeed == 0 ? 0 : Math.Cos(angle) * ecology.LightDriftSpeed,
                ecology.LightDriftSpeed == 0 ? 0 : Math.Sin(angle) * ecology.LightDriftSpeed);
        }).ToArray();
    }

    public (double X, double Y) PositionAt(int source, long tick)
    {
        var s = Schedules[source];
        var seconds = (tick - motionStartTick) / (double)Bath.TicksPerSecond;
        static double Reflect(double start, double displacement, double minimum, double maximum)
        {
            if (displacement == 0) return start; // Retain exact original coordinates for stationary controls.
            var span = maximum - minimum;
            var unfolded = start - minimum + displacement;
            var phase = (unfolded % (2 * span) + 2 * span) % (2 * span);
            return minimum + (phase <= span ? phase : 2 * span - phase);
        }
        return (Reflect(s.X, s.VelocityX * seconds, 45, arenaWidth - 45),
            Reflect(s.Y, s.VelocityY * seconds, 45, arenaHeight - 45));
    }

    public TimedLightField WithSettings(EcologySettings settings, long tick) => new(seed, settings, tick,
        Enumerable.Range(0, Schedules.Length).Select(i => PositionAt(i, tick)).ToArray(), arenaWidth, arenaHeight);

    public LightPatch[] At(long tick) => Schedules.Select((s, i) => (Schedule: s, Source: i, Phase: (tick + s.PhaseTicks) % s.PeriodTicks))
        .Where(s => s.Phase < s.Schedule.OnTicks)
        .Select(s =>
        {
            var p = PositionAt(s.Source, tick);
            return new LightPatch(p.X, p.Y, s.Schedule.Radius,
                (s.Schedule.OnTicks - s.Phase) / (double)Bath.TicksPerSecond, s.Source);
        }).ToArray();
}

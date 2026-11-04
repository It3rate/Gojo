namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Serial experiment clock. The desk requests a bench, the bench binds
/// a result through that request, and the desk's existing passage executes it.</summary>
public sealed class SocketStudy
{
    private readonly TimingStands stands;
    private readonly Func<WasherProbe,byte> unknown;
    private readonly Func<int,WasherProbe,TimingRead> reference;
    private readonly int benchBeats, units, directions;
    public WasherProbeBank Rack { get; }
    public WasherProbeDesk Desk { get; }
    public TimingBench? Bench { get; private set; }
    public List<ProbeTrial> Trials { get; } = [];
    public List<TimingCheck> Checks { get; } = [];
    public bool Stopped { get; private set; }
    public int RequestedPair { get; private set; }

    public SocketStudy(TimingStands stands, int candidates, int sensors, Func<WasherProbe,byte> unknown,
        int directions = TimingBench.Both, bool early = false, int deskBeats = 18, int benchBeats = 48,
        int units = 48, bool protect = false, bool balanced = false, bool inspect = false,
        int? goal = null, Func<int,WasherProbe,TimingRead>? reference = null)
    {
        if ((candidates & ~stands.Family) != 0 || benchBeats < 0 || units < 0 || (uint)directions > TimingBench.Both)
            throw new ArgumentOutOfRangeException();
        this.stands = stands; this.unknown = unknown; this.reference = reference ?? stands.Run;
        this.benchBeats = benchBeats; this.units = units; this.directions = directions;
        Rack = stands.FreshRack(early);
        Desk = new(sensors,deskBeats,goal,protect,inspect,candidates,balanced,rack:Rack);
    }

    public bool Step()
    {
        if (Stopped) return false;
        if (Desk.Step(unknown) is {} trial) { Trials.Add(trial); return true; }
        if (Bench == null && Desk.RequestTiming() is {} request)
        {
            RequestedPair = request.Pair;
            Bench = new(request,reference,stands.MaximumUnits,benchBeats,units,directions);
            return true;
        }
        if (Bench?.Step() is {} check) { Checks.Add(check); return true; }
        Stopped = true;
        return false;
    }
}

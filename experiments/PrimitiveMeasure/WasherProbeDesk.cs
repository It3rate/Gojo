using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure;

public readonly record struct ProbeOffers(int Split, int Direct, int Wired, int Affordable, int Safe,
    int Fresh, int Open, bool Checking);

public readonly record struct ProbeTrial(int Row, int Before, int After, byte Sample, int BeatsLeft,
    ProbeOffers Offers);

/// <summary>The local desk holds one live bit per calibrated candidate and one
/// activation to spend. Its only connection to the unknown batch accepts a probe
/// word and returns the selected sensor's ODV support.</summary>
public sealed class WasherProbeDesk
{
    private readonly WasherProbeBank bank = WasherProbeBank.Calibrated;
    private readonly int surfaces;
    private readonly int sockets;
    private readonly bool protectStock, checkPromises, preferBalanced;
    private readonly int? goal;
    private readonly PortStructure owner = new();
    private readonly PassageConnections route, rearm;
    private readonly int[] guards = new int[WasherProbeBank.Count];
    private readonly ConnectionRow[] outgoing = new ConnectionRow[WasherProbeBank.Count],
        returning = new ConnectionRow[WasherProbeBank.Count];

    public int Live { get; private set; }
    public int Tried { get; private set; }
    public int BeatsLeft { get; private set; }
    public bool Answered => Live != 0 && (goal is int yes
        ? (Live & yes) == 0 || (Live & ~yes) == 0 : BitOperations.IsPow2((uint)Live));
    public byte[] RouteBytes => route.PackedRows(5);

    public WasherProbeDesk(int surfaces, int budget, int? goalYes = null, bool protectStock = false,
        bool checkPromises = false, int candidates = WasherProbeBank.Family, bool preferBalanced = false,
        int sockets = WasherProbeBank.AllSockets)
    {
        if ((uint)surfaces > 7 || budget < 0 || (candidates & ~WasherProbeBank.Family) != 0 ||
            (sockets & ~WasherProbeBank.AllSockets) != 0 ||
            (goalYes.HasValue && (goalYes.Value & ~WasherProbeBank.Family) != 0))
            throw new ArgumentException("Invalid desk wiring, candidate set or beat budget.");
        this.surfaces = surfaces;
        this.sockets = sockets;
        this.protectStock = protectStock;
        this.checkPromises = checkPromises;
        this.preferBalanced = preferBalanced;
        goal = goalYes;
        BeatsLeft = budget;
        Live = candidates;
        route = new(owner,PassageLaw.Controlled());
        rearm = new(owner,PassageLaw.Unconditional());
        int credit = owner.Publish(owner.Create(0,0,1,1));
        for (int i = 0; i < guards.Length; i++)
        {
            guards[i] = owner.Create(0,0,1,0);
            int gate = owner.Publish(guards[i]);
            int socket = owner.Publish(owner.Create(0,0,1,0));
            outgoing[i] = route.Connect(5,credit,gate,socket);
            returning[i] = rearm.Connect(5,socket,gate,credit);
        }
    }

    public ProbeOffers Offers()
    {
        int split = 0, direct = 0, wired = 0, affordable = 0, safe = 0;
        Span<int> balance = stackalloc int[WasherTestStand.Types / 2 + 1];
        balance.Clear();
        for (int i = 0; i < guards.Length; i++)
        {
            int bit = 1 << i;
            var probe = bank.Probe(i);
            int yes = Live & bank.Yes(i), no = Live & ~yes;
            if (yes != 0 && no != 0) split |= bit;
            if (preferBalanced)
                balance[Math.Min(BitOperations.PopCount((uint)yes),BitOperations.PopCount((uint)no))] |= bit;
            if (goal is int target && (yes == (Live & target) || no == (Live & target))) direct |= bit;
            if ((surfaces & (1 << probe.Surface)) != 0) wired |= bit & sockets;
            if (probe.Beats <= BeatsLeft) affordable |= bit;
            if (!protectStock || (Live & bank.DoubleRelease(i)) == 0) safe |= bit;
        }
        int fresh = ((1 << guards.Length) - 1) & ~Tried;
        int legal = fresh & wired & affordable & safe;
        int open = Answered ? 0 : legal & split;
        if ((open & direct) != 0) open &= direct;
        if (preferBalanced && open != 0)
            for (int tier = balance.Length - 1; tier > 0; tier--)
                if ((open & balance[tier]) != 0)
                {
                    open &= balance[tier];
                    break;
                }
        // A separately wired inspection pass can also challenge a unanimous
        // prediction. A question that splits the family cannot falsify all of it.
        bool checking = checkPromises && Live != 0 && open == 0 && (legal & ~split) != 0;
        if (checking) open = legal & ~split;
        return new(split,direct,wired,affordable,safe,fresh,open,checking);
    }

    public ProbeTrial? Step(Func<WasherProbe,byte> sample)
    {
        var offers = Offers();
        if (offers.Open == 0) return null;
        for (int i = 0; i < guards.Length; i++)
            owner.StoreObservation(guards[i],false,false,(uint)((offers.Open >> i) & 1));
        for (int i = 0; i < outgoing.Length; i++)
        {
            var row = outgoing[i];
            if (!route.TryStep(row,route.Observe(row),out byte after) || after != 6) continue;
            var probe = bank.Probe(i);
            int before = Live;
            Tried |= 1 << i;
            BeatsLeft -= probe.Beats;
            try
            {
                byte read = sample(probe);
                // 00 means unavailable, 11 admits both. Each is recorded as
                // received and neither can eliminate a candidate.
                Live &= read switch
                {
                    1 => ~bank.Yes(i), 2 => bank.Yes(i), 0 or 3 => WasherProbeBank.Family,
                    _ => throw new ArgumentException("Sensor returned an incompatible support.",nameof(sample))
                };
                return new(i,before,Live,read,BeatsLeft,offers);
            }
            finally
            {
                var home = returning[i];
                if (!rearm.TryStep(home,rearm.Observe(home),out byte restored) || (restored & 5) != 4)
                    throw new InvalidOperationException("Probe credit could not return.");
            }
        }
        throw new InvalidOperationException("Open socket did not receive the probe credit.");
    }
}

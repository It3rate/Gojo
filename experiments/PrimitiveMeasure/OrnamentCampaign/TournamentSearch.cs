namespace Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

// Host-side proposal stream. Addresses and truth masks are the heritable edits.
// Only OrnamentMachine executes a phenotype. Numeric randomness is never a pen law.
internal struct ProposalRandom(ulong state)
{
    private ulong state = state;

    public ulong Next()
    {
        ulong z = unchecked(state += 0x9E3779B97F4A7C15UL);
        z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
        z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
        return z ^ (z >> 31);
    }

    public int Below(int limit) => (int)(Next() % (uint)limit);
    public byte Field(int limit) => (byte)Below(limit);
    public static ulong Key(params int[] fields)
    {
        ulong hash = 14695981039346656037UL;
        foreach (var field in fields) hash = unchecked((hash ^ (uint)field) * 1099511628211UL);
        return hash;
    }
}

internal sealed record Proposal(OrnamentProgram Program, int? ParentA, int? ParentB, string Edit);

internal static class TournamentSearch
{
    public const int Demes = 4;
    private static readonly byte[] WriterPrivate = [0, 1, 4, 5, 6, 7];

    // 16 sensor masks x 16 ink masks x 16 shift masks, fixed ports and row order.
    // Reversing the enumerated rank per replicate only permutes these 4096 trials.
    public static OrnamentProgram Parameter(int candidate, int seed)
    {
        int rank = unchecked(candidate * 109 + seed) & 4095;
        byte gap = (byte)(rank & 15), ink = (byte)((rank >> 4) & 15), shift = (byte)((rank >> 8) & 15);
        return new([new(7, 8, gap)], [new(2, 6, ink, 0, 1), new(2, 6, ink, 1, 0),
            new(2, 5, shift, 1, 2), new(2, 5, (byte)(shift ^ 15), 1, 3)]);
    }

    public static OrnamentProgram Idle() => new([new(0, 0, 0)], [new(0, 0, 0, 0, 0)]);

    private static SensorRow Sensor(int index, ref ProposalRandom rng) =>
        new(rng.Field(16 + index), rng.Field(16 + index), rng.Field(16));

    private static byte WriterInput(int index, bool open, ref ProposalRandom rng)
    {
        if (!open) return rng.Field(8 + index);
        int address = rng.Below(WriterPrivate.Length + index);
        return address < WriterPrivate.Length ? WriterPrivate[address] : (byte)(8 + address - WriterPrivate.Length);
    }

    private static WriterRow Writer(int index, bool open, ref ProposalRandom rng) =>
        new(WriterInput(index, open, ref rng), WriterInput(index, open, ref rng),
            rng.Field(16), rng.Field(4), rng.Field(4));

    public static OrnamentProgram Random(bool open, ref ProposalRandom rng)
    {
        var sensor = new SensorRow[open ? 1 : 1 + rng.Below(4)];
        for (int i = 0; i < sensor.Length; i++) sensor[i] = open ? new(0, 0, 0) : Sensor(i, ref rng);
        var writer = new WriterRow[1 + rng.Below(4)];
        for (int i = 0; i < writer.Length; i++) writer[i] = Writer(i, open, ref rng);
        return new(sensor, writer, rng.Field(4));
    }

    // Swapping/deleting rows can invalidate a reference to a preceding output.
    // In that event reconnect the invalid input to false (local endpoint zero).
    private static OrnamentProgram Rebuild(List<SensorRow> sense, List<WriterRow> write, byte flags)
    {
        for (int i = 0; i < sense.Count; i++)
        {
            var r = sense[i];
            sense[i] = new(r.A < 16+i ? r.A : (byte)0, r.B < 16+i ? r.B : (byte)0, r.Truth);
        }
        for (int i = 0; i < write.Count; i++)
        {
            var r = write[i];
            write[i] = new(r.A < 8+i ? r.A : (byte)0, r.B < 8+i ? r.B : (byte)0, r.Truth, r.From, r.To);
        }
        return new(sense.ToArray(), write.ToArray(), flags);
    }

    private static Proposal Mutate(TrainingEntry parent, ref ProposalRandom rng)
    {
        var original = OrnamentProgram.Decode(Convert.FromHexString(parent.GenomeHex));
        var sense = original.Sensor.ToList();
        var write = original.Writer.ToList();
        byte flags = original.Flags;
        int s = rng.Below(sense.Count), w = rng.Below(write.Count);
        string edit;
        switch (rng.Below(8))
        {
            case 0:
                sense[s] = sense[s] with { Truth = (byte)(sense[s].Truth ^ (1 << rng.Below(4))) };
                edit = "flip-sensor-law"; break;
            case 1:
                write[w] = write[w] with { Truth = (byte)(write[w].Truth ^ (1 << rng.Below(4))) };
                edit = "flip-writer-law"; break;
            case 2:
                sense[s] = rng.Below(2) == 0 ? sense[s] with { A = rng.Field(16+s) } : sense[s] with { B = rng.Field(16+s) };
                edit = "rewire-sensor"; break;
            case 3:
                write[w] = rng.Below(2) == 0 ? write[w] with { A = rng.Field(8+w) } : write[w] with { B = rng.Field(8+w) };
                edit = "rewire-writer"; break;
            case 4:
                write[w] = rng.Below(2) == 0 ? write[w] with { From = rng.Field(4) } : write[w] with { To = rng.Field(4) };
                edit = "rebind-passage"; break;
            case 5:
                if (rng.Below(2) == 0)
                {
                    if (sense.Count == 1 || sense.Count < 4 && rng.Below(2) == 0) sense.Add(Sensor(sense.Count, ref rng));
                    else sense.RemoveAt(s);
                    edit = "resize-sensor";
                }
                else
                {
                    if (write.Count == 1 || write.Count < 4 && rng.Below(2) == 0) write.Add(Writer(write.Count, false, ref rng));
                    else write.RemoveAt(w);
                    edit = "resize-writer";
                }
                break;
            case 6:
                int other = rng.Below(write.Count);
                (write[w], write[other]) = (write[other], write[w]);
                edit = "exchange-passage-priority"; break;
            default:
                flags ^= (byte)(1 << rng.Below(2));
                edit = "toggle-delay-or-scan"; break;
        }
        return new(Rebuild(sense, write, flags), parent.Id, null, edit);
    }

    private static Proposal Mate(TrainingEntry first, TrainingEntry second, ref ProposalRandom rng)
    {
        var a = OrnamentProgram.Decode(Convert.FromHexString(first.GenomeHex));
        var b = OrnamentProgram.Decode(Convert.FromHexString(second.GenomeHex));
        if (rng.Below(2) == 0)
            return new(new(a.Sensor.ToArray(), b.Writer.ToArray(), rng.Below(2) == 0 ? a.Flags : b.Flags),
                first.Id, second.Id, "mate-owners");
        var rows = a.Writer.Take(rng.Below(a.Writer.Count + 1)).Concat(b.Writer).Take(4).ToList();
        return new(Rebuild(a.Sensor.ToList(), rows, a.Flags), first.Id, second.Id, "mate-rows");
    }

    public static Proposal Create(string method, int id, int generation, int position, int seed,
        TrainingEntry[][] parents, ref ProposalRandom rng)
    {
        if (method == "parameter") return new(Parameter(id, seed), null, null, "enumerate-fixed-topology");
        if (method == "open" || method == "random" || generation == 0)
            return new(Random(method == "open", ref rng), null, null, method == "open" ? "sample-unwired" : "sample-wiring");
        int deme = position % Demes;
        var local = parents[deme];
        int slot = position / Demes;
        // Four niches compete in each independent replicate. Immigrants
        // occupy every eighth slot and crossovers every fourth (offset one).
        if (slot % 8 == 0) return new(Random(false, ref rng), null, null, "immigrant");
        var parent = local[rng.Below(local.Length)];
        if (slot % 4 == 1)
        {
            int neighbour = (deme + 1 + rng.Below(Demes - 1)) % Demes;
            var peers = parents[neighbour];
            return Mate(parent, peers[rng.Below(peers.Length)], ref rng);
        }
        return Mutate(parent, ref rng);
    }
}

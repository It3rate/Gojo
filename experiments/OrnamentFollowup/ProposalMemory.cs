using Gojo.Experiments.PrimitiveMeasure;
using Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

namespace Gojo.Experiments.OrnamentFollowup;

public sealed record MutationEvidence(string Family, int Attempts, int Improved, int Tickets);

/// <summary>Strictly improved child utility in the same parent's own bank
/// counts as one success. All evaluated single-parent mutations, including
/// noop, neutral and harmful children, contribute to the denominator.</summary>
public sealed class ProposalMemory
{
    public static readonly string[] Families = ["flip-sensor-law", "flip-writer-law", "rewire-sensor",
        "rewire-writer", "rebind-passage", "resize-sensor", "resize-writer",
        "exchange-passage-priority", "toggle-delay-or-scan"];
    private readonly int[] attempts = new int[9], improved = new int[9];

    public void Observe(int family, long parentUtility, long childUtility)
    {
        attempts[family]++;
        if (childUtility > parentUtility) improved[family]++;
    }

    public MutationEvidence[] Evidence() => Enumerable.Range(0, Families.Length).Select(i =>
        new MutationEvidence(Families[i], attempts[i], improved[i],
            1 + (int)(15L * improved[i] / Math.Max(1, attempts[i])))).ToArray();

    public static PackedProposer Pack(IReadOnlyList<MutationEvidence> observations, string arm)
    {
        if (observations.Count != Families.Length || observations.Where((e,i) => e.Family != Families[i] ||
                e.Tickets is < 1 or > 16 || e.Improved < 0 || e.Improved > e.Attempts).Any())
            throw new ArgumentException("Invalid family evidence.");
        var rows = new EndpointRows();
        for (var i = 0; i < Families.Length; i++)
        {
            int tickets = arm switch
            {
                "uniform" => 1, "learned" => observations[i].Tickets,
                "permuted" => observations[(i + 1) % Families.Length].Tickets, _ => throw new ArgumentOutOfRangeException(nameof(arm))
            };
            for (var ticket = 0; ticket < tickets; ticket++) rows.Append(4, i);
        }
        return new(rows.Count(1, 4), Convert.ToHexString(rows.PackedBytes(1, 4)));
    }
}

public sealed record PackedProposer(int Count, string Hex)
{
    public byte[] Decode()
    {
        var bytes = Convert.FromHexString(Hex);
        if (Count is < 9 or > 144 || bytes.Length != (Count + 1) / 2 ||
            (Count % 2 == 1 && (bytes[^1] >> 4) != 0)) throw new InvalidDataException("Invalid proposer packet.");
        var rows = Enumerable.Range(0, Count).Select(i => (byte)((bytes[i/2] >> (4*(i%2))) & 15)).ToArray();
        if (rows.Any(x=>x >= 9) || rows.Distinct().Count()!=9) throw new InvalidDataException("Invalid proposal endpoint.");
        return rows;
    }
}

internal static class FollowupEdits
{
    // Nine primary single-parent edit families get independently selectable
    // ticket positions. Apply one draw without generating or scoring rivals.
    public static OrnamentProgram Mutate(OrnamentProgram parent, int family, ref ProposalRandom rng)
    {
        var sensor = parent.Sensor.ToList();
        var writer = parent.Writer.ToList();
        byte flags = parent.Flags;
        int s = rng.Below(sensor.Count), w = rng.Below(writer.Count);
        switch (family)
        {
            case 0: sensor[s] = sensor[s] with { Truth = (byte)(sensor[s].Truth ^ 1 << rng.Below(4)) }; break;
            case 1: writer[w] = writer[w] with { Truth = (byte)(writer[w].Truth ^ 1 << rng.Below(4)) }; break;
            case 2:
                sensor[s] = rng.Below(2)==0 ? sensor[s] with { A = rng.Field(16+s) } : sensor[s] with { B = rng.Field(16+s) };
                break;
            case 3:
                writer[w] = rng.Below(2)==0 ? writer[w] with { A = rng.Field(8+w) } : writer[w] with { B = rng.Field(8+w) };
                break;
            case 4:
                writer[w] = rng.Below(2)==0 ? writer[w] with { From = rng.Field(4) } : writer[w] with { To = rng.Field(4) };
                break;
            case 5:
                if (sensor.Count == 1 || sensor.Count < 4 && rng.Below(2)==0)
                    sensor.Add(new(rng.Field(16+sensor.Count),rng.Field(16+sensor.Count),rng.Field(16)));
                else sensor.RemoveAt(s);
                break;
            case 6:
                if (writer.Count == 1 || writer.Count < 4 && rng.Below(2)==0)
                    writer.Add(new(rng.Field(8+writer.Count),rng.Field(8+writer.Count),rng.Field(16),rng.Field(4),rng.Field(4)));
                else writer.RemoveAt(w);
                break;
            case 7:
                int b = rng.Below(writer.Count);
                (writer[w],writer[b]) = (writer[b],writer[w]);
                break;
            case 8: flags ^= (byte)(1 << rng.Below(2)); break;
            default: throw new ArgumentOutOfRangeException(nameof(family));
        }
        for (var i=0;i<sensor.Count;i++)
            sensor[i]=sensor[i] with { A = sensor[i].A < 16+i ? sensor[i].A : (byte)0, B = sensor[i].B < 16+i ? sensor[i].B : (byte)0 };
        for (var i=0;i<writer.Count;i++)
            writer[i]=writer[i] with { A = writer[i].A < 8+i ? writer[i].A : (byte)0, B = writer[i].B < 8+i ? writer[i].B : (byte)0 };
        return new(sensor.ToArray(), writer.ToArray(), flags);
    }
}

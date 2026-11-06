using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure.OrnamentCampaign;

// These are small authored seeds with two exposed axis profiles, not labels
// that either strip controller can read. The private shape has seven rows.
internal sealed record FabricSeed(string Name, byte X, byte Y, byte[] Rows);
public readonly record struct FabricRead(ulong Ink, int Stock, int Unresolved, int Refused);

public static class FabricTransfer
{
    private static readonly FabricSeed[] Seeds =
    [
        new("h", 0x22, 0x08, [0x22,0x22,0x22,0x3e,0x22,0x22,0x22]),
        new("e", 0x02, 0x49, [0x3e,0x02,0x02,0x1e,0x02,0x02,0x3e]),
        new("t", 0x08, 0x01, [0x3e,0x08,0x08,0x08,0x08,0x08,0x08]),
        new("u", 0x22, 0x40, [0x22,0x22,0x22,0x22,0x22,0x22,0x3e]),
        new("ladder", 0x22, 0x2a, [0x22,0x3e,0x22,0x3e,0x22,0x3e,0x22]),
        new("cross", 0x08, 0x08, [0x08,0x08,0x08,0x7f,0x08,0x08,0x08])
    ];
    private static readonly byte[] Laws = [8, 14, 6]; // AND, OR, XOR
    private static readonly string[] Conditions = ["live", "hold-h", "hold-v", "muted-h", "cut-h", "unknown-v", "seed-profiles"];

    // Each 4 x 4 table is compiled from the existing bit relation's admitted
    // tuples. The table indices are support masks, including absent and unknown.
    private static readonly byte[,] Ports = CompilePorts();
    private static readonly PassageLaw Place = PassageLaw.Controlled();
    private static byte[,] CompilePorts()
    {
        var tables = new byte[16, 16];
        for (int law = 0; law < 16; law++)
        {
            var relation = BitRelation.FromTuples(3, from a in Enumerable.Range(0, 2)
                from b in Enumerable.Range(0, 2) select new[] { (byte)a, (byte)b, (byte)((law >> (a + 2*b)) & 1) });
            for (byte a = 0; a < 4; a++)
            for (byte b = 0; b < 4; b++)
                tables[law, a + 4*b] = relation.Predict([a,b]);
        }
        return tables;
    }

    public static void WritePlan(string dir)
    {
        File.WriteAllLines(Path.Combine(dir, "fabric-predictions.tsv"),
        [
            "id\tclaim\tthreshold",
            "F1\tAll protected marks survive and only funded new marks appear\tEvery run; any counterexample contrary",
            "F2\tA disconnected required horizontal port permits no new material\tEvery cut-h run retains original seed",
            "F3\tAn unknown vertical port can still permit OR marks from known horizontal participation\tEvolve unknown-v OR adds ink in at least 4/6 seed families",
            "F4\tReplaying the initial horizontal profile changes learned fabric output\tIn at least 4/6 seed families at least 20% of paired evolve live/hold-h final patterns differ",
            "F5\tReplaying the initial vertical profile changes learned fabric output\tSame threshold as F4 for live/hold-v",
            "F6\tTransferred controllers improve bilateral symmetry over both original profiles\tMean reduction >=1 mismatch in >=4/6 seed families on evolve live/seed-profiles; reverse >=1 in >=4 contrary, else inconclusive",
            "F7\tTwo different writer laws make visibly different results from identical phase streams\tOR versus XOR differs in at least 20% of paired evolve live patterns in >=4/6 seed families",
            "F8\tIncreasing stock cannot retract an earlier placement\tSix-slot output is a subset of matching 24-slot output in every run"
        ]);
        using var f = File.CreateText(Path.Combine(dir, "fabric.csv"));
        f.WriteLine("seed,x_profile,y_profile,protected_rows,goal_pair,truth_mask,phase,stock,condition");
        foreach (var seed in Seeds)
        foreach (var law in Laws)
        foreach (int phase in new[] { 0, 1 })
        foreach (int stock in new[] { 6, 24 })
        foreach (var condition in Conditions)
            f.WriteLine(FormattableString.Invariant($"{seed.Name},{seed.X},{seed.Y},{Convert.ToHexString(seed.Rows)},balance+infill,{law},{phase},{stock},{condition}"));
    }

    public static FabricRead Draw(ulong protectedInk, byte[] x, byte[] y, int law, int phase, int stock)
    {
        // Ink costs are in one explicit unary basis. Bits leaving each axis
        // are observations; placing a new mark consumes one finite stock site.
        ulong ink = protectedInk;
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)stock, 49U);
        if (x.Length != 7 || y.Length != 7 || x.Any(s => s > 3) || y.Any(s => s > 3) ||
            (uint)law > 15 || (uint)phase >= 49 || (protectedInk >> 49) != 0)
            throw new ArgumentException("Expected a 7 by 7 fabric with local Boolean supports and a four-bit law.");
        ulong fuelSlots = (1UL << stock) - 1;
        int unresolved = 0, refused = 0;
        for (int i = 0; i < 49; i++)
        {
            int site = (i + phase) % 49;
            ulong mark = 1UL << site;
            if ((protectedInk & mark) != 0) continue;
            byte support = Ports[law, x[site % 7] + 4*y[site / 7]];
            if (!ReadSupport.TrySingle(support, out var signal)) { unresolved++; continue; }
            var before = PassageLaw.State(fuelSlots != 0, signal == 1, false);
            var result = Place.Predict((byte)(1 << before));
            if (!ReadSupport.TrySingle(result, out var after)) throw new InvalidOperationException("Ambiguous material placement.");
            if ((after & 4) != 0) { ink |= mark; fuelSlots &= fuelSlots - 1; }
            else if (signal == 1) refused++;
        }
        return new(ink, BitOperations.PopCount(fuelSlots), unresolved, refused);
    }

    public static void Run(string folder, FrozenWinner[] winners)
    {
        Directory.CreateDirectory(folder);
        using var f = File.CreateText(Path.Combine(folder, "fabric.csv"));
        f.WriteLine("method,rep,h_genome,v_genome,seed,law,phase,stock,condition,initial,final,stock_left,unresolved,refused,mirror_mismatches,empty_quadrants");
        foreach (var method in winners.GroupBy(w => w.Method).OrderBy(g => g.Key))
        {
            var horizontal = method.Where(w => w.Goal == "balance").OrderBy(w => w.Replicate).ToArray();
            var vertical = method.Where(w => w.Goal == "infill").OrderBy(w => w.Replicate).ToArray();
            if (horizontal.Length == 0 || horizontal.Length != vertical.Length) throw new InvalidDataException("Missing paired league.");
            for (int rep = 0; rep < horizontal.Length; rep++)
            {
                if (horizontal[rep].Replicate != vertical[rep].Replicate || horizontal[rep].Seed != vertical[rep].Seed)
                    throw new InvalidDataException("Crossed programs must come from matched replicates.");
                var h = OrnamentProgram.Decode(Convert.FromHexString(horizontal[rep].GenomeHex)).Compile();
                var v = OrnamentProgram.Decode(Convert.FromHexString(vertical[rep].GenomeHex)).Compile();
                foreach (var seed in Seeds)
                foreach (int phase in new[] { 0, 1 })
                {
                    var horizontalInk = h.Run(new RailInput(7, seed.X, seed.X, 0, 2, 1, 3, 6, 21, Start:phase)).Ink;
                    var verticalInk = v.Run(new RailInput(7, seed.Y, seed.Y, 0, 2, 2, 3, 6, 21, Start:phase)).Ink;
                    ulong protectedInk = 0;
                    for (int i = 0; i < 7; i++) protectedInk |= (ulong)seed.Rows[i] << (7*i);
                    foreach (var condition in Conditions)
                    {
                        var x = Expose(condition is "hold-h" or "seed-profiles" ? seed.X : horizontalInk);
                        var y = Expose(condition is "hold-v" or "seed-profiles" ? seed.Y : verticalInk);
                        if (condition == "muted-h") Array.Fill(x, (byte)1);
                        if (condition == "cut-h") Array.Fill(x, (byte)0);
                        if (condition == "unknown-v") Array.Fill(y, (byte)3);
                        foreach (var law in Laws)
                        foreach (int stock in new[] { 6, 24 })
                        {
                            var read = Draw(protectedInk, x, y, law, phase, stock);
                            if ((read.Ink & protectedInk) != protectedInk ||
                                BitOperations.PopCount(read.Ink ^ protectedInk) + read.Stock != stock)
                                throw new InvalidDataException("The writer violated a frozen seed or spent stock twice.");
                            var metric = Inspect(read.Ink);
                            f.WriteLine(FormattableString.Invariant($"{method.Key},{rep},{horizontal[rep].GenomeHex},{vertical[rep].GenomeHex},{seed.Name},{law},{phase},{stock},{condition},{protectedInk:X13},{read.Ink:X13},{read.Stock},{read.Unresolved},{read.Refused},{metric.Mirror},{metric.Empty}"));
                        }
                    }
                }
            }
        }
    }

    private static byte[] Expose(ulong mask) => Enumerable.Range(0, 7).Select(i => (byte)(1 << (int)((mask >> i) & 1))).ToArray();

    // Conventional independent measurements of the finished artwork only.
    private static (int Mirror, int Empty) Inspect(ulong ink)
    {
        int mirror = 0, quadrants = 0;
        for (int y = 0; y < 7; y++)
        for (int x = 0; x < 7; x++)
        {
            bool marked = (ink & (1UL << (7*y+x))) != 0;
            if (x < 3 && marked != ((ink & (1UL << (7*y+6-x))) != 0)) mirror++;
            if (marked && x != 3 && y != 3) quadrants |= 1 << ((x > 3 ? 1 : 0) + (y > 3 ? 2 : 0));
        }
        return (mirror, 4 - BitOperations.PopCount((uint)quadrants));
    }
}

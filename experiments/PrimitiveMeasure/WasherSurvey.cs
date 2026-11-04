using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure;

public static class WasherSurvey
{
    public const int Window = 6;
    public static int Mask(int beats) => beats is >= 0 and <= Window ? (1 << beats) - 1 :
        throw new ArgumentOutOfRangeException(nameof(beats));

    private static int First(int word) => word & -word;

    /// <summary>Independent bit read over a known schedule, without constructing
    /// or stepping a counter. Each link word uses one availability bit per beat.</summary>
    public static WasherHistory Predict(byte law, int forward, int back, int beats, int initialStock = 2)
    {
        if (initialStock is < 0 or > 2) throw new ArgumentOutOfRangeException(nameof(initialStock));
        int mask = Mask(beats);
        int available = initialStock == 0 ? 0 : forward & mask;
        int first = First(available);
        int eligible = law switch
        {
            WasherCounter.RememberDelivery => available,
            WasherCounter.ServeEveryRequest => initialStock == 1 ? first : first | First(available ^ first),
            _ => throw new ArgumentOutOfRangeException(nameof(law))
        };
        int receipt = First(eligible & back);
        int throughReceipt = receipt == 0 ? mask : (receipt << 1) - 1;
        int drops = law == WasherCounter.RememberDelivery ? first : eligible & throughReceipt;
        return new((byte)drops,(byte)receipt);
    }

    /// <summary>Only heard receipts in visible slots and an optional final
    /// tray-lamp read cross this boundary. Keep distinct whole event histories.</summary>
    public static WasherHistory[] Possible(byte law, int beats, int heard, int visible, bool? trayLamp = null)
    {
        int mask = Mask(beats);
        var histories = new HashSet<WasherHistory>();
        for (int forward = 0; forward <= mask; forward++)
            for (int back = 0; back <= mask; back++)
            {
                var h = Predict(law,forward,back,beats);
                if (((h.Heard ^ heard) & visible & mask) != 0) continue;
                if (trayLamp.HasValue && (h.Drops != 0) != trayLamp.Value) continue;
                histories.Add(h);
            }
        return histories.OrderBy(h => h.Drops).ThenBy(h => h.Heard).ToArray();
    }

    public static WasherBeat[] Run(WasherCounter counter, int forward, int back, int beats)
    {
        _ = Mask(beats);
        var trace = new WasherBeat[beats];
        for (int i = 0; i < beats; i++)
            trace[i] = counter.Step((forward & (1 << i)) != 0,(back & (1 << i)) != 0);
        return trace;
    }

    public static WasherHistory Observe(WasherBeat[] trace)
    {
        _ = Mask(trace.Length);
        int drops = 0, heard = 0;
        for (int i = 0; i < trace.Length; i++)
        {
            if (trace[i].Dropped) drops |= 1 << i;
            if (trace[i].Heard) heard |= 1 << i;
        }
        return new((byte)drops,(byte)heard);
    }

    public static void Trials(TextWriter output, bool rows)
    {
        if (rows) output.WriteLine("admission_law,forward_open,return_open,predicted_drops,predicted_heard,observed_drops,observed_heard,final_stock_bits,final_tray_bits");
        foreach (byte law in new[]{WasherCounter.RememberDelivery, WasherCounter.ServeEveryRequest})
        {
            int confirmed = 0, duplicate = 0, pendingWithParts = 0;
            for (int f = 0; f <= Mask(Window); f++)
                for (int r = 0; r <= Mask(Window); r++)
                {
                    var trace = Run(new(law),f,r,Window);
                    var actual = Observe(trace);
                    var predicted = Predict(law,f,r,Window);
                    if (actual != predicted || trace.Any(b =>
                        BitOperations.PopCount((uint)b.StockBits) + BitOperations.PopCount((uint)b.TrayBits) != 2))
                        throw new InvalidOperationException($"Witness mismatch for law {law}, forward {f}, return {r}.");
                    if (actual.Heard != 0) confirmed++;
                    if (actual.Parts == 2) duplicate++;
                    if (actual.Heard == 0 && actual.Parts > 0) pendingWithParts++;
                    if (rows) output.WriteLine($"{law},{f},{r},{predicted.Drops},{predicted.Heard},{actual.Drops},{actual.Heard},{trace[^1].StockBits},{trace[^1].TrayBits}");
                }
            if (!rows) output.WriteLine($"Law {law:X1}. 4096 schedules, {confirmed} confirmed, {duplicate} double deliveries, {pendingWithParts} unconfirmed with washers.");
        }
    }
}

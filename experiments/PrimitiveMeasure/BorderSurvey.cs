namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>After-the-fact audit. Its observations never feed the score or
/// the coverage guard. Detailed output is reproducible CSV for each sweep.</summary>
public static class BorderSurvey
{
    private static int ReturnClass(ReachBalance end)
    {
        int x = end.SignSupport(0), y = end.SignSupport(1);
        if (x == 2 && y == 2) return 0;
        return (x & y & 2) == 0 ? 1 : 2;
    }

    public static void Scores(TextWriter output, bool details)
    {
        int[,] classes = new int[3,3];
        int grouped = 0, maxPeriod = 0, count = 0;
        if (details) output.WriteLine("moves_hex,gates_hex,distinct_reaches,period,replayed_x_support,replayed_y_support,calibrated_x_support,calibrated_y_support,return_class,equal_return_class");
        byte[] directions = [BorderStudy.Right,BorderStudy.Up,BorderStudy.Left,BorderStudy.Down];
        for (int word = 0; word < 256; word++)
            for (int gates = 0; gates < 16; gates++)
                for (int distinct = 0; distinct < 2; distinct++)
                {
                    var cells = new byte[4];
                    for (int i = 0; i < 4; i++) cells[i] = (byte)(directions[(word >> (2*i)) & 3] | (distinct*i << 4));
                    var score = new BorderScore(cells[..2], cells[2..], (byte)(gates & 3), (byte)(gates >> 2));
                    var proof = score.Predict();
                    var actual = new ReachBalance();
                    BorderPhase phase = default;
                    var beats = new List<BorderEmission[]>();
                    do
                    {
                        var beat = score.Step(ref phase);
                        foreach (var emission in beat) actual.Include(emission.Move);
                        beats.Add(beat);
                        if (beats.Count > 8) throw new InvalidOperationException("Two-by-two heads exceeded their joint state space.");
                    } while (phase != default);
                    if (proof.Beats != beats.Count) throw new InvalidOperationException("Wrong first return.");
                    for (int axis = 0; axis < 2; axis++)
                        for (int r = 0; r < 16; r++)
                            if (actual.Uses(axis,r) != proof.End.Uses(axis,r)) throw new InvalidOperationException("Wrong displacement.");
                    for (int i = beats.Count-1; i >= 0; i--)
                    {
                        var expected = beats[i].Reverse().Select(e => e with { Move = e.Move.Backward() });
                        if (!score.Step(ref phase,true).SequenceEqual(expected)) throw new InvalidOperationException("Inverse lost a firing.");
                    }
                    if (phase != default) throw new InvalidOperationException("Inverse lost a head.");
                    byte x = CalibratedSigns(beats,0), y = CalibratedSigns(beats,1);
                    if (x != proof.End.SignSupport(0) || y != proof.End.SignSupport(1))
                        throw new InvalidOperationException("Finite positive calibrations disagree with the predicted signs.");
                    int observed = ReturnClass(actual), equal = ReturnClass(actual.AssumingEqualReaches());
                    classes[observed,equal]++;
                    if (beats.Any(b => b.Length == 2)) grouped++;
                    maxPeriod = Math.Max(maxPeriod,beats.Count);
                    count++;
                    if (details) output.WriteLine($"{word:X2},{gates:X1},{distinct},{proof.Beats},{actual.SignSupport(0)},{actual.SignSupport(1)},{x},{y},{observed},{equal}");
                }
        if (details) return;
        output.WriteLine($"{count} scores; {grouped} contain a grouped beat; maximum first return {maxPeriod} beats.");
        output.WriteLine("rows=actual proof, columns=equal-reach premise (return, drift, unresolved)");
        for (int i = 0; i < 3; i++) output.WriteLine($"{classes[i,0]},{classes[i,1]},{classes[i,2]}");
    }

    // Independent numerical apparatus for the survey alphabet R,U,L,D.
    // Each used reach may take any integer length 1..8. Distribute the finite
    // choices over the observed firings; none is available to the proof.
    private static byte CalibratedSigns(List<BorderEmission[]> beats, int axis)
    {
        var factors = new int[4];
        foreach (var beat in beats)
            foreach (var e in beat)
            {
                int cell = e.Move.Bits;
                if ((cell & 8) == axis*8) factors[cell >> 4] += (cell & 7) == 1 ? 1 : -1;
            }
        var totals = new HashSet<int>{0};
        foreach (int factor in factors.Where(f=>f!=0))
        {
            var expanded = new HashSet<int>();
            foreach (int total in totals)
                for (int length=1; length<=8; length++) expanded.Add(total+factor*length);
            totals = expanded;
        }
        return (byte)totals.Aggregate(0,(support,t)=>support | (1 << (Math.Sign(t)+1)));
    }

    public static void Windows(TextWriter output, bool details)
    {
        var family = BorderStudy.Family.Select(h => h.Word).Distinct().ToArray();
        var counts = new int[5,4];
        if (details) output.WriteLine("ink_hex,visible_hex,raw_completions,compatible_words,within_family,actual_admitted");
        for (int n = 0; n < 625; n++)
        {
            int digits = n, word = 0;
            for (int i = 0; i < 4; i++) { word |= digits % 5 << (4*i); digits /= 5; }
            for (int mask = 0; mask < 16; mask++)
            {
                var evidence = BorderEvidence.Observe((ushort)word, mask);
                var candidates = family.Where(evidence.Admits).ToArray();
                bool retained = candidates.Contains((ushort)word);
                bool member = family.Contains((ushort)word);
                if (retained != member || !evidence.Admits((ushort)word)) throw new InvalidOperationException("Evidence lost its source.");
                int hidden = 4-System.Numerics.BitOperations.PopCount((uint)mask);
                int column = candidates.Length switch { 0 => 0, 1 when retained => 1, 1 => 2, _ => 3 };
                counts[hidden,column]++;
                if (details) output.WriteLine($"{word:X4},{mask:X1},{evidence.CompletionCount()},{candidates.Length},{member},{retained}");
            }
        }
        if (details) return;
        output.WriteLine($"{BorderStudy.Family.Count} transforms, {family.Length} distinct words; 10000 observation windows.");
        output.WriteLine("hidden,no_match,unique_correct,unique_wrong,multiple");
        for (int h = 0; h <= 4; h++) output.WriteLine($"{h},{counts[h,0]},{counts[h,1]},{counts[h,2]},{counts[h,3]}");
    }
}

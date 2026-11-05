using System.Globalization;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Offline conformance and cost observations over a declared finite
/// domain. These records do not drive the owners, repair their structures or
/// rank possible generators. The independent oracle uses the supplied sites.</summary>
public static class GuaranteeSurvey
{
    public static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        var cases = 0;
        using (var output = File.CreateText(Path.Combine(folder, "finite-routes.csv")))
        {
            output.WriteLine("layout,sites,steps,case,continuous,closed,inspections,rows_read,transport_bytes,restored,word_changes_preserve_topology,unpack_preserves_topology");
            for (var layout = 0; layout < 3; layout++)
            for (var sites = 1; sites <= 3; sites++)
            for (var steps = 1; steps <= 3; steps++)
            {
                var fields = layout == 0 ? 2 * steps : layout == 1 ? steps + 1 : steps;
                var possibilities = (int)Math.Pow(sites, fields);
                for (var example = 0; example < possibilities; example++)
                {
                    var digits = Digits(example, sites, fields);
                    var pairs = layout == 0
                        ? Enumerable.Range(0, steps).Select(i => new[] { digits[2 * i], digits[2 * i + 1] }).ToArray()
                        : Enumerable.Range(0, steps).Select(i => new[] { digits[i], digits[(i + 1) % digits.Length] }).ToArray();
                    var continuous = Enumerable.Range(1, steps - 1).All(i => pairs[i - 1][1] == pairs[i][0]);
                    var closed = continuous && pairs[^1][1] == pairs[0][0];
                    var words = Enumerable.Range(0, steps).Select(i => (byte)((example + i) % 8)).ToArray();
                    var route = layout switch
                    {
                        0 => StructuralRoute.Pairs(sites, pairs, 2, 0, words),
                        1 => StructuralRoute.Chain(sites, digits, 2, 0, words),
                        _ => StructuralRoute.Cycle(sites, digits, 2, 0, words)
                    };
                    var transport = route.Encode();
                    var restored = StructuralRoute.Decode(transport);
                    Require(transport.SequenceEqual(restored.Encode()), "Transport changed the structural artifact.");
                    Require(restored.ContinuitySupport == 3 && restored.ClosureSupport == 3, "A restored observation claimed old proof.");
                    restored.Inspect(0);
                    Require(restored.ContinuitySupport == 3, "Zero budget performed an inspection.");
                    for (var beat = 0; beat < steps; beat++)
                    {
                        restored.Inspect(1);
                        Require(Admits(restored.ContinuitySupport, continuous), "A partial inspection excluded the actual continuity.");
                        Require(Admits(restored.ClosureSupport, closed), "A partial inspection excluded the actual closure.");
                    }
                    Require(restored.ContinuitySupport == (continuous ? 2 : 1), "Completed continuity differs from the supplied endpoint oracle.");
                    Require(restored.ClosureSupport == (closed ? 2 : 1), "Completed closure differs from the supplied endpoint oracle.");
                    Require(restored.ReadAll() == new StructuralRouteRead(continuous, closed, steps), "Full observer differs from the independent endpoint oracle.");
                    var before = restored.TopologyBytes();
                    var proof = (restored.ContinuitySupport, restored.ClosureSupport);
                    for (var edge = 0; edge < steps; edge++) restored.SetWord(edge, (byte)(words[edge] ^ 7));
                    Require(before.SequenceEqual(restored.TopologyBytes()), "Owned ODV changes altered topology.");
                    Require(proof == (restored.ContinuitySupport, restored.ClosureSupport), "An unrelated observation invalidated topology proof.");
                    var inspections = restored.Inspections;
                    var reads = restored.RowsRead;
                    restored.Unpack();
                    Require(restored.ReadAll() == new StructuralRouteRead(continuous, closed, steps), "Exact expansion changed endpoint meaning.");
                    for (var edge = 0; edge < steps; edge++)
                        Require(pairs[edge].SequenceEqual(restored.Edge(edge)), "Expansion changed occurrence correspondence.");
                    output.WriteLine(Csv(layout, sites, steps, example, continuous, closed, inspections, reads,
                        transport.Length, true, true, true));
                    cases++;
                }
            }
        }

        using (var output = File.CreateText(Path.Combine(folder, "inspection-costs.csv")))
        {
            output.WriteLine("steps,layout,topology_bits,word_bits,payload_bytes,transport_bytes,construction_rows,inspection_units,inspection_rows_read,closed");
            foreach (var steps in new[] { 4, 16, 256, 16384 })
            {
                var vertices = Enumerable.Range(0, steps).ToArray();
                var pairs = vertices.Select(i => new[] { i, (i + 1) % steps }).ToArray();
                var routes = new[] { StructuralRoute.Pairs(steps, pairs), StructuralRoute.Chain(steps, [.. vertices, 0]), StructuralRoute.Cycle(steps, vertices) };
                foreach (var route in routes)
                {
                    route.Inspect(steps);
                    Require(route.ClosureSupport == 2, "A cost fixture failed its closed read.");
                    output.WriteLine(Csv(steps, route.Layout, route.TopologyBits, route.ValueBits,
                        route.TopologyBytes().Length + route.WordBytes().Length, route.Encode().Length,
                        route.RowCount, route.Inspections, route.RowsRead, true));
                }
            }
        }

        var attempts = 0;
        using (var output = File.CreateText(Path.Combine(folder, "shared-variation.csv")))
        {
            output.WriteLine("opposed,shared,pinned,initially_hidden,push,hidden,moved,changed_uses,waiting,requests,actual_in_prediction,source_unchanged");
            foreach (var opposed in new[] { false, true })
            foreach (var shared in new[] { false, true })
            foreach (var pinned in new[] { false, true })
            foreach (var hidden in new[] { false, true })
            {
                var run = opposed ? SharedVariation.OpposedReads(shared, pinned) : SharedVariation.Reaches(shared, pinned: pinned);
                var original = run.SourceBytes();
                for (var beat = 0; beat < 2; beat++)
                {
                    var concealed = hidden && beat == 0;
                    var predicted = run.Predict(concealed);
                    var step = run.Push(concealed);
                    var count = (run.Adjustment.ReservePresent ? 1 : 0) + (run.Adjustment.Waiting ? 1 : 0)
                        + run.Adjustment.Tips().Count(t => t == 2);
                    var fits = step.ActualScopeSupport != 0 && (step.ActualScopeSupport & ~predicted.AfterSupport) == 0;
                    var unchanged = original.SequenceEqual(run.SourceBytes());
                    Require(count == 1 && fits && unchanged, "Variation lost a request, source or supported outcome.");
                    output.WriteLine(Csv(opposed, shared, pinned, hidden, beat + 1, concealed, step.Moved,
                        string.Join(";", run.ChangedUses()), run.Adjustment.Waiting, count, fits, unchanged));
                    attempts++;
                }
            }
        }

        var pulses = 0;
        using (var output = File.CreateText(Path.Combine(folder, "gate-closure.csv")))
        {
            output.WriteLine("steps,controlled_edge,initial_observation,pulse,current_observation,site,moved,committed,missing,resolved,closed,activations,source_unchanged");
            for (var steps = 2; steps <= 6; steps++)
            for (var controlled = 0; controlled < steps; controlled++)
            foreach (byte support in new byte[] { 0, 1, 2, 3 })
            {
                var run = new ClosurePassage(steps, controlled);
                var original = run.SourceBytes();
                run.ObserveControl(support);
                for (var beat = 0; beat < 2 * steps + 3; beat++)
                {
                    if (beat == steps + 2) run.ObserveControl(2);
                    var step = run.Pulse();
                    var unchanged = original.SequenceEqual(run.SourceBytes());
                    Require(unchanged && run.Closed && run.Walk.OccupiedStateCount == 1, "A gate changed topology or lost an activation.");
                    output.WriteLine(Csv(steps, controlled, support, beat + 1, run.ControlSupport,
                        run.Walk.ActiveFamilySite, step.Moved, step.Committed, step.Prediction.Missing,
                        step.Prediction.Resolved, run.Closed, run.Walk.OccupiedStateCount, unchanged));
                    pulses++;
                }
            }
        }
        var summary = $"{cases} endpoint cases: all pair, vertex-chain and positional-cycle encodings with 1..3 sites and 1..3 occurrences.\n" +
            "Each case checks finite-budget soundness, completed truth, independent transport, all-owned ODV observation changes and exact expansion into pairs.\n" +
            "12 cost cases: 4, 16, 256 and 16384 occurrences under three layouts. Construction/load validation is paid separately; no timings or general complexity proof are claimed.\n" +
            $"{attempts} shared/independent and opposed-read adjustment attempts, including pinned and concealed response controls. Every attempt retains exactly one request.\n" +
            $"{pulses} gate pulses across 2..6-site cycles, every gate position and four initial observations. Control is revealed partway through each run without supplying another activation.\n" +
            "No case changes its retained topology through word updates or gate observations. No drawing geometry or generator label enters the readers.\n" +
            "Exhaustiveness applies only to the declared small endpoint domain. Geometry, all future edits, statistical confidence, generator discovery and automatic repair are not established.\n";
        File.WriteAllText(Path.Combine(folder, "summary.txt"), summary);
        Console.Write(summary);
    }

    private static int[] Digits(int number, int radix, int count)
    {
        var result = new int[count];
        for (var i = 0; i < count; i++) { result[i] = number % radix; number /= radix; }
        return result;
    }
    private static bool Admits(byte support, bool answer) => (support & (answer ? 2 : 1)) != 0;
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static string Csv(params object?[] values) => string.Join(",", values.Select(value =>
        "\"" + (Convert.ToString(value, CultureInfo.InvariantCulture) ?? "").Replace("\"", "\"\"") + "\""));
}

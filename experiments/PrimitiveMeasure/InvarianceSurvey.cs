using System.Globalization;
using System.Numerics;

namespace Gojo.Experiments.PrimitiveMeasure;

/// <summary>Offline observations over explicitly bounded transformation domains.
/// The existing owners, laws and public ports execute each case. This survey
/// checks their results; it does not classify concepts or create new structure
/// in response to an editorial concept name.</summary>
public static class InvarianceSurvey
{
    public static void Export(string folder)
    {
        Directory.CreateDirectory(folder);
        var cases = 0;
        var sharedEquality = 0;
        var matched = 0;
        var mismatched = 0;
        var unresolved = 0;
        var absent = 0;
        using (var output = File.CreateText(Path.Combine(folder, "cases.csv")))
        {
            output.WriteLine("case,variation,concept,limit,change,before_support,after_support,correspondence_support,equality_law_reused,law_bytes,row_bytes,source_bytes_before,source_bytes_after,consumer_committed,consumer_moved,consumer_activation_count");
            for (var which = 0; which < InvarianceExperiments.Cases.Count; which++)
            for (var variation = 0; variation < 3; variation++)
            {
                var result = InvarianceExperiments.Run(which, variation);
                var verdict = result.Port.ReadSupport();
                Require(variation != 0 || verdict == 2, "A preserving case lost its declared correspondence.");
                Require(variation != 1 || verdict is 1 or 3, "A challenging case neither rejects nor withdraws its guarantee.");
                Require(variation != 2 || verdict is 0 or 3, "A withheld observation manufactured a definite answer.");
                var reused = ReferenceEquals(result.Probe.Law, InvarianceExperiments.Equality);
                if (reused) sharedEquality++;
                switch (verdict) { case 0: absent++; break; case 1: mismatched++; break; case 2: matched++; break; case 3: unresolved++; break; }

                // The consumer only receives the public probe port. Its source
                // and target are fresh activation sites, not the source material.
                var consumer = new PulseCircuit(2);
                var source = consumer.State(true);
                var target = consumer.State();
                var guard = consumer.Guard();
                consumer.Bind(guard, result.Port);
                consumer.Link(consumer.AddTable(PassageLaw.Controlled()), source, guard, target);
                var pulse = consumer.Pulse();
                var count = (consumer.Expose(source).ReadSupport() == 2 ? 1 : 0)
                    + (consumer.Expose(target).ReadSupport() == 2 ? 1 : 0);
                Require(pulse.Moved == (verdict == 2) && count == 1, "The public consumer guessed, lost or duplicated its activation.");
                Require(pulse.Committed == (verdict is 1 or 2), "Unresolved or absent correspondence committed a consumer step.");
                var description = InvarianceExperiments.Cases[which];
                output.WriteLine(Csv(which, variation, description.Concept, description.Limit, result.Change,
                    result.BeforeSupport, result.AfterSupport, verdict, reused, Convert.ToHexString(result.LawBytes),
                    Convert.ToHexString(result.RowBytes), Convert.ToHexString(result.BeforeBytes), Convert.ToHexString(result.AfterBytes),
                    pulse.Committed, pulse.Moved, count));
                cases++;
            }
        }

        var addition = 0;
        var additionNonempty = 0;
        var additionAmbiguous = 0;
        using (var output = File.CreateText(Path.Combine(folder, "addition-frames.csv")))
        {
            output.WriteLine("left,right,source_support,expected_reoriented_support,actual_reoriented_support,sets_correspond,domain_present,outcome_correspondence_support");
            for (byte a = 0; a < 8; a++)
            for (byte b = 0; b < 8; b++)
            {
                var before = Mask(ThreeBitMeasure.Add(a, b));
                var expected = Mask(ThreeBitMeasure.Add(a, b).Select(ThreeBitMeasure.Reorient));
                var after = Mask(ThreeBitMeasure.Add(ThreeBitMeasure.Reorient(a), ThreeBitMeasure.Reorient(b)));
                var probe = new CorrespondenceProbe(InvarianceExperiments.Reorientation, new Observation(before), new Observation(after));
                Require(expected == after, "Addition violated the declared reorientation of its possible-answer set.");
                if (before != 0) additionNonempty++;
                if (Count(before) > 1) additionAmbiguous++;
                Require(probe.ReadSupport() == (before == 0 ? 0 : Count(before) == 1 ? 2 : 3),
                    "Possible-answer correspondence was confused with independent actual-outcome equality.");
                output.WriteLine(Csv(a, b, before, expected, after, expected == after, before != 0, probe.ReadSupport()));
                addition++;
            }
        }

        var switches = 0;
        using (var output = File.CreateText(Path.Combine(folder, "joint-switches.csv")))
        {
            output.WriteLine("y_material_before,control,t_material_before,y_material_after,expected_t_material_after,t_material_after,mapped_match,initial_material_count,y_material_count,t_material_count");
            for (byte material = 0; material < 8; material++)
            for (byte control = 0; control < 2; control++)
            {
                var y = GlyphJunction.YBranch((byte)(material | (control << 3)));
                var t = GlyphJunction.TBar((byte)(YToT(material) | (control << 3)), true);
                y.Advance(); t.Advance();
                var yAfter = (byte)(y.Flow.Stored & 7);
                var tAfter = (byte)(t.Flow.Stored & 7);
                var expected = YToT(yAfter);
                Require(expected == tAfter, "The Y/T role map failed on a material occupancy pattern.");
                Require(Count(yAfter) == Count(material) && Count(tAfter) == Count(material), "A mapped switch lost or duplicated material.");
                Require((y.Flow.Stored >> 3) == control && (t.Flow.Stored >> 3) == control, "The material transform altered control.");
                output.WriteLine(Csv(material, control, YToT(material), yAfter, expected, tAfter, true,
                    Count(material), Count(yAfter), Count(tAfter)));
                switches++;
            }
        }

        var collections = 0;
        using (var output = File.CreateText(Path.Combine(folder, "collecting-inputs.csv")))
        {
            output.WriteLine("inputs,exchanged_inputs,first_store,second_store,first_presence,second_presence,presence_preserved,initial_material_count,first_material_count,second_material_count");
            for (byte inputs = 0; inputs < 4; inputs++)
            {
                var exchanged = Exchange(inputs);
                var first = GlyphJunction.YCollect(inputs);
                var second = GlyphJunction.YCollect(exchanged);
                first.Advance(); second.Advance();
                var firstPresence = first.Stem.ReadSupport();
                var secondPresence = second.Stem.ReadSupport();
                Require(firstPresence == secondPresence, "Collecting presence changed when contributors were exchanged.");
                Require(firstPresence == (inputs == 0 ? 1 : 2), "The OR read lost the participation premise.");
                Require(Count(first.Flow.Stored) == Count(inputs) && Count(second.Flow.Stored) == Count(inputs),
                    "A collecting read consumed or duplicated material.");
                output.WriteLine(Csv(inputs, exchanged, first.Flow.Stored, second.Flow.Stored, firstPresence, secondPresence,
                    true, Count(inputs), Count(first.Flow.Stored), Count(second.Flow.Stored)));
                collections++;
            }
        }

        var merges = 0;
        var priorityCounterexamples = 0;
        using (var output = File.CreateText(Path.Combine(folder, "merge-priority.csv")))
        {
            output.WriteLine("inputs,exchanged_inputs,first_store,expected_exchanged_store,actual_second_store,exact_exchange_preserved,outlet_presence_preserved,first_material_count,second_material_count");
            for (byte inputs = 0; inputs < 4; inputs++)
            {
                var first = GlyphJunction.YMerge(inputs);
                var second = GlyphJunction.YMerge(Exchange(inputs));
                first.Advance(); second.Advance();
                var expected = Exchange(first.Flow.Stored);
                var equal = expected == second.Flow.Stored;
                var samePresence = first.Stem.ReadSupport() == second.Stem.ReadSupport();
                Require(samePresence, "The selected coarse merge presence unexpectedly changed.");
                Require(Count(first.Flow.Stored) == Count(inputs) && Count(second.Flow.Stored) == Count(inputs),
                    "A priority merge lost or duplicated material.");
                if (inputs != 3) Require(equal, "A single-arrival merge failed its input exchange.");
                if (!equal) priorityCounterexamples++;
                output.WriteLine(Csv(inputs, Exchange(inputs), first.Flow.Stored, expected, second.Flow.Stored,
                    equal, samePresence, Count(first.Flow.Stored), Count(second.Flow.Stored)));
                merges++;
            }
        }

        var spans = 0;
        var unchangedLawCounterexamples = 0;
        using (var output = File.CreateText(Path.Combine(folder, "boolean-spans.csv")))
        {
            output.WriteLine("truth,exchanged_truth,first_start,first_end,second_start,second_end,first_occupancy,second_occupancy,original_result,exchanged_mapped_result,exchanged_unmapped_result,mapped_match,unchanged_law_match");
            var rail = new LandmarkRun(2, [0, 1, 2, 3]);
            for (byte truth = 0; truth < 16; truth++)
            for (var a = 0; a < 4; a++)
            for (var b = a; b < 4; b++)
            for (var c = 0; c < 4; c++)
            for (var d = c; d < 4; d++)
            {
                var first = OrdinalInk.Closed(rail, a, b);
                var second = OrdinalInk.Closed(rail, c, d);
                // Swap the two input roles of the actual four-bit law as well
                // as the operands. Bits for 01 and 10 exchange; 00/11 remain.
                var swappedTruth = (byte)((truth & 9) | ((truth & 2) << 1) | ((truth & 4) >> 1));
                var original = first.Combine(second, truth)!.Bits;
                var mapped = second.Combine(first, swappedTruth)!.Bits;
                var unmapped = second.Combine(first, truth)!.Bits;
                Require(original == mapped, "Exchanging Boolean span roles and law failed to preserve occupied cells.");
                if (original != unmapped) unchangedLawCounterexamples++;
                output.WriteLine(Csv(truth, swappedTruth, a, b, c, d, first.Bits, second.Bits, original, mapped,
                    unmapped, original == mapped, original == unmapped));
                spans++;
            }
        }

        var cycles = 0;
        using (var output = File.CreateText(Path.Combine(folder, "cycle-participation.csv")))
        {
            output.WriteLine("sites,participation_pattern,topology_unchanged,closure_support,continuity_support,proof_unchanged");
            for (var count = 2; count <= 6; count++)
            for (var pattern = 0; pattern < 1 << count; pattern++)
            {
                var route = StructuralRoute.Cycle(count, Enumerable.Range(0, count).ToArray());
                route.Inspect(1);
                var before = route.TopologyBytes();
                var proof = (route.ClosureSupport, route.ContinuitySupport);
                for (var i = 0; i < count; i++) route.SetWord(i, (byte)((pattern >> i) & 1));
                var unchanged = before.SequenceEqual(route.TopologyBytes());
                var sameProof = proof == (route.ClosureSupport, route.ContinuitySupport);
                Require(unchanged && sameProof && route.ClosureSupport == 2 && route.ContinuitySupport == 2,
                    "A value observation changed retained cycle topology or its published proof.");
                output.WriteLine(Csv(count, pattern, unchanged, route.ClosureSupport, route.ContinuitySupport, sameProof));
                cycles++;
            }
        }

        var summary = $"{cases} concrete case/variation observations: {matched} established correspondences, {mismatched} rejected, {unresolved} unresolved, {absent} without an admitted read.\n" +
            $"{sharedEquality} case probes bind the same immutable equality-law instance to different exposed inputs; the other probes use explicit frame or role maps. Every case also controls a separate one-activation consumer through its public probe port.\n" +
            $"{addition} ODV operand pairs exhaust the 8 x 8 domain: {additionNonempty} nonempty addition domains, {addition - additionNonempty} incompatible empty domains, {additionAmbiguous} nonempty ambiguous answers. Reorientation preserves every answer set; empty-set agreement is not positive evidence, and matching sets do not identify independently unknown outcomes.\n" +
            $"{switches} Y/T switch trials exhaust 8 material patterns x 2 known controls under the specified three-arm map. Role correspondence and material counts are preserved.\n" +
            $"{collections} collecting-Y trials exhaust the two contributor bits: exchanging contributors preserves common presence without merging the retained material.\n" +
            $"{merges} priority-merge trials exhaust the two contributor bits: {priorityCounterexamples} exact-state counterexample(s) remain visible despite equal outlet presence. These are expected boundary observations, not failed conservation checks.\n" +
            $"{spans} Boolean-span trials exhaust 16 truth masks x 10 closed spans x 10 closed spans on one four-landmark rail. Exchanging operands together with the law preserves exact occupancy; exchanging operands alone produces {unchangedLawCounterexamples} counterexamples.\n" +
            $"{cycles} cycle trials exhaust all V patterns on cycles of 2..6 distinct sites. Retained closure is unchanged; circulation and geometric enclosure are not asserted.\n" +
            "All required conformance checks passed. The finite domains above do not exhaust geometry, all possible edits, unknown construction histories, concept discovery or general learning. Concept and limit sentences describe the apparatus after its structural reads; they never select its runtime laws.\n";
        File.WriteAllText(Path.Combine(folder, "summary.txt"), summary);
        Console.Write(summary);
    }

    private static byte YToT(byte pattern) => (byte)(((pattern << 1) & 7) | (pattern >> 2));
    private static byte Exchange(byte pattern) => (byte)((pattern & ~3) | ((pattern & 1) << 1) | ((pattern & 2) >> 1));
    private static byte Mask(IEnumerable<byte> words) => words.Aggregate((byte)0, (mask, word) => (byte)(mask | ReadSupport.Of(word)));
    private static int Count(byte pattern) => BitOperations.PopCount((uint)pattern);
    private sealed class Observation(byte support) : IPrimitiveReadPort { public byte ReadSupport() => support; }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static string Csv(params object?[] values) => string.Join(",", values.Select(value =>
        "\"" + (Convert.ToString(value, CultureInfo.InvariantCulture) ?? "").Replace("\"", "\"\"") + "\""));
}

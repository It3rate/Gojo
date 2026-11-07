using System.Text.Json;
using Gojo.Experiments.OrnamentPathCampaign;
using Gojo.Experiments.OrnamentPlateStudy;
using Gojo.Experiments.PrimitiveMeasure;

namespace Gojo.Experiments.OrnamentFlowPilot;

internal static class Program
{
    private sealed record Mark(int Pen, int Beat, int X1, int Y1, int X2, int Y2, string Kind);
    private sealed record Sample(string Name, int A, int B, int Beats, Mark[] Marks, PathSite[] BayEnds, string Stop);
    private sealed class Input(byte support) : IPrimitiveReadPort
    {
        public int Reads { get; private set; }
        public byte ReadSupport() { Reads++; return support; }
    }

    private static void Require(bool condition, string claim)
    {
        if (!condition) throw new InvalidOperationException(claim);
    }

    private static Sample Observe(string name, int a, int b, FretRun run, IEnumerable<Mark>? accent = null)
    {
        var lines = run.Trace.Edges.Select(edge =>
        {
            var from = run.Trace.Sites[edge.From];
            var to = run.Trace.Sites[edge.To];
            return new Mark(0, edge.Beat, from.X, from.Y, to.X, to.Y, "line");
        });
        return new(name, a, b, run.Beats.Length, lines.Concat(accent ?? []).OrderBy(mark => mark.Beat).ToArray(),
            run.BayEnds, run.Stop);
    }

    private static void CheckFrame(FretRun original, FretRun transformed, Func<PathSite, PathSite> map)
    {
        Require(transformed.Completed && original.Trace.Edges.Length == transformed.Trace.Edges.Length,
            "Frame changed the number of emissions.");
        foreach (var (left, right) in original.Trace.Edges.Zip(transformed.Trace.Edges))
        {
            Require(left.Beat == right.Beat && left.From == right.From && left.To == right.To &&
                map(original.Trace.Sites[left.From]) == transformed.Trace.Sites[right.From] &&
                map(original.Trace.Sites[left.To]) == transformed.Trace.Sites[right.To],
                "Frame failed to carry a stroke, time or site correspondence.");
        }
    }

    private static object[] ReadParticipation()
    {
        var pen = new FlowPen();
        var results = new List<object>();
        foreach (var (name, support, bound, magnitude) in new (string, byte, bool, int)[]
        {
            ("missing", 0, true, 4), ("off", 1, true, 4), ("uncertain", 3, true, 4),
            ("unmeasured contact", 2, false, 0), ("exact zero", 2, true, 0), ("detected extent", 2, true, 4)
        })
        {
            int reads = 0;
            var contact = pen.Touch(new Input(support), new PathSite(1, 0), bound ? () => { reads++; return magnitude; } : null);
            Require(contact.Participation == support && reads == (support == 2 && bound ? 1 : 0) &&
                contact.DrewSpot == (support == 2 && !bound) && contact.DrewStroke == (support == 2 && bound && magnitude > 0) &&
                contact.To.X - contact.From.X == (contact.DrewStroke ? magnitude : 0), "Participation and extent collapsed.");
            Require(pen.MarkPort.ReadSupport() == (contact.DrewSpot || contact.DrewStroke ? 2 : 1),
                "The public ink port reported a different deposit.");
            results.Add(new { name, support, extentReads = reads, contact });
        }
        return results.ToArray();
    }

    private static bool SameBeat(FretBeat first, FretBeat second) =>
        first.Beat == second.Beat && first.Bay == second.Bay &&
        first.Before == second.Before && first.After == second.After &&
        first.Start == second.Start && first.End == second.End &&
        first.Emitted == second.Emitted && first.Reflected == second.Reflected &&
        first.Reads.SequenceEqual(second.Reads);

    // The trainer replays a fresh copy of the same wired machine, changing only
    // its choice input. A published stroke in the first trial is never retracted.
    private static (FretRun First, FretRun? Alternative, int Prefix) Trial(int a, int b)
    {
        var fixedInput = new Input(1);
        var first = FlowFret.Run(a, b, false, bays: 1, choice: fixedInput);
        Require(first.Completed && fixedInput.Reads > 0, "Fixed trial missed its exposed choice.");
        if (first.BayEnds.Single().Y == 0) return (first, null, 0);

        var feedbackInput = new Input(2);
        var other = FlowFret.Run(a, b, false, bays: 1, choice: feedbackInput);
        var prefix = first.Beats.Zip(other.Beats).TakeWhile(pair => SameBeat(pair.First, pair.Second)).Count();
        Require(feedbackInput.Reads > 0 && prefix > 0 && prefix < Math.Min(first.Beats.Length, other.Beats.Length) &&
            first.Beats.Take(prefix).Count(beat => beat.Emitted) == 4 &&
            first.Beats[prefix].Before == first.Wiring.Fork && other.Beats[prefix].Before == first.Wiring.Fork,
            "Trials must share an actual prefix and then diverge.");
        return (first, other, prefix);
    }

    private static void Main(string[] args)
    {
        if (args is ["patterns", var patternOutput])
        {
            PatternStudy.Write(patternOutput);
            return;
        }
        if (args is ["signature", var signatureOutput])
        {
            SignatureStudy.Write(signatureOutput);
            return;
        }
        if (args is ["symmetry", var output])
        {
            SymmetryStudy.Write(output);
            return;
        }
        if (args.Length != 1) throw new ArgumentException("Supply an output path for the pilot JSON.");
        var samples = new List<Sample>();
        var fixedRun = FlowFret.Run(4, 2, false);
        var feedback = FlowFret.Run(4, 2, true);
        var control = PathMachine.Run(PlateReads.Author(0), new PathInput(4, 2, 6, 2));
        var controlInk = PlateReads.InkKey(control);
        Require(PlateReads.Measure(control, 0).ExactInk && new[] { fixedRun, feedback }.All(run =>
            run.Completed && PlateReads.InkKey(run.Trace) == controlInk && PlateReads.Measure(run.Trace, 0).ExactInk),
            "Both constructions must match the original Figure 1 control over two bays.");
        samples.Add(Observe("fixed", 4, 2, fixedRun));
        samples.Add(Observe("feedback", 4, 2, feedback));

        var stretchedFixed = FlowFret.Run(5, 2, false);
        var stretchedFeedback = FlowFret.Run(5, 2, true);
        Require(stretchedFixed.Completed && stretchedFeedback.Completed &&
            stretchedFixed.BayEnds.SequenceEqual([new PathSite(8, 1), new PathSite(16, 2)]) &&
            stretchedFeedback.BayEnds.SequenceEqual([new PathSite(8, 0), new PathSite(16, 0)]),
            "Increasing only A should separate the two mechanisms without resetting the pen at the seam.");
        samples.Add(Observe("stretched-fixed", 5, 2, stretchedFixed));
        samples.Add(Observe("stretched-feedback", 5, 2, stretchedFeedback));

        var mirror = FlowFret.Run(5, 2, true, frame: new FretFrame(new(2, 0), new(1, 0), new(0, -1)));
        var diagonal = FlowFret.Run(5, 2, true, frame: new FretFrame(default, new(0, 1), new(1, 0)));
        CheckFrame(stretchedFeedback, mirror, p => new(p.X + 2, -p.Y));
        CheckFrame(stretchedFeedback, diagonal, p => new(p.Y, p.X));
        samples.Add(Observe("mirrored", 5, 2, mirror));
        samples.Add(Observe("diagonal", 5, 2, diagonal));

        var alternating = FlowFret.Run(5, 2, true, alternateAtSeam: true);
        var bay0 = alternating.Trace.Edges.Where(e => e.Cell == 0).ToArray();
        var bay1 = alternating.Trace.Edges.Where(e => e.Cell == 1).ToArray();
        Require(alternating.Completed && alternating.Wiring.Alternation is not null &&
            alternating.BayEnds.SequenceEqual(stretchedFeedback.BayEnds) &&
            bay0.Length == bay1.Length && bay0[^1].To == bay1[0].From,
            "The second mirrored bay must share the first bay's actual seam site.");
        foreach (var (source, mirrored) in bay0.Zip(bay1))
        {
            var start = alternating.Trace.Sites[source.From];
            var end = alternating.Trace.Sites[source.To];
            Require(mirrored.Beat - bay1[0].Beat == source.Beat - bay0[0].Beat &&
                alternating.Trace.Sites[mirrored.From] == new PathSite(8 + start.X, -start.Y) &&
                alternating.Trace.Sites[mirrored.To] == new PathSite(8 + end.X, -end.Y),
                "The seam switch must preserve translated, reflected stroke correspondence.");
        }
        samples.Add(Observe("alternating", 5, 2, alternating));

        SingleSpotPen? spot = null;
        var dots = new List<Mark>();
        var accented = FlowFret.Run(4, 2, true, beforeBeat: (beat, previousMark) =>
        {
            spot ??= new SingleSpotPen(new PathSite(3, 3), previousMark);
            var contact = spot.Step();
            if (contact.DrewSpot) dots.Add(new Mark(1, beat, contact.From.X, contact.From.Y, contact.To.X, contact.To.Y, "spot"));
        });
        Require(spot?.Stopped == true && dots.Count == 1 &&
            dots[0].Beat == accented.Trace.Edges[0].Beat + 1 &&
            accented.Trace.Edges[^1].Beat > dots[0].Beat &&
            accented.Trace.Edges.SequenceEqual(feedback.Trace.Edges), "Expected one delayed spot and an undisturbed main pen.");
        samples.Add(Observe("accent", 4, 2, accented, dots));

        var nominal = Trial(4, 2);
        var retry = Trial(5, 2);
        Require(nominal.Alternative is null && retry.Alternative?.Completed == true &&
            retry.First.BayEnds.Single().Y == 1 && retry.Alternative.BayEnds.Single().Y == 0,
            "A second trial should help the baseline goal only on the stretched input.");
        samples.Add(Observe("trial-fixed", 5, 2, retry.First));
        samples.Add(Observe("trial-feedback", 5, 2, retry.Alternative!));

        var shortRise = FlowFret.Run(1, 2, true);
        var openChoice = FlowFret.Run(5, 2, false, choice: new Input(3));
        var missingChoice = FlowFret.Run(5, 2, false, choice: new Input(0));
        var budget = FlowFret.Run(4, 2, true, pulseBudget: 0);
        Require(shortRise.Stop == "no admitted outlet" && shortRise.BaselineOffset == -1 && shortRise.BayEnds.Length == 0 &&
            openChoice.Stop == "unresolved guard" && missingChoice.Stop == "missing guard" &&
            openChoice.Trace.Edges.Length == 4 && missingChoice.Trace.Edges.SequenceEqual(openChoice.Trace.Edges) &&
            budget.Stop == "pulse budget" && budget.Beats.Length == 0, "Incomplete or blocked cases must retain their distinct causes.");
        var report = new
        {
            samples, participation = ReadParticipation(),
            printed = new { controlInk, fixedEdges = fixedRun.Trace.Edges.Length, feedbackEdges = feedback.Trace.Edges.Length },
            wiring = new { fixedDrop = fixedRun.Wiring, feedback = feedback.Wiring, choice = retry.First.Wiring,
                seamSwitch = alternating.Wiring.Alternation,
                spot = Enumerable.Range(0, 2).Select(table => new {
                    law = Convert.ToHexString(spot!.Circuit.LawBytes(table)),
                    rows = Convert.ToHexString(spot.Circuit.PackedRows(table)) }) },
            replay = new { prefixBeats = retry.Prefix, matchedTrials = 1, stretchedTrials = 2,
                fixedTimeline = retry.First.Beats, feedbackTimeline = retry.Alternative!.Beats },
            boundaries = new { shortRise = shortRise.Stop, shortRise.BaselineOffset, openChoice = openChoice.Stop, missingChoice = missingChoice.Stop, budget = budget.Stop }
        };
        var file = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, JsonSerializer.Serialize(report, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }));
        Console.WriteLine($"Verified {samples.Count} illustrated runs. Both match Figure 1 at A=4, B=2. Stretched trial shares {retry.Prefix} initial beats.");
    }
}

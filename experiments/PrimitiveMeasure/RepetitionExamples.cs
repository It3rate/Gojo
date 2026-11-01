namespace Gojo.Experiments.PrimitiveMeasure;

public static class RepetitionExamples
{
    public static void Run()
    {
        Console.WriteLine("Overview: one activation visits a reusable body, checks a public stop port, and either feeds back or completes.");
        Console.WriteLine("A second cycle supplies phase. References identify stages and shared parameters; no geometric lengths are assigned.");
        Console.WriteLine("Every pulse follows at most one existing passage row. The eight-byte law is the same allowed before/after bitmap as before.");
        foreach (var (label, refs, phases) in new (string, int[], int)[]
        {
            ("square: one reach, four directed equal turns", [0], 4),
            ("rectangle: two shared reaches, two passes", [0, 1], 4),
            ("threefold body over four phases", [0, 1, 2], 4),
            ("five-part contour sampling", [0], 5)
        })
        {
            var body = new RepeatedBody(refs, phases);
            body.BindTurns(Contour(phases, true, false));
            var trace = Run(body, 200);
            Console.WriteLine($"\n{label}: {string.Join(" ", trace.Where(t => t.Parameter is not null).Select(t => $"r{t.Parameter}@phase{t.Phase}"))}");
            Console.WriteLine($"completed={trace.Last().Completed}; parameter rows {Convert.ToHexString(body.ParameterBytes())}");
            Console.WriteLine($"body forward rows {Convert.ToHexString(body.Circuit.PackedRows(body.ForwardTable))}");
            Console.WriteLine($"feedback rows {Convert.ToHexString(body.Circuit.PackedRows(body.AbsentTable))}; phase rows {Convert.ToHexString(body.Phase.Circuit.PackedRows(body.Phase.Table))}");
            Console.WriteLine($"equal turn correspondence={body.HasEqualTurnBasis}; rows {Convert.ToHexString(body.TurnBytes())}");
        }

        var continuous = new RepeatedBody([0, 1], 4, continuous: true);
        var recurring = Run(continuous, 30);
        Console.WriteLine($"\nContinuous rectangle controller: {recurring.Count(t => t.Parameter is not null)} participations, no completion.");
        Console.WriteLine("The 30-pulse observation window is a harness limit, not a stop rule in the structure.");

        var composition = new PatternComposition();
        Console.WriteLine("\nRectangle -> five waiting sites -> request another rectangle. Displacement runs during the wait.");
        for (var pulse = 0; pulse < 100 && composition.Instances < 3; pulse++)
        {
            var step = composition.Pulse();
            if (step.StartedAt is int placement) Console.WriteLine($"pulse {pulse}: motif starts at exposed placement {placement}");
            if (step.Body.Completed) Console.WriteLine($"pulse {pulse}: motif completed");
            if (step.PlacementAdvanced) Console.WriteLine($"pulse {pulse}: driver attached a new placement through shared offset 0");
        }
        Console.WriteLine($"placement rows {Convert.ToHexString(composition.TranslationBytes())}; instance bindings {Convert.ToHexString(composition.FrameBytes())}");

        var moving = new MovingFramePattern();
        for (var pulse = 0; pulse < 10; pulse++) moving.Pulse();
        Console.WriteLine("\nBoth structures run: local rectangle participations bind to a changing placement.");
        for (var i = 0; i < moving.SampleCount; i++) Console.WriteLine($"[parameter,phase,placement] = [{string.Join(',', moving.Sample(i))}]");
        Console.WriteLine("Local recurrence remains; world-space closure is not established because the placement changes during the body.");

        Console.WriteLine("\nRadial construction: shared radius throughout arcs, one whole traversal, actual return binding.");
        foreach (var parts in new[] { 1, 3, 4, 5, 6 })
        {
            var contour = Contour(parts, equalParts: true, joinChords: parts >= 3);
            Console.WriteLine($"{parts} parts: {contour.Read()}");
            if (parts == 5) Console.WriteLine($"pentagon arc rows {Convert.ToHexString(contour.ArcBytes())}; closure {Convert.ToHexString(contour.ClosureBytes())}; chords {Convert.ToHexString(contour.ChordBytes())}");
        }
        Console.WriteLine($"Alternating circle parts: {Contour(4, equalParts: false, joinChords: true).Read()}");
        Console.WriteLine("These are selected geometric constraint families, not a numerical trajectory or an emergent field simulation.");

        var contact = new Observation(3);
        var spiral = new RepeatedBody([0], 4, stop: contact);
        var partial = Run(spiral, 10);
        Console.WriteLine($"\nOpen contact read: committed={partial.Last().Committed}, retained at check={spiral.Circuit.Active == spiral.Check}.");
        contact.Support = 1;
        Console.WriteLine($"Supply 'not in contact': feedback commits={spiral.Pulse().Committed}.");
        contact.Support = 2;
        Console.WriteLine($"Supply 'contact': completes after body/check={Run(spiral, 10).Last().Completed}.");
        Console.WriteLine("Shrinking reach, self-distance and contact are still separate unresolved geometric inputs; V=1 cannot measure them.");
        Console.WriteLine("See RepetitionExperiments.md for all connections, branching/rejoining, live placement, and limits.");
    }

    public static List<BodyStep> Run(RepeatedBody body, int pulseBudget)
    {
        var result = new List<BodyStep>();
        for (var pulse = 0; pulse < pulseBudget; pulse++)
        {
            var step = body.Pulse();
            result.Add(step);
            if (step.Completed || !step.Committed) break;
        }
        return result;
    }

    public static ContourPattern Contour(int parts, bool equalParts, bool joinChords)
    {
        if (parts is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(parts));
        var contour = new ContourPattern(new Observation(2), new Observation(2));
        var sites = new[] { ContourPattern.Start }.Concat(Enumerable.Range(5, parts - 1)).Append(ContourPattern.End).ToArray();
        for (var i = 0; i < parts; i++)
        {
            contour.Arc(sites[i], sites[i + 1], 32 + (equalParts ? 0 : i % 2));
            if (joinChords) contour.Chord(sites[i], i + 1 == parts ? ContourPattern.Start : sites[i + 1]);
        }
        contour.Close();
        return contour;
    }

    // Demonstration-world observations supplied to public read surfaces. These
    // support masks are not extra fields in the encoded ODV value arrays.
    private sealed class Observation(byte support) : IPrimitiveReadPort
    {
        public byte Support { get; set; } = support;
        public byte ReadSupport() => Support;
    }
}

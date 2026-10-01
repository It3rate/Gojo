using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Patterns;
using Gojo.Structures;
using SkiaSharp;
using static PosterScratchpad.PosterSurfaces;
using static PosterScratchpad.QualifiedAxisSurface;

namespace PosterScratchpad;

internal static class SeriesTwoAssessment
{
    internal static void Run(string output)
    {
        var documents = IndependentPosters.Build();
        var manifest = File.ReadAllText(Path.Combine(output, "generation-manifest.txt"));
        var names = new[] { "c", "d", "e" };
        for (var index = 0; index < documents.Length; index++)
        {
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output, $"poster-{names[index]}.png"))));
            Check(manifest.Contains($"{names[index]} SHA256: {hash}", StringComparison.Ordinal), "frozen PNG hash");
            FrozenSource.RequireMatch(documents[index], Path.Combine(output, $"poster-{names[index]}.snapshot.gojo.json"));
        }
        var provenance = Words(manifest);
        var notebook = new List<IElement> { Group(documents), provenance };
        var report = new List<string>
        {
            "# Independent posters: resolution and contextual faces",
            $"Assessment UTC: {DateTime.UtcNow:O}. Frozen PNGs and Gojo source graphs verified.",
            "Three designs were generated before assessment. Signs and opposed faces remain separate. Numeric folds are explicitly calibrated context reads, not kernel identities."
        };

        var pair = Pair("strong", "weak", Group(documents));
        var forward = Forward(pair);
        var inverse = Opposed(pair);
        var coarse = Observe(documents[0], pair, forward, 1, 2, "Declared two-cell observation for the resolution probe.", provenance);
        var fine = Observe(documents[0], pair, forward, 100, 200, "Declared 200-cell observation for the resolution probe; not 200 human judgments.", provenance);
        Check(Value(coarse).Ticks == 5000 && Value(fine).Ticks == 5000, "same proportional value");
        Check(Low(coarse).Ticks == 2500 && High(coarse).Ticks == 7500, "coarse half-cell envelope");
        Check(Low(fine).Ticks == 4975 && High(fine).Ticks == 5025, "fine half-cell envelope");
        var rewrittenAxis = new Carrier();
        var rewriting = Project(Equation.Affine(forward, 0, 2, rewrittenAxis, 0, 200), NativeValue(coarse), rewrittenAxis);
        var rewritten = ReadWitness.Create(new Carrier(), coarse,
            Relation.Segment(rewrittenAxis, 0, 200), Value(rewriting), Bounds(coarse), Group(rewriting, coarse));
        Check(Value(rewritten).Ticks == 100, "equivalent notation on a finer display scale");
        ReadWitness.TryRead(rewritten, out _, out _, out _, out var retainedGrain, out _);
        Check(ReferenceEquals(retainedGrain, Bounds(coarse)), "rewriting keeps original resolution evidence");
        notebook.Add(Group(pair, coarse, fine, rewritten));
        report.Add("Resolution probe: 1/2 and 100/200 both project to 0.5. Their declared half-cell envelopes are [0.25, 0.75] and [0.4975, 0.5025]. Rewriting the coarse value as 100/200 retains the coarse envelope; no new evidence is invented. These are resolution cells, not statistical confidence intervals.");

        var strong = Observe(documents[1], pair, forward, 4, 5, "Affirming the strong face.", provenance);
        var negativeWeak = Observe(documents[1], pair, inverse, -4, 5, "Rejecting weakness, with the weak face retained as source context.", provenance);
        var weak = Observe(documents[0], pair, inverse, 4, 5, "Affirming weakness.", provenance);
        var negativeStrong = Observe(documents[0], pair, forward, -4, 5, "Rejecting strength, on the original face.", provenance);
        Check(Value(strong).Ticks == 8000 && Value(negativeWeak).Ticks == 8000, "explicit strong / negative weak fold");
        Check(Value(weak).Ticks == -8000 && Value(negativeStrong).Ticks == -8000, "explicit weak / negative strong fold");
        Check(!NativeValue(negativeWeak).ReadFrom(forward).IsVisible, "opposed face is not directly visible as a negative forward location");
        Check(NativeValue(negativeStrong).ReadFrom(forward).IsVisible, "negative scalar remains visible on its own face");
        var unresolved = OperationClaim.Equality(new Carrier(), NativeValue(strong), NativeValue(negativeWeak), new Carrier());
        OperationClaim.TryRead(unresolved, out _, out var nativeResidual, out _, out _, out _, out _);
        Check(nativeResidual is Relation, "native face comparison stays unresolved without contextual fold");
        notebook.Add(Group(strong, negativeWeak, weak, negativeStrong, unresolved));
        report.Add("Face/sign probe: +strong 4/5 and -weak 4/5 both fold to +0.8 through the declared comparison. +weak and -strong fold to -0.8. The source carrier identities, signs, and witnesses remain different. The unaligned native comparison remains unresolved, and the inverse face is not a reciprocal.");

        var layers = documents.Select(d => Parts((Relation)Parts(d)[1])).ToArray();
        var logos = new[] { layers[0][5], layers[1][4], layers[2][6] };
        var pairs = new List<Relation>();
        var observations = new[] { new List<Relation>(), new List<Relation>(), new List<Relation>() };
        report.Add("## Paired measures\n\nThe chosen positive direction is a convention, not a universal preference. A negative numerator on the inverse face is distinct from a positive numerator on the forward face. Fractions are never reduced. Denominators describe the declared reading resolution, not the number of people surveyed.\n\n| Natural direction / inverse | C | D | E | Evidence |\n|---|---|---|---|---|");
        void Measure(string positive, string opposed, IElement[] selected,
            (long Numerator, long Denominator, bool OnInverse) c,
            (long Numerator, long Denominator, bool OnInverse) d,
            (long Numerator, long Denominator, bool OnInverse) e, string evidence)
        {
            var basis = Pair(positive, opposed, Group(selected));
            pairs.Add(basis);
            var samples = new[] { c, d, e };
            var cells = new List<string>();
            for (var index = 0; index < documents.Length; index++)
            {
                var sample = samples[index];
                var face = sample.OnInverse ? Opposed(basis) : Forward(basis);
                var reading = Observe(selected[index], basis, face, sample.Numerator, sample.Denominator,
                    evidence, Group(provenance, Words("Assistant's post-render observation; cultural interpretation remains open.")));
                observations[index].Add(reading);
                cells.Add($"{(sample.OnInverse ? opposed : positive)} {sample.Numerator}/{sample.Denominator}");
            }
            report.Add($"| {positive} / {opposed} | {string.Join(" | ", cells)} | {evidence} |");
        }
        Measure("strong", "weak", logos, (3, 5, true), (9, 10, false), (4, 5, false),
            "C has slender ring strokes; D's solid blades and E's hard contrasting rails have heavier visual presence. This compares graphic weight, not material strength.");
        Measure("big", "small", logos, (1, 2, true), (4, 5, false), (3, 5, false),
            "Apparent dominance in each composition: C's isolated mark feels restrained, D's colored field amplifies its mark, and E's mark commands the right panel. This is not a measured bounding-box ratio.");
        Measure("happy", "sad", documents, (0, 2, false), (2, 3, false), (1, 3, true),
            "C is quiet with no settled emotional direction; D permits a cheerful read; E permits a somber read. These deliberately coarse cells retain affective ambiguity: calm is not automatically sadness.");
        Measure("colorful", "muted", documents, (4, 5, true), (9, 10, false), (19, 20, true),
            "D has broad saturated fields; C uses a limited palette with a small warm accent; E is nearly monochrome. Colorfulness is not a claim that a palette is harmonious.");
        Measure("balanced", "unbalanced", documents, (7, 10, false), (3, 5, false), (7, 10, false),
            "C balances the left title against the low right mark; D uses an off-axis panel with a stable footer; E balances unequal light and dark columns, with competing left-date/right-logo focal points.");
        Measure("spacious", "crowded", documents, (9, 10, false), (2, 5, true), (4, 5, false),
            "C has long quiet intervals, D deliberately packs large fields and type, and E has broad unoccupied dark support. Spacious does not mean light-colored.");
        Measure("ordered", "disordered", documents, (4, 5, false), (1, 2, false), (9, 10, false),
            "C uses consistent edges; D mixes a tilted field with rectangular information tiles; E repeats nested rails and a strict two-column division.");
        Measure("harmonious", "clashing", documents, (9, 10, false), (3, 5, false), (19, 20, false),
            "C's navy/red restraint and E's narrow neutral palette are consistent; D's four bright colors deliberately produce more tension.");
        Measure("clear", "obscure", documents, (3, 5, false), (4, 5, false), (1, 2, false),
            "C's small details are secondary, D's bold information tiles scan directly, and E splits date and title across columns. Word meanings and the actual date/price are excluded.");
        Measure("resilient at small size", "fragile at small size", logos, (1, 2, false), (4, 5, false), (3, 5, false),
            "At the inspected reduced preview, D keeps broad simple pieces; C's fine rings and E's nested inner details are more vulnerable. This is an observer hypothesis, not a recognition study.");
        Measure("welcoming", "distant", documents, (1, 2, false), (4, 5, false), (2, 5, true),
            "C is reserved, D reads as accessible and exuberant, and E as severe. These associations are observer/context bindings, not intrinsic meanings of shapes.");
        Measure("expressive", "anonymous", documents, (3, 5, false), (4, 5, false), (3, 5, false),
            "C's open rings and asymmetry, D's four-blade arrangement, and E's labyrinth have distinct formal character; no originality or trademark claim is made.");
        notebook.Add(Group(Group(pairs.ToArray()), Group(observations.Select(o => (IElement)Group(o.ToArray())).ToArray())));

        var colorEnergy = Pair("RGB channel spread", "opposed RGB spread read", Group(documents));
        report.Add("## Measured color support\n\nA 10×20 grid contributes 200 spatial samples. Each sample's maximum minus minimum encoded RGB channel is read by Gojo, then accumulated. The denominator is 200 samples × 255 channel steps; this is a sampled color-energy proxy, not perceptual saturation or 51,000 independent observations.");
        for (var index = 0; index < documents.Length; index++)
            notebook.Add(ColorSupport(documents[index], colorEnergy, Path.Combine(output, $"poster-{names[index]}.png"), names[index], provenance, report));

        report.Add("## Predicted eye motion\n\nC: title → red accent → open rings → lower details. D: title or central blades → bold time tile → fee tile. E: large left date or right labyrinth → title on the other column → lower details. These are competing hypotheses, not gaze observations.");
        notebook.Add(ReadWitness.Create(new Carrier(), documents[0], Group(layers[0][3], layers[0][4], logos[0], layers[0][7], layers[0][8]),
            Group(layers[0][3], logos[0], layers[0][7], layers[0][8]), Group(logos[0], layers[0][3]), provenance));
        notebook.Add(ReadWitness.Create(new Carrier(), documents[1], Group(layers[1][2], logos[1], layers[1][8], layers[1][9]),
            Group(layers[1][2], logos[1], layers[1][8], layers[1][9]), Group(logos[1], layers[1][2]), provenance));
        notebook.Add(ReadWitness.Create(new Carrier(), documents[2], Group(layers[2][3], logos[2], layers[2][5], layers[2][8]),
            Group(layers[2][3], logos[2], layers[2][5], layers[2][8]), Group(logos[2], layers[2][3]), provenance));

        report.Add("## Contextual suitability\n\nWeights bind to the same paired axes for every design. A negative preference coefficient can favor the inverse direction. Gojo composes each weighted contribution and both cell endpoints, then maps the result to 0–10. Brackets propagate represented cell resolution only; they do not cover every cultural or modeling uncertainty.\n\n| Context | C /10 | D /10 | E /10 | Assumed context |\n|---|---:|---:|---:|---|");
        var rankReads = new List<string>();
        void Context(string name, string assumption, params long[] weights)
        {
            var weightAxis = new Carrier();
            var totalMagnitude = new Carrier();
            var bindings = new List<IElement>();
            for (var i = 0; i < weights.Length; i++)
            {
                var location = new CarrierLocation(weightAxis, weights[i]);
                var magnitudeClaim = OperationClaim.Equality(new Carrier(), location, new CarrierLocation(weightAxis, 0), weightAxis);
                OperationClaim.TryRead(magnitudeClaim, out _, out _, out _, out var magnitude, out _, out _);
                totalMagnitude.Add((ulong)magnitude.Ticks);
                bindings.Add(new Relation(pairs[i], location));
            }
            Check(totalMagnitude.Value == 100 && weights.Length == pairs.Count, "context basis has total absolute weight 100");
            var context = Group(Words(name), Group(bindings.ToArray()), totalMagnitude.ValueSlot, Words(assumption));
            var scoreAxis = new Carrier();
            var scores = observations.Select(o => Suitability(o, context, scoreAxis)).ToArray();
            notebook.Add(ReadWitness.Create(new Carrier(), Group(documents), context, Group(scores),
                Group(scores.Select(s => Parts(s)[1]).ToArray()), Group(observations.Select(o => (IElement)Group(o.ToArray())).ToArray())));
            string Display(Relation score)
            {
                var values = Parts(score);
                return $"{Value((Relation)values[0]).Ticks / 200000.0:0.0} [{Value((Relation)values[1]).Ticks / 200000.0:0.0}–{Value((Relation)values[2]).Ticks / 200000.0:0.0}]";
            }
            report.Add($"| {name} | {string.Join(" | ", scores.Select(Display))} | {assumption} |");
            // Compare before display rounding. A positive separation says the
            // nominal leader clears every other represented cell envelope.
            var ranking = ExtremaTournament.Maximum(new Carrier(), scores.Select(s =>
                ExtremaTournament.Entry(s, Value((Relation)Parts(s)[0]))).ToArray(), out _);
            ExtremaTournament.TryReadWinner(ranking, out var winningEntry);
            ExtremaTournament.TryReadEntry(winningEntry, out var selected, out _);
            var winner = Array.FindIndex(scores, s => ReferenceEquals(s, selected));
            var lower = Value((Relation)Parts(scores[winner])[1]);
            var comparisonAxis = new Carrier();
            var difference = Equation.Difference(scoreAxis, scoreAxis, comparisonAxis);
            var separations = scores.Where(s => !ReferenceEquals(s, selected)).Select(s =>
                Calculate(difference, new Relation(Value((Relation)Parts(s)[2]), lower), comparisonAxis)).ToArray();
            var least = ExtremaTournament.Minimum(new Carrier(), separations.Select(s =>
                ExtremaTournament.Entry(s, Value(s))).ToArray(), out _);
            ExtremaTournament.TryReadWinner(least, out var leastEntry);
            ExtremaTournament.TryReadEntry(leastEntry, out _, out var margin);
            notebook.Add(ReadWitness.Create(new Carrier(), Group(scores), context, margin,
                UnitShape.DescriptorSlot(0), Group(ranking, Group(separations), least)));
            rankReads.Add($"| {name} | {names[winner].ToUpperInvariant()} | {margin.Ticks / 200000.0:+0.000;-0.000;0.000} | {(margin.Ticks > 0 ? "Separated within represented cells" : "Overlapping cells; ranking unresolved at this resolution")} |");
        }
        Context("Children's event", "Welcoming family event, with cheer, color and readable details.", 5, 5, 20, 20, 0, 0, 0, 5, 15, 10, 20, 0);
        Context("Tech trade show", "An energetic public technology showcase with precision and clear information.", 15, 5, 0, 5, 5, 5, 20, 5, 20, 10, 5, 5);
        Context("Oil industry meeting", "A restrained operational/engineering meeting. The colorfulness preference is negative here.", 20, 0, 0, -5, 10, 5, 20, 10, 20, 5, 5, 0);
        Context("Art show", "Independent art with space, harmony and formal expression.", 0, 0, 0, 0, 10, 15, 5, 20, 5, 5, 5, 35);
        Context("Small-town bake sale", "Neighborly welcome and clear, cheerful information.", 0, 0, 10, 5, 5, 0, 0, 10, 15, 5, 40, 10);

        report.Add("## Ordering under the represented resolution\n\nGojo selects the nominal leader, subtracts every rival's upper endpoint from that leader's lower endpoint, and keeps the smallest signed separation. Positive means separated within these represented cells only; a nonpositive margin leaves the ordering open. Score comparisons precede display rounding. Source projections still retain their own grain and residue.\n\n| Context | Nominal leader | Minimum separation /10 | Read |\n|---|---|---:|---|");
        report.AddRange(rankReads);

        report.Add("The stronger denominators here preserve a finer declared grid; they do not manufacture observer reliability. The unbound contextual evidence in each reading remains open. C's thinness is not a defect in every context, D's colorfulness is not automatically better, and E's somber reading is not a negative number on the happy face unless the chosen fold is applied.");

        File.WriteAllText(Path.Combine(output, "qualified-assessment.snapshot.gojo.json"),
            JsonSerializer.Serialize(GojoEncodedSnapshot.Capture(Group(notebook.ToArray())), new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(output, "qualified-assessment.md"), string.Join("\n\n", report).Replace("|\n\n|", "|\n|"));
        Console.WriteLine(string.Join("\n", report));
    }

    private static Relation Suitability(IReadOnlyList<Relation> observations, Relation context, Carrier scoreAxis)
    {
        var bindings = Parts((Relation)Parts(context)[1]);
        var raw = new Carrier();
        var centerContributions = new List<IElement>();
        var lowContributions = new List<IElement>();
        var highContributions = new List<IElement>();
        var contributionEvidence = new List<IElement>();
        foreach (Relation binding in bindings)
        {
            var observation = observations.Single(o => ReferenceEquals(ReadPair(o), binding.Left));
            var weight = (CarrierLocation)binding.Right;
            var surface = Equation.Sum(raw, Equation.WeightedPort(Value(observation).Carrier, 0, 1, 0, weight.Ticks));
            var center = Calculate(surface, Value(observation), raw);
            var first = Calculate(surface, Low(observation), raw);
            var second = Calculate(surface, High(observation), raw);
            var endpoints = new[] { ExtremaTournament.Entry(first, Value(first)), ExtremaTournament.Entry(second, Value(second)) };
            var min = ExtremaTournament.Minimum(new Carrier(), endpoints, out _);
            var max = ExtremaTournament.Maximum(new Carrier(), endpoints, out _);
            ExtremaTournament.TryReadWinner(min, out var minEntry);
            ExtremaTournament.TryReadWinner(max, out var maxEntry);
            ExtremaTournament.TryReadEntry(minEntry, out _, out var minimum);
            ExtremaTournament.TryReadEntry(maxEntry, out _, out var maximum);
            centerContributions.Add(Value(center)); lowContributions.Add(minimum); highContributions.Add(maximum);
            contributionEvidence.Add(Group(observation, binding, center, first, second, min, max));
        }
        var sumSurface = Equation.Sum(raw, bindings.Select(_ => Equation.UnitPort(raw)).ToArray());
        var totalWeight = (CarrierLocation)Parts(context)[2];
        var limit = Calculate(Equation.Product(raw, Equation.UnitPort(raw), Equation.UnitPort(totalWeight.Carrier)),
            new Relation(new CarrierLocation(raw, 10000), totalWeight), raw);
        var positiveLimit = Value(limit);
        // Preserve all accumulated ticks for comparison; decimal formatting
        // belongs only at the report boundary, not in the preference graph.
        var route = Equation.Affine(raw, positiveLimit.Dual().Ticks, positiveLimit.Ticks, scoreAxis, 0, 2000000);
        var projections = new List<IElement>();
        foreach (var values in new[] { centerContributions, lowContributions, highContributions })
        {
            var sum = Calculate(sumSurface, StructuralPattern.Chain(values), raw);
            projections.Add(Project(route, Value(sum), scoreAxis));
            contributionEvidence.Add(sum);
        }
        return Group(projections[0], projections[1], projections[2], Group(contributionEvidence.ToArray()), limit, context);
    }

    private static Relation ColorSupport(Relation document, Relation pair, string path, string name,
        Relation provenance, List<string> report)
    {
        using var image = SKBitmap.Decode(path);
        var channel = new Carrier();
        var spread = new Carrier();
        var total = new Carrier();
        var count = new Carrier();
        var samples = new List<IElement>();
        var difference = Equation.Difference(channel, channel, spread);
        for (var row = 0; row < 20; row++)
        for (var column = 0; column < 10; column++)
        {
            // Sampling positions and channel extraction are the raster boundary.
            var x = column * 108 + 54;
            var y = row * 72 + 36;
            var color = image.GetPixel(x, y);
            var entries = new[]
            {
                ExtremaTournament.Entry(new CarrierLocation(Red, color.Red), new CarrierLocation(channel, color.Red)),
                ExtremaTournament.Entry(new CarrierLocation(Green, color.Green), new CarrierLocation(channel, color.Green)),
                ExtremaTournament.Entry(new CarrierLocation(Blue, color.Blue), new CarrierLocation(channel, color.Blue))
            };
            var min = ExtremaTournament.Minimum(new Carrier(), entries, out _);
            var max = ExtremaTournament.Maximum(new Carrier(), entries, out _);
            ExtremaTournament.TryReadWinner(min, out var minEntry);
            ExtremaTournament.TryReadWinner(max, out var maxEntry);
            ExtremaTournament.TryReadEntry(minEntry, out _, out var lower);
            ExtremaTournament.TryReadEntry(maxEntry, out _, out var upper);
            var read = Calculate(difference, new Relation(lower, upper), spread);
            total.Add((ulong)Value(read).Ticks);
            count.Add(1);
            samples.Add(Group(Point(x, y), min, max, read));
        }
        var capacity = new Carrier();
        var denominator = Calculate(Equation.Product(capacity, Equation.UnitPort(count), Equation.UnitPort(channel)),
            new Relation(count.ValueSlot, new CarrierLocation(channel, 255)), capacity);
        var observation = Observe(document, pair, Forward(pair), (long)total.Value, Value(denominator).Ticks,
            "Exact read of this 200-cell sample; spatial sampling error and perceptual interpretation remain unresolved.",
            Group(provenance, Group(samples.ToArray()), count.ValueSlot, denominator));
        report.Add($"Poster {name.ToUpperInvariant()}: RGB spread {total.Value}/{Value(denominator).Ticks}, projected {Value(observation).Ticks / 10000.0:0.0000}. The denominator retains the 200×255 factorization in provenance.");
        return observation;
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException($"Scratchpad check failed: {description}");
    }
}

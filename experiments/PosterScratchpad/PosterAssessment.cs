using System.Security.Cryptography;
using System.Text.Json;
using Gojo.Core;
using Gojo.Encoding;
using Gojo.Equations;
using Gojo.Patterns;
using Gojo.Structures;
using SkiaSharp;
using static PosterScratchpad.PosterSurfaces;

namespace PosterScratchpad;

// Added only after both PNGs were generated and visually inspected.
// Subjective observations are supplied judgments, not an image-understanding
// algorithm. Their sources, alternatives, and context weights remain Gojo
// relations. All weighting, interval propagation, differences and measurements
// are executed by Gojo; formatting is the final adapter boundary.
internal static class PosterAssessment
{
    private static Relation Words(string value) => EncodedTextSurface.Create(new Carrier(), value);
    private static Relation Predict(Relation equation, IElement input, Carrier result) =>
        Equation.PredictWitness(new Carrier(), equation, input, result);
    private static CarrierLocation Result(Relation witness)
    {
        if (!Equation.TryReadPredictionWitness(witness, out _, out _, out var result, out _, out _))
            throw new InvalidOperationException("Unreadable prediction witness.");
        return result;
    }

    internal static void Run(Relation a, Relation b, string output)
    {
        var manifestPath = Path.Combine(output, "generation-manifest.txt");
        var manifest = File.ReadAllText(manifestPath);
        foreach (var (letter, file) in new[] { ("A", "poster-a.png"), ("B", "poster-b.png") })
        {
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output, file))));
            if (!manifest.Contains($"{letter} SHA256: {hash}", StringComparison.Ordinal))
                throw new InvalidOperationException("Poster has changed since generation; assessment was not run.");
        }
        foreach (var (document, file) in new[] { (a, "poster-a.snapshot.gojo.json"), (b, "poster-b.snapshot.gojo.json") })
        {
            FrozenSource.RequireMatch(document, Path.Combine(output, file));
        }
        var provenance = Words(manifest);
        var aLayers = Parts((Relation)Parts(a)[1]);
        var bLayers = Parts((Relation)Parts(b)[1]);
        var aLogo = (Relation)aLayers[4];
        var bLogo = (Relation)bLayers[3];
        var aPalette = Group(aLayers[0], aLayers[3], aLogo);
        var bPalette = Group(bLayers[0], bLogo, bLayers[7]);
        var aType = Group(aLayers[6], aLayers[8], aLayers[9]);
        var bType = Group(bLayers[4], bLayers[6], bLayers[7]);
        var notebook = new List<IElement> { a, b, provenance };
        var report = new List<string>
        {
            "# Poster assessment: generated first, assessed second",
            $"Assessment UTC: {DateTime.UtcNow:O}",
            "Both PNG SHA256 values match the generation manifest, and both reconstructed Gojo snapshots match the frozen generation artifacts after filling empty diagnostic-schema defaults. No image is redrawn during assessment.",
            "Numbers below are declared judgments and contextual preferences, not objective aesthetic measurements. Uncertainty bands are plausible alternative reads, not probabilities or confidence intervals. Words, event date, price, and implied event identity do not enter the fit calculations."
        };

        var aMeasured = Measure(a, Path.Combine(output, "poster-a.png"), "A", report, provenance);
        var bMeasured = Measure(b, Path.Combine(output, "poster-b.png"), "B", report, provenance);
        notebook.Add(Group(aMeasured, bMeasured));

        // A scale is attached to observable source selections in both posters.
        // Its name is documentary; the anchored visual evidence is still reachable.
        Relation Criterion(string name, string footing, IElement aSource, IElement bSource)
        {
            var axis = new Carrier();
            return Group(Relation.Segment(axis, 0, 10), Group(aSource, bSource), Words(name), Words(footing));
        }
        Relation Judge(Relation criterion, IElement selected, long grade, long low, long high,
            string evidence, string alternative)
        {
            var domain = (Relation)Parts(criterion)[0];
            var axis = ((Relation)domain.Left).LeftLocation.Carrier;
            return ReadWitness.Create(new Carrier(), selected, criterion,
                Group(new CarrierLocation(axis, grade), Relation.Segment(axis, low, high)),
                Group(Words(alternative), UnitShape.DescriptorSlot(0)),
                Group(provenance, Words("Observer: assistant; post-render visual judgment"), Words(evidence)));
        }

        report.Add("## Aesthetic reads\n\n| Criterion | A /10 | B /10 | Post-render observation |\n|---|---:|---:|---|");
        Relation Compare(string name, string footing, IElement aSource, IElement bSource,
            long aGrade, long aLow, long aHigh, long bGrade, long bLow, long bHigh,
            string aEvidence, string bEvidence, string alternative)
        {
            var criterion = Criterion(name, footing, aSource, bSource);
            var aRead = Judge(criterion, aSource, aGrade, aLow, aHigh, aEvidence, alternative);
            var bRead = Judge(criterion, bSource, bGrade, bLow, bHigh, bEvidence, alternative);
            var comparison = Group(criterion, aRead, bRead);
            notebook.Add(comparison);
            report.Add($"| {name} | {aGrade} ({aLow}–{aHigh}) | {bGrade} ({bLow}–{bHigh}) | A: {aEvidence} B: {bEvidence} |");
            return comparison;
        }
        var balance = Compare("Balance", "Distribution around the page axis and counterweights, not simple symmetry.", a, b,
            8, 7, 9, 9, 8, 9,
            "Centered flower, offset dots, and the round lower-right badge create an animated diagonal counterweight.",
            "Centered angular emblem, repeated rules, and a divided footer create firmer alignment; the left title mass is balanced by the right fee block.",
            "An observer may prefer asymmetry to formal equilibrium; the sampled ink centroid is not perceptual balance.");
        notebook.Add(ReadWitness.Create(new Carrier(), Group(aMeasured, bMeasured), Parts(balance)[0],
            Group(Parts(balance)[1], Parts(balance)[2]),
            Words("Metric/judgment tension: A's unweighted occupied centroid is closer to the horizontal center, while the observer prefers B's formal alignment. Neither reading cancels the other."), provenance));
        Compare("Whitespace", "Unoccupied support around and within source forms; dark background also counts.", a, b,
            8, 7, 9, 9, 8, 9,
            "Generous margins and breathing room, with a few deliberately scattered accents.",
            "The open diamond interior and broad quiet regions give the dominant forms separation.",
            "More empty area is not automatically better; sparse layouts can feel remote or unfinished.");
        Compare("Eye-motion hypothesis", "A selected sequence of salience and spatial proximity, not recorded gaze.", a, b,
            7, 6, 8, 8, 7, 9,
            "Likely flower → title → circular badge → left details, with the gold dot offering a distracting branch.",
            "Likely lime emblem → heavy title → lower-left details → fee; lime repeated below connects distant regions.",
            "Either headline can win the first fixation; the small top line is probably late. No eye-tracking was performed.");
        Compare("Color harmony", "Palette relationships and repetition over selected colored sources.", aPalette, bPalette,
            9, 8, 9, 8, 7, 9,
            "Cream, coral, gold, and plum form a warm palette; plum links title, small accents, and badge.",
            "Near-black, off-white, and acid lime are cohesive and high-contrast; the lime is intentionally abrasive.",
            "A restrained palette can still feel harsh; warmth and harmony are observer- and display-dependent.");
        Compare("Form and type cohesion", "Repeated curvature or angularity and their relationship to letterforms.", Group(aLogo, aType), Group(bLogo, bType),
            8, 7, 9, 9, 8, 9,
            "Round lobes, circular badge, dots, and expressive serif type reinforce an organic character.",
            "Diagonal rails, straight rules, condensed uppercase type, and column divisions reinforce a hard geometric character.",
            "A's round logo also permits a flower or eye read; B's emblem can read as generic corporate geometry rather than distinctive authorship.");

        // Eye paths themselves are ordered Gojo source references. Alternative
        // first fixation and distractor branches are kept as unresolved readings.
        notebook.Add(ReadWitness.Create(new Carrier(), a,
            Group(aLogo, aLayers[6], aLayers[10], aLayers[8], aLayers[9]),
            Group(aLogo, aLayers[6], aLayers[10], aLayers[8], aLayers[9]),
            Group(Group(aLayers[6], aLogo), aLayers[3], aLayers[5]), provenance));
        notebook.Add(ReadWitness.Create(new Carrier(), b,
            Group(bLogo, bLayers[4], bLayers[6], bLayers[7], bLayers[9]),
            Group(bLogo, bLayers[4], bLayers[6], bLayers[7], bLayers[9]),
            Group(bLayers[4], bLogo), provenance));

        // Trait interpretations are attached to actual selected source relations.
        // The evidence is shared across contexts. A new audience changes the
        // focus/weights rather than retroactively changing the poster observations.
        var traits = new List<Relation>();
        var aReads = new List<Relation>();
        var bReads = new List<Relation>();
        void Trait(string name, string footing, IElement aSource, IElement bSource,
            long aGrade, long bGrade, string aEvidence, string bEvidence)
        {
            var basis = Criterion(name, footing, aSource, bSource);
            traits.Add(basis);
            aReads.Add(Judge(basis, aSource, aGrade, aGrade - 1, aGrade + 1, aEvidence,
                "A different observer or audience may shift this interpretation by about one scale step; cultural calibration is open."));
            bReads.Add(Judge(basis, bSource, bGrade, bGrade - 1, bGrade + 1, bEvidence,
                "A different observer or audience may shift this interpretation by about one scale step; cultural calibration is open."));
        }
        Trait("Welcoming", "Warm light ground and soft contours versus dark ground and hard edges.", aPalette, bPalette, 9, 4,
            "Cream ground and rounded coral forms suggest approachability.", "Dark ground and sharp lime forms suggest distance and intensity.");
        Trait("Playful", "Curved lobes, small displaced accents, and typographic variation versus repeated straight geometry.", a, b, 8, 3,
            "The flower/eye ambiguity and detached dots allow a playful read.", "The straight emblem and strict footer feel more controlled than playful.");
        Trait("Technical precision", "Alignment, repeated rules, sharp joins, and condensed type as a declared cultural interpretation.", a, b, 4, 9,
            "Organic forms dominate the engineered grid.", "Crisp diagonals and repeated alignments evoke engineered systems.");
        Trait("Institutional gravity", "Visual restraint and disciplined structure as a declared conference-context preference.", aType, bType, 4, 7,
            "Expressive serif type and the rounded badge feel informal.", "Heavy condensed capitals and restrained palette feel authoritative; acid lime may feel promotional.");
        Trait("Expressive personality", "Strength of formal character, not a claim of originality or authorship.", Group(aLogo, aPalette, aType), Group(bLogo, bPalette, bType), 8, 7,
            "The warm oversized bloom and serif pairing have a clear personality.", "The acid geometry is assertive, although its corporate vocabulary is familiar.");
        Trait("Local intimacy", "Handmade/neighborly association as an observer-supplied contextual reading.", a, b, 9, 3,
            "Warm stock-like background and soft forms suggest a small social gathering.", "The dark structured treatment resembles a larger branded production.");
        Trait("Information legibility", "Typographic size, contrast, grouping and separation; content meaning is excluded.", aType, bType, 8, 9,
            "Large lettering is clear, but the detached circular badge splits the details.", "Large high-contrast type and the divided footer make scanning straightforward.");
        notebook.Add(Group(Group(traits.ToArray()), Group(aReads.ToArray()), Group(bReads.ToArray())));

        report.Add("## Shared visual interpretations\n\n| Interpretation | A /10 | B /10 |\n|---|---:|---:|");
        for (var i = 0; i < traits.Count; i++)
        {
            EncodedTextSurface.TryRead(Parts(traits[i])[2], out var name);
            report.Add($"| {name} | {Grade(aReads[i], 0).Ticks} | {Grade(bReads[i], 0).Ticks} |");
        }
        report.Add("## Suitability\n\nContext weights sum to 100. Each result is a Gojo weighted sum read at tenths; the discarded fraction stays in its prediction residue. Brackets propagate the declared alternative feature reads, not population uncertainty.\n\n| Context | A /10 | B /10 | Weight basis |\n|---|---:|---:|---|");

        void Context(string name, string rationale, params long[] weights)
        {
            var weightAxis = new Carrier();
            var total = new Carrier();
            var bindings = weights.Select((weight, i) =>
            {
                total.Add((ulong)weight);
                return (IElement)new Relation(traits[i], new CarrierLocation(weightAxis, weight));
            }).ToArray();
            if (total.Value != 100 || bindings.Length != traits.Count)
                throw new InvalidOperationException("Incomplete context calibration.");
            var profile = Group(Words(name), Group(bindings), Words(rationale),
                Relation.Segment(weightAxis, 0, 100), total.ValueSlot);
            var scoreAxis = new Carrier();
            var aScore = Score(profile, aReads, scoreAxis);
            var bScore = Score(profile, bReads, scoreAxis);
            var aParts = Parts(aScore);
            var bParts = Parts(bScore);
            var difference = Predict(Equation.Difference(scoreAxis, scoreAxis, scoreAxis),
                new Relation(Result((Relation)bParts[0]), Result((Relation)aParts[0])), scoreAxis);
            notebook.Add(ReadWitness.Create(new Carrier(), Group(a, b), profile, Group(aScore, bScore, difference),
                Group(aParts[1], aParts[2], bParts[1], bParts[2]), Group(aReads.Concat(bReads).Cast<IElement>().ToArray())));
            string Display(IElement[] parts) => $"{Result((Relation)parts[0]).Ticks / 10.0:0.0} [{Result((Relation)parts[1]).Ticks / 10.0:0.0}–{Result((Relation)parts[2]).Ticks / 10.0:0.0}]";
            report.Add($"| {name} | {Display(aParts)} | {Display(bParts)} | {rationale} Weights: {string.Join('/', weights)}. |");
        }
        Context("Children's event", "Assume a welcoming, playful, family-oriented event.", 30, 35, 0, 0, 10, 10, 15);
        Context("Technology trade show", "Prioritize technical precision, legibility, and professional presence.", 5, 0, 40, 20, 15, 0, 20);
        Context("Oil industry meeting", "Assume a professional operational/engineering meeting; prioritize gravity, clarity and precision.", 5, 0, 25, 45, 5, 0, 20);
        Context("Art show", "Assume a broad independent visual-art show that values expressive personality and invitation.", 10, 5, 10, 0, 55, 5, 15);
        Context("Small-town bake sale", "Prioritize neighborly intimacy, approachability and easy reading.", 35, 10, 0, 0, 5, 35, 15);
        Context("Art show — digital/minimalist alternative", "Sensitivity check: prioritize technical clarity, graphic discipline, and expression; keep the poster reads unchanged.", 0, 0, 25, 10, 45, 0, 20);

        report.Add("No claim is made that Gojo learned these aesthetic associations or audience preferences. It preserves the assumptions, performs the conditional comparison, and exposes alternate readings. The two art-show profiles deliberately test whether a winner changes when the context changes.");
        var root = Group(notebook.ToArray());
        File.WriteAllText(Path.Combine(output, "assessment.snapshot.gojo.json"),
            JsonSerializer.Serialize(GojoEncodedSnapshot.Capture(root), new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(output, "assessment.md"), string.Join("\n\n", report).Replace("|\n\n|", "|\n|"));
        Console.WriteLine(string.Join("\n", report));
    }

    private static CarrierLocation Grade(Relation judgment, int position)
    {
        ReadWitness.TryRead(judgment, out _, out _, out var result, out _, out _);
        var parts = Parts((Relation)result);
        if (position == 0) return (CarrierLocation)parts[0];
        var band = (Relation)parts[1];
        return ((Relation)(position == 1 ? band.Left : band.Right)).LeftLocation;
    }

    private static Relation Score(Relation profile, IReadOnlyList<Relation> judgments, Carrier scoreAxis)
    {
        var bindings = Parts((Relation)Parts(profile)[1]);
        var reads = new List<IElement>();
        for (var position = 0; position < 3; position++)
        {
            var ports = new List<Relation>();
            var input = new List<IElement>();
            foreach (Relation binding in bindings)
            {
                var judgment = judgments.Single(j =>
                {
                    ReadWitness.TryRead(j, out _, out var basis, out _, out _, out _);
                    return ReferenceEquals(basis, binding.Left);
                });
                var value = Grade(judgment, position);
                var weight = (CarrierLocation)binding.Right;
                ports.Add(Equation.WeightedPort(value.Carrier, 0, 1, 0, weight.Ticks));
                input.Add(value);
            }
            var raw = new Carrier();
            var sum = Predict(Equation.Sum(raw, ports.ToArray()), StructuralPattern.Chain(input), raw);
            var projection = Predict(Equation.Affine(raw, 0, 10, scoreAxis, 0, 1), Result(sum), scoreAxis);
            // Keep the contributing sum and its contextual read attached to the
            // projected witness's provenance instead of reducing to a number.
            Equation.TryReadPredictionWitness(projection, out var supplied, out var equation,
                out var answer, out var residue, out var provenance);
            reads.Add(ReadWitness.Create(new Carrier(), supplied, equation, answer, residue,
                Group(provenance, sum, profile, Group(judgments.Cast<IElement>().ToArray()))));
        }
        return Group(reads.ToArray());
    }

    private static Relation Measure(Relation document, string png, string label, List<string> report, Relation provenance)
    {
        using var bitmap = SKBitmap.Decode(png);
        var background = bitmap.GetPixel(0, 0);
        var blank = new Carrier();
        var occupied = new Carrier();
        var total = new Carrier();
        var xMoment = new Carrier();
        var yMoment = new Carrier();
        // The raster adapter supplies observed sample categories and coordinates;
        // Gojo carriers account for their count and moments. One sample per 20px cell.
        for (var y = 10; y < bitmap.Height; y += 20)
        for (var x = 10; x < bitmap.Width; x += 20)
        {
            total.Add(1);
            if (bitmap.GetPixel(x, y) == background) blank.Add(1);
            else { occupied.Add(1); xMoment.Add((ulong)x); yMoment.Add((ulong)y); }
        }
        var proportion = new Carrier();
        var coverage = Predict(Equation.Affine(blank, 0, (long)total.Value, proportion, 0, 1000), blank.ValueSlot, proportion);
        var xCenter = Predict(Equation.Affine(xMoment, 0, (long)occupied.Value, X, 0, 1), xMoment.ValueSlot, X);
        var yCenter = Predict(Equation.Affine(yMoment, 0, (long)occupied.Value, Y, 0, 1), yMoment.ValueSlot, Y);
        report.Add($"Poster {label}: {blank.Value}/{total.Value} grid samples equal the background, about {Result(coverage).Ticks / 10.0:0.0}% background coverage. Unweighted occupied-sample centroid: ({Result(xCenter).Ticks}, {Result(yCenter).Ticks}) on the 1080×1440 sheet. This is a coarse geometric proxy, not perceptual balance, semantic whitespace, or gaze data.");
        return ReadWitness.Create(new Carrier(), document,
            Group(Relation.Segment(X, 0, bitmap.Width), Relation.Segment(Y, 0, bitmap.Height), Relation.Point(new Carrier(), 20)),
            Group(coverage, xCenter, yCenter), UnitShape.DescriptorSlot(0),
            Group(provenance, blank.ValueSlot, occupied.ValueSlot, total.ValueSlot, xMoment.ValueSlot, yMoment.ValueSlot));
    }
}

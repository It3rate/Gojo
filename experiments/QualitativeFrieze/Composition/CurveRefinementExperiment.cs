using System.Diagnostics;
using System.IO.Compression;
using System.Numerics;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Shapes = Gojo.Experiments.QualitativeExecution.CurveShapeQualities;
using Scenes = Gojo.Experiments.QualitativeFrieze.Composition.CurveEvidenceScene;
using Pack = Gojo.Experiments.QualitativeFrieze.Composition.CurveRefinementMessage;
using Stats = Gojo.Experiments.QualitativeExecution.StatisticalEvidence;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Frozen-pool profile-question ablation and endpoint-scaffold
/// reconstruction. Priors are authored defaults; no channel is learned here.</summary>
public static class CurveRefinementExperiment
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public sealed record Model(string Name, Scenes.Scene Scene);
    public sealed record Run(Pack.Packet Packet, CurveContextRead.Context Context, IReadOnlyList<Model> Models,
        IReadOnlyList<Pack.Assessment> BaseChecks, IReadOnlyList<Pack.Assessment> FullChecks,
        Relation BaseState, Relation FullState, Stats.Distribution Prior, Stats.Distribution BaseDistribution, Stats.Distribution FullDistribution,
        int BaseSelected, int FullSelected, int Ties, Relation ClosureGoal, Relation Preferences, Relation Surface);

    public static Run Calculate(CurveRefinementCorpus.Case source)
    {
        var packet = Pack.Publish(source);
        var context = CurveContextRead.Read(packet.Base, source.ProvisionalInterior, source.RidgeFocus, source.OfferBridge,
            source.Similarity ? packet.Similarity : null);
        var models = new List<Model>(); var aliases = new List<IElement>(); var signatures = new Dictionary<string, Scenes.Scene>();
        var sites = packet.Base.Spans.SelectMany(span => new[] { span.From, span.To }).Distinct(FieldNative.Participants.Instance)
            .Select((site, i) => (site, i)).ToDictionary(row => row.site, row => row.i, FieldNative.Participants.Instance);
        void Add(string name, Scenes.Scene candidate)
        {
            if (candidate.InteriorSide is null && context.ProposedInteriorSide is int side)
                candidate = new(candidate.Frame, candidate.Sections, candidate.Assumed, Scenes.Interior(candidate.Frame, side));
            var key = string.Join(";", candidate.Reads.Select(read => sites[read.Start] + ":" + sites[read.End] + ":" +
                string.Join("/", read.Controls.Select(v => v.X + "," + v.Y))));
            if (signatures.TryGetValue(key, out var previous)) { aliases.Add(new Relation(candidate.Surface, previous.Surface)); return; }
            signatures.Add(key, candidate); models.Add(new(name, candidate));
        }
        Add("Earlier short-handle receiver", Scenes.Propose(packet.Base, 1));
        Add("Thin relative handles", Pack.Propose(packet, Pack.Ratio.Of(1, 4), Pack.Ratio.Of(1, 4)));
        Add("Circular-like relative handles", Pack.Propose(packet, Pack.Ratio.Of(5, 9), Pack.Ratio.Of(5, 9)));
        Add("Full relative handles", Pack.Propose(packet, Pack.Ratio.Of(3, 4), Pack.Ratio.Of(3, 4)));
        Add("Longer exit participation", Pack.Propose(packet, Pack.Ratio.Of(1, 3), Pack.Ratio.Of(2, 3)));
        Add("Longer entrance participation", Pack.Propose(packet, Pack.Ratio.Of(2, 3), Pack.Ratio.Of(1, 3)));
        var adaptive = Pack.Propose(packet, Pack.Ratio.Of(5, 9), Pack.Ratio.Of(5, 9), adaptive: true);
        Add("Receiver fitted to public profile bands", adaptive);
        Add("Straight chord alternative", Pack.Propose(packet, Pack.Ratio.Of(1, 2), Pack.Ratio.Of(1, 2), straight: true));
        if (packet.Spans.Any(report => Pack.ReadFact(report.Progress) == true))
            Add("Collinear reversal alternative", Pack.Propose(packet, Pack.Ratio.Of(1, 2), Pack.Ratio.Of(1, 2), fold: true));
        if (source.OfferBridge && context.Gap.Incoming is not null && context.Gap.Outgoing is not null)
            Add("Profile construction with an assumed tangent bridge", Bridge(packet, adaptive, context.Gap));
        var baseline = models.Select(model => Pack.Assess(packet, model.Scene, false)).ToArray();
        var refined = models.Select(model => Pack.Assess(packet, model.Scene)).ToArray();
        var closureGoal = source.RequestClosure && context.Gap.From is not null && context.Gap.To is not null ?
            S(new Relation(context.Gap.From, context.Gap.To)) : StructuralConstraint.Open(packet.Base.Surface);
        var defaults = models.Select(model => DefaultWeight(packet, context, closureGoal, model.Scene)).ToArray();
        var preferences = S(defaults.Select(value => (IElement)value.Surface).ToArray());
        var basis = new Relation(new Relation(packet.Surface, context.Surface), new Relation(closureGoal, preferences));
        var frame = new Stats.Frame(basis, models.Select(model => (IElement)model.Scene.Surface).ToArray());
        var weights = defaults.Select(value => value.Weight).ToArray();
        var prior = frame.Prior(models.Select((model, i) => new Relation(model.Scene.Surface, frame.Weight(weights[i], weights.Sum(), basis))).ToArray());
        var baseUpdate = Update(frame, prior, basis, models, baseline);
        var fullUpdate = Update(frame, prior, basis, models, refined);
        var baseState = baseUpdate.State; var fullState = fullUpdate.State;
        var baseRead = Stats.Read(baseState); var fullRead = Stats.Read(fullState);
        int Select(Stats.Distribution read) => Enumerable.Range(0, models.Count).OrderByDescending(i => Weight(read, models[i]),
            ShapeEvidenceWeighting.RatioComparer.Instance).ThenBy(i => i).First();
        var selected = Select(fullRead); var best = Weight(fullRead, models[selected]);
        var surface = new Relation(new Relation(packet.Surface, context.Surface),
            new Relation(S(models.Select(model => (IElement)model.Scene.Surface).ToArray()),
                new Relation(new Relation(baseState, fullState), new Relation(new Relation(baseUpdate.Receipt, fullUpdate.Receipt),
                    new Relation(new Relation(closureGoal, preferences), S(aliases.ToArray()))))));
        return new(packet, context, models, baseline, refined, baseState, fullState, Stats.Read(prior), baseRead, fullRead,
            Select(baseRead), selected, models.Count(model => Weight(fullRead, model) == best), closureGoal, preferences, surface);
    }

    private sealed record WeightedUpdate(Relation State, Relation Receipt);
    private static WeightedUpdate Update(Stats.Frame frame, Relation prior, Relation basis, IReadOnlyList<Model> models, IReadOnlyList<Pack.Assessment> checks)
    {
        var focus = new Relation(basis, S(checks.Select(check => (IElement)check.Surface).ToArray()));
        if (!checks.Any(check => check.Matches + check.Conflicts > 0)) return new(prior, new Relation(focus, StructuralConstraint.Open()));
        var calibration = CompatibilityEvidence.Calibrate(frame, 15, 16); var likelihoods = new List<IElement>(); var receipts = new List<IElement>();
        foreach (var (model, i) in models.Select((model, i) => (model, i)))
        {
            var check = checks[i];
            var assessment = CompatibilityEvidence.Assess(basis, model.Scene.Surface,
                S(check.Checks.SelectMany(group => E(group.Supported)).ToArray()),
                S(check.Checks.SelectMany(group => E(group.Contradicted)).ToArray()),
                S(check.Checks.SelectMany(group => E(group.Open)).ToArray()));
            if (!CompatibilityEvidence.TryReadLikelihood(assessment, calibration, out var weight, out var receipt)) throw new InvalidOperationException("Compatibility channel is open.");
            likelihoods.Add(new Relation(model.Scene.Surface, weight));
            receipts.Add(receipt);
        }
        var question = new Stats.Question(basis, new Relation(focus, StructuralConstraint.Open()), basis);
        var offer = Stats.Offer(question.Observe(new Placeholder()), S(likelihoods.ToArray()), S(), S(), calibration);
        var result = Stats.Update(prior, offer);
        if (!result.Accepted) throw new InvalidOperationException("The estimated profile event was not accepted.");
        return new(result.State, new Relation(focus, new Relation(S(receipts.ToArray()), result.Surface)));
    }

    private sealed record Preference(int Weight, Relation Surface);
    private static Preference DefaultWeight(Pack.Packet packet, CurveContextRead.Context context, Relation closureGoal, Scenes.Scene scene)
    {
        var reads = scene.Reads;
        var qualities = reads.Select(read => Shapes.Read(read, packet.Similarity)).ToArray();
        var applications = new List<IElement>(); var f = packet.Base.Frame;
        void Record(IElement premise, bool? applied, Pack.Ratio configured, Pack.Ratio actual) => applications.Add(
            new Relation(new Relation(scene.Surface, premise),
                new Relation(Pack.NativeRatio(f, configured), new Relation(Pack.Fact(applied, premise), Pack.NativeRatio(f, actual)))));
        // Each seed is applied once to a candidate, not once per repetition.
        // The resulting native prior partition retains this selected context.
        var retraces = qualities.Any(read => read.Retracing == true);
        var weight = retraces ? 19 : 20;
        var progress = S(qualities.Select(quality => (IElement)((Relation)((Relation)((Relation)quality.Surface.Right).Right).Right).Right).ToArray());
        Record(progress, retraces ? true : qualities.All(read => read.Retracing == false) ? false : null,
            Pack.Ratio.Of(19, 20), retraces ? Pack.Ratio.Of(19, 20) : Pack.Ratio.Of(1, 1));
        var convexDefault = context.ProposedInteriorSide is int side && reads.Any(read => read.Straight == false) &&
            !reads.Any(read => Curve.Convexity(read, side) == -1);
        if (convexDefault) weight *= 2;
        Record(context.InteriorPremise, context.ProposedInteriorSide is null ? null : convexDefault,
            Pack.Ratio.Of(2, 1), Pack.Ratio.Of(convexDefault ? 2 : 1, 1));
        var closedGoal = Closed(closureGoal) && E(closureGoal).Length != 0 && scene.Closed;
        if (closedGoal) weight *= 3;
        Record(closureGoal, !Closed(closureGoal) ? null : closedGoal, Pack.Ratio.Of(3, 1), Pack.Ratio.Of(closedGoal ? 3 : 1, 1));
        var gapDefault = false;
        if (Shapes.IsSimilarityBasis(packet.Base.Frame, packet.Similarity) && context.Gap.SupportSquared > 0)
        {
            var bridge = E(scene.Assumed).Length != 0;
            gapDefault = context.Gap.Relatable == true && context.Gap.GapSquared * 4 <= context.Gap.SupportSquared && bridge ||
                (context.Gap.Relatable == false || context.Gap.GapSquared > context.Gap.SupportSquared) && !bridge;
            if (gapDefault) weight *= 2;
        }
        Record(new Relation(context.Gap.Surface, Pack.NativeRatio(f, Pack.Ratio.Of(1, 4))),
            context.Gap.SupportSquared == 0 || !Shapes.IsSimilarityBasis(f, packet.Similarity) ? null : gapDefault,
            Pack.Ratio.Of(2, 1), Pack.Ratio.Of(gapDefault ? 2 : 1, 1));
        return new(weight, new Relation(new Relation(scene.Surface, context.Surface), S(applications.ToArray())));
    }

    private static Scenes.Scene Bridge(Pack.Packet packet, Scenes.Scene scene, CurveContextRead.GapRead gap)
    {
        var f = packet.Base.Frame; var incoming = gap.Incoming!; var outgoing = gap.Outgoing!;
        f.Vector(incoming.End, out var a); f.Vector(outgoing.Start, out var b); var delta = b - a;
        var d0 = Scenes.Direction(incoming.EndHeading, f); var d1 = Scenes.Direction(outgoing.StartHeading, f);
        var extent = BigInteger.Max(1, BigInteger.Max(BigInteger.Abs(delta.X), BigInteger.Abs(delta.Y)) / 2);
        var c1 = a + new Curve.Vector(d0.X * extent, d0.Y * extent); var c2 = b - new Curve.Vector(d1.X * extent, d1.Y * extent);
        var bridge = f.Cubic(gap.From!, gap.To!, incoming.End, f.Word((int)c1.X, (int)c1.Y), f.Word((int)c2.X, (int)c2.Y), outgoing.Start);
        return new(f, scene.Sections.Append(bridge), S(bridge), scene.Interior);
    }

    public static int RunExperiment(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "curve-refinement"); var limit = int.MaxValue;
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else if (args[i] == "--limit" && i + 1 < args.Length) limit = int.Parse(args[++i]);
            else throw new ArgumentException("Usage: --curve-refinement [--output DIRECTORY] [--limit COUNT]");
        Directory.CreateDirectory(output); Directory.CreateDirectory(Path.Combine(output, "native"));
        var watch = Stopwatch.StartNew(); var cases = CurveRefinementCorpus.Cases().Take(limit).ToArray();
        var examples = new List<CurveEvidenceGallery.Example>(); var results = new List<object>();
        var changed = 0; var reduced = 0; var raised = 0; var same = 0; var roundTrips = 0; var interior = 0; var tied = 0;
        foreach (var source in cases)
        {
            var run = Calculate(source); var chosen = run.Models[run.FullSelected]; var old = run.Models[run.BaseSelected];
            var oldScore = run.FullChecks[run.BaseSelected]; var newScore = run.FullChecks[run.FullSelected];
            if (run.BaseSelected != run.FullSelected) changed++;
            if (newScore.Conflicts < oldScore.Conflicts) reduced++; else if (newScore.Conflicts > oldScore.Conflicts) raised++; else same++;
            if (run.Context.ProposedInteriorSide is not null) interior++; if (run.Ties > 1) tied++;
            var packet = run.Packet; var f = packet.Base.Frame;
            var sourceBasis = source.Similarity ? Shapes.SimilarityBasis(source.Source.Frame) : null;
            var sourceQualities = source.Source.Reads.Select(read => Shapes.Read(read, sourceBasis)).ToArray();
            var reference = Shapes.Read(f.Read(packet.Reference), packet.Similarity);
            var candidates = run.Models.Select((model, i) =>
            {
                var modelReads = model.Scene.Reads;
                var q = modelReads.Select(read => Shapes.Read(read, packet.Similarity)).ToArray();
                var joins = packet.Joins.Select(report =>
                {
                    var a = Pack.MatchRead(packet.Base, modelReads, report.Join.Incoming, model.Scene.Frame);
                    var b = Pack.MatchRead(packet.Base, modelReads, report.Join.Outgoing, model.Scene.Frame);
                    return a is null || b is null ? null : Shapes.Join(a, b, packet.Similarity);
                }).ToArray();
                var strengths = string.Join(", ", q.Where((read, j) => modelReads[j].Straight == false).Select(read => read.Strength?.ToString() ?? "Open").Distinct());
                var offsets = string.Join(", ", modelReads.Select((read, j) => Offset(q[j].Strength, CircularReference(read, packet))).Where(value => value != "Open").Distinct());
                var interpretation = strengths.Length == 0 ? "No curved passage in this candidate." :
                    $"Sampled curve strength: {strengths}. Circular-like fullness offset: {(offsets.Length == 0 ? "not applicable or open" : offsets)}.";
                return new CurveEvidenceGallery.Candidate("H" + (i + 1), model.Name, Scenes.Drawing(model.Scene,
                    "The receiver uses public cue bands and its own finite control construction."),
                    Weight(run.Prior, model).ToString(), Weight(run.FullDistribution, model).ToString(),
                    interpretation + " Weight is conditional on the displayed program pool.",
                    new { BaseQuestionWeight = Weight(run.BaseDistribution, model).ToString(), run.FullChecks[i].Matches, run.FullChecks[i].Conflicts,
                        run.FullChecks[i].Unresolved, AssumedSections = E(model.Scene.Assumed).Length, ClosedSiteCycle = model.Scene.Closed,
                        SourceProfileError = ProfileError(sourceQualities, q),
                        Profile = q.Take(packet.Spans.Count).Select((read, j) => new { Span = j + 1,
                            Strength = read.Strength?.ToString() ?? "Open", CircularLikeOffset = Offset(read.Strength, CircularReference(modelReads[j], packet)),
                            Retracing = read.Retracing?.ToString() ?? "Open" }).ToArray(),
                        Interior = model.Scene.InteriorSide?.ToString() ?? "Open",
                        TangentCurvatureBalance = joins.Select(join => new { Balanced = join?.EqualCurvature?.ToString() ?? "Open",
                            OpposedBending = join?.OpposedBending?.ToString() ?? "Open" }).ToArray(),
                        InteriorStatus = packet.Base.InteriorSide is not null ? "Observed publication" : run.Context.ProposedInteriorSide is not null ? "Provisional chord-parent premise" : "Open" });
            }).ToArray();
            var evidence = new List<CurveEvidenceGallery.Evidence>
            {
                new("Three-part profile", source.Profiles ? "Exposed as signed ratio bands at quarter, half and three-quarter parameter partitions" : "Withheld control",
                    "Detached native partitions; private controls stay with the source", "Finite cue resolution: excursion bands of width 1/24",
                    "This finite profile is neither maximum curvature nor an arc-length sampling. A stronger S is visible even when its central sample lies on the chord."),
                new("Circular-like reference", "A relative fullness zero, not an exact-circle claim", "Native quarter-turn and half-turn constructions under a selected similarity premise",
                    reference.Strength?.ToString() ?? "Open", "Positive offset means greater sampled excursion than the applicable quarter/half-turn exemplar. S shapes keep a separate strength read; one number is not a universal shape identity."),
                new("Tangent balance", "Directional balance and local squared-curvature balance are separate",
                    "Endpoint tangent participation plus actual shared-site joins", "Coarse directional and curvature-share bands",
                    "Equal local curvature does not prove whole-curve mirror symmetry. Opposed bending remains a separate report."),
                new("Interior premise", packet.Base.InteriorSide is not null ? "Published orientation retained" : run.Context.ProposedInteriorSide is not null ? "Coarse chord-parent supplies a provisional interior" : "Interior remains open",
                    "Public endpoint topology and retained coarse hypothesis", "Default premise; no additional observation event", "Observed message, coarse hypothesis and conditional convexity remain separate."),
                new("Focus", source.RidgeFocus ? "Constructed point/centreline with support outline" : "Boundary/enclosure context",
                    "Explicit question over the same endpoint scaffold", "A construction hypothesis, not a medial-axis theorem",
                    "Identical outlines can afford enclosure and stroke/ribbon interpretations. The context does not establish how the object was really constructed."),
                new("Question ablation", $"Withholding profile questions selects H{run.BaseSelected + 1}; full questions select H{run.FullSelected + 1}",
                    "Identical frozen program pool and prior in both rankings", $"Exposed conflicts: {oldScore.Conflicts} → {newScore.Conflicts}",
                    "The adaptive proposal uses public profile hints before the pool is frozen. This is a ranking-question ablation, not a blind no-profile generator comparison.")
            };
            if (source.OfferBridge) evidence.Add(new("Gap completion default", run.Context.Gap.Relatable?.ToString() ?? "Open",
                "Endpoint tangents and squared gap/support relation", $"{run.Context.Gap.GapSquared}/{run.Context.Gap.SupportSquared}",
                "Weak authored preferences only. Large gaps, offsets and S-demand do not make completion impossible; an assumed bridge stays dashed."));
            var note = source.FlowingPrior ? "The earlier source is reused. Explicit straight reports make this round's generated constructions coincide; duplicate models receive no additional prior mass." : source.Note;
            if (source.ProvisionalInterior) note += " A separate chord-parent hypothesis can supply a provisional interior premise in this round.";
            examples.Add(new(source.Id, source.Name, note, Scenes.Drawing(source.Source, "Authored source before publication."),
                Scenes.Drawing(run.Context.Coarse, source.RidgeFocus ? "Constructed centre read with its boundary support." : "Coarse chord-parent; any gap bridge is assumed."),
                candidates, evidence, new { source.Profiles, source.Similarity, source.Control, source.RidgeFocus,
                    run.Ties, Baseline = "H" + (run.BaseSelected + 1), Selected = "H" + (run.FullSelected + 1),
                    LegacyProfileError = ProfileError(sourceQualities, run.Models[0].Scene.Reads.Select(read => Shapes.Read(read, packet.Similarity)).ToArray()),
                    SelectedProfileError = ProfileError(sourceQualities, chosen.Scene.Reads.Select(read => Shapes.Read(read, packet.Similarity)).ToArray()),
                    PublicBands = packet.Spans.Select((span, i) => new { Span = i + 1,
                        Entrance = Pack.DescribeBand(span.Entrance, f), Exit = Pack.DescribeBand(span.Exit, f),
                        Excursions = span.Samples.Select(sample => Pack.DescribeBand(sample, f)).ToArray() }).ToArray(),
                    PublicJoinBands = packet.Joins.Select(report => new { Balance = Pack.DescribeBand(report.Balance, f),
                        OpposedBending = Pack.ReadFact(report.Opposed)?.ToString() ?? "Open" }).ToArray(),
                    NativePacket = "native/" + source.Id + ".gojo.json.gz",
                    Limits = "Known endpoints supplied. Authored construction/search policies and estimated channels; no sketch parsing or model training." }, "H" + (run.FullSelected + 1)));
            var correspondence = S(source.Source.Reads.Select((read, i) => (IElement)new Relation(read.Section,
                new Relation(packet.Spans[i].Span.Role, new Relation(new Relation(read.Start, packet.Spans[i].Span.From),
                    new Relation(read.End, packet.Spans[i].Span.To))))).ToArray());
            var native = new Relation(source.Source.Surface, new Relation(correspondence, run.Surface));
            var states = new Relation(run.BaseState, run.FullState);
            var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("curve-refinement-states", states));
            if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(text), out var restored) || restored.Root is not Relation copy ||
                !EqualShares(run.BaseState, (Relation)copy.Left) || !EqualShares(run.FullState, (Relation)copy.Right))
                throw new InvalidOperationException("Independent statistical state transport changed the selected shares.");
            roundTrips++;
            Save(Path.Combine(output, "native", source.Id + ".gojo.json.gz"), native);
            results.Add(new { source.Id, source.Name, Candidates = run.Models.Count, run.BaseSelected, run.FullSelected, run.Ties,
                BaseConflicts = oldScore.Conflicts, FullConflicts = newScore.Conflicts,
                LegacyProfileError = ProfileError(sourceQualities, run.Models[0].Scene.Reads.Select(read => Shapes.Read(read, packet.Similarity)).ToArray()),
                SelectedProfileError = ProfileError(sourceQualities, chosen.Scene.Reads.Select(read => Shapes.Read(read, packet.Similarity)).ToArray()),
                ProvisionalInterior = run.Context.ProposedInteriorSide, source.RidgeFocus, source.Profiles, source.Similarity,
                Events = run.FullDistribution.Events.Length });
            Console.WriteLine($"{source.Id}: {run.Models.Count} programs; H{run.FullSelected + 1}; conflicts {oldScore.Conflicts}->{newScore.Conflicts}");
        }
        var summary = new { Schema = "curve-refinement-study-v1", Cases = cases.Length, CandidateRealizations = examples.Sum(row => row.Candidates.Count),
            ChangedSelections = changed, ReducedExposedConflicts = reduced, IncreasedExposedConflicts = raised, UnchangedExposedConflicts = same,
            ProvisionalInteriorCases = interior, TiedChoices = tied, NativeRoundTrips = roundTrips,
            SecondsBeforeGallery = watch.Elapsed.TotalSeconds,
            Scope = "Profile and join refinement on supplied endpoint scaffolds. Authored sources, receiver recipes and weak defaults; no imported drawings or trained likelihoods.",
            Dependence = "One pooled compatibility event per informative source. Coarse interior/defaults are prior context and never independent copies of their source evidence.",
            Metric = "Fullness, equal-axis diagonals and squared curvature are conditional on an explicit equal-reach right-angle similarity premise. Omitted premise leaves those reads open.",
            Reference = "5/9-handle circle-like quarter-turn exemplar; finite signed excursions, not exact circles or global curvature maxima.",
            Control = "Frozen-pool ranking with profile questions withheld, retaining the same prior. Adaptive proposal construction already uses public profile hints.",
            Results = results };
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, Json));
        CurveEvidenceGallery.Write(output, summary, examples);
        Console.WriteLine($"Curve refinement: {cases.Length} cases, {watch.Elapsed.TotalSeconds:0.00}s. {Path.GetFullPath(output)}");
        return 0;
    }

    private static Stats.Ratio Weight(Stats.Distribution read, Model model) => read.Rows.Single(row => ReferenceEquals(row.Model, model.Scene.Surface)).Probability!.Value;
    private static bool EqualShares(Relation a, Relation b) => Stats.Read(a).Rows.Select(row => row.Probability).SequenceEqual(Stats.Read(b).Rows.Select(row => row.Probability));
    private static string Offset(Shapes.Fraction? value, Shapes.Fraction? reference)
    {
        if (value is null || reference is null) return "Open";
        var offset = Pack.Ratio.Of(value.Numerator, value.Denominator) - Pack.Ratio.Of(reference.Numerator, reference.Denominator);
        return (offset.N > 0 ? "+" : "") + offset;
    }
    private static Shapes.Fraction? CircularReference(Curve.Read read, Pack.Packet packet)
    {
        if (!read.Complete || read.Straight != false || read.Inflected || read.BendSign is null || read.StartVector.Zero || read.EndVector.Zero) return null;
        var dot = Curve.Vector.Dot(read.StartVector, read.EndVector); var cross = Curve.Vector.Cross(read.StartVector, read.EndVector);
        var example = dot.IsZero ? packet.Reference : cross.IsZero && dot < 0 ? packet.HalfReference : null;
        return example is null ? null : Shapes.Read(packet.Base.Frame.Read(example), packet.Similarity).Strength;
    }
    private static object ProfileError(IReadOnlyList<Shapes.QualityRead> a, IReadOnlyList<Shapes.QualityRead> b)
    {
        var sum = Pack.Ratio.Of(0, 1); var count = 0;
        for (var i = 0; i < Math.Min(a.Count, b.Count); i++) for (var j = 0; j < Math.Min(a[i].Samples.Count, b[i].Samples.Count); j++)
        {
            if (a[i].Samples[j].SignedExcursion is not { } first || b[i].Samples[j].SignedExcursion is not { } second) continue;
            var d = Pack.Ratio.Of(first.Numerator, first.Denominator) - Pack.Ratio.Of(second.Numerator, second.Denominator);
            sum += Pack.Ratio.Of(BigInteger.Abs(d.N), d.D); count++;
        }
        return count == 0 ? new { Samples = 0, MeanAbsoluteExcursionDifference = "Open" } :
            new { Samples = count, MeanAbsoluteExcursionDifference = (sum / count).ToString() };
    }
    private static void Save(string path, Relation root)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("curve-refinement-v1", root));
        using var file = File.Create(path); using var gzip = new GZipStream(file, CompressionLevel.SmallestSize); using var writer = new StreamWriter(gzip); writer.Write(text);
    }
}

using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Structures;
using Gojo.Experiments.QualitativeExecution;
using static Gojo.Experiments.QualitativeFrieze.Composition.JointNative;
using Curve = Gojo.Experiments.QualitativeExecution.QualitativeCurve;
using Scenes = Gojo.Experiments.QualitativeFrieze.Composition.CurveEvidenceScene;
using Stats = Gojo.Experiments.QualitativeExecution.StatisticalEvidence;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Curve mechanics on a supplied anonymous endpoint scaffold.
/// Estimated compatibility is one pooled event, so tangents, bend and join
/// consequences from the same source are not multiplied as independent data.</summary>
public static class CurveEvidenceExperiment
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public sealed record Group(string Name, Relation Supported, Relation Contradicted, Relation Open, Relation Surface)
    { public int Matches => E(Supported).Length; public int Conflicts => E(Contradicted).Length; public int Unresolved => E(Open).Length; }
    public sealed record Assessment(IReadOnlyList<Group> Groups, Relation Surface)
    { public int Matches => Groups.Sum(group => group.Matches); public int Conflicts => Groups.Sum(group => group.Conflicts); public int Unresolved => Groups.Sum(group => group.Unresolved); }
    public sealed record Run(Scenes.Message Message, IReadOnlyList<Scenes.Scene> Candidates, IReadOnlyList<Assessment> Assessments,
        Stats.Distribution Prior, Stats.Distribution Posterior, Relation State, Relation Surface, int Selected, int Ties, bool Admitted);

    public static int RunExperiment(string[] args)
    {
        var output = Path.Combine("experiments", "QualitativeFrieze", "results", "curve-evidence");
        for (var i = 0; i < args.Length; i++)
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
            else throw new ArgumentException("Usage: --curve-evidence [--output DIRECTORY]");
        Directory.CreateDirectory(output); var watch = Stopwatch.StartNew();
        var sources = CurveEvidenceCorpus.Cases(); var examples = new List<CurveEvidenceGallery.Example>();
        var archive = new List<IElement>(); var states = new List<Relation>(); var results = new List<object>();
        var nativeChecks = 0; var smooth = 0; var cusps = 0; var corners = 0; var inflections = 0; var mixed = 0; var gaps = 0; var ties = 0;
        foreach (var source in sources)
        {
            var run = Calculate(source); var message = run.Message; var reads = source.Source.Reads;
            var envelope = Scenes.OuterRead(message);
            var coarse = source.OuterFocus && envelope.Complete ? envelope.Scene : Scenes.Propose(message, 0);
            var sourceSmooth = message.Joins.Count(join => Closed(join.Smooth) && E(join.Smooth).Length != 0);
            var sourceCusps = message.Joins.Count(join => Closed(join.Cusp) && E(join.Cusp).Length != 0);
            var sourceCorners = message.Joins.Count(join => Closed(join.Corner) && E(join.Corner).Length != 0);
            smooth += sourceSmooth; cusps += sourceCusps; corners += sourceCorners;
            inflections += reads.Count(read => read.Inflected);
            if (reads.Any(read => read.Straight == true) && reads.Any(read => read.Straight == false)) mixed++;
            if (!source.Source.Closed) gaps++; if (run.Ties > 1) ties++;
            var candidates = run.Candidates.Select((sample, i) => {
                var sampleReads = sample.Reads; var assessment = run.Assessments[i];
                return new CurveEvidenceGallery.Candidate("H" + (i + 1), i == 0 ? "Straight interpolation" : i == 1 ? "Short-handle tangent construction" :
                    source.OfferBridge ? "Longer curve with an assumed closing span" : "Longer or bowed construction",
                    Scenes.Drawing(sample, "Native construction sampled from the public scaffold; handle extent is a receiver choice."),
                    Weight(run.Prior, sample).ToString(), Weight(run.Posterior, sample).ToString(),
                    "A conditional choice within these three authored proposals. Handle controls were generated locally; no source handle or measured curvature was imported.",
                    new { assessment.Matches, assessment.Conflicts, assessment.Unresolved, AssumedSections = E(sample.Assumed).Length,
                        ClosedSiteCycle = sample.Closed, CurvedSections = sampleReads.Count(read => read.Straight == false),
                        RegularCertificates = sampleReads.Count(read => read.Regular), InflectedSections = sampleReads.Count(read => read.Inflected),
                        ConvexLocalSections = sampleReads.Count(read => Curve.Convexity(read, sample.InteriorSide) == 1),
                        ConcaveLocalSections = sampleReads.Count(read => Curve.Convexity(read, sample.InteriorSide) == -1),
                        InteriorSide = sample.InteriorSide?.ToString() ?? "Open",
                        Limits = "No global no-intersection, containment, curvature-continuity or exact-circle certificate." });
            }).ToArray();
            var evidence = run.Assessments[run.Selected].Groups.Select(group => new CurveEvidenceGallery.Evidence(group.Name,
                $"{group.Matches} supported · {group.Conflicts} contradicted · {group.Unresolved} open",
                "Native source descriptor compared with generated curve witnesses", "Pooled channel: hit 15/16, miss 1/16, unresolved 1/2",
                "These checks share their source. They participate in one mixture likelihood, not a product of independent feature likelihoods.")).ToList();
            evidence.Add(new("Whole-shape read", source.OuterFocus && envelope.Complete ? "Four outer tips support a weak diamond-like envelope" :
                "The endpoint scaffold is a coarser view than its sampled curved trace", "Selected endpoint extrema / existing scaffold",
                "Weak hypothesis; no extra Bayesian event", source.OuterFocus ?
                    $"The detailed boundary retains {envelope.Notches} locally concave corner readings. The proposed tip cycle does not replace that boundary or prove containment or equal side lengths." :
                    "Endpoint positions alone do not identify the curve between them."));
            evidence.Add(new("Interior-relative bend", message.InteriorSide is null ? "Interior side remains open" : "Oriented interior is explicitly supplied",
                "Independent boundary-context premise", "Derived read, not another event", "Reversal flips signed turning and interior side together; physical convexity is preserved."));
            if (source.OfferBridge) evidence.Add(new("Observed gap and completion", source.RequestClosure ? "A separate closure goal favours an assumed bridge" : "Closing bridge remains an alternative",
                "Observed endpoint identities plus explicit goal/prior", "Goal changes preference, not observation", "Dashed amber sections are proposed additions. The original open source and public message remain retained."));
            var selectedAssessment = run.Assessments[run.Selected];
            examples.Add(new(source.Id, source.Name, source.Note, Scenes.Drawing(source.Source, "Source polynomial construction, authored before analysis."),
                Scenes.Drawing(coarse, source.OuterFocus ? "Weak envelope from outer landmarks; dashed edges are inferred, not the literal boundary." :
                    "Coarse endpoint/chord scaffold: these directions are not curve tangents."), candidates, evidence,
                new { SourceSections = reads.Count, SourceCurved = reads.Count(read => read.Straight == false), SourceSmoothJoins = sourceSmooth,
                    SourceCusps = sourceCusps, SourceCorners = sourceCorners, SourceInflections = reads.Count(read => read.Inflected),
                    SourceClosed = source.Source.Closed, CommunicatedInterior = message.InteriorSide?.ToString() ?? "Open",
                    run.Ties, Selected = "H" + (run.Selected + 1), LocalMatches = selectedAssessment.Matches,
                    LocalConflicts = selectedAssessment.Conflicts, OpenChecks = selectedAssessment.Unresolved,
                    StatisticalEvents = run.Posterior.Events.Length, WeakOuterHypothesis = source.OuterFocus && envelope.Complete,
                    LocalNotchReads = envelope.Notches,
                    Privacy = "The receiver gets anonymous endpoints at an unmeasured repeated-reach resolution and qualitative feature reports; private source handles and curve schemes are withheld.",
                    Scope = "Authored endpoint-scaffold completion and feature assessment, not blind sketch parsing or trained recognition." }, "H" + (run.Selected + 1)));
            states.Add(run.State);
            // Evaluator-only source and correspondence are retained for later
            // inspection. They are never part of the published message or
            // supplied to candidate construction/assessment.
            var correspondence = S(reads.Select((read, i) => (IElement)new Relation(read.Section,
                new Relation(message.Spans[i].Role, new Relation(new Relation(read.Start, message.Spans[i].From),
                    new Relation(read.End, message.Spans[i].To))))).ToArray());
            archive.Add(new Relation(source.Source.Surface, new Relation(correspondence, new Relation(run.Surface, envelope.Evidence))));
            results.Add(new { source.Id, source.Name, Selected = run.Selected + 1, run.Ties, run.Admitted,
                CandidateChecks = run.Assessments.Select(read => new { read.Matches, read.Conflicts, read.Unresolved }).ToArray(),
                Priors = run.Prior.Rows.Select(row => row.Probability?.ToString()).ToArray(), Posteriors = run.Posterior.Rows.Select(row => row.Probability?.ToString()).ToArray(),
                SourceClosed = source.Source.Closed, CandidateClosures = run.Candidates.Select(sample => sample.Closed).ToArray(),
                AssumedSections = run.Candidates.Select(sample => E(sample.Assumed).Length).ToArray(),
                OuterRead = new { Offered = source.OuterFocus, envelope.Complete, envelope.Notches }, StatisticalEvents = run.Posterior.Events.Length });
        }
        // Statistical replay is independent of the display. Curve-local
        // frame rebinding is tested separately in the focused mechanics suite.
        var packet = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("curve-state-roundtrip", S(states.Cast<IElement>().ToArray())));
        if (!GojoStructureCompactJson.TryToElement(GojoStructureCompactJson.Deserialize(packet), out var restored) || restored.Root is not Relation imported)
            throw new InvalidOperationException("Native states did not survive transport.");
        var copies = E(imported).Cast<Relation>().ToArray();
        for (var i = 0; i < states.Count; i++)
        {
            var before = Stats.Read(states[i]); var after = Stats.Read(copies[i]);
            if (!after.Complete || !before.Rows.Select(row => row.Probability).SequenceEqual(after.Rows.Select(row => row.Probability)))
                throw new InvalidOperationException("An independently imported native state changed its posterior.");
            nativeChecks++;
        }
        Save(Path.Combine(output, "receipts.gojo.json.gz"), S(archive.ToArray()));
        var summary = new { Schema = "curve-evidence-study-v1", Cases = sources.Count, CandidateRealizations = examples.Sum(row => row.Candidates.Count),
            SourceSmoothJoins = smooth, SourceCusps = cusps, SourceCorners = corners, SourceInflectedSections = inflections,
            MixedLineCurveCases = mixed, OpenSourceCases = gaps, TiedChoices = ties, NativeRoundTrips = nativeChecks,
            SecondsBeforeGallery = watch.Elapsed.TotalSeconds,
            Scope = "Native polynomial curve mechanics and authored reconstruction on supplied anonymous endpoints; no dataset import, bitmap recognition, learned likelihood or measured curve parameters.",
            Dependence = "One pooled estimated compatibility event per source; macro envelope, tangent, bend, join and micro reads are not independent copies of the source evidence.",
            Archive = "Separate evaluator source/correspondence branch and public receiver branch. Private handles are retained for audit, never supplied to reconstruction or assessment.",
            Interior = "Explicit oriented-boundary premise; closure does not imply convexity or establish interior.", Results = results };
        JointReconstructionGallery.WriteText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(summary, Json));
        CurveEvidenceGallery.Write(output, summary, examples);
        Console.WriteLine($"Curve evidence: {sources.Count} cases, {nativeChecks} exact statistical round trips; {watch.Elapsed.TotalSeconds:0.00}s. {Path.GetFullPath(output)}");
        return 0;
    }

    public static Run Calculate(CurveEvidenceCorpus.Case source)
    {
        var message = Scenes.Publish(source.Source, source.Tangents, source.Bends, source.Traces, source.Interior);
        var candidates = Enumerable.Range(0, 3).Select(mode => Scenes.Propose(message, mode, source.OfferBridge && mode == 2)).ToArray();
        var groups = candidates.Select(candidate => Assess(message, candidate)).ToArray();
        IElement goal = source.RequestClosure ? new Relation(new Relation(message.Spans[^1].To, message.Spans[0].From), S(candidates[^1].Assumed)) :
            source.FlowingPrior ? message.CurveTemplate : S();
        var basis = new Relation(message.Surface, goal);
        var frame = new Stats.Frame(basis, candidates.Select(candidate => (IElement)candidate.Surface).ToArray());
        var priorWeights = source.RequestClosure ? new[] { 1, 1, 3 } : source.FlowingPrior ? [1, 2, 2] : [1, 1, 1];
        var state = frame.Prior(candidates.Select((candidate, i) => new Relation(candidate.Surface,
            frame.Weight(priorWeights[i], priorWeights.Sum(), basis))).ToArray());
        var prior = Stats.Read(state); var receipts = new List<IElement>(); var admitted = false;
        if (groups.Any(group => group.Matches + group.Conflicts > 0))
        {
            var calibration = CompatibilityEvidence.Calibrate(frame, 15, 16); var rows = new List<IElement>();
            foreach (var (candidate, i) in candidates.Select((candidate, i) => (candidate, i)))
            {
                var assessment = CompatibilityEvidence.Assess(message.Surface, candidate.Surface,
                    S(groups[i].Groups.SelectMany(group => E(group.Supported)).ToArray()),
                    S(groups[i].Groups.SelectMany(group => E(group.Contradicted)).ToArray()),
                    S(groups[i].Groups.SelectMany(group => E(group.Open)).ToArray()));
                if (!CompatibilityEvidence.TryReadLikelihood(assessment, calibration, out var weight, out var receipt))
                    throw new InvalidOperationException("The finite curve compatibility channel remained open.");
                rows.Add(new Relation(candidate.Surface, weight)); receipts.Add(receipt);
            }
            var question = new Stats.Question(message.Surface, new Relation(message.Surface, StructuralConstraint.Open()), basis);
            var offer = Stats.Offer(question.Observe(new Placeholder()), S(rows.ToArray()), S(), S(), calibration);
            var update = Stats.Update(state, offer);
            if (!update.Accepted) throw new InvalidOperationException("The supplied curve likelihood update failed.");
            state = update.State; receipts.Add(update.Surface); admitted = true;
        }
        var posterior = Stats.Read(state);
        var ordered = candidates.Select((candidate, i) => (i, Weight: Weight(posterior, candidate)))
            .OrderByDescending(item => item.Weight, ShapeEvidenceWeighting.RatioComparer.Instance).ThenBy(item => item.i).ToArray();
        return new(message, candidates, groups, prior, posterior, state,
            new Relation(new Relation(message.Surface, S(candidates.Select(candidate => (IElement)candidate.Surface).ToArray())),
                new Relation(state, S(receipts.ToArray()))), ordered[0].i, ordered.Count(item => item.Weight == ordered[0].Weight), admitted);
    }

    public static Assessment Assess(Scenes.Message message, Scenes.Scene candidate)
    {
        var reads = candidate.Reads; var output = new List<Group>();
        // A local descriptor comparison needs an actual correspondence in the
        // selected frame. Missing spans, foreign frames and inconsistent site
        // positions are not negative geometric observations.
        var matched = message.Spans.Select(span =>
        {
            if (!ReferenceEquals(message.Frame.Surface, candidate.Frame.Surface) ||
                !message.Frame.Vector(span.Start, out var start) || !message.Frame.Vector(span.End, out var end)) return null;
            var possible = reads.Where(read => read.Complete && FieldNative.Same(span.From, read.Start) &&
                FieldNative.Same(span.To, read.End) && read.Controls[0] == start && read.Controls[^1] == end).Take(2).ToArray();
            return possible.Length == 1 ? possible[0] : null;
        }).ToArray();
        var trace = new Lane("Straight or curved trace"); var tangent = new Lane("Endpoint tangent directions");
        var bend = new Lane("Bend or inflection participation"); var joins = new Lane("Shared-site join types");
        for (var i = 0; i < message.Spans.Count; i++)
        {
            var span = message.Spans[i];
            if (matched[i] is not { } read)
            {
                var unresolved = new Relation(span.Surface, StructuralConstraint.Open(candidate.Surface));
                trace.Add(null, unresolved); tangent.Add(null, new Relation(span.StartHeading, unresolved));
                tangent.Add(null, new Relation(span.EndHeading, unresolved)); bend.Add(null, unresolved);
                continue;
            }
            var expected = Scenes.TraceStraight(message, span);
            trace.Add(expected is null || read.Straight is null ? null : expected == read.Straight, new Relation(span.Surface, read.Surface));
            tangent.Add(HeadingMatches(span.StartHeading, read.StartVector, message.Frame), new Relation(span.StartHeading, read.StartTangent));
            tangent.Add(HeadingMatches(span.EndHeading, read.EndVector, message.Frame), new Relation(span.EndHeading, read.EndTangent));
            bool? turns = !Closed(span.Bends) ? null : E(span.Bends).Length == 2 ? read.Inflected ? true : read.BendSign is null ? null : false :
                read.BendSign is null ? null : E(span.Bends).Length == 0 ? read.BendSign == 0 :
                    read.BendSign == (Scenes.Side(message.Frame, span.Bends) ?? 0);
            bend.Add(turns, new Relation(span.Bends, read.Surface));
        }
        foreach (var join in message.Joins)
        {
            var i = message.Spans.ToList().IndexOf(join.Incoming); var j = message.Spans.ToList().IndexOf(join.Outgoing);
            if (i < 0 || j < 0 || matched[i] is not { } incoming || matched[j] is not { } outgoing)
            {
                joins.Add(null, new Relation(join.Smooth, StructuralConstraint.Open(candidate.Surface)));
                joins.Add(null, new Relation(join.Cusp, StructuralConstraint.Open(candidate.Surface)));
                joins.Add(null, new Relation(join.Corner, StructuralConstraint.Open(candidate.Surface)));
                continue;
            }
            var actual = Curve.Join(incoming, outgoing);
            void Add(Relation expected, bool? actualValue) => joins.Add(!Closed(expected) || actualValue is null ? null :
                (E(expected).Length != 0) == actualValue, new Relation(expected, actual.Surface));
            Add(join.Smooth, actual.Smooth); Add(join.Cusp, actual.Cusp); Add(join.Corner, actual.Corner);
        }
        foreach (var lane in new[] { trace, tangent, bend, joins })
            output.Add(new(lane.Name, S(lane.Yes.ToArray()), S(lane.No.ToArray()), S(lane.Open.ToArray()),
                new Relation(new Relation(message.Surface, candidate.Surface), new Relation(S(lane.Yes.ToArray()), new Relation(S(lane.No.ToArray()), S(lane.Open.ToArray()))))));
        return new(output, S(output.Select(group => (IElement)group.Surface).ToArray()));
    }
    private sealed class Lane(string name)
    {
        public string Name { get; } = name;
        public List<IElement> Yes { get; } = []; public List<IElement> No { get; } = []; public List<IElement> Open { get; } = [];
        public void Add(bool? match, IElement witness) => (match == true ? Yes : match == false ? No : Open).Add(witness);
    }
    private static bool? HeadingMatches(Relation heading, Curve.Vector vector, Curve.Frame frame)
    { if (!Closed(heading) || vector.Zero) return null; var expected = Scenes.Direction(heading, frame); return expected.X.Sign == vector.X.Sign && expected.Y.Sign == vector.Y.Sign; }
    private static Stats.Ratio Weight(Stats.Distribution read, Scenes.Scene scene) => read.Rows.Single(row => ReferenceEquals(row.Model, scene.Surface)).Probability!.Value;
    private static void Save(string path, Relation root)
    {
        var text = GojoStructureCompactJson.Serialize(GojoStructureCompactJson.FromElement("curve-evidence-v1", root));
        using var file = File.Create(path); using var zip = new GZipStream(file, CompressionLevel.SmallestSize); using var writer = new StreamWriter(zip); writer.Write(text);
    }
}

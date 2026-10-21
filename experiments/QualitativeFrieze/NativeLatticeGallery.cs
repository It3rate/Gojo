using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Gojo.Core;
using Gojo.Patterns;
using E = Gojo.Experiments.QualitativeFrieze.StructuralEvidenceRules;
using P = Gojo.Experiments.QualitativeFrieze.StructuralPotential;

namespace Gojo.Experiments.QualitativeFrieze;

/// <summary>Presentation of actual native support selections and premise-service
/// reads. Coordinates here are a chosen display of exposed endpoint words;
/// neither pixels nor private execution provenance supply evidence.</summary>
public static class NativeLatticeGallery
{
    public static void Write(string output, IReadOnlyList<NativeLineSupplier> suppliers)
    {
        Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var sources = new List<object>(); var queryCount = 0; var sampleCount = 0; var candidateCount = 0;
        var answers = new Dictionary<string, int>();
        foreach (var supplier in suppliers)
        {
            var source = supplier.Source; var view = new View(source); var candidates = new List<object>();
            var summaries = Enumerable.Range(3, 3).Select(_ => new Dictionary<string, int>()).ToArray();
            var sourceImage = $"patterns/{source.Id}-source.svg";
            File.WriteAllText(Path.Combine(output, sourceImage), view.Svg(source.Name));
            for (var ci = 0; ci < supplier.Candidates.Count; ci++)
            {
                var candidate = supplier.Candidates[ci]; var frame = candidate.Frame;
                var id = "C" + (ci + 1).ToString("00", CultureInfo.InvariantCulture);
                var vocabulary = frame.Leaves.Concat(frame.Goals).ToArray();
                string Name(IElement predicate) => vocabulary.FirstOrDefault(row => ReferenceEquals(row.Predicate, predicate))?.Name ?? "Unresolved native predicate";
                string[] Names(Relation scope) => Items(scope).Select(Name).ToArray();
                var original = PublishedStrokes(supplier, candidate);
                var selectedImage = $"patterns/{source.Id}-{id}-source.svg";
                File.WriteAllText(Path.Combine(output, selectedImage), view.Svg(source.Name + " · " + id, original));
                var reads = new List<object>();
                for (var gi = 0; gi < frame.Goals.Count; gi++)
                {
                    var goal = frame.Goals[gi]; var finite = gi >= 3;
                    var outcome = finite ? candidate.Complete(goal.Predicate, S(frame.SpanCoverage.Cast<IElement>().ToArray())) : null;
                    var read = outcome?.SourceRead ?? candidate.Read(goal.Predicate);
                    var completion = outcome?.Completion; var sample = completion?.Sample;
                    queryCount++; Increment(answers, read.Answer);
                    if (finite) Increment(summaries[gi - 3], read.Answer);
                    string? sampleImage = null;
                    if (sample is not null)
                    {
                        sampleImage = $"patterns/{source.Id}-{id}-g{gi}.svg";
                        File.WriteAllText(Path.Combine(output, sampleImage), view.Svg(source.Name + " · observational boundary", original, candidate, sample));
                        sampleCount++;
                    }
                    var trace = Items(read.ServiceRequests).Cast<Relation>().Select(request =>
                    {
                        var predicate = (Relation)((Relation)((Relation)request.Right).Right).Right;
                        var response = Items(read.ServiceAccepted).OfType<Relation>().SingleOrDefault(row => ReferenceEquals(row.Left, request));
                        DtoResponse reply = Response(response);
                        return new { Name = Name(predicate), reply.Answer, Accepted = response is not null };
                    }).ToArray();
                    reads.Add(new
                    {
                        Goal = "g" + gi, goal.Name, Finite = finite, Answer = read.Answer,
                        QueryCapacity = 64, ServiceCapacity = 32,
                        Visited = Names(read.Visited), Unasked = Names(read.Unasked), Missing = Names(read.Missing), BudgetCuts = Names(read.BudgetCuts),
                        Consumed = Items(read.Consumed).Length,
                        ServiceRequests = trace, ServiceAccepted = Items(read.ServiceAccepted).Length,
                        ServiceBlocked = Items(read.ServiceBlocked).Length, ServiceOpen = Items(read.ServiceOpen).Length,
                        ServiceConsumed = Items(read.ServiceConsumed).Length,
                        ObservedFacts = read.ObservedPublication.Evidence.Select(fact => new { Name = Name(fact.Predicate), fact.Answer }).ToArray(),
                        RefutationProofs = Items(read.Refuted).Length, ConflictProofs = Items(read.Conflicts).Length,
                        BlockedProofs = Items(read.Blocked).Length,
                        CompletionAnswer = completion?.Answer, ConditionalAnswer = completion?.ConditionalRead.Answer,
                        Assumed = completion is null ? [] : Names(completion.AssumedPredicates),
                        ConditionalMissing = completion is null ? [] : Names(completion.ConditionalRead.Missing),
                        SampleImage = sampleImage, SampleSites = sample is null ? 0 : Items(sample.Observation.Sites).Length,
                        SampleEdges = sample is null ? 0 : Items(sample.Observation.Segments).Length,
                        CycleWitnesses = sample is null ? 0 : Items(sample.CycleWitness).Length,
                        Promises = sample is null ? [] : Names(sample.Promises)
                    });
                }
                candidates.Add(new { Id = id, Index = ci, SelectedImage = selectedImage,
                    SourceStrokes = original.Count, VisibleSourceStrokes = view.Strokes.Count(stroke => original.Contains(stroke.Row)),
                    Reads = reads });
            }
            candidateCount += supplier.Candidates.Count;
            // Capacity comparisons are individual actual reads of the first
            // candidate, rather than another large source/question product.
            var probes = new List<object>();
            if (supplier.Candidates.FirstOrDefault() is { } first)
                foreach (var (q, s, label) in new[] { (64, 1, "One service slot"), (64, 0, "No service capacity"), (0, 32, "No traversal capacity") })
                {
                    var read = first.Read(first.Frame.FiniteSquare, q, s);
                    probes.Add(new { label, QueryCapacity = q, ServiceCapacity = s, read.Answer,
                        Queries = Items(read.Consumed).Length, Replies = Items(read.ServiceAccepted).Length,
                        ServiceOpen = Items(read.ServiceOpen).Length, Cuts = Items(read.BudgetCuts).Length });
                }
            sources.Add(new
            {
                source.Id, source.Name, source.Note,
                TurnModel = supplier.Basis.Contour.TryReadEqualTurns(supplier.Basis.Contour.Evaluate(), out _), SourceImage = sourceImage,
                SourceSites = Items(source.Graph.Sites).Length, SourceStrokes = Items(source.Graph.Strokes).Length,
                VisibleSourceStrokes = view.Strokes.Count, UnresolvedPositions = view.UnknownPositions,
                DerivedSupports = Items(supplier.Support.Graph.Strokes).Length,
                Unsearched = Items(supplier.Unsearched).Length, Unsupported = Items(supplier.Unsupported).Length,
                SupportUnresolved = Items(supplier.Support.Unresolved).Length,
                Summaries = summaries.Select((counts, i) => new { Goal = "g" + (i + 3), Counts = counts }).ToArray(),
                Candidates = candidates, CapacityExamples = probes
            });
        }
        var goals = suppliers.FirstOrDefault()?.Candidates.FirstOrDefault()?.Frame.Goals.Select((goal, index) =>
            new { Id = "g" + index, goal.Name, Finite = index >= 3 }).ToArray() ?? [];
        var model = new
        {
            Schema = "native-lattice-evidence-v1",
            Counts = new { Sources = suppliers.Count, Candidates = candidateCount, Queries = queryCount, Samples = sampleCount, Answers = answers },
            Goals = goals, Sources = sources,
            Limits = new[]
            {
                "These publications preserve geometry from executed native ornaments while keeping private construction history outside the public port. Fresh public identities preserve incidence and reach sharing. Source names never select a premise answer.",
                "Support discovery is a bounded, eager preprocessing read. Each selected construction then queries only its reached, missing premises through the advertised native service.",
                "A candidate selects two horizontal-family and two vertical-family supports. Other orientations, arbitrary motifs and complete face decomposition are outside this experiment.",
                "Overview totals concern the searched support selections. A search cut leaves additional selections untested; even a supported boundary is not automatically an elementary or empty face.",
                "A No result refutes this selected conjunctive construction under the exposed model. It does not establish that no other square or interpretation exists in the whole ornament.",
                "Perpendicularity is read from the separately selected native radial/turn construction. Direction labels and a right-angled drawing do not supply that premise.",
                "Straight interpolation and whole finite support are explicit public premises. Endpoint words alone do not turn an unspecified or notched route into a straight line.",
                "The display assigns screen extents to exposed motion words. It omits unresolved endpoint positions instead of reading private provenance. Its colors, lengths and angles never become engine evidence.",
                "Read-corner markers belong to the derived observational boundary. Published source passages and incidence are preserved; filled-interior ownership is not inferred.",
                "Conditional completion permits only missing finite-span premises in this context. Actual Open replies, known refusals, conflicts and service/traversal cuts cannot be silently replaced by assumed source facts.",
                "The gallery prepares comparisons in advance. Per-query service counts demonstrate avoided premise reads, rather than an end-to-end rendering or training speed claim."
            }
        };
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        File.WriteAllText(Path.Combine(output, "diagnostics.json"), json);
        File.WriteAllText(Path.Combine(output, "data.js"), "globalThis.NATIVE_LATTICE_EVIDENCE=" + json + ";");
        File.WriteAllText(Path.Combine(output, "gallery.html"), Gallery());
        File.WriteAllText(Path.Combine(output, "overview.html"), Overview());
    }

    private sealed record DtoResponse(string Answer);
    private static DtoResponse Response(Relation? accepted)
    {
        if (accepted?.Right is not Relation { Right: Relation { Right: Relation raw } }) return new("Unavailable");
        return new(new OrnamentDefaultsPolicy.Evidence(raw).Answer);
    }

    private static HashSet<Relation> PublishedStrokes(NativeLineSupplier supplier, NativeLineSupplier.Candidate candidate)
    {
        var result = new HashSet<Relation>(ReferenceEqualityComparer.Instance);
        foreach (var support in candidate.Supports)
        {
            var row = Items(supplier.Support.Correspondence).Cast<Relation>().Single(actual => ReferenceEquals(actual.Left, support));
            foreach (var use in Items((Relation)row.Right).OfType<Relation>())
                if (use.Left is Relation stroke) result.Add(stroke);
        }
        return result;
    }

    private sealed record Ink(Relation Row, IElement From, IElement To, double X1, double Y1, double X2, double Y2);
    private sealed class View
    {
        private readonly NativeLatticeSources.SourceCase source;
        private readonly Dictionary<IElement, (double X, double Y)> points = new(ReferenceEqualityComparer.Instance);
        public List<Ink> Strokes { get; } = [];
        public int UnknownPositions { get; }

        public View(NativeLatticeSources.SourceCase source)
        {
            this.source = source;
            foreach (var site in Items(source.Graph.Sites))
            {
                var rows = Items(source.Bindings).OfType<Relation>().Where(row => ReferenceEquals(row.Left, site)).ToArray();
                if (rows is [Relation { Right: Relation word }] && TryPoint(word, out var point)) points[site] = point;
            }
            UnknownPositions = Items(source.Graph.Sites).Count(site => !points.ContainsKey(site));
            foreach (var row in Items(source.Graph.Strokes).OfType<Relation>())
                if (QualitativeStrokeGraph.TryReadStroke(row, out _, out var from, out var to, out _) &&
                    points.TryGetValue(from, out var a) && points.TryGetValue(to, out var b))
                    Strokes.Add(new(row, from, to, a.X, a.Y, b.X, b.Y));
        }

        private bool TryPoint(Relation word, out (double X, double Y) point)
        {
            point = default;
            if (!Closed(word)) return false;
            double x = 0, y = 0;
            foreach (var item in Items(word))
            {
                if (item is not Relation move || !source.Graph.Frame.Direction(move, out var axis, out var forward, out _)) return false;
                var step = forward ? 1 : -1;
                if (ReferenceEquals(axis, source.Graph.Frame.Horizontal)) x += step; else y += step;
            }
            point = (x, y); return true;
        }

        public string Svg(string title, HashSet<Relation>? selected = null, NativeLineSupplier.Candidate? candidate = null, P.BoundarySample? sample = null)
        {
            (double X, double Y)[] corners = [];
            if (sample is not null && candidate is not null)
            {
                var starts = new List<(double X, double Y)>();
                foreach (var support in candidate.Supports)
                    if (QualitativeStrokeGraph.TryReadStroke(support, out _, out var from, out _, out _) && points.TryGetValue(from, out var point)) starts.Add(point);
                if (starts.Count == 4) corners = [(starts[2].X, starts[0].Y), (starts[3].X, starts[0].Y),
                    (starts[3].X, starts[1].Y), (starts[2].X, starts[1].Y)];
            }
            var all = corners.Length == 4 ? corners : points.Values.ToArray();
            var minX = all.Length == 0 ? 0 : all.Min(point => point.X); var maxX = all.Length == 0 ? 1 : all.Max(point => point.X);
            var minY = all.Length == 0 ? 0 : all.Min(point => point.Y); var maxY = all.Length == 0 ? 1 : all.Max(point => point.Y);
            var cx = (minX + maxX) / 2; var cy = (minY + maxY) / 2;
            var margin = corners.Length == 4 ? 1.6 : 1.05;
            var scale = Math.Min(730 / (Math.Max(1, maxX - minX) * margin), 390 / (Math.Max(1, maxY - minY) * margin));
            (double X, double Y) Screen(double x, double y) => (410 + (x - cx) * scale, 245 - (y - cy) * scale);
            var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 820 500' role='img'><title>{H(title)}</title><defs><clipPath id='window'><rect x='20' y='35' width='780' height='420'/></clipPath></defs><rect width='820' height='500' fill='#faf8ef'/><g clip-path='url(#window)' fill='none' stroke-linecap='round' stroke-linejoin='round'>");
            foreach (var stroke in Strokes)
            {
                var a = Screen(stroke.X1, stroke.Y1); var b = Screen(stroke.X2, stroke.Y2);
                var active = selected?.Contains(stroke.Row) == true;
                svg.Append($"<path class='source-segment{(active ? " selected-support" : "")}' d='M {F(a.X)} {F(a.Y)} L {F(b.X)} {F(b.Y)}' stroke='{(active ? "#397984" : selected is null ? "#537179" : "#c6cfc8")}' stroke-width='{(active ? "3.6" : selected is null ? "2.15" : "1.4")}'/>");
            }
            var degree = Strokes.SelectMany(stroke => new[] { stroke.From, stroke.To })
                .GroupBy<IElement, IElement>(site => site, ReferenceEqualityComparer.Instance);
            foreach (var group in degree.Where(group => group.Count() >= 3))
            {
                var p = points[group.Key]; var screen = Screen(p.X, p.Y);
                svg.Append($"<circle class='source-join' cx='{F(screen.X)}' cy='{F(screen.Y)}' r='2.7' fill='#537179' stroke='none'/>");
            }
            if (sample is not null && corners.Length == 4)
            {
                var sites = Items(sample.Observation.Sites);
                foreach (var edge in Items(sample.Observation.Segments).Cast<Relation>())
                    if (sample.Observation.TryReadSegment(edge, out _, out var from, out var to, out _))
                    {
                        var a = corners[Array.FindIndex(sites, site => ReferenceEquals(site, from))];
                        var b = corners[Array.FindIndex(sites, site => ReferenceEquals(site, to))];
                        var aa = Screen(a.X, a.Y); var bb = Screen(b.X, b.Y);
                        svg.Append($"<path class='read-boundary' d='M {F(aa.X)} {F(aa.Y)} L {F(bb.X)} {F(bb.Y)}' stroke='#b28139' stroke-width='5'/>");
                    }
                for (var index = 0; index < corners.Length; index++)
                {
                    var p = Screen(corners[index].X, corners[index].Y);
                    svg.Append($"<circle class='read-site' cx='{F(p.X)}' cy='{F(p.Y)}' r='6' fill='#faf8ef' stroke='#b28139' stroke-width='2.5'/><text x='{F(p.X + 10)}' y='{F(p.Y - 10)}' fill='#806332' stroke='none' font-family='system-ui,sans-serif' font-size='13'>K{index}</text>");
                }
            }
            svg.Append("</g>");
            var omitted = Items(source.Graph.Strokes).Length - Strokes.Count;
            svg.Append($"<text x='25' y='480' font-family='system-ui,sans-serif' font-size='12' fill='#728183'>{(sample is null ? "Published native words · dots mark shared sites" : "Observational boundary · read corners do not weld source passages")}{(omitted > 0 ? $" · {omitted} unresolved passage(s) omitted" : "")}</text></svg>");
            return svg.ToString();
        }
    }

    private static void Increment(Dictionary<string, int> counts, string answer) => counts[answer] = counts.GetValueOrDefault(answer) + 1;
    private static Relation S(params IElement[] rows) => StructuralConstraint.Closed(rows);
    private static IElement[] Items(Relation scope) => StructuralConstraint.TryReadScope(scope, out var rows, out _, out _) ? rows : [];
    private static bool Closed(Relation scope) => StructuralConstraint.TryReadScope(scope, out _, out var start, out var end) && ReferenceEquals(start, end);
    private static string H(string value) => WebUtility.HtmlEncode(value);
    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private const string Css = """
[hidden]{display:none!important}
.controls>label{min-width:0}select{width:100%}
*{box-sizing:border-box}body{margin:0;background:#f5f3eb;color:#2e4b52;font:15px/1.55 system-ui,sans-serif}main{max-width:1580px;margin:auto;padding:27px 28px 55px}h1,h2,h3{font-family:Georgia,serif;font-weight:500;line-height:1.2}h1{font-size:clamp(32px,4vw,48px);max-width:1100px;margin:10px 0 16px}h2{font-size:25px;margin:8px 0 14px}h3{font-size:20px;margin:12px 0}a{color:#397984;text-underline-offset:3px}p{margin:8px 0 15px}.intro{max-width:1080px;font-size:18px}.eyebrow{font-size:12px;text-transform:uppercase;letter-spacing:.12em;color:#7c7d6d}.links{display:flex;gap:20px;flex-wrap:wrap;margin:20px 0}.controls{display:grid;grid-template-columns:1.4fr .8fr 1.3fr;gap:16px;padding:16px;background:#e9ede5;border:1px solid #cbd4ca;border-radius:6px}label{display:flex;flex-direction:column;gap:7px;font-size:13px;font-weight:650}select,button{font:inherit;color:inherit;background:#fffdf7;border:1px solid #aebeb7;border-radius:4px;padding:9px;max-width:100%;min-width:0}button{cursor:pointer}button:focus-visible,select:focus-visible,summary:focus-visible{outline:3px solid #bd9250;outline-offset:3px}.note{padding:13px 17px;background:#fffdf7;border-left:3px solid #8aa49c;margin:17px 0}.panels{display:grid;grid-template-columns:1.1fr 1fr 1.05fr;gap:17px;align-items:start}.panel{min-width:0;background:#fffdf7;padding:17px;border:1px solid #ccd3cb;border-radius:6px}.panel img{display:block;width:100%;height:270px;object-fit:contain;background:#faf8ef}.muted,.caption{color:#6a7c7c;font-size:13px}.status{display:inline-block;font-size:14px;font-weight:650;padding:8px 11px;border:1px solid #a8bbb5;border-radius:4px;background:#edf2ec;margin:0 0 12px}.status.open{background:#f3ecdd;border-color:#d3bf94}.status.no,.status.conflict,.status.blocked{background:#efedeb;border-color:#b9b4ac}.facts{font-size:13px}.fact{display:grid;grid-template-columns:minmax(0,1fr) auto;gap:13px;border-bottom:1px solid #e0e4db;padding:9px 0}.fact b{font-size:12px}.trace{list-style:none;padding:0;margin:0}.trace li{border-left:2px solid #c9d7d2;padding:7px 11px;margin:4px 0;display:flex;justify-content:space-between;gap:12px;font-size:13px}.trace li span{font-size:12px;color:#5e7378}.empty{padding:30px 18px;border:1px dashed #c8c9bd;background:#f8f6ee;color:#71807b;min-height:130px}.counts{font-size:13px;padding:10px;background:#eef0e8}.views{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:10px;margin-top:17px}.views button{text-align:left;font-size:12px;padding:11px}.views button strong{display:block;margin-bottom:6px;font-size:13px}.views [aria-selected=true]{border-color:#397984;box-shadow:inset 0 0 0 1px #397984}.remainders{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:14px;margin:22px 0}.remainder{background:#ecefe6;padding:14px;font-size:13px}.remainder ul{padding-left:19px}.limits{padding:20px;background:#e9ede4;margin-top:27px}.limits ul{padding-left:22px}.limits li{margin:9px 0;font-size:13px}details{margin:14px 0;font-size:13px}summary{cursor:pointer;color:#52767b}.cards{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:17px}.card{background:#fffdf7;border:1px solid #ccd3cb;border-radius:6px;padding:17px;min-width:0}.card img{width:100%;height:230px;object-fit:contain}.card h2{font-size:23px}.card p{font-size:13px}.summary{font-size:12px;display:grid;gap:6px}.summary a{display:block;border:1px solid #c9d2c9;padding:6px 9px;text-decoration:none}.warning{font-size:12px;color:#846d43}footer{font-size:12px;color:#7a857b;margin-top:25px}table{width:100%;border-collapse:collapse;font-size:12px}th,td{text-align:left;vertical-align:top;padding:7px;border-bottom:1px solid #d4dacf}th{font-weight:600}.table-scroll{overflow:auto}@media(max-width:1050px){.panels{grid-template-columns:1fr 1fr}.panel:last-child{grid-column:1/-1}.panel:last-child img{height:320px}.cards{grid-template-columns:1fr 1fr}}@media(max-width:650px){main{padding:20px 15px}.controls,.panels,.cards,.remainders{grid-template-columns:1fr}.panel:last-child{grid-column:auto}.panel img,.panel:last-child img{height:auto}.views{grid-template-columns:1fr 1fr}.intro{font-size:16px}}
""";

    private static string Gallery() => """
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Gojo · questions over native lattices</title><style>
""" + Css + """
</style></head><body><main><div class="eyebrow">Gojo · actual sources, local support questions</div><h1>A lattice can answer more than its original joints expose</h1><p class="intro">Select four supports inside a published view of an executed ornament, then ask for a boundary, rectangle or square. The engine requests actual native premise reads only when that question reaches them.</p><nav class="links"><a href="overview.html">All source lattices</a><a href="../structural-potential/gallery.html">Partial construction contracts</a><a href="diagnostics.json">Read diagnostics</a></nav><div class="controls"><label>Published source<select id="source"></select></label><label>Four-support selection<select id="candidate"></select></label><label>Requested construction<select id="goal"></select></label></div><p class="note" id="source-note"></p><div class="panels"><section class="panel"><div class="eyebrow">1 · public source</div><h2>The whole pattern</h2><img id="source-image" alt="Published native pattern with selected supports highlighted"><p class="caption">Teal strokes carry the selected supports; pale strokes preserve the rest of the ornament. Dots identify shared published sites. A drawn crossing alone adds no mechanical join.</p><div class="counts" id="source-counts"></div><p class="warning" id="source-warning"></p><details><summary>Published source without selection</summary><img id="original-image" alt="Unhighlighted published source pattern"></details></section><section class="panel"><div class="eyebrow">2 · on-demand evidence</div><h2 id="question-title"></h2><div id="answer" class="status"></div><p id="query-note" class="caption"></p><div class="counts" id="read-counts"></div><h3>Producer questions actually asked</h3><ol id="service-trace" class="trace"></ol><details><summary>All visited construction obligations</summary><ol id="visited" class="trace"></ol></details><h3>Facts received through the service</h3><div id="facts" class="facts"></div></section><section class="panel"><div class="eyebrow">3 · derived view</div><h2>Selected boundary read</h2><div id="completion-answer" class="status"></div><img id="sample-image" alt="Derived boundary and observational corner markers" hidden><div id="no-sample" class="empty"></div><p class="caption" id="sample-note"></p><div id="sample-facts"></div><div id="assumptions"></div></section></div><section><h2>Other questions about the same selection</h2><p class="caption">These tiles are independent prepared comparisons. They are not extra facts consulted by the selected read.</p><div id="views" class="views"></div></section><div class="remainders"><div class="remainder"><strong>Missing or unresolved source premises</strong><div id="missing"></div></div><div class="remainder"><strong>Not evaluated after early stop</strong><div id="unasked"></div></div><div class="remainder"><strong>Traversal cuts</strong><div id="cuts"></div></div></div><details><summary>Small capacity comparison · first selection in this source</summary><p>The following are separate native reads of the finite-square question.</p><div class="table-scroll"><table><thead><tr><th>Selected capacity</th><th>Answer</th><th>Visited</th><th>Replies</th><th>Open service routes / traversal cuts</th></tr></thead><tbody id="capacity"></tbody></table></div></details><section class="limits"><h2>Scope of this experiment</h2><ul id="limits"></ul></section><footer id="run-note"></footer></main><script src="data.js"></script><script>
(()=>{'use strict';const d=globalThis.NATIVE_LATTICE_EVIDENCE,$=id=>document.getElementById(id),esc=v=>String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));const status=a=>({Yes:'Selected construction supported',No:'Selected construction refused',Open:'Evidence remains unresolved',Conflict:'Conflicting evidence retained',Blocked:'Evidence route blocked'})[a]||a;const list=(a,empty='None in this read.')=>a.length?'<ul>'+a.map(x=>'<li>'+esc(x)+'</li>').join('')+'</ul>':'<p class="muted">'+esc(empty)+'</p>';const chosen=()=>d.sources.find(s=>s.id===$('source').value)||d.sources[0],candidate=()=>chosen().candidates.find(c=>c.id===$('candidate').value)||chosen().candidates[0],current=()=>candidate()?.reads.find(r=>r.goal===$('goal').value);function pill(id,text,answer){$(id).textContent=text;$(id).className='status '+String(answer||'').toLowerCase()}function choices(id,values,label){$(id).innerHTML=values.map(x=>'<option value="'+esc(x.id)+'">'+esc(label(x))+'</option>').join('')}function render(){const s=chosen(),c=candidate(),r=current();$('source-note').textContent=s.note;$('source-image').src=c?c.selectedImage:s.sourceImage;$('original-image').src=s.sourceImage;$('source-counts').textContent=s.sourceStrokes+' published passages · '+s.sourceSites+' published sites · '+s.derivedSupports+' derived supports · '+s.candidates.length+' searched selections.';$('source-warning').textContent=[s.unsearched?s.unsearched+' additional support selections remain unsearched.':'',s.unsupported?s.unsupported+' support passages are outside the selected pure-axis grammar.':'',s.unresolvedPositions?s.unresolvedPositions+' endpoint position(s) unresolved; affected strokes are omitted from the drawing.':'',s.turnModel?'A native equal-turn model is selected.':'Geometric quarter turns are not supplied by direction names.'].filter(Boolean).join(' ');if(!r){pill('answer','No candidate available','Open');return}document.title='Gojo · '+s.id+' '+c.id+' · '+r.name;$('question-title').textContent=r.name;pill('answer',status(r.answer),r.answer);$('query-note').textContent=r.finite?'This asks for actual finite-support coverage, together with any stronger requested angle and reach promises.':'This concerns the extended supporting-line construction; finite reach is a separate question.';$('read-counts').textContent=r.consumed+' construction predicates visited · '+r.serviceRequests.length+' service requests · '+r.serviceAccepted+' admitted replies · '+r.serviceOpen+' unresolved service receipts.';$('service-trace').innerHTML=r.serviceRequests.map((x,i)=>'<li><strong>'+esc((i+1)+'. '+x.name)+'</strong><span>'+esc(x.answer)+'</span></li>').join('')||'<li>No source service was invoked.</li>';$('visited').innerHTML=r.visited.map((name,i)=>'<li>'+esc((i+1)+'. '+name)+'</li>').join('');$('facts').innerHTML=r.observedFacts.map(f=>'<div class="fact"><span>'+esc(f.name)+'</span><b>'+esc(f.answer)+'</b></div>').join('')||'<p class="muted">No producer account was imported.</p>';const has=!!r.sampleImage;pill('completion-answer',has?(r.assumed.length?'Conditional boundary witnessed':'Boundary witnessed from producer evidence'):r.finite?(r.completionAnswer?status(r.completionAnswer):'No completed boundary committed'):'Observation only · no boundary emitted',has?'Yes':r.completionAnswer||'Open');$('sample-image').hidden=!has;$('no-sample').hidden=has;if(has)$('sample-image').src=r.sampleImage;else{$('sample-image').removeAttribute('src');$('no-sample').textContent=r.finite?'This read has not supplied a complete finite boundary. Missing evidence remains visible.':'A supporting-line potential can be read without creating a finite boundary sample.'}$('sample-note').textContent=has?'Amber lines and K markers belong to a new observational read, over the published source. The display does not create source joints or filled-interior ownership.':'A blank result is an unresolved, refused or unrequested construction; it is not a zero-size object.';$('sample-facts').innerHTML=has?'<p class="counts">'+r.sampleSites+' read sites · '+r.sampleEdges+' edges · '+r.cycleWitnesses+' native cycle witness.</p>'+list(r.promises,'No additional angle or shared-reach promise requested.') :'';$('assumptions').innerHTML=r.assumed.length?'<h3>Conditional additions · not source facts</h3>'+list(r.assumed):'';$('views').innerHTML=c.reads.map(x=>'<button data-goal="'+esc(x.goal)+'" aria-selected="'+(x.goal===r.goal)+'"><strong>'+esc(x.name)+'</strong>'+esc(status(x.answer))+'<br><span>'+x.serviceRequests.length+' source questions</span></button>').join('');$('views').querySelectorAll('button').forEach(b=>b.onclick=()=>{$('goal').value=b.dataset.goal;render()});$('missing').innerHTML=list(r.missing);$('unasked').innerHTML=list(r.unasked);$('cuts').innerHTML=list(r.budgetCuts);$('capacity').innerHTML=s.capacityExamples.map(p=>'<tr><td>'+esc(p.label)+'</td><td>'+esc(p.answer)+'</td><td>'+p.queries+'</td><td>'+p.replies+'</td><td>'+p.serviceOpen+' / '+p.cuts+'</td></tr>').join('');const params=new URLSearchParams({source:s.id,candidate:c.id,goal:r.goal});history.replaceState(null,'','?'+params.toString())}choices('source',d.sources,s=>s.id+' · '+s.name);choices('goal',d.goals,g=>g.name);const params=new URLSearchParams(location.search);if(d.sources.some(s=>s.id===params.get('source')))$('source').value=params.get('source');else if(d.sources.some(s=>s.id==='N02'))$('source').value='N02';function selectCandidates(){choices('candidate',chosen().candidates,c=>c.id+' · '+c.sourceStrokes+' source passages')}selectCandidates();if(chosen().candidates.some(c=>c.id===params.get('candidate')))$('candidate').value=params.get('candidate');$('goal').value=d.goals.some(g=>g.id===params.get('goal'))?params.get('goal'):'g5';$('source').onchange=()=>{selectCandidates();render()};$('candidate').onchange=render;$('goal').onchange=render;$('limits').innerHTML=d.limits.map(x=>'<li>'+esc(x)+'</li>').join('');$('run-note').textContent=d.counts.sources+' executed native sources · '+d.counts.candidates+' searched support selections · '+d.counts.queries+' prepared native reads · '+d.counts.samples+' witnessed finite boundary views.';render()})();
</script></body></html>
""";

    private static string Overview() => """
<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Gojo · native lattice source atlas</title><style>
""" + Css + """
</style></head><body><main><div class="eyebrow">Gojo · public native patterns</div><h1>Local construction reads inside whole lattice sources</h1><p class="intro">The same lattice can support several boundary views. Counts below concern searched four-support selections, including larger enclosing boundaries; they are not counts of elementary cells or empty faces.</p><nav class="links"><a href="gallery.html">Inspect source, selection and evidence</a><a href="diagnostics.json">Native read diagnostics</a></nav><div id="sources" class="cards"></div><footer id="run-note"></footer></main><script src="data.js"></script><script>
(()=>{const d=globalThis.NATIVE_LATTICE_EVIDENCE,esc=v=>String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));document.getElementById('sources').innerHTML=d.sources.map(s=>'<article class="card" data-source="'+esc(s.id)+'"><div class="eyebrow">'+esc(s.id)+'</div><h2>'+esc(s.name)+'</h2><a href="gallery.html?source='+encodeURIComponent(s.id)+'&amp;candidate=C01&amp;goal=g3"><img loading="lazy" src="'+esc(s.sourceImage)+'" alt="'+esc(s.name)+' native source"></a><p>'+esc(s.note)+'</p><p class="caption">'+s.sourceStrokes+' published passages · '+s.derivedSupports+' derived supports.<br>'+s.candidates.length+' selections searched'+(s.unsearched?' · '+s.unsearched+' remain unsearched':'')+'.</p><div class="summary">'+s.summaries.map(q=>{const g=d.goals.find(g=>g.id===q.goal),c=q.counts;return '<a href="gallery.html?source='+encodeURIComponent(s.id)+'&amp;candidate=C01&amp;goal='+q.goal+'">'+esc(g.name)+'<br>Supported '+(c.Yes||0)+' · refused '+(c.No||0)+' · unresolved '+(c.Open||0)+(c.Conflict?' · conflict '+c.Conflict:'')+(c.Blocked?' · blocked '+c.Blocked:'')+'</a>'}).join('')+'</div><p class="warning">Within searched candidates only.'+(s.unsearched?' Bounded search is incomplete.':'')+(s.unsupported?' Some passages are outside the selected grammar.':'')+(s.unresolvedPositions?' Unresolved endpoint positions are omitted from this drawing.':'')+'</p></article>').join('');document.getElementById('run-note').textContent=d.counts.sources+' actual source patterns · '+d.counts.queries+' prepared native queries. No image pixels or private recipe provenance were read.'})();
</script></body></html>
""";
}

using System.Text;
using System.Text.Json;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>
/// A presentation of the recorded native reads. Drawing, sorting and navigation
/// never establish equality, admissibility or permission to change a source.
/// </summary>
public static class ConstraintFamiliesGallery
{
    public static void Write(string output, string json)
    {
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "index.html"), Page.Replace("@@DATA@@", json.Replace("<", "\\u003c")));
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var ranges = root.GetProperty("Ranges");
        var symmetry = root.GetProperty("Symmetry");
        var neighborhood = root.GetProperty("Neighborhood");
        var md = new StringBuilder();
        md.AppendLine("# Construction families: what can change, and what stays?");
        md.AppendLine();
        md.AppendLine("One drawing is one output. A construction family is the set of outputs a connected machine permits under selected bindings. This experiment compares those sets, changes the source reused by a symmetry machine, and distinguishes nearby shapes that need different kinds of structural change.");
        md.AppendLine();
        md.AppendLine("Open [the interactive gallery](index.html) for the drawings and comparison controls. [results.json](results.json) contains the recorded observations. All programs and tested bindings are supplied; this study does not learn a new generator or autonomously replace a machine.");
        md.AppendLine("Connected native evidence is retained in [ranges.gojo](ranges.gojo), [symmetry.gojo](symmetry.gojo) and [neighborhood.gojo](neighborhood.gojo). These archives preserve more than the report's presentation summaries.");
        md.AppendLine();
        md.AppendLine("## Read this first");
        md.AppendLine();
        md.AppendLine("- **A relaxed binding can enlarge a range.** Releasing width = height admits rectangles that the tied square family rejects.");
        md.AppendLine("- **A different program need not enlarge it.** A free four-step rectangle walk already has independent width and height. Factoring it into a corner and its half-turn can preserve the same finite boundary range.");
        md.AppendLine("- **Source choice is part of repetition.** Reuse the original, feed back the last result, or transform the accumulated group: the same action can produce different participation and overlap.");
        md.AppendLine("- **A shifted crop is not automatically a different infinite pattern.** Conversely, matching a supplied phase correspondence does not erase finite boundaries or prove every continuation.");
        md.AppendLine("- **Nearby pictures can require different edits.** Changing a binding, applying a geometric word substitution, changing connectivity and replacing a boundary construction are separate moves.");
        md.AppendLine();
        md.AppendLine("## 1. Finite construction ranges");
        md.AppendLine();
        md.AppendLine(Text(ranges, "Domain"));
        md.AppendLine();
        md.AppendLine("The inputs count shared reaches in a declared local basis, not pixels or universally calibrated physical lengths. W and H are offered roles, not guaranteed measured dimensions of the resulting shape: a machine can reuse one role or leave another unused. In particular, the six-equal-step walk keeps a fixed 2:1 aspect ratio and does not consume the independent H offering. An admitted input satisfies this program's tested obligations. Several admitted inputs can have the same boundary read; attempts and unique outputs are therefore different counts.");
        md.AppendLine();
        md.AppendLine("| Family | Retained constraint | Released or variable condition |");
        md.AppendLine("| --- | --- | --- |");
        foreach (var family in Items(ranges, "Families"))
            md.AppendLine($"| {Cell(Text(family, "Name"))} | {Cell(Text(family, "Constraint"))} | {Cell(Text(family, "Release"))} |");
        md.AppendLine();
        md.AppendLine("| Family | Attempts | Admitted | Rejected | Unresolved | Unique outlines | Unique directed outlines |");
        md.AppendLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: |");
        foreach (var range in Items(ranges, "Ranges"))
            md.AppendLine($"| {Cell(Text(range, "Family"))} | {Text(range, "Attempts")} | {Text(range, "Admitted")} | {Text(range, "Rejected")} | {Text(range, "Unresolved")} | {Text(range, "UniqueBoundaries")} | {Text(range, "UniqueDirected")} |");
        md.AppendLine();
        md.AppendLine("### What an overlap means");
        md.AppendLine();
        md.AppendLine("An outline is a multiset of shared-reach line atoms. The read ignores section subdivision; it preserves repeated occurrences. The directed version also retains the direction of each atom. Neither read establishes the same site identities, traversal schedule, topology, history, or future behavior. Set intersections are over admitted outputs, not just over attractive drawings.");
        md.AppendLine();
        md.AppendLine("The inclusion statements below concern only the tested finite envelope. They are not proofs that one unrestricted construction language contains another. An exclusive sample is an input whose output was not found in the other family's tested range.");
        md.AppendLine("The same input envelope can produce different output scales: six equal steps reuse W more times than the four-step walk. Some six-step rectangles fall outside the four-step inventory's largest sampled width. A finite unmatched size therefore does not prove that a free rectangle machine cannot construct that rectangle with a larger input.");
        md.AppendLine();
        md.AppendLine("| First | Second | Read | First / second sizes | Intersection | First-only / second-only | First subset of second | Second subset of first | Exclusive sample references |");
        md.AppendLine("| --- | --- | --- | ---: | ---: | ---: | --- | --- | --- |");
        foreach (var pair in Items(ranges, "Comparisons"))
            md.AppendLine($"| {Cell(Text(pair, "First"))} | {Cell(Text(pair, "Second"))} | {(Flag(pair, "Directed") ? "Directed" : "Undirected")} | {Text(pair, "FirstCount")} / {Text(pair, "SecondCount")} | {Text(pair, "Intersection")} | {Text(pair, "FirstOnly")} / {Text(pair, "SecondOnly")} | {Text(pair, "FirstSubsetOfSecond")} | {Text(pair, "SecondSubsetOfFirst")} | {Cell(Text(pair, "FirstOnlySample"))} / {Cell(Text(pair, "SecondOnlySample"))} |");
        md.AppendLine();
        md.AppendLine("### Range limits");
        md.AppendLine();
        foreach (var limit in Items(ranges, "Limits")) md.AppendLine("- " + Text(limit));
        md.AppendLine();
        md.AppendLine("## 2. Symmetry machines: what becomes the next source?");
        md.AppendLine();
        md.AppendLine("A repeat consists of a selected source, a connected action, a controller and a next-source relation. Reapplying a fixed transform to the original can repeatedly stamp the same location. Feeding back the previous result accumulates the transform. Reusing the whole group transforms all retained participation, so group growth and overlap matter. An advancing placement clock is another input; it must not be smuggled into 'reuse the original'.");
        md.AppendLine("These samples copy observations; they do not divide or duplicate conserved material flow. A phase shift step is a placement slot in the supplied rule, not necessarily one native reach or one line section.");
        md.AppendLine();
        md.AppendLine("| Sample | Motif | Construction | Rule | Steps | Retained sections |");
        md.AppendLine("| --- | --- | --- | --- | ---: | ---: |");
        foreach (var sample in Items(symmetry, "Samples"))
            md.AppendLine($"| {Cell(Text(sample, "Id"))} | {Cell(Text(sample, "Motif"))} | {Cell(Text(sample, "Name"))} | {Cell(Text(sample, "Rule"))} | {Text(sample, "Steps")} | {Text(sample, "Sections")} |");
        md.AppendLine();
        md.AppendLine("### Order and phase comparisons");
        md.AppendLine();
        md.AppendLine("Raw equality compares the recorded finite outputs. Aligned equality compares the explicitly supplied correspondence. The cyclic comparison validates actual cell slots, the connected placement action, its doubled period and inverse wrap, plus an explicit permission to read modulo that period. Forward continuation alone does not grant that permission. No arbitrary best-looking crop is a proof of universal equivalence. A directed match is still weaker than identical timing or producer history.");
        md.AppendLine();
        md.AppendLine("| Comparison | From → to | Raw | Under supplied alignment | Directed | Shift steps | Qualification |");
        md.AppendLine("| --- | --- | --- | --- | --- | ---: | --- |");
        foreach (var pair in Items(symmetry, "Comparisons"))
            md.AppendLine($"| {Cell(Text(pair, "Name"))} | {Cell(Text(pair, "From"))} → {Cell(Text(pair, "To"))} | {Text(pair, "RawEqual")} | {Text(pair, "AlignedEqual")} | {Text(pair, "DirectedEqual")} | {Text(pair, "ShiftSteps")} | {Cell(Text(pair, "Note"))} |");
        md.AppendLine();
        md.AppendLine("## 3. A neighborhood of constructions");
        md.AppendLine();
        md.AppendLine("This is a small set of supplied neighboring constructions, not an exhaustive map of shape space. The labels name useful reads; they are not new primitive shape opcodes. A rotated square, a rhombus, a pentagon, an open walk and a contour boundary do not all arise by relaxing the same condition.");
        md.AppendLine();
        md.AppendLine("| Example | Kind of change | What changed | What remains | Limits |");
        md.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var shape in Items(neighborhood, "Shapes"))
            md.AppendLine($"| {Cell(Text(shape, "Name"))} | {Cell(Text(shape, "ChangeKind"))} | {Cell(Text(shape, "Change"))} | {Cell(Text(shape, "Retains"))} | {Cell(Text(shape, "Limits"))} |");
        md.AppendLine();
        md.AppendLine("| Example | Equal section lengths | Perpendicular joins | Components | Sections | Contour markers | Whole-contour read |");
        md.AppendLine("| --- | --- | --- | ---: | ---: | ---: | --- |");
        foreach (var shape in Items(neighborhood, "Shapes"))
            md.AppendLine($"| {Cell(Text(shape, "Name"))} | {Text(shape, "EqualSectionLengths")} | {Text(shape, "PerpendicularJoins")} | {Text(shape, "Components")} | {Text(shape, "Sections")} | {Text(shape, "RadialParts")} | {Text(shape, "WholeContour")} |");
        md.AppendLine();
        md.AppendLine("Equal section lengths compares the retained sections, which need not be maximal geometric sides. Perpendicular joins checks adjacent section directions; it admits both a convex 90-degree corner and a concave 270-degree interior corner. It does not certify convexity.");
        md.AppendLine();
        foreach (var finding in Items(neighborhood, "Findings")) md.AppendLine("- " + Text(finding));
        md.AppendLine();
        md.AppendLine("The finite diagonal substitution is (x,y) → (x−y,x+y). It actively copies and transforms the coordinate words into a new output, leaving the source unchanged; it is not merely a different observer's view. In the displayed Euclidean basis it includes both a 45-degree rotation and a √2 scale, so it is not a length-preserving rotation. Stretching before this substitution can retain perpendicular joins; stretching after it can retain equal section lengths while changing the joins. The order of actions matters.");
        md.AppendLine();
        md.AppendLine("Radial illustrations are regular display layouts of the supplied native contour, marker count and successor structure. Pixel locations, drawn straight chords and the smooth circle are presentation choices. They do not establish missing metric settlement, chord lengths, area or a general regular-polygon solver. 'Not read' in a property field remains unavailable evidence, not a negative result.");
        md.AppendLine();
        md.AppendLine("## What this prepares, and what remains open");
        md.AppendLine();
        md.AppendLine("The useful output is a collection of bounded correspondences and counterexamples: where two machines overlap, what an extra constraint costs, and what changes when the next source changes. These are candidates for later machine exchange, prediction and learned preferences. The present study supplies the candidate machines, input envelope, correspondences and transformations; it does not discover them from drawings or learn a new vocabulary autonomously.");
        md.AppendLine();
        md.AppendLine("Changing a candidate is not permission to mutate an observed source. Participating zero is zero at the selected resolution; it does not remove the participating section or prove absolute absence. Geometric equality does not establish topology, identical flow, quality, taste or interoperability under every future input. More general relaxation, settlement, interpolation and machine selection need their own connected laws and evidence.");
        File.WriteAllText(Path.Combine(output, "README.md"), md.ToString());
    }

    private static IEnumerable<JsonElement> Items(JsonElement value, string key) => value.GetProperty(key).EnumerateArray();
    private static bool Flag(JsonElement value, string key) => value.GetProperty(key).GetBoolean();
    private static string Text(JsonElement value, string key) => value.TryGetProperty(key, out var item) ? Text(item) : "Not read";
    private static string Text(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? "Not read",
        JsonValueKind.Null or JsonValueKind.Undefined => "Not read",
        JsonValueKind.True => "Yes",
        JsonValueKind.False => "No",
        JsonValueKind.Array => string.Join("; ", value.EnumerateArray().Select(Text)),
        _ => value.ToString()
    };
    private static string Cell(string value) => value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");

    private const string Page = """
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Gojo · Construction families</title>
<style>
:root{color-scheme:light;--ink:#20323b;--muted:#596b70;--paper:#f4f3ec;--border:#d8dfd9;--green:#21664e;--blue:#2c608b;--purple:#79559b;--red:#a14c3b;--amber:#a56a14}*{box-sizing:border-box}body{margin:0;color:var(--ink);background:var(--paper);font:16px/1.55 system-ui,sans-serif}main{max-width:1440px;margin:auto;padding:32px 28px 70px}h1{font:600 clamp(29px,3.6vw,48px)/1.13 Georgia,serif;max-width:1050px;margin:12px 0 17px}h2{font-size:24px;line-height:1.25;margin:0 0 11px}h3{font-size:17px;line-height:1.35;margin:0 0 8px}p{margin:0 0 12px}a{color:var(--blue)}.kicker{font-size:12px;letter-spacing:.16em;text-transform:uppercase;font-weight:750;color:var(--green)}.intro{font-size:18px;max-width:1000px}.muted,small{color:var(--muted)}.lead{background:#e7eee5;border-left:4px solid var(--green);padding:15px 18px;margin:20px 0}.stats{display:flex;flex-wrap:wrap;gap:9px;margin:20px 0}.stat{background:#fff;border:1px solid var(--border);border-radius:7px;padding:9px 13px;font-size:13px}.stat strong{font-size:24px;margin-right:5px}.tabs{display:flex;gap:8px;flex-wrap:wrap;border-bottom:1px solid #b8c5bf;padding-bottom:12px;margin:30px 0 25px}button,select{font:inherit;color:var(--ink);background:#fff;border:1px solid #9eaeaa;border-radius:5px;padding:9px 12px;max-width:100%}button{cursor:pointer}button:hover{background:#eaf1eb}button[aria-selected=true]{background:var(--green);color:white;border-color:var(--green)}button:focus-visible,select:focus-visible,a:focus-visible,input:focus-visible{outline:3px solid #c28226;outline-offset:3px}.controls{display:flex;gap:12px;align-items:end;flex-wrap:wrap;background:#e9eeea;padding:15px;border-radius:8px}.controls label{display:flex;flex-direction:column;gap:5px;font-size:13px;font-weight:650;max-width:100%;min-width:0}.controls select{min-width:180px}.check{display:flex!important;flex-direction:row!important;align-items:center;padding:9px 0}.check input{width:18px;height:18px}.panel{background:white;border:1px solid var(--border);border-radius:10px;padding:22px;margin:20px 0;min-width:0}.grid{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:15px}.two{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px}.card{background:#fff;border:1px solid var(--border);border-radius:9px;padding:15px;min-width:0}.card.selected{box-shadow:inset 0 0 0 2px var(--blue)}.card.bad{background:#f8efeb}.card p{font-size:14px}.card svg,figure svg{display:block;width:100%;height:220px;border-radius:6px;background:#fafaf5;margin:8px 0 12px}.card.compact svg{height:160px}.card .facts,.facts{font-size:12px;color:var(--muted)}.card button{font-size:13px}.tag{display:inline-block;margin:2px 4px 7px 0;background:#edf2eb;color:var(--green);font-size:12px;font-weight:650;border-radius:4px;padding:3px 7px}.tag.open{background:#fff0d6;color:#855d16}.tag.blue{background:#e5edf5;color:var(--blue)}.tag.bad{background:#f4e1db;color:var(--red)}.callout{padding:13px 16px;background:#edf2ee;border-left:3px solid #759b83;font-size:14px;margin:16px 0}.callout.warn{background:#fbf3e3;border-color:#c39137}.table-scroll{overflow-x:auto;max-width:100%;margin:12px 0}table{border-collapse:collapse;width:100%;font-size:13px}th,td{text-align:left;vertical-align:top;border-bottom:1px solid var(--border);padding:9px 11px}th{background:#f0f3ed}.matrix td{text-align:center;padding:4px}.matrix th{font-size:12px;padding:6px}.matrix button{min-width:38px;padding:5px;font-size:12px}.matrix button.equal{background:#d6eadb;color:#20563c}.matrix button.subset{background:#dcebf5;color:#275475}.matrix button.some{background:#f9edcf;color:#805b18}.matrix button.none{background:#f1e7e1;color:#80584c}.matrix button.picked{outline:2px solid var(--blue);outline-offset:-2px}.yes{color:var(--green);font-weight:700}.no{color:var(--red);font-weight:700}.unknown{color:var(--amber);font-weight:650}figure{margin:0;min-width:0}figcaption{font-size:14px}.note{border-top:2px solid var(--border);padding-top:17px;margin-top:25px;font-size:14px}.tiny{font-size:12px}.section-label{font:600 10px system-ui;paint-order:stroke;stroke:#fafaf5;stroke-width:3px;stroke-linejoin:round}.axis{stroke:#d8dfd9;stroke-width:1;stroke-dasharray:3 5}.sample-title{display:flex;justify-content:space-between;align-items:start;gap:12px;flex-wrap:wrap}.sample-title h3{margin:0}.step-strip{display:flex;gap:7px;flex-wrap:wrap;font-size:13px;align-items:center;margin:12px 0}.step-strip span{padding:6px 9px;background:#e9edf3;border-radius:5px}.caption{font-size:13px;color:var(--muted);margin-top:7px}.footer{font-size:13px;color:var(--muted);margin-top:30px}details{border:1px solid var(--border);padding:15px;background:#fff;border-radius:8px;margin:20px 0}summary{cursor:pointer;font-weight:650}details p:first-of-type{margin-top:12px}ul{padding-left:23px}li{margin:6px 0}[hidden]{display:none!important}
@media(max-width:1050px){.grid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:650px){main{padding:22px 13px 45px}.grid,.two{grid-template-columns:1fr}.panel{padding:14px}.controls label{width:100%}.controls select{min-width:0;width:100%}.tabs{gap:5px}.tabs button{font-size:13px;padding:9px}.stat strong{font-size:21px}.intro{font-size:16px}.matrix th:first-child{min-width:140px}.card svg,figure svg{height:200px}}
</style></head><body><main>
<div class="kicker">Gojo · Bounded construction study</div>
<h1>What can a machine become?</h1>
<p class="intro">A square is one result. Its construction tells us which changes are easy, which are forbidden, and which need a different machine. Here we compare whole <strong>finite ranges of outputs</strong>, then examine repetition, feedback and neighboring constructions.</p>
<div class="lead"><strong>Three different questions:</strong> What outputs does this machine admit? What happens when a transform reuses a different source? What must change to reach a nearby shape? Similar pictures do not give the same answer to all three.</div>
<p><a href="README.md">Complete Markdown notes</a> · <a href="results.json">Recorded observations</a> · <a href="#limits">Scope and remaining gaps</a></p>
<p class="tiny muted">Native evidence: <a href="ranges.gojo">construction ranges</a> · <a href="symmetry.gojo">symmetry machines</a> · <a href="neighborhood.gojo">shape neighborhood</a>. These retain connected evidence beyond the rendered summary.</p>
<div class="stats" id="stats"></div>
<noscript><p class="lead">This page displays three recorded studies: finite output-range intersections, symmetry composition and neighboring shape constructions. The Markdown notes contain their tables and qualifications without JavaScript.</p></noscript>
<nav class="tabs" role="tablist" aria-label="Construction studies"><button id="tab-ranges" role="tab" aria-selected="true" aria-controls="ranges">1 · Output ranges</button><button id="tab-symmetry" role="tab" aria-selected="false" aria-controls="symmetry" tabindex="-1">2 · Repeat, mirror & source</button><button id="tab-neighborhood" role="tab" aria-selected="false" aria-controls="neighborhood" tabindex="-1">3 · Nearby constructions</button></nav>
<div class="controls" style="margin-bottom:22px"><label class="check"><input id="arrows" type="checkbox" checked>Show section direction</label><label class="check"><input id="labels" type="checkbox">Show section numbers</label><span class="tiny muted">Arrow keys move through a focused selector; its focus stays in place.</span></div>

<section id="ranges" role="tabpanel" aria-labelledby="tab-ranges">
<h2>One shape versus a range of shapes</h2>
<p>Each machine receives the tested width, height and notch bindings. We keep the admitted results, collapse only duplicates under the selected outline read, and compare those finite sets.</p>
<p class="callout" id="domain"></p>
<div class="controls"><label>First family<select id="range-first"></select></label><label>Second family<select id="range-second"></select></label><label>Compare these outputs<select id="range-read"><option value="false">Undirected outlines</option><option value="true">Directed outlines</option></select></label></div>
<div id="range-summary" aria-live="polite"></div>
<div class="two" id="range-family-details"></div>
<section class="panel"><h3>Representative inputs</h3><p class="muted">These are selectable examples from each family, not necessarily a matching pair. The matrix and counts compare the entire recorded admitted range. All range drawings use one scale.</p><p class="caption">W and H name offered input roles, not guaranteed dimensions of the output. A machine may reuse one role and leave another unused; the six-equal-step walk has a fixed 2:1 aspect ratio even when its offered H changes.</p><div class="controls"><label>First input<select id="range-first-sample"></select></label><label>Second input<select id="range-second-sample"></select></label></div><div class="two" id="range-drawings" style="margin-top:16px"></div></section>
<section class="panel"><h3>Where the ranges differ</h3><p class="muted">A witness below is admitted by one family but absent from the other family's tested output set. Absence outside this finite envelope has not been checked.</p><p class="caption">The same input envelope can produce different output scales. Six equal steps reuse W more times; a larger six-step rectangle may exceed the four-step inventory's sampled width. This is a finite sampling limit, not evidence that the free rectangle machine cannot make it with a larger input.</p><div class="two" id="range-exclusive"></div></section>
<section class="panel"><h3>Overlap map</h3><p class="muted">Each cell counts outputs shared by its row and column. Green means the tested sets are equal, blue means one contains the other, amber means partial overlap, and brown means none. Click a cell to inspect the pair.</p><div class="table-scroll" id="range-matrix"></div><p class="tiny muted">Outline matching preserves repeated line occurrences. Direction is a separate read. This map does not prove topology, execution order, unrestricted substitutability or aesthetic quality.</p></section>
<details><summary>All supplied family constraints and recorded sizes</summary><div id="range-table" class="table-scroll"></div></details>
<div class="two"><section class="note"><h3>Why the corner repeat may not add rectangles</h3><p>A four-step walk with independent horizontal and vertical reaches already permits rectangles of different aspect ratios. Rewriting those steps as a corner plus its half-turn changes the program and its correspondence. It need not enlarge the geometric range.</p><p>The stronger tied-square machine has an additional equality. Releasing that equality really does admit new bindings.</p></section><section class="note"><h3>Why a hidden notch is still structure</h3><p>A notch whose reach reads zero can retain extra sites and phases. Giving the retained notch a nonzero reach makes the detour visible. A four-step program without those phases cannot acquire them merely by changing a width.</p><p>A participating zero means zero at this resolution. It does not delete the section or establish absolute absence.</p></section></div>
</section>

<section id="symmetry" role="tabpanel" aria-labelledby="tab-symmetry" hidden>
<h2>The action is only half of repetition</h2>
<p>A mirror or translation must act on something. Reusing the original, reusing the previous result and reusing the whole accumulated group are different connected machines.</p>
<p class="caption">These are copied observations, not conserved material fanout. A reported phase step is one placement slot in the supplied rule, not necessarily one native reach or one drawn section. An orange ×n label marks n coincident section drawings in the same direction; it does not merge their identities.</p>
<div class="grid"><article class="card"><h3>Original source</h3><div class="step-strip"><span>original</span>→<span>same action</span>→<span>another copy</span></div><p>A fixed action places every copy in the same transformed position. To advance copies, the machine also needs an advancing placement input.</p></article><article class="card"><h3>Previous result</h3><div class="step-strip"><span>last result</span>→<span>action</span>→<span>next source</span></div><p>The transform accumulates through feedback. A reflection may alternate while a translation keeps advancing.</p></article><article class="card"><h3>Whole group</h3><div class="step-strip"><span>all retained copies</span>→<span>action</span>→<span>larger group</span></div><p>Each step acts on more participation. Copies can coincide; fused-looking ink does not erase that multiplicity.</p></article></div>
<section class="panel"><div class="controls"><label>Inspect a machine<select id="symmetry-sample"></select></label><button type="button" id="symmetry-prev">← Previous</button><button type="button" id="symmetry-next">Next →</button></div><div id="symmetry-focus" aria-live="polite" style="margin-top:18px"></div></section>
<div class="grid" id="symmetry-cards"></div>
<section class="panel"><h3>Order and phase: compare the recorded outputs</h3><p>These comparisons use explicit correspondences. A finite crop has a beginning and an end; a phase shift may change those boundaries even when the interior has a recurring pattern.</p><div class="controls"><label>Comparison<select id="symmetry-comparison"></select></label></div><div class="two" id="symmetry-pair" style="margin-top:18px"></div><div id="symmetry-result" aria-live="polite"></div><div id="symmetry-aligned"></div></section>
<p class="callout warn"><strong>Phase is a declared read.</strong> The cyclic case checks the actual slots, placement action, period and inverse wrap, together with an explicit permission to read modulo that period. Forward continuation alone does not grant this permission. The matching correspondence does not make finite endpoints disappear or prove all possible continuations.</p>
</section>

<section id="neighborhood" role="tabpanel" aria-labelledby="tab-neighborhood" hidden>
<h2>“Nearby” can mean several different edits</h2>
<p>A rectangle, a rotated square, a rhombus and a pentagon are nearby pictures. Reaching them can require a released binding, a geometric word substitution, a different connection pattern, or a different boundary construction. The substitutions here actively create transformed copies; they are not merely observer changes.</p>
<div class="controls"><label>Neighbor to inspect<select id="neighbor"></select></label><button type="button" id="neighbor-prev">← Previous</button><button type="button" id="neighbor-next">Next →</button></div>
<div class="panel" id="neighbor-focus" aria-live="polite"></div>
<div class="callout"><strong>Try the two orders:</strong> stretch an axis-aligned square, then apply the diagonal substitution; compare that with applying the diagonal substitution first, then stretching horizontally. One can preserve perpendicular joins, the other equal section lengths. The two machines retain different promises. The diagonal map includes a √2 scale as well as a 45-degree turn.</div>
<p class="caption"><strong>Read the property names narrowly:</strong> equal section lengths need not mean equal maximal sides. Perpendicular joins include concave 270-degree interior corners as well as convex 90-degree corners; this read does not certify convexity.</p>
<div class="grid" id="neighbor-cards"></div>
<section class="panel"><h3>What this neighborhood reveals</h3><ul id="neighbor-findings"></ul></section>
<p class="callout warn"><strong>Radial drawings are diagrams.</strong> Their regular display layout illustrates a supplied native contour and marker/successor structure. The renderer does not prove chord lengths, area, calibrated angles or a general polygon settlement. A smooth contour drawing is not the limit of an executed infinite polygon.</p>
</section>

<section class="note" id="limits"><h2>What the results do—and do not—establish</h2><p>The candidate programs, finite bindings, action schedules and comparison correspondences are supplied. The native structures execute and retain evidence; this page only renders their recorded reads. No generator was learned from the pictures, and no observed source is automatically rewritten.</p><div id="range-limits"></div><p>Boundary equality, topology, flow, timing, history and aesthetic quality remain different questions. Directed line agreement does not establish a common stroke order. Equal screen coordinates do not establish a shared site. A better hypothesis grants no permission to mutate the source.</p><p>The next useful evidence would come from testing predictions beyond this input envelope, finding a correspondence under an additional contract, or learning which machine change is useful in context. None of those future behaviors follows merely from a similar outline.</p></section>
<p class="footer">This gallery and its Markdown companion are generated from the same recorded run. Numbers count finite supplied participation in a selected basis; screen positions are presentation only.</p>
</main><script type="application/json" id="data">@@DATA@@</script><script>
'use strict';
const data=JSON.parse(document.getElementById('data').textContent),R=data.Ranges,S=data.Symmetry,N=data.Neighborhood;
const $=id=>document.getElementById(id),esc=v=>String(v??'Not read').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const colors=['#286788','#79559b','#287554','#aa6b21','#ae5741','#536b91'];
const bool=v=>v===null||v===undefined?'<span class="unknown">Not read</span>':`<span class="${v?'yes':'no'}">${v?'Yes':'No'}</span>`;
const arrayText=v=>Array.isArray(v)?v.join(' '):String(v??'');
const allRangeLines=R.Samples.flatMap(s=>s.Lines),allNeighborLines=N.Shapes.flatMap(s=>s.Lines||[]);
function bounds(lines){const points=lines.flat();if(!points.length)return[-1,-1,1,1];const xs=points.map(p=>p[0]),ys=points.map(p=>p[1]);return[Math.min(...xs),Math.min(...ys),Math.max(...xs),Math.max(...ys)];}
const rangeBounds=bounds(allRangeLines),neighborBounds=bounds(allNeighborLines);
function drawing(lines,title,color=colors[0],frame=null){
 const b=frame||bounds(lines),w=Math.max(1,b[2]-b[0]),h=Math.max(1,b[3]-b[1]),scale=Math.min(334/w,166/h),cx=(b[0]+b[2])/2,cy=(b[1]+b[3])/2;
 const px=x=>190+(x-cx)*scale,py=y=>110-(y-cy)*scale;
 let out=`<svg viewBox="0 0 380 220" role="img" aria-label="${esc(title)}"><line class="axis" x1="18" y1="${py(0)}" x2="362" y2="${py(0)}"/><line class="axis" x1="${px(0)}" y1="18" x2="${px(0)}" y2="202"/>`;
 const zeros=new Map();
 lines.forEach((line,i)=>{if(line.length<1)return;const a=line[0],z=line[line.length-1],x=px(a[0]),y=py(a[1]),u=px(z[0]),v=py(z[1]),dx=u-x,dy=v-y,len=Math.hypot(dx,dy);
  if(len===0){const k=a.join(','),count=zeros.get(k)||0;zeros.set(k,count+1);out+=`<circle cx="${x}" cy="${y}" r="${4+count*3}" fill="none" stroke="#ad741b" stroke-width="2"/>`;return;}
  out+=`<polyline points="${line.map(p=>px(p[0])+','+py(p[1])).join(' ')}" fill="none" stroke="${color}" stroke-width="2.8" stroke-linecap="round" stroke-linejoin="round"/><circle cx="${x}" cy="${y}" r="2" fill="#fafaf5" stroke="${color}"/>`;
  const nx=dx/len,ny=dy/len,mx=x+dx*.57,my=y+dy*.57;
  if($('arrows').checked)out+=`<path d="M ${mx-nx*5-ny*3} ${my-ny*5+nx*3} L ${mx+nx*4} ${my+ny*4} L ${mx-nx*5+ny*3} ${my-ny*5-nx*3} Z" fill="${color}"/>`;
  if($('labels').checked)out+=`<text class="section-label" x="${(x+u)/2-ny*10}" y="${(y+v)/2+nx*10+3}" text-anchor="middle" fill="${color}">${i+1}</text>`;
 });
 const occurrences=new Map();for(const line of lines){const key=JSON.stringify(line),entry=occurrences.get(key);if(entry)entry.count++;else occurrences.set(key,{line,count:1});}
 for(const {line,count} of occurrences.values()){if(count<2||line.length<2)continue;const a=line[0],z=line[line.length-1];if(a[0]===z[0]&&a[1]===z[1])continue;const dx=px(z[0])-px(a[0]),dy=py(z[1])-py(a[1]),len=Math.hypot(dx,dy);out+=`<text class="section-label" text-anchor="middle" x="${(px(a[0])+px(z[0]))/2+dy/len*12}" y="${(py(a[1])+py(z[1]))/2-dx/len*12+3}" fill="#a06412">×${count}</text>`;}
 return out+'</svg>';
}
function radial(shape){
 const n=shape.RadialParts||0,cx=190,cy=108,r=78,points=Array.from({length:n},(_,i)=>[cx+r*Math.cos(-Math.PI/2+i*2*Math.PI/n),cy+r*Math.sin(-Math.PI/2+i*2*Math.PI/n)]);
 let out=`<svg viewBox="0 0 380 220" role="img" aria-label="${esc(shape.Name)}; contour illustration only"><circle cx="${cx}" cy="${cy}" r="${r}" fill="none" stroke="${shape.WholeContour?colors[2]:'#b6c8bc'}" stroke-width="${shape.WholeContour?3:1.5}" ${shape.WholeContour?'':'stroke-dasharray="4 5"'}/>`;
 if(n){out+=`<polygon points="${points.map(p=>p.join(',')).join(' ')}" fill="none" stroke="${colors[2]}" stroke-width="2.6"/>`;points.forEach((p,i)=>{out+=`<line x1="${cx}" y1="${cy}" x2="${p[0]}" y2="${p[1]}" stroke="#d5dfd5" stroke-width="1"/><circle cx="${p[0]}" cy="${p[1]}" r="4" fill="#fafaf5" stroke="${colors[2]}" stroke-width="2"/>`;if($('labels').checked)out+=`<text class="section-label" x="${p[0]+8}" y="${p[1]-7}" fill="${colors[2]}">${i+1}</text>`;});}
 out+=`<circle cx="${cx}" cy="${cy}" r="3" fill="#698b76"/><text x="190" y="211" text-anchor="middle" font-size="11" fill="#68786e">${shape.WholeContour?'Whole contour illustration':'Regular display layout · metric settlement not inferred'}</text></svg>`;return out;
}
function shapeDrawing(s){return s.WholeContour||s.RadialParts>0?radial(s):drawing(s.Lines,s.Name,colors[2],neighborBounds);}
function sampleName(s){return `W role ${s.Width} · H role ${s.Height} · notch ${s.Notch}${s.Admissible?'':' · rejected / unresolved'}`;}
function family(id){return R.Families.find(f=>f.Id===id);}
function range(id){return R.Ranges.find(f=>f.Family===id);}
function comparison(a,b,directed){const row=R.Comparisons.find(c=>c.Directed===directed&&((c.First===a&&c.Second===b)||(c.First===b&&c.Second===a)));if(!row)return null;if(row.First===a)return row;return{...row,First:a,Second:b,FirstCount:row.SecondCount,SecondCount:row.FirstCount,FirstOnly:row.SecondOnly,SecondOnly:row.FirstOnly,FirstSubsetOfSecond:row.SecondSubsetOfFirst,SecondSubsetOfFirst:row.FirstSubsetOfSecond,FirstOnlySample:row.SecondOnlySample,SecondOnlySample:row.FirstOnlySample};}
function setSamples(selectId,familyId){const select=$(selectId),previous=select.value;select.replaceChildren();R.Samples.filter(s=>s.Family===familyId).forEach(s=>select.add(new Option(sampleName(s),s.Id)));if([...select.options].some(o=>o.value===previous))select.value=previous;else{const preferred=R.Samples.find(s=>s.Family===familyId&&s.Admissible&&s.Width===3&&s.Height===2)||R.Samples.find(s=>s.Family===familyId&&s.Admissible);if(preferred)select.value=preferred.Id;}}
function rangeFigure(s,label,color){if(!s)return `<article class="card"><h3>${esc(label)}</h3><p>No witness is needed: the tested set has no exclusive output in this direction.</p></article>`;return `<article class="card ${s.Admissible?'':'bad'}"><h3>${esc(label)}</h3><span class="tag ${s.Admissible?'':'bad'}">${s.Admissible?'Admitted':'Rejected / unresolved'}</span>${drawing(s.Lines,sampleName(s),color,rangeBounds)}<p>${esc(sampleName(s))}</p><p class="facts">${s.Sections} sections · ${s.ZeroSections} zero-at-resolution sections · ${s.Failed} failed · ${s.Unresolved} unresolved obligations</p><p class="facts">Reference: ${esc(s.Id)}</p></article>`;}
function renderRangeDrawings(){const a=R.Samples.find(s=>s.Id===$('range-first-sample').value),b=R.Samples.find(s=>s.Id===$('range-second-sample').value);$('range-drawings').innerHTML=rangeFigure(a,family($('range-first').value).Name,colors[0])+rangeFigure(b,family($('range-second').value).Name,colors[1]);}
function renderRanges(reset=true){
 const a=$('range-first').value,b=$('range-second').value,d=$('range-read').value==='true',c=comparison(a,b,d);
 if(reset){setSamples('range-first-sample',a);setSamples('range-second-sample',b);}
 $('range-family-details').innerHTML=[a,b].map(id=>{const f=family(id),r=range(id);return `<article class="card"><h3>${esc(f.Name)}</h3><p><strong>Retains:</strong> ${esc(f.Constraint)}</p><p><strong>Can vary:</strong> ${esc(f.Release)}</p><p class="facts">${r.Attempts} attempts · ${r.Admitted} admitted · ${r.Rejected} rejected · ${r.Unresolved} unresolved</p><span class="tag blue">${d?r.UniqueDirected:r.UniqueBoundaries} distinct ${d?'directed ':''}outlines</span></article>`;}).join('');
 if(c){const same=c.FirstOnly===0&&c.SecondOnly===0;let message=same?'The two tested output sets are equal under this read.':c.FirstSubsetOfSecond?'Every tested first-family output occurs in the second family; the second also permits extra outputs.':c.SecondSubsetOfFirst?'Every tested second-family output occurs in the first family; the first also permits extra outputs.':c.Intersection?'The tested ranges overlap, and each has outputs the other did not produce.':'These tested output ranges do not overlap under the selected read.';
  $('range-summary').innerHTML=`<div class="callout"><strong>${c.Intersection} shared outputs.</strong> ${message} This concerns the supplied finite envelope.</div><div class="stats"><div class="stat"><strong>${c.FirstOnly}</strong>first only</div><div class="stat"><strong>${c.Intersection}</strong>shared</div><div class="stat"><strong>${c.SecondOnly}</strong>second only</div></div>`;
  const sa=R.Samples.find(s=>s.Id===c.FirstOnlySample),sb=R.Samples.find(s=>s.Id===c.SecondOnlySample);$('range-exclusive').innerHTML=rangeFigure(sa,'First family only',colors[0])+rangeFigure(sb,'Second family only',colors[1]);
 }else{$('range-summary').innerHTML='<p class="callout">Choose two different families for a recorded pair comparison. The same family is shown on both sides.</p>';$('range-exclusive').innerHTML='';}
 renderRangeDrawings();renderMatrix();
}
function renderMatrix(){const d=$('range-read').value==='true',a=$('range-first').value,b=$('range-second').value;
 $('range-matrix').innerHTML=`<table class="matrix"><thead><tr><th>Family / shared count</th>${R.Families.map((f,i)=>`<th title="${esc(f.Name)}">${i+1}</th>`).join('')}</tr></thead><tbody>${R.Families.map((f,i)=>`<tr><th>${i+1}. ${esc(f.Name)}</th>${R.Families.map(g=>{if(f.Id===g.Id)return `<td>${d?range(f.Id).UniqueDirected:range(f.Id).UniqueBoundaries}</td>`;const c=comparison(f.Id,g.Id,d);if(!c)return '<td>—</td>';const type=c.FirstOnly===0&&c.SecondOnly===0?'equal':c.FirstSubsetOfSecond||c.SecondSubsetOfFirst?'subset':c.Intersection?'some':'none';return `<td><button type="button" data-first="${esc(f.Id)}" data-second="${esc(g.Id)}" class="${type} ${f.Id===a&&g.Id===b?'picked':''}" aria-label="Compare ${esc(f.Name+' and '+g.Name)}: ${c.Intersection} shared outputs">${c.Intersection}</button></td>`;}).join('')}</tr>`).join('')}</tbody></table>`;
 $('range-matrix').querySelectorAll('button').forEach(button=>button.addEventListener('click',()=>{$('range-first').value=button.dataset.first;$('range-second').value=button.dataset.second;renderRanges();$('range-summary').scrollIntoView({behavior:'smooth',block:'center'});}));
}
function renderSymmetry(){
 const s=S.Samples.find(x=>x.Id===$('symmetry-sample').value);if(s){$('symmetry-focus').innerHTML=`<div class="two"><div><h3>${esc(s.Name)}</h3><span class="tag blue">${esc(s.Motif)}</span><p>${esc(s.Rule)}</p><p class="facts">${s.Steps} steps · ${s.Sections} retained sections</p><p class="caption">Coincident occurrences may render as one stroke. The section count preserves their participation; the drawing alone cannot show the full history.</p></div><figure>${drawing(s.Lines,s.Name)}<figcaption>Recorded native line positions. Scale fits this sample.</figcaption></figure></div>`;}
 $('symmetry-cards').innerHTML=S.Samples.map((s,i)=>`<article class="card compact ${s.Id===$('symmetry-sample').value?'selected':''}"><h3>${esc(s.Name)}</h3><span class="tag">${esc(s.Motif)}</span>${drawing(s.Lines,s.Name,colors[i%colors.length])}<p class="facts">${s.Steps} steps · ${s.Sections} sections</p><p>${esc(s.Rule)}</p><button type="button" data-sample="${esc(s.Id)}">Inspect machine</button></article>`).join('');
 $('symmetry-cards').querySelectorAll('button').forEach(button=>button.addEventListener('click',()=>{$('symmetry-sample').value=button.dataset.sample;renderSymmetry();$('symmetry-focus').scrollIntoView({behavior:'smooth',block:'center'});}));
 renderSymmetryPair();
}
function renderSymmetryPair(){const c=S.Comparisons.find(x=>x.Id===$('symmetry-comparison').value);if(!c)return;const a=S.Samples.find(x=>x.Id===c.From),b=S.Samples.find(x=>x.Id===c.To),aligned=c.AlignedLines||[],frame=bounds([...(a?.Lines||[]),...(b?.Lines||[]),...aligned]);
 $('symmetry-pair').innerHTML=[a,b].map((s,i)=>s?`<figure><h3>${esc(s.Name)}</h3>${drawing(s.Lines,s.Name,colors[i],frame)}<figcaption>${esc(s.Rule)}</figcaption></figure>`:'<p>Sample unavailable.</p>').join('');
 $('symmetry-result').innerHTML=`<div class="table-scroll"><table><thead><tr><th>Raw finite equality</th><th>Equality after alignment</th><th>Directed equality after alignment</th><th>Shift slots</th></tr></thead><tbody><tr><td>${bool(c.RawEqual)}</td><td>${bool(c.AlignedEqual)}</td><td>${bool(c.DirectedEqual)}</td><td>${c.ShiftSteps}</td></tr></tbody></table></div><p class="callout">${esc(c.Note)}</p>`;
 $('symmetry-aligned').innerHTML=aligned.length?`<figure><h3>Recorded aligned output</h3>${drawing(aligned,'Output under the supplied alignment',colors[2],frame)}<figcaption>The supplied correspondence is applied for this comparison. Its finite-window qualification remains in the note above.</figcaption></figure>`:'';
}
function renderNeighbors(){const s=N.Shapes.find(x=>x.Id===$('neighbor').value);if(s){$('neighbor-focus').innerHTML=`<div class="two"><div><h3>${esc(s.Name)}</h3><span class="tag blue">${esc(s.ChangeKind)}</span><p><strong>Change:</strong> ${esc(s.Change)}</p><p><strong>Retains:</strong> ${esc(s.Retains)}</p><p class="callout warn">${esc(s.Limits)}</p></div><figure>${shapeDrawing(s)}<figcaption>${s.WholeContour||s.RadialParts>0?'Illustration of the contour promise; not a derived metric realization.':'Native line coordinates in the same display basis as the other non-radial neighbors.'}</figcaption></figure></div><div class="table-scroll"><table><thead><tr><th>Equal section lengths</th><th>Perpendicular joins</th><th>Components</th><th>Sections</th></tr></thead><tbody><tr><td>${bool(s.EqualSectionLengths)}</td><td>${bool(s.PerpendicularJoins)}</td><td>${esc(s.Components)}</td><td>${esc(s.Sections)}</td></tr></tbody></table></div>`;}
 $('neighbor-cards').innerHTML=N.Shapes.map(s=>`<article class="card compact ${s.Id===$('neighbor').value?'selected':''}"><h3>${esc(s.Name)}</h3><span class="tag blue">${esc(s.ChangeKind)}</span>${shapeDrawing(s)}<p>${esc(s.Change)}</p><p class="facts">Equal section lengths: ${bool(s.EqualSectionLengths)} · perpendicular joins: ${bool(s.PerpendicularJoins)}</p><button type="button" data-neighbor="${esc(s.Id)}">Inspect change</button></article>`).join('');
 $('neighbor-cards').querySelectorAll('button').forEach(button=>button.addEventListener('click',()=>{$('neighbor').value=button.dataset.neighbor;renderNeighbors();$('neighbor-focus').scrollIntoView({behavior:'smooth',block:'center'});}));
}
function changeSelect(id,delta){const s=$(id);s.selectedIndex=(s.selectedIndex+delta+s.options.length)%s.options.length;s.dispatchEvent(new Event('change'));}
function selectTab(id,focus=false){for(const name of ['ranges','symmetry','neighborhood']){const selected=name===id,button=$('tab-'+name);$(name).hidden=!selected;button.setAttribute('aria-selected',String(selected));button.tabIndex=selected?0:-1;if(selected&&focus)button.focus();}}
for(const [i,id] of ['ranges','symmetry','neighborhood'].entries()){const tab=$('tab-'+id);tab.addEventListener('click',()=>selectTab(id));tab.addEventListener('keydown',event=>{if(!['ArrowLeft','ArrowRight','Home','End'].includes(event.key))return;event.preventDefault();const index=event.key==='Home'?0:event.key==='End'?2:(i+(event.key==='ArrowLeft'?-1:1)+3)%3;selectTab(['ranges','symmetry','neighborhood'][index],true);});}
for(const f of R.Families){$('range-first').add(new Option(f.Name,f.Id));$('range-second').add(new Option(f.Name,f.Id));}
$('range-first').value=R.Families[0].Id;$('range-second').value=R.Families[Math.min(1,R.Families.length-1)].Id;
S.Samples.forEach(s=>$('symmetry-sample').add(new Option(s.Motif+' · '+s.Name,s.Id)));S.Comparisons.forEach(c=>$('symmetry-comparison').add(new Option(c.Name,c.Id)));N.Shapes.forEach(s=>$('neighbor').add(new Option(s.Name,s.Id)));
$('domain').textContent=arrayText(R.Domain);
$('stats').innerHTML=[['supplied families',R.Families.length],['input trials',R.Samples.length],['symmetry samples',S.Samples.length],['neighbor constructions',N.Shapes.length]].map(([label,n])=>`<div class="stat"><strong>${n}</strong>${label}</div>`).join('');
$('range-table').innerHTML=`<table><thead><tr><th>Family</th><th>Retains</th><th>Can vary</th><th>Admitted / tried</th><th>Distinct outline / directed</th></tr></thead><tbody>${R.Families.map(f=>{const r=range(f.Id);return `<tr><td>${esc(f.Name)}</td><td>${esc(f.Constraint)}</td><td>${esc(f.Release)}</td><td>${r.Admitted} / ${r.Attempts}</td><td>${r.UniqueBoundaries} / ${r.UniqueDirected}</td></tr>`;}).join('')}</tbody></table>`;
$('range-limits').innerHTML='<ul>'+R.Limits.map(x=>'<li>'+esc(x)+'</li>').join('')+'</ul>';$('neighbor-findings').innerHTML=N.Findings.map(x=>'<li>'+esc(x)+'</li>').join('');
for(const id of ['range-first','range-second'])$(id).addEventListener('change',()=>renderRanges());$('range-read').addEventListener('change',()=>renderRanges(false));for(const id of ['range-first-sample','range-second-sample'])$(id).addEventListener('change',renderRangeDrawings);
$('symmetry-sample').addEventListener('change',renderSymmetry);$('symmetry-comparison').addEventListener('change',renderSymmetryPair);$('neighbor').addEventListener('change',renderNeighbors);
$('symmetry-prev').addEventListener('click',()=>changeSelect('symmetry-sample',-1));$('symmetry-next').addEventListener('click',()=>changeSelect('symmetry-sample',1));$('neighbor-prev').addEventListener('click',()=>changeSelect('neighbor',-1));$('neighbor-next').addEventListener('click',()=>changeSelect('neighbor',1));
for(const id of ['arrows','labels'])$(id).addEventListener('change',()=>{renderRanges(false);renderSymmetry();renderNeighbors();});
document.querySelectorAll('select').forEach(select=>select.addEventListener('keydown',event=>{if(event.altKey||event.ctrlKey||event.metaKey||!['ArrowLeft','ArrowRight','ArrowUp','ArrowDown'].includes(event.key))return;event.preventDefault();changeSelect(event.currentTarget.id,['ArrowLeft','ArrowUp'].includes(event.key)?-1:1);}));
renderRanges();renderSymmetry();renderNeighbors();
</script></body></html>
""";
}

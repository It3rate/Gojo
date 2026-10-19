using System.Text;
using Gojo.Core;
using N = Gojo.Experiments.QualitativeFrieze.FieldNative;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class BourgoinColourExperiment
{
    private static string Svg(string id, string title, IReadOnlyList<DrawPath> paths, HashSet<Relation>[] layers,
        OrnamentFieldPacket packet, BourgoinPatternLibrary.DisplayBasis basis, Palette palette, int columns, int rows,
        bool fitAll = false)
    {
        const double width = 1200, height = 820, margin = 34;
        var all = paths.SelectMany(path => path.Points).ToArray();
        var centre = new Point((all.Min(point => point.X) + all.Max(point => point.X)) / 2,
            (all.Min(point => point.Y) + all.Max(point => point.Y)) / 2);
        var layout = (Relation)packet.Program.Layout.Right; var slots = (Relation)layout.Left;
        Relation Bound(IElement slot) => (Relation)N.E(packet.Bindings).Cast<Relation>().Single(row => ReferenceEquals(row.Left, slot)).Right;
        var u = Position(packet.Program.Frame, Bound(slots.Left), basis); var v = Position(packet.Program.Frame, Bound(slots.Right), basis);
        var determinant = u.X * v.Y - u.Y * v.X;
        if (Math.Abs(determinant) < 1e-10) throw new InvalidOperationException("The screen read needs two independent repeat directions.");
        var aspect = (width - 2 * margin) / (height - 2 * margin);
        var maxU = (Math.Abs(v.Y) * aspect + Math.Abs(v.X)) / (2 * Math.Abs(determinant));
        var maxV = (Math.Abs(u.Y) * aspect + Math.Abs(u.X)) / (2 * Math.Abs(determinant));
        var worldHeight = Math.Min(((columns - 1) / 2d - .12) / maxU, ((rows - 1) / 2d - .12) / maxV);
        if (fitAll) worldHeight = Math.Max((all.Max(point => point.Y) - all.Min(point => point.Y)),
            (all.Max(point => point.X) - all.Min(point => point.X)) / aspect) * 1.16;
        var scale = (height - 2 * margin) / worldHeight;
        Point Screen(Point point) => new(width / 2 + (point.X - centre.X) * scale, height / 2 - (point.Y - centre.Y) * scale);
        var band = Math.Clamp(palette.Band * scale, 1.8, 25);
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {F(width)} {F(height)}' role='img' aria-label='{H(title)}' data-focus='motif' data-view='ornament' style='--ground:{palette.Ground};--field:{palette.Field};--motif:{palette.Motif};--detail:{palette.Detail};--gold:{palette.Gold};--pale:{palette.Pale};--ink:{palette.Ink};--band:{F(band)}px;--detail-strength:1'><title>{H(title)}</title>");
        svg.Append("""
<style>
.ground{fill:var(--ground)}.base{fill:none;stroke:var(--gold);stroke-width:.85;opacity:.28}.layer{stroke-linecap:round;stroke-linejoin:round}
.edge{fill:none;stroke:var(--ink);stroke-width:calc(var(--band) + 3px)}.keyline{fill:none;stroke:var(--pale);stroke-width:calc(var(--band) + 1.4px)}
.pigment{fill:none;stroke:var(--colour);stroke-width:var(--band)}.mass{fill:var(--colour);stroke:none}
.field{--colour:var(--field);opacity:.86}.motif{--colour:var(--motif);opacity:1}.detail{--colour:var(--detail);opacity:calc(.8 * var(--detail-strength))}
.detail .edge{stroke-width:2.6px}.detail .keyline{stroke-width:1.7px}.detail .pigment{stroke-width:1px}
.motif .edge{stroke-width:calc(var(--band) * .55 + 3px)}.motif .keyline{stroke-width:calc(var(--band) * .55 + 1.4px)}.motif .pigment{stroke-width:calc(var(--band) * .55)}
[data-focus=field] .field{opacity:1}[data-focus=field] .motif{opacity:.4}[data-focus=field] .detail{opacity:calc(.4 * var(--detail-strength))}
[data-focus=detail] .field{opacity:.58}[data-focus=detail] .motif{opacity:.48}[data-focus=detail] .detail{opacity:var(--detail-strength)}
[data-view=skeleton] .layer{display:none}[data-view=skeleton] .base{stroke:var(--pale);opacity:.9;stroke-width:1.6}
.anchors{display:none;fill:var(--detail);stroke:var(--ink);stroke-width:1}[data-anchors=true] .anchors{display:block}.frame{fill:none;stroke:var(--gold);stroke-width:1.2}
</style>
""");
        svg.Append($"<defs><clipPath id='{id}-crop'><rect x='{margin}' y='{margin}' width='{width - 2 * margin}' height='{height - 2 * margin}'/></clipPath>");
        foreach (var path in paths)
        {
            var d = string.Join(" ", path.Points.Select((point, index) => { var p = Screen(point); return (index == 0 ? "M " : "L ") + F(p.X) + " " + F(p.Y); })) + (path.Closed ? " Z" : "");
            svg.Append($"<path id='{id}-{path.Id}' d='{d}'/>");
        }
        svg.Append($"</defs><rect width='1200' height='820' class='ground'/><g clip-path='url(#{id}-crop)'><g class='base'>");
        foreach (var path in paths) svg.Append($"<use href='#{id}-{path.Id}'/>");
        svg.Append("</g>");
        // Paint each closed mass once, in its coarsest selected layer.
        // Overlapping observational selections can then emphasise its line
        // again without covering a sibling layer's interior ornament.
        var filled = new HashSet<string>(StringComparer.Ordinal);
        for (var layer = 0; layer < layers.Length; layer++)
        {
            svg.Append($"<g class='layer {Roles[layer]}' data-native-layer='{layer}'>");
            foreach (var path in paths.Where(path => path.Closed && layers[layer].Contains(path.Source)))
                if (filled.Add(path.Id)) svg.Append($"<use class='mass' href='#{id}-{path.Id}'/>");
            svg.Append("</g>");
        }
        for (var layer = 0; layer < layers.Length; layer++)
        {
            var selected = paths.Where(path => layers[layer].Contains(path.Source)).ToArray();
            svg.Append($"<g class='layer {Roles[layer]}' data-native-layer='{layer}'>");
            foreach (var pass in new[] { "edge", "keyline", "pigment" })
                foreach (var path in selected) svg.Append($"<use class='{pass}{(path.Closed ? " closed" : "")}' href='#{id}-{path.Id}'/>");
            svg.Append("</g>");
        }
        svg.Append("<g class='anchors'>");
        foreach (var path in paths.Where(path => layers[2].Contains(path.Source)))
        { var point = Screen(path.Points[0]); svg.Append($"<circle cx='{F(point.X)}' cy='{F(point.Y)}' r='3.4'/>"); }
        return svg.Append("</g></g><rect x='18' y='18' width='1164' height='784' class='frame'/><rect x='24' y='24' width='1152' height='772' class='frame'/></svg>").ToString();
    }

    private static string Html(IReadOnlyList<SceneResult> results, string output)
    {
        var cards = new StringBuilder();
        foreach (var result in results)
        {
            var source = Path.GetRelativePath(output, Path.Combine("experiments", "QualitativeFrieze", "results", "bourgoin-patterns", "gallery.html")).Replace('\\', '/');
            var options = string.Join("", Roles.Select((role, index) => $"<option value='{role}'{(role == "motif" ? " selected" : "")}>{Labels[index]}</option>"));
            cards.Append($"<article id='{result.Id}'><header><span class='eyebrow'>{result.Id} / {result.SourceId}</span><h2>{H(result.Title)}</h2><p class='palette'>{H(result.Palette)}</p></header><div class='plate'>{result.Svg}</div><div class='card-controls'><label>Bring forward <select class='focus'>{options}</select></label><span class='focus-note'>Intermediate motif is prominent; all three levels remain.</span></div><p>{H(result.Description)}</p><div class='facts'><span>{result.Paths} native paths / {result.Chords} chords per unit</span><span>{result.AddedPaths} additions / {result.Attachments} attachment witnesses</span><span>{result.Accepted} accepted seams / {result.Blocked} blocked / {result.Open} open</span></div>");
            cards.Append($"<details><summary>Construction, focus and limits</summary><p>Field / motif / detail select {string.Join(" / ", result.LayerPaths)} paths. Selections may overlap without consuming or duplicating source participation. Each focus carries the other two native layers. The {result.Units}-unit finite field is executed after archive import; its {result.Frontier} frontier obligations are distinct from interior failures. The screen crops the accepted field's interior.</p><p>Finite chords and actual landmark attachments generate the ornament. Colour, edging and filled closed contours are chosen screen interpretations; they do not prove occupied native faces, analytic curves, tangent joins or knot order. The palette interprets Jones's guidance without claiming historical pigment calibration or universal beauty.</p><p><a href='{result.Id}.gojo.json'>Native program</a> · <a href='{result.Id}-focus.gojo.json'>Three native focus reads</a> · <a href='{source}#{result.SourceId}'>Source skeleton and engraving</a> · <a href='{result.Id}.svg'>Default SVG</a> · <a href='{result.Id}-field.svg'>Field focus</a> · <a href='{result.Id}-detail.svg'>Detail focus</a></p></details></article>");
        }
        var findings = Path.GetRelativePath(output, "Gojo/docs/GojoColouredOrnamentStudy.md").Replace('\\', '/');
        return """
<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Colour &amp; the reading of ornament · Gojo</title><style>
*{box-sizing:border-box}body{margin:0;background:#eee7d8;color:#293d40;font:17px/1.65 system-ui,sans-serif}main{max-width:1460px;margin:auto;padding:50px 32px 80px}
h1,h2{font-family:Georgia,serif;font-weight:400;line-height:1.12}h1{font-size:clamp(40px,5vw,76px);max-width:900px;margin:14px 0 22px}h2{font-size:34px;margin:8px 0}
.eyebrow{font-size:12px;letter-spacing:.18em;text-transform:uppercase;color:#806539}.intro{max-width:900px;font-size:19px}a{color:#27606c;text-underline-offset:4px}nav{display:flex;flex-wrap:wrap;gap:22px;margin:25px 0}
.toolbar{position:sticky;top:0;z-index:5;display:flex;flex-wrap:wrap;align-items:center;gap:15px 28px;background:#f6f0e2f5;border-top:1px solid #c7baa0;border-bottom:1px solid #c7baa0;padding:15px 20px;backdrop-filter:blur(6px)}
label{display:flex;align-items:center;gap:10px;font-size:14px}select{border:1px solid #bbae92;background:#fffaf0;padding:7px;color:#293d40;font:inherit}input[type=range]{width:125px}
article{margin:52px 0 80px;border-bottom:1px solid #cbbfa5;padding-bottom:36px}article header{margin-bottom:20px}.palette{margin:10px 0;color:#7a6851;font-size:14px}
.plate{border:1px solid #bbae92;box-shadow:0 12px 32px #4a39201a}.plate svg{display:block;width:100%;height:auto}
.card-controls{display:flex;flex-wrap:wrap;align-items:center;gap:20px;margin-top:18px}.focus-note{font-size:13px;color:#6d756c}article>p{max-width:1050px}
.facts{display:flex;flex-wrap:wrap;gap:10px 24px;font-size:13px;color:#6c745f}details{margin-top:18px;max-width:1100px}summary{cursor:pointer;color:#395d63;font-size:15px}details p{font-size:14px}
.sources{max-width:1050px;border-top:1px solid #c7baa0;padding-top:25px}.sources p{font-size:15px}
@media(min-width:1100px){.collection{display:grid;grid-template-columns:1fr 1fr;gap:38px}.collection article{margin:36px 0;padding-bottom:30px}.collection h2{font-size:28px}.card-controls{align-items:flex-start;gap:8px}.facts{display:block}.facts span{display:block}}
@media(max-width:600px){main{padding:25px 14px 50px}h2{font-size:27px}.toolbar{padding:10px;gap:10px}.toolbar label{font-size:12px}.toolbar select{max-width:145px}article{margin:35px 0}input[type=range]{width:90px}.card-controls label{flex-wrap:wrap}}
</style></head><body><main><span class='eyebrow'>Gojo / an ornament interlude</span><h1>One construction.<br>Several ways to see it.</h1><p class='intro'>Four geometric skeletons, enriched through their own landmarks. Colour can bring forward the large field, a repeated motif, or its smaller ornament. Switch the focus: the other levels stay present, quieter and still connected.</p><nav><a href='overview.html'>The four plates together</a><a href='overview.svg'>Whole collection · SVG</a><a href='overview.png'>Whole collection · PNG</a><a href='#sources'>Jones's rules and references</a></nav><div class='toolbar'><label>All plates <select id='global-focus'><option value='motif'>Intermediate motif</option><option value='field'>Overall disposition</option><option value='detail'>Fine ornament</option></select></label><label>Detail contrast <input id='detail-contrast' type='range' min='.1' max='1' step='.01' value='1'></label><label><input id='skeleton' type='checkbox'>Construction only</label><label><input id='anchors' type='checkbox'>Detail start landmarks</label></div><section class='collection'>
""" + cards + $"""
</section><section id='sources' class='sources'><span class='eyebrow'>A selected grammar, not a beauty theorem</span><h2>Structure first. Enrichment in its place.</h2><p>Jones's consulted 1856 edition has 37 propositions. We use proposition 7's order of construction and enrichment; 14 and 16's clarifying colour and restrained strong accents; and 28–32's separating outlines. The Moresque chapter describes main lines, nearer detail and detail on that detail. It also describes bringing different chains in one construction into prominence through colouring.</p><p><a href='https://archive.org/details/grammarornament00Jone/page/n13/mode/1up'>Construction · propositions 1–13</a> · <a href='https://archive.org/details/grammarornament00Jone/page/n14/mode/1up'>Colour · 14–22</a> · <a href='https://archive.org/details/grammarornament00Jone/page/n15/mode/1up'>Edging · 23–37</a> · <a href='https://archive.org/details/grammarornament00Jone/page/n130/mode/1up'>Three distances</a> · <a href='https://archive.org/details/grammarornament00Jone/page/n134/mode/1up'>Alternate chains</a> · <a href='https://archive.org/details/grammarornament00Jone/page/n142/mode/1up'>Colour-plate inspiration</a></p><p>Our colours are display choices rather than pigment reconstructions. These are authored demonstrations. They do not train a beauty model, prove human preference, or change native geometry when a focus control moves. The controls select three precomputed native focus reads and apply their display emphasis.</p><p><a href='{H(findings)}'>Construction study and reproduction</a> · <a href='diagnostics.json'>Execution diagnostics</a></p></section>
""" + """
</main><script>
function focus(card,value){card.querySelector('svg').dataset.focus=value;card.querySelector('.focus').value=value;card.querySelector('.focus-note').textContent=({field:'Overall disposition',motif:'Intermediate motif',detail:'Fine ornament'})[value]+' is prominent; all three levels remain.';}
for(const card of document.querySelectorAll('article'))card.querySelector('.focus').addEventListener('change',e=>focus(card,e.target.value));
document.querySelector('#global-focus').addEventListener('change',e=>{for(const card of document.querySelectorAll('article'))focus(card,e.target.value);});
document.querySelector('#detail-contrast').addEventListener('input',e=>{for(const svg of document.querySelectorAll('.plate svg'))svg.style.setProperty('--detail-strength',e.target.value);});
document.querySelector('#skeleton').addEventListener('change',e=>{for(const svg of document.querySelectorAll('.plate svg'))svg.dataset.view=e.target.checked?'skeleton':'ornament';});
document.querySelector('#anchors').addEventListener('change',e=>{for(const svg of document.querySelectorAll('.plate svg'))svg.dataset.anchors=e.target.checked?'true':'false';});
</script></body></html>
""";
    }

    private static string Overview(IReadOnlyList<SceneResult> results)
    {
        var svg = new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='2440' height='1840' viewBox='0 0 2440 1840'><rect width='2440' height='1840' fill='#eee7d8'/><text x='34' y='55' font-family='Georgia,serif' font-size='37' fill='#293d40'>Colour and the reading of ornament</text><text x='34' y='91' font-family='sans-serif' font-size='18' fill='#6c745f'>Four native constructions · shared landmarks, three connected levels · Jones-inspired colour interpretations</text>");
        for (var index = 0; index < results.Count; index++)
        {
            var result = results[index]; var x = 28 + index % 2 * 1206; var y = 125 + index / 2 * 850;
            var root = System.Xml.Linq.XElement.Parse(result.Svg); root.SetAttributeValue("x", x); root.SetAttributeValue("y", y + 40);
            root.SetAttributeValue("width", 1172); root.SetAttributeValue("height", 800);
            svg.Append($"<a xlink:href='gallery.html#{result.Id}'><text x='{x + 3}' y='{y + 23}' font-family='Georgia,serif' font-size='25' fill='#293d40'>{H(result.Id + " · " + result.Title)}</text>{root.ToString(System.Xml.Linq.SaveOptions.DisableFormatting)}</a>");
        }
        return svg.Append("</svg>").ToString();
    }
}

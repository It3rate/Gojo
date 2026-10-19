using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Gojo.Experiments.QualitativeFrieze;

public static partial class BourgoinColourExperiment
{
    private static string DesignHtml(IReadOnlyList<DesignResult> results, string output, bool overview)
    {
        var groups = results.GroupBy(result => result.Plate.SourceId).ToArray();
        var cards = new StringBuilder(); var nav = new StringBuilder();
        foreach (var group in groups)
        {
            var ordered = Roles.Select(role => group.Single(result => result.Mode == role)).ToArray();
            nav.Append($"<a href='#{group.Key}'>{H(group.Key)}</a>");
            cards.Append($"<article id='{group.Key}'><header><span class='eyebrow'>{H(group.Key)} · one retained skeleton</span><h2>{H(ordered[0].SkeletonName)}</h2></header>");
            if (overview)
            {
                cards.Append("<div class='triptych'>");
                foreach (var result in ordered)
                {
                    var plate = result.Plate;
                    cards.Append($"<figure><a href='{plate.Id}.svg'><img loading='lazy' decoding='async' src='{plate.Id}.svg' width='1200' height='820' alt='{H(plate.Title)}'></a><figcaption><b>{Labels[Array.IndexOf(Roles, result.Mode)]}</b><span>{H(plate.Title)}</span></figcaption></figure>");
                }
                cards.Append("</div>");
            }
            else
            {
                var lead = ordered[1].Plate;
                cards.Append($"<a class='hero' href='{lead.Id}.svg'><img loading='lazy' decoding='async' src='{lead.Id}.svg' width='1200' height='820' alt='{H(lead.Title)}'></a><div class='choices' role='group' aria-label='Choose the ornament design'>");
                foreach (var result in ordered)
                    cards.Append($"<button type='button' class='choice' data-mode='{result.Mode}' aria-pressed='{(result.Mode == "motif" ? "true" : "false")}'><img loading='lazy' decoding='async' src='{result.Plate.Id}.svg' width='1200' height='820' alt=''><span>{Labels[Array.IndexOf(Roles, result.Mode)]}</span></button>");
                cards.Append($"</div><p class='selected-title'>{H(lead.Title)} <span>{H(lead.Palette)}</span></p><p class='description'>{H(lead.Description)}</p>");
            }
            var source = Path.GetRelativePath(output, Path.Combine("experiments", "QualitativeFrieze", "results", "bourgoin-patterns", "gallery.html")).Replace('\\', '/');
            cards.Append("<details><summary>Native construction and design notes</summary>");
            foreach (var result in ordered)
            {
                var plate = result.Plate;
                cards.Append($"<section><b>{H(plate.Id)} · {Labels[Array.IndexOf(Roles, result.Mode)]}</b><p>{H(plate.Description)}</p><p>{plate.Paths} paths / {plate.Chords} chords per unit · {plate.AddedPaths} additions with {plate.Attachments} witnessed attachments · {plate.Accepted} accepted seams · {plate.Blocked} blocked / {plate.Open} open</p><p><a href='{plate.Id}.svg'>Full-size SVG</a> · <a href='{plate.Id}-construction.svg'>All native paths</a> · <a href='{plate.Id}.gojo.json.gz'>Native program · gzip</a> · <a href='{plate.Id}-focus.gojo.json.gz'>Native focus reads · gzip</a></p></section>");
            }
            cards.Append($"<p><a href='{source}#{group.Key}'>Original skeleton and engraving</a></p>");
            if (File.Exists(Path.Combine(output, "first-fine", $"O-{group.Key}-detail.svg")))
                cards.Append($"<p><a href='first-fine/O-{group.Key}-detail.svg'>Earlier fine-scale draft</a> · <a href='first-fine/O-{group.Key}-detail.gojo.json.gz'>Earlier native construction</a></p>");
            cards.Append("</details></article>");
        }
        var metadata = JsonSerializer.Serialize(results.Select(result => new { result.Mode, result.Plate.Id, result.Plate.SourceId,
            result.Plate.Title, result.Plate.Description, result.Plate.Palette }), new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var findings = Path.GetRelativePath(output, "Gojo/docs/GojoOrnamentDesignCollection.md").Replace('\\', '/');
        return """
<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>A grammar in colour · Gojo ornament collection</title><style>
*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:#eee8db;color:#303e41;font:16px/1.6 system-ui,sans-serif}main{max-width:1600px;margin:auto;padding:42px 28px 80px}h1,h2{font-family:Georgia,serif;font-weight:400;line-height:1.15}h1{font-size:clamp(40px,5vw,76px);margin:12px 0 24px;max-width:1100px}h2{font-size:29px;margin:8px 0 19px}.eyebrow{font-size:12px;letter-spacing:.16em;text-transform:uppercase;color:#83673e}.intro{font-size:19px;max-width:1050px}a{color:#275d68;text-underline-offset:4px}nav{display:flex;gap:12px 24px;flex-wrap:wrap;margin:24px 0}.index{gap:8px 18px;padding:18px 0;border-top:1px solid #cfc3ac;border-bottom:1px solid #cfc3ac}.index a{font-size:13px;text-decoration:none}.tools{position:sticky;top:0;z-index:5;display:flex;gap:18px;flex-wrap:wrap;align-items:center;padding:14px 18px;background:#f6f0e4f5;border:1px solid #c9bda6;backdrop-filter:blur(8px)}label{font-size:14px;display:flex;gap:10px;align-items:center}select{font:inherit;background:#fff9ee;color:#303e41;border:1px solid #bcae94;padding:7px}button{font:inherit;cursor:pointer}article{scroll-margin-top:90px;border-bottom:1px solid #cdbfa5;padding:30px 0 35px;min-width:0}.hero{display:block}.hero img{width:100%;height:auto;display:block;border:1px solid #bbae96}.choices{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:9px;margin-top:12px}.choice{padding:0;background:#f6f0e4;color:#4d5c59;border:2px solid transparent;min-width:0;text-align:left}.choice[aria-pressed=true]{border-color:#887043;background:#e4dac6}.choice img{display:block;width:100%;height:auto}.choice span{display:block;padding:6px 8px;font-size:12px}.selected-title{font-family:Georgia,serif;font-size:22px;line-height:1.3;margin:18px 0 12px}.selected-title span{display:block;font:13px/1.6 system-ui;color:#796c56;margin-top:7px}.description{font-size:14px}.triptych{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:16px}figure{margin:0;min-width:0}figure img{display:block;width:100%;height:auto;border:1px solid #bbae96}figcaption{font-size:13px;padding:9px 0}figcaption b,figcaption span{display:block}figcaption span{color:#776952}details{margin-top:18px;font-size:13px}summary{cursor:pointer;color:#395e65}details section{border-top:1px solid #d7ccb6;padding-top:12px;margin-top:15px}details p{margin:8px 0}.sources{margin-top:42px;max-width:1100px;font-size:14px}.hidden{display:none!important}.empty{display:none}
@media(min-width:1100px){.collection:not(.all){display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px 38px}.all h2{font-size:26px}}
@media(max-width:650px){main{padding:28px 16px 55px}.triptych{grid-template-columns:1fr}.tools{position:static}.tools label{min-width:0;max-width:100%}.tools select{min-width:0;max-width:70vw}.index{max-height:160px;overflow:auto}.choice span{font-size:10px;padding:5px}.triptych figcaption{margin-bottom:15px}h2{font-size:25px}}
@media print{.tools,.index,nav,details{display:none}.collection:not(.all){display:block}.triptych{grid-template-columns:repeat(3,minmax(0,1fr))}article{break-inside:avoid}.hero img{max-height:85vh;width:auto;max-width:100%}}
</style></head><body><main>
""" + $"""
<span class='eyebrow'>Gojo · an authored ornament collection</span><h1>A grammar in colour</h1><p class='intro'>{groups.Length} skeletons. {results.Count} designs. Broad disposition, recurring ornament and close detail each receive a different construction over the same skeleton. Look for a lattice becoming brocade, a fret carrying leaves, or a star becoming a garden.</p><nav><a href='gallery.html'>Browse and compare</a><a href='overview.html'>Every design on one page</a><a href='overview.svg'>Whole collection · SVG</a><a href='overview.png'>Whole collection · PNG</a><a href='#principles'>Design principles</a></nav>
""" + (overview ? "<p>Read each row from overall disposition to intermediate motif to fine ornament. Click any plate for its full-size vector image.</p>" :
"""
<div class='tools'><label>All skeletons <select id='global-mode'><option value='motif'>Intermediate motif</option><option value='field'>Overall disposition</option><option value='detail'>Fine ornament</option></select></label><label><input id='construction' type='checkbox'> Show all native paths</label><label>Jump to <select id='jump'><option value=''>Choose a skeleton…</option>
""" + string.Join("", groups.Select(group => $"<option value='{group.Key}'>{H(group.Key)} · {H(group.First().SkeletonName)}</option>")) +
"""
</select></label></div>
""") + $"<nav class='index' aria-label='Skeleton catalogue'>{nav}</nav><div class='collection{(overview ? " all" : "")}'>{cards}</div>" +
$"""
<section id='principles' class='sources'><span class='eyebrow'>Construction · subdivision · enrichment</span><h2>The detail serves the design.</h2><p>Jones's hierarchy of construction and enrichment guides these plates: keep a clear distant arrangement, give the repeated unit its own character, and let finer ornament reward a closer view. Repeated prototypes, geometric conventionalisation, stems carrying leaves, balanced empty spaces and separating outlines give us a small reusable design vocabulary. Strong colour is concentrated in selected roles; other levels provide quieter support.</p><p>Each of the three versions is a separate native enrichment program. They share their source skeleton and retain its repetition, public landmarks and seam obligations. The added rosettes, leaves, insets and veins are finite Gojo paths, attached through actual public ports and retained offset witnesses. Colour, stroke width, edging, filled contours and the rectangular crop are display choices. No analytic curve, occupied face or automatic beauty judgement is claimed.</p><p><a href='https://archive.org/details/grammarornament00Jone/page/n13/mode/1up'>Jones · construction</a> · <a href='https://archive.org/details/grammarornament00Jone/page/n14/mode/1up'>Colour</a> · <a href='https://archive.org/details/grammarornament00Jone/page/n15/mode/1up'>Edging</a> · <a href='https://archive.org/details/grammarornament00Jone/page/n130/mode/1up'>Three viewing distances</a> · <a href='https://archive.org/details/grammarornament00Jone/page/n142/mode/1up'>Historical colour inspiration</a></p><p><a href='{H(findings)}'>Construction study and reproduction</a> · <a href='diagnostics.json'>Native execution diagnostics</a></p></section></main>
""" + "<script type='application/json' id='design-data'>" + metadata + "</script>" + """
<script>
const designs=JSON.parse(document.querySelector('#design-data').textContent);
let construction=false;
function choose(card,mode){
 const item=designs.find(d=>d.sourceId===card.id&&d.mode===mode);if(!item)return;
 card.dataset.mode=mode;
 const image=card.querySelector('.hero img');if(!image)return;
 image.src=item.id+(construction?'-construction':'')+'.svg';image.alt=item.title;card.querySelector('.hero').href=image.src;
 for(const button of card.querySelectorAll('.choice'))button.setAttribute('aria-pressed',button.dataset.mode===mode?'true':'false');
 const title=card.querySelector('.selected-title');title.replaceChildren(document.createTextNode(item.title));const palette=document.createElement('span');palette.textContent=item.palette;title.append(palette);
 card.querySelector('.description').textContent=item.description;
}
for(const card of document.querySelectorAll('article')){card.dataset.mode='motif';for(const button of card.querySelectorAll('.choice'))button.addEventListener('click',()=>choose(card,button.dataset.mode));}
document.querySelector('#global-mode')?.addEventListener('change',e=>{for(const card of document.querySelectorAll('article'))choose(card,e.target.value);});
document.querySelector('#construction')?.addEventListener('change',e=>{construction=e.target.checked;for(const card of document.querySelectorAll('article'))choose(card,card.dataset.mode);});
document.querySelector('#jump')?.addEventListener('change',e=>{if(e.target.value)document.getElementById(e.target.value)?.scrollIntoView({behavior:'smooth'});});
</script></body></html>
""";
    }

    private static string DesignOverview(IReadOnlyList<DesignResult> results)
    {
        var groups = results.GroupBy(result => result.Plate.SourceId).ToArray();
        const int width = 2460, tileWidth = 786, tileHeight = 538, rowHeight = 638, header = 160;
        var height = header + groups.Length * rowHeight + 25;
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='{width}' height='{height}' viewBox='0 0 {width} {height}'><rect width='{width}' height='{height}' fill='#eee8db'/><text x='34' y='57' font-family='Georgia,serif' font-size='40' fill='#303e41'>A grammar in colour</text><text x='34' y='92' font-family='sans-serif' font-size='19' fill='#756a55'>{groups.Length} retained skeletons · {results.Count} native enrichment designs · three different constructions per skeleton</text>");
        for (var mode = 0; mode < Roles.Length; mode++)
            svg.Append($"<text x='{34 + mode * 813}' y='133' font-family='sans-serif' font-size='18' fill='#756a55'>{Labels[mode]}</text>");
        for (var row = 0; row < groups.Length; row++)
        {
            var group = groups[row]; var y = header + row * rowHeight;
            svg.Append($"<text x='34' y='{y + 25}' font-family='Georgia,serif' font-size='24' fill='#303e41'>{H(group.Key + " · " + group.First().SkeletonName)}</text>");
            for (var mode = 0; mode < Roles.Length; mode++)
            {
                var result = group.Single(result => result.Mode == Roles[mode]);
                var root = XElement.Parse(result.Plate.Svg);
                root.SetAttributeValue("x", 28 + mode * 813); root.SetAttributeValue("y", y + 44);
                root.SetAttributeValue("width", tileWidth); root.SetAttributeValue("height", tileHeight);
                svg.Append($"<a xlink:href='gallery.html#{group.Key}'>{root.ToString(SaveOptions.DisableFormatting)}</a>");
                svg.Append($"<text x='{34 + mode * 813}' y='{y + 609}' font-family='sans-serif' font-size='16' fill='#756a55'>{H(result.Plate.Id + " · " + result.Plate.Palette)}</text>");
            }
        }
        return svg.Append("</svg>").ToString();
    }
}

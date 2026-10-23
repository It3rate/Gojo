using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Display-only projection of native band, boundary and visibility reads.
/// Path commands, masks, colours and viewport clipping do not feed native meaning.</summary>
public static class BandEvidenceGallery
{
    public readonly record struct ViewBox(double X, double Y, double Width, double Height);
    public sealed record MaskCut(string Data, double StrokeWidth = 0);
    public sealed record InkPath(string Data, string Fill = "none", string Stroke = "#285762",
        double StrokeWidth = .08, string Note = "", string Role = "ink", string? Dash = null,
        double Opacity = 1, IReadOnlyList<MaskCut>? Cuts = null, string Cap = "round",
        string Join = "round", string FillRule = "nonzero");
    public sealed record Panel(string Name, IReadOnlyList<InkPath> Paths, string Note = "",
        ViewBox? Bounds = null, string Background = "#fbf8ee", bool FlipY = true);
    public sealed record Evidence(string Name, string Outcome, string Basis, string Note);
    public sealed record Example(string Id, string Name, string Note, IReadOnlyList<Panel> Panels,
        IReadOnlyList<Evidence> Evidence, object Audit, string? NativePacket = null);

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static void Write(string output, object summary, IReadOnlyList<Example> examples)
    {
        Directory.CreateDirectory(output);
        Directory.CreateDirectory(Path.Combine(output, "patterns"));
        var rows = new List<object>();
        var atlas = new StringBuilder("<main class='atlas'>");
        foreach (var example in examples)
        {
            var panels = example.Panels.Select((panel, index) =>
            {
                var image = $"patterns/{Token(example.Id)}-{index + 1}.svg";
                WriteText(Path.Combine(output, image), Svg(panel, example.Id + " · " + panel.Name));
                return new { panel.Name, panel.Note, Image = image, Drawing = panel };
            }).ToArray();
            rows.Add(new { example.Id, example.Name, example.Note, Panels = panels,
                example.Evidence, example.Audit, example.NativePacket });
            var href = "gallery.html?case=" + Uri.EscapeDataString(example.Id);
            atlas.Append($"<article class='tile'><h2><a href='{href}'>{H(example.Id)} · {H(example.Name)}</a></h2><div class='tile-panels'>");
            foreach (var panel in panels)
                atlas.Append($"<figure><a href='{panel.Image}'><img src='{panel.Image}' alt='{H(panel.Name)}' loading='lazy'></a><figcaption>{H(panel.Name)}</figcaption></figure>");
            atlas.Append($"</div><p>{H(example.Note)}</p><a class='text-link' href='{href}'>Inspect relations and read witnesses →</a></article>");
        }
        atlas.Append("</main>");
        var data = JsonSerializer.Serialize(new { Schema = "band-evidence-gallery-v1", summary, Examples = rows }, Json);
        WriteText(Path.Combine(output, "gallery-data.json"), data);
        WriteText(Path.Combine(output, "gallery.html"), Page("Gojo · paths, bands and boundaries",
            Header + Metrics(summary) + Inspector.Replace("PAYLOAD", data, StringComparison.Ordinal)));
        WriteText(Path.Combine(output, "overview.html"), Page("Gojo · paths and bands atlas",
            Header + Metrics(summary) + atlas));
    }

    private static string Svg(Panel panel, string title)
    {
        var box = panel.Bounds ?? new ViewBox(-12, -8, 24, 16);
        if (!double.IsFinite(box.X) || !double.IsFinite(box.Y) || !double.IsFinite(box.Width)
            || !double.IsFinite(box.Height) || box.Width <= 0 || box.Height <= 0)
            throw new ArgumentException("Band presentation needs a finite positive viewport.");
        var rect = $"x='{F(box.X)}' y='{F(box.Y)}' width='{F(box.Width)}' height='{F(box.Height)}'";
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='{F(box.X)} {F(box.Y)} {F(box.Width)} {F(box.Height)}' role='img'><title>{H(title)}</title><desc>{H(panel.Note)} Artwork is a display read of native relations; clipping does not close an open path.</desc><rect {rect} fill='{H(panel.Background)}'/><defs><clipPath id='viewport'><rect {rect}/></clipPath>");
        for (var index = 0; index < panel.Paths.Count; index++)
        {
            var path = panel.Paths[index];
            if (path.Cuts is not { Count: > 0 } cuts) continue;
            svg.Append($"<mask id='visibility-{index}' maskUnits='userSpaceOnUse' {rect}><rect {rect} fill='white'/>");
            foreach (var cut in cuts)
            {
                if (!double.IsFinite(cut.StrokeWidth) || cut.StrokeWidth < 0)
                    throw new ArgumentException("Display mask width must be finite and nonnegative.");
                svg.Append($"<path d='{H(cut.Data)}' fill='{(cut.StrokeWidth == 0 ? "black" : "none")}' stroke='{(cut.StrokeWidth == 0 ? "none" : "black")}' stroke-width='{F(cut.StrokeWidth)}' stroke-linecap='round' stroke-linejoin='round'/>");
            }
            svg.Append("</mask>");
        }
        svg.Append("</defs>");
        svg.Append($"<g clip-path='url(#viewport)'{(panel.FlipY ? $" transform='translate(0 {F(2 * box.Y + box.Height)}) scale(1 -1)'" : "")}>");
        for (var index = 0; index < panel.Paths.Count; index++)
        {
            var path = panel.Paths[index];
            if (!double.IsFinite(path.StrokeWidth) || path.StrokeWidth < 0
                || !double.IsFinite(path.Opacity) || path.Opacity < 0 || path.Opacity > 1)
                throw new ArgumentException("Band display width and opacity must be finite and legal.");
            svg.Append($"<path class='ink {H(Token(path.Role))}' data-role='{H(path.Role)}' d='{H(path.Data)}' fill='{H(path.Fill)}' fill-rule='{H(path.FillRule)}' stroke='{H(path.Stroke)}' stroke-width='{F(path.StrokeWidth)}' stroke-linecap='{H(path.Cap)}' stroke-linejoin='{H(path.Join)}' opacity='{F(path.Opacity)}'{(path.Dash is null ? "" : $" stroke-dasharray='{H(path.Dash)}'")}{(path.Cuts is { Count: > 0 } ? $" mask='url(#visibility-{index})'" : "")}><title>{H(path.Note)}</title></path>");
        }
        svg.Append("</g></svg>");
        return svg.ToString();
    }

    private static string Metrics(object summary)
    {
        var summaryElement = JsonSerializer.SerializeToElement(summary, Json);
        var rows = summaryElement.ValueKind == JsonValueKind.Object
            ? summaryElement.EnumerateObject().Where(row => row.Value.ValueKind is JsonValueKind.Number).ToArray()
            : [];
        if (rows.Length == 0) return "";
        var html = new StringBuilder("<dl class='metrics'>");
        foreach (var row in rows.Take(8)) html.Append($"<div><dt>{H(Words(row.Name))}</dt><dd>{H(row.Value.ToString())}</dd></div>");
        return html.Append("</dl>").ToString();
    }

    private static string Page(string title, string body) =>
        "<!doctype html><html lang='en'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>"
        + H(title) + "</title><style>" + Css + "</style></head><body>" + body + "</body></html>";
    private static string H(string? value) => WebUtility.HtmlEncode(value ?? "");
    private static string F(double value) => value.ToString("0.########", CultureInfo.InvariantCulture);
    private static string Token(string value) => new(value.Select(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());
    private static string Words(string value) => string.Concat(value.Select((character, index) => index > 0 && char.IsUpper(character) ? " " + char.ToLowerInvariant(character) : character.ToString()));
    private static void WriteText(string path, string content) => File.WriteAllText(path, content, new UTF8Encoding(false));

    private const string Header = """
        <header><p class='eyebrow'>Gojo · construction and interpretation</p><h1>A line, a band, a boundary</h1>
        <p class='intro'>Follow the same underlying paths through several reads: their centre lines, the boundaries on either side, occupied bands, and a contextual interpretation. Width, connection, crossing order and visibility answer different questions.</p>
        <nav><a href='gallery.html'>Case inspector</a><a href='overview.html'>All designs on one page</a><a href='gallery-data.json'>Display data</a><a href='summary.json'>Run summary</a></nav>
        <p class='key'><span class='key-line'></span>Blue, rust and teal distinguish participants or views. They do not stand for yes and no. Dashed paths retain a selected reference or an unresolved continuation.</p></header>
        """;

    private const string Inspector = """
        <main class='inspector'><section class='controls'><button id='previous' aria-label='Previous case'>←</button><label for='case'>Inspect a construction</label><select id='case'></select><button id='next' aria-label='Next case'>→</button><span id='position'></span></section><div id='content'></div></main>
        <script>const data=PAYLOAD;globalThis.BAND_EVIDENCE=data;
        const select=document.querySelector('#case'),content=document.querySelector('#content');
        const esc=value=>String(value??'').replace(/[&<>"']/g,character=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[character]));
        for(const row of data.examples){const option=document.createElement('option');option.value=row.id;option.textContent=row.id+' · '+row.name;select.append(option);}
        function render(id){const row=data.examples.find(row=>row.id===id)||data.examples[0];if(!row)return;select.value=row.id;const index=data.examples.indexOf(row);document.querySelector('#position').textContent=(index+1)+' / '+data.examples.length;document.querySelector('#previous').disabled=index===0;document.querySelector('#next').disabled=index===data.examples.length-1;
        content.innerHTML=`<article class='case' data-case='${esc(row.id)}'><div class='case-heading'><p class='eyebrow'>${esc(row.id)}</p><h2>${esc(row.name)}</h2><p>${esc(row.note)}</p></div><section class='comparison'>${row.panels.map(panel=>`<figure class='panel'><h3>${esc(panel.name)}</h3><a href='${esc(panel.image)}' title='Open standalone SVG'><img src='${esc(panel.image)}' alt='${esc(panel.name)}'></a><figcaption>${esc(panel.note)}</figcaption></figure>`).join('')}</section><section class='read-section'><h3>What the connected reads say</h3><p class='section-note'>These statements concern different aspects of the construction. An open reading keeps the missing premise visible.</p><div class='table-wrap'><table class='evidence'><thead><tr><th>Question or relation</th><th>Read</th><th>Basis</th><th>Qualification</th></tr></thead><tbody>${row.evidence.map(cue=>`<tr><th>${esc(cue.name)}</th><td><span class='read-label'>${esc(cue.outcome)}</span></td><td>${esc(cue.basis)}</td><td>${esc(cue.note)}</td></tr>`).join('')}</tbody></table></div></section><details><summary>Construction audit and native evidence</summary>${row.nativePacket?`<p><a href='${esc(row.nativePacket)}'>Download this construction’s native Gojo packet</a></p>`:''}<p class='section-note'>The numerical paths above are display projections. Native relations retain the construction, width participation, connection and visibility evidence.</p><pre>${esc(JSON.stringify(row.audit,null,2))}</pre></details></article>`;
        const url=new URL(location.href);url.searchParams.set('case',row.id);url.hash='';history.replaceState(null,'',url);}
        select.addEventListener('change',()=>render(select.value));document.querySelector('#previous').onclick=()=>render(data.examples[Math.max(0,select.selectedIndex-1)].id);document.querySelector('#next').onclick=()=>render(data.examples[Math.min(data.examples.length-1,select.selectedIndex+1)].id);render(new URL(location.href).searchParams.get('case'));
        </script>
        """;

    private const string Css = """
        :root{color-scheme:light;--ink:#24434c;--muted:#68777a;--line:#d7d9cf;--paper:#f3f1e7;--card:#fffdf6;--blue:#325c73;--rust:#ac644c}*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.55 system-ui,-apple-system,'Segoe UI',sans-serif}a{color:var(--blue);text-underline-offset:.2em}header{max-width:1540px;margin:0 auto;padding:38px 34px 19px}.eyebrow{font-size:.73rem;letter-spacing:.14em;text-transform:uppercase;font-weight:700;color:var(--rust);margin:0 0 8px}h1{font:normal clamp(2rem,4vw,3.2rem)/1.1 Georgia,serif;letter-spacing:-.03em;margin:0 0 18px}.intro{max-width:970px;font-size:1.03rem;margin:0 0 20px}nav{display:flex;flex-wrap:wrap;gap:10px 24px;font-size:.9rem}nav a{text-decoration:none;border-bottom:1px solid #adb9b5}.key{font-size:.82rem;max-width:1050px;color:var(--muted);margin:20px 0 0}.key-line{display:inline-block;width:25px;border-top:3px solid var(--blue);margin-right:6px;vertical-align:middle}.metrics{max-width:1472px;display:flex;flex-wrap:wrap;gap:18px 34px;padding:15px 0 22px;margin:0 auto 8px;border-bottom:1px solid var(--line)}.metrics div{display:flex;flex-direction:column-reverse}.metrics dt{font-size:.72rem;text-transform:uppercase;letter-spacing:.07em;color:var(--muted)}.metrics dd{font:1.65rem/1.3 Georgia,serif;margin:0}.inspector{max-width:1540px;margin:0 auto;padding:0 34px 60px}.controls{display:flex;align-items:center;gap:12px;position:sticky;top:0;z-index:2;background:var(--paper);padding:18px 0;border-bottom:1px solid var(--line)}.controls label{font-size:.85rem;color:var(--muted)}button,select{font:inherit;color:var(--ink);background:var(--card);border:1px solid #b9c2ba;border-radius:4px;min-height:40px}button{padding:0 13px;cursor:pointer}button:disabled{opacity:.35;cursor:default}select{padding:5px 34px 5px 10px;max-width:720px;min-width:0;flex:1}#position{font-size:.8rem;color:var(--muted);white-space:nowrap}.case-heading{padding:28px 0 20px;max-width:1060px}.case-heading h2{font:normal clamp(1.6rem,2.7vw,2.3rem)/1.2 Georgia,serif;margin:0 0 12px}.case-heading p:last-child{margin:0;color:#52656b}.comparison{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px}.panel{margin:0;min-width:0;background:var(--card);border:1px solid var(--line);border-radius:5px;overflow:hidden;display:flex;flex-direction:column}.panel h3{font-size:.85rem;font-weight:650;margin:0;padding:13px 15px;border-bottom:1px solid #e5e5dc;min-height:50px}.panel a{display:block;background:#fbf8ee}.panel img{width:100%;height:255px;object-fit:contain;display:block}.panel figcaption{padding:13px 15px 16px;font-size:.79rem;color:#5c6e73;flex:1}.read-section{margin-top:33px}.read-section>h3{font:normal 1.45rem Georgia,serif;margin:0 0 9px}.section-note{font-size:.83rem;color:var(--muted);max-width:950px;margin:0 0 14px}.table-wrap{overflow-x:auto;background:var(--card);border:1px solid var(--line);border-radius:4px}table{width:100%;border-collapse:collapse;font-size:.83rem;text-align:left}thead th{font-size:.72rem;text-transform:uppercase;letter-spacing:.06em;color:var(--muted);background:#eeefe6}th,td{padding:13px 15px;border-bottom:1px solid #e0e3d9;vertical-align:top}tbody tr:last-child>*{border-bottom:0}tbody th{font-weight:600;width:20%}td:nth-child(2){width:18%}.read-label{display:inline-block;color:#315a6f;background:#e8eef0;padding:2px 7px;border-radius:3px}details{margin:24px 0 0;border-top:1px solid var(--line);padding:15px 0}summary{cursor:pointer;font-size:.85rem;font-weight:600}details p{margin-top:16px}pre{background:#e8ebe4;max-height:520px;overflow:auto;font-size:.73rem;padding:20px;white-space:pre-wrap;word-break:break-word}.atlas{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:26px;max-width:1540px;margin:0 auto;padding:20px 34px 70px}.tile{background:var(--card);padding:18px;border:1px solid var(--line);border-radius:6px;min-width:0}.tile h2{font:normal 1.3rem/1.25 Georgia,serif;margin:0 0 17px}.tile h2 a{text-decoration:none}.tile-panels{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:13px}.tile figure{margin:0;min-width:0}.tile img{display:block;width:100%;height:205px;object-fit:contain;background:#fbf8ee}.tile figcaption{font-size:.74rem;color:var(--muted);padding:7px 0 0}.tile p{font-size:.81rem;color:#586c70;margin-bottom:9px}.text-link{font-size:.78rem}@media(min-width:1900px){.panel img{height:300px}}@media(max-width:1100px){.comparison{grid-template-columns:repeat(2,minmax(0,1fr))}.panel img{height:280px}.controls label{display:none}.metrics{margin-left:34px;margin-right:34px}.atlas{gap:18px}.tile img{height:175px}}@media(max-width:650px){header{padding:26px 18px 18px}.intro{font-size:.94rem}.metrics{margin:0 18px;padding:10px 0 19px;gap:14px 25px}.metrics dd{font-size:1.4rem}.inspector{padding:0 18px 35px}.controls{gap:7px;padding:12px 0}.controls button{padding:0 10px}.controls select{font-size:.82rem;padding-right:24px}#position{display:none}.comparison{gap:11px}.panel h3{font-size:.77rem;padding:10px;min-height:57px}.panel img{height:180px}.panel figcaption{font-size:.73rem;padding:10px}.case-heading{padding:24px 0 18px}.case-heading p:last-child{font-size:.9rem}.table-wrap{border-radius:0}table{min-width:700px}.atlas{grid-template-columns:1fr;padding:20px 18px 40px}.tile{padding:14px}.tile img{height:195px}}@media print{.controls,nav,details,.metrics{display:none}body{background:white}header{padding:10px}.atlas{display:block;padding:0}.tile{break-inside:avoid;margin:12px 0}.tile-panels{grid-template-columns:repeat(4,1fr)}.tile img{height:150px}.comparison{grid-template-columns:repeat(4,1fr)}}
        """;
}

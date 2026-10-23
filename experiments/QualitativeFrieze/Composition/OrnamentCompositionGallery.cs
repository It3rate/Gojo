using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Art = Gojo.Experiments.QualitativeFrieze.Composition.BandEvidenceGallery;

namespace Gojo.Experiments.QualitativeFrieze.Composition;

/// <summary>Presentation adapter for composed native ornament constructions.</summary>
public static class OrnamentCompositionGallery
{
    public sealed record Example(string Id, string Name, string Note,
        Art.Panel Frame, Art.Panel Prototype, Art.Panel Design, Art.Panel Alternate,
        IReadOnlyList<Art.Evidence> Evidence, object Audit, string? NativePacket = null);

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
            var sourcePanels = new[] { example.Frame, example.Prototype, example.Design, example.Alternate };
            var panels = sourcePanels.Select((panel, index) =>
            {
                var image = $"patterns/{Token(example.Id)}-{index + 1}.svg";
                WriteText(Path.Combine(output, image), Svg(panel, example.Id + " · " + panel.Name));
                return new { panel.Name, panel.Note, Image = image, Drawing = panel };
            }).ToArray();
            var overlay = $"patterns/{Token(example.Id)}-frame-overlay.svg";
            WriteText(Path.Combine(output, overlay), Svg(example.Frame with
                { Background = "transparent", Bounds = example.Design.Bounds }, example.Id + " · construction frame overlay"));
            rows.Add(new { example.Id, example.Name, example.Note, Panels = panels,
                FrameOverlay = overlay, example.Evidence, example.Audit, example.NativePacket });
            var href = "gallery.html?case=" + Uri.EscapeDataString(example.Id);
            atlas.Append($"<article class='tile'><h2><a href='{href}'>{H(example.Id)} · {H(example.Name)}</a></h2><div class='tile-designs'>");
            foreach (var panel in panels.Skip(2))
                atlas.Append($"<figure><a href='{panel.Image}'><img src='{panel.Image}' alt='{H(panel.Name)}' loading='lazy'></a><figcaption>{H(panel.Name)}</figcaption></figure>");
            atlas.Append($"</div><p>{H(example.Note)}</p><a class='text-link' href='{href}'>Inspect the frame, repeated segment and native construction →</a></article>");
        }
        atlas.Append("</main>");
        var data = JsonSerializer.Serialize(new { Schema = "ornament-composition-v1", summary, Examples = rows }, Json);
        WriteText(Path.Combine(output, "gallery-data.json"), data);
        WriteText(Path.Combine(output, "gallery.html"), Page("Gojo · composed ornaments",
            Header + Metrics(summary) + Inspector.Replace("PAYLOAD", data, StringComparison.Ordinal)));
        WriteText(Path.Combine(output, "overview.html"), Page("Gojo · ornament composition atlas", Header + Metrics(summary) + atlas));
    }

    private static string Svg(Art.Panel panel, string title)
    {
        var box = panel.Bounds ?? new Art.ViewBox(-12, -8, 24, 16);
        if (!double.IsFinite(box.X) || !double.IsFinite(box.Y) || !double.IsFinite(box.Width)
            || !double.IsFinite(box.Height) || box.Width <= 0 || box.Height <= 0)
            throw new ArgumentException("Ornament display needs a finite positive viewport.");
        var rect = $"x='{F(box.X)}' y='{F(box.Y)}' width='{F(box.Width)}' height='{F(box.Height)}'";
        var svg = new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='{F(box.X)} {F(box.Y)} {F(box.Width)} {F(box.Height)}' role='img'><title>{H(title)}</title><desc>{H(panel.Note)} A display projection of native path, band and repeat relations.</desc><rect {rect} fill='{H(panel.Background)}'/><defs><clipPath id='viewport'><rect {rect}/></clipPath>");
        for (var index = 0; index < panel.Paths.Count; index++)
        {
            if (panel.Paths[index].Cuts is not { Count: > 0 } cuts) continue;
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
            var ink = panel.Paths[index];
            if (!double.IsFinite(ink.StrokeWidth) || ink.StrokeWidth < 0
                || !double.IsFinite(ink.Opacity) || ink.Opacity < 0 || ink.Opacity > 1)
                throw new ArgumentException("Display width and opacity must be finite and legal.");
            svg.Append($"<path class='ink {H(Token(ink.Role))}' data-role='{H(ink.Role)}' d='{H(ink.Data)}' fill='{H(ink.Fill)}' fill-rule='{H(ink.FillRule)}' stroke='{H(ink.Stroke)}' stroke-width='{F(ink.StrokeWidth)}' stroke-linecap='{H(ink.Cap)}' stroke-linejoin='{H(ink.Join)}' opacity='{F(ink.Opacity)}'{(ink.Dash is null ? "" : $" stroke-dasharray='{H(ink.Dash)}'")}{(ink.Cuts is { Count: > 0 } ? $" mask='url(#visibility-{index})'" : "")}><title>{H(ink.Note)}</title></path>");
        }
        return svg.Append("</g></svg>").ToString();
    }

    private static string Metrics(object summary)
    {
        var element = JsonSerializer.SerializeToElement(summary, Json);
        if (element.ValueKind != JsonValueKind.Object) return "";
        var rows = element.EnumerateObject().Where(row => row.Value.ValueKind == JsonValueKind.Number).Take(7);
        var html = new StringBuilder("<dl class='metrics'>");
        foreach (var row in rows)
            html.Append($"<div><dt>{H(Words(row.Name))}</dt><dd>{H(row.Value.ToString())}</dd></div>");
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
        <header><p class='eyebrow'>Gojo · native ornament composition</p><h1>One segment, a larger rhythm</h1>
        <p class='intro'>Curved paths become bounded bands, leaves and petals. Larger segments repeat, reflect and turn through a shared construction frame. Two colour readings bring different levels of the same ornament forward.</p>
        <nav><a href='gallery.html'>Design inspector</a><a href='overview.html'>All ornaments on one page</a><a href='gallery-data.json'>Display data</a><a href='summary.json'>Run summary</a></nav>
        <p class='key'>Colour distinguishes the frame, ground, bands and accents. It expresses a selected design intention, rather than a yes/no verdict or a guaranteed perceptual effect.</p></header>
        """;

    private const string Inspector = """
        <main class='inspector'><section class='controls'><button id='previous' aria-label='Previous ornament'>←</button><label for='case'>Ornament</label><select id='case'></select><button id='next' aria-label='Next ornament'>→</button><span id='position'></span></section><div id='content'></div></main>
        <script>const data=PAYLOAD;globalThis.ORNAMENT_COMPOSITION=data;
        const select=document.querySelector('#case'),content=document.querySelector('#content');
        const esc=value=>String(value??'').replace(/[&<>"']/g,character=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[character]));
        for(const row of data.examples){const option=document.createElement('option');option.value=row.id;option.textContent=row.id+' · '+row.name;select.append(option);}
        function render(id){const row=data.examples.find(row=>row.id===id)||data.examples[0];if(!row)return;select.value=row.id;const index=data.examples.indexOf(row);document.querySelector('#position').textContent=(index+1)+' / '+data.examples.length;document.querySelector('#previous').disabled=index===0;document.querySelector('#next').disabled=index===data.examples.length-1;
        const design=row.panels[2],alternate=row.panels[3];
        content.innerHTML=`<article class='case' data-case='${esc(row.id)}'><div class='case-heading'><p class='eyebrow'>${esc(row.id)}</p><h2>${esc(row.name)}</h2><p>${esc(row.note)}</p></div><figure class='hero'><div class='hero-toolbar'><div role='group' aria-label='Colour reading'><button id='design-view' aria-pressed='true'>${esc(design.name)}</button><button id='alternate-view' aria-pressed='false'>${esc(alternate.name)}</button></div><label class='overlay-control'><input type='checkbox' id='frame-overlay'> Show construction frame</label><a id='hero-link' href='${esc(design.image)}'>Open SVG</a></div><div class='hero-art'><img id='hero-image' src='${esc(design.image)}' alt='${esc(design.name)}'><img id='frame-image' src='${esc(row.frameOverlay)}' alt='' hidden></div><figcaption id='hero-note'>${esc(design.note)}</figcaption></figure><section class='comparison' aria-label='Construction and colour readings'>${row.panels.map((panel,index)=>`<figure class='panel'><h3><span>${index+1}</span>${esc(panel.name)}</h3><a href='${esc(panel.image)}' title='Open standalone SVG'><img src='${esc(panel.image)}' alt='${esc(panel.name)}'></a><figcaption>${esc(panel.note)}</figcaption></figure>`).join('')}</section><section class='read-section'><h3>What is connected, repeated and left open</h3><div class='table-wrap'><table class='evidence'><thead><tr><th>Relation or intention</th><th>Read</th><th>Basis</th><th>Qualification</th></tr></thead><tbody>${row.evidence.map(cue=>`<tr><th>${esc(cue.name)}</th><td><span class='read-label'>${esc(cue.outcome)}</span></td><td>${esc(cue.basis)}</td><td>${esc(cue.note)}</td></tr>`).join('')}</tbody></table></div></section><details><summary>Construction audit and native packet</summary>${row.nativePacket?`<p><a href='${esc(row.nativePacket)}'>Download this construction’s native Gojo packet</a></p>`:''}<p class='key'>The SVG paths are presentation projections. Native relations retain the original units, compound segments, transforms, thickness and colour intention.</p><pre>${esc(JSON.stringify(row.audit,null,2))}</pre></details></article>`;
        function reading(alternateSelected){const panel=row.panels[alternateSelected?3:2];document.querySelector('#hero-image').src=panel.image;document.querySelector('#hero-image').alt=panel.name;document.querySelector('#hero-link').href=panel.image;document.querySelector('#hero-note').textContent=panel.note;document.querySelector('#design-view').setAttribute('aria-pressed',String(!alternateSelected));document.querySelector('#alternate-view').setAttribute('aria-pressed',String(alternateSelected));}
        document.querySelector('#design-view').onclick=()=>reading(false);document.querySelector('#alternate-view').onclick=()=>reading(true);document.querySelector('#frame-overlay').onchange=event=>document.querySelector('#frame-image').hidden=!event.target.checked;
        const url=new URL(location.href);url.searchParams.set('case',row.id);url.hash='';history.replaceState(null,'',url);}
        select.addEventListener('change',()=>render(select.value));document.querySelector('#previous').onclick=()=>render(data.examples[Math.max(0,select.selectedIndex-1)].id);document.querySelector('#next').onclick=()=>render(data.examples[Math.min(data.examples.length-1,select.selectedIndex+1)].id);render(new URL(location.href).searchParams.get('case'));
        </script>
        """;

    private const string Css = """
        :root{color-scheme:light;--ink:#293f45;--muted:#697579;--line:#d4d8cc;--paper:#f3f1e9;--card:#fffdf7;--blue:#385e70;--rust:#a0644b}*{box-sizing:border-box}body{margin:0;background:var(--paper);color:var(--ink);font:16px/1.55 system-ui,-apple-system,'Segoe UI',sans-serif}a{color:var(--blue);text-underline-offset:.2em}header{max-width:1560px;margin:auto;padding:35px 36px 18px}.eyebrow{font-size:.72rem;letter-spacing:.15em;text-transform:uppercase;font-weight:700;color:var(--rust);margin:0 0 9px}h1{font:normal clamp(2rem,4vw,3.3rem)/1.1 Georgia,serif;letter-spacing:-.03em;margin:0 0 16px}.intro{max-width:970px;font-size:1.03rem;margin:0 0 18px}nav{display:flex;flex-wrap:wrap;gap:10px 24px;font-size:.88rem}nav a{text-decoration:none;border-bottom:1px solid #aeb9b4}.key{font-size:.81rem;max-width:1050px;color:var(--muted);margin:18px 0 0}.metrics{max-width:1488px;display:flex;flex-wrap:wrap;gap:18px 34px;padding:12px 0 20px;margin:0 auto 6px;border-bottom:1px solid var(--line)}.metrics div{display:flex;flex-direction:column-reverse}.metrics dt{font-size:.7rem;text-transform:uppercase;letter-spacing:.07em;color:var(--muted)}.metrics dd{font:1.6rem/1.3 Georgia,serif;margin:0}.inspector{max-width:1560px;margin:auto;padding:0 36px 65px}.controls{display:flex;align-items:center;gap:12px;position:sticky;top:0;z-index:3;background:var(--paper);padding:16px 0;border-bottom:1px solid var(--line)}.controls label{font-size:.83rem;color:var(--muted)}button,select{font:inherit;color:var(--ink);background:var(--card);border:1px solid #b8c2bb;border-radius:4px;min-height:38px}button{padding:5px 12px;cursor:pointer}button:disabled{opacity:.35;cursor:default}button[aria-pressed=true]{background:#315563;color:white;border-color:#315563}select{padding:5px 30px 5px 10px;max-width:900px;min-width:0;flex:1}#position{font-size:.8rem;color:var(--muted);white-space:nowrap}.case-heading{padding:26px 0 21px;max-width:1100px}.case-heading h2{font:normal clamp(1.6rem,2.7vw,2.3rem)/1.2 Georgia,serif;margin:0 0 11px}.case-heading p:last-child{margin:0;color:#52656b}.hero{margin:0 0 24px;border:1px solid var(--line);border-radius:6px;background:var(--card);overflow:hidden}.hero-toolbar{display:flex;flex-wrap:wrap;align-items:center;gap:12px 25px;padding:12px 16px;border-bottom:1px solid var(--line);font-size:.8rem}.hero-toolbar [role=group]{display:flex;gap:8px}.hero-toolbar button{font-size:.8rem}.hero-toolbar>a{margin-left:auto}.overlay-control{display:flex;align-items:center;gap:7px;color:#586d71}.hero-art{position:relative;background:#f9f6ec}.hero-art>img{display:block;width:100%;height:clamp(350px,45vw,670px);object-fit:contain}.hero-art>#frame-image{position:absolute;inset:0;pointer-events:none;opacity:.8}.hero-art>#frame-image[hidden]{display:none}.hero>figcaption{padding:12px 17px 15px;font-size:.82rem;color:#586d71}.comparison{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px}.panel{margin:0;min-width:0;background:var(--card);border:1px solid var(--line);border-radius:5px;overflow:hidden;display:flex;flex-direction:column}.panel h3{font-size:.83rem;font-weight:650;margin:0;padding:12px;border-bottom:1px solid #e3e4d9;min-height:52px;display:flex;gap:9px;align-items:baseline}.panel h3 span{color:#9b6e53;font:normal 1.15rem Georgia,serif}.panel a{display:block}.panel img{width:100%;height:250px;object-fit:contain;display:block}.panel figcaption{padding:12px 14px 15px;font-size:.77rem;color:#5d7075;flex:1}.read-section{margin-top:32px}.read-section>h3{font:normal 1.4rem Georgia,serif;margin:0 0 16px}.table-wrap{overflow-x:auto;background:var(--card);border:1px solid var(--line);border-radius:4px}table{width:100%;border-collapse:collapse;font-size:.82rem;text-align:left}thead th{font-size:.7rem;text-transform:uppercase;letter-spacing:.06em;color:var(--muted);background:#eeefe6}th,td{padding:13px 15px;border-bottom:1px solid #e0e3d9;vertical-align:top}tbody tr:last-child>*{border-bottom:0}tbody th{font-weight:600;width:20%}td:nth-child(2){width:18%}.read-label{display:inline-block;color:#315a6f;background:#e8eef0;padding:2px 7px;border-radius:3px}details{margin:24px 0 0;border-top:1px solid var(--line);padding:15px 0}summary{cursor:pointer;font-size:.85rem;font-weight:600}pre{background:#e8ebe4;max-height:520px;overflow:auto;font-size:.73rem;padding:20px;white-space:pre-wrap;word-break:break-word}.atlas{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:25px;max-width:1560px;margin:auto;padding:20px 36px 70px}.tile{background:var(--card);padding:17px;border:1px solid var(--line);border-radius:6px;min-width:0}.tile h2{font:normal 1.27rem/1.25 Georgia,serif;margin:0 0 15px}.tile h2 a{text-decoration:none}.tile-designs{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}.tile figure{margin:0;min-width:0}.tile img{display:block;width:100%;height:285px;object-fit:contain}.tile figcaption{font-size:.74rem;color:var(--muted);padding:7px 0 0}.tile p{font-size:.8rem;color:#586c70;margin-bottom:8px}.text-link{font-size:.77rem}@media(max-width:1150px){.comparison{grid-template-columns:repeat(2,minmax(0,1fr))}.panel img{height:280px}.metrics{margin-left:36px;margin-right:36px}.atlas{gap:18px}.tile img{height:220px}.hero-art>img{height:550px}}@media(max-width:700px){header{padding:25px 18px 18px}.intro{font-size:.93rem}.metrics{margin:0 18px;gap:14px 25px}.metrics dd{font-size:1.35rem}.inspector{padding:0 18px 35px}.controls{gap:7px;padding:12px 0}.controls>label,#position{display:none}.controls select{font-size:.8rem;padding-right:20px}.comparison{gap:11px}.panel h3{font-size:.76rem;padding:10px;min-height:58px}.panel img{height:190px}.panel figcaption{font-size:.72rem;padding:10px}.hero-art>img{height:360px}.hero-toolbar{padding:10px;gap:10px}.hero-toolbar [role=group]{flex-wrap:wrap}.hero-toolbar button{font-size:.73rem;min-height:34px}.hero-toolbar>a{margin-left:0}.hero>figcaption{padding:11px;font-size:.76rem}.case-heading p:last-child{font-size:.9rem}table{min-width:680px}.atlas{grid-template-columns:1fr;padding:20px 18px 40px}.tile{padding:14px}.tile img{height:235px}}@media print{.controls,nav,details,.metrics,.hero-toolbar,.comparison,.read-section{display:none}body{background:white}header{padding:10px}.atlas{display:block;padding:0}.tile{break-inside:avoid;margin:12px 0}.tile img{height:260px}.hero-art>img{height:700px}}
        """;
}

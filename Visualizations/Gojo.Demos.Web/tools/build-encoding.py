"""Generate browser notes from Markdown; install markdown==3.8.2 first."""
from pathlib import Path
import re, html, shutil, tempfile
import markdown
from urllib.parse import unquote
root=Path(__file__).resolve().parents[3]
destination=root/'Visualizations/Gojo.Demos.Web/encoding'
# Rewrite linked examples in a private build directory before updating files
# exposed to browsers and file watchers. Unchanged assets keep their handles.
stage=tempfile.TemporaryDirectory(prefix='gojo-encoding-notes-')
web=Path(stage.name)
notes=web/'notes'; notes.mkdir(parents=True,exist_ok=True)
source=root/'experiments/PrimitiveMeasure'
files=list(source.glob('*.md'))
# The explorer guide links back into this same document set.
files.append(root/'Visualizations/Gojo.PrimitiveExplorer/README.md')
for file in files:
    for url in re.findall(r'\]\(([^)]+)\)',file.read_text(encoding='utf-8')):
        if url.startswith(('http:','https:','#')): continue
        linked=(file.parent/unquote(url.split('#')[0])).resolve()
        if linked.is_relative_to(root) and linked.suffix=='.md' and linked.is_file() and linked not in [f.resolve() for f in files]: files.append(linked)
names={p.resolve(): ('ExplorerGuide.html' if p.parent.name=='Gojo.PrimitiveExplorer' else p.stem+'.html' if p.parent==source else str(p.relative_to(root)).replace('\\','--').replace('/','--')[:-3]+'.html') for p in files}

style='body{max-width:960px;margin:40px auto;padding:0 24px;background:#f7f5ee;color:#213b3a;font:17px/1.65 system-ui}a{color:#167c6c}pre{overflow:auto;padding:20px;background:#152d2d;color:#e2eee8}table{border-collapse:collapse;display:block;overflow:auto}td,th{padding:9px;border:1px solid #ccd8d0}img{max-width:100%}code{font-size:.9em}nav{display:flex;gap:24px;flex-wrap:wrap}h1,h2,h3{line-height:1.25}'
for file in files:
    text=file.read_text(encoding='utf-8')
    def link(m):
        label,url=m.group(1),m.group(2)
        if url.startswith(('http:','https:','#')): return m.group(0)
        target,_,anchor=url.partition('#'); resolved=(file.parent/unquote(target)).resolve()
        if not resolved.is_relative_to(root): return label+' (external repository reference)'
        if resolved in names: url=names[resolved]+('#'+anchor if anchor else '')
        elif resolved.is_file():
            if resolved.suffix=='.md': raise ValueError('Unconverted Markdown link: '+str(resolved))
            asset=resolved.relative_to(root).as_posix().replace('/','--'); dest=notes/'assets'/asset;dest.parent.mkdir(exist_ok=True);shutil.copyfile(resolved,dest);url='assets/'+asset
        elif resolved.is_dir(): url='https://github.com/It3rate/Gojo/tree/4229d552/'+resolved.relative_to(root).as_posix()
        else: return label+' (source link unavailable)'
        return '['+label+']('+url+')'
    text=re.sub(r'\[([^\]]+)\]\(([^)]+)\)',link,text)
    body=markdown.markdown(text,extensions=['tables','fenced_code','toc','sane_lists'])
    title=html.escape(next((l.lstrip('# ') for l in text.splitlines() if l.startswith('# ')),file.stem))
    (notes/names[file.resolve()]).write_text('<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+title+'</title><style>'+style+'</style><nav><a href="/encoding/">Encoding</a><a href="/encoding/explorer/">Interactive explorer</a><a href="/">Gojo home</a></nav><main>'+body+'</main></html>',encoding='utf-8')
links=''.join('<li><a href="notes/'+names[f.resolve()]+'">'+html.escape(f.stem)+'</a></li>' for f in files)
(web/'index.html').write_text('<!doctype html><html lang="en"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Gojo encoding</title><style>'+style+'</style><nav><a href="/">Gojo home</a></nav><h1>Gojo encoding</h1><p>From three bits of participation to connected flow, shapes and repeated patterns.</p><h2><a href="explorer/">Open the interactive Primitive Explorer →</a></h2><p>Inspect 46 layers. Hover diagrams and bits to trace their correspondence; change observations and advance flow. Best on a desktop-sized screen.</p><p><a href="explorer/?layer=25">Straight capital atlas →</a> · <a href="explorer/?layer=26">Curves in capitals →</a> · <a href="explorer/?layer=27">Across a glyph gap →</a> · <a href="explorer/?layer=28">Push a bowl outward →</a> · <a href="explorer/?layer=29">Glyph clues &amp; junctions →</a> · <a href="explorer/?layer=30">One stem, two inputs →</a> · <a href="explorer/?layer=31">Two reaches, one choice →</a> · <a href="explorer/?layer=32">When two rails meet →</a> · <a href="explorer/?layer=33">Read a repeating border →</a> · <a href="explorer/?layer=34">Did the washer drop? →</a> · <a href="explorer/?layer=35">Which test should run next? →</a> · <a href="explorer/?layer=36">Can a test earn a socket? →</a> · <a href="explorer/?layer=37">Ornament workshop →</a> · <a href="explorer/?layer=38">Draw from hints →</a> · <a href="explorer/?layer=39">Whitespace becomes a path →</a> · <a href="explorer/?layer=40">One template, many instances →</a> · <a href="explorer/?layer=41">Construction clues →</a> · <a href="explorer/?layer=42">Let the structure respond →</a> · <a href="explorer/?layer=43">What survives a change? →</a> · <a href="explorer/?layer=44">Small concepts under change →</a> · <a href="explorer/?layer=45">Who crossed first? →</a> · <a href="explorer/?layer=46">Which action helps? →</a></p><h2>Encoding notes</h2><ul>'+links+'</ul></html>',encoding='utf-8')

# HTML examples have their own relative links. Preserve their source-relative
# meaning rather than leaving them relative to the flattened asset directory.
asset_sources={f.relative_to(root).as_posix().replace('/','--'):f for f in root.glob('Gojo/docs/examples/*.html')}
for asset in (notes/'assets').glob('*.html'):
    original=asset_sources.get(asset.name)
    if original is None: continue
    def html_link(m):
        prefix,url,quote=m.groups()
        if url.startswith(('http:','https:','#','data:','mailto:')): return m.group(0)
        from urllib.parse import urlsplit
        parts=urlsplit(url); dest=(original.parent/unquote(parts.path)).resolve()
        suffix=('?'+parts.query if parts.query else '')+('#'+parts.fragment if parts.fragment else '')
        if dest in names: result='../'+names[dest]+suffix
        elif dest.is_relative_to(root) and dest.is_file():
            key=dest.relative_to(root).as_posix().replace('/','--')
            if dest.suffix=='.md': result='https://github.com/It3rate/Gojo/blob/4229d552/'+dest.relative_to(root).as_posix()+suffix
            else:
                target=notes/'assets'/key
                if not target.exists(): shutil.copyfile(dest,target)
                result=key+suffix
        else: result='https://github.com/It3rate/Gojo/tree/4229d552/'
        return prefix+html.escape(result,quote=True)+quote
    asset.write_text(re.sub(r'((?:href|src)=["\'])([^"\']+)(["\'])',html_link,asset.read_text(encoding='utf-8')),encoding='utf-8')

for candidate in web.rglob('*'):
    if candidate.is_file():
        target=destination/candidate.relative_to(web)
        target.parent.mkdir(parents=True,exist_ok=True)
        if not target.exists() or target.read_bytes()!=candidate.read_bytes():
            shutil.copyfile(candidate,target)
for old in (destination/'notes').rglob('*'):
    if old.is_file() and not (web/old.relative_to(destination)).exists():
        assert old.resolve().is_relative_to((destination/'notes').resolve())
        old.unlink()
stage.cleanup()
print('Generated',len(files),'HTML documents and encoding landing page.')

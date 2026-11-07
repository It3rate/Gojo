"""Render the repository's study notes beside its recorded local HTML viewer.

Requires Python Markdown (`python -m pip install markdown`). The experiment
runner produces the data and viewer; this adapter only publishes documentation.
"""
from pathlib import Path
import html
import re
import sys
from urllib.parse import quote

import markdown

ROOT = Path(__file__).resolve().parents[2]
SOURCE = Path(__file__).resolve().parent
OUTPUT = Path(sys.argv[1]) if len(sys.argv) > 1 else SOURCE / "results/machine-settlement"
OUTPUT.mkdir(parents=True, exist_ok=True)
PAGES = {
    "MachineSettlementStudy.md": "notes.html",
    "MachineGlossary.md": "glossary.html",
    "MachineSettlementProtocol.md": "protocol.html",
}
STYLE = """
body{margin:0;background:#f5f4ef;color:#243142;font:17px/1.65 system-ui,sans-serif}
main{max-width:980px;margin:32px auto;padding:34px;background:white;border:1px solid #dce1e4;border-radius:12px}
nav{display:flex;gap:22px;flex-wrap:wrap;font-size:14px;margin-bottom:32px}a{color:#176baa;text-underline-offset:3px}
h1{font-size:36px;line-height:1.2}h2{margin-top:38px;font-size:25px}h3{margin-top:25px}
table{border-collapse:collapse;display:block;overflow:auto;font-size:14px}td,th{padding:9px 12px;border:1px solid #dce1e4;text-align:left;min-width:110px}th{background:#edf2f6}
pre{background:#edf2f6;padding:18px;border-radius:6px;overflow:auto;font-size:14px}code{font-size:.88em}
blockquote{border-left:3px solid #287158;padding-left:20px;color:#455467}img{max-width:100%}
footer{margin-top:30px;font-size:13px;color:#596777}@media(max-width:650px){main{margin:0;padding:22px;border:0;border-radius:0}h1{font-size:29px}}
"""


def rewrite_link(match):
    target = html.unescape(match.group(1))
    if ":" in target or target.startswith("#"):
        return match.group(0)
    path, _, fragment = target.partition("#")
    resolved = (SOURCE / path).resolve()
    if resolved.name in PAGES and resolved.parent == SOURCE:
        href = PAGES[resolved.name]
    elif resolved.parent == OUTPUT.resolve():
        href = resolved.name
    else:
        try:
            relative = resolved.relative_to(ROOT).as_posix()
        except ValueError:
            return match.group(0)
        href = "https://github.com/It3rate/Gojo/blob/master/" + quote(relative, safe="/")
    if fragment:
        href += "#" + fragment
    return 'href="' + html.escape(href, quote=True) + '"'


for filename, destination in PAGES.items():
    content = (SOURCE / filename).read_text(encoding="utf-8")
    title = content.splitlines()[0].lstrip("# ")
    body = markdown.markdown(content, extensions=["tables", "fenced_code", "toc"])
    body = re.sub(r'href="([^"]+)"', rewrite_link, body)
    page = f"""<!doctype html><html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>{html.escape(title)} · Gojo</title><style>{STYLE}</style></head><body><main>
<nav><a href="index.html">Recorded experiment</a><a href="notes.html">Study notes</a>
<a href="glossary.html">Machine glossary</a><a href="protocol.html">Prospective protocol</a></nav>
{body}<footer>Generated from the repository Markdown. Source links open their readable repository pages.</footer>
</main></body></html>"""
    (OUTPUT / destination).write_text(page, encoding="utf-8")
    print(destination)

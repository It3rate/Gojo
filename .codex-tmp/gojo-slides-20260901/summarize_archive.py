import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent
manifest = json.loads((ROOT / "archive-manifest.json").read_text(encoding="utf-8"))
items = manifest["files"] if isinstance(manifest, dict) else manifest
out_dir = ROOT / "source-texts"
out_dir.mkdir(exist_ok=True)

index = []
for item in items:
    rel = item["path"]
    content = item.get("content", {})
    kind = content.get("kind", item.get("kind", "")) if isinstance(content, dict) else item.get("kind", "")
    lines = []
    if isinstance(content, dict) and kind == "pptx":
        entries = []
        for slide in content.get("slides", []):
            entries.append({"kind": "slide", "number": slide.get("number", slide.get("slide", "")), "text": slide.get("text", [])})
        for note in content.get("notes", []):
            entries.append({"kind": "notes", "number": note.get("number", note.get("slide", "")), "text": note.get("text", [])})
    elif isinstance(content, dict) and kind == "xlsx":
        entries = [{"kind": "sheet", **sheet} for sheet in content.get("sheets", [])]
    elif isinstance(content, dict) and kind in {"odt", "text", "ipynb"}:
        raw = content.get("text", content.get("cells", []))
        entries = raw if isinstance(raw, list) else [raw]
    else:
        entries = content.get("blocks", content.get("items", content.get("content", []))) if isinstance(content, dict) else content
    if isinstance(entries, str):
        entries = [entries]
    for entry in entries or []:
        if isinstance(entry, str):
            lines.append(entry)
            continue
        if not isinstance(entry, dict):
            continue
        if "text" in entry and entry["text"] and not isinstance(entry["text"], list):
            style = entry.get("style", "")
            prefix = f"[{style}] " if style else ""
            lines.append(prefix + entry["text"])
        elif entry.get("type") == "table":
            for row in entry.get("rows", []):
                lines.append(" | ".join(str(cell) for cell in row))
        elif entry.get("type") == "sheet" or entry.get("kind") == "sheet":
            lines.append(f"[Sheet] {entry.get('title', '')}")
            for row in entry.get("rows", []):
                lines.append(" | ".join("" if cell is None else str(cell) for cell in row))
        elif entry.get("type") in {"slide", "notes"} or entry.get("kind") in {"slide", "notes"}:
            entry_type = entry.get("type", entry.get("kind"))
            title = f"[{entry_type.title()} {entry.get('number', '')}]"
            lines.append(title)
            slide_text = entry.get("text", [])
            lines.extend(slide_text if isinstance(slide_text, list) else [str(slide_text)])
        elif "source" in entry:
            lines.append(str(entry["source"]))

    text = "\n".join(line.strip() for line in lines if str(line).strip())
    safe = re.sub(r"[^A-Za-z0-9._-]+", "_", rel)[:180] + ".txt"
    (out_dir / safe).write_text(text, encoding="utf-8")
    headings = [
        line for line in text.splitlines()
        if line.startswith("[Heading") or line.startswith("[Title") or line.startswith("[Slide")
    ]
    index.append({
        "path": rel,
        "kind": kind,
        "chars": len(text),
        "headings": headings[:120],
        "preview": text[:1200],
    })

(ROOT / "source-text-index.json").write_text(json.dumps(index, indent=2), encoding="utf-8")
print(json.dumps({"files": len(index), "text_dir": str(out_dir)}, indent=2))

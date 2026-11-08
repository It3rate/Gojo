"""Render the partial-decision notes beside the local replay."""
from pathlib import Path
import html
import os
import sys
import RenderMachineCoordinationNotes as renderer

renderer.OUTPUT = Path(sys.argv[1]) if len(sys.argv) > 1 else renderer.SOURCE / "results/partial-decisions"
renderer.PAGES = {
    "PartialDecisionStudy.md": "notes.html",
    "PartialDecisionGlossary.md": "glossary.html",
    "PartialDecisionProtocol.md": "protocol.html",
}
previous_rewrite = renderer.rewrite_link


def rewrite_link(match):
    target = html.unescape(match.group(1))
    if ":" in target or target.startswith("#"):
        return match.group(0)
    path, _, fragment = target.partition("#")
    prior = {"AdaptiveClaimStudy.md": "notes.html", "AdaptiveClaimGlossary.md": "glossary.html", "AdaptiveClaimProtocol.md": "protocol.html"}
    resolved = (renderer.SOURCE / path).resolve()
    if resolved.parent == renderer.SOURCE and resolved.name in prior:
        href = Path(os.path.relpath(renderer.SOURCE / "results/adaptive-claims" / prior[resolved.name], renderer.OUTPUT)).as_posix()
        return 'href="' + html.escape(href + ("#" + fragment if fragment else ""), quote=True) + '"'
    return previous_rewrite(match)


if __name__ == "__main__":
    renderer.rewrite_link = rewrite_link
    renderer.main()

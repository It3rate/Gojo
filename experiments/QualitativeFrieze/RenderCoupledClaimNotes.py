"""Render the coupled-claim Markdown using the established local report style."""
from pathlib import Path
import html
import os
import sys

import RenderMachineCoordinationNotes as renderer

renderer.OUTPUT = Path(sys.argv[1]) if len(sys.argv) > 1 else renderer.SOURCE / "results/coupled-claims"
renderer.PAGES = {
    "CoupledClaimStudy.md": "notes.html",
    "CoupledClaimGlossary.md": "glossary.html",
    "CoupledClaimProtocol.md": "protocol.html",
}
previous_rewrite = renderer.rewrite_link


def rewrite_link(match):
    target = html.unescape(match.group(1))
    path, _, fragment = target.partition("#")
    prior = {
        "MachineCoordinationStudy.md": "notes.html",
        "MachineCoordinationGlossary.md": "glossary.html",
        "MachineCoordinationProtocol.md": "protocol.html",
    }
    resolved = (renderer.SOURCE / path).resolve()
    if resolved.parent == renderer.SOURCE and resolved.name in prior:
        href = Path(os.path.relpath(renderer.SOURCE / "results/machine-coordination" / prior[resolved.name], renderer.OUTPUT)).as_posix()
        if fragment:
            href += "#" + fragment
        return 'href="' + html.escape(href, quote=True) + '"'
    return previous_rewrite(match)


if __name__ == "__main__":
    renderer.rewrite_link = rewrite_link
    renderer.main()

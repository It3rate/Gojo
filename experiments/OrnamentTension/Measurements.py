"""Generate the compact reader/utility tables from actual held-out records."""
import csv
import json
import sys
from pathlib import Path


def build(root):
    data=json.loads((root/"analysis.json").read_bytes())
    panels=list(csv.DictReader((root/"panel-use.csv").open(encoding="utf-8")))
    judges=list(data["participation"]); slots=["2","5","9"]
    lines=["# Which readers had influence?","",
           "Each panel spends sixteen shares and activates two, three or four readers. The pilot data nominate a panel separately for each family, width and search replicate. The broad judge used for later comparison is fixed. These counts describe selection under those readings. They are not human preference frequencies.","",
           "| Reader | Panels including it (of 96) | Shares allocated (of 1,536) |","| --- | ---: | ---: |"]
    lines+=[f"| {j} | {data['participation'][j]} | {data['shares'][j]} |" for j in judges]
    lines+=["","Twenty-one panels had two participating readers, 26 had three, and 49 had four. The most frequent subset was frieze and reference (seven nominations). Material, motif, frieze and reference appeared together six times. Subsets share budgets and evidence, so these frequencies do not identify which single reader caused a winner to succeed. [Every chosen panel and its shares](../../artifacts/local-tension-ornament/wave1/panel-use.csv).","",
            "## What changed on new bases?","",
            "Deltas are coupled minus the comparison on the fixed broad score, except for the intended-versus-wrong reference comparison. Each row contains 24 registered cells (eight families × three widths). The changed count asks whether the final deposited ink differed on the 2,304 matched runs. Repeating this count on a trained competitor compares different programs. Applying a cut or a fixed-route intervention compares the same program.","",
            "| Comparison | Mean delta | Changed material | Supported | Inconclusive | Contrary |","| --- | ---: | ---: | ---: | ---: | ---: |"]
    for k,d in data["contrast"].items():
        v=d["verdicts"]; changed="—" if k=="wrong-reference" else str(d["changed"])
        lines.append(f"| {k} | {d['delta']:+.4f} | {changed} | {v.get('supported',0)} | {v.get('inconclusive',0)} | {v.get('contrary',0)} |")
    lines+=["","Static fixes route strengths while keeping actuation. Disconnect disables pressure-driven actuation while retaining sensing. Wrong-route displaces the received lane; cut-up and cut-down instead block passages in one direction. See the [complete contrasts](../../artifacts/local-tension-ornament/wave1/contrasts.csv).","",
            "## What did extra vertical capacity buy?","",
            "The same coupled finalists run across each width. A slot is one B extent on either side of the central line. Vertical travel is total absolute vertical displacement divided by the band width, including downward returns. Range is the occupied vertical extent in the experiment's realized units. Each column includes 768 drawings.","",
            "| Read | 2 slots | 5 slots | 9 slots |","| --- | ---: | ---: | ---: |"]
    for key in ["broad","range","travel","slot-reach","turn-variety","tips","forks","merges","crossings","overlaps","whole-course-span","shared-seam",
                "motif-mirror","alternating-mirror","cell-variation","shape-novelty","actions","route_changes","relief","misses"]:
        lines.append("| "+key+" | "+" | ".join(f"{data['coupled'][s][key]:.3f}" for s in slots)+" |")
    lines+=["","Wider bands supported more vertical travel and a greater absolute range; the fraction of the available registers occupied declined. The motif and neighboring-mirror reads remained approximately flat or declined. [All family, mode and width means](../../artifacts/local-tension-ornament/wave1/means.csv).","",
            "## Joint words","",
            "We retain the pair of successive turn directions and the incoming/outgoing rays at an actual fork. We compare both across distinct winners on matched inputs and within the same winner with actuation cut. The first is an association. The second changes a real connection but can also change other parts of the drawing. In crossing ribbons, the word *straight, then counterclockwise eighth-turn* separated 52 informative live/quiet pairs across nine independent bases. The member containing it averaged +0.057 broad utility; all nine basis means favored it. No negative or fork word met that audit's coverage rule. [Paired joint audit](../../artifacts/local-tension-ornament/wave1/JointAssociations.md) · [All matched inputs across winners](../../artifacts/local-tension-ornament/wave1/joint-combinations.csv)."]
    return "\n".join(lines)+"\n"


if __name__=="__main__":
    Path(sys.argv[2]).write_text(build(Path(sys.argv[1])),encoding="utf-8")

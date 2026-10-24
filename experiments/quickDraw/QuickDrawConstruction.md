# Interpreting acquired church drawings through construction steps

This exploratory checkpoint maps the five previously curated Quick Draw
churches into executable Gojo construction programs. It compares a close fit,
gentle directional expectations and stronger interpretation. Each proposal
begins with actual rough construction controls and places subsequent details
relative to those controls and earlier acquired strokes. The purpose is to
inspect the boundary between fitting a drawing and interpreting its intention.

Open `D:\_quickDraw\gojo-quickdraw-structure\review.html` or the
[portable review](results/quickdraw-construction/review/index.html).
The earlier [church and alphabet resolution study](ConstructionResolution.md)
is now published on the website. This new acquired-drawing study remains local
and in GitHub. No model was trained for this checkpoint.

## What the review compares

The fixture contains five drawings, 47 acquired strokes and 1,295 sample events.
They were selected visually before this fitting experiment: a simple facade,
an oblique building, a clock with a side wing, three rounded towers, and a
stepped tower. They are useful contrasts, not a representative population test.

Each drawing has three native programs and the same sequence of construction
rungs. There are 15 programs and 171 rungs in total.

| Read | Relative displacement grade | Directional expectation |
| --- | --- | --- |
| Close fit | 1 page unit | No axis attraction |
| Gentle interpretation | 4 page units | Attract line chords within 12 degrees of horizontal or vertical |
| Stronger interpretation | 12 page units | Attract line chords within 24 degrees of horizontal or vertical |

Measurements use a 512-unit page with aspect-preserving normalization and a
32-unit margin on the longer dimension. The grades affect displacement from
an earlier site. They are not image pixelation, confidence levels or learned
probabilities. A distinguishable span that would collapse at the requested
grade retains its finer displacement, with that exception recorded.

These alternatives deliberately do not guarantee geometric symmetry,
containment or contact. Unlike the earlier authored resolution study, those
relationships have not yet been established for the raw drawings. Source
stroke count, interval coverage, acquired order and distinct pen-lift identities
are preserved. Candidate contacts remain candidate reads.

## Rough shape first and actual earlier references

The first rung runs the existing small rectangle engine over a coarse measured
envelope. It reserves the drawing's extent; it does not assert a building wall.
The second rung simplifies long acquired strokes into rough connected chords.
Those strokes are selected by their extent, so the outline is a positioning
proposal rather than a proven outer boundary. Both rungs are authored offline
from the whole drawing, not recovered records of the person's mental template.

Each later rung introduces one acquired stroke in the recorded order. Its
first site is displaced from a nearby control of an already drawn construction.
Subsequent endpoints consume their actual predecessor site; curve handles also
consume actual earlier sites. The envelope supplies the horizontal and vertical
measurement spans. Deformation therefore propagates through real dependencies.
Changing the envelope changes subsequent geometry even after source mappings
and explanatory metadata are removed.

When a detailed stroke arrives, its rough chords retire into the guide layer.
Other rough chords remain until their corresponding strokes arrive. Retired
guides retain identity and can fade visually without breaking references.
No instruction consumes a future control. Whole-drawing scaffold authorship and
original human stroke order are recorded as different histories.

The fitter selects line or quadratic intervals with measured residuals. It
retains acquired sample indexes, including repeated XY values and significant
collinear retracing. A boundary shared by consecutive intervals of the same
stroke uses one actual target site. A separate pen lift gets a fresh site even
when its coordinates coincide. Closed-looking endpoints also remain distinct
unless they are the same acquired event.

## Geometric candidates and disagreement

The observation adapter proposes corners, near-right-angle bends, upper small
crossings, approximate ovals, close endpoints across pen lifts, and chains where
two near-right turns do not establish parallel outer legs. Each proposal keeps
actual source interval or sample references, measured quantities, thresholds
and explanatory qualifications. Category names do not generate missing parts.

The five fixtures produce cross-candidate counts of **1, 1, 0, 3 and 1**. The
clock drawing has one approximate oval and no cross candidate. Four chains
expose incompatible local axis readings. These are evidence-backed review
annotations, not a general church recognizer or native semantic classifier.

Selecting a candidate highlights its source participants and corresponding
native edges. Selecting an edge exposes its acquired interval, dependencies,
fit evidence and interpretation decision. The original trace, faded guides,
stroke numbering and three simultaneous proposals make changes inspectable.
The proposal panel displays restored native output; the browser does not fit,
snap, solve or regenerate the drawing from source samples.

## Native storage and the acquisition boundary

The retained exact source file is authoritative for acquisition. The adapter's
floating measurements are finite observations, not a replacement for the
exact rational native acquisition. Packets include the original input line so
native import does not inherit a Python float reserialization of the source.

There are three complementary artifacts:

1. The construction program contains numeric frame references, actual earlier
   site references, ordered finite curves, rung visibility and guide history.
   It restores and replays without original samples or candidate labels.
2. The correspondence evidence archive connects an exact `NativeAcquisition`
   to that actual program. Each `ReadWitness` selects the actual ordered source
   sample rows and the actual target edge row. Calibration connects actual
   source bounds and coordinate bases to the target's earlier envelope sites.
   Endpoint correspondences retain acquisition-event identity through transport.
3. The observation and review reports retain candidate measurements, fitting
   policies, source indexes and diagnostic error. These aid inspection; they
   do not secretly control the native receiver's geometry.

The correspondence archive retains the entire selected source interval as
unaccounted read residue. It certifies a supported correspondence, not an exact
fit or a numeric native error calculation. This observation does not drain
samples, transfer material or create tension. There is no assertion that
accepted plus residual geometric error has been numerically settled.

Native restoration checks exact acquisition values and times, complete ordered
source coverage, actual target aliases, final-edge correspondence and distinct
pen-lift identities. The independent Python audit also checks every affine
read, control dependency and actual earlier drawn-control ancestry.

## What this pass reveals

The diagnostic below compares each acquired sample with the native curve at
its corresponding acquired-arclength fraction inside the fitted interval.
Every sample event is counted once, including repeated coordinates. This is
an RMS displacement in the normalized page, not an intention score or a
nearest-outline distance.

| Drawing | Close fit RMS | Gentle RMS | Stronger RMS |
| --- | ---: | ---: | ---: |
| Simple facade | 4.02 | 10.27 | 13.51 |
| Oblique building | 3.90 | 7.00 | 10.57 |
| Clock and side wing | 4.11 | 13.45 | 16.02 |
| Rounded towers | 4.13 | 6.19 | 34.32 |
| Stepped tower | 4.87 | 23.32 | 27.23 |

The close fits preserve the different silhouettes. Gentle expectations can
clarify a facade or cross, while accumulated directional changes can move an
opening or enlarge a gap. The rounded towers are particularly sensitive to
the stronger setting. The stepped tower drifts even at the gentle setting.
These failures are retained rather than repaired by an ideal church template.

A useful next pass is to review these disagreements and compare alternatives
locally: retain a measured tilt, share an axis, close a supported gap, or keep
an open relation. Local part frames should reduce long-chain displacement,
provided their support is recorded. Containment and symmetry need evidence
before they become constraints. Later learned priors can rank these alternatives
using source support and explicit feedback. All variants and descendants of
one acquired source must remain in the same future train/test split.

The independent source/construction audit passes 22,071 checks. Browser checks
cover all 15 programs and 171 rungs, 2,604 native path reads and 81 candidate
selections, including mobile layout and a rejected out-of-span crossing read.
All 179 Python tests pass, and eight native correspondence tests exercise
transport, exact decimal source values, false aliases, residue, coverage and
pen-lift separation. The final native archive batch restores all 15 evidence
graphs, representing 111 source intervals in three alternatives.
The complete solution run passes 5,383 .NET tests across twelve projects; the
eight focused tests also pass against the final reference-identity hashing fix.

Retained reports: [source and proposal audit](results/quickdraw-construction/verification.json),
[browser checks](results/quickdraw-construction/qa/browser-summary.json),
[native evidence](results/quickdraw-construction/native-evidence-summary.json),
and [checkpoint verification](results/quickdraw-construction/checkpoint-summary.json).

## Reproduce and inspect

From the repository root:

```powershell
python experiments/quickDraw/tools/quickdraw_interpretation.py --input D:\_quickDraw\gojo-reconstruction\church\selected.ndjson --output D:\_quickDraw\gojo-quickdraw-structure
dotnet run --project experiments/quickDraw -- --construct --input D:\_quickDraw\gojo-quickdraw-structure\programs.ndjson --output D:\_quickDraw\gojo-quickdraw-structure\native
dotnet run --project experiments/quickDraw -- --interpret-evidence --input D:\_quickDraw\gojo-quickdraw-structure\packets.ndjson --output D:\_quickDraw\gojo-quickdraw-structure\native-evidence
python experiments/quickDraw/tools/verify_quickdraw_interpretation.py --root D:\_quickDraw\gojo-quickdraw-structure --source D:\_quickDraw\gojo-reconstruction\church\selected.ndjson
python experiments/quickDraw/tools/inspect_quickdraw_interpretation.py --observations D:\_quickDraw\gojo-quickdraw-structure\observations.ndjson --programs D:\_quickDraw\gojo-quickdraw-structure\programs.ndjson --native D:\_quickDraw\gojo-quickdraw-structure\native --output D:\_quickDraw\gojo-quickdraw-structure\review.html
python experiments/quickDraw/tools/inspect_quickdraw_interpretation.py --observations D:\_quickDraw\gojo-quickdraw-structure\observations.ndjson --programs D:\_quickDraw\gojo-quickdraw-structure\programs.ndjson --native D:\_quickDraw\gojo-quickdraw-structure\native --portable --output D:\_quickDraw\gojo-quickdraw-structure\review\index.html
python -m unittest discover -s experiments/quickDraw/tools -p "test_*.py"
dotnet test Gojo.slnx
node experiments/quickDraw/tools/check_quickdraw_interpretation.cjs D:\_quickDraw\gojo-quickdraw-structure\review.html D:\_quickDraw\gojo-quickdraw-structure\qa
```

The review needs no server. Set `GOJO_PLAYWRIGHT_MODULE` and `GOJO_BROWSER` for
another installed browser/test environment. The source, compressed inputs,
portable review, evidence archives and verification reports are retained in
[the checkpoint directory](results/quickdraw-construction).
On another machine, use its `selected.ndjson` as the input to the first command;
the full Quick Draw download is not required to reproduce these five cases.

# Reading lines joints and symmetry with interpretive tolerance

This exploratory checkpoint adds annotation reads to the five acquired church
drawings and their three existing construction alternatives. It marks supported
line roles, joint arrangements, reflection and repeated or aligned parts. The
construction programs and original source observations stay fixed. An
annotation can admit an approximate relation while preserving the angle, gap
or unmatched participation that prevents a stricter reading.

Open `D:\_quickDraw\gojo-quickdraw-structure\review.html` for the current review.
The [portable checkpoint](results/quickdraw-annotations/review/index.html) retains
the same inspection controls. The earlier
[construction checkpoint](QuickDrawConstruction.md) describes the source
selection, acquisition preservation and generation of the existing proposals.

## What gets marked

Every fitted source interval has a trace annotation, including intervals with
weak or degenerate support. The read can expose straightness, horizontal or
vertical agreement, slope, curvature and local endpoint tangents. An upper
converging pair can support an apex or roof-like reading. Its actual geometry
and position supply the evidence; the drawing's category name supplies no part.

Joint candidates retain participating source intervals and curve locations,
outward tangent rays, pairwise angular residuals and the spatial gap between
participants. The existing line-art vocabulary remains:

| Read | Participating directional support |
| --- | --- |
| Endpoint | One distinct outward ray |
| I | Two approximately opposed rays |
| L | Two turning rays; right-angle agreement is an additional read |
| T | Three rays with an approximately opposed pair |
| Y | Three branching rays without an admitted opposed pair |
| X | Four rays with two admitted opposed pairs |
| K | Four rays with one admitted opposed pair |

A near-closed O or oval is a separate loop read. It does not add a junction or
close the recorded gap. Smooth continuation and tangent agreement are also
distinct from a physical join. Multiple admissible readings may remain visible.

The same acquired sample event can provide shared source-site identity.
Proximity between two pen strokes, an interior crossing, and equal coordinates
provide different evidence. None of these annotations welds acquired events,
changes endpoint identity or establishes physical attachment.

Reflection reads retain an explicit axis and actual source/target interval
correspondences. A local mirror pair is distinguished from a whole-drawing
reflection proposal. Unmatched intervals and residual mismatch stay visible;
a plausible symmetric opening does not establish a symmetric building.
Parallel, perpendicular and repeated-profile candidates likewise name their
actual participants rather than asserting a whole-object classification.

## Why interpretation admits more

The source measurements are retained while each mode selects wider allowances:

| Mode | Angular allowance | Nearby-part allowance |
| --- | ---: | ---: |
| Measured fit | 10 degrees | 4 normalized page units |
| Gentle interpretation | 20 degrees | 10 normalized page units |
| Stronger interpretation | 32 degrees | 20 normalized page units |

Straightness, reflection and repetition also have explicit per-mode limits.
Straightness limits are 4/7/12 page units, reflection mismatch limits are
6/14/28, and repeated-profile mismatch limits are 5/12/24. A separate one-degree
same-direction test marks coincident rays as ambiguous rather than manufacturing
an extra directional arm. Curved traces also retain signed sagitta and bend
sense in their acquired ordered frame; outward/inward needs a separate region
basis and is not inferred here.
These are authored exploratory choices. A threshold is an admissibility
premise, not a learned probability or a claim that the source angle is exact.
The normalized page is 512 units across, with the earlier aspect-preserving
source calibration retained.

The mode buttons continue selecting their existing captured construction
proposal. They now also select the corresponding annotation tolerance.
Annotations are calculated over the measured source fits; highlighted target
edges are their native correspondences. Where the current proposal changes
the measured relationship, the inspector distinguishes that current geometry
from the source-supported interpretation.

The five fixed drawings produce the following candidate inventories. All 111
source intervals receive a trace annotation in each mode.

| Mode | Trace reads | Joint reads | Reflection reads | Relation reads |
| --- | ---: | ---: | ---: | ---: |
| Measured fit | 111 | 223 | 1 | 38 |
| Gentle interpretation | 111 | 270 | 23 | 113 |
| Stronger interpretation | 111 | 300 | 83 | 210 |

Joint counts include endpoints, overlaps, alternative neighborhoods and possible
cycles. Reflection counts include local pairs and incomplete whole-scope reads.
These are supported candidate scopes, not counts of physical joints or objects.
All computed candidates are retained; display layers and sparse text control
visual density. The clock's scribbles remain a useful dense example rather
than being pruned to an ideal church.

Line, joint, symmetry and relation layers can be toggled independently.
Selecting an annotation highlights its supporting source intervals and native
counterparts. The detail read exposes its measurements, allowance and remaining
qualification. Dense labels can be hidden while all entries remain inspectable.

## Native evidence and meaning

The native annotation graph connects the existing exact acquisition and
construction correspondence to finite source-curve profiles. A profile retains
its actual source interval and actual target edge correspondence. Its measured
controls are an explicit finite read, with the original interval retained as
unaccounted fit residue.

Line reads select those actual profile controls and their tangent participation.
Joint reads select profile locations and ordered outward rays, retaining
opposition and perpendicular discrepancy through the chosen tolerance basis.
The I/L/T/Y/X/K name is a display read of the admitted ray arrangement; the name
does not provide native connectivity. Reflection and repetition retain their
actual correspondences in the observation packet. The native reflection scope
also retains its transformation basis and unmatched support. General relation
rows retain their actual profile participants and numeric ray-pair reads; richer
adapter names such as a roof or repeated profile remain inspection hypotheses.

Explicit participant-location witnesses preserve curve parameters even when a
local derivative vanishes and supplies no directional ray. The nearby-part
allowance uses the diameter between all participant positions. T reads admit
an opposed pair; X reads require two disjoint opposed pairs covering four
rays. Counting opposed pairs without their incidence would produce false X
readings under wider tolerances.

An acquired endpoint is an explicitly witnessed source sample, distinct from
its finite parameter location. An interior location rounded onto parameter zero
does not acquire the source endpoint's identity. Endpoint attestation validates
the original exact parameter and actual acquired sample reference.

The annotations observe existing structure. They do not transfer material,
introduce tension, settle geometric errors or drive a generator. A future
interpretation pass can select among these candidates and apply a repair or
construction through its own explicit operation and witness.

## Verification

The independent annotation audit checks source interval coverage, acquired-site
claims, ordered curve tangents, ray-pair residuals, reflection participants and
widening tolerance. All 15 executable construction archives remain byte-identical
to the preceding checkpoint. Native annotation transport restores 333 finite
profiles, 793 joint reads, 1,803 participant locations, 2,290 rays and 107
reflection reads across the three alternatives.

All 200 Python tests pass. The complete solution run passes 5,396 .NET tests
across twelve projects. After the final endpoint-attestation correction, all
16 focused native annotation tests pass against the final implementation.
Browser checks cover all 171 rungs and 2,604 unchanged native paths, 1,594
annotation markers, 217 selections and all 30 JSON/native annotation downloads.

Retained evidence: [checkpoint summary](results/quickdraw-annotations/checkpoint-summary.json),
[independent annotation audit](results/quickdraw-annotations/annotation-verification.json),
[native annotation summary](results/quickdraw-annotations/native-annotation-summary.json),
and [browser report](results/quickdraw-annotations/qa/browser-summary.json).

## Reproduce the annotation pass

Starting from the existing frozen construction run:

```powershell
python experiments/quickDraw/tools/annotate_quickdraw_interpretation.py --root D:\_quickDraw\gojo-quickdraw-structure
python experiments/quickDraw/tools/verify_quickdraw_annotations.py --root D:\_quickDraw\gojo-quickdraw-structure
dotnet run --project experiments/quickDraw -- --interpret-annotations --input D:\_quickDraw\gojo-quickdraw-structure\annotation-packets.ndjson --output D:\_quickDraw\gojo-quickdraw-structure\native-annotations
python experiments/quickDraw/tools/inspect_quickdraw_interpretation.py --observations D:\_quickDraw\gojo-quickdraw-structure\observations.ndjson --programs D:\_quickDraw\gojo-quickdraw-structure\programs.ndjson --native D:\_quickDraw\gojo-quickdraw-structure\native --annotation-native D:\_quickDraw\gojo-quickdraw-structure\native-annotations\summary.json --output D:\_quickDraw\gojo-quickdraw-structure\review.html
python experiments/quickDraw/tools/inspect_quickdraw_interpretation.py --observations D:\_quickDraw\gojo-quickdraw-structure\observations.ndjson --programs D:\_quickDraw\gojo-quickdraw-structure\programs.ndjson --native D:\_quickDraw\gojo-quickdraw-structure\native --annotation-native D:\_quickDraw\gojo-quickdraw-structure\native-annotations\summary.json --portable --output D:\_quickDraw\gojo-quickdraw-structure\annotation-review\index.html
python -m unittest discover -s experiments/quickDraw/tools -p "test_*.py"
dotnet test Gojo.slnx
node experiments/quickDraw/tools/check_quickdraw_interpretation.cjs D:\_quickDraw\gojo-quickdraw-structure\review.html D:\_quickDraw\gojo-quickdraw-structure\annotation-qa
```

The [retained checkpoint](results/quickdraw-annotations) contains annotation
inputs, the portable review and verification reports. All three alternatives
retain the original acquired source identity for future train/test grouping.

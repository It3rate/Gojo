# Interpreting church drawings through simpler constructions

Status: exploratory, 24 October 2026. This follows the source-supported
[annotation study](QuickDrawAnnotations.md). The five curated drawings are
unchanged. No new model was trained, and this checkpoint is local and on GitHub;
it does not publish a replacement website review.

The objective is a clearer approximation of the conceptual drawing. Increased
distance from mouse-drawn ink can be desirable. Source distance is a diagnostic
and a broad guard against losing correspondence, rather than the objective to
minimize independently. Simplicity and regularity can outweigh tracing accuracy.
The rule strengths are authored experimental priors, not learned probabilities
or proof of what a participant intended.

## Four inspectable passes

1. Retain the existing measured source-span profiles as a baseline.
2. Combine supported continuous intervals into longer lines or whole curves.
   Preserve small tangled detail as an explicitly selected faint guide layer.
3. Try three-sided wall/door rectangles, pointed or closed roof triangles,
   whole door arches, upright/level lines and distinct angular roof slopes.
4. Apply stronger relational preferences: center observed cross arms, balance
   supported roof structure and reuse a similar curve through an affine
   transform. Retry geometric candidates with looser source correspondence.

These are finite local passes over evidence, not a new global Gojo solver.
Every proposed replacement has actual inputs, before/after geometry, source
interval support, a declared distance allowance, complexity and regularity
costs, and an accepted or rejected decision. A patch only replaces its selected
profiles. Retargeting a shared boundary also selects and charges affected
incident profiles; nearby pen lifts do not silently become shared sites.
Original fitted boundary tangents guard against erasing corners through a
sequence of individually gentle merges. Supported upper converging slopes
resist upright snapping, and substantial coherent bends resist flattening.

The local candidate score is

`fitWeight × symmetricRms² + 7 × edges + 3 × controls + regularityCost`.

Fit weight decreases through the passes so conceptual regularity can take
precedence later. The exact weights, allowances and local regularity costs are
saved in the policy and each operation's measurements. The review exposes them.
The current weight/maximum-distance settings are `0.45/36`, `0.12/50` and
`0.04/84`. These are separate adapter decision settings, not changes to the
numeric resolution of the stored drawing.
Distances use a 512-unit page with a 448-unit normalized source extent. Source
samples are compared with sampled proposed curves, and proposed samples are
compared with actual source-polyline segments. Both directions contribute
equally to RMS. This is a finite sampling diagnostic, not an analytic curve
distance. A lower score does not imply a closer trace.

The clock drawing is a useful negative: its clock does not become a cross.
The angled side view, stepped shell and three rounded towers also remain
different constructions. Curated examples were chosen before this experiment;
five drawings cannot establish population accuracy.

| Source | Baseline profiles | Final main profiles | Active faint detail | Baseline / final symmetric RMS |
| --- | ---: | ---: | ---: | ---: |
| 5743197026254848 | 11 | 8 | 0 | 1.85 / 10.37 |
| 5181133614481408 | 17 | 16 | 0 | 1.84 / 6.19 |
| 5437095780810752 | 47 | 27 | 16 | 1.67 / 5.38 |
| 6169132418465792 | 19 | 14 | 0 | 2.46 / 8.12 |
| 4578701515685888 | 17 | 13 | 0 | 2.05 / 5.54 |

These distances describe the accepted floating-point proposal profiles; native
integer calibration is checked independently within two page units per control.
Final distance excludes explicitly retained faint detail. Neither lower edge
count nor greater distance alone certifies a better conceptual interpretation.
Some pen-lift gaps remain unresolved, and some oblique side-wall cues become
more level under the upright/level prior. These are visible limits of this
first interpretation policy.

## What the native structure contains

The [author](tools/quickdraw_gestalt.py) imports accepted measured proposals into
the existing [ConstructionLadder](ConstructionLadders.md) mechanism. Each drawing
has one executable program with four rungs. New sites consume actual earlier
control sites through explicit affine expressions. The finite source/target
frames and their remaining rounding residue are inspectable. Current numeric
coordinates are retained; this is not a nonnumeric completion experiment.

Rectangles have shared corner sites and a four-edge primitive guide. Triangles
have three ordered vertices in a common local frame, including oblique bases.
Observed open boundaries stay open in the main drawing; missing primitive
boundaries remain construction guides. Proposed closure joins are explicit and
do not merge the original acquired events. A cross has a shared proposed center
and helper arms; the two main long profiles consume that center. Balancing a
roof references its earlier apex and actual base midpoint. Affine profile reuse
consumes the earlier template's control sites. The existing specialized repeat
reader validates translations; scaled reuse is expressed through the actual
affine control dependencies rather than mislabeled as a translation.

Containment includes conservative native control-hull checks against the
observed whole-drawing envelope. This is weaker than knowing the interior of a
particular wall. It must not be read as complete architectural understanding.
Local reflection is likewise distinct from claiming the whole church is
symmetric. Smooth whole-curve fits are available; general exact tangency solving,
arbitrary affine rotation/shear, semantic part recognition and learned
architectural constraints remain future work.

The [source/pass evidence graph](QuickDrawGestaltEvidence.cs) connects exact
native acquisition events, the executable program, calibration, the canonical
source-interval partition and the actual rung-plan/target-edge rows. Each pass
contains many-to-many non-draining read links, explicitly unmatched source
intervals, inferred targets, and active residue curves. These observations do
not transfer or duplicate conserved material. All original intervals remain
available as read residue. A historical guide cannot substitute for current
source coverage; active suppressed detail is named separately.

Original stroke, sample and time order remains unchanged. Refinement-pass
history is a later interpretation timeline, not a claim that the person drew
idealized parts in that order. Acquired event identity, projected target sites,
coordinate equality, contact and proposed construction joins remain distinct.

## Review and retained checkpoint

The local review is
[D:/_quickDraw/gojo-quickdraw-gestalt/review.html](D:/_quickDraw/gojo-quickdraw-gestalt/review.html).
It compares the original ink with every restored native pass, with all four
thumbnails, source-supported decisions, primitive roles and separate layers for
suppressed detail, earlier guides, previous-pass geometry and source annotations.
Source annotations are qualified as source observations; they do not certify a
repaired joint in the current interpretation. Rejected replacements are not
rendered as executed geometry.

The repository checkpoint is [results/quickdraw-gestalt](results/quickdraw-gestalt).
It retains exact source bytes, frozen observations, proposals, executable
recipes, source/pass packets, compact native source-free programs and execution
archives, source/pass evidence archives, an offline portable review and audit
reports. JSON and NDJSON archives may be compressed with gzip; decompression
restores the exact recorded bytes. The review's geometry comes only from native
restored execution, never from the manifest's descriptive labels.

## Reproduction and verification

From the repository root, generate the adapters, execute the native programs,
and archive the source correspondence:

```powershell
python experiments/quickDraw/tools/quickdraw_gestalt.py --observations D:/_quickDraw/gojo-quickdraw-structure/observations.ndjson --source D:/_quickDraw/gojo-reconstruction/church/selected.ndjson --output D:/_quickDraw/gojo-quickdraw-gestalt
dotnet run --project experiments/quickDraw -- --construct --input D:/_quickDraw/gojo-quickdraw-gestalt/programs.ndjson --output D:/_quickDraw/gojo-quickdraw-gestalt/native
dotnet run --project experiments/quickDraw -- --gestalt-evidence --input D:/_quickDraw/gojo-quickdraw-gestalt/packets.ndjson --output D:/_quickDraw/gojo-quickdraw-gestalt/native-evidence
python experiments/quickDraw/tools/verify_quickdraw_gestalt.py --root D:/_quickDraw/gojo-quickdraw-gestalt
python experiments/quickDraw/tools/inspect_quickdraw_gestalt.py --observations D:/_quickDraw/gojo-quickdraw-gestalt/observations.ndjson --manifest D:/_quickDraw/gojo-quickdraw-gestalt/manifest.json --native D:/_quickDraw/gojo-quickdraw-gestalt/native --output D:/_quickDraw/gojo-quickdraw-gestalt/review/index.html --portable
node experiments/quickDraw/tools/check_quickdraw_gestalt.cjs D:/_quickDraw/gojo-quickdraw-gestalt/review/index.html D:/_quickDraw/gojo-quickdraw-gestalt/qa-portable
dotnet test Gojo.slnx
```

Focused tests cover accepted/rejected patch replay, actual input ancestry,
shared proposal sites, true affine template reuse, source partition coverage,
false cross/unsupported-ink reads and native transport. Independent auditing
recomputes finite fit and local score, checks the exact acquired source and
selected interval accounting, and replays affine coordinates without a source
geometry fallback. Native round trips also execute the saved program alone.
Browser checks compare every rendered main curve with native inspector controls
and verify that inspection layers do not mutate the current construction.

At this checkpoint, all 5,407 solution tests and 236 Python tests pass. The five
native construction archives and five source/pass evidence archives restore and
replay successfully. The portable browser review covers all twenty native rungs.
The [checkpoint retainer](tools/retain_quickdraw_gestalt.py) verifies current audit
hashes and browser reports before copying exact compressed artifacts into Git.

The useful next training question is which evidence should support each
regularity pressure, and how strongly. The paired original, intermediate
construction, accepted/rejected candidate, retained discrepancy and dependency
records provide examples for that work. They are not yet a learned notion of
the ideal church.

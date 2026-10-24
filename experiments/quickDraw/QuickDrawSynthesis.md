# Joint convergence and view-aware church synthesis

Status: exploratory, 24 October 2026. This extends the
[Gestalt simplification study](QuickDrawGestalt.md) with three later stages.
All four earlier proposal states remain unchanged. The five acquired drawings,
their exact stroke/sample/time order and their source interval partition are
also unchanged. No model is trained in this checkpoint.

The purpose is a coherent construction that still expresses the particular
drawing. A larger difference from the mouse-drawn ink can be desirable. The
late stages therefore construct from their preceding interpretation instead of
repeatedly fitting every decision back to the original coordinates.

## Seven stages

The first four stages retain the previous measured profiles, continuous
profiles, primitive proposals and initial relational interpretation. The added
stages are:

1. **Converge nearby joints.** Try two rounds of nearby endpoint clustering.
   A cluster has a finite radius and diameter, and cannot collapse the two ends
   of one substantial edge. Selected targets become one shared construction
   site. Incident profiles are explicitly replaced to consume that site.
2. **Read the resulting shapes.** Regularize supported lines and whole arches,
   combine redundant fragments, and retain collapsed or subordinate scribbles
   as faint detail. Approximate part readings are hypotheses over actual
   profiles, not permission to manufacture a missing part.
3. **Synthesize in the selected view.** Construct an upright facade, attach a
   supported roof to its actual upper corners, apply local reflection, place
   supported crosses and doors relative to current parts, and construct a
   separate side plane when depth evidence supports one.

The circled roof/wall gaps in the motivating example now become actual common
target corners. Merely moving independent points to equal coordinates would
leave their topology unchanged and would not satisfy this experiment.

The [completion adapter](tools/quickdraw_gestalt_completion.py) uses authored
ordered construction choices. Its late weights are:

| Added stage | Original ink | Previous interpretation | Geometric prior |
| --- | ---: | ---: | ---: |
| Joint convergence | 0.015 | 0.25 | 4 |
| Primitive regularization | 0.004 | 0.12 | 6 |
| View-aware synthesis | 0.001 | 0.04 | 10 |

These weights calibrate inspectable diagnostic costs. They are not learned
probabilities, a precision dial, or a claim of iterative minimization. An
explicitly chosen later construction can increase both source distance and its
weighted diagnostic score. The recorded selection policy says so. Current
control bindings, their earlier inputs and actual local construction laws own
the resulting geometry; the cost report does not silently execute a solver.

Original-ink discrepancy, displacement from the selected current inputs and
geometric constraint mismatch are separate reads. An unchanged detail carried
to a faint guide is an exact retained observation, not a scored replacement.

## Front and oblique views

The view read examines a supported main facade, exterior extent, long outer
walls and angular roof/depth marks. It retains front and oblique candidates
with their supporting actual profiles and a qualification. The current five
examples produce three front-view and two oblique hypotheses. The heuristic
support values are not calibrated camera probabilities.

For a front view, a common local axis can organize the main outline. For an
oblique view, reflection is limited to the facade; a separate side frame
preserves the sign of the supported depth displacement. Its fourth corner is
constructed by `D = B + C - A`, using the actual current corners. The two edge
directions therefore repeat as a parallelogram rather than imposing global
front-facing symmetry on a side section. This is an affine construction
hypothesis, before full projective camera geometry.

Part expectations remain optional and source-supported. The clock drawing
does not acquire a cross. A drawing without a supported door does not acquire a
door. The three-tower drawing keeps three upper parts and three supported
crosses. Their sourced sizes and heights can still differ; this pass does not
impose one universal church template.

## Native construction and identity

The [recipe author](tools/quickdraw_gestalt.py) accepts `--synthesize` to append
the new stages. No new kernel dispatcher is introduced. The existing native
ConstructionLadder and source/pass evidence graph already support arbitrary
rung counts and preserve exact transport.

The relevant connected shapes are:

```text
actual earlier controls -> weighted affine coordinate -> shared target site
shared target site -> each incident target profile
earlier apex and wall corners -> centered roof construction
actual local axis and first profile -> reflected second profile
actual diagonal corners -> other rectangle corners
actual A, B, C -> D = B + C - A -> side-plane edges
```

The bindings can use different actual inputs for x and y. Signed affine
coefficients are coordinate participation, not confidence or opposed flow.
Native finite projection retains its rounding residue. A direct reference to
one existing target point reuses that actual site, instead of wrapping it in a
new point with equal coordinates. Exact reflection consumes the first current
target and its actual axis; exact parallelogram closure consumes the other
three current corners.

Acquired events remain distinct from these target interpretations. A proposed
join does not weld two acquired pen events. Earlier rungs remain available,
and every source interval remains in the active main read or its explicitly
retained faint complement. Replaced intermediate constructions remain guides.
All source correspondence is non-draining observation.

Source-free native programs replay every stage after restoration. The native
source/pass archives additionally connect original acquisition rows to actual
target edge rows and actual rung plans. Descriptive names, view labels and
binding manifests are inspection adapters; replay does not require them.

## Review and checks

Open the local review at
[D:/_quickDraw/gojo-quickdraw-synthesis/review.html](D:/_quickDraw/gojo-quickdraw-synthesis/review.html).
It shows all seven stages, the original ink, retained detail, current view
hypotheses and the changing reference weights. Shared-joint markers are
calculated from actual visible native endpoint identities. A crossing,
proximity or equal coordinates alone does not produce such a marker.

The retained checkpoint is
[results/quickdraw-synthesis](results/quickdraw-synthesis), including an offline
portable review and compact native archives. The earlier four-stage review and
its retained checkpoint remain available for comparison.

The [independent audit](tools/verify_quickdraw_synthesis.py) checks the unchanged
earlier proposal prefix, exact source bytes and timing, complete source/main/
detail accounting, accepted/rejected patch replay, finite diagnostic costs,
actual shared target identities, coordinate-specific affine influence, exact
native reflection and side-plane closure, and native source-free replay.
Equal-coordinate aliases and decorative input references are negative tests.

The checkpoint passes all 5,407 solution tests, 263 Python tests and browser
checks over all 35 native stages. The independent audit verifies 152 bound
target sites, 19 exact native reflection pairs, two axis rectangles and two
side parallelograms. Main profiles decrease from 78 to 72 across the added
stages; the retained faint complement grows from 16 to 20 profiles.

The earlier four stages retain their two-unit native control-import allowance.
The added signed affine constructions declare a conservative six-unit
allowance for accumulated integer projection residue. This concerns the finite
native encoding of its accepted proposal, not how far synthesis may move from
the original drawing. Actual deviations are recorded by the audit.

To reproduce the new pipeline from the repository root:

```powershell
python experiments/quickDraw/tools/quickdraw_gestalt.py --synthesize --observations D:/_quickDraw/gojo-quickdraw-gestalt/observations.ndjson --source D:/_quickDraw/gojo-quickdraw-gestalt/selected.ndjson --output D:/_quickDraw/gojo-quickdraw-synthesis
dotnet run --project experiments/quickDraw -- --construct --input D:/_quickDraw/gojo-quickdraw-synthesis/programs.ndjson --output D:/_quickDraw/gojo-quickdraw-synthesis/native
dotnet run --project experiments/quickDraw -- --gestalt-evidence --input D:/_quickDraw/gojo-quickdraw-synthesis/packets.ndjson --output D:/_quickDraw/gojo-quickdraw-synthesis/native-evidence
python experiments/quickDraw/tools/verify_quickdraw_synthesis.py --root D:/_quickDraw/gojo-quickdraw-synthesis --original-root D:/_quickDraw/gojo-quickdraw-gestalt
python experiments/quickDraw/tools/inspect_quickdraw_gestalt.py --observations D:/_quickDraw/gojo-quickdraw-synthesis/observations.ndjson --manifest D:/_quickDraw/gojo-quickdraw-synthesis/manifest.json --native D:/_quickDraw/gojo-quickdraw-synthesis/native --output D:/_quickDraw/gojo-quickdraw-synthesis/review/index.html --portable
dotnet test Gojo.slnx
```

This remains a small authored experiment. Intentional gaps versus accidental
gaps, architectural part ownership, alternative perspectives, clock/window
interpretation and repeated part proportions still need broader examples and
feedback. The source, intermediate choices, discarded-detail reads and actual
construction dependencies provide useful training material without certifying
that this synthesized version is the drawing's unique intended meaning.
Some rear-roof attachment gaps remain, and a supported cross can overlap its
upper construction. The three crosses in the rounded-tower example keep their
different source sizes and heights even though the facade itself is reflected.

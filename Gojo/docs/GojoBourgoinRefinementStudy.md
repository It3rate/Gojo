# Bourgoin reconstruction: source corrections and local refinements

Follow-up experiment, 2026-10-19. This checkpoint produced fifteen native
examples in the [comparison gallery](../../experiments/QualitativeFrieze/results/bourgoin-patterns/gallery.html), including the preserved B06 interpretation
and its source-oriented counterpart B06.B. Revised cards retain their first-pass
images and provide an adjustable overlay of actual native output on the source.
The subsequent [vocabulary study](GojoBourgoinVocabularyStudy.md) expands that
retained baseline to thirty-six examples and adds a single-sheet atlas.

The first pass established executable descriptions, archive replay and explicit
seam admission. Those checks did not establish agreement with the historical
drawings. User inspection identified differences in connectivity, gap
orientation, corner order and the choice of repeating unit. This round adds
construction-specific checks and closer source comparisons.

## Source corrections

The primary source remains Bourgoin's *Théorie de l'ornement*, the linked 1883
[Archive scan](https://archive.org/details/thoriedelorneme01bourgoog).
These are authored reconstructions with finite shared-reach proportions, not
an automatic image-recognition result.

| Case | Earlier interpretation | Revised construction |
| --- | --- | --- |
| B02, [fig.188](https://archive.org/details/thoriedelorneme01bourgoog/page/n275/mode/1up) | Four rectangles organized as a pinwheel | One elongated rectangle supplies staggered horizontal/vertical images. Small square interstices result from spacing and side contacts; no square motif is placed in a gap. |
| B06.B, [fig.201](https://archive.org/details/thoriedelorneme01bourgoog/page/n278/mode/1up) | The retained B06 has diamond gaps | Apply `R → R+U`, `U → L+U` to the contour and both periods. Upright four-edge circuits now use horizontal/vertical octagon sides. B06 keeps its original geometry. |
| B08, [fig.203](https://archive.org/details/thoriedelorneme01bourgoog/page/n279/mode/1up) | Cranked diagonal waves | Two paired straight ribbon families give large diamond areas, narrow intervening compartments and smaller crossings. Dotted guides are omitted. Short curved/steeper junction transitions remain simplified. |
| B09, [plate1 fig.1](https://archive.org/details/thoriedelorneme01bourgoog/page/n392/mode/1up) | One continuous inward/outward spiral | Short detached sideways-S traversals interleave their extents. Each has two free ends; no seam joins their bodies. Framing rails are omitted. |
| B10, [plate9 fig.4](https://archive.org/details/thoriedelorneme01bourgoog/page/n408/mode/1up) | Orthogonal cranks | Refine diamond-grid edges into pointed four-chord detours, retaining coarse/fine word correspondence and closed compartment boundaries. |
| B11, plate9 fig.5 | Whole-motif reflected rows | Alternate handedness at the appropriate root rows inside a two-course macrocell. Edge reversal and row reflection remain distinct constructions. |
| B12, plate9 fig.10 | Four open spiral arms | Two different closed sixteen-corner negative chambers interlock; mirrored images supply the next course. Their stepped arms/hands and diagonal junctions determine the boundary network. |
| B13, [plate14 fig.3](https://archive.org/details/thoriedelorneme01bourgoog/page/n418/mode/1up) | Four-armed Greek-key contour | A closed twenty-eight-corner serif-I chamber has inset short serifs. Horizontal and quarter-turned instances interlock. |
| B14, [plate19 fig.4](https://archive.org/details/thoriedelorneme01bourgoog/page/n428/mode/1up) | Greek keys attached to a grid | A closed twenty-corner serif-I has serifs at the cap ends. It uses the same staggered horizontal/vertical disposition as B13, with a different boundary. |

The [first-pass diagnostics](../../experiments/QualitativeFrieze/results/bourgoin-patterns/first-pass/diagnostics.json)
and original SVGs retain the earlier results. The untouched recipes remain in
the catalogue; their inclusion is not a new source-fidelity claim.

## Retained native construction

The packet format is unchanged: program, selected bindings, sampling window
and explicit seam law travel together. Recipe names and source pixels do not
select runtime operations. Reused contour images retain:

```text
actual contour prototype -> actual executable path
    -> selected word substitution and its receipts
    -> placement word and difference witness
    -> source-corner selector / actual executable-port correspondence
```

Instances share the prototype identity while keeping separate execution paths,
heads and sites. The field admits joins through current public landmark
publications and the saved seam law. It never joins stroke interiors because
they look coincident in a render.

B10/B11 use the existing `UnitArrangementProgram.Refine` mechanism. One coarse
root-to-neighbour diagonal becomes the boundary chain corresponding to
`(0,0) → (1,1) → (3,1) → (3,-3) → (4,-4)`. These authoring pairs count the
shared unmeasured reach. Native words, actual chord selections, replacement
chains, endpoint-difference witnesses and landmark correspondences remain in
the packet. A chord with both axis contributions is diagonal; successive
chords create actual corners.

This refinement preserves selected endpoint displacement. Its path can bend,
and its enclosed interpretation can change. Retaining a coarse lattice while
refining its boundary consistently is a practical incremental design method.

## Negative compartments and useful simplification

B12 requires two prototypes rather than one fourfold spiral. B13 and B14 share
a disposition while differing in corner order. A negative-space reading can
therefore supply a generator: its chamber boundaries become native executable
paths. Closed paths, occupied faces and the whole complement remain different
claims; native filled-face inference and coincident-edge folding are still
unimplemented here.

An independent diagnostic interprets actual native words on a selected formal
lattice and samples periodic chamber ownership. Each B12 chamber has area98
selected lattice cells; each B13/B14 chamber has area25. Four images cover their
`14 × 28` and `10 × 10` macrocells without missing or multiple ownership at the
tested interior samples. These are diagnostic counts in a chosen basis, not
numeric carriers or a native filled-face theorem.

An initial implementation exposed every reach tick along the boundaries. The
source corners already include every relevant meeting. A regression check now
verifies that no neighbouring chamber corner falls inside another boundary
edge in the seam neighbourhood. Retaining those meaningful corners preserves
contacts and reduces strokes per macrocell:

| Case | With collinear tick splits | With source corners |
| --- | ---: | ---: |
| B12 | 240 | 64 |
| B13 | 208 | 112 |
| B14 | 208 | 80 |

Repeated reaches remain inside longer chord words. This reduces execution and
witness detail at the selected resolution; it does not prove those omitted
sites irrelevant to every possible future question.

## Visual checks and reproduction

The source-overlay panels contain accepted native strokes. Observed engraving
landmarks provide an initial affine screen registration where available.
Registration stays in the presentation catalogue, outside executable packets.
Controls adjust the comparison only, and reset to its initial alignment.

Overlays help expose wrong handedness, edge alignment and compartment order.
They do not certify exact historical geometry or aesthetic quality. Scan
distortion, finite crop, ink width and selected proportions can leave visible
differences. B08 is deliberately a straightened ribbon skeleton rather than a
source-exact junction trace.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --bourgoin-patterns
dotnet run --project experiments/QualitativeFrieze -c Release -- --bourgoin-patterns --cases B10,B11 --output artifacts/diamond-review
dotnet test Gojo.slnx
```

The generated catalogue contains 208 occurrences, 5,364 native strokes and
3,975 accepted seams across fifteen cases. There are no blocked or unresolved
seam obligations in these selected windows; the declared finite frontier stays
separate. B09 has twelve detached ink components, as intended. Every other
case has one joined ink component. Both reusable declarations and archived
packets contain only relation/placeholder participants, with no numeric
elements.

Verification:

- `dotnet test Gojo.slnx`: **4,006 passed, 0 failed, 0 skipped**, across twelve
  test assemblies. This includes twenty-four new construction-specific tests
  and archive/import/execution checks across all fifteen cases.
- B06's rendered ink paths are identical to the retained first pass. B06.B
  is an additional native recipe, not a replacement.
- Browser checks cover all fifteen cards, nine before/after and overlay
  workbenches, the four main display controls, overlay adjustment/reset,
  local links, image decoding and desktop/mobile overflow. No browser errors,
  broken images or broken local links were found.
- Enlarged source overlays were inspected for B02, B09 and B10–B14, alongside
  the other native previews. Finite samples and engraving crops have different
  extents; registration aligns a repeated region rather than their outer cuts.

Execution, target shape checks, finite coverage and visual comparison answer
different questions. In particular, successful native execution does not
upgrade B08's simplified junctions into a source-exact reconstruction.

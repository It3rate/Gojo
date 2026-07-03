# Encoder JSON Fixtures

These files are strict JSON fixtures for inspecting the text-facing encoding
shape. JSON has no comments, so each fixture object uses a `represents` field
as the in-band note for what the object stands for.

`glossary-objects/` contains every Gojo.Blocks glossary-backed object exported
by `BlocksGlossaryExport.Structures()`, with one object per file. Each file is
generated from one `BlocksGlossaryStructureExport` and one source `IElement`.
There is intentionally no page-level structure that contains sibling examples.

`concept-definitions/` contains small definition-ledger samples that are not
Gojo.Blocks pages. These are used when a lower-level concept needs to be built
beside several nearby variants before it is ready to become glossary text. For
example, the boundary/area band sample keeps positive area, anti-area, and
mixed-lane residue side by side so the differences are inspectable in the
encoding itself. The offset variant sample does the same for segment-side
offsets, circumference offsets, interior/exterior facing, orientation evidence,
open/closed offsets, mismatch residue, and crystallized offset reuse. The noun
composition sample then checks a few useful combinations: margin, ring/annulus,
clearance, and keepout, while still leaving them as locally discoverable
definition structure rather than a global CAD noun catalog. The preposition
sample makes the first step beyond nouns: open role slots, a comparison
template, salient focus zones, crystallized focus words such as `near`, and
applied bindings such as store/house or trace/pad.

`scene-mechanics/` is linked from
`Gojo.Encoding/Fixtures/EncoderJson/scene-mechanics/` and contains one compact
pure-structure fixture per scene mechanic from
`Gojo.Encoding/SceneMechanics.md`. These files intentionally keep only points,
segments, directed segments, relations, and groups in the fixture body. The file
names, source ids, definition ids, and carrier ids are inspection labels; the
bodies do not include focus surfaces, read witnesses, projections, or lexical
text.

The glossary fixtures are generated from the live Gojo structures through
`GojoStructureEncodingFixtureProjection`. The concept-definition fixtures are
generated from their own relation ledgers. In both cases, the JSON is an
intentionally sparse documentary read over Gojo structure, not a storage
inventory. Each file keeps only a few local `gojo` observations:

- `known`: the minimal structure the sample appears to assert.
- `paths`: compact read paths that can be inspected without expanding the whole
  encoding surface.
- `tension`: unresolved or deliberately underspecified information that remains
  part of the read.

The fuller relation-shaped encoding surface still exists in code for round-trip
and diagnostic tests, but these fixtures avoid counts, raw carriers, row ids,
observer tables, and other adapter machinery unless a future fixture explicitly
asks for that diagnostic view.

Flow is encoded as `flow`, not as a separate primitive for radial, linear,
sheet, or volumetric forms. Those shapes are consequences of local source
structure and read context: flow on a point in a plane can read as radial, flow
on a span can read as sheet-like or perpendicular, and flow in a volume can read
as volumetric. Directional readings such as inward, outward, positive, inverse,
or opposed participation belong in local paths or tension rather than in a
global flow class.

The current glossary principle is local participation first. A fixture should
not infer extent, direction, relation closure, or a collapsed basis just because
two marks share a visual place. Same-reference roles such as `A@x, B@x` remain
point multiplicity until a shared substrate, opposed lane participation,
collapsed unit shape, or read residue makes a stronger structure visible.
Known lane/sign information can be shown with debug values such as `[+2f]` or
`[+2i]`; unmarked values stay lower-resolution rather than defaulting to a
forward positive lane.

These fixtures also keep physical source structure separable from field,
runtime, and observer interpretations. A later read may encode how a structure
behaves in a field or under an observer limit, but that joint behavior should
not overwrite the sparse object encoding unless the relationship is actually
present in the local Gojo structure.

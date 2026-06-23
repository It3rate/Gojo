# Encoder JSON Fixtures

These files are strict JSON fixtures for inspecting the text-facing encoding
shape. JSON has no comments, so each fixture object uses a `represents` field
as the in-band note for what the object stands for.

`glossary-objects/` contains every Gojo.Blocks glossary-backed object exported
by `BlocksGlossaryExport.Structures()`, with one object per file. Each file is
generated from one `BlocksGlossaryStructureExport` and one source `IElement`.
There is intentionally no page-level structure that contains sibling examples.

The fixtures are generated from the live Gojo structures through
`GojoStructureEncodingFixtureProjection`. They are intentionally sparse
documentary reads over the source structures, not storage inventories. Each file
keeps only a few local `gojo` observations:

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

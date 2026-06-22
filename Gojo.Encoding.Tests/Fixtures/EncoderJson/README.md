# Encoder JSON Fixtures

These files are strict JSON fixtures for inspecting the text-facing encoding
shape. JSON has no comments, so each fixture object uses a `represents` field
as the in-band note for what the object stands for.

`first-10-glossary-objects/` contains the first ten Gojo.Blocks
glossary-backed objects we are using for early encoder inspection, with one
object per file. Each file is generated from one `BlocksGlossaryStructureExport`
and one source `IElement`. There is intentionally no page-level structure that
contains all ten items.

1. The six `resolution-tension` objects.
2. The first four `site-flow-boundary` objects.

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

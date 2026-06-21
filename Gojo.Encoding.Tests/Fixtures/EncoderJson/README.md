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
`GojoStructureEncodingFixtureProjection`. They are intentionally human-sized:
they show expressions, encoding notes, full structure counts, local sparse row
ids, one compact row example from each encoded table, and observer path shape.
Default values and absent links are omitted. They do not attempt to dump every
generated row for every object yet.

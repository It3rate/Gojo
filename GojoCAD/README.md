# GojoCAD

GojoCAD is the Gojo-native graphical format library. It is deliberately not a
renderer, SVG adapter, bitmap sampler, CAD solver, or scene graph. Its job is
to define graphic and geometric concepts as inspectable Gojo structure.

The central rule is the same as the Gojo kernel:

```text
meaning lives in connected carriers, locations, relations, unit shapes,
residue, tension, and timelines
```

A helper may make structure easier to build or read, but the helper must not
become the source of meaning. If a helper disappeared, the structure should
still expose the concept through local Gojo reads.

## Format Direction

A graphical definition should be readable as a connected surface:

```text
GraphicSurface
  selected structure
  topology / connection surface
  focus / feature sites
  flow / direction / lane surface
  metric / parameter surface
  style / attribute participation
  settlement witness / residue / tension
  refinement timeline / provenance
```

The format can remain low resolution. A curve can be defined by tangent,
length, curvature, radius, partition, or transition evidence before any sampled
points exist. A branch can be defined by shared sites and lane transitions
before any drawing engine decides how thick or smooth the junction should be.

## Current Foothold

The first executable surface is intentionally small:

- shared graphic sites through `TickRef`
- point construction over Gojo carriers
- radial flow as an orthogonal outlet from a point
- ordinary positive-face segments
- directed segments whose start is aligned inverse flow and whose end is
  positive flow

These are enough to start defining dots, radial potential, ordinary spans, and
the inverse-start/positive-end directed segment shape before adding linework
composition, curves, or junctions.

`GraphicReadGraph` is the first executable read-algebra surface over these
relations. It derives temporary read nodes such as points, spans, radial seeds,
circular markers, contour spans, point attachments, shared sites, segment
connections, partitions, and early second-order control reads without making
those code objects the source of meaning. It can also navigate from changed
relations, carriers, locations, and shared sites to report the local reads that
need to be reconsidered.

## Non-Goals For Now

- no rendering
- no path flattening
- no pixel output
- no Bezier, clothoid, or involute calculation
- no global shape classifier
- no metadata bag that owns graphic meaning

Output systems can come later as lenses over settled or partially settled
GojoCAD structure.

## Important Reads To Preserve

- lower-resolution reads are valid reads
- unresolved parameters should remain visible as residue or tension
- equal visible geometry may have different provenance
- joins should be actual shared sites or explicit relations, not equal numbers
- branch, merge, intersection, and crossing are different local reads over
  connection structure
- style presence and style detail should be separate participation surfaces

See [docs/FormatPlan.md](docs/FormatPlan.md) for the current roadmap.
See [docs/ReadAlgebra.md](docs/ReadAlgebra.md) for the initial relation-read
algebra and read graph direction.

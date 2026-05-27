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
- directed spans
- ordered polylines
- closed polyline reads
- branch and merge surfaces through shared junction sites
- unsolved curve-transition evidence
- clothoid-like length-to-curvature transition evidence

These are enough to start defining simple curves, stroke order, connected
linework, and Y/T style junctions without adding rendering or symbolic shape
enums.

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

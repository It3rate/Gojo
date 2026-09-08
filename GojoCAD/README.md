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

The September 2026 [syntax assessment for CAD and electronics](../Gojo/docs/GojoCadEcadSyntaxAssessment-2026-09.md)
compares this inventory with current kernel constructions, the sibling GojoEcad
importer and external formats. It distinguishes implemented reads from proposed
geometry, uncertainty, circuit and interchange contracts.

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
connections, partitions, early second-order control reads, and closed segment
boundaries without making those code objects the source of meaning. It can also
navigate from changed relations, carriers, locations, and shared sites to
report the local reads that need to be reconsidered.

## Non-Goals For Now

- no rendering
- no path flattening
- no pixel output
- no built-in curve sampler or clothoid/involute calculation
- no global shape classifier
- no metadata bag that owns graphic meaning

Output systems can come later as lenses over settled or partially settled
GojoCAD structure.

The kernel's [selected affine construction reads](../Gojo/docs/GojoAffineConstructionFixture.md)
now exercise Bézier interpolation and local derivative constraints over exposed
coordinates. These are exact, non-mutating reads of authored construction
graphs; they do not add a general curve sampler or change the compact CAD
recognition heuristics. Rational coordinate results require that selected
reader; existing CAD point recognition still reads integral locations.

## Important Reads To Preserve

- lower-resolution reads are valid reads
- unresolved parameters should remain visible as residue or tension
- equal visible geometry may have different provenance
- joins should be actual shared sites or explicit relations, not equal numbers
- two-span joins, higher-valence junctions, and closed boundaries should stay
  separate until stronger evidence upgrades them
- local flow evidence should be carried upward from joins into boundaries
  before naming turns, corners, or smoothing
- carrier unit and descriptor provenance should be carried separately from
  geometry so turns and changes of kind do not collapse into one read
- shared-frame polylines and repeated-step polylines should emerge from
  coordinate groups, segment joins, and unit/template provenance
- branch, merge, intersection, and crossing are different local reads over
  connection structure
- style presence and style detail should be separate participation surfaces

See [docs/FormatPlan.md](docs/FormatPlan.md) for the current roadmap.
See [docs/ReadAlgebra.md](docs/ReadAlgebra.md) for the initial relation-read
algebra and read graph direction.
See [docs/EcadDocumentaryBundle.md](docs/EcadDocumentaryBundle.md) for the
current direction on PCB projects as Gojo-encoded documentary bundles.
See [electronics/components/README.md](electronics/components/README.md) for
the first experimental electronics component-encoding notes.

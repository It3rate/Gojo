# Circle construction before coordinates

Status: implemented exploratory construction contract, September 2026.

[Open the twelve construction stages](examples/contour-rotation.html).

The follow-up [primitive construction audit](GojoPrimitiveConstructionAudit-2026-09.md)
checks this standard against parallel segments, Béziers and other graphic
elements. It includes a constructed-parallelogram-to-Bézier syntax fixture
and records where native construction is still missing.
The next [basis-to-Bézier walkthrough](examples/grounded-affine-basis.html)
shows a justified affine basis, partial points and independent viewing choices.

The first rotation example supplied a plane, metric and radial endpoint
coordinates. It tested a later turning operation but skipped how a contour,
radius and landmarks come into being. This fixture starts before that basis.

## The structure to build

```text
source       = P(origin,0)
outward      = O(source)
contour      = carrier with point-orthogonal unit provenance
offset       = outward -> (common reach -> contour)
radial       = group(source -> radial start,
                     radial start -> radial end,
                     radial end -> contour reference site)
whole        = contour reference site -> end of one traversal
closure      = end of one traversal -> same contour reference site
circle       = group(offset, radial, whole, closure)
partition    = group(circle, subdivided whole -> whole)
mark request = group(partition, selected subdivision site)
```

`crystal(outward)` stabilizes the already-readable contour-potential grammar;
it does not by itself crystallize a complete physical circle or an angle unit.
The common reach can initially be an open slot. Selecting an offset gives a
level path; creating and attaching a radius supplies a local measure for that
reach and a reference site. The radial carrier is derived from a collapsed
segment at the same source. Its direction is established by attachment, with
no X axis, Y axis or world bearing. Choosing its span `0..1` uses the selected
radius as one native radial tick, without assigning millimetres to it.

The whole uses contour coordinates `0..1` plus an explicit return connection.
Zero and one are different positions in the *unwrapped traversal*. The closure
connects the end back to the actual starting site. The entire path is therefore
not a zero-length segment merely because it returns to the same site.

Only after a whole is present do `subdivision(unit_of(contour),n)` and its
explicit whole-to-whole correspondence provide equal native contour parts.
This selects subdivision of the circumference, not arbitrary placement of
numbered markers. Four parts, ten parts and 360 parts are different reads of
the same whole. Counting 360 unrelated landmarks would not establish degrees.

This is a selected homogeneous planar offset-contour contract. It records the
construction and its exact intrinsic partitions; it does not discover a plane,
solve a field, or infer an isotropic distance law for every possible container.
A point's orthogonal possibility remains broader than a completed circle.
The analogous segment-offset path belongs to the same source/outlet/offset
idea, but the circle reader must refuse a segment source.

## Where the landmarks come from

The [authored syntax](../../Gojo.Encoding.Tests/Fixtures/Syntax/circle-construction.gojo)
uses ordinary version-1 constructors throughout. It exposes the offset before
its reach is bound, the radius binding, the circle pattern, four subdivisions
and seven selected landmark requests. The caller applies the radius binding
with `StructuralPattern.Refine`, preserving shared references, before asking
for the complete circle and its partitions.

```gojo
let $fourths = carrier(subdivision(unit_of($contour), 4));
let $four = group($circle_pattern, span($fourths, 0, 4) -> $whole);
root group($four, point($fourths, 1));
```

The first line supplies a finer tick promise. The second connects its complete
four-part span to the actual whole circumference. Both are needed: compatible
units alone do not identify which whole is being partitioned. The selected
equal subdivision is a construction rule; the code does not infer equal
spacing from an arbitrary list of landmarks.

A landmark read at subdivision tick `k` of `n` retains
`k/n = completed traversals + wrapped whole participation`. For example:

| Selected construction | Wrapped contour position | Signed completed traversals |
| --- | --- | --- |
| First of four parts | 1/4 | 0 |
| First of ten parts | 1/10 | 0 |
| 90 of 360 parts | 1/4 | 0 |
| 900 of 3,600 parts | 1/4 | 0 |
| 637 of 3,600 parts | 637/3600 | 0 |
| Four of four parts | 0 | 1 |
| One part backwards in a four-part basis | 3/4 | -1 |

These are exact intrinsic positions under the selected construction. Their
precision does not assert measurement accuracy. The last row uses signed
quotient/remainder accounting: `-1/4 = -1 + 3/4`; it did not travel a whole
circuit backwards. The captured request preserves the actual signed travel.

Away from the seam, the reader creates a new radial carrier with the same
point-derived linear unit promise, connects its start to the source and its
end to the selected contour site, and retains a source-radius-to-new-radius
correspondence. Its reach therefore follows the established offset. It does
not borrow the original radius's endpoint identity for a different site.
At the seam, closure returns the actual shared reference site and original
radial attachment. Fine and coarse markers elsewhere may have corresponding
positions without being the same raw tick reference.

## Implemented procedures

[ContourConstruction](../Structures/ContourConstruction.cs) supplies four
selected reads over ordinary relation structure:

- `TryReadOffset`: reads a point-derived shared offset. An open reach remains
  valid partial structure and appears in the witness's remainder.
- `TryReadCircle`: requires that the radius bind that same reach, that both
  radial ends be attached, and that the whole close at the actual reference.
- `TryReadPartition`: requires both the subdivision unit promise and the
  explicit correspondence to this circle's whole.
- `TryLocate`: derives the selected native site and its radial attachment,
  retaining wrapped participation, signed traversals and radius correspondence.

Successful reads capture exposed numeric sites and reference sharing. Carrier
identities and stable unit promises are retained; private attachment worlds
are not traversed. The selected-source field is a backlink, while the captured
request in provenance supports historical rereading. No carrier value or
tension is changed. These construction reads do not model physical motion or
autonomous settlement.

The native construction does not yet execute a bridge into the earlier
[coordinate-based plate experiment](GojoContourRotationFixture.md). That
experiment's `(13,24)` endpoint remains supplied geometry. Claiming that the
new intrinsic tick `637/3600` is that old endpoint would require an actual
calibrated read; no such correspondence is invented here.

## Degrees, radians and scale

The later [metric construction fixture](GojoMetricConstructionFixtures.md)
now supplies a bounded native-to-coordinate bridge. It calculates four quarter
rim observations from this circle, an explicit metric and radius calibration,
then passes them to the existing rotation reader. General fractions such as
`637/3600` and an arc-length/radius correspondence remain outside that bridge.

A 360-way equal division can establish the degree convention with provenance
back to the whole. A degree is therefore not an unexplained scalar inserted
before construction. An arbitrary count of 360 marks would not suffice.

A radian read requires arc length compared with radius length on a compatible
length basis. Native circumference fractions do not yet supply that length
correspondence. The Euclidean relationship involving pi can be a later
calibrated or learned mathematical construction, with its applicability and
resolution retained. This fixture contains no pi constant or trigonometric
evaluation. Subdividing the radius is possible with the same existing unit
mechanism, but doing so alone does not establish arc length.

Similarly, a later radial-to-millimetre calibration can supply physical scale.
It is separate from partitioning the circumference. A finer contour subdivision
does not imply a larger or more accurately measured circle. Clockwise, world
orientation and X/Y coordinates require further attachment to an observer or
coordinate frame.

## What the old anchor meant

In `let $anchor = $origin -> $centre;`, the right-hand side created a new
ordered relation. `$anchor` was only a file-local name for that relation. It
was not an alias for either endpoint, a special anchor primitive, or a
pre-existing point on the circumference.

`$origin` was the source point and `$centre` was its supplied X/Y representation.
The later fixture now calls the relation `$centre_representation`. The native
construction needs no such extra representation: its source point is the
centre, and the radius-to-contour attachment establishes its reference site.

## Verification and limits

[GojoSyntaxCircleConstructionTests](../../Gojo.Encoding.Tests/GojoSyntaxCircleConstructionTests.cs)
checks open construction, closure identity, radial attachment, subdivision,
signed traversal, large counts, wrong sources and lookalike connections,
captured evidence and numeric restoration. It also checks the twelve demo
stages and displayed syntax against the authored fixture and selected readers.

This is an exact selected construction. Uncertain radial geometry, physical
offset discovery, continuous length parameterization, general segment offsets,
3D frames and learned calibration remain further work. The browser chooses
screen coordinates, radius and orientation only to illustrate the intrinsic
relations; its SVG transforms do not supply kernel results.

At this checkpoint, `dotnet test Gojo.slnx` passes 1,446 tests: 782 kernel,
488 encoding, 155 CAD and 21 gear-ladder cases. There are 25 dedicated native
circle cases. Desktop and phone browser checks cover all twelve construction
stages, both disclosures, links, keyboard controls and direct step URLs;
representative stages were visually inspected.

# Selected affine construction, joins and motif correspondence

Status: implemented local reads and syntax fixtures, September 2026. This
continues the [graphic syntax review](GojoSyntaxGeometryReview-2026-09.md).
It addresses that review's interpolation contract, continuity evidence and
pointwise correspondence gaps without introducing graphic opcodes.

The [kernel helper](../Structures/AffineFrame.cs) observes explicitly selected
relations. The parser still only constructs a graph. The fixtures apply each
local read and bind its witnessed result into the next request; there is no
new general execution loop, sampler or settlement policy.

## Structure before helper names

```text
coordinate basis = group_on(space, S(x,0,1), S(y,0,1), ...)
point            = group_on(holder, exact x coordinate, exact y coordinate, ...)
grade            = group(sample, parameter frame, allowed support, coordinate basis)
interpolation    = ports(first, second, grade) -> requested output
displacement     = group(first, second, coordinate basis)
projection       = group(point, ports(coordinate calibrations...), target basis)
motif mapping    = group(motif, ports(exposed points...),
                        ports(coordinate calibrations...), target basis)
```

These are selected contracts over ordinary relation structure. Their shapes
are not universal classifiers. In particular, the generic three-input
`TransformSurface.Interpolate` reader alone does not prove an affine law.
`AffineFrame.TryInterpolate` additionally requires the four connected grade
fields and checks the coordinates against the selected basis.

The coordinate basis has nonempty, distinct, ordered, concrete forward axes.
Each unit span supplies the local tick calibration. A reversed parameter or
target frame remains valid; an opposed carrier face is not silently converted
into one. Points may have different holders while sharing the selected ordered
axes, consistent with current CAD coordinate reads. Output points use the
explicit basis holder. Repeated dimensions in unit products remain legal;
rejecting duplicate axes here concerns this particular coordinate read.

No Euclidean metric, orthogonality, angle or physical contact follows from a
list of axes. Different units may coexist on different axes. Cross-carrier
calibration must be explicit.

## Interpolation and exact fractions

For an ordered parameter frame `[a,b]`, the selected read uses
`u=(sample-a)/(b-a)` and `(1-u)A+uB` coordinate by coordinate. The frame must
have nonzero extent. Allowed support is a separate closed interval with
ascending endpoints on that same parameter carrier. Extrapolation requires
support that admits it; reversing the frame changes the parameter read.

For example, the [affine fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/affine-frame.gojo)
interpolates `(-1,2)` and `(2,3)` one third of the way to obtain `(0,7/3)`.
The fraction is retained as `at(y,7) -> point(y,3)`. This uses the existing
grounded subdivision representation, not fractional carrier state or a float.
Exact rational samples are also accepted in the parameter role.

The internal integer/rational arithmetic is shared with `BoundedQuantity`.
Products use wide integers; each published numerator and positive denominator
must still fit the numeric format's signed 64-bit ticks. An unencodable result
returns an unresolved witness containing the request. It is not rounded or
wrapped. An intermediate that cannot be encoded can therefore block a chain
even when a later result would fit.

No whole-tick rounding is performed between construction steps. A coarser
display/material read needs its own selected resolution and remainder policy.
The existing CAD point classifier still expects integral coordinate locations;
these rational points are consumed by the selected affine reader, without
silently extending every graphic classifier.

## Partial structure and witnesses

An unknown coordinate stays attached to its dependencies. Other coordinates
can still be calculated. A wholly open point can acquire distinct open
coordinate roles from the explicitly selected basis; this does not assert
that its x and y values are equal. Repeated coordinate slots remain shared.

At a parameter endpoint, an unneeded open control does not prevent an exact
endpoint read. An incompatible *grounded* control still prevents the read:
zero weight cannot hide a wrong basis. Displacement cancels the same open
coordinate or whole point against itself, but does not cancel separate slots
merely because both are unknown.

All operations return ordinary five-part `ReadWitness` structure:
source, selected surface, result, remainder, provenance. An exact successful
read has an empty ordered remainder group. A partial read has known/open
coordinates and dependency-to-result relations in the remainder. A malformed
or unsupported request has an open result and retains the entire request.
`false` can therefore accompany a useful partial result; it is not a verdict
that the source structure is meaningless.

Empty remainder means every requested coordinate was computed under the
selected function. It does not assert that a map is invertible or conserves
material: a constant map can discard distinctions while remaining exactly
computable. The source remains in the witness. Material transfer still needs
its own accepted, blocked and remaining participation witnesses.

For interpolation, provenance connects the requested output with the observed
point. A supplied output of `999` is retained even if the local calculation
gives `2`. Success means the observation was calculated; it does not validate
the claim, identify the faulty participant, or change carrier value/tension.

These are finite observations. If a control, parameter or calibration changes,
the dependent reads must be rebuilt or explicitly invalidated. The tests use
fresh application bindings for each curve sample. Reusing earlier intermediate
bindings would reuse earlier observations, not reevaluate their formulas.

## Curves and continuity

The updated [Bézier fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/bezier-construction.gojo)
uses six adjacent interpolation requests with one parameter contract. Tests
compare their results with an independently written Bernstein polynomial at
nine parameter values, including fractions that must survive intermediate
steps. The construction follows the
[de Casteljau relation](https://pages.mtu.edu/~shene/COURSES/cs3621/NOTES/spline/Bezier/de-casteljau.html).

Closed endpoints, repeated controls and coincident points retain a valid
construction. Reversing the control sequence and using the complementary
parameter gives the same coordinates. No compact chord/control heuristic is
used to decide which curve the construction means. The earlier ambiguous
pair-of-spans classifier remains an explicitly documented legacy limitation.

The [bridge fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/bridge-continuity.gojo)
connects control polygons, a common normalized parameter, adjacent differences,
threefold/sixfold calibrations, desired zero mismatches and open curvature/history.
The endpoint derivatives of a cubic use `3(P1-P0)` and `3(P3-P2)`; second
derivatives use six times the difference of adjacent legs. See the
[derivative construction](https://pages.mtu.edu/~shene/COURSES/cs3621/NOTES/spline/Bezier/bezier-der.html).

| Selected evidence | Fixture result | Limit of the conclusion |
| --- | --- | --- |
| Shared junction reference | Incoming end and outgoing start are the same participant | The rest of the bridge need not be smooth |
| Endpoint displacement | Zero for both a shared junction and an independent lookalike | Coordinate agreement does not manufacture site identity |
| First derivative difference | Zero with derivatives `(3,0)` on both sides | C1 is conditional on the common parameterization |
| Second derivative difference | `(0,6)` while the requested mismatch is zero | This join is not C2 under that parameterization |
| Positive tangent rescaling | `(6,0)` becomes `(3,0)` under explicit half-scale calibration | Same forward direction is weaker than equal derivatives |
| Collapsed first leg | Zero derivative | No regular tangent direction is established by this result |

These examples exercise local derivative constraints; they are not a general
continuity classifier. Different parameter speeds require explicit calibration
before derivatives can be compared. C2 is not itself a curvature read.
Geometric curvature needs a metric and regularity, and a zero first derivative
requires further analysis. The fixture keeps that relationship open rather
than equating a zero vector with a smooth join. Existing Blocks controls remain
illustrative; this pass does not fit replacement bridges or claim that those
examples satisfy these constraints.

## Mapping an exposed motif

[`TryMapExposed`](../Structures/AffineFrame.Correspondence.cs) projects only
the explicitly exposed point references in a selected motif's relation graph.
It does not traverse carrier attachments or discover private geometry. Each
projection records a source-to-result clause and its own read witness.

Repeated point and subgraph references remain repeated references in the
result. Shared whole-coordinate ticks stay shared under the same calibration;
the same unresolved coordinate also yields a shared unresolved result.
Independently authored equal coordinates are not merged, even if a projection
collapses them to the same value. Rational coordinates reuse identity when
the same rational coordinate relation is exposed; a matching numerator alone
does not identify a fractional coordinate across different subdivisions.

Unexposed structure remains connected and unchanged. Success covers the
exposure list, not a claim that every geometric feature was discovered or
transformed. A separate complete-exposure contract would be needed for that
stronger claim. Disjoint exposure is required; an ancestor and descendant
cannot both replace overlapping regions in this read.

The extended [frieze fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/frieze-patterns.gojo)
exposes all three triangle sites. Applying the glide twice agrees with one
translation on the actual motif, retaining edge order and shared junctions.
An inverse calibration returns the original coordinates and retains the route;
it does not revive the source object's identity. The resulting witness can
fill the existing transform's open correspondence slot. The repetition
support remains open and the finite observation window remains separate.

## Remaining boundaries and next useful fixture

The immediate three contracts now have executable examples. Remaining work is
specific rather than a missing numeric vocabulary:

- Range/distribution-valued curve parameters and controls need dependency-aware
  enclosures across the construction. Current open slots are not probability
  distributions; a chain of partial results is not a normalized symbolic solver.
- General curvature, parameter-speed matching and metric-dependent geometry
  need explicit local laws and witnesses.
- Live revision must invalidate/replay dependent observations. Coordinate
  correspondence alone does not establish that ongoing protocol.
- Full motif exposure, general cross-axis affine matrices, integer-domain
  repetition, and generator discovery remain separate extensions. The current
  coordinate maps are separable and the fixture is not a frieze classifier.
- Authoring imports, reusable text definitions and a canonical writer remain
  conveniences to examine after these small contracts have more users.

The next useful exercise is a control-point or period change followed through
existing dependency/replay surfaces: keep the earlier sample, expose why it is
stale, and build the new sample without losing correspondence. That stays close
to the live-system thesis while keeping the mathematical contract fixed.

## Verification

[Kernel tests](../../Gojo.Tests/Core/AffineFrameTests.cs) cover exact arithmetic,
invalid bases, support, partial inputs, alias cancellation and overflow.
[Syntax tests](../../Gojo.Encoding.Tests/GojoSyntaxAffineFrameTests.cs) exercise
the actual fixtures before and after one/two numeric restorations, and restore
computed witness chains as well. The inventory additionally round-trips each
complete fixture graph. Solution verification uses `dotnet test Gojo.slnx`.

# Circle calibration, measured offsets and a joined corner

Status: September 2026. Implemented selected reads, with syntax fixtures and
exact conformance tests. These complete three bounded follow-ups from the
[primitive construction audit](GojoPrimitiveConstructionAudit-2026-09.md).

The [walkthrough](examples/metric-construction.html) shows the inputs,
intermediate constructions and an explicitly refused join. Its displayed
traces and syntax are checked against the executable fixtures. It illustrates
captured kernel results; the browser does not execute Gojo or discover geometry.

## What is supplied and what is constructed

| Example | Supplied participants | Calculated result |
| --- | --- | --- |
| Circle calibration | Intrinsic circle and whole subdivision; selected metric chart; centre observation; radius calibration; reference direction and relative side | Four native quarter landmarks, their metric radial observations, and a surface consumed directly by the existing rotation reader |
| Straight offset | Ordered source edge; selected metric; its orthogonal outlet; native reach mapped to a length span; a relative side guide | A metric-normal displacement applied equally to both endpoints, with endpoint correspondence and measurement evidence |
| Joined offset | Two source edges sharing the actual corner, metric and reach calibration; two offset requests; explicit parameter supports | Two calculated offsets, their support intersection, and two trimmed/extended edges sharing one calculated miter site |

Input observations and design choices are legitimate. They are retained as
inputs instead of being attributed to a construction that did not produce them.
The metric chart is explicitly supplied in these fixtures. The earlier
[grounded basis](GojoGroundedAffineBasisFixture.md) remains a valid source of
affine coordinates, but does not automatically supply a physical metric.
Moving its coordinates into this common length chart needs explicit affine
calibration. Its two independently described units are not silently identified.

All three reads are observations/constructions. They produce new exposed
structure without changing carrier value or material tension. A physical
movement or changed design would require a separate import and settlement.
Successful witnesses capture numeric inputs, so changing a live calibration
requires a fresh read; it does not rewrite the old result.

## One common metric contract

The existing rotation metric has been factored into a shared internal reader.
Its public structural form is unchanged:

```gojo
let $basis = group_on($space, $ux, $uy);
let $metric = group($basis, frame(
    ($ux -> $ux) -> $xx,
    ($ux -> $uy) -> $xy,
    ($uy -> $uy) -> $yy));
```

The two actual unit spans identify the ordered coordinate roles. Coordinates
must use the same holder and coordinate carriers. The axes have compatible
linear unit shapes; all three pairings use one compatible squared-unit carrier.
The selected symmetric pairing must be positive definite: `xx > 0` and
`xx*yy - xy*xy > 0`.

This selects a flat Euclidean metric expressed in a possibly skew chart. It
does not infer physical perpendicularity from the generic orthogonal outlet.
For example, after expressing a point as `x' = x+y, y' = y`, the ordinary
metric has pairings `(1,-1,2)`. A normal may then look oblique in the drawing.
The tests exercise this change of chart and both choices of relative side.

A reach calibration is an actual relation between a chosen native span
`S(reach,0,n)` with `n > 0` and `S(length,0,d)` in the metric's common linear
unit. This first contract requires nonnegative integral target ticks; output
coordinates can still be exact fractions. It does not silently convert a
foreign or finer unit. Direction is selected separately from the magnitude.

## From a native circle to a usable turn

Fixture:
[`constructed-circle-rotation.gojo`](../../Gojo.Encoding.Tests/Fixtures/Syntax/constructed-circle-rotation.gojo).
Reader: `ContourConstruction.TryCalibrateQuarters`.

The circle is first built in the existing way: source point, orthogonal outlet,
common radial reach, radius attachment, actual seam and partition of a whole.
The selected metric read then consumes:

```gojo
root group($partition, $metric,
    $origin -> $centre,
    $reach -> span($length, 0, 5),
    $centre -> $direction,
    $centre -> $side);
```

The native reach must be the actual radius in that circle. The centre attaches
to the actual source site. The direction and side relations share the actual
centre observation. Neither a matching label nor independently equal point
coordinates establish those connections.

In the fixture the centre reads `(10,20)`. The reference point `(16,28)` selects
direction `(6,8)`; it is not a supplied rim landmark. Calibrating the radius to
five length ticks constructs the first displacement `(3,4)`. A guide toward
`(10,21)` selects the relative side. The metric-normal equal-reach displacement
is `(-4,3)`. Their opposites complete the four quarters:

| Native participation | Constructed rim observation |
| --- | --- |
| Start | `(13,24)` |
| Quarter | `(6,23)` |
| Half | `(7,16)` |
| Three quarters | `(14,17)` |

Every squared radius is `25`. In the 360-part fixture these are actual native
markers `0,90,180,270`; using four or 3600 parts produces the same metric read.
The selected homogeneous metric-circle law identifies equal native quarters
with the two perpendicular radial directions and their opposites. This law is
an explicit selected interpretation, not a conclusion about every closed path.
No degree constant, radian value, trigonometric sample or output rim coordinate
is authored in the fixture.

The result is the existing rotation surface with the native partition in its
perimeter position. The start marker retains the actual circle seam; the other
markers retain their subdivision carrier. We do not invent a second carrier
and silently equate its sites with the native ones. Native positive cyclic
order is checked, including rejection of a numerically equal but unshared seam.

Provenance retains all four native locate witnesses and explicit relations
from each native radial attachment to its calculated metric observation.
The existing traversal and two-reflection rotation readers then consume that
surface. An attached point `(12,20)` turns to `(10,22)` on one positive quarter.
Repeated exposed point references remain shared through application and numeric
restoration. Traversal steps are edges in the selected cycle: one step here is
one quarter, not one of the 360 original fine ticks.

This read requires a partition divisible by four. Tenths remain a valid native
partition but need another embedding procedure. Arbitrary circle fractions,
arc length, circumference/radius calibration and general algebraic-coordinate
evaluation are not implemented by this bridge.

## Constructing a measured straight offset

Fixture:
[`measured-segment-offset.gojo`](../../Gojo.Encoding.Tests/Fixtures/Syntax/measured-segment-offset.gojo).
Reader: `SegmentOffset.TryConstruct`.

```gojo
let $edge = $a -> $b;
let $calibration = $reach -> span($length, 0, 2);
root group($edge, $metric,
    orthogonal($edge) -> $calibration,
    $a -> $side_point);
```

The outlet belongs to the actual edge. The guide is relative to the actual
start, and must have some metric-normal participation. Its tangent component
does not affect the selected side. A tangent-only guide leaves the side open.
Opposing the guide and reversing the edge are different inputs; reversing an
edge while retaining the same physical guide can preserve the physical side.
Neither operation defines world-up or world-left.

For direction `v` and pairing `G`, the reader constructs a direction `n` such
that `vᵀGn = 0`, scales it to the requested metric length, and chooses its sign
from the guide's pairing with it. It uses the same displacement at both ends:

```text
A' = A + n
B' = B + n
B' - A' = B - A
vᵀGn = 0
nᵀGn = d²
```

For `(10,20) -> (13,24)` at distance two, the selected displacement can be
`(-8/5,6/5)`. The squared displacement is exactly four, and its dot product
with `(3,4)` is exactly zero. This is stronger than supplying a parallel edge
or checking endpoint distances separately.

The result is the calculated ordered edge. Witness provenance is:
`group(endpoint correspondences, displacement, dot product, squared distance)`.
Correspondences retain the captured source and actual output endpoints. A zero
distance preserves those captured endpoint sites; a collapsed source still
does not establish a regular direction.

All normalization arithmetic is rational and uses exact integer square-root
checks. If a required coordinate is irrational, the read returns a placeholder
and retains the request as remainder. That is an implementation boundary, not
measurement uncertainty or evidence that the geometric answer does not exist.
No guessed floating-point tolerance is attached. The later
[measured-offset refinement](GojoMeasuredOffsetRefinement.md) now adds explicit
preparation and observation stages: retain the normal, its open square equation
and shared endpoint recipes, then observe guaranteed correlated bounds at a
chosen subdivision. The exact `TryConstruct` contract remains unchanged.

## A shared corner with bounded straight supports

Reader: `SegmentOffset.TryJoin`, using the same offset fixture.

Consider the source path `(0,0) -> (6,0) -> (6,6)`. The actual middle point is
shared by both source edges. At distance two toward the interior, the first
calculated offset is `(0,2) -> (6,2)`, and the second is `(4,0) -> (4,6)`.
Those two displaced copies of the old corner are different sites. A join must
be constructed; equal distances do not already connect them.

The selected policy intersects the straight supports and permits only the
parameters admitted by both supplied windows:

```gojo
let $first_whole = span($t, 0, 1);
let $second_whole = span($u, 0, 1);
root group($offset_a, $offset_b, group(
    $first_whole -> $first_whole,
    $second_whole -> $second_whole));
```

The calculated intersection is `(4,2)`, at parameters `2/3` and `1/3`. Both lie
inside their whole segments. The result is two ordered edges with the **same
constructed joint object**: `(0,2) -> joint -> (4,6)`. The witnesses retain the
two independent offset constructions, intersection and parameter evidence,
and correspondence from each offset edge to its trimmed/extended result.

On the opposite side the intersection is `(8,-2)`, at `4/3` and `-1/3`.
The whole-only policy refuses the join. It retains both valid offsets and the
candidate intersection as evidence, with an unresolved result and remainder.
Explicit supports `[-1,2]` authorize this bounded extension. There is no hidden
unbounded miter or automatic switch to a round corner.

The two requests must share their actual source corner, metric and reach
calibration, and select the same relative side along the ordered path. This
is deliberately stricter than merely equal scalar distances from unrelated
requests. Separate local spaces would need a contact/calibration correspondence
before applying this contract. Independently equal-looking corners do not weld.

Parallel or collinear supports, incomplete geometry, inconsistent sides and
foreign parameter carriers remain unresolved. Parameter windows limit extension
in each edge's own calibrated frame; they are not a conventional dimensionless
miter-length limit. Caps, bevel/round policies, whole-path self-intersections,
curve-normal evolution and region topology remain separate work.

## Syntax, execution and verification

The parser gains no tokens or geometry opcodes. These fixtures use ordinary
groups, ordered relations, native spans, unit shapes and open result slots.
The host selects a reader and passes actual results into later requests.
That selection still needs an explicit execution contract in a future compiler;
it is not currently inferred from variable names or compiled from a demo.

The [tests](../../Gojo.Encoding.Tests/GojoSyntaxMetricConstructionTests.cs) run
before and after numeric restoration, compare exact results with independent
integer identities, exercise a skew chart and opposite guide, reject false
identity/calibration reads, and preserve bounded failures and captured history.
Existing circle and rotation tests continue to run against the shared metric.

```powershell
dotnet test Gojo.Encoding.Tests/Gojo.Encoding.Tests.csproj --filter 'FullyQualifiedName~GojoSyntaxMetricConstructionTests|FullyQualifiedName~GojoSyntaxCircleConstructionTests|FullyQualifiedName~GojoSyntaxRotationTests'
dotnet test Gojo.slnx
```

At this checkpoint the full solution passes 1,557 tests: 782 kernel, 599
encoding, 155 CAD and 21 gear-ladder cases. Sixty dedicated metric construction
cases cover this addition. Browser checks cover all eight walkthrough stages
on desktop and phone, both view directions, the support toggle, syntax panels,
local links and direct step URLs. Representative layouts were visually inspected.

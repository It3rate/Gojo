# Rotation with supplied radial coordinates

Status: implemented exploratory selected contract, September 2026. This develops the
[contour-grounded plan](GojoContourRotationAndLocalInterfaces-2026-09.md).

[Open the nine-step dial and plate example](examples/calibrated-contour-rotation.html).

This is a later turning fixture: it **supplies** an X/Y basis, metric and
radial landmarks. It did not establish how the circle or those landmarks came
into being. Start with [circle construction before coordinates](GojoCircleConstructionFixture.md)
and its [revised first demo](examples/contour-rotation.html) for that earlier work.
The two fixtures do not yet have an executable intrinsic-to-Cartesian bridge.

The first experiment uses a finite ordered set of marked radial attachments
on a planar constant-reach contour. A turn selects a walk between markers.
Its geometric action is derived from the source and destination radii under
an explicitly supplied positive-definite local metric. No degrees, radians,
trigonometric constants or authored rotation matrix define that action.

## Structural contract

The notation below describes ordered Gojo structure. The executable authored
example uses the existing `gojo 1` constructors.

```text
basis = group_on(space, x unit span, y unit span)
metric = group(basis, group(
    (x unit span -> x unit span) -> xx,
    (x unit span -> y unit span) -> xy,
    (y unit span -> y unit span) -> yy))
surface = group(origin point -> exposed centre, circular carrier, metric,
                group(marker -> (centre -> rim), ...),
                group(marker -> next marker, ..., last -> first))
traverse request = group(surface, start marker, signed step span)
traverse result = group(start marker, finish marker, copied step span,
                        signed crossings)
turn result = group(metric, centre -> first rim, centre -> last rim)
apply request = group(traversal witness, exposed ordered points)
```

The [authored fixture](../../Tests/Gojo.Encoding.Tests/Fixtures/Syntax/contour-rotation.gojo)
contains these structures using existing `gojo 1` constructors. Its six roots
expose the base surface, a one-edge request, an ordered point frame, a refined
surface, a partial surface and an application template. The reader's name is
not stored as an operation tag, and identifiers can be renamed without changing
the read. No grammar change was needed.

The metric pairings are squared-length participation on one compatible basis.
The selected symmetric bilinear read is
`<u,v> = xx*ux*vx + xy*(ux*vy + uy*vx) + yy*uy*vy`.
It requires `xx > 0` and `xx*yy - xy*xy > 0`. Zero cross participation supplies
perpendicular coordinate directions in this selected metric; a generic
orthogonal outlet does not do so. This is a declared metric, not discovery of
geometry from unconstrained tension.

Topology requires a closed ordered cycle with actual shared marker sites.
Equal coordinates cannot substitute for a missing connection. Traversal counts
edges, not angular distance: unevenly spaced markers remain legal. A geometric
turn additionally requires concrete, nonzero, equal metric radii and a simple
convex cyclic ordering. Open geometry can therefore retain a successful
topological walk while the stronger geometric result remains open.

## Turning without an angle parameter

Let `a` and `b` be the source and destination displacements from the centre.
The metric establishes `<a,a> = <b,b> > 0`. Reflecting a displacement `v`
along the source radius gives `w = 2*a*<a,v>/<a,a> - v`. Reflecting `w`
across the normal `a-b` then gives
`w - 2*(a-b)*<a-b,w>/<a-b,a-b>`. Together these preserve the metric, preserve
planar orientation and carry `a` to `b`. When `a=b`, the endpoint transform is
identity; any completed traversal remains recorded separately.

The reflections are a derived evaluation of the connected radial relation.
They keep their intermediate displacements in witnesses. An attached point is
read relative to the centre, transformed, and reattached to that centre. The
helper supplies no universal angular unit. A Cartesian or angular export can
be added later as a calibrated read.

The first contract is discrete: it evaluates authored contour landmarks. It
does not interpolate a continuous turn between them, discover a metric, infer
equal angular sectors, model torque, or provide general three-dimensional
rotation. These remain subsequent constructions.

## A concrete turn

Imagine a dial with a plate attached to its pointer. In the authored local
plane the centre is `(10,20)` and four radial endpoints are `(15,20)`,
`(10,25)`, `(5,20)` and `(10,15)`. All have reach five under the selected metric.
Each has a marker on the contour. The ordered cycle connects those markers,
including the final connection back to the first.

The plate's ordered corners are `(12,20)`, `(12,21)`, `(13,21)` and `(13,20)`.
Following the first edge carries the first radius to the second. The plate's
corners consequently become `(10,22)`, `(9,22)`, `(9,23)` and `(10,23)`.
Their pairwise distances and handedness survive the turn.

Adding a landmark at `(13,24)` introduces the radial displacement `(3,4)`.
It has the same reach of five. One edge in this refined cycle sends the first
plate corner to exactly `(56/5,108/5)`. Two refined edges reach the earlier
endpoint `(10,25)` and reproduce the original turn. The new path has five
edges, but their geometric sizes are unequal. Tick labels `0,2,4,8,12` identify
sites; neither their values nor their differences are angular units.

If that next radial endpoint is `?missing_rim`, the cycle can still say which
marker comes next. The geometric read retains an open result. Binding the
missing attachment permits a new geometric read while the earlier witness
continues to record its unresolved state. In this first contract, **every**
radial landmark must be concrete before the whole contour's stronger geometric
claim succeeds. A later read may justify turning over a sufficiently known
subsurface; the current reader does not infer it automatically.

## Procedures and evidence

[ContourRotation](../Structures/ContourRotation.cs) implements three selected
reads. Each returns the ordinary five-part witness: selected source, read
surface, result, remainder and provenance.

| Procedure | Result | Retained evidence |
| --- | --- | --- |
| `TryTraverse` | Start marker, end marker, signed step span, signed closing-edge crossings | Captured surface and request; shared marker sites and all authored edges |
| `TryTurn` | Metric, first radial relation, final radial relation | Successful traversal and exact squared reach |
| `TryApply` | New ordered point frame, possibly partial | Geometric witness, source-to-result relations and per-point reflection evidence |

The native circular origin is linked to the exposed centre through the
`$centre_representation` relation, formerly named `$anchor`.
The reader checks that the anchor's source carrier and tick match the frozen
circular-origin promise. This is an explicit calibration relation, not a claim
that two independently exposed coordinate-equal points are the same site.
Within the cycle, edges must use the actual shared marker ticks. Radial
relations must share the actual exposed centre relation.

The metric connects two distinct compatible length axes through their `0..1`
unit spans. Its pair operands reference those same spans. Coefficients live on
one compatible squared-length carrier. For example, pairings `1,1,2` describe
a valid nonperpendicular basis; the rotation preserves that metric rather than
assuming the displayed coordinate axes are perpendicular.

Traversal counts signed edges, including any number of complete circuits. A
crossing is measured at the cycle's authored closing edge, relative to its
first marker. A one-edge backward walk from that first marker records `-1`;
this does not mean it travelled a whole circuit. Start, endpoint, step count,
cycle structure and crossings together retain the route. A four-edge circuit
and a zero-edge walk have the same endpoint action but different witnesses.

`TryApply` is a construction read. A caller explicitly adopts the resulting
point frame and can put before, application witness, after and remainder into
a `TimeStepSequence.Step`. Four such steps are tested as a connected timeline
before and after numeric restoration. This does not model forces or material
settlement; externally driven material change must still enter through tension.
The fixture transforms only the exposed ordered points, not hidden CAD internals.

Repeated whole-point references remain shared in the result. Independent
coordinate-equal points stay distinct. Derived coordinate sites are new;
the reader does not promise that an input coordinate alias survives as an
output coordinate alias, since a turned coordinate can depend on both inputs.
Captured per-point sources preserve shared input ticks for that dependency
evidence. An open input coordinate remains open wherever it contributes;
an output independent of that unknown can still be exact. For example, the
first turn takes `(?,21)` to `(9,?)`.

Captures copy exposed numeric locations and relation sharing while retaining
carrier references and their stable unit shapes. They do not traverse private
attachments or freeze an entire live local world. The selected-source field
can remain a backlink to the live request; historical evaluation uses the
captured surface and request. Re-reading a recorded traversal does not silently
pick up later edits to the original centre, rims or metric ticks.

## Refusal and current limits

An unresolved witness retains the request and an open result. It does not
declare which participant caused the conflict. Refused geometric claims
include unequal or zero radii, repeated radial poses, crossing landmark order,
an invalid metric, incompatible units and ungrounded coordinates. A valid
weaker traversal remains available even when the stronger read fails.

Calculations use exact rational intermediates. The current numeric encoding
still bounds stored numerators and denominators; an unencodable squared reach
or intermediate/output displacement remains unresolved instead of wrapping
or introducing a float.
The reader currently preserves the relevant request rather than producing a
fine-grained conflict diagnosis. Uncertainty in radial distance, metric or
pose needs a subsequent conditional interpretation; `approximately five`
must not become an invented hard tolerance or an exact radius.

This validates later discrete planar turning with supplied geometry. Its
earlier description as an implementation of R1–R4 overstated the construction
coverage: the source of the contour and its landmarks was still missing.
The native construction fixture now addresses those earlier stages. Remaining work
includes continuous contour parameterization, conditional turns under uncertain
geometry, native calibration between different resolutions and units, frame
transport between local spaces, and three-dimensional composition. A 3D turn
needs a selected plane/axis and orientation structure: one radius alone does
not determine how the remaining frame turns. Angle-unit exports, if added,
must read these constructions rather than define them.

## Verification

[GojoSyntaxRotationTests](../../Tests/Gojo.Encoding.Tests/GojoSyntaxRotationTests.cs)
contains 37 cases covering exact turns, nonperpendicular metrics, signed
traversal, refinement, partial inputs, refused claims, identity, history,
numeric restoration and range failures. The nine demo states are checked
against the actual readers after two numeric round trips, and its displayed
syntax is checked against the authored fixture. The demo draws those recorded
results; it does not run the Gojo kernel in JavaScript.

`dotnet test Gojo.slnx` passes 1,420 tests at this checkpoint: 782 kernel,
462 encoding, 155 CAD and 21 gear-ladder tests. These results establish this
selected contract, not the later continuous or physical rotation systems.

The standalone demo also passes browser checks at desktop and phone widths:
all nine steps, the syntax disclosure, local links, direct step URLs, keyboard
controls and page overflow. Representative complete and open states were
visually inspected.

# Gojo Structure Through Tension

This note captures the current speculative direction for how unresolved tension
may become new structure in Gojo. It is guidance, not an implementation
contract yet.

## 1. Core Principle

Tension is participation that existing structure cannot currently account for.
Shape is the local rule for where that tension may settle. Crystallization is
settlement hardened enough to become a stable unit or carrier surface.

```text
tension         unaccounted participation
shape           settlement possibility
crystallization hardened settlement
```

This keeps discovery numeric. A complex shape should not appear as a symbolic
tag. It should emerge through intermediate tension-bearing structures that can
be inspected, perturbed, and either stabilized or abandoned.

## 2. Orthogonal Means Unaccounted Here

Orthogonal should not mean only "ninety degrees." More generally:

```text
orthogonal = not accountable in this structure's native order
```

Examples:

```text
point tension              radial possibility
line-point escape          perpendicular possibility
whole-line escape          sheet/fill possibility
endpoint escape            ray/cone/branch possibility
enclosed-boundary tension  interior/exterior pressure
path tension               branch or alternate route
static-structure tension   growth or time-like change
```

The existing structure does not fully determine what must happen next, but it
does constrain the first honest directions of exploration.

## 3. Shape Is Guidance, Not Destiny

If tension escapes from a point with no other sink, a circular or spherical
settlement candidate is natural for the appropriate grade. If it finds another
point-sink, a line may harden instead. If it finds several sinks, a graph or
polygon may emerge. If it discovers a stable boundary, interior/exterior
structure may emerge.

So:

```text
point -> radial tension -> perimeter candidate -> interior/exterior
point -> radial tension -> external sink -> line/path
line  -> side tension   -> perpendicular line or sheet
line  -> endpoint escape -> ray, cone, or branch
```

The system can leap from simple structure to rich structure only by building
the intermediate model first. A point can eventually account for polygon-like
tension, but only after it has produced structure capable of expressing a
polygonal claim.

## 4. Unit Shape Versus Settlement Shape

Unit shape is the scaling grammar of a tick. Settlement shape is where tension
is allowed to harden. Order surface is how the hardened structure is traversed
or read.

```text
unit shape       scaling participation
settlement shape allowed hardening surface
order surface    read/traversal order
```

For example, `u^2` says two participations of `u` are involved. It does not by
itself force Cartesian area, circular area, shell area, or field spread.
However, absent other constraints, a point-like source of grade-two tension may
naturally explore a radial/circular settlement before discovering another
route.

Unit shape should not be confused with basis calibration, magnitude, or
partition. A unit shape may carry an opinion about settlement direction, but it
does not by itself declare that one tick must mean inch, meter, degree, lumen,
or any particular resolution. Those are calibrations placed on the unit shape.

```text
unit shape              unparameterized settlement grammar
calibration structure   endpoint, marker, or extent that makes measuring possible
partition map           how a calibrated extent or cycle is divided or grouped
```

A crystallized shape created from an inch-like structure may naturally default
to an inch calibration because that is the structure that hardened it. The same
unit shape can still be reused by a meter-like calibration if the meaning of
its calibrated extent is reinterpreted, and it can be given millimeter-like
resolution by a partition map. The shape is stable; its calibration and
resolution are local uses of that shape.

Unparameterized unit tension has no internal reason to prefer one compatible
direction, endpoint, or marker. Flow settlement and contour settlement are
symmetric in this sense. Each can support real relations before it is measured.
One unmeasured flow can be three times longer than another flow, just as one
circle can have three times the diameter of another circle. The ratio can be
true before either side has a local unit.

```text
flow settlement      ordered gradient/path possibility without a measured end
contour settlement   closed equal-tension possibility without a marked start
flow calibration     endpoint/basis marker that makes distance countable
contour calibration  marker/orientation that makes cycle countable
```

Both can be partitioned after calibration. A flow unit might be divided into
tenths, eighths, millimeters, or larger estimate ticks. A calibrated contour
unit might be divided into degrees, radians, encoder ticks, clock positions, or
any other resolution. The partition does not create the unit shape; it only
chooses how a local user walks the calibrated extent or contour.

Many calculations do not require calibrated endpoints or zero markers. Two
sticks can have a length ratio without declaring inches. Two circles can have a
diameter ratio, speed ratio, tooth ratio, or belt contact relation without
declaring where either circle's revolution starts. Those ratios can be real
settlement facts before any distance counter or rotation counter exists.

A calibrated endpoint or marker appears only when orthogonal tension hardens as
a distinguishable site. On a flow settlement, that may be an endpoint or tick
mark on the path. On a contour settlement, that may be a zero marker on the
contour. The contour marker may be created by a flow-settlement ray from the
center, by another contour event on the circumference, or by any other
structure that can touch the contour without being absorbed into its existing
traversal. The marker does not need to leave the circumference. It is
orthogonal because it is not merely more of the existing flow or contour
traversal; if it were aligned with the existing order, it would be absorbed
instead of becoming a distinguishable site.

The structural difference is where the uncalibrated origin lives. A flow
settlement may have an origin on the path but no measured endpoint. A contour
settlement may have a center/origin that created the contour but is not itself
on the contour, so it needs a second marker or fixed orientation before cycles
can be counted. After such a marker exists, a contour can have a relative zero,
partitioned cycle, and revolution counter. Before it exists, a circle does not
know where "east" is and does not even have an absolute rotation concept to
preserve or violate. Likewise, a complete structure has no universal true
horizontal; even physical horizontal is a local calibration against a local
field such as gravity.

## 5. Local Evolution

Tension should interact with the existing local structure before inventing a
new structure. This avoids magical jumps and keeps the search physically
interpretable.

```text
unresolved tension
-> local orthogonal candidate
-> settlement attempt
-> stable route or stable boundary
-> crystallized structure
```

Later code may need machinery for candidates, attempts, routes, and hardening,
but those should be relation/carrier structures where possible, not symbolic
case lists.

## 6. Flow, Gradient, And Contour Settlement

An existing structure usually presents a local flow or gradient. New tension
can attempt to settle relative to that gradient in two broad orientations, and
each orientation has two possible directions:

```text
aligned settlement       with or against the gradient/flow
contour settlement       one way or the other along equal tension
```

So "opposed" is not a third kind beside aligned and contour. It is the inverse
direction of aligned settlement. Likewise clockwise/counterclockwise, left/right
around a contour, or the two directions along an isoline are inverse directions
of contour settlement.

Contour settlement is not merely geometric tangent motion. It means following
sites of equal or near-equal tension instead of climbing or descending the
gradient. In a point-centered field this may become a circle. In a line field
it may become a parallel carrier. In an area or volume field it may become a
level curve or level surface.

This gives a small generative grammar:

```text
aligned settlement   with-flow / against-flow
contour settlement   contour-forward / contour-inverse
surface settlement   filled / empty region between two potentials
```

Surface settlement can occur in either orientation. Along a gradient, the
surface is the interval or material between two flow potentials, such as the
space between two endpoints of a segment. Along a contour, the surface is the
region between a source and a contour, or between two contours, such as the
space between a circle's center and perimeter or the shell between two parallel
offset carriers. Filled generally implies multiplicative structure, empty
implies addition.

Those surfaces can harden as filled or empty:

```text
segment endpoints      filled bar or empty interval
center and perimeter   disk/area or boundary-only circle
parallel contours      shell/buffer or only paired contour paths
```

Examples:

```text
point on carrier          contour tension trapped at a location
whole carrier side escape parallel contour carrier with buffer/shell between
segment boundary overflow aligned continuation or contour side-wall
closed boundary overflow  radial leak or perimeter traversal
field gradient            flowline or isoline/equipotential surface
```

The contour may remain only potential. An observed contour or equal-tension
read is not automatically a hardened path. Hardening requires repeated,
stable, or useful settlement.

This two-orientation/two-direction shape is a useful sanity check. If a model
seems to have three primitive settlement choices, it may be missing the dual or
inverse side of one of them.

## 7. Domain Analogies

Several physical domains are useful analogies, but they should not generate
the Gojo rules. Gojo should keep the common settlement grammar and let domain
specific structures decide conservation, dissipation, copying, and observation.

Electrical analogy:

```text
voltage gradient   tension slope
current            routed tension flow
resistor           controlled dissipation
capacitor          reservoir/storage
inductor           momentum / resistance to change
node               junction negotiation
equipotential      contour carrier
```

Heat analogy:

```text
temperature gradient   flow direction
isotherm               contour read or hardened contour
insulation             shell/buffer carrier
heat sink              attractor/sink
thermal mass           reservoir
```

Fluid/pressure analogy:

```text
pressure gradient      flow direction
pipe                   hardened route
tank                   reservoir
valve                  conditional boundary
vortex                 cyclic contour settlement
turbulence             unresolved branch/merge tension
```

Mechanics/field analogy:

```text
constraint force       orthogonal to allowed motion
rail/track             hardened path
collision              opposed tension
gravity potential      inward/outward gradient
orbit                  tangent flow plus radial constraint
equipotential surface  contour settlement/read
```

The shared Gojo layer is:

```text
gradient
contour
reservoir
route
boundary
junction
settlement
crystallization
```

The domain layer decides what is materially conserved, what is observed
without draining, what dissipates, and what may be copied.

## 8. Junction Shape Grammar

Simple junction-like glyphs can be read as compact settlement sketches. These
are candidate interpretations, not fixed symbolic tags.

```text
I  aligned continuation
T  contour settlement at an endpoint of flow settlement
Y  T where the branches become parallel, suggesting both paths participate
L  T with one available direction; contour turn without branch choice
O  stable closed contour
X  structure overlap with no relation, crossing, disconnect, or no correlation
H  bridge between parallel contours
V  split from point into two gradient/contour routes; connected version of =
=  parallel contour carriers with shell/buffer between
```

Alternative or complementary readings:

```text
T  branch negotiation from a blocked point
Y  graded split or merge
L  contour turn around blocked continuation
X  crossing, exchange, or coupled orthogonality depending relation strength
H  connector across a buffer
V  fanout or convergence
```

The important point is that these shapes can be described by combinations of:

```text
aligned with-flow
aligned against-flow
contour forward
contour inverse
buffer/shell gap
bridge/connector
split/merge
overlap without relation
closed contour
```

They should eventually be constructible as numeric relation/carrier
structures, not selected from a junction enum.

## 9. Conservation Versus Observation

Some fanout distributes material participation. Other fanout observes without
draining. A pressure gauge does not consume the pressure it reads in the same
way a drain consumes water. A temperature reading does not cut the measured
temperature in half when two readers observe it.

Gojo should keep this distinction structural:

```text
material route       drains/distributes participation
observation route    copies or reads participation without material drain
coupled route        partially drains and partially observes
```

This distinction matters for branches, sensors, ports, and live crystal
projections.

## 10. Circularity And Cycles

Circular and cyclical structures should not be collapsed too early into a
linear phase or Cartesian projection. A phase counter is useful, but it may
hide radius, boundary resolution, orthogonal tension, and the way endpoint
resolution grows with radius.

Possible circular settlement forms include:

```text
pinned origin + fixed radius + tangent/orthogonal tension
closed boundary order with fused endpoints
constant curvature path
radial tension leaking into boundary flow
origin reservoir growing into perimeter capacity
```

These are different settlement claims. Degrees, radians, and Cartesian
sine/cosine reads are likely adapters over deeper circular structure, not the
only possible circular primitive.

The current geometric read is that multiplication-like shape formation is an
orthogonal push. The source structure determines what "orthogonal" means:

```text
point orthogonal         unconstrained level-line possibility
zero-segment orthogonal  ray/line seed constrained by a collapsed basis
segment orthogonal       sheet/rectangle possibility
closed-boundary normal   interior/exterior or shell possibility
```

A point and a zero-length segment can occupy the same coordinate while exposing
different orthogonals. A point gives a place but no orientation constraint, so
its unconstrained orthogonal naturally supports circular/level-line structure.
A zero-length segment gives two coincident points in a relation; it is still a
collapsed basis, so its orthogonal supports a ray or line seed rather than an
unoriented circle. This keeps the distinction structural instead of adding a
mode flag.

The symmetry is:

```text
line/ray     source point + start marker on path + endpoint/calibrator
circle/arc   source point + start marker on contour + endpoint/calibrator
```

For a line or ray, the start marker often coincides with the source. For a
circle, the source is not on the contour; the start and end marker may coincide
on the contour for a closed cycle. An arc is the same contour structure with
distinct start and end markers. A wrapped arc lets the end pass the start with
residue or cycle count.

Calibration is separate from shape formation. A provisional notation is:

```text
shape | calibrator
```

read as "shape through calibrator" or "the right side calibrates the left
side." For example, an uncalibrated segment orthogonal can become square-like
when calibrated by the source segment, while a point contour can become
radius-calibrated when read through a ray from its source. This order matches
the current Gojo habit that the right side is the target/read/calibration
surface.

Contour orientation is still open. A closed contour can have marker and
partition calibration without knowing which traversal is positive in an
external frame. A nearby next-marker is one possible encoding, but it assumes
extra resolution that may not exist. Other possibilities include calibration
against another oriented contour, a segment/ray handedness relation, or a
right-hand-like rule emerging from coupled structure. For now, Gojo should not
pretend this is solved.

In the gradient/contour grammar:

```text
radius      gradient carrier of a point-centered field
circle      contour carrier at stable equal radius
disk/area   filled settlement between origin and contour
empty disk  boundary-only relation between origin and contour
angle       contour traversal read anchored by radius
orbit       tangent/contour flow plus radial constraint
```

This makes circle more than a wrapped line. It is a hardened contour of a
field-like settlement around an origin.

The first executable foothold for this is a partitioned level-line cycle. It
does not require a calibrated radius:

```text
cycle carrier C
closure       P(C,n) -> P(C,0)
```

This says the same contour marker is read at the start and end of a partitioned
walk. The marker is not a radius and does not declare how far the contour is
from the source point. It only makes the contour countable in `n` local parts.
The same relation can be used as modulo, phase residue, or crossing counter
input. If no counter receives crossed cycles, the crossed participation remains
visible as carrier tension. If a counter carrier is attached, the same
structure counts wraparounds.

This is intentionally parallel to a line segment:

```text
line calibration      origin + endpoint + partition
contour calibration   marker as start/end + partition
```

The structural difference is that the line's calibrated origin lies on the
line, while the contour's generating origin lies elsewhere. A later ray from
that origin can calibrate the marker against the surrounding structure, but
many uses do not need that. Gear teeth, clock positions, phase residues, and
relative cycle ratios can work on the partitioned contour alone.

## 11. Current Code Implications

The current executable Gojo core is intentionally narrower than this note. It
mostly implements aligned line settlement:

```text
Carrier.Value/Tension       local reservoir and unresolved signed tension
Relation.Apply             line boundary accounting and tension routing
Relation.FlowOn            directed linear read in a carrier frame
Relation.BoundariesOn      sorted linear boundary read
Relation.SpanOn            unsigned linear span read
UnitShape                  stable relation-shaped scaling surface
TryCrystallize             hardens the calculated unit shape onto a new carrier
```

This is a good foothold, but it should not be mistaken for the whole tension
grammar. `FlowOn`, `SpanOn`, and `BoundariesOn` are line-frame reads. They do
not yet express contour traversal, contour zero calibration, shell surfaces, or
candidate settlement paths.

`UnitShape` currently records compatible additive/product participation. It
does not yet record whether a shape is being used as uncalibrated flow
settlement, unmarked contour settlement, or calibrated measurement/counting
structure. That distinction should enter as numeric structure, not as an enum.
A calibrated flow unit can remain relation-shaped as a basis segment. A contour
unit needs a closed/fused contour surface; zero, partition, and revolution
count are later calibration structures attached by orthogonal markers.

The code now has a relation-readable unit-shape-level orthogonal outlet:
`source -> S(Unknown,0,0)`. `UnitShape.OrthogonalOf(element)` emits that
surface, and `Relation.TryReadUnitShape(...)` recognizes it before ordinary
zero-extent port reads. That is enough to crystallize a carrier whose tick
shape remembers that it came from a point, segment, or zero-length segment
orthogonal. `UnitShape.TryReadOrthogonalSource(out source)` exposes the source
as a snapshot. Today, product operations still multiply concrete port
operands; they do not yet execute `P*+`, `S*+`, or `S(C,a,a)*+` as settlement
without manually creating the receiving carrier/surface first.

The code names the first three supported orthogonal pushes:

```text
CircularFrom(point)        level-line/perimeter carrier
LinearFrom(zero segment)   ray/line carrier
SheetFrom(segment)         sheet/rectangle-like carrier
```

These names are only API clarity around relation shape. The perimeter carrier
for a gear is still an ordinary carrier after crystallization: tooth count is
ordinary partitioning on that carrier, while direction and mesh ratio are
relations to another perimeter carrier. A circular carrier does not yet claim a
global clockwise zero, radius, tooth geometry, or hardening-from-tension rule.

`Carrier.IsInverse` is currently an opposed-flow face for aligned carrier
settlement. It is not a general contour inverse. A contour inverse should be
represented by the reversed traversal of a contour surface, just as ordinary
relation reversal represents opposite traversal in the existing line model.

Likely near-term adjustments:

```text
1. Keep Carrier.Value/Tension as the one-shot settlement foothold.
2. Keep UnitShape stable and read-only as the crystallized tick promise.
3. Rename or document linear reads as linear-frame reads if ambiguity grows.
4. Extend uncalibrated flow and contour unit-shape examples from
   `source -> S(Unknown,0,0)` toward executable settlement.
5. Represent flow endpoints and contour zero as orthogonal calibration marker
   relations, not as absolute fields on the shape.
6. Add settlement-candidate reads only when there is a concrete operation that
   needs them, such as circular traversal, min/max, or hardening from tension.
7. Keep the orthogonal outlet convention relation-shaped as it moves from
   provenance into executable uncalibrated shape-unit settlement.
```

The important compatibility rule is that crystallization hardens unit shape,
not magnitude or partition. A crystallized carrier may hold mutable value and
tension, but its unit shape should not remain live-bound to the structure that
created it.

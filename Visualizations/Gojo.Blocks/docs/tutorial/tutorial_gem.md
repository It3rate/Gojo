
# Gojo Interactive Tutorial

This document defines an interactive tutorial page system built to explain Gojo's physical math. The target is an interactive sandbox where every concept is physically necessary and manipulatable by the user.

The visual layout relies on GojoCAD and GojoDraw layers to render points, segments, contours, fills, carrier rails, partition ticks, flow overlays, tension glows, residue markers, and correspondence lines. The renderer exposes meaning; the underlying Gojo structure carries it.

## Page Contract

Every tutorial page adheres to the following required structure.

```text
Page Title
  teaching goal
  guided animation
  interactive controls
  default Gojo structure
  drawable surfaces
  narration
  AI query context
  completion cue

```

## First Build Cut

The initial application will focus on a subset of pages that establish the core shape of the system without requiring full concept-surface UI implementations.

### Page 01 - Physical Structure, Not Scalars

Teaching goal
Gojo starts from inspectable structure, not from detached numbers and operator symbols.

Guided animation
A pulse enters a small network of rails, points, and boundaries. Part of the pulse settles into a channel. The rest remains as pressure at a boundary.

Interactive controls

* Inject pulse amount
* Toggle "show structure" to reveal carriers, locations, and relations
* Toggle "show rendered view" to return to the clean diagram

Default Gojo structure
`source carrier`, `channel carrier`, `boundary relation`, `result location`, `tension location`

Drawable surfaces
Carrier rails, point pins, relation arrows, and a pressure glow.

Narration
Most math libraries start with numbers and operations. Gojo starts with participation in a local structure. A value can settle, route, or remain unresolved. The unresolved part stays visible because it may be the next piece of structure.

AI query context
Load `Carrier`, `CarrierLocation`, `Relation`, and `ReadWitness` terminology.

Completion cue
User injects a pulse that leaves both settled value and visible tension.

### Page 02 - Resolution Comes Before Precision

Teaching goal
Every read exposes a smallest distinction. Finer detail is not secretly known unless another structure exposes it.

Guided animation
The same span is read through a coarse basis and a fine basis. The coarse read leaves more residue.

Interactive controls

* Resolution slider from 2 to 32 divisions
* Drag source marker along the span
* Toggle "show residue"

Default Gojo structure
`source = CarrierLocation(C,n)`
`coarse basis = Segment(C,0,4)`
`fine basis = Segment(C,0,16)`
`read witness = source -> basis -> result + residue`

Drawable surfaces
One segment, tick partitions, movable marker, residue chip.

Narration
Resolution is not a display setting for a hidden exact value. It is the distinction made available by the current read surface. A low-resolution read is still a valid read.

AI query context
Explain `Segment(C,a,b)` as a basis/read surface and distinguish residue from rounding error.

Completion cue
User changes resolution and sees the same source leave different residue.

### Page 03 - Tension Is Unresolved Participation

Teaching goal
Unspecified, incompatible, blocked, or below-resolution input remains as tension or residue.

Guided animation
Seven ticks enter a structure that can account for five. Five settle. Two remain visible.

Interactive controls

* Input amount stepper
* Capacity slider
* "Add route" button that creates a second carrier able to receive overflow

Default Gojo structure
`Carrier.Value`, `Carrier.Tension`, `capacity surface`, `optional route relation`

Drawable surfaces
Rail with capacity boundary, accepted ticks, tension glow, optional overflow route.

Narration
Gojo does not need to hide mismatch behind exceptions. The mismatch is structural information. If a later route appears, the tension can move.

AI query context
Load `Carrier.Value`, `Carrier.Tension`, settlement, and residue-preserving transfer.

Completion cue
User adds a route and watches previously unresolved tension become routed.

### Page 04 - Site / Point and Emitted Flow

Teaching goal
A point is already relation structure. Tension at a site can emanate flow or seek an orthogonal outlet.

Guided animation
A dot appears without axes. A carrier rail fades in, and the relation from the location to the carrier appears. Tension then enters the point and radiates outward.

Interactive controls

* Drag the point along the rail
* Outward/inward flow toggle
* Environment grade selector for line, plane, or volume projection

Default Gojo structure
`Point(C,n) = L(C,n) -> C`
`orthogonal outlet candidate`, `flow face evidence`

Drawable surfaces
Point pin, carrier rail, relation arrow, pressure rings, unresolved glow.

Narration
A point is not a coordinate bag; it is a small connected structure grounded to its carrier. When tension enters it, flow emanates. This flow is a tendency away from or toward the site, shaped by the available environment.

AI query context
Load `Relation.Point(carrier,ticks)` and explain `Relation.OrthogonalOutlet(source)` as "unaccounted here", not merely geometric perpendicular.

Completion cue
User switches the environment grade and observes the source relation emit different readable outlet shapes.

### Page 05 - Second Point: Direction Or Extent

Teaching goal
A second point can define direction or mark radial extent.

Guided animation
A second point appears. The page splits into two live reads: an ordered segment and a radius/extent marker.

Interactive controls

* Drag the second point
* Toggle "direction read" and "extent read"
* Lock first point as center

Default Gojo structure
`direction read = P0 -> P1`
`extent read = center point + radius marker`

Drawable surfaces
Two points, directed segment, ring/halo, radius marker.

Narration
The same two points can participate in different reads. Ordered, they create a linear frame. As extent, they calibrate a contour without deciding traversal.

AI query context
Explain relation-shaped reads over the same visible evidence.

Completion cue
User drags the second point and sees both reads update at once.
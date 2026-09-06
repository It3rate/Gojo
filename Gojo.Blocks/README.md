# Gojo Blocks Bootstrap

Status: teaching sequence and experimental inventory, not a conformance
specification. Some glossary specimens contain illustrative backing geometry
and open placeholders without the complete operation, claim, or witness named
by their caption. Animation cues can also live in the presentation timeline.
Use the [numeric milestone review](../Gojo/docs/GojoNumericMilestoneReview-2026-09.md)
for the current distinction between executable mechanisms and teaching intent.

Gojo Blocks is a visual workbench for explaining and experimenting with Gojo's
physical math. The goal is not to draw decorative diagrams beside the model.
The goal is to show the actual kinds of Gojo structure that make a concept
readable: carriers, locations, relations, flow faces, boundary evidence, unit
shapes, residue, tension, and ladder rungs.

This document is a first teaching sequence for the Blocks project. It starts
from the physical bootstrapping ideas that make Gojo feel different from an
ordinary symbolic math system, then grows toward numbers, dimensions, flow,
settlement, and complex structures.

## Teaching Spine

```text
resolution
  -> tension
  -> duality
  -> site / point
  -> emitted flow
  -> second point
  -> line or radial extent
  -> boundary and flow
  -> orthogonal escape
  -> orientation
  -> partition
  -> tick
  -> unit shape
  -> calibration
  -> crystallization
  -> number
  -> higher-grade structure
  -> settlement
  -> lanes
  -> branch / merge
  -> ladders
  -> equations
  -> concepts
```

The early sections should feel physical before they feel formal. A viewer
should be able to see why later Gojo structure is inevitable instead of being
asked to accept names such as "carrier", "relation", or "unit shape" too soon.

## Bootstrap

### 1. Structure Is Physical

Gojo is trying to represent math as physically inspectable structure. A value is
not only a scalar. It is a participation in a local world of places, flow,
boundaries, unresolved pressure, and stable units.

Visual:

- Draw a quiet field with a few visible marks and channels.
- Add pressure or heat-like color where something is unresolved.
- The Gojo structure being suggested is not a class named "field"; it is a
  local connected surface of carriers, locations, and relations that can be
  read.

Animation:

- Let a small pulse enter the field and either settle into a channel or remain
  as visible pressure.

### 2. Every Read Has Resolution

Every observation has a smallest distinction it can expose. Anything below that
resolution is not secretly known by the read.

Visual:

- Show the same span under a coarse grid, then under a finer grid.
- The coarse read should expose fewer ticks and leave finer detail unclaimed.
- A Gojo structure for this is a carrier or port read through a resolution
  surface, such as a ladder level or a segment basis.

Animation:

- Sweep a magnifier over the span. The displayed ticks change only when the
  selected resolution changes.

### 3. Tension Is Unresolved Participation

Anything below resolution, unspecified, incompatible, blocked, or not yet
settled remains as tension or residue. It is information, not failure.

Visual:

- Draw a pressure glow at the part of a structure that does not fit the current
  read.
- Show accepted value and leftover residue as separate visible pieces.
- The Gojo structure is a read witness: selected source, read surface, result,
  residue or tension, and provenance.

Animation:

- Let unresolved pressure try several local paths. If no path accounts for it,
  leave it visible instead of hiding it.

### 4. Everything Has A Dual

Every participation can be read with an opposite, inverse, return, or opposed
face. This is broader than ordinary negative number. Negative location,
opposed-flow face, reverse frame, inward radial flow, and counterclockwise
traversal are different structural reads.

Visual:

- Show paired faces of one channel.
- On a line, show forward and backward reads.
- On a point source, show outward and inward radial reads.
- On a contour, show clockwise and counterclockwise reads.

Gojo structure:

- A signed `CarrierLocation` such as `L(C,-5)` is a location on the same carrier.
- An inverse carrier face such as `C*` is opposed-flow participation.
- A reversed segment is an ordered frame read, not a new object by itself.

Animation:

- Flip a read direction while the underlying boundary stays still.

## Sites, Flow, And First Boundaries

### 5. A Point Is A Local Possibility

For teaching, start with "point" as a simple name for an uncalibrated site. It
has position-like presence, but not yet a full ruler, direction, or scale.

Visual:

- Draw one dot in an empty field.
- Do not draw axes yet.
- The Gojo structure is point-like relation shape: a carrier location grounded
  to its carrier, `L(C,n) -> C`.

Animation:

- Let the point become brighter when tension enters it.

### 6. A Point Can Emit Flow

A point with unresolved tension can emanate flow. The shape of that flow depends
on the grade and environment. At first this is unbounded: more "away from this
site" or "toward this site" than "a circle".

Visual:

- Draw arrows or pressure rings expanding from a point.
- In 1D, show left/right escape.
- In 2D, show radial spread.
- In 3D, show spherical spread or a projected sphere shell.

Gojo structure:

- The point is still the boundary/site evidence.
- The emitted possibility is orthogonal or unaccounted tension relative to the
  point's current local order.

Animation:

- Let the flow radiate until it finds a sink, boundary, or calibration marker.

### 7. Flow Depends On Grade

The same source can settle differently depending on the environment grade.

Visual:

- Three panels:
  - point on a line: left/right flow
  - point in a plane: radial or contour possibility
  - point in a volume: spherical or shell possibility

Gojo structure:

- The point source may be the same local relation shape.
- The surrounding carrier/unit-shape surface changes what settlement is
  available.

### 8. A Second Point Creates First Difference

A second point can introduce the first stable difference. It can do at least
two important things:

- define direction, which makes linear structure
- mark extent, which calibrates radial or contour structure without necessarily
  choosing traversal direction

Visual:

- Panel A: two points connected as `A -> B`, with an arrow.
- Panel B: center point plus second point used as radius marker, with a halo.

Gojo structure:

- Direction read: `Point(A) -> Point(B)` as an ordered segment.
- Extent read: the second point becomes boundary or calibration evidence for a
  radial or contour settlement.

Animation:

- Move the second point. In direction mode, the segment rotates or changes
  length. In extent mode, the contour grows or shrinks.

### 9. Linear Structure

Two ordered points form a segment or directed frame. Now "before", "after",
"toward", "away", and signed read direction become available.

Visual:

- Draw an interval with a visible start and end.
- Overlay a forward arrow and a reverse read arrow.

Gojo structure:

- `Segment(C,a,b) = Point(C,a) -> Point(C,b)`.
- Reversing the ordered frame changes the read without moving the endpoint
  locations.

Animation:

- Drag the endpoints and show the ordered frame update.
- Toggle read direction and show the same points read through a different
  orientation.

### 10. Radial And Contour Structure

A point plus an extent can create contour possibility: circle, sphere, or
higher-grade level surface. This is boundary-like, but it does not automatically
decide traversal direction.

Visual:

- Center point, extent marker, and ring.
- Keep circumference traversal arrows hidden at first.
- Then reveal clockwise/counterclockwise as separate flow along the boundary.

Gojo structure:

- A point orthogonal can be read as circular or spherical possibility.
- The extent marker is calibration evidence, not the same thing as traversal
  orientation.

Animation:

- Add a marker on the contour. Only after the marker appears can the contour
  show a relative origin and countable traversal.

### 11. Boundary And Flow Are Separate

A boundary says where a structure is addressable. Flow says how participation
moves, presses, routes, or opposes. One boundary can support many flow reads.

Visual:

- Draw one circle boundary.
- Toggle overlays:
  - no flow
  - interior fill
  - exterior pressure
  - inward radial flow
  - outward radial flow
  - clockwise circumference traversal
  - counterclockwise circumference traversal

Gojo structure:

- Boundary evidence is the point, segment, outline, rim, edge, or site.
- Flow evidence is a separate face, lane, route, or participation surface.

Animation:

- Keep the boundary stable while switching flow overlays.

### 12. Orthogonal Means Unaccounted Here

Orthogonal does not only mean "ninety degrees". In Gojo's physical intuition,
orthogonal means the current structure cannot account for the incoming
participation in its native order, so the participation seeks another order.

Visual:

- A point emits radial flow because the point cannot absorb directional spread.
- A line emits side flow because along-line order cannot account for side
  pressure.
- A closed boundary emits interior/exterior pressure because perimeter order
  cannot account for fill by itself.

Gojo structure:

- `Relation.OrthogonalOutlet(source)` is the general uncalibrated outlet.
- Named unit-shape reads can later identify circular, linear, or sheet-like
  outcomes when the source shape supports them.

Animation:

- Push tension into a structure. If along-structure settlement fails, show the
  pressure escaping into an orthogonal outlet.

### 13. Flow And Dimensions

A useful rule of thumb is:

```text
unsettled flow often seeks the next grade,
while settled structure can also carry flow along its own grade
```

That means a boundary may emit a higher-grade possibility, but a settled
boundary can also support traversal along itself.

Examples:

- 0D point in 1D: left/right line flow.
- 0D point in 2D: radial flow or circular contour possibility.
- 0D point in 3D: spherical or shell possibility.
- 1D segment in 2D: perpendicular line, side wall, sheet, or band possibility.
- 1D segment in 3D: orthogonal sweep can read as cylindrical surface or shell
  around the distant-point axis.
- touching or coincident points in 3D: with no stable distant axis, the
  orthogonal read can settle as a plane-like surface of possible directions.
- circumference: once settled as structure, it can carry clockwise and
  counterclockwise flow along its own grade.

Visual:

- Use a dimension ladder:
  - two points make a line
  - line in a plane emits perpendicular side structure
  - line in space emits cylinder-like side structure
  - coincident points in space expose plane-like direction surface

Animation:

- Move two points from coincident to separated. The visual can morph from
  plane-like orthogonal possibility into cylinder-like side possibility around
  the newly stable axis.

## Number And Unit Bootstrap

### 14. Positive And Negative Are Local Read Directions

Positive and negative should be introduced after direction, duality, and flow
are visible. They are not global signs first. They are local read directions.

Visual:

- Segment: forward is normal, backward is opposite.
- Radial source: outward is normal, inward is opposite.
- Contour: clockwise is normal, counterclockwise is opposite.

Gojo structure:

- Negative scalar location, inverse flow face, and reversed frame should be
  shown as separate toggles.

Animation:

- Switch the selected normal direction. The same physical structure remains,
  but the sign of a read changes.

### 15. Partition

A structure can be internally divided. Partition is the start of countable
resolution.

Visual:

- Segment divided into tick marks.
- Circle divided into cycle positions.
- Area divided into cells.
- Whole object divided into percent-like parts.

Gojo structure:

- A segment basis such as `S(C,0,10)` exposes ten countable distinctions.
- A closure such as `P(C,12) -> P(C,0)` can expose a cycle partition.

Animation:

- Increase and decrease partition count. Keep the underlying source structure
  stable while the read resolution changes.

### 16. Tick

A tick is one exposed distinction at the current resolution.

Visual:

- Highlight one ruler mark, one cycle step, or one cell.

Gojo structure:

- A tick belongs to a carrier or carrier-like surface. It is not a bare number.

Animation:

- Step tick by tick and show value as accumulated participation.

### 17. Unit Shape

A tick has shape. Unit shape says what kind of participation one tick scales
as: linear, circular, sheet-like, product-like, opposed, inverse, or composite.

Visual:

- Draw little stamps:
  - line stamp for linear unit
  - ring stamp for circular unit
  - square patch for area/product unit
  - paired opposite stamps for opposed faces

Gojo structure:

- A carrier's `UnitShape` is stable.
- Ordinary relations calculate exposed unit shape from their carrier sites,
  relation surface, and read context.

Animation:

- Reuse the same unit stamp on two carriers to show compatible unit shape
  without shared value state.

### 18. Calibration

Calibration compares one structure to another structure used as a ruler,
marker, or extent. It makes measurement possible without pretending the
structure was born numbered.

Visual:

- Unknown span laid beside a marked ruler.
- Circle with radius marker.
- Contour with an origin marker added to its circumference.

Gojo structure:

- Calibration is an explicit relation between source structure and measuring
  basis. It should not be hidden in metadata.

Animation:

- Slide or resize the calibrator and show the read result and residue change.

### 19. Crystallization

A stable structure can harden into a reusable unit. "This length", "this
contour step", or "this area patch" can become one tick of a new carrier.

Visual:

- Turn a measured shape into a stamp.
- Repeat the stamp along another structure.

Gojo structure:

- Crystallization hardens unit shape, not live value.
- The new carrier may share unit shape with others while keeping independent
  value and tension.

Animation:

- Freeze a shape into a unit stamp, then use it to count a new source.

### 20. Number

A number is a read through partition and unit structure. Internal partition can
create fractional or sub-tick reads. External comparison can create measured
counts. Residue remains visible when the read does not divide cleanly.

Visual:

- Show `3 ticks + residue`.
- Show a coarse read and a finer read side by side.
- Show a fraction as internal partition, not as a hidden decimal.

Gojo structure:

- Number is carried by carrier values, locations, ladder levels, unit shapes,
  and read residue.

Animation:

- Change the resolution and watch the same source read as whole ticks, then as
  sub-ticks, without inventing unsupported detail.

### 21. Higher Dimensions

Higher-dimensional structure should be shown as settlement and product of
structure, not only as coordinate tuples.

Visual:

- Segment swept sideways into a rectangle-like sheet.
- Circle filled into a disk.
- Surface shell enclosing a volume.
- Product stamps such as `length * length` becoming area-like unit shape.

Gojo structure:

- Orthogonal outlets, product unit shapes, fill bases, and boundary-flow reads
  build higher-grade structure.

Animation:

- Sweep a segment into a sheet.
- Expand a contour into a filled region.
- Leave gaps or mismatches as residue instead of snapping them away.

## Larger Gojo Structures

### 22. Settlement

Tension tries to settle through available structure. It may absorb, route,
wrap, clamp, branch, merge, or remain unresolved.

Visual:

- Channels with pressure entering.
- A boundary that wraps residue.
- A route that carries overflow into another carrier.

Gojo structure:

- Attached relations form the local settlement world.
- A relation like `Point(lower,10) -> Point(upper,0)` can route crossed tension
  into a higher level.

Animation:

- Feed ticks into the lower carrier and show overflow moving into the upper
  carrier while residue remains in the lower level.

### 23. Lanes

Participation can occupy different lanes: positive-forward, negative-forward,
positive-inverse, negative-inverse, no-flow, both-flow, or bounded whole
surfaces.

Visual:

- Road-like lanes over the same boundary.
- Occupied lanes colored separately.
- Opposed lanes meeting as visible tension instead of immediate cancellation.

Gojo structure:

- Flow face, boundary side, lane basis, occupied lane, transition, residue, and
  tension should remain inspectable.

Animation:

- Move a value from one lane to another only when a transition relation exists.

### 24. Support And Occupancy

Boolean-like ideas come later. A point, span, area, or volume is occupied only
relative to a selected boundary-flow basis and scope.

Visual:

- Partition cells showing occupied, blocked, uncovered, shared, and exclusive
  participation.
- Distinguish equal coordinate from shared occupied support.

Gojo structure:

- Support, overlap, union, intersection, difference, and complement are reads
  over occupied participation. They are not primitive booleans.

Animation:

- Drag two spans across each other and show partitioned support cells update.

### 25. Branch And Merge

Flow can split into several possible structures or merge from several sources.
Branching is routed participation, not a symbolic `if`.

Visual:

- One channel forking into several candidate routes.
- Several routes merging into a settlement surface.
- Show unresolved branch pressure when no route wins at the current resolution.

Gojo structure:

- Branch/fanout is an ordered relation surface over shared input.
- Merge is another relation-shaped read or operation surface.

Animation:

- Let tension choose a route at coarse resolution, then refine only when
  candidates remain close.

### 26. Ladders

Structures can exist at multiple resolutions. A lower rung remains valid when a
finer rung appears.

Visual:

- rectangle
- rounded rectangle
- thick frame
- ornate frame

Show correspondence:

- this corner
- this carried edge
- this replaced transition
- this residue

Gojo structure:

- Rungs are connected structures, not render layers.
- Focus, carry-forward, replacement, correspondence, result, residue, and
  provenance should be traversable.

Animation:

- Refine one corner at a time. Intermediate states remain valid and readable.

### 27. Equations

An equation is a relation-shaped read. It admits input, predicts output, and
exposes residue or error.

Visual:

- Input segment projected onto output segment.
- Prediction marker, observed marker, and visible gap.

Gojo structure:

- `Segment(input,a,b) -> Segment(output,c,d)` is an affine equation surface.
- The comparison surface records predicted output, observed output, residual,
  violation, and residue.

Animation:

- Move the input value and watch prediction and residual update continuously.

### 28. Concepts

A concept is a stable read over structure, boundary, flow, support, function,
material, residue, and history. It is not a label pasted onto data.

Visual:

- Show an object with:
  - boundary evidence
  - support flow
  - blocked or open passages
  - functional affordance arrows
  - unresolved residue
  - lower-resolution concept rung

Gojo structure:

- A concept surface should be a connected read over carriers, relations, unit
  shapes, flow lanes, support, focus, timeline, and residue.

Animation:

- Fade between low-resolution and high-resolution concept reads while keeping
  the same underlying structure visible.

## Visual Style For Blocks

Gojo Blocks should avoid making the drawing look like an external metaphor that
owns the meaning. The display should make the Gojo structure inspectable:

- carriers as rails, channels, contours, or reservoirs
- carrier locations as pins, tick marks, or occupied sites
- relations as ordered links, frames, routes, or structural bindings
- flow faces as directional overlays
- boundary evidence as endpoints, rims, outlines, cuts, and cells
- unit shapes as repeated stamps or tick molds
- tension as pressure, glow, spring compression, or unabsorbed pulse
- residue as leftover chips, gap marks, or carried remainder
- ladder rungs as complete structures with correspondence lines

Animations are most useful when they show a read or settlement becoming
inspectable:

- input tension entering a carrier
- unresolved pressure seeking an orthogonal outlet
- partition resolution changing
- a structure being calibrated by a ruler
- a stable shape crystallizing into a reusable unit
- overflow routing through a resolution ladder
- a focused structure refining into a higher rung

The display should not hide incomplete states. A partial, incompatible, or
low-resolution read is still a Gojo read.

# Gojo Blocks Bootstrap

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

## Assistant

# Gojo Blocks Bootstrap
## Screen-by-Screen Tutorial Screenplay
### Audio narration + animation script · programmer audience

**Format notes**
- Each **SCREEN** is one tutorial “page” (~35–55 seconds narration).
- **NARRATOR** = spoken transcript.
- **VISUAL / ANIMATION** = what the viewer sees.
- **ON SCREEN** = short labels or notation (optional).
- **GOJO** = structure mapping for devs (not spoken unless noted).
- **Total:** 32 screens · ~24–28 minutes at natural pace.

**Revised teaching spine** (fixes ordering: physical intuition → named primitives → complex reads)

```text
physical field → resolution → tension/residue → duality
→ site → flow → grade → segment vs contour
→ boundary ≠ flow → orthogonal → dimensions
→ partition → tick → unit shape → calibration → crystallization → number
→ settlement → lanes → support → branch/merge
→ ladders → equations → concepts
```

---

# ACT I — WHY THIS ISN’T SYMBOLIC MATH

---

### SCREEN 1 — Cold Open: Structure, Not Scalars

**Duration:** ~40s

**[NARRATOR]**  
Most math libraries give you numbers and operators. You call `add`, you get a result. Gojo starts somewhere else. A value is not only a scalar. It is participation in a local world — places, flow, boundaries, unresolved pressure, and stable units. Gojo Blocks is a workbench for *seeing* that world, not decorating it with diagrams. What you draw here is meant to be the same kind of structure the kernel can traverse.

**[VISUAL]**  
Dark field. No axes. A few faint channels and one soft pulse entering from off-screen.

**[ANIMATION]**  
Pulse travels a channel and dims — settled. Second pulse hits a dead end and stays visible as warm pressure at the edge.

**[ON SCREEN]**  
`participation ≠ scalar`

**[GOJO]**  
Meaning lives in connected carriers, locations, relations — not in helper class names.

---

### SCREEN 2 — The One Rule

**Duration:** ~45s

**[NARRATOR]**  
Here is the rule that governs everything else. If the visualization code disappeared, the structure on screen should still express the concept. Incomplete states are information, not errors. A low-resolution read is a valid read. What does not settle stays visible as tension or residue. Gojo Blocks never hides that on purpose.

**[VISUAL]**  
Split view: left = polished “finished” diagram (crossed out); right = same scene with pressure glow and gap chips still visible.

**[ANIMATION]**  
Crossfade from “clean UI lie” to “inspectable Gojo read.”

**[ON SCREEN]**  
`read → result + residue + provenance`

**[GOJO]**  
`ReadWitness`: source, read surface, result, residue/tension, provenance.

---

# ACT II — OBSERVATION BEFORE OBJECTS

---

### SCREEN 3 — Every Read Has Resolution

**Duration:** ~50s

**[NARRATOR]**  
Before we name carriers or relations, observe this: every read has a resolution — the smallest distinction it can expose. Anything finer is not secretly known. A coarse grid does not “contain” hidden sub-ticks. It simply has not claimed them. Resolution is not a quality setting on a float. It is structural: which distinctions the current read surface exposes.

**[VISUAL]**  
One horizontal span. Coarse overlay: 4 large cells. Fine overlay: 40 tick marks. Same underlying geometry.

**[ANIMATION]**  
Magnifier sweeps left to right. Tick labels appear only when fineness increases. Coarse read leaves inner detail gray — unclaimed, not zero.

**[ON SCREEN]**  
`coarse read ⊄ fine detail`

**[GOJO]**  
`Segment(C,0,4)` vs `Segment(C,0,40)` — same carrier, different basis/resolution.

---

### SCREEN 4 — Tension Is Unresolved Participation

**Duration:** ~50s

**[NARRATOR]**  
Whatever sits below resolution, or is unspecified, incompatible, or not yet routed, remains as **tension** or **residue**. That is not failure. It is the material the next read or settlement will use. Accepted value and leftover pressure are separate visible pieces. If nothing accounts for incoming change, do not snap it to zero — leave it glowing.

**[VISUAL]**  
Channel with 7 units of pressure entering a boundary that only accepts 5.

**[ANIMATION]**  
5 units settle into the channel. 2 units bounce, try alternate paths, fail, and remain as persistent glow.

**[ON SCREEN]**  
`settled: 5` · `tension: 2`

**[GOJO]**  
`Carrier.Value` + `Carrier.Tension`; read witness records both.

---

### SCREEN 5 — Everything Has a Dual (But Not All “Negatives” Are the Same)

**Duration:** ~55s

**[NARRATOR]**  
Every participation can be read through an opposed face — forward and backward, outward and inward, clockwise and counterclockwise. But in Gojo, “negative” is not one universal knob. A signed location on a carrier, an inverse flow face, and a reversed ordered frame are three different structural reads. Programmers: do not collapse them into a single minus sign in your head. The animation will flip reads while the underlying geometry stays fixed.

**[VISUAL]**  
Three panels: (A) segment with forward/back read arrows, (B) radial source with in/out, (C) contour with CW/CCW overlays.

**[ANIMATION]**  
Toggle each read direction independently. Geometry unchanged; numeric labels flip sign or face.

**[ON SCREEN]**  
`L(C,-5)` · `C*` inverse face · `reverse(segment)`

**[GOJO]**  
Signed `CarrierLocation` ≠ inverse carrier face ≠ reversed relation frame.

---

# ACT III — SITES, FLOW, FIRST BOUNDARIES

---

### SCREEN 6 — A Point Is a Local Possibility

**Duration:** ~40s

**[NARRATOR]**  
We can finally put something on the field. A **point**, for teaching, is an uncalibrated site: position-like presence without a ruler, without a chosen direction, without scale. No axes yet. Just a place where participation can arrive and wait.

**[VISUAL]**  
Single dot in empty field. No grid, no compass.

**[ANIMATION]**  
Dot brightens when a pulse enters; dims when pressure leaves.

**[ON SCREEN]**  
`Point(C,n) = L(C,n) → C`

**[GOJO]**  
Point-like relation: location grounded to carrier.

---

### SCREEN 7 — A Point Can Emit Flow

**Duration:** ~45s

**[NARRATOR]**  
A site with unresolved tension does not have to sit still. It can emanate flow — more “away from here” or “toward here” than “along a named circle.” At this stage the flow is possibility, not a finished geometric object.

**[VISUAL]**  
Dot with soft arrows or pressure rings expanding.

**[ANIMATION]**  
Flow radiates until it hits a sink, a boundary, or a calibration marker — or stays unresolved at the edge.

**[ON SCREEN]**  
`unaccounted participation → flow possibility`

**[GOJO]**  
Orthogonal / unaccounted tension relative to the point’s native order.

---

### SCREEN 8 — Flow Depends on Grade

**Duration:** ~50s

**[NARRATOR]**  
The same point settles differently depending on **grade** — the dimensionality of the environment it sits in. On a line, escape is left or right. In a plane, radial or contour possibility. In volume, spherical or shell possibility. The source relation can look similar; what changes is what settlement is *available*.

**[VISUAL]**  
Three panels side by side: 1D line, 2D plane, 3D volume around one source.

**[ANIMATION]**  
Same pulse into each panel; different escape geometry.

**[ON SCREEN]**  
`grade shapes settlement menu`

**[GOJO]**  
Surrounding carrier / unit-shape surface constrains settlement.

---

### SCREEN 9 — Second Point: Direction vs Extent

**Duration:** ~55s

**[NARRATOR]**  
A second point introduces the first stable *difference*. It can do two jobs. **Direction:** an ordered segment from A to B. **Extent:** a marker that calibrates radial or contour size without yet choosing how you traverse the boundary. Move the second point: in direction mode the segment rotates and stretches; in extent mode a ring grows or shrinks around the center.

**[VISUAL]**  
Panel A: `A → B` with arrow. Panel B: center + rim marker with halo ring.

**[ANIMATION]**  
Drag second point; split-screen shows both readings from one motion.

**[ON SCREEN]**  
`ordered: A → B` · `extent: radius evidence`

**[GOJO]**  
`Segment(C,a,b)` vs contour calibration marker — different reads, same dots.

---

### SCREEN 10 — Linear Structure

**Duration:** ~45s

**[NARRATOR]**  
Two ordered points form a **segment** — a directed frame. Now “before,” “after,” “toward,” and “away” mean something. Reversing the read flips orientation without moving the endpoints. Endpoints are locations; orientation is how you traverse the ordered relation.

**[VISUAL]**  
Interval with start/end pins. Forward arrow solid; reverse arrow dashed overlay.

**[ANIMATION]**  
Drag endpoints; toggle read direction on same geometry.

**[ON SCREEN]**  
`Segment(C,a,b) = Point(C,a) → Point(C,b)`

**[GOJO]**  
Ordered relation; reversal is frame read, not new endpoints.

---

### SCREEN 11 — Radial and Contour Structure

**Duration:** ~50s

**[NARRATOR]**  
A point plus extent gives **contour** possibility: ring, sphere, level surface. Boundary-like — but traversal direction is not automatic. Hide CW and CCW at first. Add a marker on the contour; only then can you count cycles and speak of a relative origin on the boundary.

**[VISUAL]**  
Center, extent marker, ring. No traversal arrows initially.

**[ANIMATION]**  
Marker appears on rim → partition ticks fade in → optional CW/CCW overlays.

**[ON SCREEN]**  
`contour ≠ traversal direction (yet)`

**[GOJO]**  
Point orthogonal → circular possibility; marker = calibration, not orientation field.

---

### SCREEN 12 — Boundary and Flow Are Separate

**Duration:** ~50s

**[NARRATOR]**  
A **boundary** says where structure is addressable. **Flow** says how participation moves, presses, routes, or opposes. One circle boundary can support many flow overlays: no flow, interior fill, exterior pressure, radial in, radial out, clockwise rim, counterclockwise rim. Keep the boundary stable; swap overlays.

**[VISUAL]**  
One circle. Toggle overlay chips along the bottom.

**[ANIMATION]**  
Same rim; six flow modes cycle without redrawing the boundary.

**[ON SCREEN]**  
`boundary evidence` · `flow evidence`

**[GOJO]**  
Boundary = site/outline; flow = face, lane, route, participation surface.

---

### SCREEN 13 — Orthogonal Means “Unaccounted Here”

**Duration:** ~55s

**[NARRATOR]**  
**Orthogonal** does not mean “ninety degrees” first. It means: this structure’s native order cannot account for incoming participation, so participation seeks another order. A point cannot absorb radial spread along a zero-dimensional order — so radial escape. A line cannot absorb side pressure along its axis — so sheet or side-wall possibility. A closed boundary cannot express fill by perimeter order alone — so interior/exterior pressure.

**[VISUAL]**  
Three mini-scenes: point → radial burst; segment → side bands; closed loop → inside/outside glow.

**[ANIMATION]**  
Push tension along native order; failure triggers orthogonal outlet (dashed escape cone).

**[ON SCREEN]**  
`orthogonal = not accountable in native order`

**[GOJO]**  
`Relation.OrthogonalOutlet(source)` → `source → Segment(Unknown,0,0)`

---

### SCREEN 14 — Flow and Dimensions (The Grade Ladder)

**Duration:** ~55s

**[NARRATOR]**  
Rule of thumb: unsettled flow often seeks the **next grade**, while settled structure can also carry flow **along its own grade**. Two points make a line. A line in a plane emits perpendicular structure. A line in space sweeps a cylinder-like shell. Coincident points in 3D — no stable axis — expose a plane of direction possibilities. Separate the points: the plane of possibilities collapses into axis-aligned side structure. Circumference, once settled, carries flow along itself.

**[VISUAL]**  
Dimension ladder: 0D→1D→2D→3D with morph between coincident and separated points.

**[ANIMATION]**  
Two points merge/split; orthogonal possibility morphs plane-like → cylinder-like.

**[ON SCREEN]**  
`unsettled → next grade` · `settled → along-grade flow`

**[GOJO]**  
`CircularFrom` / `LinearFrom` / `SheetFrom` as named orthogonal outcomes.

---

# ACT IV — NUMBER AND UNIT BOOTSTRAP

---

### SCREEN 15 — Positive and Negative as Local Read Directions

**Duration:** ~45s

**[NARRATOR]**  
Now — and not before — introduce sign. Positive and negative are **local read directions**, not global metaphysical poles. Forward along a segment; backward along the same segment. Outward from a radial source; inward. Clockwise on a contour; counterclockwise. Three toggles, three meanings.

**[VISUAL]**  
Segment, radial, contour — each with a “normal direction” chip.

**[ANIMATION]**  
Flip normal; labels change sign; geometry fixed.

**[ON SCREEN]**  
`sign ∈ read relation`

**[GOJO]**  
Do not equate `L(C,-5)`, `C*`, and `reverse(frame)`.

---

### SCREEN 16 — Partition

**Duration:** ~45s

**[NARRATOR]**  
Structure can be **internally divided**. Partition is countable resolution: tick marks on a segment, positions on a cycle, cells in an area, percent slices of a whole. Change partition count; the source structure stays the same — only the read surface changes.

**[VISUAL]**  
Segment with 4, then 10, then 100 ticks. Circle with 8 cycle positions.

**[ANIMATION]**  
Slider: partition count up/down; underlying span length constant.

**[ON SCREEN]**  
`S(C,0,10)` → ten distinctions

**[GOJO]**  
Segment basis exposes resolution; `P(C,n) → P(C,0)` exposes cycle closure.

---

### SCREEN 17 — Tick

**Duration:** ~35s

**[NARRATOR]**  
A **tick** is one exposed distinction at the current resolution. One ruler mark. One step on a cycle. One cell. A tick is not a bare integer floating in memory — it belongs to a carrier or carrier-like surface.

**[VISUAL]**  
Single tick highlighted on ruler; counter increments as you step.

**[ANIMATION]**  
Step tick-by-tick; value = accumulated participation, not injected magic number.

**[ON SCREEN]**  
`1 tick @ current resolution`

**[GOJO]**  
Ticks live on carriers; `CarrierLocation` is the inspectable site.

---

### SCREEN 18 — Unit Shape

**Duration:** ~50s

**[NARRATOR]**  
Each tick has **shape**. Unit shape says what kind of participation one tick scales as: linear, circular, sheet-like, product-like, opposed, composite. Think type-level grammar for measurement, not a string unit label. Two carriers can share unit shape without sharing value state — like two `int` variables with the same type.

**[VISUAL]**  
Stamp icons: line, ring, square patch, opposed pair.

**[ANIMATION]**  
Same line-stamp applied to two separate rails; values differ, stamp matches.

**[ON SCREEN]**  
`P(u,1)` · `P(u,1) → P(u,1)` (area-like)

**[GOJO]**  
`Carrier.UnitShape` stable after crystallization; relations *calculate* exposed shape.

---

### SCREEN 19 — Calibration

**Duration:** ~45s

**[NARRATOR]**  
**Calibration** compares structure to structure — a ruler, a radius marker, a zero on a contour. Measurement becomes possible without pretending the geometry was born numbered. Slide the calibrator; read result and residue both update.

**[VISUAL]**  
Unknown span beside marked ruler; circle with radius pin; contour with origin marker.

**[ANIMATION]**  
Resize calibrator; numeric read changes; mismatch → residue chips.

**[ON SCREEN]**  
`structure | calibrator → read`

**[GOJO]**  
Explicit relation between source and basis — not hidden metadata.

---

### SCREEN 20 — Crystallization

**Duration:** ~50s

**[NARRATOR]**  
Stable structure can **crystallize** into a reusable unit. “This length,” “this contour step,” “this area patch” becomes one tick of a new carrier. Crystallization hardens **unit shape**, not live value. The new carrier can hold fresh value and tension while remembering what one tick means.

**[VISUAL]**  
Measured span freezes into a stamp; stamp tiles along a new rail.

**[ANIMATION]**  
Freeze → stamp → repeat on different source; independent counters.

**[ON SCREEN]**  
`stable structure → 1 carrier tick`

**[GOJO]**  
`TryCrystallize`; provenance optional; unit shape is the promise.

---

### SCREEN 21 — Number

**Duration:** ~50s

**[NARRATOR]**  
A **number** is a read through partition and unit structure — not a primitive. Internal partition gives fractions and sub-ticks. External comparison gives measured counts. Coarse and fine reads of the same source sit side by side; residue stays when division is not clean. `3 ticks + residue` is a valid number read.

**[VISUAL]**  
`3 + gap` on coarse ruler; same source as `3.25` on fine ruler.

**[ANIMATION]**  
Resolution slider; number representation morphs without inventing unsupported digits.

**[ON SCREEN]**  
`number = read(carrier, basis, unitShape)`

**[GOJO]**  
Values, ladder levels, locations, residue — not a separate `Number` class that owns meaning.

---

### SCREEN 22 — Higher Dimensions

**Duration:** ~50s

**[NARRATOR]**  
Higher dimensions are **settlement and product of structure**, not `(x,y,z)` tuples pasted on top. Sweep a segment sideways → sheet. Fill a contour → disk. Product stamps `length × length` → area-like unit shape. Gaps and mismatches stay as residue — no silent snap-to-grid.

**[VISUAL]**  
Segment sweeps to rectangle; ring fills to disk; product stamp algebra.

**[ANIMATION]**  
Sweep/fill with deliberate gap; gap pulses as tension.

**[ON SCREEN]**  
`orthogonal push + product unit shape`

**[GOJO]**  
`SheetFrom(segment)`, fill bases, boundary-flow reads build grade-2 structure.

---

# ACT V — SETTLEMENT, LANES, TOPOLOGY

---

### SCREEN 23 — Settlement

**Duration:** ~50s

**[NARRATOR]**  
**Settlement** is what tension *does* when structure is available. Absorb locally. Route overflow. Wrap at a boundary. Clamp. Branch. Merge. Or remain unresolved. Carriers settle through **attached relations** — the local world, not a global solver.

**[VISUAL]**  
Two-level ladder: ones rail + tens rail. Pressure enters ones.

**[ANIMATION]**  
Ones fills to capacity; overflow routes via `Point(lower,10) → Point(upper,0)`; lower residue visible.

**[ON SCREEN]**  
`tension → route → value + remainder`

**[GOJO]**  
`Add`/`Remove`/`Set` feed tension; relations define settlement graph.

---

### SCREEN 24 — Lanes

**Duration:** ~45s

**[NARRATOR]**  
Participation occupies **lanes** — positive-forward, negative-forward, inverse faces, no-flow, both-flow, bounded whole. Same boundary, multiple legal traffic patterns. Values move lanes only when a transition relation exists; opposed lanes meeting can show tension instead of instant cancellation.

**[VISUAL]**  
Road-like lanes over one boundary; occupied lanes color-coded.

**[ANIMATION]**  
Token moves lane only across drawn transition bridge.

**[ON SCREEN]**  
`axis · face · lane basis`

**[GOJO]**  
`BasisReadSurface`: axis, face, lane, metric evidence, residue.

---

### SCREEN 25 — Support and Occupancy

**Duration:** ~45s

**[NARRATOR]**  
Boolean ideas arrive **late**. Occupied, empty, overlap, union, intersection — these are reads over **participation** relative to a boundary-flow basis and scope. Same coordinate is not automatically shared support. Two spans can sit at the same pixel and differ in occupancy evidence.

**[VISUAL]**  
Partitioned cells: occupied, blocked, shared, exclusive.

**[ANIMATION]**  
Drag two spans across each other; cell colors update locally.

**[ON SCREEN]**  
`support = read(participation, scope)`

**[GOJO]**  
Support/tension/exclusive reads = lower projections over flow structure.

---

### SCREEN 26 — Branch and Merge

**Duration:** ~45s

**[NARRATOR]**  
Flow **branches** and **merges** as routed participation — not `if (symbolic)`. One channel forks into candidate routes; several routes meet at a settlement surface. At coarse resolution one route may win; close candidates stay as branch tension until refined.

**[VISUAL]**  
Fork into three routes; merge node with combined pressure.

**[ANIMATION]**  
Coarse pick highlights winner; runners-up glow as unresolved branch tension.

**[ON SCREEN]**  
`fanout = relation surface` · `merge = settlement read`

**[GOJO]**  
Branch/merge as ordered relation surfaces, not control-flow enums.

---

# ACT VI — LADDERS, EQUATIONS, CONCEPTS

---

### SCREEN 27 — Ladders: Refinement Without Replacement

**Duration:** ~55s

**[NARRATOR]**  
**Ladders** hold multiple resolutions of the same idea. A lower rung stays valid when a finer rung appears. Rectangle → rounded rectangle → thick frame → ornate frame. Not render layers — **connected structure** with correspondence: this corner, this carried edge, this replaced transition, this residue. Refine one corner at a time; intermediate states remain readable.

**[VISUAL]**  
Four rungs stacked with correspondence lines: corner focus, carried edge, arc transition, gap chip.

**[ANIMATION]**  
Single corner rounds first; other three stay sharp; ladder timeline advances.

**[ON SCREEN]**  
`refinement ≠ replacement`

**[GOJO]**  
`FocusSurface` → `TransformSurface` → `StructuralLadderSurface` / rung records.

---

### SCREEN 28 — Focus and Transform (How Ladders Edit)

**Duration:** ~50s

**[NARRATOR]**  
Two mechanisms worth naming for implementers. **Focus** selects part of a source, carries the complement forward, replaces a participation, and records correspondence and residue. **Transform** is the reusable instruction; **application** is what actually settled on that focus. Replay the transform when upstream changes — old patches stay inspectable as history, not overwritten.

**[VISUAL]**  
Sharp corner: focus halo on two edge spans; arc replaces corner; patch witness panel.

**[ANIMATION]**  
Move upstream edge → stale patch ghost + replay pulse on same transform.

**[ON SCREEN]**  
`transform` vs `application` vs `patch witness`

**[GOJO]**  
`TransformApplication`, `Patch`, `Replay`, `StalePatch`, `PartialReplay`.

---

### SCREEN 29 — Equations

**Duration:** ~45s

**[NARRATOR]**  
An **equation** is a relation-shaped read. Input structure projects to predicted output; observation compares; gap is residue or violation — visible, not thrown as an exception. Move the input; prediction and residual update continuously. Same grammar as geometric refinement: witness, residue, timeline.

**[VISUAL]**  
`Segment(in,a,b) → Segment(out,c,d)` with prediction marker and observed marker.

**[ANIMATION]**  
Drag input; prediction slides; gap bar grows/shrinks.

**[ON SCREEN]**  
`predicted` · `observed` · `residual`

**[GOJO]**  
Affine equation surface + `RefinementRead` + `TimeStepSequence`.

---

### SCREEN 30 — Concepts

**Duration:** ~50s

**[NARRATOR]**  
A **concept** is a stable read over boundary, flow, support, function, material, residue, and history — not a label pasted on a mesh. “Chair” at low resolution might be boundary + support + affordance arrows. At high resolution: mesh detail, material residue, construction ladder. Fade between read levels; underlying structure stays connected.

**[VISUAL]**  
Object with boundary rim, support flow, blocked passage, affordance arrow, concept rung chip.

**[ANIMATION]**  
Crossfade L0 tag → L1 parameterized surface → L2 settled witnesses.

**[ON SCREEN]**  
`concept = read(structure, requested level)`

**[GOJO]**  
`ConceptSurface`: topology, focus, flow, constraints, settlement, timeline.

---

### SCREEN 31 — Visual Language Recap

**Duration:** ~40s

**[NARRATOR]**  
Gojo Blocks visual vocabulary — quick recap. Carriers: rails, channels, contours, reservoirs. Locations: pins and tick marks. Relations: ordered links and frames. Flow: directional overlays. Boundaries: rims and cuts. Unit shapes: stamps. Tension: pressure and glow. Residue: chips and gaps. Ladders: full rungs with correspondence lines. The drawing is the argument.

**[VISUAL]**  
Legend montage — each glyph animates once.

**[ANIMATION]**  
Fast cascade: pulse → outlet → partition → stamp → overflow → rung step.

**[ON SCREEN]**  
(style legend)

**[GOJO]**  
Display converts to/from Gojo structure at adapter boundary only.

---

### SCREEN 32 — Close: Inspect, Don’t Hide

**Duration:** ~45s

**[NARRATOR]**  
You now have the bootstrap path: resolution and tension before names; sites and flow before numbers; unit shape and crystallization before equations; settlement and lanes before booleans; ladders and witnesses before concepts. The workbench is for experiment. Change resolution, flip a read direction, block a route, refine one corner — and watch what remains unresolved. In Gojo, that remainder is not noise. It is the next rung.

**[VISUAL]**  
Pull back: full field with rectangle ladder, equation gap, concept overlay — all tension visible.

**[ANIMATION]**  
Slow zoom out; pressure glows pulse once in sync.

**[ON SCREEN]**  
`Gojo Blocks — structure you can traverse`

**[GOJO]**  
`dotnet test Gojo.slnx` — kernel tests lock the reads you just saw.

---

# PRODUCTION APPENDIX

## Pacing map

| Act | Screens | Theme |
|-----|---------|-------|
| I | 1–2 | Philosophy |
| II | 3–5 | Observation primitives |
| III | 6–14 | Physical geometry bootstrap |
| IV | 15–22 | Number & units |
| V | 23–26 | Settlement & topology |
| VI | 27–32 | Ladders, equations, concepts |

## Narration tone
- Second person sparingly (“you’ll see”) — mostly declarative.
- Pause ~0.5s after each ON SCREEN notation.
- Avoid reading `GOJO` blocks aloud; they are implementer captions.

## Animation principles (Blocks-specific)
1. **Never hide tension** for aesthetic cleanup.
2. **Geometry stable, read toggles** for duality/orientation screens.
3. **Resolution changes** alter tick overlays, not source span.
4. **Ladder screens** keep lower rung ghosted, not deleted.
5. **Settlement** always shows two outputs: absorbed + remainder.

## Optional interactive beats (app version)
- Screen 3: resolution slider
- Screen 5: three dual toggles
- Screen 16: partition count slider
- Screen 27: per-corner refine buttons
- Screen 29: input drag with live residual bar

## Suggested chapter markers (audio)
`0:00` Physical field · `2:30` Resolution & tension · `6:00` Sites & flow · `11:00` Numbers & units · `16:00` Settlement · `19:30` Ladders · `22:30` Equations & concepts · `25:30` Recap

---

*End of screenplay. Suitable for export to storyboard columns: Screen | Narration | Visual | Animation | On-screen | Duration.*
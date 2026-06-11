# Gojo Interactive Tutorial

This file synthesizes the current tutorial drafts into one app-oriented
tutorial plan. The target is not a cinematic video. The target is an
interactive sandbox where every concept becomes physically necessary and
manipulatable by the user:

```text
tutorial page
  -> animated explanation of one concept
  -> user can drag, toggle, scrub, or perturb the example
  -> AI can answer questions with the current Gojo code and page structure loaded
  -> user hits Next
```

The visuals should be drawable with Gojo elements and a modestly expanded
GojoCAD/GojoDraw layer: points, segments, contours, fills, carrier rails,
partition ticks, flow overlays, tension glows, residue markers, correspondence
lines, and simple projected 3D diagrams. Meaning must stay in Gojo structure;
the renderer only exposes it.

## Source Draft Review

### `tutorial_cur.md`

Pros:

- Strong conceptual bootstrap. It starts with physical representation,
  resolution, tension, and duality before introducing named Gojo primitives.
- The early "visual / animation / Gojo structure" outline is concise and easy
  to scan.
- The appended screenplay draft adds useful pacing, act breaks, and several
  strong lines such as "inspectable Gojo read" and "do not collapse negatives
  into one minus sign".
- It includes a small list of optional interactive beats, which points in the
  right direction for an app tutorial.

Cons:

- It is two drafts pasted together, so it repeats itself and is hard to treat
  as a build plan.
- The appended section has mojibake characters and cinematic language that
  does not fit an in-app tutorial.
- It is mostly passive narration. The user interactions are appended after the
  script instead of being designed into every page.
- Some visuals are atmospheric or stage-like rather than directly tied to
  GojoCAD elements and GojoDraw overlays.
- It does not consistently say what the AI assistant should know or expose on
  each page.

### `tutorial_cod.md`

Pros:

- Clean 30-screen structure with consistent fields: On Screen, Animation,
  Narration, and Gojo Structure.
- Better at attaching each idea to carriers, locations, relations, unit shapes,
  residue, tension, witnesses, and ladder rungs.
- Includes the important dimensional clarification: in 3D, distant points make
  an axis whose orthogonal can be cylinder-like, while touching/coincident
  points can expose plane-like direction surface.
- Ends with implementation notes and a useful reduced first-build subset.

Cons:

- Still reads like a linear video transcript. It does not center the user
  manipulating the structure after the animation explains it.
- Thirty screens is a little long for a first interactive tutorial. Several
  pages can be merged when the user can inspect and query locally.
- Some animation descriptions still feel like storyboard shots instead of
  deterministic drawable states.
- The page format lacks controls, interaction goals, query hooks, and drawable
  requirements for GojoDraw/GojoCAD.
- It does not distinguish enough between "guided animation" and "sandbox state"
  after the narration completes.

### `tutorial_gem.md`

Pros:

- Very clear app framing. It names the tutorial as an interactive sandbox where
  each concept must become physically necessary and manipulatable.
- Strong MVP pacing. It moves directly into a first-build cut rather than
  asking the implementer to digest the whole long tutorial first.
- It sensibly combines site/point and emitted flow for the first build, which
  keeps early interaction tight: drag a point, inject tension, switch grade,
  and watch the same source emit different outlets.
- It is concise enough to function as an implementation brief.

Cons:

- It is a partial cut, not a complete tutorial. It stops after the second point
  and does not cover unit shape, calibration, crystallization, ladders,
  equations, or concepts.
- It has little new Gojo theory beyond what is already in the other drafts.
- Its compactness removes some important distinctions that the full tutorial
  needs, especially duality before minus, contour traversal, boundary versus
  flow, and the 3D orthogonal clarification.
- It does not add much AI-query or GojoCAD/GojoDraw implementation detail
  beyond the shared page contract.

## Synthesis

The best version should keep:

- `tutorial_cur.md`'s physical bootstrap and teaching spine.
- `tutorial_cur.md`'s act-level pacing, but without cinematic framing.
- `tutorial_cod.md`'s cleaner screen structure and Gojo structure mapping.
- `tutorial_cod.md`'s dimensional / orthogonal clarification.
- `tutorial_gem.md`'s MVP-first pacing and "physically necessary and
  manipulatable" app framing.
- Both drafts' insistence that incomplete reads, residue, and tension remain
  visible.

The final app tutorial should change:

- Each page must include user controls and an interaction goal.
- Each animation must be drawable as GojoCAD geometry plus GojoDraw overlays.
- Each page must expose the Gojo structure behind the visual.
- AI query context should be explicit: page goal, current structures, selected
  source, read surface, result, residue/tension, and relevant code references.
- The first pass should be about 20 to 24 pages, not 30 plus appendix.
- The implementation path should start with the short interactive arc before
  trying to build the complete tutorial.

## Page Contract

Each tutorial page should provide this structure:

```text
Page
  title
  teaching goal
  guided animation
  interactive controls
  default Gojo structure
  drawable surfaces
  narration
  AI query context
  completion cue
```

The guided animation is the first explanation. The interactive controls let the
user test the concept. The completion cue is not a quiz; it is a small
structural event that tells the page it has done its job.

## Pacing Recommendation

The full tutorial is a 24-page learning path, but the app should not be built
or introduced that way at first. Start with a compact first-run arc that proves
the interaction model:

```text
physical structure
  -> resolution
  -> tension
  -> site with emitted flow
  -> second point as direction or extent
```

This short arc should feel like one coherent sandbox: inject participation,
watch what settles, preserve what does not, drag a point, switch grade, and see
the same evidence support different reads. Once that feels good, add the later
pages for duality, contour traversal, unit shape, calibration, crystallization,
ladders, equations, and concepts.

## Drawing Requirements

The first implementation can stay mostly 2D. For 3D ideas, use projected
diagrams rather than a full 3D renderer.

GojoCAD should be able to describe or approximate:

- frame points
- frame segments
- circles / contours
- simple filled areas
- partition markers on spans and contours
- correspondence lines between structures
- simple projected plane and cylinder diagrams
- result, residue, and tension witness markers

GojoDraw should be able to render:

- carrier rails and value ticks
- points, segments, contours, and fills
- flow arrows and lane overlays
- pressure/tension glow
- residue chips or gap markers
- animated pulse movement along relation routes
- scrubbed time for guided animation
- hit areas for draggable points and toggles

These are render and interaction affordances. They should not own Gojo meaning.

## Implementation Readiness Snapshot

What exists now:

- Gojo Core has carriers, carrier locations, relations, unit shapes, tension,
  residue-oriented reads, resolution ladders, focus/refinement surfaces,
  transforms, repeat surfaces, and equation reads.
- GojoCAD has construction helpers for shared sites, points, segments, frame
  points, frame segments, circles, arcs, radial flow, area fill, anti-fill, and
  early read-graph discovery for joins, partitions, branches, merges, and
  closed boundaries.
- GojoDraw has a Skia adapter that walks GojoCAD read graphs and renders dots,
  spans, directed segments, radial seeds, line/sheet seeds, circumference
  evidence, partitions, areas, topology nodes, and hit areas.
- GojoDraw also has generic presentation pieces: theme factory, visual cues,
  controls/components, animation clock/timeline helpers, hit-area behavior, and
  audio cue abstraction.
- Gojo Blocks now has a tutorial page contract and AI/query/tutorial-specific
  content records that consume those generic GojoDraw presentation pieces.

What is still missing or thin:

- General interaction: drag handles and hit behavior can be described, but
  there is not yet a reusable hit-test-to-Gojo-edit command layer.
- Animation application: GojoDraw has a generic clock/timeline model, but visual cues still
  need a renderer that turns `Pulse`, `FlowArrow`, `TensionGlow`, and
  `ResidueChip` into Skia overlays.
- Tutorial scenes: the pages are specified in docs, but not yet instantiated
  as `BlocksTutorialPage` records.
- GojoCAD style surfaces: render style is still a boundary concern. There is
  no Gojo-native style participation surface yet, which is fine for tutorial
  display but should not become concept meaning.
- Projected 3D: the tutorial can fake 3D with 2D projected diagrams, but there
  is no general 3D GojoCAD projection surface.
- Audio playback: GojoDraw audio cue metadata exists, but the current desktop
  project has no real audio backend package. The web version should map the
  same cue model to browser audio.

Core probably does not need tutorial-specific primitives yet. Most missing work
belongs in GojoCAD reads/builders, GojoDraw overlays, and Gojo Blocks
interaction state.

## Tutorial Pages

### Page 01 - Physical Structure, Not Scalars

Teaching goal:

Gojo starts from inspectable structure, not from detached numbers and operator
symbols.

Guided animation:

A pulse enters a small network of rails, points, and boundaries. Part of the
pulse settles into a channel. The rest remains as pressure at a boundary.

Interactive controls:

- Inject pulse amount.
- Toggle "show structure" to reveal carriers, locations, and relations.
- Toggle "show rendered view" to return to the clean diagram.

Default Gojo structure:

```text
source carrier
channel carrier
boundary relation
result location
tension location
```

Drawable surfaces:

Carrier rails, point pins, relation arrows, and a pressure glow.

Narration:

Most math libraries start with numbers and operations. Gojo starts with
participation in a local structure. A value can settle, route, or remain
unresolved. The unresolved part stays visible because it may be the next piece
of structure.

AI query context:

Load `Carrier`, `CarrierLocation`, `Relation`, and `ReadWitness` terminology.

Completion cue:

User injects a pulse that leaves both settled value and visible tension.

### Page 02 - Resolution Comes Before Precision

Teaching goal:

Every read exposes a smallest distinction. Finer detail is not secretly known
unless another structure exposes it.

Guided animation:

The same span is read through a coarse basis and a fine basis. The coarse read
leaves more residue.

Interactive controls:

- Resolution slider from 2 to 32 divisions.
- Drag source marker along the span.
- Toggle "show residue".

Default Gojo structure:

```text
source = CarrierLocation(C,n)
coarse basis = Segment(C,0,4)
fine basis = Segment(C,0,16)
read witness = source -> basis -> result + residue
```

Drawable surfaces:

One segment, tick partitions, movable marker, residue chip.

Narration:

Resolution is not a display setting for a hidden exact value. It is the
distinction made available by the current read surface. A low-resolution read
is still a valid read.

AI query context:

Explain `Segment(C,a,b)` as a basis/read surface and distinguish residue from
rounding error.

Completion cue:

User changes resolution and sees the same source leave different residue.

### Page 03 - Tension Is Unresolved Participation

Teaching goal:

Unspecified, incompatible, blocked, or below-resolution input remains as
tension or residue.

Guided animation:

Seven ticks enter a structure that can account for five. Five settle. Two
remain visible.

Interactive controls:

- Input amount stepper.
- Capacity slider.
- "Add route" button that creates a second carrier able to receive overflow.

Default Gojo structure:

```text
Carrier.Value
Carrier.Tension
capacity surface
optional route relation
```

Drawable surfaces:

Rail with capacity boundary, accepted ticks, tension glow, optional overflow
route.

Narration:

Gojo does not need to hide mismatch behind exceptions. The mismatch is
structural information. If a later route appears, the tension can move.

AI query context:

Load `Carrier.Value`, `Carrier.Tension`, settlement, and residue-preserving
transfer.

Completion cue:

User adds a route and watches previously unresolved tension become routed.

### Page 04 - Duality Before Minus

Teaching goal:

Opposition is structural before it becomes a scalar sign.

Guided animation:

Three examples flip independently: segment forward/backward, radial
outward/inward, contour clockwise/counterclockwise.

Interactive controls:

- Toggle selected normal direction for each example.
- Toggle display of signed location, inverse face, and reversed frame.

Default Gojo structure:

```text
L(C,-n)       signed location
C*            opposed carrier face
S(C,b,a)      reversed ordered frame
```

Drawable surfaces:

Line arrows, radial arrows, ring traversal arrows, face labels.

Narration:

Do not compress all opposition into one minus sign. A negative location, an
opposed flow face, and a reversed frame are different pieces of structure.

AI query context:

Explain signed `CarrierLocation`, `Carrier.Inverse()`, and segment orientation.

Completion cue:

User toggles the three duals and sees that the geometry can remain unchanged
while the read changes.

### Page 05 - Site / Point

Teaching goal:

A point is already relation structure: a carrier location grounded to its
carrier.

Guided animation:

A dot appears without axes. A carrier rail fades in. The relation from the
location to the carrier appears.

Interactive controls:

- Drag the point along the rail.
- Toggle axes off/on.
- Inspect the relation shape.

Default Gojo structure:

```text
Point(C,n) = L(C,n) -> C
```

Drawable surfaces:

Point pin, carrier rail, relation arrow.

Narration:

For teaching, call this a point. In Gojo, it is not a coordinate bag. It is a
small connected structure: location to carrier.

AI query context:

Load `Relation.Point(carrier,ticks)`.

Completion cue:

User drags the point and sees the carrier location update.

### Page 06 - A Point Emits Flow

Teaching goal:

Unresolved tension at a site can emanate flow or seek an orthogonal outlet.

Guided animation:

Tension enters a point and radiates outward. The user can reverse it inward.

Interactive controls:

- Outward/inward toggle.
- Environment grade selector: line, plane, volume projection.
- Add sink marker.

Default Gojo structure:

```text
point source
orthogonal outlet candidate
flow face evidence
optional sink relation
```

Drawable surfaces:

Point, pressure rings, arrows, sink point, unresolved glow.

Narration:

At this stage the flow is not a measured circle. It is a tendency: away from
this site or toward this site, shaped by the available environment.

AI query context:

Explain `Relation.OrthogonalOutlet(source)` as "unaccounted here", not merely
geometric perpendicular.

Completion cue:

User adds a sink and watches unbounded flow become routed toward it.

### Page 07 - Grade Shapes Settlement

Teaching goal:

The same source can settle differently in 1D, 2D, or 3D-like environments.

Guided animation:

One point receives the same pulse in three panels: rail, plane, projected
volume.

Interactive controls:

- Switch environment grade.
- Scrub between the three panels.
- Toggle "same source" highlight.

Default Gojo structure:

```text
same point source
different surrounding basis / unit-shape surfaces
different readable outlet shapes
```

Drawable surfaces:

Line escape, radial plane escape, projected shell.

Narration:

The point did not receive a symbolic command saying "be spherical". The local
structure made that settlement available.

AI query context:

Explain grade as a read/environment property, not a global object class.

Completion cue:

User switches grade and sees the source relation remain highlighted.

### Page 08 - Second Point: Direction Or Extent

Teaching goal:

A second point can define direction or mark radial extent.

Guided animation:

The second point appears. The page splits into two live reads: ordered segment
and radius/extent marker.

Interactive controls:

- Drag the second point.
- Toggle "direction read" and "extent read".
- Lock first point as center.

Default Gojo structure:

```text
direction read = P0 -> P1
extent read = center point + radius marker
```

Drawable surfaces:

Two points, directed segment, ring/halo, radius marker.

Narration:

The same two points can participate in different reads. Ordered, they create a
linear frame. As extent, they calibrate a contour without deciding traversal.

AI query context:

Explain relation-shaped reads over the same visible evidence.

Completion cue:

User drags the second point and sees both reads update at once.

### Page 09 - Segment Orientation And Local Sign

Teaching goal:

A segment is a directed frame. Sign is local to the selected read direction.

Guided animation:

A marker moves along a segment. Reversing the frame flips the read sign without
moving endpoints.

Interactive controls:

- Reverse frame toggle.
- Drag marker.
- Show endpoint identities.

Default Gojo structure:

```text
Segment(C,a,b) = Point(C,a) -> Point(C,b)
reverse read = Segment(C,b,a)
```

Drawable surfaces:

Segment, start/end pins, marker, read arrow, signed label.

Narration:

The endpoints are stable structure. Orientation is how we read across them.
Local positive and negative appear only after a normal direction exists.

AI query context:

Explain frame orientation separately from carrier identity.

Completion cue:

User reverses the frame and sees the same marker read with opposite sign.

### Page 10 - Contour: Extent Is Not Traversal

Teaching goal:

A contour can have size before it has a countable origin or traversal
direction.

Guided animation:

A center and radius marker create a ring. No traversal arrows appear until a
zero marker is added to the ring.

Interactive controls:

- Drag radius marker.
- Add/remove contour zero marker.
- Toggle clockwise/counterclockwise traversal after marker exists.

Default Gojo structure:

```text
center point
radius / extent evidence
contour boundary
optional contour origin marker
optional traversal face
```

Drawable surfaces:

Center, radius, ring, marker, partition ticks, traversal arrows.

Narration:

Extent is not traversal. The contour can exist as boundary-like structure
before it has a marked origin or positive direction around itself.

AI query context:

Explain circular unit shape, contour calibration, and traversal direction as
separate evidence.

Completion cue:

User adds the contour marker and sees cycle ticks become countable.

### Page 11 - Boundary And Flow Are Separate

Teaching goal:

One boundary can support multiple flow or participation reads.

Guided animation:

One ring remains fixed while overlays switch: no-flow, interior fill, exterior
pressure, inward radial, outward radial, clockwise, counterclockwise.

Interactive controls:

- Overlay selector.
- Multi-select overlays to create tension between incompatible flows.
- Inspect boundary evidence separately.

Default Gojo structure:

```text
boundary evidence
flow face evidence
occupied lane evidence
residue/tension when overlays conflict
```

Drawable surfaces:

Ring, fill tint, arrows, glow at conflicts.

Narration:

Boundary says where structure is addressable. Flow says how participation
moves, presses, routes, or opposes. Keeping them separate prevents boolean and
geometry shortcuts from owning meaning.

AI query context:

Load ConceptSurface boundary/flow terms.

Completion cue:

User selects two incompatible overlays and sees tension remain inspectable.

### Page 12 - Orthogonal Outlet And Dimensions

Teaching goal:

Orthogonal means "not accountable in this order"; its shape depends on the
source and environment.

Guided animation:

Two points in projected 3D begin coincident. The outlet is plane-like. As the
points separate, a stable axis appears and the orthogonal outlet becomes
cylinder-like around that axis.

Interactive controls:

- Distance slider from coincident to separated.
- Environment selector: 2D plane, 3D projection.
- Toggle source shape: point, zero-length segment, nonzero segment.

Default Gojo structure:

```text
point source -> radial / shell possibility
zero segment source -> plane-like or ray seed possibility
nonzero segment source -> side surface / cylinder-like possibility
```

Drawable surfaces:

Projected plane, projected cylinder, axis segment, side arrows.

Narration:

In 3D, two distant points define an axis; the orthogonal read can become
cylinder-like. If the points touch, there is no distant axis yet, so the
orthogonal read can be plane-like.

AI query context:

Explain `UnitShape.LinearFrom`, `UnitShape.SheetFrom`, and generic
`OrthogonalOf` as reads over source shape.

Completion cue:

User moves from coincident to separated and sees the outlet change shape.

### Page 13 - Partition And Tick

Teaching goal:

Counting requires exposed distinctions; a tick is one distinction at the
selected resolution.

Guided animation:

A segment, contour, and area each gain partitions. One tick/cell is
highlighted.

Interactive controls:

- Partition count slider.
- Select segment, contour, or area mode.
- Step through ticks.

Default Gojo structure:

```text
segment basis = S(C,0,n)
cycle closure = P(C,n) -> P(C,0)
area cells = fill basis + partition surface
```

Drawable surfaces:

Tick marks, contour divisions, area grid cells.

Narration:

A tick is not a bare integer. It belongs to a carrier-like surface, and the
shape of that surface matters.

AI query context:

Explain partition versus unit shape.

Completion cue:

User changes partition count and sees value resolution change without changing
the source boundary.

### Page 14 - Unit Shape

Teaching goal:

Unit shape says what one tick scales as. It is separate from resolution,
orientation, and accounting.

Guided animation:

Several unit stamps appear: line, contour, sheet, product, opposed face. Two
carriers share the same stamp but hold different values.

Interactive controls:

- Assign compatible unit shape to a second carrier.
- Change value independently on both carriers.
- Toggle descriptor/provenance display.

Default Gojo structure:

```text
Carrier(UnitShape)
UnitShape.Atom
UnitShape.CircularFrom(point)
UnitShape.SheetFrom(segment)
UnitShape.Product(a,b)
```

Drawable surfaces:

Unit stamps, carrier rails, independent value ticks.

Narration:

Two carriers can share a unit shape without being the same carrier. Unit shape
is the promise of what one tick means, not the mutable value currently stored.

AI query context:

Load `UnitShape` docs and tests.

Completion cue:

User changes one carrier's value and sees the shared unit shape remain stable.

### Page 15 - Calibration And Number

Teaching goal:

Number is a read through unit and partition structure, not the primitive
source of meaning.

Guided animation:

An unknown segment is compared to a unit ruler. The result reads as whole ticks
plus residue. A finer partition converts some residue into sub-ticks.

Interactive controls:

- Drag source length.
- Drag unit length.
- Resolution slider for sub-ticks.
- Toggle "external comparison" and "internal partition".

Default Gojo structure:

```text
source segment
calibrator segment
read result = whole ticks + residue
optional sub-tick ladder
```

Drawable surfaces:

Source segment, ruler, residue chip, structured number display.

Narration:

Internal partition gives fractional resolution. External comparison gives
measured count. Neither requires pretending the original structure was born as
a floating point number.

AI query context:

Explain structured reads such as `3 + residue` and `3 4|10`.

Completion cue:

User changes unit length and sees the count and residue update.

### Page 16 - Crystallization

Teaching goal:

A stable structure can become the unit shape of a new carrier.

Guided animation:

A calibrated span freezes into a unit stamp. A new carrier appears using that
stamp. It then accumulates its own independent value.

Interactive controls:

- Crystallize current span.
- Use crystal unit to measure another span.
- Toggle provenance visibility.

Default Gojo structure:

```text
source structure
crystallized unit shape
new Carrier(crystallizedUnitShape)
explicit provenance relation
```

Drawable surfaces:

Stamp extraction, new carrier rail, provenance line.

Narration:

Crystallization hardens unit shape, not live value. The original structure can
remain as provenance, but the new carrier has its own state.

AI query context:

Explain `TryCrystallize`, unit-shape stability, and independent value state.

Completion cue:

User crystallizes one span and uses it as a unit elsewhere.

### Page 17 - Resolution Ladder

Teaching goal:

A value can be a ladder of carriers at different resolutions.

Guided animation:

Ticks enter the ones carrier. At ten, overflow routes into tens. Then tens
overflow into hundreds.

Interactive controls:

- Add ticks.
- Change radix.
- Hide/show lower levels.
- Read at selected level.

Default Gojo structure:

```text
Root
ones
Segment(ones,0,10) -> Point(tens,0)
tens
Segment(tens,0,10) -> Point(hundreds,0)
hundreds
```

Drawable surfaces:

Stacked rails, carry pulse, residue on lower rail.

Narration:

The ones, tens, and hundreds levels are distinct value sites. The shared value
identity comes from relation structure, not from one giant carrier.

AI query context:

Load `ResolutionLadder` and settlement route tests.

Completion cue:

User reads the same ladder at ones, tens, and hundreds resolution.

### Page 18 - Settlement Routes And Lanes

Teaching goal:

Settlement uses local relation structure. Lanes preserve different boundary
and flow participation.

Guided animation:

An input enters a boundary-flow surface. It can route through one lane, become
blocked in another, or remain as opposed tension.

Interactive controls:

- Choose lane basis.
- Add/remove transition relation.
- Inject forward or inverse flow.

Default Gojo structure:

```text
flow face
boundary side
lane basis
occupied lane
transition relation
result + tension
```

Drawable surfaces:

Lane overlays, arrows, blocked gates, glow at unresolved opposition.

Narration:

Gojo does not need a broad classifier to decide what happened. The local
surface shows which lane was occupied, which transition existed, and what
remained unresolved.

AI query context:

Load boundary/flow grade codex concepts and current ConceptSurface helpers.

Completion cue:

User adds a transition and watches blocked tension become routed flow.

### Page 19 - Support And Occupancy

Teaching goal:

Boolean-like operations are reads over occupied participation through a basis.

Guided animation:

Two spans overlap on a partitioned carrier. Cells show A support, B support,
shared support, exclusive support, blocked support, and uncovered scope.

Interactive controls:

- Drag spans.
- Toggle flow/anti-flow for each span.
- Change partition resolution.
- Select read: union, intersection, difference, complement.

Default Gojo structure:

```text
selected participants
scope
boundary evidence
flow basis
partitioned cells
occupied lane evidence
result support
residue / tension
```

Drawable surfaces:

Partitioned rail or area cells with support colors and residue markers.

Narration:

Equal coordinate is not the same as shared occupied support. A boolean-style
read needs selected participants, a scope, a basis, and occupied lanes.

AI query context:

Load participation settlement notes and support read helpers.

Completion cue:

User switches from equal-coordinate view to occupied-support view.

### Page 20 - Branch And Merge

Teaching goal:

Branching is routed participation and can remain unresolved at the current
resolution.

Guided animation:

One input flows to three candidate routes. Coarse support picks a route. A
near-tie leaves tension until finer support is read.

Interactive controls:

- Adjust support sliders.
- Change comparison resolution.
- Add merge surface.

Default Gojo structure:

```text
input
branch group
candidate support ladders
selected branch read
merge surface
residue / unresolved branch tension
```

Drawable surfaces:

Forked channels, support ladders, merge node, unresolved glow.

Narration:

A branch is not an `if` statement. It is visible route structure with support,
cost, residue, and possible unresolved tension.

AI query context:

Explain fanout, branch group, and comparison ladders.

Completion cue:

User creates a near-tie and then resolves it by increasing comparison
resolution.

### Page 21 - Structural Ladders

Teaching goal:

Refinement adds detail without destroying lower-resolution structure.

Guided animation:

A rectangle refines into a rounded rectangle, then a thick frame. Focus,
carried edges, replacement arcs, and residue remain visible.

Interactive controls:

- Select a corner.
- Adjust radius.
- Toggle lower rung visibility.
- Crystallize current rung.

Default Gojo structure:

```text
source rung
focus surface
carry-forward
replacement / detail
correspondence
residue
result rung
```

Drawable surfaces:

Rectangle, tangent points, arc, correspondence lines, residue markers.

Narration:

The rectangle is not wrong after it is refined. It remains a lower-resolution
read. The finer rung should know which corner it refined and what residue it
left.

AI query context:

Load `FocusSurface`, `RefinementRead`, and `StructuralLadderSurface`.

Completion cue:

User rounds one corner and sees the source, result, and residue together.

### Page 22 - Equations As Reads

Teaching goal:

An equation is a relation-shaped read that predicts output and exposes error.

Guided animation:

Input segment maps to output segment. User moves input. Predicted output moves.
Observed output can be dragged away, creating residual error.

Interactive controls:

- Drag input sample.
- Drag observed output.
- Reverse output frame.
- Add sample to stream.

Default Gojo structure:

```text
equation surface = Segment(input,0,10) -> Segment(output,0,100)
prediction witness
comparison witness
residual / violation
```

Drawable surfaces:

Input ruler, output ruler, prediction marker, observed marker, residual gap.

Narration:

The equation is not a symbolic formula tree first. It is a read surface. Error
is structure: predicted, observed, residual, violation, and provenance.

AI query context:

Load `Gojo.Equations` README and affine equation helpers.

Completion cue:

User creates a residual and inspects the comparison witness.

### Page 23 - Concept Surface

Teaching goal:

A concept is a stable read over structure, not a label pasted onto data.

Guided animation:

An object-like shape shows overlays: boundary, support, flow, capacity,
blocked passage, affordance, residue, and concept rung.

Interactive controls:

- Toggle evidence layers.
- Ask for low-resolution or high-resolution read.
- Select an unresolved part and ask AI what structure is missing.

Default Gojo structure:

```text
selected source
boundary-flow basis
support lanes
capacity / settlement witness
focus surface
timeline / provenance
residue or tension
concept read result
```

Drawable surfaces:

Object outline, support colors, flow arrows, blocked regions, residue glows,
concept ladder labels.

Narration:

Gojo concepts should be rediscoverable from connected evidence: boundary,
flow, support, function, capacity, residue, and history. A helper can inspect
the concept, but the structure should carry it.

AI query context:

Load ConceptSurface docs and local selected structure.

Completion cue:

User switches between low and high concept resolution while evidence stays
visible.

### Page 24 - Playground And Recap

Teaching goal:

Tie the common read shape across geometry, numbers, equations, and concepts.

Guided animation:

The tutorial spine appears as a compact ladder. Selecting any rung reopens the
page's example in sandbox mode.

Interactive controls:

- Jump to any page.
- Toggle "show common read witness".
- Ask AI about selected structure.

Default Gojo structure:

```text
selected source
read surface or basis
result
residue or tension
provenance
```

Drawable surfaces:

Tutorial map, selected examples, witness diagram.

Narration:

The same pattern keeps returning. Select structure, read through a surface,
produce a result, preserve residue or tension, and keep provenance visible.
That is the through-line from points to equations to concepts.

AI query context:

Load the whole tutorial page index and current code references.

Completion cue:

User opens a previous page and asks an AI question grounded in the current
Gojo structure.

## AI Query Behavior

The built-in AI helper should not receive only the visible page text. It should
receive the current page's Gojo structure context:

```text
page title
teaching goal
selected source
read surface / basis
result
residue / tension
visible controls and their values
relevant code files or docs
```

Example page-specific questions:

- "Why is this residue not rounding error?"
- "What is the difference between `L(C,-5)` and `L(C*,5)` here?"
- "Which relation makes this overflow route into tens?"
- "What structure would be missing if this contour cannot be traversed?"
- "Why is this intersection a support read instead of coordinate equality?"

The answer should refer to the actual page structure and, when useful, to the
Gojo code or docs.

## First Build Cut

The first implementation should not try to build all 24 pages. Build the
smallest coherent tutorial arc:

1. Physical structure, resolution, tension.
2. Site, emitted flow, grade.
3. Second point as line or extent.
4. Boundary versus flow.
5. Orthogonal outlet and dimensions.
6. Partition, tick, unit shape, calibration.
7. Resolution ladder.
8. Structural ladder.
9. Equation read.

This cut teaches the core shape without requiring full concept-surface UI yet.

## Non-Goals

- Do not build a cinematic camera system for this tutorial.
- Do not make static screenshots that only mimic Gojo structure.
- Do not store tutorial state as broad React-style snapshots if local Gojo
  structures and controls can regenerate the read.
- Do not turn visual labels into meaning. Labels explain; relations carry.
- Do not hide unresolved states to make the page look cleaner.

## Final Principle

Every page should make one idea feel physically necessary, then let the user
touch it.

The tutorial succeeds when a programmer can say:

```text
I can see the selected source.
I can see the read surface.
I can see the result.
I can see what did not settle.
I can ask why, and the answer points back to traversable Gojo structure.
```

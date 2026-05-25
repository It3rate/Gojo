# Gojo ConceptSurface

**Design codex:** see [`ConceptShapesCodex.md`](ConceptShapesCodex.md) for the
full primitive algebra, participation model, assembly rules, extension pipeline,
and missing core structure proposals.

`ConceptSurface` is a proposed Gojo-native foothold for low-resolution
conceptual structures. It is meant to sit above `UnitShape` in scale, not above
Gojo in meaning.

A `UnitShape` says:

```text
one tick of this carrier participates like this
```

A `ConceptSurface` says:

```text
this connected structure affords this kind of topology, focus, flow,
parameterization, and settlement
```

The name is only a design handle. A future helper may build and read these
surfaces, much like `RepeatSurface`, but the concept must remain visible as
ordinary carriers, locations, relations, unit shapes, slots, tension, residue,
and timelines. If the helper code disappears, the structure should still be
inspectable and meaningful.

This note starts with physical, mechanical, CAD, manufacturing, and engineering
concepts because they are more grounded than pure language concepts. The same
structure can later support metaphorical reads, but the metaphor should replace
or rebind descriptors rather than hide the original physical shape.

## Intent

Many mechanical words are not primitive objects. They are focused reads over a
small set of primitive structural phenomena.

For example, bridge, connector, funnel, filter, choke point, reducer, nozzle,
and sprayer can all share a small-neck topology. The word changes mostly by:

- which part is in focus
- which flow is expected
- which side is constrained
- which parameters are open or settled
- what residue or tension appears when the structure is used

The goal is to let a design start at very low resolution:

```text
some orthogonal meeting
some narrowed passage
some one-way convergence
some repeated stable sites
some shortening relation
```

and then refine through local reads, parameter bindings, resolution ladders,
settlement, residue, and timelines until it becomes a specific design.

This is analogous to how equation surfaces can start coarse and then become
more precise through prediction, residue, resolution search, and repeat
evidence. A concept word is a read at a resolution, not the source of meaning.

## Proposed Surface Skeleton

A low-resolution concept surface can be represented as an ordered group:

```text
ConceptSurface(root)
  selected structure
  topology surface
  focus surface
  flow surface
  parameter surface
  constraint surface
  settlement / residue / tension witness
  refinement timeline
```

This shape is intentionally ordinary Gojo structure:

- `selected structure` is the thing being interpreted.
- `topology surface` records how carriers, spans, regions, or joins connect.
- `focus surface` records the local site, side, span, or observer read being
  emphasized.
- `flow surface` records allowed, blocked, agnostic, convergent, divergent, or
  opposed participation using carrier faces and relation direction.
- `parameter surface` contains open slots, carrier locations, spans, or
  repeat controllers that can later refine the concept.
- `constraint surface` bounds parameters or flow using faces, spans, ratios,
  ladders, or opposed carriers.
- `settlement / residue / tension witness` records what happens when the
  structure is perturbed or read.
- `refinement timeline` records one or more steps where parameters become more
  resolved, residue changes, or topology hardens.

An executable helper could expose:

```csharp
ConceptSurface.Create(...)
ConceptSurface.TryRead(...)
ConceptSurface.TryReadFocus(...)
ConceptSurface.TryReadParameters(...)
```

but those helpers should only construct and inspect relation structure. They
must not own concept identity.

## Primitive Gojo Vocabulary

These are the primitive pieces that seem useful for mechanical concepts. The
important rule is that the primitive should be visible in Gojo structure rather
than in an external classifier.

| Primitive | Gojo expression |
| --- | --- |
| Positive face | ordinary carrier participation. |
| Opposed face | inverse carrier participation, distinct from negative ticks. |
| Negative location | signed location on a carrier, not an inverse face. |
| Inverse negative | negative location on an opposed face; useful but not the same as either ingredient alone. |
| Point | `Relation.Point(C,n)`, often an origin, end, marker, focus, or boundary. |
| Span | `Relation.Segment(C,a,b)`, a directed bounded carrier interval. |
| Directed span | a segment using positive and opposed carrier faces to preserve flow orientation. |
| Aligned scale | relation between compatible carrier spans, producing projection, ratio, carry, or resolution. |
| Additive gathering | ordered/additive group anchored at a holder carrier. |
| Orthogonal outlet | `Relation.OrthogonalOutlet(source)`, the generic source-to-new-participation surface. |
| Point radial | orthogonal of a point, the low-resolution seed of circular, radial, or level-line behavior. |
| Zero-segment linear | orthogonal of a collapsed segment, the seed of ray or line behavior. |
| Segment sheet | orthogonal of a nonzero segment, the seed of sheet, area, or panel behavior. |
| Area | product of two non-aligned positive participations, before choosing grid, radius, or measurement resolution. |
| Anti-area | product involving opposed or inverse participation; useful for voids, absence, anti-fill, or removal. |
| Join | two or more structures sharing a point, endpoint, origin, marker, or carrier site. |
| Passage | a relation where two regions or sides are connected through a narrower site or span. |
| Boundary | a contour, rim, edge, or limit relation around a region or support. |
| Gap | explicit clearance carrier or span between two structures. |
| Contact | opposed or adjacent surfaces with low gap and possible pressure or tension. |
| Block | attempted read or flow leaves residue or tension rather than projecting cleanly. |
| Allow | attempted read or flow projects with low residue. |
| Slip | normal participation blocked while tangential participation remains traversable. |
| Friction | attempted tangential slip leaves intentional tension. |
| Preload | stored tension that is part of the structure's function, not an error. |
| Threshold | carrier boundary where accumulated tension changes settlement or topology. |
| Stable site | local low-residue settlement position. |
| Repeat | repeated carrier sites, spans, teeth, holes, detents, threads, or timeline steps. |
| Focus | a local selected point, span, side, surface, or observer read. |
| Parameter | an open carrier, location, relation, descriptor slot, placeholder, span, or controller. |
| Residue | unresolved remainder from a read through a chosen basis. |
| Tension | unresolved transferred change available to settlement. |
| Timeline | ordered witness of refinement, traversal, use, or settling. |

## Flow From Carrier Faces

Flow should be encoded by carrier faces, signed locations, and relation shape,
not by a symbolic flow tag.

Useful distinctions:

```text
agnostic traversable
  a connection exists, but no preferred direction is encoded

explicit bidirectional
  two opposed compatible routes or a symmetric pair of read surfaces exist

one-way
  one oriented route exists; reverse projection leaves residue or tension

convergent
  many source positions project toward fewer target positions

divergent
  one or few source positions project toward many target positions

split
  one incoming participation branches into multiple outward participations

merge
  multiple incoming participations settle into one outgoing participation

block
  attempted continuation cannot be accounted for by the available face/span

filter
  selected basis passes; nonmatching basis remains residue or tension
```

Agnostic traversable is not the same as explicitly bidirectional. A bridge can
exist before it has traffic rules. Bidirectional flow is a higher-resolution
commitment because it installs both directions as structure.

## Topology As Relation Connection

Topology should be expressed by real connection:

```text
point join
  several structures share Point(C,0)

end join
  one span endpoint is physically the same site as another span endpoint

origin join
  branches meet at their origin, useful for T, Y, L, X, and radial patterns

orthogonal join
  one structure is the orthogonal outlet of another

area join
  two non-aligned carriers combine into a product participation

boundary join
  a carrier or contour bounds a fill, passage, or void

ladder join
  aligned carriers connect through resolution, carry, or scale relations

repeat join
  repeated sites share a controller axis or contour carrier
```

Compact glyph words such as `I`, `T`, `Y`, `L`, `O`, `X`, and `H` are helpful
as sketches, but they should not become tags.

Possible low-resolution reads:

| Sketch | Structural read |
| --- | --- |
| `I` | aligned continuation through one carrier or compatible span. |
| `T` | one stem and two side participations sharing a point. |
| `Y` | split or merge where branches have compatible focus but distinct outward spans. |
| `L` | orthogonal turn or corner with one incoming and one outgoing leg. |
| `O` | closed contour, cycle, or stable boundary. |
| `X` | crossing, overlap, exchange, or disconnected co-location depending relation strength. |
| `H` | two side regions connected by a bridge span or crossbar. |

The flow read then decides whether `T` is a split, merge, intersection, inlet,
outlet, valve, or blocked junction.

## Focus As Structure

Focus can be encoded as a relation-readable selection. It is not a comment or
metadata bag.

Examples:

```text
focus on a point
  Point(neck,0)

focus on a span
  Segment(slotAxis,a,b)

focus on a side
  AdditiveGroup(focusRoot, selectedSide, boundary, local relation)

focus on a boundary
  contour carrier or rim structure

focus on an input
  source span plus route into selected structure

focus on an output
  target span or fanout region

focus on residue
  read residue group from a projection, claim, or settlement

focus on tension
  carrier tension slot or claim violation magnitude

focus on refinement
  latest step in a `TimeStepSequence`
```

The same topology can produce different words under different focus:

| Shared topology | Focus | Natural read |
| --- | --- | --- |
| two regions plus neck | neck | bridge or connector |
| two regions plus neck | input side | funnel, reducer, filter, choke |
| two regions plus neck | output side | sprayer, diffuser, expander |
| two regions plus neck | blocked residue | obstruction or bottleneck |
| three-leg join | shared point | junction |
| three-leg join | incoming stem | split |
| three-leg join | outgoing stem | merge |
| contact pair | normal gap | seat, stop, clamp |
| contact pair | tangential slip | guide, bearing, friction surface |

This makes focus a first-class part of the concept surface. A bridge is not
only the topology. It is a read over that topology centered on the passage.

## Parameter Surfaces

Parameters should start as open structure. They can later be bound by evidence,
resolution search, design constraints, or settlement.

Useful parameter encodings:

| Parameter need | Gojo encoding |
| --- | --- |
| Unknown element | `Placeholder` or descriptor slot. |
| Unknown length | carrier location or span endpoint slot. |
| Nonnegative length | positive face participation only. |
| Reduction | inverse or negative participation relative to an input span. |
| Maximum | boundary point or clamp segment. |
| Minimum | boundary point plus blocked below-origin residue. |
| Range | `Segment(parameter, min, max)`. |
| Direction | oriented carrier face or relation from source to target. |
| Spread | output arc, fanout span, or orthogonal parameter carrier. |
| Clearance | gap carrier with positive-only desired participation. |
| Interference | negative or opposed gap residue. |
| Count | repeat controller span. |
| Pitch | relation between repeat axis and physical span. |
| Threshold | boundary point where tension changes settlement. |
| Friction | relation that preserves slip residue as intentional tension. |
| Resolution | `ResolutionLadder` or `UnitShape.ResolutionOf(...)`. |

Parameterization can change the word:

```text
bridge + pass constraint        -> filter
bridge + inward one-way flow    -> funnel
bridge + outward fanout         -> sprayer
bridge + demand and blockage    -> choke point
corner + transition surface     -> chamfer or fillet
slot + low friction             -> guide or bearing track
slot + one-way stops            -> ratchet path
shortening span + repeat        -> screw or bolt
contact pair + preload          -> clamp
stable sites + threshold        -> detent or snap
```

## Constraints By Face And Span

Many constraints can be expressed with carrier participation instead of a
separate symbolic rule.

Examples:

```text
nonnegative
  use the positive carrier face as the allowed parameter carrier

negative-only
  use negative locations on the same face when the value is a signed offset

opposed-only
  use the inverse carrier face when the participation is materially opposed

bounded
  use Segment(parameter, min, max)

one-way
  install one route/read face and leave reverse projection unresolved

reducer
  bind output span through a relation constrained against the input span

positive area
  product of two non-aligned positive carriers

anti-area
  product where one side is opposed or inverse participation

one-cell two-dimensional parameter
  positive carrier combined with an orthogonal inverse carrier at resolution 1
```

This keeps constraints inspectable. If a design tries to use the blocked side,
the result is tension, residue, or unresolved structure rather than an
exception or hidden validation flag.

## Settlement And Refinement

Concept surfaces should be useful as solver seeds. A large design equation can
start with low-resolution concepts and refine them.

Refinement should be recorded as structure:

```text
TimeStepSequence
  initial low-resolution surface
  step 0: bind one parameter
  step 1: test settlement
  step 2: preserve residue
  step 3: narrow a span
  step 4: harden a more specific topology
```

Typical refinement paths:

```text
corner
  orthogonal meeting
  -> bounded transition span
  -> chamfer, fillet, hinge, crack point, or reinforced gusset

bridge
  agnostic passage
  -> directed flow
  -> connector, funnel, filter, choke, or reducer

snap
  thresholded transition
  -> bistable timeline
  -> latch, detent, clip, or breakaway feature

slot
  linear allowed motion plus orthogonal block
  -> clearance, friction, stops, repeat sites
  -> guide, rail, ratchet, or keyed track
```

## Example Catalog

The examples below are deliberately low resolution. Each one maps to a word,
but the word is only a local read. Small changes in focus, flow, or parameters
can legitimately produce another word.

### 1. Bridge

Selected structure: two areas, regions, supports, or sides connected through a
single neck point or span.

Topology: an `H`-like relation where two side participations share a cross
connection. The neck is the only traversable relation between the sides.

Focus: the neck or bridge span.

Flow: agnostic traversable by default. No preferred direction is encoded yet.

Parameters: optional width, capacity, length, allowed basis, or support
resolution. A simple bridge does not need a parameter for side area size.

Variations:

- Add explicit bidirectional routes: road or connector.
- Add one-way route: chute, inlet, or outlet depending focus.
- Add pass constraint: filter.
- Add high demand and blocked residue: choke point.
- Add shortening or clamp relation across the sides: fastener or tie.

### 2. Connector

Selected structure: a passage, neck, contact, or relation between two larger
structures.

Topology: two sides connected by a local relation; side geometry is mostly
ignored.

Focus: the bridge element itself, not the shape of the sides.

Flow: one-way or two-way if encoded; otherwise agnostic traversable.

Parameters: contact type, allowed carrier basis, retention, resistance, or
residue tolerance.

Variations:

- Add shape matching: socket, plug, key, or coupler.
- Add material route: pipe, wire connector, hinge pin, bearing seat.
- Add pass/block basis: filter connector.

### 3. Funnel

Selected structure: a collector region or arc converging toward a point, neck,
or smaller span.

Topology: inverse radial or many-to-one relation. A physical funnel often adds
an outlet carrier orthogonal to the collector plane.

Focus: collector side moving toward the neck.

Flow: one-way inward. Reverse flow is not automatically present.

Parameters: collector span, neck span, outlet direction, outlet length,
convergence rate, gravity or downstream descriptor.

Variations:

- Add pass constraint at the neck: filter.
- Add blocked demand at the neck: choke.
- Add smaller output span relative to input: reducer.
- Focus on reverse outward flow: sprayer or diffuser.

### 4. Sprayer

Selected structure: a point, neck, or small span diverging into an area, arc,
or volume.

Topology: radial-out or one-to-many fanout.

Focus: output side and spread region.

Flow: one-way outward from the neck.

Parameters: fanout angle, output arc, droplet/particle resolution, pressure
carrier, pulse timeline.

Variations:

- Bound the fanout arc with a parameter: adjustable nozzle.
- Add repeat/pulse: spray pattern.
- Add blocked output region: splatter, obstruction, or back-pressure.
- Reverse flow focus: funnel.

### 5. Filter

Selected structure: a passage with a constraint surface.

Topology: bridge-like, but with selected pass basis attached.

Focus: what crosses the passage.

Flow: often one-way, but can be agnostic if the pass constraint is symmetric.

Parameters: allowed carrier unit, resolution, pore size, threshold, orientation,
or repeated pass sites.

Settlement: matching participation passes with low residue. Nonmatching
participation remains as residue, tension, or blocked structure.

Variations:

- Remove pass constraint: bridge.
- Add one-way collector: funnel filter.
- Add pressure/tension: membrane, screen, valve, or separator.

### 6. Choke Point

Selected structure: convergent passage where incoming participation exceeds
the local accounting capacity.

Topology: bridge or funnel topology with a narrow neck.

Focus: neck plus residue under demand.

Flow: usually directed into the neck, but the key read is blockage/tension.

Parameters: capacity span, demand span, allowed basis, queue or pressure
carrier.

Settlement: excess flow remains as tension or delayed timeline residue.

Variations:

- If demand fits: connector or passage.
- If pass basis matters: filter.
- If output span is deliberately smaller: reducer.

### 7. Reducer

Selected structure: input span projected to a smaller output span.

Topology: directed scale relation between two aligned or comparable spans.

Focus: relation between input size and output size.

Flow: input-to-output. Reverse is not implied.

Parameters: reduction ratio, minimum output, maximum output, resolution ladder.
The reduction parameter can be expressed with inverse or negative participation
relative to the input instead of a symbolic less-than rule.

Variations:

- If the output can also grow: adapter or scaler.
- If nonmatching basis is blocked: filter reducer.
- If the smaller span raises pressure: nozzle or choke.

### 8. Expander

Selected structure: smaller input span projected to larger output span.

Topology: directed scale relation or fanout.

Focus: output span or spread.

Flow: input-to-output expansion.

Parameters: expansion ratio, spread angle, output support area, pressure drop.

Variations:

- Radial output: diffuser or sprayer.
- Linear output: spreader.
- Reversible explicit relation: adapter.

### 9. Corner

Selected structure: two carriers, spans, or surfaces meeting orthogonally or
at an unresolved angle.

Topology: `L`-like join at a point, endpoint, or origin.

Focus: meeting site.

Flow: unspecified by default. It can turn, stop, split, merge, or simply mark a
boundary change.

Parameters: angle, transition span, radius, sharpness, material thickness,
local stress/tension.

Variations:

- Add transition facet: chamfer.
- Add radial transition: fillet.
- Add pivot relation: hinge.
- Add reinforcement area: gusset.
- Add blocked flow: stop or shoulder.

### 10. Chamfer

Selected structure: a corner with an intermediate linear transition span.

Topology: orthogonal meeting replaced by a third span connecting the two
original directions.

Focus: transition span.

Flow: usually allowed along the transition; may be read as clearance, lead-in,
or softened edge.

Parameters: offset distances on each side, angle, transition resolution.

Variations:

- Replace transition with radial orthogonal: fillet.
- Focus on assembly insertion: lead-in.
- Focus on touch safety: softened edge.

### 11. Fillet

Selected structure: a corner with a radial or curved transition.

Topology: orthogonal meeting plus point-radial or contour participation.

Focus: transition surface.

Flow: smooth turning or stress redistribution, depending read.

Parameters: radius carrier, arc span, tangent contact sites, resolution.

Variations:

- Very low radius: sharp corner.
- Linear transition: chamfer.
- Load focus: stress relief.
- Manufacturing focus: tool radius.

### 12. T-Junction

Selected structure: three spans sharing a point.

Topology: `T` relation with a stem and two side participations.

Focus: shared point.

Flow: determined by carrier faces. If stem feeds side spans, it is a split. If
side spans feed stem, it is a merge. If no flow is chosen, it is an agnostic
intersection.

Parameters: branch widths, allowed directions, branch constraints, gate slots.

Variations:

- Add branch selection: valve.
- Add blockage on one branch: capped tee.
- Add equal output spans: splitter.

### 13. Y-Junction

Selected structure: one span connected to two angled or diverging spans.

Topology: three-way join with branch directions not necessarily orthogonal.

Focus: shared origin or branch pair.

Flow: split or merge depending carrier orientation.

Parameters: branch angle, flow ratio, branch capacity, selected branch focus.

Variations:

- Symmetric branch split: wye.
- Asymmetric branch split: diverter.
- Reverse focus: collector.

### 14. Cross

Selected structure: two spans crossing at a site.

Topology: `X` can mean connected exchange, disconnected overlap, or relation
crossing depending whether the shared site is real.

Focus: crossing point.

Flow: agnostic until route relations are attached.

Parameters: pass-through, exchange, clearance, priority, or block relation.

Variations:

- No relation, just co-location: overlap.
- Relation allows transfer: junction.
- One route blocked: crossing with stop.
- Orthogonal clearance: overpass or underpass.

### 15. Slot

Selected structure: elongated passage with orthogonal constraint.

Topology: linear span plus width/gap participation.

Focus: allowed axis.

Flow: motion allowed along length; normal or transverse motion blocked by side
surfaces.

Parameters: length, width, clearance, end stops, friction, one-way stops.

Variations:

- Low friction: guide.
- High friction: brake or clamp slot.
- Repeated stops: ratchet path.
- Matching pin added: pin-in-slot mechanism.

### 16. Guide

Selected structure: slot, rail, track, or surface that permits one motion and
resists others.

Topology: allowed carrier span plus orthogonal block faces.

Focus: allowed motion path.

Flow: directed or agnostic along path; constrained orthogonally.

Parameters: clearance, friction, travel, end stops, alignment tolerance.

Variations:

- Closed contour guide: cam track.
- Very low friction: bearing guide.
- Intentional drag: damper or brake.

### 17. Rail

Selected structure: positive raised carrier that another structure follows.

Topology: linear or contour carrier with contact pair.

Focus: guide surface.

Flow: carried object moves along the rail; normal motion blocked.

Parameters: profile, clearance, friction, support span, repeated mounts.

Variations:

- Inverse structure: groove.
- Paired rails: track.
- Rail plus capture: slide.

### 18. Groove

Selected structure: recessed linear or contour channel.

Topology: anti-area or void-like channel inside a surface.

Focus: recess path.

Flow: can guide, retain, drain, or receive another structure.

Parameters: depth, width, bottom radius, clearance, slope.

Variations:

- Carries fluid/residue: channel.
- Carries a ring: seal groove.
- Carries sliding body: track.

### 19. Channel

Selected structure: path with side boundaries.

Topology: linear span plus side surfaces, often an anti-area inside a body.

Focus: passage interior.

Flow: directed or agnostic along the channel.

Parameters: cross-section, slope, allowed material carrier, blockage, drain
residue.

Variations:

- Human finger focus: grip groove.
- Fluid focus: drain path.
- Wire focus: cable channel.

### 20. Hole

Selected structure: anti-area or absent fill bounded by a rim.

Topology: boundary contour plus interior void/pass-through participation.

Focus: opening or passage.

Flow: may be agnostic, one-way, blocked, or observational depending relations.

Parameters: diameter, depth, through/blind behavior, axis, clearance, thread
repeat.

Variations:

- Add receiver focus: socket.
- Add fastener route: bolt hole.
- Add observation focus: aperture.
- Add pass constraint: filter pore.

### 21. Pocket

Selected structure: bounded recess with depth.

Topology: anti-area inside a surface plus rim and bottom support.

Focus: receiving volume.

Flow: object or material may enter and settle; exit may be agnostic or blocked.

Parameters: depth, wall clearance, bottom seat, drainage, grip access.

Variations:

- Fit object with low residue: nest.
- Add retention lip: catch pocket.
- Add open side: tray.

### 22. Seat

Selected structure: receiving support surface for another structure.

Topology: contact surface plus expected matching support.

Focus: settlement site.

Flow: candidate object moves into contact and leaves low residue when seated.

Parameters: contact patch, normal direction, alignment landmarks, preload,
clearance, residue tolerance.

Variations:

- Circular seat: bearing seat.
- Soft/compliant seat: gasket seat.
- Seat plus capture: socket or nest.

### 23. Nest

Selected structure: multiple receiving surfaces arranged around a candidate
object.

Topology: cradle, pocket, or seat group with matching landmarks.

Focus: object settlement.

Flow: object moves into a stable low-residue configuration.

Parameters: allowed orientations, clearance, retention, insertion path.

Variations:

- Single stable orientation: keyed nest.
- Loose fit: tray.
- Retained fit: clip or socket.

### 24. Cradle

Selected structure: partial contour or support pair under a body.

Topology: receiving arc or two opposed supports.

Focus: support from below or around.

Flow: object settles under gravity/load; escape may remain open.

Parameters: radius, support angle, contact patch, load direction.

Variations:

- Add capture lip: nest.
- Add pivot: saddle bearing.
- Add clamp: retained cradle.

### 25. Hinge

Selected structure: two bodies sharing a pivot or contour axis.

Topology: common point/axis plus relative rotation carrier.

Focus: pivot site.

Flow: rotation allowed; translation blocked or constrained.

Parameters: angle span, stops, friction, preload, pin clearance, repeated
knuckle structure.

Variations:

- Flexible material instead of pin: living hinge or flexure.
- Add detents: indexed hinge.
- Add one-way stop: ratchet hinge.

### 26. Pivot

Selected structure: point or axis around which another structure can rotate.

Topology: point-centered radial or contour relation.

Focus: origin or bearing site.

Flow: angular/contour flow allowed; radial translation resisted.

Parameters: radius, bearing clearance, angular span, friction.

Variations:

- Add two bodies: hinge.
- Add torque transfer: knob or shaft.
- Add cam contour: cam pivot.

### 27. Bearing

Selected structure: contact pair that supports load while allowing relative
motion.

Topology: opposed surfaces with normal support and tangential slip.

Focus: contact and allowed motion direction.

Flow: normal blocked, tangent low-residue.

Parameters: clearance, lubrication, friction, load, wear timeline.

Variations:

- Linear: slide bearing.
- Circular: journal bearing.
- High friction: brake.
- Intentional preload: bushing or clamp bearing.

### 28. Clamp

Selected structure: opposed surfaces intentionally holding tension around
another body.

Topology: two faces with a gap carrier and preload relation.

Focus: normal contact and retained tension.

Flow: normal motion blocked; tangential motion may be blocked by friction or
allowed by guide structure.

Parameters: preload, gap, friction, contact area, release path.

Variations:

- Add screw shortening: vise or bolt clamp.
- Add compliance: spring clamp.
- Add snap entry: clip.

### 29. Fastener

Selected structure: span between bodies that shortens or holds under tension.

Topology: relation crossing layers, usually with head/seat on one side and
receiver/thread/nut on the other.

Focus: shortening and preload.

Flow: one side moves toward the other. Settlement intentionally leaves clamp
tension.

Parameters: length, preload, pitch, thread repeat, head seat, washer support.

Variations:

- Add helical repeat: screw or bolt.
- Add permanent deformation: rivet.
- Add removable clip: latch or retainer.

### 30. Screw

Selected structure: rotation carrier coupled to axial travel.

Topology: repeat/helical relation between contour carrier and linear carrier.

Focus: rotation-to-shortening or rotation-to-translation.

Flow: rotation reads as axial motion; reverse may be allowed or resisted by
friction.

Parameters: pitch, thread count, handedness, preload, clearance, drive shape.

Variations:

- Focus on clamp: bolt.
- Focus on motion: lead screw.
- Add one-way friction: self-locking screw.

### 31. Thread

Selected structure: repeated inclined surface around a contour.

Topology: repeat surface connecting angular position to axial span.

Focus: helical route.

Flow: contour traversal projects to linear motion.

Parameters: pitch, flank angle, major/minor diameter, handedness, engagement
length.

Variations:

- External thread: positive raised repeat.
- Internal thread: matching anti-area repeat.
- Coarse/fine: resolution of axial-per-turn relation.

### 32. Detent

Selected structure: repeated stable sites on a linear or circular carrier.

Topology: repeat controller plus local settlement wells.

Focus: stable site sequence.

Flow: motion allowed between sites, but tension settles into repeated minima.

Parameters: count, pitch, threshold, retention force, release force.

Variations:

- Add tactile feedback: click.
- Add two stable sites: bistable toggle.
- Add one-way ramp: ratchet.

### 33. Snap

Selected structure: thresholded transition between two configurations.

Topology: before and after settlement sites connected by tension threshold.

Focus: transition event and residue.

Flow: accumulated tension crosses a boundary, then settles into another state.

Parameters: threshold, travel, preload, release path, hysteresis timeline.

Variations:

- Add retained state: latch.
- Add repeated sites: detent.
- Add removable capture: clip.

### 34. Latch

Selected structure: snap or catch that blocks reverse motion until release.

Topology: entry path, retained site, blocked return path, release relation.

Focus: retained state and release.

Flow: one-way entry; reverse blocked unless release parameter is activated.

Parameters: catch depth, release travel, preload, wear, allowable residue.

Variations:

- Remove release: barb.
- Add hand surface: trigger or button latch.
- Add repeated teeth: ratchet latch.

### 35. Clip

Selected structure: compliant clamp with entry path and retained state.

Topology: flexible arms, capture surface, and release/escape route.

Focus: insertion and retention.

Flow: object enters through temporary deformation; reverse blocked or resisted.

Parameters: compliance, opening size, retention force, release direction.

Variations:

- Add hard one-way catch: latch.
- Add symmetrical release: spring clip.
- Add repeated mounting sites: cable clip.

### 36. Hook

Selected structure: curved or angled capture path.

Topology: entry route and blocked escape direction around a lip.

Focus: retained path.

Flow: often agnostic until load direction is applied; under load one direction
is blocked.

Parameters: throat gap, load direction, radius, catch depth.

Variations:

- Add mating receiver: catch.
- Add asymmetric insertion: barb.
- Add closure: carabiner-like gate.

### 37. Catch

Selected structure: receiving structure for a hook, latch, or snap.

Topology: local blocked return surface and capture site.

Focus: receiver, not moving part.

Flow: incoming structure settles; reverse leaves tension until released.

Parameters: entrance width, retention depth, release path, wear.

Variations:

- With hook: hook-and-catch.
- With snap arm: snap fit.
- With repeated teeth: ratchet receiver.

### 38. Ratchet

Selected structure: repeated asymmetric stops.

Topology: repeat of ramp plus blocked face over linear or circular carrier.

Focus: one-way motion over repeated sites.

Flow: one direction passes through ramps; reverse strikes blocked faces.

Parameters: tooth count, pitch, slope, retention threshold, pawl stiffness.

Variations:

- Single tooth: latch.
- Symmetric stable teeth: detent.
- Add release carrier: reversible ratchet.

### 39. Key

Selected structure: asymmetric matching shape that allows fewer orientations
than it rejects.

Topology: alignment surfaces plus blocked nonmatching orientations.

Focus: orientation and fit.

Flow: insertion/settlement succeeds in selected orientation; other orientations
leave residue.

Parameters: key width, depth, orientation index, tolerance, insertion direction.

Variations:

- Rotary torque focus: spline.
- Linear insertion focus: keyed slot.
- Security focus: lock key pattern.

### 40. Alignment

Selected structure: two landmarks or surfaces intended to read in the same
frame.

Topology: matching points, spans, axes, or contours.

Focus: residual mismatch.

Flow: settlement attempts to reduce misalignment residue.

Parameters: tolerance span, degrees of freedom, datum order, resolution.

Variations:

- One orientation only: keying.
- Repeated possible orientations: index.
- Dynamic alignment: guide or funnel lead-in.

### 41. Mirror

Selected structure: two structures related by opposed or reflected carrier
participation.

Topology: correspondence between faces, points, or spans through an axis or
plane.

Focus: symmetry relation.

Flow: perturbation on one side can read as opposed or reflected participation
on the other.

Parameters: mirror plane, offset, allowed asymmetry, residue tolerance.

Variations:

- Exact match: symmetry.
- Opposed contact: mating pair.
- Partial match: template or gauge.

### 42. Template

Selected structure: one structure used as a read basis for another.

Topology: matching or projection relation between source and candidate.

Focus: candidate residue against template.

Flow: candidate settles or fails to settle into template relation.

Parameters: tolerance, scale, orientation, selected features.

Variations:

- Hard physical receiver: gauge.
- Design-only comparison: pattern.
- Repeated template: fixture.

### 43. Gauge

Selected structure: read surface that admits or rejects a candidate.

Topology: template plus pass/fail residue surface.

Focus: measurement or fit read.

Flow: candidate passes if residue is inside allowed span; otherwise tension or
blockage remains.

Parameters: tolerance span, resolution ladder, selected datum, wear.

Variations:

- Binary pass focus: go/no-go gauge.
- Numeric read focus: scale or caliper.
- Process focus: inspection fixture.

### 44. Scale

Selected structure: repeated marks along a carrier.

Topology: resolution ladder or repeat surface over a span.

Focus: readable position.

Flow: observer/read location projects to nearest or selected mark.

Parameters: pitch, origin, unit shape, major/minor repeat grouping.

Variations:

- Pointer added: indicator.
- Circular carrier: dial.
- Low-resolution marks only: coarse index.

### 45. Pointer

Selected structure: landmark that reads against a scale or contour.

Topology: point relation from moving structure into calibrated carrier.

Focus: indicated position.

Flow: movement of source reads as location on target scale.

Parameters: offset, calibration, resolution, parallax/view descriptor.

Variations:

- Circular scale: dial pointer.
- Slot window focus: gauge window.
- Feedback control: indicator needle.

### 46. Window

Selected structure: opening whose concept is observation rather than passage.

Topology: hole or aperture with observer/read relation.

Focus: visible selected interior.

Flow: visual or informational read passes; material flow may be blocked.

Parameters: aperture size, viewing direction, mask, selected internal carrier.

Variations:

- Moving internal scale: gauge window.
- Light route: light pipe.
- Material pass allowed: ordinary hole.

### 47. Seal

Selected structure: contact boundary intended to block passage of another
participation.

Topology: closed contact contour, gap carrier, and blocked flow basis.

Focus: leakage residue.

Flow: normal contact/preload blocks passage; leakage appears as residue.

Parameters: compression, material compliance, surface finish, pressure,
allowed leakage.

Variations:

- Soft component focus: gasket.
- Dynamic motion focus: sliding seal.
- One-way pressure focus: check valve.

### 48. Gasket Seat

Selected structure: receiving support for a compliant sealing element.

Topology: seat plus compression span plus closed boundary.

Focus: compression and leakage residue.

Flow: preload intentionally creates tension; passage across boundary is
blocked.

Parameters: compression range, groove shape, pressure, material recovery.

Variations:

- Add removable cover: lid seal.
- Add groove retention: O-ring groove.
- Add over-compression block: crush limit.

### 49. Friction Surface

Selected structure: contact pair where tangential motion leaves tension.

Topology: normal support plus tangential opposed participation.

Focus: slip residue.

Flow: normal motion blocked; tangential motion resisted.

Parameters: preload, surface texture, allowable slip, wear timeline.

Variations:

- Low friction: bearing.
- High intentional resistance: brake.
- Directional friction: clutch or ratchet-like surface.

### 50. Brake

Selected structure: friction surface used to reduce motion.

Topology: moving carrier opposed by contact/tension route.

Focus: kinetic or travel reduction through intentional tension.

Flow: attempted motion is converted into residue/tension/dissipation.

Parameters: pressure, friction surface area, heat/residue carrier, release
travel.

Variations:

- Holds position: lock.
- Controls rate: damper.
- Engages only above threshold: centrifugal or safety brake.

### 51. Spring

Selected structure: relation that stores tension with displacement.

Topology: displacement carrier coupled to stored tension carrier.

Focus: reversible settlement.

Flow: input displacement stores tension; release routes it back toward motion.

Parameters: stiffness, travel, preload, compression/tension face, stops.

Variations:

- Preloaded spring: biased mechanism.
- Thresholded spring: snap.
- Repeated wave spring: compliant stack.

### 52. Flexure

Selected structure: compliant connection allowing motion through deformation.

Topology: thin or constrained span whose orthogonal displacement stores
tension.

Focus: allowed elastic motion.

Flow: relative motion occurs without discrete sliding contact.

Parameters: thickness, length, bend direction, fatigue timeline, preload.

Variations:

- Repeated flexure: compliant mechanism.
- Thin living hinge: living hinge.
- Force focus: spring arm.

### 53. Handle

Selected structure: human contact/grip surface connected to a body transform.

Topology: grip/contact carrier plus route into motion, force, or torque.

Focus: human input site.

Flow: touch or force participation transfers into controlled movement.

Parameters: grip area, friction, clearance, leverage, allowed direction.

Variations:

- Rotation focus: knob.
- Pull focus: pull tab.
- Push focus: button or pad.

### 54. Knob

Selected structure: turnable carrier with accessible grip surface.

Topology: circular/contour carrier plus human contact participation.

Focus: torque input.

Flow: hand contact routes to rotation; optional detents or stops constrain it.

Parameters: radius, grip texture, rotation span, detent count, torque
threshold.

Variations:

- Add pointer/scale: dial.
- Add thread coupling: screw knob.
- Add fanout output: valve knob.

### 55. Button

Selected structure: push surface plus travel and threshold.

Topology: contact pad, linear displacement carrier, return/preload relation.

Focus: activation travel.

Flow: inward human force moves through travel; threshold changes output state.

Parameters: travel, spring preload, actuation threshold, return timeline.

Variations:

- Two stable sites: toggle.
- Retained after press: latch button.
- No threshold, only motion: push pad.

### 56. Toggle

Selected structure: two or more stable states with a transition path.

Topology: bistable settlement surface over a travel or angle carrier.

Focus: state transition.

Flow: input tension crosses unstable region and settles into another site.

Parameters: stable site positions, threshold, hysteresis, return relation.

Variations:

- Many repeated sites: detent.
- One-way retained: latch.
- Spring-centered: momentary switch.

### 57. Valve

Selected structure: gate relation that changes pass/block state.

Topology: passage plus movable blocker or alignment surface.

Focus: controlled passage.

Flow: allowed basis passes when gate aligns; otherwise residue/tension remains.

Parameters: opening span, direction, pressure, actuator, seal, leakage.

Variations:

- One-way pressure: check valve.
- Rotating passage: ball valve.
- Gradual opening: throttle.

### 58. Damper

Selected structure: motion path with resistance proportional or related to
speed/displacement.

Topology: allowed motion plus residue route into dissipative carrier.

Focus: rate-limiting settlement.

Flow: motion allowed, but excess or rapid change becomes tension/residue.

Parameters: damping coefficient as relation, travel span, heat/fluid residue,
directionality.

Variations:

- Motion blocked strongly: brake.
- Energy stored instead: spring.
- One-way resistance: dashpot/check combination.

### 59. Fixture

Selected structure: arrangement that holds a workpiece in selected relation to
a process.

Topology: multiple seats, clamps, stops, and datum surfaces in one group.

Focus: repeatable settlement of workpiece.

Flow: workpiece enters, aligns, and is held; process loads route through
fixture.

Parameters: datum order, clamp preload, clearance, tolerance, release path.

Variations:

- Measurement focus: gauge fixture.
- Manufacturing focus: jig.
- Assembly focus: nest plus locator.

### 60. Datum

Selected structure: reference surface, point, axis, or plane used to align
other reads.

Topology: selected carrier or group acts as projection basis for other
relations.

Focus: reference participation.

Flow: other structures read their residue relative to this basis.

Parameters: priority, tolerance, resolution, contact requirement.

Variations:

- Point datum: locator pin.
- Plane datum: seat or table.
- Axis datum: shaft, bore, or hinge line.

## Language And Metaphor

Concept surfaces should work well with language because many words are focused
reads over shared structure. The same surface can be read at several
resolutions:

```text
low resolution
  passage

more focused
  bridge, connector, funnel, reducer, filter

more parameterized
  one-way funnel with outlet axis and screen pore resolution

specific design
  injection molded hopper with ribbed wall, gasketed outlet, and removable
  filter mesh
```

Metaphorical use should preserve the same pattern when useful:

```text
bridge round
  two funding states connected through a narrowed financing passage

filter in data
  passage where selected basis passes and nonmatching samples become residue

funnel in sales
  many prospects converge through narrowing stages toward a conversion point
```

The descriptors change, but the structural ghost remains. A concept surface
can use descriptors to preserve or replace context, while the algebraic
structure keeps the relation pattern inspectable.

## Near-Term Code Direction

Potential implementation steps:

1. Add a small `ConceptSurface` helper that creates and reads the skeleton
   ordered group.
2. Keep all fields as `IElement`; do not introduce concept enums.
3. Add local reads for focus, parameter slots, and residue surfaces.
4. Reuse `StructuralPattern` for unresolved parameter slots.
5. Reuse `TimeStepSequence` for refinement witnesses.
6. Reuse `RepeatSurface` for repeated features such as teeth, detents, marks,
   threads, slots, and pores.
7. Add tests that prove low-resolution surfaces remain inspectable when
   partially parameterized.

The first executable target should be modest: build and read a concept surface
without interpreting the word. Interpretation should remain a local read over
topology, focus, flow, parameters, and settlement evidence.

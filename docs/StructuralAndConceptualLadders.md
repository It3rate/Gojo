# Structural And Conceptual Ladders

This note captures a Gojo design goal: many structures should be readable as
ladders. A ladder rung is a complete structure in its own right, but it can
also participate in a relation to a coarser or finer rung.

The central idea is refinement without replacement.

```text
rough structure
  -> refined structure
  -> detailed structure
  -> ornamented / conditioned / realized structure
```

The rough rung should remain available. A rectangle refined into rounded
corners is still readable as a rectangle. A circle refined into a crescent may
still preserve the original outer circular edge. An ornate frame may still
carry the earlier thick rounded frame and the even earlier rectangle skeleton.

This is not only a graphics concern. The same pattern appears in concepts,
routes, electronics, planning, language, equations, software, and learning.

## What A Ladder Is Not

A ladder is not just a stack of render layers. It is also not only a historical
log. A rung can be drawn as a layer, remembered as provenance, read as a lower
resolution abstraction, or crystallized into a new independent structure, but
none of those views is the whole idea.

The important property is local correspondence. A finer rung should be able to
refer to a coarser rung by structure:

```text
this corner
this edge span
this carried boundary
this removed interior
this unresolved transition
```

It should not need to rediscover the page position, global orientation, or
external rendering frame just to know what it refined. If a higher-level player
rounds a corner, routes a trace, thickens a stroke, or explains an object, it
should be able to use the lower rung as a local structural reference.

## Rung Behavior

A rung should be allowed to do three things at once:

- Stand alone as a complete structure.
- Relate to a coarser or finer rung.
- Maintain its own external relations, even when viewed as part of a larger
  ladder.

For example, a low-detail rectangle can be used as a placement reference by a
higher-detail rounded rectangle refinement. The refinement may care about the
selected corner, radius, tangent start, and tangent end, but not about the
absolute page position or final render orientation. The lower structure gives
local reference and identity, not necessarily global coordinates.

This means a ladder relation needs more than "parent" and "child". It needs
structural correspondence.

Useful evidence includes:

- source structure
- focus or selected part
- carried-forward part
- settled, replaced, or hidden part
- added detail
- correspondence bindings
- residue or tension
- result structure
- optional timeline or provenance

These should be ordinary Gojo structure, not a metadata object that owns the
meaning.

## Refinement, Abstraction, And Crystallization

There are at least three important modes.

### Provenanceful Refinement

The higher rung explicitly carries the lower rung forward.

```text
source rectangle
  focus: one sharp corner
  carry: adjacent edge interiors and other corners
  detail: tangent points and circular arc
  residue: any radius/tangent mismatch
  result: rounded-corner boundary
```

The higher structure remembers which lower structure it refined.

### Abstraction Read

A lower-resolution structure is inferred from a higher-resolution structure.

```text
ornate frame
  read as thick rounded frame
  read as rounded rectangle
  read as rectangle
```

The read may be valid even if there is no historical provenance saying the
ornate frame was originally made from a rectangle.

### Crystallization / Forgetting

A structure may settle into a new form and forget its construction path.

```text
rounded rectangle with remembered rectangle source
  -> crystallized rounded rectangle
```

The source can be forgotten without making the new structure invalid. Gojo
should support provenanceful structures and provenance-free structures.

### Multiple Histories

There is not a single required path from coarse to fine. A rounded square might
have started as a square with rounded corners, a circle constrained toward four
flatter sides, a superellipse approximation, or a freehand shape later read as
a rounded square.

Gojo should allow all of these:

```text
square -> rounded square
circle-like shape -> rounded square
freehand boundary -> read as rounded square
rounded square -> crystallized rounded square
```

Different histories may produce visually similar structures. The read should
preserve provenance when it exists, but should not require provenance when a
valid abstraction read can explain the current structure.

## Relationship To Existing Gojo Ideas

Structural and conceptual ladders are a natural extension of several existing
Gojo behaviors.

### Resolution Ladders

Numeric resolution already has this shape:

```text
coarse value
  -> finer value
  -> still finer value
```

Structural ladders apply the same principle to form:

```text
coarse boundary
  -> rounded boundary
  -> thick boundary
  -> ornamented boundary
```

The lower rung is not wrong. It is a lower-resolution read.

### Tension

Tension preserves unresolved work. A refinement may know that a corner should
be rounded but not yet know the radius, tangent positions, or curve family.
That missing detail should remain inspectable.

```text
sharp corner + smoothing intent
  -> partial transition
  -> residue/tension for radius or tangent mismatch
```

### Residue

A read may explain part of a structure and leave the rest behind as residue.
This is essential for abstraction.

```text
ornate frame read as rectangle
  explained: coarse boundary
  residue: carving, stroke variation, local asymmetry
```

The residue is not waste. It is evidence for a finer rung.

### Repetition

Repetition can be seen as a ladder-like mechanism. In `3 + 3 + 3`, each step
adds information about the accumulating result:

```text
3
  -> 6
  -> 9
```

A repeated structural template likewise creates rungs where each new instance
depends on the previous output. Repetition is not the same as refinement, but
both involve source, transition, result, feedback or correspondence, and
residue.

### Read Graphs

Gojo read graphs already allow multiple compatible readings of one source. A
structure can be a path, a boundary candidate, a branch topology, and a lower
resolution concept surface at the same time. Ladders should preserve this
multiplicity rather than forcing one canonical interpretation.

### Unit Provenance

Unit shape provenance lets equal-looking values remain distinct by origin.
Ladders need the same capacity. Two rounded rectangles may look identical but
one may refine from a square and the other may abstract from a circle-like
shape. Equal render output should not erase different structural ancestry.

## Structural Encoding Direction

A refinement surface should be relation-shaped and traversable.

One possible shape:

```text
source
focus
carry
settlement
detail
correspondence
residue
result
```

But those should not become symbolic enum cases. The important requirement is
that Gojo structure can answer local questions:

- What lower structure is being refined?
- Which part is the focus?
- Which parts were carried forward unchanged?
- Which parts were hidden, removed, replaced, or anti-participating?
- Which new detail was added?
- Which sites or carriers correspond across rungs?
- What remains unresolved?
- Can a lower-resolution read be recovered?

For graphics, this might look like:

```text
rectangle boundary
  -> corner focus
  -> tangent partitions on adjacent edges
  -> arc transition
  -> rounded boundary result
```

For a crescent:

```text
circle area
  -> carry outer circular span
  -> add inner anti-area circle
  -> crescent result
```

The outer circle edge can literally remain part of the refined structure.

## Encoding Commitments

The ladder mechanism should follow the same rule as the rest of Gojo: if the
helper code disappeared, the structure should still express the concept.

That points toward a few commitments:

- Rungs are connected Gojo structures, not opaque objects.
- Correspondence is carried by relations between carriers, locations, unit
  shapes, areas, paths, or concept surfaces.
- Carry-forward, replacement, anti-participation, residue, and result should be
  inspectable as structure.
- Reads may succeed at lower resolution while preserving unexplained residue.
- Missing parameters should remain as tension or unresolved structure.
- Crystallized rungs may stand alone after provenance is forgotten.
- Competing reads should be allowed when the evidence supports more than one
  explanation.

This keeps ladders compatible with graphics, concept surfaces, repetition,
equations, and future learning systems without making a separate ladder engine
own the meaning.

## Conceptual Encoding Direction

Conceptual ladders follow the same pattern:

```text
thing
  -> furniture
  -> chair
  -> office chair
  -> black mesh office chair
  -> this exact chair
```

The lower concept is not just a label. It should correspond to structural
evidence:

- affordance
- boundary
- flow
- support
- material
- function
- part relations
- context
- uncertainty/residue

A high-detail instance can still be read at a low conceptual resolution.

```text
specific car photo
  -> car
  -> vehicle
  -> movable object
```

The abstraction read may preserve residue for color, brand, exact geometry,
damage, lighting, and pose.

## Structural Examples

### Electronics

```text
netlist
  -> logical pins and nets
  -> schematic grouping
  -> rough component placement
  -> trace routing
  -> width/clearance/vias/layers
  -> manufacturing geometry
  -> signal/thermal/EM refinement
```

Useful Gojo ideas:

- nets as node equivalence
- traces as path refinements of logical connections
- vias as layer-transition structure
- obstacles and clearance as tension/residue
- routed geometry as a refinement, not a replacement, of net intent

### Architecture

```text
room bubbles
  -> adjacency graph
  -> rough plan
  -> walls
  -> doors/windows/openings
  -> circulation paths
  -> structure/MEP
  -> construction drawings
```

Useful Gojo ideas:

- adjacency and containment reads
- wall thickness as orthogonal refinement
- openings as anti-structure
- circulation as flow lanes
- code constraints as unresolved or settled tension

### Typography

```text
stroke skeleton
  -> monoline glyph
  -> weighted stroke glyph
  -> outline glyph
  -> serifs and terminals
  -> optical corrections
  -> hinting at pixel sizes
```

Useful Gojo ideas:

- stroke order and path graph
- thickening around skeletons
- corner/terminal refinements
- hinting as resolution-specific ladder rung
- outline abstraction back to skeleton

### Mechanical Design

```text
kinematic diagram
  -> force/load paths
  -> rough linkage geometry
  -> solid bodies
  -> holes, fillets, chamfers
  -> tolerances
  -> manufacturing/toolpath detail
```

Useful Gojo ideas:

- force-flow carriers
- features as refinements of solids
- tolerance as tension bands around ideal values
- toolpaths as manufacturing refinements

### Roads And Maps

```text
node graph
  -> route graph
  -> lane graph
  -> curves and turning radii
  -> intersections/merges
  -> elevation, bridges, tunnels
  -> surveyed geometry
```

Useful Gojo ideas:

- branch/merge reads
- lane basis
- crossings versus connected intersections
- clothoids as carrier-native transition refinements
- map generalization as abstraction reads

### Painting And Drawing

```text
gesture sketch
  -> construction lines
  -> block-in
  -> value masses
  -> local color
  -> edges and details
  -> texture/final rendering
```

Useful Gojo ideas:

- indecision lines as tension
- construction structure that remains referenceable
- areas refined into color/value fields
- detail residue when reading a final painting as a sketch

### Animation

```text
story beat
  -> key pose
  -> motion arc
  -> timing chart
  -> in-betweens
  -> secondary motion
  -> deformation
  -> rendered frames
```

Useful Gojo ideas:

- time carrier
- pose correspondence
- easing curves
- motion residue between coarse and detailed timing

### Robotics And Path Planning

```text
goal
  -> topological route
  -> waypoints
  -> collision-free path
  -> velocity profile
  -> control trajectory
  -> motor commands
  -> feedback correction
```

Useful Gojo ideas:

- obstacles as anti-area
- route feasibility tension
- feedback as refinement/re-entry
- path abstraction back to intent

### 3D Modeling

```text
primitive volume
  -> blockout
  -> control cage
  -> subdivision surface
  -> creases/support loops
  -> sculpt detail
  -> retopology
  -> UV/material maps
```

Useful Gojo ideas:

- volume carriers
- surface refinement
- topology-preserving versus topology-changing rungs
- detail projection with residue

### Game Levels

```text
flow graph
  -> rooms and encounters
  -> graybox geometry
  -> navigation mesh
  -> cover/interaction points
  -> art pass
  -> lighting
  -> optimization and LODs
```

Useful Gojo ideas:

- player flow as graph
- affordances as structural evidence
- navmesh refinement
- visibility/occlusion reads
- LOD as structural resolution

## Conceptual Examples

### Object Recognition

```text
object
  -> furniture
  -> chair
  -> office chair
  -> black mesh office chair
  -> this exact chair
```

Gojo need:

- prototype participation
- feature evidence
- partial-match residue
- multiple simultaneous abstraction reads

### Language

```text
topic
  -> intent
  -> proposition
  -> sentence plan
  -> syntax
  -> words
  -> phonemes/glyphs
```

Gojo need:

- meaning-to-form correspondence
- ambiguity as tension
- paraphrase ladders
- grammar as structure

### Software

```text
goal
  -> algorithm
  -> dataflow graph
  -> module design
  -> types/interfaces
  -> code
  -> optimized code
  -> machine behavior
```

Gojo need:

- behavior carriers
- type refinement
- test/proof witnesses
- abstraction-to-implementation correspondence

### Planning

```text
desire
  -> goal
  -> strategy
  -> milestones
  -> tasks
  -> schedule
  -> execution
  -> feedback/replan
```

Gojo need:

- intention structure
- commitment strength
- time/resource carriers
- plan residue and replanning

### Learning And Explanation

```text
intuition
  -> analogy
  -> toy model
  -> diagram
  -> formal model
  -> proof
  -> transfer to new cases
```

Gojo need:

- analogy/projection reads
- simplification residue
- learner-state refinement
- multiple valid resolutions of one idea

### Medicine

```text
symptom cluster
  -> hypothesis
  -> differential diagnosis
  -> tests
  -> mechanism/pathology
  -> treatment plan
  -> response monitoring
```

Gojo need:

- evidence weighting
- competing refinements
- uncertainty/tension
- causal carrier structure

### Legal And Contracts

```text
intent
  -> obligation graph
  -> clauses
  -> definitions
  -> exceptions
  -> jurisdiction-specific wording
  -> executed document
  -> case interpretation
```

Gojo need:

- obligations and permissions as structure
- exception/anti-structure
- scope refinement
- conflict/tension reads

### Music And Performance

```text
gesture
  -> motif
  -> phrase
  -> harmony/rhythm structure
  -> arrangement
  -> performance timing
  -> recording/mix
```

Gojo need:

- motif correspondence across variations
- timing and dynamics as refinement
- improvisation residue
- abstraction from recording back to musical structure

### Scientific Models

```text
observation
  -> pattern
  -> toy model
  -> governing equation
  -> simulation
  -> calibrated model
  -> experimental prediction
```

Gojo need:

- evidence-to-model correspondence
- approximation residue
- parameter tension
- competing model reads over the same evidence

## Current Strengths

Gojo already has strong substrate for ladders:

- carriers and carrier locations
- relations as traversable structure
- unit shapes and unit provenance
- resolution reads
- residue and tension
- focus surfaces for structural selection
- repeat surfaces
- read graphs
- branch/merge topology
- area and anti-area
- orthogonal thickening
- circular and linear carriers
- carrier-native curve transitions
- `FocusSurface` for partial selection, projection, result witnesses, and
  focus timelines
- `TransformSurface` for reusable selection transforms, concrete transform
  applications, patch witnesses, and transform-application ladder steps
- `StructuralLadderSurface` for exact rung records, rung frames, timelines,
  current-rung reads, local change reads, and rungs created from transform
  applications

These are enough to start expressing real structural ladders without inventing
a separate model layer.

## Current Gaps

The main missing tools are not primitives. They are correspondence tools.

Needed:

- deeper focus projection across ladder rungs
- carry-forward relations
- replacement/settlement relations
- anti-participation for removed or hidden parts
- richer explicit result surfaces
- abstraction reads from fine to coarse
- crystallization/forgetting of provenance
- residue preservation across structural rungs
- comparison between independent ladder histories
- multi-parent and competing refinement reads
- confidence or settlement strength without symbolic tags
- local read order that can increase detail without global classification

The important rule remains: these should be Gojo-native relation surfaces, not
metadata wrappers.

`FocusSurface`, `TransformSurface`, and `StructuralLadderSurface` are the
current core steps. Focus can already hold source, focus basis, selected
participation, complement, replacement, correspondence, residue, and
provenance/timeline. It can project focus through correspondence into another
rung, create a result witness, and participate in ordered focus steps.

`TransformSurface` separates the reusable instruction from the patch that
settled on one focus. A transform has input ports, operation structure, output
ports, residue policy, and provenance. A transform application binds that
instruction to a real focus, records parameter bindings, produces a result,
and carries a patch witness. The patch witness preserves source, transform,
focus, selected participation, carried complement, replacement/introduction,
correspondence, result, residue, and provenance.

The first operation reads are deliberately small relation shapes:

- map/carry: `input -> output`
- introduce: `unknown -> output`
- remove: `input -> unknown`
- split: `input -> output ports`
- fold: `input ports -> output`
- interpolate: `first, second, grade/basis ports -> output`

These operation shapes can derive correspondence bindings during a transform
application. Explicit bindings can still be supplied, but a transform does not
need a separate metadata table to say that an output came from an input.

Executable operation surfaces now have a first output witness:

```text
OperationOutput
  application
  operation
  bound input evidence
  operation read
  output target surface
  result
  residue or tension
  provenance
```

The creator applies an application's bindings to the operation port surface,
uses the core operation reader to produce a concrete operation read, and
records the extracted output target surface. A simple read produces one target;
a product expansion produces an ordered surface of term targets. This lets
dependency discovery find a producer output even when the target is only one
element in a larger result frame. It is not a full transform solver yet; it is
the first structural witness that an operation surface produced this output
surface in this application.

Concrete example: a transform can bind fused `x` faces and fused `y` faces into
a product operation over an area carrier. The operation-output witness can expose
term targets like `area^-:8`, `area^-:10`, `area^-:12`, and `area:15`. A later
application can consume only `area^-:12`, and dependency discovery can now read
that one term as producer output instead of treating the whole result frame as
one vague dependency.

Transform applications can also be replayed against a changed focus. Replay
reuses the transform instruction, creates a new application for the new focus,
and records stale patch evidence when the previous patch no longer settles
against that focus:

```text
Replay
  previous application
  new focus
  new application
  stale patch or residue
  residue or tension
  provenance

StalePatch
  previous application
  previous patch
  previous focus
  new focus
  residue or tension
  provenance
```

This is the CAD-history behavior: the instruction survives edits upstream, and
the old patch remains inspectable as historical settlement instead of becoming
the transform itself.

Partial replay records a bounded slice of that behavior:

```text
PartialReplay
  previous application frame
  carried applications
  replay frame
  current application frame
  residue or tension
  provenance
```

The current read is intentionally ordered through the previous application
frame. Carried applications remain unchanged, replay records replace their
previous applications, and unmentioned applications are pending. This preserves
the old carried-prefix case while allowing a later independent application to
remain carried after a middle application replays.

Dependency partial replay can now expose pending dependent consumers as residue:

```text
StaleDependency
  dependency
  producer replay
  stale consumer application
  residue or tension
  provenance
```

This is a structural witness that a dependency-producing application has moved
forward while its consumer is still the previous application. It gives a partial
rung a valid current frame plus explicit tension for the missing dependent
update.

The first affected-slice discovery is focus based:

```text
FocusChange
  previous focus
  new focus
  correspondence
  residue or tension
  provenance

AffectedSlice
  previous application frame
  focus change
  carried prefix
  affected application suffix
  residue or tension
  provenance
```

The affected suffix begins at the first application whose focus matches the
previous focus in the focus-change record. This is conservative, but it is
structural: a later dependency read can refine the suffix, split it, or carry
more applications forward without changing the basic replay shape.

The affected slice can feed partial replay directly. The replay builder reads
the carried prefix from the slice and verifies that replay records replace the
affected applications in order. That removes one code-side restatement of
intent: once a focus-change read finds the affected slice, replay can follow
that structure.

Application dependencies now have an initial relation shape:

```text
ApplicationDependency
  producer application
  consumer application
  dependency evidence
  residue or tension
  provenance
```

The automatic read discovers local consumption of producer output structure.
It now walks nested operation inputs, focus participation, patch participation,
and correspondence bindings, so a producer replacement can be found inside a
consumer transform operation or correspondence frame. It deliberately does not
treat a shared source as dependency evidence by itself; dependency needs output
structure, operation-output evidence, explicit binding evidence, or a whole
producer result being consumed. The exact surface can also carry explicit
binding evidence from a producer output to a consumer input, so dependency can
be represented as structure rather than a side table. This is useful for replay
because it begins to distinguish "everything after this focus changed" from
"this later application actually consumes that earlier application."

Dependency-guided affected slices make that distinction explicit:

```text
ApplicationDependencyFrame
  dependency...

DependencyAffectedSlice
  previous application frame
  focus change
  dependency frame
  carried applications
  affected applications
  residue or tension
  provenance
```

The changed application and transitive consumers are affected. Independent
applications are carried even if they occur later in the previous frame. This
also clarified an important structural rule: focus-change evidence should
reference the actual focus relation carried by an application, not an equivalent
copy with a different root carrier.

Concrete replay example: application A produces the four area terms above,
application B consumes `area^-:12`, and application C is independent. If A is
replayed with a changed input and the consumed term becomes `area^-:16`, the
dependency-aware slice marks A and B as affected, carries C, and partial replay
can replace A and B while preserving C in the current rung.
If only A has replayed so far, the current rung can contain `A'` and C, while
the partial replay residue carries `StaleDependency(A -> B, A', B)`.

Structural ladders can now carry complete rungs, rung frames,
focus-transition timelines, transform-application timelines, current-rung
reads, local change reads, and rungs created from transform applications.

One implementation lesson is already clear: broad ordered-group reads are too
eager for crystallized ladder surfaces. A rung should read as a rung because
the exact rung surface is present, not because nested participant relations
happen to contain enough fields. Convenience constructors should also avoid
overload shapes that can reinterpret full evidence as rung lists.

The same is true for focus. A structural refinement should start from a real
focus surface. The focus surface selects participation, carries complement,
provides replacement or anti-participation, and preserves correspondence and
residue. A refined rung can then read its source from that focus rather than
duplicating the source in a separate argument. This keeps "what is selected"
inside Gojo structure instead of turning it into code-side intent.

The next work is broadening operation-output settlement. Union, projection,
multi-output operations, multi-parent patch histories, inferred dependency
evidence from live carrier settlement, and stronger replay residue are still
open.
Those should remain operation structure and witnessed residue, not symbolic
operation enums.

## Implementation Bias

The first implementation should avoid a grand ladder class. A better path is
to make one or two small refinement examples that are useful in the current
graphical work, then let the common shape harden from repeated use.

Good early reads:

- sharp corner to rounded corner through a generic transform application
- segment to thick strip
- circle to crescent
- skeleton stroke to outline stroke
- net connection to routed path

Each target should prove the same essentials: source, focus, reusable
transform, carry-forward, introduced detail, patch witness, residue, and
result. If those pieces remain traversable, the ladder idea can grow without
becoming a wrapper around Gojo.

## Near-Term Target

A good first executable ladder is:

```text
rectangle
  -> rounded rectangle
  -> thick rounded frame
  -> crescent / cutout variant
  -> ornate frame detail
```

This target exercises:

- coarse boundary preservation
- corner focus
- arc/transition refinement
- orthogonal thickening
- area and anti-area
- carry-forward edges
- residue when detail does not exactly settle
- abstraction back down to rectangle

It is small enough to implement, but rich enough to force the core ladder
questions.

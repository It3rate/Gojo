# Concept Shapes Codex

This is a design note for building primitive mechanical and physical
concept structures out of existing Gojo primitives. It follows the same rule as
the rest of Gojo:

```text
meaning lives in inspectable structure
```

The note uses the names `ConceptShape`, `ConceptSurface`, and `ConceptUnit`
as thinking handles. None of these names should imply a meaning-owning wrapper
object. A future helper can create and read ordinary Gojo relation groups, but
the graph must remain the source of meaning.

The point of this document is to strip the concept layer back down to physical
Gojo elements: carriers, carrier faces, signed locations, points, spans,
orthogonals, additive groups, products, relations, slots, repeats, residue,
tension, and timelines. Mechanical words such as bridge, funnel, filter, slot,
clamp, latch, and hinge should fall out as local reads over those structures.

## 1. Central Claim

`UnitShape` crystallizes calibrated tick meaning into a stable tick promise:

```text
one tick of this carrier participates like this
```

A primitive concept shape should do something similar at a larger scale:

```text
this local connected structure affords this kind of topology,
flow, parameter openness, and settlement
```

The analogy is useful, but a concept shape is not a unit shape. A unit shape
describes one tick. A concept shape describes a local arrangement that may
contain many carriers, relations, open sites, reads, tensions, and timelines.

The important shared move is deparameterization. A calibrated thing can be
crystallized to a lower-resolution promise while leaving detailed partition,
scale, radius, material, or precision for later.

Examples:

```text
calibrated inch carrier
  -> UnitShape for inch-like tick participation

specific bridge geometry
  -> low-resolution passage between sides through a neck

specific valve design
  -> passage with a controllable capacity partition

specific corner geometry
  -> orthogonal meeting with an unresolved transition parameter
```

The primitive concept is not the English word. The word is a read at some
resolution.

## 2. Layers Of Meaning

The concept layer should be separated into layers so the primitive vocabulary
does not become a flat table of composed behaviors.

```text
physical primitive
  carrier, face, signed site, point, span, orthogonal, product, group

structural primitive
  shared site, endpoint pair, join, boundary, closure, branch, open outlet

concept shape
  low-resolution composition of physical and structural primitives

concept surface record
  selected structure, reads, parameters, constraints, witnesses, timeline

word read
  local interpretation such as bridge, funnel, filter, slot, latch

specific design
  parameterized and settled structure beyond ordinary language
```

This keeps the small generator set clean while still letting the surface record
be rich enough to support engineering concepts.

## 3. Relation To UnitShape

`UnitShape` already performs a kind of conceptual compression. It can preserve
the source of a tick without preserving every measurement detail.

Important properties to borrow:

- The result remains inspectable.
- The result is not a symbolic tag.
- Descriptors can preserve provenance without owning algebraic meaning.
- Resolution is explicit structure, not an invisible decimal.
- Lower resolution is not failure.
- Missing detail can remain open rather than invalid.

Concept shapes should behave similarly.

For example:

```text
corner
  low resolution:
    two structures meet orthogonally

  later parameter:
    transition radius

  later settlement:
    radius driven toward zero -> sharp-ish corner
    radius resolves positive -> fillet-ish corner
    transition resolves linear -> chamfer-ish corner
```

There is no need for a primitive `SharpCorner`. Sharpness is a parameterized
read over the primitive corner structure.

## 4. ConceptSurface Record

A rich record can be useful for inspection:

```text
ConceptSurface(root)
  selected structure
  topology surface
  exposed read / focus surface
  flow or traversal surface
  parameter surface
  constraint surface
  settlement witness
  refinement timeline
```

This is an ordered group shape, not a concept class. It is acceptable for a
helper to build and read this shape:

```csharp
ConceptSurface.Create(...)
ConceptSurface.TryRead(...)
```

but the helper must not decide that a structure "is a bridge" through a hidden
enum. The word read should be local and optional.

The record should be thought of as a notebook page attached to a graph, not as
the graph's meaning.

## 5. Physical Primitive Alphabet

The strict primitive alphabet starts with what Gojo already has or nearly has.

| Primitive | Current Gojo shape | Concept role |
| --- | --- | --- |
| Carrier | `new Carrier()` | participating axis, reservoir, line, contour, value site, or conceptual basis. |
| Flow face evidence | `FlowFaceSurface.Forward/Inverse(...)` | opposed participation carried as explicit face structure, not negative value. |
| Signed location | `new CarrierLocation(C,n)` | site on a face; negative location is not inverse face. |
| Shared coordinate | same `TickRef` | same physical site, not coincidence of equal values. |
| Point | `Relation.Point(C,n)` | grounded site, landmark, focus, endpoint, boundary. |
| Directed segment | `Relation.Segment(C,a,b)` | ordered extent, traversal, projection basis, frame. |
| Orthogonal outlet | `Relation.OrthogonalOutlet(source)` | new participation escaping source order. |
| Point orthogonal | `UnitShape.CircularFrom(point)` | radial or contour potential, unmarked and unpartitioned. |
| Zero-segment orthogonal | `UnitShape.LinearFrom(zeroSegment)` | line or ray potential from collapsed basis. |
| Segment orthogonal | `UnitShape.SheetFrom(segment)` | sheet, panel, or area-like potential. |
| Product | `UnitShape.Product(...)` | non-aligned combined participation. |
| Opposed unit face | `UnitShape.OpposedFaceOf(...)` | opposed kind, anti-participation seed. |
| Resolution | `UnitShape.ResolutionOf(...)` | coarse tick promise. |
| Additive group | `Relation.OrderedGroup(...)` | gathered participants or selection. |
| Projection relation | segment-to-segment relation | scale, map, adapter, reducer/expander seed. |
| Reset/boundary relation | point/segment return relation | wrap, clamp, carry, boundary, residue exposure. |
| Placeholder | `Placeholder` | open parameter or unresolved concept site. |
| Descriptor slot | `UnitShape.DescriptorSlot(n)` | algebraic provenance placeholder. |
| Repeat surface | `RepeatSurface` | repeated primitive: teeth, holes, detents, pores, threads. |
| Time sequence | `TimeStepSequence` | refinement, traversal, use, and settlement witness. |

Anything that cannot be made from this alphabet should be treated as a missing
Gojo structure or as a higher-level read, not smuggled in as a concept enum.

## 6. Four Signed Face Quadrants

Gojo should keep three ideas separate:

```text
carrier face
signed site on that face
traversal law attached to structure
```

This produces four signed face quadrants:

| Quadrant | Shape | Conceptual reading |
| --- | --- | --- |
| forward positive | `L(C,+n)` | ordinary forward participation. |
| forward negative | `L(C,-n)` | participation located against the local goal or flow. |
| inverse positive | `L(C*,+n)` | opposed participation on the inverse face. |
| inverse negative | `L(C*,-n)` | opposed participation against its face's local flow. |

The forward negative site is not the same as an inverse positive site.

One useful intuition:

```text
if the goal of a line is to move toward the end of its flow,
negative sites get larger as they move farther away from that goal
```

This lets Gojo express constraints without symbolic signs:

```text
positive-only span
  unsigned amount, permitted growth, open capacity, forward travel

negative-only span
  deficit, retreat, distance from goal, behind-origin pressure

inverse-positive span
  opposed material participation or reverse face

inverse-negative span
  opposed deficit, reverse-side lack, or against-opposed-flow site
```

Concept structures can use any one quadrant or combine quadrants. A one-way
constraint may be expressible by exposing only one quadrant. A reduction can be
expressed as participation against an input span. An interference read may
appear as negative gap participation rather than as a failed boolean.

### Constructing Unsigned And Signed Quantities

A uint-like carrier can be modeled by exposing only the positive face or
positive segment:

```text
allowed parameter:
  Segment(C,0,n) on the forward face
```

A signed number-like structure can be modeled by pairing forward positive and
forward negative participation:

```text
positive side:
  Segment(C,0,+n)

negative side:
  Segment(C,0,-n)
```

That is not the same as a carrier and its inverse face. The inverse face is
opposed participation, not merely negative scalar notation.

This matters for concept shapes:

```text
clearance
  positive gap site

interference
  negative gap site

opposed surface
  inverse face contact

reverse-side interference
  inverse negative gap site
```

## 7. Sites, Points, And Shared Coordinates

A `CarrierLocation` is a signed site. A `Relation.Point(C,n)` grounds that site
on its carrier.

For concept shapes, points act as:

- focus sites
- endpoints
- origins
- boundaries
- landmarks
- contact candidates
- attach points
- pivot sites
- closure sites

Equal numeric ticks are not enough to mean shared physical structure. If two
points should be the same physical site, they should share a `TickRef` or be
connected by explicit relation structure.

```text
same numeric value:
  two sites happen to read 10

same TickRef:
  two locations share one coordinate reference
```

This is crucial for:

- joins
- corners
- closure
- hinges
- matching surfaces
- alignment
- datums
- repeated marks that must move together

A concept shape should use shared coordinates when the claim is physical
identity, and relation/projection when the claim is correspondence.

## 8. Extents And The Missing Unoriented Span

`Relation.Segment(C,a,b)` is directed. That is correct for projection and
traversal. Many concept seeds, however, need a weaker structure:

```text
there is an extent between two sites
but no start and end have been chosen
```

This is different from:

```text
Segment(C,a,b)
```

because the segment commits order.

Current approximations:

```text
endpoint pair:
  OrderedGroup(root, Point(C,a), Point(C,b))

declared reversible extent:
  OrderedGroup(root, Segment(C,a,b), Segment(C,b,a))

boundary span:
  Segment(C,min,max) used as an envelope rather than a traversal
```

The first is closest to a primitive undirected extent. It treats the two points
as an unordered or not-yet-ordered pair.

Ordering an extent is a design act:

```text
unordered extent
  -> directed segment

unordered bridge passage
  -> one-way funnel

unordered group
  -> ordered contour traversal
```

This act can be recorded in a refinement timeline. It may be a parameter
binding, a local read decision, or a settlement event.

## 9. Unordered Before Ordered

Gojo's current ordered/additive groups preserve construction order. That is
useful, but many concept surfaces start as unordered incidence:

```text
these sites participate together
but no traversal order has been selected
```

Examples:

- two endpoints of an unoriented extent
- several candidate branch outlets before flow is chosen
- a set of contact patches
- matching landmarks before datum order is chosen
- attachment options around a neck

The important distinction:

```text
unordered membership
  participants are present together

ordered traversal
  participant sequence has become meaningful

declared reversible traversal
  both directions are structurally available

agnostic traversability
  traversal exists, but no direction law is attached
```

Agnostic is not reversible. Reversible is a stronger claim.

## 10. Orthogonal Generation

Orthogonal structure is one of the strongest bridges between current Gojo and
concept shapes.

### Point Orthogonal

```text
source:
  Point(C,0)

orthogonal:
  OrthogonalOutlet(Point(C,0))
```

`UnitShape.CircularFrom(point)` gives a contour, radial, or level-line carrier
seed. It does not yet know radius, direction, start marker, or mesh behavior.

Conceptual readings:

- radial potential
- unmarked contour
- possible perimeter
- fanout seed
- inflow seed
- field around a point
- pivot-centered possibility

A single point gives unmarked radial/contour potential. Later structure can
calibrate:

```text
radius
  relation from center/radial endpoint to perimeter marker

start marker
  point on the contour

direction
  relation to another marker, contour, handed structure, or repeated order

partition
  repeat or resolution structure around the contour
```

### Zero-Segment Orthogonal

```text
source:
  Segment(C,0,0)

orthogonal:
  OrthogonalOutlet(Segment(C,0,0))
```

`UnitShape.LinearFrom(zeroSegment)` gives a line or ray seed. It has a collapsed
basis, which is more directed than a bare point but still not fully measured.

Conceptual readings:

- line potential
- ray seed
- guide axis
- possible slot axis
- zero-width hinge or pivot line

### Segment Orthogonal

```text
source:
  Segment(C,a,b)

orthogonal:
  OrthogonalOutlet(Segment(C,a,b))
```

`UnitShape.SheetFrom(segment)` gives sheet, panel, or area potential.

Conceptual readings:

- sheet
- face
- panel
- wall
- side field
- region seed
- surface normal possibility

### Radial Fanout vs Group Fanout

Do not collapse these:

```text
additive fanout
  many participants attached to one holder

radial fanout
  orthogonal potential from a point

branch fanout
  one shared site connected to multiple spans
```

They can all be read as "fanout" in language, but they are different Gojo
structures.

An additive group gathers participation. A radial outlet creates physical or
topological potential. A branch join connects actual routes.

## 11. Products, Area, And Anti-Participation

When two non-aligned positive carriers combine, the result can be area-like or
interaction-like:

```text
positive carrier A
+ positive carrier B
  -> product participation
```

The low-resolution concept is not yet a grid, rectangle, polar sector, sampled
field, or measured area. It is simply combined non-aligned participation.

Opposed or inverse participation creates anti-area or anti-fill-like seeds:

```text
positive A * inverse B
  -> opposed product participation

positive region + opposed/void participation
  -> hole, pocket, cut, absence, subtractive feature seed
```

Absence should not be a bare missing object. It should be read over envelope,
boundary, opposed participation, or anti-fill structure.

## 12. Additive Groups And Selection

Additive or ordered groups are useful for gathered participants:

```text
OrderedGroup(root, A, B, C)
```

Current groups preserve order. A read may ignore order, but the structure still
has construction order. This is acceptable for many surfaces, but a future
unordered-group helper would make concept seeds cleaner.

Group roles:

- selection
- participant gathering
- side membership
- additive property set
- candidate options
- repeated evidence
- local focus record

Groups are not physical radial flow. They are attached participation. Use
orthogonals or branch topology when the concept needs physical expansion.

## 13. Relation Laws

The primitive relation laws should not duplicate settlement outcomes. Use
relation laws for structural possibilities:

| Relation law | Gojo shape family | What it can seed |
| --- | --- | --- |
| Gather | additive/ordered group | selection, attribute set, sides, evidence. |
| Project / scale | segment-to-segment relation | adapter, reducer, expander, gauge, read. |
| Oppose | inverse face or opposed span relation | contact, cancellation, anti-fill, conflict. |
| Reset / boundary | point or segment return | wrap, clamp, residue, counter, capacity. |
| Branch | shared site to multiple spans | split, merge, manifold, junction. |
| Close | endpoint/origin closure | contour, loop, cycle, enclosure. |
| Repeat | repeat surface/controller | teeth, detents, threads, pores, marks. |
| Timeline | time step sequence | refinement, process, settlement witness. |

Do not make `pass relation` and `block relation` separate relation kinds if
`pass` and `block` also appear as settlement outcomes. Instead:

```text
relation law:
  project, oppose, reset, branch, close

settlement outcome:
  passes, blocks, stores, routes, cancels, leaves residue
```

## 14. Topological Assembly

Topology comes from actual connection.

| Topology | Construction |
| --- | --- |
| Shared point | several structures use the same point or shared `TickRef`. |
| Endpoint join | one extent endpoint is physically connected to another endpoint. |
| Origin join | several branches originate at the same site. |
| Continuation | end-to-origin or aligned projection. |
| Turn | continuation through orthogonal or angled structure. |
| Branch | one site connected to multiple extents or outlets. |
| Merge | branch topology plus inward traversal law. |
| Split | branch topology plus outward traversal law. |
| Crossing | co-located structures, with or without exchange relation. |
| Closure | endpoint returns to origin, or boundary reset loops. |
| Envelope | boundary plus inside/outside or fill/void relation. |
| Passage | two sides connected through a local point/span. |
| Open outlet | attachable site with expected flow but no receiver yet. |

Topology is not a word. A `T` topology can become split, merge, junction,
inlet, outlet, valve, or blocked branch depending flow, focus, parameters, and
witnesses.

## 15. Traversal And Flow

Traversal is an attached structural law, not the same thing as carrier face.

Layers:

```text
carrier face
  forward or inverse

signed site
  positive or negative location on that face

traversal law
  agnostic, directed, declared reversible, blocked, controlled
```

Useful traversal states:

| State | Meaning |
| --- | --- |
| Agnostic traversable | connection exists; no preferred direction is encoded. |
| Directed | one traversal direction has structure. |
| Declared reversible | both directions are explicitly available as one surface or paired routes. |
| One-way | directed route exists; reverse leaves residue or has no receiver. |
| Controlled | parameter or gate controls traversal resolution/capacity. |
| Material transfer | source participation is routed or drained. |
| Observation read | source is read without material drain. |

Material transfer and observation are not the same. A sensor tap and a drain
can share topology while settling differently.

## 16. Focus As Exposed Read

Focus does not always need a permanent field. Often the focus is simply the
read being exposed.

Examples:

```text
read bridge neck capacity
  the bridge is being used as choke-relevant

read bridge pass basis
  the bridge is being used as filter-relevant

read bridge output fanout
  the bridge is being used as nozzle/sprayer-relevant

read corner transition parameter
  the corner is being used as fillet/chamfer/sharpness-relevant
```

An explicit focus surface is still useful when a concept surface is stored for
later inspection:

```text
focus surface:
  Point(neck,0)

or:
  Segment(slotAxis,a,b)

or:
  read relation into a capacity carrier
```

Writable focus parameters are stronger. They do not only observe the structure;
they can control it.

```text
readable capacity
  notice bridge capacity

writable capacity parameter
  control bridge capacity, making valve/filter/choke behavior explicit
```

## 17. Absence, Omission, Void, And Open Outlet

Concept shapes need to distinguish several cases that look similar in ordinary
code:

| State | Meaning |
| --- | --- |
| Omitted | no structure is present or implied. |
| Open / unimplemented | an attachment site exists; expected flow has no receiver yet. |
| Void / anti-participation | explicit opposed, empty, subtractive, or anti-fill structure. |
| Unknown wildcard | a read placeholder such as `Carrier.Unknown`, not a physical open site. |
| Stable site potential | an input-side attach point where future perturbation may settle. |

Example:

```text
collector region -> neck
```

If there is no outlet structure, this might mean:

```text
omitted output
  the design does not speak about output

open outlet
  the neck expects a receiver later

void outlet
  flow leaves into explicit absence or anti-fill

unknown receiver
  a wildcard read frame, not a physical attachment site
```

These should not be collapsed.

An open outlet is especially useful:

```text
open outlet
  site gathers unresolved orthogonal or directed tension
  later attached structure can receive that flow
```

This is close to "there is a socket for meaning here." It should be visible as
an attachment site, not hidden as an omitted argument.

The reverse idea also matters:

```text
open inlet / stable site potential
  site is ready to receive future input and settle it
```

Seats, detents, sockets, and nests often start as input-side open sites before
anything has used them.

## 18. Parameters And Constraints

Parameters are open Gojo structure:

- placeholder
- descriptor slot
- unbound carrier
- unbound endpoint
- unresolved span
- controller
- resolution ladder
- open attachment site

Constraints should be expressed by faces, spans, relations, resolution, and
settlement rather than symbolic predicates.

Examples:

```text
nonnegative length
  expose positive face only

deficit
  expose negative site on forward face

opposed amount
  expose inverse face

bounded amount
  Segment(parameter,min,max)

reduction
  output relation uses against-input or inverse/negative participation

capacity
  partition a whole passage or expose a boundary/reset relation

one-way
  provide one directed traversal and leave reverse unresolved or blocked

filter
  provide pass basis and preserve nonmatching residue
```

Parameterization can change the natural word read:

```text
bridge + inward directed flow
  funnel

bridge + outward fanout
  sprayer

bridge + capacity read
  choke point

bridge + writable capacity
  valve

bridge + pass basis
  filter

corner + transition radius -> 0
  sharp corner

corner + positive radial transition
  fillet

corner + linear transition span
  chamfer
```

## 19. Ticks, Partitions, And Whole Resolution

Ticks and partitions should not be confused.

```text
3 ticks
  three units of participation

3 partitions
  one whole divided into three addressable parts
```

If a carrier has value `3`, that does not tell us whether it is `3 out of 10`
or `3 out of 100`. It is count, not proportion.

A partition requires a whole:

```text
whole span
  Segment(C,0,whole)

partition count
  divide the whole into n addressable parts
```

For concept shapes this matters because many low-resolution parameters are
about whole surfaces:

```text
valve capacity
  whole passage partitioned into coarse open states

switch
  two coarse partitions: off/on or closed/open

valve
  three or more coarse partitions: low/medium/high or increasingly open

detent
  contour whole partitioned into repeated stable sites

scale
  whole measure partitioned into readable marks
```

If there are no partitions, a single tick may represent the whole concept at
the current resolution. If a measurement is `3 inches at 1/8 inch resolution`,
the tick represents an eighth-inch, not a third of a whole.

Concept shapes should state whether they are using:

```text
count ticks
  amount of unit participation

whole partitions
  resolution of one bounded whole
```

## 20. Capacity By Coarse Partition

Capacity does not need exact precision at the primitive layer. A valve can be
represented as a passage with a coarse partitioned capacity surface.

Examples:

```text
switch-like:
  2 partitions
  closed/open

valve-like:
  3 partitions
  low/medium/high

fine valve:
  n partitions or resolution ladder
```

The purpose of the first few partitions is not to claim exact values. It is to
say:

```text
this concept is variable and can refine
```

A capacity surface can later become:

- fixed switch
- valve
- filter pore resolution
- choke capacity
- adjustable nozzle
- gauge scale
- tolerance band

## 21. Settlement Outcomes

Settlement outcomes are not topology primitives, but they are essential
witnesses.

Useful outcomes:

| Outcome | Meaning |
| --- | --- |
| Pass | perturbation projects through with low residue. |
| Block | perturbation cannot project through available structure. |
| Route | tension moves to another carrier. |
| Store | tension remains intentionally available. |
| Cancel | opposed participation reduces before residue remains. |
| Settle | structure reaches a stable local site. |
| Release | stored tension routes after threshold or unlock. |
| Latch | reverse route remains blocked after entry. |
| Residue | read remainder remains visible. |
| Tension | transferred unresolved change remains visible. |

Composed concepts:

```text
friction
  contact + tangent attempt + stored slip tension

preload
  contact/gap relation + intentional stored tension

filter
  passage + pass basis + nonmatching residue

detent
  repeat + stable settlement sites + threshold/residue witness

snap
  threshold + transition timeline + release/settlement witness

latch
  one-way entry + blocked reverse + release route
```

These should not be in the generator list. They should be built.

## 22. Refinement Timelines

Concepts are often processes of thought and design, not static snapshots.

A refinement timeline can record:

```text
initial low-resolution shape
  bridge-like passage

step 0
  expose capacity read

step 1
  bind capacity partition count

step 2
  perturb with flow demand

step 3
  preserve residue at neck

step 4
  refine as choke, valve, or filter
```

Use `TimeStepSequence` style structure:

```text
time span
before evidence
transition evidence
after evidence
residue evidence
```

This allows concept solving to look like equation solving:

```text
low-resolution seed
  -> local reads
  -> residue/tension
  -> parameter binding
  -> narrowed shape
  -> higher-resolution design
```

## 23. Assembly Recipe

To build a primitive concept shape:

1. Choose the physical atoms.

```text
carrier faces
signed sites
points
spans or endpoint pairs
orthogonal outlets
products
groups
```

2. Decide whether each extent is unordered, directed, or reversible.

```text
endpoint pair
directed segment
paired reversible segments
```

3. Connect topology through shared sites or relations.

```text
shared TickRef
shared point
end-to-origin relation
orthogonal relation
boundary relation
closure relation
```

4. Expose reads instead of labels.

```text
capacity read
gap read
pass basis read
residue read
transition read
```

5. Add open parameters only where the primitive needs a future refinement.

```text
unbound endpoint
placeholder receiver
partition count
threshold carrier
repeat controller
```

6. Preserve use or refinement evidence.

```text
residue
tension
timeline
selected sample
settlement witness
```

7. Let word reads be local.

```text
same graph + different read = different word
```

## 24. Example: Bridge Family

### Primitive Passage

Low-resolution structure:

```text
side A
side B
neck point or neck extent
side A connected to neck
side B connected to neck
```

No preferred traversal is required.

Concept read:

```text
bridge / passage / connector seed
```

### Bridge

Focus:

```text
neck
```

Flow:

```text
agnostic traversable
```

No capacity, direction, or side size is required.

### Connector

Focus:

```text
neck as transfer element
```

Side shape is less important than attachment.

### Funnel

Add:

```text
input side radial-in or branch-in relation
directed traversal toward neck
optional open outlet at neck
```

If the outlet is omitted, this may read more like a collector or peninsula. If
an open outlet is present, it says the funnel expects a receiver.

### Sprayer

Reverse focus:

```text
neck -> output radial fanout
```

Add output spread parameter for nozzle behavior.

### Filter

Add:

```text
pass basis
nonmatching residue witness
```

The passage is not the filter. The filter is passage plus selective read.

### Choke

Add:

```text
capacity partition
demand perturbation
residue/tension at neck
```

The choke word comes from the capacity read and tension witness.

### Valve

Add writable capacity:

```text
capacity parameter
coarse partition: closed/open or low/medium/high
```

The valve can refine from switch-like to continuously adjustable by increasing
resolution.

## 25. Example: Corner Family

### Primitive Corner

Low-resolution structure:

```text
span or extent A
span or extent B
shared endpoint or origin
orthogonal or angled relation
```

Flow is unspecified. It may be a boundary change, turn, stress site, hinge
candidate, or visual corner.

### Sharp Corner

Do not create a primitive sharp-corner tag.

Add:

```text
transition parameter
perturbation toward zero
```

Sharpness is a read over the transition parameter.

### Fillet

Add:

```text
positive radial transition
radius parameter
arc or contour relation
```

### Chamfer

Add:

```text
linear transition extent
offset parameters on both legs
```

### Hinge Candidate

Add:

```text
shared pivot site
rotation/contour potential
translation constraint
```

The same primitive corner can refine toward a hinge under a different read.

## 26. Example: Slot Family

### Primitive Slot

Structure:

```text
unoriented or directed length extent
orthogonal gap/width extent
side boundaries
```

Flow:

```text
motion allowed along length
motion constrained across width
```

### Guide

Add:

```text
candidate moving element
gap read
low-residue traversal along length
```

### Clamp Slot

Add:

```text
gap reduction
opposed contact
stored tension
```

### Ratchet Slot

Add:

```text
repeated asymmetric stops
directed traversal
blocked reverse witness
```

### Keyed Slot

Add:

```text
asymmetric matching surface
orientation residue for nonmatching entry
```

## 27. Example: Fastener Family

### Primitive Shortening Relation

Structure:

```text
body A
body B
span between bodies
relation that moves one endpoint toward the other
```

Flow:

```text
shortening
```

Settlement:

```text
intentional stored tension across contact surfaces
```

### Clamp

If the focus is opposed surfaces and preload:

```text
clamp
```

### Screw / Bolt

Add:

```text
repeat/helical relation
rotation carrier
axial travel carrier
seat/head contact
thread receiver
```

### Rivet

Add:

```text
one-way deformation
no normal release path
```

The primitive is not a fastener tag. It is shortening plus settlement.

## 28. Example: Radial Family

### Radial Potential

Structure:

```text
Point(center,0)
OrthogonalOutlet(Point(center,0))
```

No radius, direction, or partition exists yet.

### Circle / Contour

Add:

```text
contour carrier from point orthogonal
radius calibration
optional marker on contour
```

### Arc

Add:

```text
start marker
end marker
possibly direction relation
```

### Detent Ring

Add:

```text
repeat controller on contour
stable site witness
threshold/release evidence
```

### Nozzle Spread

Use radial-out fanout from a neck:

```text
neck point
output radial/arc extent
spread parameter
```

### Funnel Intake

Use radial-in convergence:

```text
collector arc or region
directed relation toward neck
```

## 29. Example: Contact Family

### Contact Seed

Structure:

```text
surface A
surface B or B inverse face
gap carrier or shared contact site
```

Contact can be unresolved before use.

### Clearance

Read:

```text
positive gap
```

### Interference

Read:

```text
negative gap or opposed residue
```

### Bearing

Add:

```text
normal support
tangential traversal
low slip residue
```

### Friction

Add:

```text
tangential motion attempt
stored slip tension
```

### Seal

Add:

```text
closed boundary
blocked passage basis
leakage residue read
```

## 30. Example: Repeat Family

Repeat is not a named concept. It is a primitive controller skeleton.

### Teeth

```text
selected tooth shape
repeat controller around contour
contact/read relation
```

### Detents

```text
selected stable site
repeat controller
settlement threshold
```

### Threads

```text
selected inclined surface
repeat around contour
projection to axial travel
```

### Pores

```text
selected pass/void site
repeat over sheet or boundary
pass basis residue
```

### Marks

```text
selected readable point
repeat over scale carrier
observer read
```

## 31. Word Reads

Words should be local reads, not enum values.

Example:

```text
base structure:
  two sides connected through a neck

focus neck, no direction:
  bridge

focus transfer element:
  connector

focus input, inward flow:
  funnel

focus output, outward radial flow:
  sprayer

focus pass basis:
  filter

focus capacity residue:
  choke

focus writable capacity:
  valve
```

This allows one graph to support several valid reads. The read resolution and
exposed focus decide the word.

## 32. Descriptor Re-Grounding

Descriptors can carry context without owning meaning. The same algebraic ghost
can be re-grounded:

```text
physical bridge
  two regions connected by a passage

bridge round
  two funding states connected by a financing passage

data filter
  samples pass through selected basis and nonmatching samples become residue

sales funnel
  many candidates converge through narrowing stages
```

The descriptors change. The structural pattern remains inspectable.

A concept shape may use descriptors to remember where the metaphor came from,
but the relation graph must still carry the read.

## 33. Missing Core Gojo Structure

This section lists missing or weak structures that would make concept shapes
cleaner. These are proposed extensions, not current requirements.

### 33.1 Unoriented Extent

Problem:

```text
Relation.Segment(C,a,b)
```

commits direction. Many primitive concepts need extent without start/end.

Proposed structure:

```text
UnorientedExtent(root, pointA, pointB)
  OrderedGroup(root, pointA, pointB)
```

The helper read treats the two endpoints as an unordered pair. Internally this
can still be an ordinary relation group.

Implementation sketch:

```csharp
public static Relation Extent(Carrier root, Relation endpointA, Relation endpointB)
public static bool TryReadExtent(Relation surface, out Relation endpointA, out Relation endpointB)
public static Relation Orient(Relation extent, bool reversed = false)
```

Use:

- primitive span-as-one-thing
- corner legs before traversal is chosen
- bridge neck before direction is chosen
- focus range without flow
- datum distance without measurement direction

Replaces:

- ad hoc endpoint ordered groups
- pretending every extent is a directed segment

Extends:

- `Relation.Segment` by giving a lower-resolution precursor.

### 33.2 Unordered Group Read

Problem:

Current ordered/additive groups preserve order. Some concept seeds need
membership without traversal.

Proposed structure:

```text
participants attached to holder
read ignores order and duplicate traversal
```

Implementation sketch:

```csharp
public static Relation UnorderedGroup(Carrier root, params IElement[] members)
public static bool TryReadUnorderedMembers(Relation group, out IElement[] members)
```

This does not require a hidden set. The helper can build ordinary attachment
relations and provide an order-neutral read.

Use:

- contact patch set
- candidate attachment options
- unordered endpoints
- datum candidates
- feature evidence before traversal

Replaces:

- relying on construction order when order has no meaning

Extends:

- `OrderedGroup` and additive participation with an explicit order-neutral
  read.

### 33.3 Declared Reversible Traversal

Problem:

Agnostic traversable and reversible are different. Reversible means both
directions are intentionally available.

Proposed structure:

```text
ReversibleTraversal(root)
  forward segment/relation
  reverse segment/relation
  shared endpoints
```

Implementation sketch:

```csharp
public static Relation Reversible(Carrier root, Relation forward, Relation reverse)
public static bool TryReadReversible(Relation surface, out Relation forward, out Relation reverse)
```

Use:

- two-way connector
- bidirectional guide
- reversible hinge read
- declared two-way road-like bridge

Replaces:

- treating absence of direction as two-way flow
- duplicating route pairs with no shared surface

Extends:

- directed `Segment` and projection relations.

### 33.4 Open Attachment Site

Problem:

Omitted structure, unknown wildcard, void, and open receiver currently blur
together.

Proposed structure:

```text
OpenAttachment(root)
  site
  expected incoming/outgoing surface
  receiver slot
  unresolved residue/tension site
```

This says:

```text
flow is expected here
but no receiver has been installed yet
```

Implementation sketch:

```csharp
public static Relation OpenOutlet(Carrier root, IElement site, IElement expectedFlow)
public static Relation OpenInlet(Carrier root, IElement site, IElement expectedFlow)
public static bool TryReadOpenAttachment(...)
```

Use:

- funnel outlet waiting for conduit
- socket waiting for plug
- stable site potential
- sensor port waiting for observer
- future branch connection

Replaces:

- using `Carrier.Unknown` as if it were a physical open socket
- omitting output and losing the distinction between absent and unimplemented

Extends:

- placeholders, descriptor slots, and orthogonal outlets.

### 33.5 Explicit Void / Anti-Fill Surface

Problem:

Absence can mean omitted, unresolved, or explicit void. A hole needs explicit
anti-participation or bounded absence.

Proposed structure:

```text
VoidSurface(root)
  boundary
  opposed/anti-fill participation
  optional pass-through axis
```

Implementation sketch:

```csharp
public static Relation Void(Carrier root, IElement boundary, IElement antiFill)
public static bool TryReadVoid(...)
```

Use:

- holes
- pockets
- cutouts
- negative space
- excluded regions

Replaces:

- treating missing fill as a hole

Extends:

- product/opposed unit shapes and boundary reads.

### 33.6 Whole Partition Surface

Problem:

Tick count and partition count are easy to confuse.

Proposed structure:

```text
PartitionSurface(root)
  whole surface/span
  partition axis
  partition count/controller
  optional selected partition
```

Implementation sketch:

```csharp
public static Relation Partition(Carrier root, IElement whole, Carrier axis, long count)
public static bool TryReadPartition(...)
```

Use:

- valve positions
- detent count
- scale marks
- coarse capacity
- fractional read of a whole concept

Replaces:

- raw count spans when the intended meaning is whole division

Extends:

- `ResolutionLadder`, `RepeatSurface`, and `UnitShape.ResolutionOf`.

### 33.7 Quadrant Helpers

Problem:

The four signed face quadrants are already expressible, but easy to misuse.

Proposed helper layer:

```csharp
FaceSite.ForwardPositive(C,n)
FaceSite.ForwardNegative(C,n)
FaceSite.InversePositive(C,n)
FaceSite.InverseNegative(C,n)
```

or relation helpers:

```csharp
Relation.FacePoint(carrier, face, signedTicks)
Relation.FaceSpan(carrier, face, startTicks, endTicks)
```

Use:

- nonnegative constraints
- deficit reads
- opposed participation
- interference
- reverse-side pressure

Replaces:

- collapsing negative ticks and inverse faces

Extends:

- `CarrierLocation`, `Carrier.Inverse`, and `Relation.DirectedSegment`.

### 33.8 Read-As-Focus Surface

Problem:

Focus is often the read being exposed, but stored concept surfaces sometimes
need to preserve that read.

Proposed structure:

```text
ReadFocus(root)
  selected structure
  read surface
  result or residue carrier
```

Implementation sketch:

```csharp
public static Relation ReadFocus(Carrier root, IElement selected, Relation read)
public static bool TryReadFocus(...)
```

Use:

- capacity-focused bridge
- residue-focused choke
- pass-basis-focused filter
- transition-focused corner

Replaces:

- separate focus metadata

Extends:

- live outputs, equation reads, and concept-surface records.

### 33.9 Material Transfer vs Observation Route

Problem:

Same topology can mean material transfer or observation.

Current foothold:

```text
AttachLiveOutput
```

is closer to observation/read than material drain.

Proposed distinction:

```text
MaterialRoute
  source tension/value can be consumed or redistributed

ObservationRoute
  source is read without material drain
```

Implementation sketch:

```csharp
Relation.MaterialRoute(input, outputPort)
Relation.ObservationRoute(input, outputPort)
```

Use:

- sensor tap vs drain
- gauge vs outlet
- visual window vs hole
- measurement fixture vs cutting fixture

Replaces:

- treating all fanout as material distribution

Extends:

- live output and settlement route relations.

### 33.10 Capacity / Saturation Surface

Problem:

Chokes, valves, filters, queues, and overloads need capacity without boolean
pass/fail tags.

Proposed structure:

```text
CapacitySurface(root)
  passage or receiver
  whole capacity
  partition/resolution
  overflow/residue carrier
```

Implementation sketch:

```csharp
public static Relation Capacity(Carrier root, IElement passage, IElement wholeCapacity, IElement overflow)
public static bool TryReadCapacity(...)
```

Use:

- choke point
- valve opening
- maximum load
- queue
- filter pore capacity
- overload protection

Replaces:

- `Allow` / `Block` as primitive flags

Extends:

- boundary/reset relations, partition surfaces, and tension reads.

### 33.11 Stable Site Potential

Problem:

Gojo can witness settlement after perturbation, but representing a site as a
potential well before use is still fuzzy.

Proposed structure:

```text
StablePotential(root)
  site
  approach surface
  return or holding relation
  threshold/residue slot
```

Use:

- detent site before click
- seat before object arrives
- socket before plug arrives
- latch catch before latch arrives

Replaces:

- pretending stable sites only exist after use

Extends:

- open inlet, threshold surfaces, and settlement witness.

### 33.12 Conditional / Gate Surface

Problem:

Some concepts need "this route is available only through this parameter" while
remaining structural.

Proposed structure:

```text
GateSurface(root)
  candidate route
  controlling parameter surface
  pass residue
  blocked residue
```

This should not be a boolean enum. The parameter should be a carrier/span/read
whose current resolution exposes how much route is available.

Use:

- valve
- switch
- latch release
- filter enable
- directional clutch

Replaces:

- hidden `if` dispatch in concept reads

Extends:

- capacity, partition, and settlement witnesses.

## 34. What The Missing Structures Replace

| Missing structure | Replaces | Extends |
| --- | --- | --- |
| Unoriented extent | pretending every extent is directed | `Relation.Segment` with a lower-resolution precursor |
| Unordered group read | accidental meaning from construction order | `OrderedGroup` and additive participation |
| Declared reversible traversal | confusing agnostic with bidirectional | directed segments and projection routes |
| Open attachment site | confusing omitted, unknown, and unimplemented | placeholders and orthogonal outlets |
| Explicit void surface | treating absence as a hole | opposed products and boundary reads |
| Whole partition surface | confusing count with division of a whole | resolution ladders and repeats |
| Quadrant helpers | collapsing negative and inverse | carrier locations and inverse faces |
| Read-as-focus surface | external focus metadata | live/equation reads |
| Material vs observation route | treating all fanout as transfer | live output and route settlement |
| Capacity surface | primitive pass/block flags | boundary/reset and tension reads |
| Stable potential | only post-settlement stable-site witnesses | open inlet and threshold reads |
| Gate surface | hidden boolean dispatch | capacity/partition/control surfaces |

## 35. Minimal Primitive Set After Cleanup

After removing composed reads, the small primitive set is:

```text
1. carrier face
   forward / inverse

2. signed site
   positive / negative location on a face

3. shared coordinate
   TickRef-backed physical identity

4. point
   grounded site

5. extent
   directed segment today, future unoriented extent

6. orthogonal
   point, zero-segment, segment outlets

7. product
   aligned and non-aligned combined participation

8. group
   gathered participants, ordered today, future unordered read

9. incidence
   shared point, endpoint join, origin join, continuation

10. boundary / closure
    limits, wraps, closed contours, envelopes

11. projection / scale
    relation between spans or frames

12. openness
    placeholder, descriptor slot, open attachment site

13. repeat
    repeated selected structure over controller

14. witness
    residue, tension, timeline
```

Everything else should be built from these.

## 36. Design Rule

The rule for deciding whether something is primitive:

```text
if removing the word still leaves the Gojo graph with the same local
physical affordance, the word is not primitive
```

Examples:

```text
funnel
  not primitive
  build from radial/branch convergence + directed traversal + neck/open outlet

filter
  not primitive
  build from passage + pass basis + nonmatching residue

friction
  not primitive
  build from contact + tangent motion attempt + stored tension

detent
  not primitive
  build from repeat + stable potential/witness + threshold

sharp corner
  not primitive
  build from corner + transition parameter driven toward zero
```

## 37. Practical Direction

The best next documentation or code step would be:

1. Add small helper docs or tests for the four face quadrants.
2. Add an `UnorientedExtent` helper as ordinary relation structure.
3. Add an `OpenAttachment` helper to separate omitted, unknown, void, and
   unimplemented receiver.
4. Add a `PartitionSurface` helper to separate tick count from whole
   partition.
5. Add tests showing that bridge, funnel, filter, and choke are different
   reads over related structures rather than separate concept tags.

The first code should avoid word classifiers. The tests should inspect:

- topology
- chosen read/focus
- parameter slots
- residue/tension witness
- refinement timeline

The goal is not to make Gojo know English words. The goal is to make the
structure rich enough that words become honest local reads.

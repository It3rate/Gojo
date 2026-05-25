# Flow Boundary Grade Codex

This note records the current conclusion for boundary and flow in ConceptSurface
work. It is intentionally written as a bootstrap document, not as an API
manual. The goal is to keep the Gojo meaning in inspectable structure while the
code helpers remain small reads and builders over that structure.

The short version:

```text
grade 0: flow face
  F or I

grade 1: boundary side cut over a flow face
  P or N, always born on F or I

summary reads:
  None flow = no exposed primitive flow face in this local read
  FI flow   = both F and I faces are physically present
  P         = only positive side is present in this local boundary read
  N         = only negative side is present in this local boundary read
  PN        = positive and negative sides are both present
  S         = bounded whole, interval, closure, or whole-partition surface
```

The important correction is that `None`, `FI`, `PN`, and `S` are not all equal
primitive things at the same layer. They are useful low-resolution reads, but
they come from different structural situations.

## Why This Exists

The earlier boundary/flow table was useful because it gave us the sixteen compact
states:

```text
P:F   P:I   P:FI   P:None
N:F   N:I   N:FI   N:None
PN:F  PN:I  PN:FI  PN:None
S:F   S:I   S:FI   S:None
```

That table is still useful as a read vocabulary. It is not the primitive
physical model.

The primitive model needs to answer more basic questions:

```text
what ordered difference is exposed?
what side has been cut over that ordered difference?
what lanes can a value legally occupy?
what relation, if any, lets tension move between lanes?
```

Those questions have to be answerable by traversing Gojo structure. If a helper
class or local read disappears, the flow face, boundary side, lane basis,
occupied lane, transition, residue, and tension should still be visible as
connected carriers and relations.

## Terms

`Flow`

In this document flow does not mean material movement. It means recognizable
ordered difference. It is the first exposed grade of order: an observer can read
along it one way or the dual way.

`Flow face`

A primitive exposed face of that ordered difference. At grade 0 the primitive
faces are:

```text
F: forward face
I: inverse face
```

`Boundary side`

A primitive side cut over a concrete flow face. At grade 1 the primitive sides
are:

```text
P: positive side
N: negative side
```

A side is not born in isolation. It is born on a flow face. A flowless boundary
is a later stripped or abstracted read, not first-principles structure.

`Exposure`

A local summary read over surfaces that are present:

```text
no F or I face found    -> FlowExposure.None
only F found            -> FlowExposure.Forward
only I found            -> FlowExposure.Inverse
both F and I found      -> FlowExposure.Both

only P side found       -> BoundaryExposure.Positive
only N side found       -> BoundaryExposure.Negative
P and N sides found     -> BoundaryExposure.FullSigned
bounded whole closure   -> BoundaryExposure.BoundedWhole
```

Exposure is not a global classifier. It is just a low-resolution read over
nearby structure.

`Basis`

The legal lane basis for a carrier-like or unit-shape-like value. A basis says
which lanes a value may occupy. It does not by itself say which lane a specific
value currently occupies.

`Occupied lane`

The lane marker on one value. A value of magnitude `5` can occupy `PF`, `NF`,
`PI`, `NI`, or a whole/no-flow lane depending on the surrounding basis.

`Transition`

A physical relation that allows tension to cross from one occupied lane to
another. The presence of both `P` and `N` sides does not automatically mean one
scalar can move between them.

## Grade 0: Flow Face

The first grade is ordered difference.

At this grade the primitive states are only:

```text
F
I
```

`F` and `I` are dual faces of the same idea. They are not value signs yet. They
are the first observable direction through order.

This is why the primitive ConceptSurface code now has:

```text
FlowFace
FlowFaceSurface
```

A `FlowFaceSurface` is ordinary ordered Gojo structure:

```text
root
  face segment    0 -> 1 for F, 0 -> -1 for I
  selected        the thing this face is exposed on
  evidence        local witness, unknown point if unresolved
```

The helper read `FlowFace` is compact. The meaning is the surface.

The face marker is a tiny directed segment rather than a single point. That is
intentional: flow is ordered difference, and the segment shape keeps primitive
flow faces from colliding with boundary side points during broad local
traversal.

### Why None Is Not Primitive Flow

`None` means no primitive flow face is exposed in the local read.

That can happen for several honest reasons:

```text
the structure is only being used as placement
the flow was stripped away after construction
the reader is intentionally ignoring flow
the concept is unresolved at this resolution
the surface is a whole-partition object
```

None is a valid read, but it is not grade-0 flow. Grade-0 flow requires a
recognizable ordered difference, so the primitive exposed face must be `F` or
`I`.

### Why FI Is Not Primitive Flow

`FI` means both primitive faces are physically present.

That may be a two-way bridge, a balanced score lane, an opposed transfer pair,
or the flow-face portion of a complex-like lane system. It is still a composed
read:

```text
F surface present
I surface present
local read summarizes this as FI
```

The composed read must not erase the two faces. If opposed participation
creates tension, Gojo should keep that tension visible until a settlement,
projection, or cancellation relation consumes it.

## Grade 1: Boundary Side

Boundary appears after flow because a side is a cut over ordered difference.

The primitive boundary sides are:

```text
P
N
```

But `P` and `N` are not detached tags. A side is born over a concrete flow face:

```text
P over F
N over F
P over I
N over I
```

This is why the primitive ConceptSurface code now has:

```text
BoundarySide
BoundarySideSurface
```

A `BoundarySideSurface` is ordinary ordered Gojo structure:

```text
root
  side point      0 for P, 1 for N
  flow face       a FlowFaceSurface
  cut evidence    the local side/cut witness
```

The read fails if the side does not carry a readable primitive flow face. That
is deliberate. It prevents a boundary side from pretending it was born without
ordered difference.

## Grade Lanes

Once grade 0 and grade 1 are both visible, a value lane can be read:

```text
PF
PI
NF
NI
```

The current bridge helper is:

```text
FlowBoundaryGradeLaneSurface
```

It builds:

```text
root
  FlowFaceSurface
  BoundarySideSurface cut over that exact FlowFaceSurface
  selected
```

Then it can read that physical pair as the compact occupied-lane vocabulary:

```text
P over F -> BoundaryFlowLane.PositiveForward
P over I -> BoundaryFlowLane.PositiveInverse
N over F -> BoundaryFlowLane.NegativeForward
N over I -> BoundaryFlowLane.NegativeInverse
```

This lets the current value and lane-basis code continue to read compact lane
terms while the evidence moves toward lower-grade physical surfaces.

## Boundary Exposure Reads

The compact `BoundaryExposure` record keeps the two-side summary vocabulary:

```text
positive-side evidence present
negative-side evidence present
```

That gives:

```text
FF -> PN
TF -> P
FT -> N
TT -> S
```

That table remains useful for the current API, but it is now a summary read
over structural evidence. The grade interpretation is more precise:

```text
P  = only positive side is present in this local read
N  = only negative side is present in this local read
PN = positive and negative sides are both present in this local read
S  = bounded whole, closure, interval, or whole-partition surface
```

`PN` is not automatically "an integer." It may only mean that both sides are
visible. To behave like one signed scalar, it also needs physical transition
structure between sides.

`S` is also not "nothing." It is a bounded whole. It belongs to interval,
ratio, percent, range, selected segment, valve opening, tolerance band, and
non-count whole concepts.

`S` now has an explicit primitive foothold:

```text
BoundedWholeSurface
  whole
  boundary pair: left boundary, right boundary
  partition/resolution witness
```

This lets `S:None` preserve bounded-whole structure without inventing a
primitive no-flow face. If flow is later attached, the same bounded-whole
surface can pair with primitive `F` or `I` face evidence.

## Signed Scalars Need Coupling

This distinction matters:

```text
P side present
N side present
no transition between them
```

That is two visible buckets or two visible sides. It is not yet one signed
number.

For signed scalar behavior, Gojo needs transition structure:

```text
PF <-> NF
```

With that transition, a single occupied value can move from `PF` to `NF` when a
settlement crosses zero. Without that transition, underflow leaves residue or
tension.

This is already the right direction in the current lane-transition code:

```text
BoundaryFlowLaneTransitionSurface
BoundaryFlowUnitShape.SignedForward()
BoundaryFlowValueSettlement
```

The unit shape can carry the possible lanes and the transition pair, so another
carrier using that unit shape does not have to rebuild the same crossing
relation every time.

## Independent Buckets Are Different

Some systems intentionally keep wins and losses apart:

```text
wins:   P lane
losses: N lane
```

They may share a display, a report, or a final read, but they do not
automatically cancel. That is a different structure from one signed scalar.

Gojo should preserve that difference:

```text
both sides visible, no transition     -> independent participation
both sides visible, transition pair   -> one signed scalar basis
both sides visible, projection read   -> report or summary
```

This is the same local-structure rule as the rest of Gojo: do not let a reader
invent the relation that would make the concept true.

## Values And Basis

A complete value has two different pieces of structural evidence:

```text
possible lanes: supplied by the basis, usually through a unit shape descriptor
occupied lane:  supplied by the value's lane marker
```

Example:

```text
unit shape says PN:F is possible
value says it occupies NF with magnitude 5
read notation: -5f
```

The value does not need to carry the full basis if its carrier's unit shape
already supplies it. The value only needs its magnitude and occupied lane.

If the occupied lane is outside the possible lanes, that is not hidden. It
becomes an illegal read, residue, or tension-bearing state.

## Unit Shape Role

A unit shape is the right place for stable lane possibilities and stable lane
transitions.

Examples:

```text
plain descriptor-free unit shape
  default low-resolution basis: S:None

positive count unit shape
  possible lane: PF

signed forward unit shape
  possible lanes: PF, NF
  transitions: PF -> NF and NF -> PF

signed both-flow unit shape
  possible lanes: PF, PI, NF, NI
  transitions are explicit, not assumed
```

This matches the user's intuition: unit shapes crystallize tick character, but
they should not become named concept classes. They can carry lane bases and
transition structures as descriptors.

## Duals

Use the word `dual` for the core idea.

Use `dual` for the structural operation. `Inverse` remains a flow-face name;
`invert` is not a separate concept in this layer.

There are multiple duals because there are multiple grades.

Boundary dual:

```text
PF <-> NF
PI <-> NI
```

Flow dual:

```text
PF <-> PI
NF <-> NI
```

Full lane dual:

```text
PF <-> NI
PI <-> NF
```

At an ordered resolution, "both" can also have an order-sensitive dual:

```text
PN <-> NP
FI <-> IF
```

At low resolution those may summarize to the same unordered exposure. At higher
resolution the order can matter, especially when a value, transfer, or timeline
has already selected one side first.

## Flow Phase Step

The complex-like step through the four signed flow lanes is:

```text
PF -> PI -> NF -> NI -> PF
```

This should not be called a geometric turn in the general documentation. It
can occur in a one-dimensional directed segment, a score system, an abstract
lane surface, or a contour. "Flow phase step" is a better low-resolution name.

The current helper:

```text
BoundaryFlowLane.TryStepFlowPhase
BoundaryFlowLaneTransitionSurface.FlowPhaseStepTransitions
```

is a read/build convenience for that four-step pattern. The physical meaning is
the transition surfaces.

## Axis And Face Split

The likely core-adjacent shape is:

```text
axis / carrier identity
  anchors
  partitions
  locations
  topology
  unit shape

flow face
  F or I evidence over selected axis/order

boundary side
  P or N evidence cut over a flow face

lane basis
  legal lanes, usually held by unit shape

value occupancy
  magnitude plus one occupied lane

transition
  relation that allows movement from one occupied lane to another
```

This is the axis/face split direction. Concept surfaces should build explicit
evidence first and let compact reads summarize that evidence only afterward.

## Old Sixteen States As Summary Reads

The old sixteen-state table still has meaning as a surface read. The
difference is that each state should be explained as a composition or absence
of lower-grade evidence.

| State | Low-resolution read |
| --- | --- |
| `P:F` | positive side over forward face |
| `P:I` | positive side over inverse face |
| `P:FI` | positive side participating with both exposed flow faces |
| `P:None` | positive side read after flow is stripped, absent, or ignored |
| `N:F` | negative side over forward face |
| `N:I` | negative side over inverse face |
| `N:FI` | negative side participating with both exposed flow faces |
| `N:None` | negative side read after flow is stripped, absent, or ignored |
| `PN:F` | both sides visible over forward flow, transition optional |
| `PN:I` | both sides visible over inverse flow, transition optional |
| `PN:FI` | both sides and both flow faces visible, transitions optional |
| `PN:None` | signed placement or side structure with no exposed flow |
| `S:F` | bounded whole with forward flow exposure |
| `S:I` | bounded whole with inverse flow exposure |
| `S:FI` | bounded whole with both flow faces exposed |
| `S:None` | bounded whole, interval, ratio, partition, or placement surface |

The table is still good for local inspection. It should not be treated as a
primitive enum.

## Examples

### Positive Count

Structure:

```text
FlowFaceSurface(F)
BoundarySideSurface(P over F)
lane basis: PF
value occupancy: PF
```

Meaning:

```text
count, radius, length, accumulated amount
```

Crossing below zero without a transition leaves residue.

### Negative-Only Count

Structure:

```text
FlowFaceSurface(F)
BoundarySideSurface(N over F)
lane basis: NF
value occupancy: NF
```

Meaning:

```text
deficit, distance from goal, countdown, retreat amount
```

This is not a fake positive value. It is the dual side as a valid domain.

### Signed Forward Number

Structure:

```text
FlowFaceSurface(F)
BoundarySideSurface(P over F)
BoundarySideSurface(N over F)
lane basis: PF, NF
transition pair: PF <-> NF
```

Meaning:

```text
ordinary signed scalar on one forward flow face
```

The transition pair is what makes it one scalar instead of two buckets.

### Signed Both-Flow Number

Structure:

```text
FlowFaceSurface(F)
FlowFaceSurface(I)
BoundarySideSurface(P over F)
BoundarySideSurface(N over F)
BoundarySideSurface(P over I)
BoundarySideSurface(N over I)
lane basis: PF, PI, NF, NI
transitions: explicit, not assumed
```

Meaning:

```text
one value may occupy one of four lanes
```

This can support complex-like flow phase steps, mirrored systems, opposed
traffic systems, or other intentionally fused dual surfaces.

### Whole Segment

Structure:

```text
bounded whole closure
partition surface over the whole
lane basis: S:None unless flow is explicitly attached
```

Meaning:

```text
interval, percent, valve opening, tolerance band, selected range
```

Partition count is not tick count. Three partitions divide a whole into three
regions. Three ticks are three units at some calibration.

### Directionless Placement

Structure:

```text
positions, anchors, extents, or partitions
no exposed FlowFaceSurface in the local read
```

Meaning:

```text
overlap, union, intersection, geometric placement, unordered endpoint pair
```

The system can still compare positions. It is simply not using those positions
as calibrated flow values.

### One-Way Bridge

Structure:

```text
two areas
bridge relation between them
FlowFaceSurface(F) on the bridge route
boundary/side read depends on selected focus
```

Meaning:

```text
flow is structurally one-way
```

A reader cannot honestly make it two-way unless an inverse face or reverse
route is actually introduced. An external request can add tension or propose a
new inverse face, but it should not pretend the old structure already had it.

### Two-Way Bridge

Structure:

```text
FlowFaceSurface(F)
FlowFaceSurface(I)
route or bridge selected by both
```

Meaning:

```text
both directions are physically present
```

That is different from "direction not specified." Unspecified means no local
flow face is exposed. Two-way means both faces are present.

### Funnel

Structure:

```text
inverse radial or converging order
selected mouth/collector side
bridge/throat focus
optional output attachment point
```

Meaning:

```text
collector side directs toward a throat
```

If the output is omitted, it reads like a collector or peninsula. If an
attachment site is present but unresolved, it reads as open capacity or
unreceived flow, not as ordinary absence.

### Filter

Structure:

```text
bridge or throat
capacity/gate parameter
residue or blocked-transfer witness
```

Meaning:

```text
bridge with controlled acceptance
```

The parameter changes the word read from simple bridge to filter-like behavior.
The word does not own the meaning. The gate, accepted transfer, blocked
transfer, and residue do.

### Football Field Style Extension

This is not primitive, but it is a good example.

Structure:

```text
two origins
two positive flows facing each other
shared yard coordinate
possession/focus selects which origin reads the number
```

Meaning:

```text
one field, two attacking perspectives, one active read depending on possession
```

This does not require changing the primitive flow/boundary model. It can be
built as a higher-order structure with two origins, two flow faces, and a focus
or possession surface selecting the active read.

## Focus

Focus does not need to be a global tag.

In Gojo terms, focus is often just:

```text
a read attached to a selected element
a writable parameter attached to a selected element
a settlement witness attached to a selected element
a timeline whose first frame exposes selected evidence
```

If we read bridge capacity at the throat, the bridge starts to behave as a
choke point in the local concept read. If we make that capacity writable, the
surface becomes more filter-like or valve-like. Meaning changes because the
structure being exposed changes.

## Parameters

Parameters should enter as structure:

```text
slot
relation to selected element
constraint surface
partition or resolution ladder
settlement witness
residue/tension if unresolved
```

A primitive concept surface should usually avoid parameters. A higher-resolution
concept surface can add them.

Examples:

```text
corner
  low resolution: orthogonal meeting
  higher resolution: transition-radius slot
  sharp corner: radius slot with perturbation toward zero

bridge
  low resolution: two areas connected by one selected passage
  higher resolution: capacity slot, direction face, residue witness

valve
  low resolution: bounded whole opening
  higher resolution: partitioned capacity, writable gate, accepted/blocked flow
```

## Residue And Tension

Residue and tension are part of the concept.

Examples:

```text
positive-only value receives negative delta
  value settles to zero
  residue keeps the rejected negative change visible

forward-only route receives inverse-flow value
  projected value remains visible as illegal
  residue/tension records what the target could not accept

bridge gets requested to become two-way
  existing structure still reads one-way
  request can add tension or propose inverse face
```

This is important for concept surfaces because many design ideas begin as
incompatible or underspecified. The failed read is often the useful information.

## Implementation Plan

### Stage 1: ConceptSurface Bootstrap

Add physical low-grade evidence inside `Gojo/ConceptSurface`:

```text
FlowFace
FlowFaceSurface
BoundarySide
BoundarySideSurface
FlowBoundaryGradeLaneSurface
```

This stage uses explicit concept-surface evidence instead of carrier-face
adapters.

### Stage 2: Summary Reads Over Primitive Evidence

Keep existing summary records:

```text
FlowExposure
BoundaryExposure
BoundaryFlowExposure
BoundaryFlowLane
BoundaryFlowLaneBasis
```

But teach them, where useful, to read from primitive faces and sides.

Current first pass:

```text
FlowFaceSurface.ReadExposure
BoundarySideSurface.TryReadExposure
FlowBoundaryGradeLaneSurface.TryRead
```

Current second pass:

```text
FlowSurface.TryRead can summarize FlowFaceSurface evidence
BoundarySurface.TryRead can summarize BoundarySideSurface evidence
BoundaryFlowSurface.TryRead can pair primitive boundary evidence with primitive flow evidence
```

This keeps the compact surface vocabulary useful while letting lower-grade
physical evidence become the preferred source underneath it.

Current consolidation pass:

```text
FlowSurface.Create emits FlowFaceSurface or NoFlowSurface evidence
BoundarySurface.Create emits BoundarySideSurface, StrippedBoundarySideSurface, or BoundedWholeSurface evidence
FlowSurface.TryRead reads FlowFaceSurface and NoFlowSurface evidence
BoundarySurface.TryRead reads BoundarySideSurface, StrippedBoundarySideSurface, or BoundedWholeSurface evidence
fallback to compact construction rows has been removed from these exposure surfaces
```

Current paired-evidence pass:

```text
BoundaryFlowPrimitiveEvidence builds paired boundary/flow evidence from one exposure read
BoundaryFlowSurface.Create uses paired primitive evidence when flow is present or the boundary is a bounded whole
BoundaryFlowUnitShape primitive-basis construction shares the same paired evidence builder
CarrierAxisSurface uses paired primitive evidence before falling back to stripped/no-flow boundary surfaces
```

If a mixed surface carries both compact summary language and lower-grade
physical evidence, the physical evidence should win. A real `F` face is a
flow face even if some external note calls the read `None`. A real positive
side is a positive side even if a conflicting compact note calls it `N`.

### Stage 3: Unit Shape Basis

Keep the rule:

```text
unit shape supplies possible lanes
value supplies occupied lane
```

A descriptor-free carrier should still have a default basis. Today that default
is `S:None`, which means whole/no-flow at the lowest explicit resolution.

For stable signed behavior, the unit shape should carry both:

```text
lane basis
lane transition surfaces
```

Current pass:

```text
BoundaryFlowUnitShape.DescribedAtom first tries primitive grade evidence
BoundaryFlowUnitShape.TryCreatePrimitiveBasis builds basis from FlowFaceSurface and BoundarySideSurface
Positive/signed forward/signed both-flow unit shapes now preserve primitive evidence in descriptors
Bounded whole/no-flow now preserves BoundedWholeSurface evidence with no primitive flow face
Bounded whole with flow pairs BoundedWholeSurface with primitive FlowFaceSurface evidence
```

Occupied lane surfaces now follow the same rule:

```text
FlowLaneSurface.Create(F/I) builds FlowFaceSurface evidence
FlowLaneSurface.Create(None) builds explicit no-flow summary evidence
BoundaryLaneSurface.Create(P/N over a flow face) builds BoundarySideSurface evidence
BoundaryLaneSurface.Create(S) builds BoundedWholeSurface evidence
BoundaryFlowLaneSurface.Create pairs those physical boundary and flow lane surfaces
```

For example, a `PF` occupied lane is not just a two-letter marker. It is a
forward flow face plus a positive side cut over that exact face. A `S:None`
occupied lane is a bounded whole plus an explicit no-flow read.

The remaining no-flow side lanes are also structural:

```text
P:None -> StrippedBoundarySideSurface(P) plus explicit no-flow evidence
N:None -> StrippedBoundarySideSurface(N) plus explicit no-flow evidence
```

These are not primitive grade-1 sides. They are derived surfaces for side
structure whose flow has been stripped, ignored, or left unresolved.

Fallback to compact lane markers has been removed from the occupied-lane
surface path. The compact summary records still exist as helper reads, but
surface constructors no longer emit marker rows for lane, boundary, or flow
evidence.

### Stage 4: Transition-First Signed Behavior

Do not assume `PN` means automatic crossing.

Signed scalar behavior should require transition evidence:

```text
PF -> NF
NF -> PF
```

Flow-phase behavior should require transition evidence:

```text
PF -> PI
PI -> NF
NF -> NI
NI -> PF
```

One-way transitions remain valid and leave residue in the blocked direction.

### Stage 5: Core-Adjacent Axis/Face Split

After enough ConceptSurface tests exist, consider moving the split closer to
core:

```text
Carrier axis
Flow face
Boundary side
Lane basis
Occupied value
Transition
```

This is the larger migration. It changes how code thinks about carriers, so it
should happen after the physical evidence and tests are boring.

## What This Replaces

This direction should gradually replace:

```text
global "what kind is this?" classifiers
standalone sign tags
standalone flow tags
reader-invented two-way behavior
automatic cancellation of opposed participation
integer behavior implied by P and N merely being visible
```

It extends:

```text
BoundaryExposure
FlowExposure
BoundaryFlowLane
BoundaryFlowLaneBasis
BoundaryFlowValueSurface
BoundaryFlowLaneTransitionSurface
UnitShape descriptors
```

## Testing Rules

Tests should prefer structural evidence:

```text
can the primitive face be read from relation structure?
can the boundary side prove it was cut over a primitive face?
does the exposure read summarize present surfaces correctly?
does a value keep its occupied lane separate from its possible-lane basis?
does crossing without transition leave residue?
does crossing with transition move the occupied lane?
```

Tests should not only assert labels. Labels are useful for debugging, but the
important thing is that a local traversal can find the physical pieces.

## Current Status

This document tracks the current experimental implementation in
`BoundaryFlowSurface.cs`.

Implemented in this pass:

```text
FlowFace
FlowFaceSurface
NoFlowSurface
BoundarySide
BoundarySideSurface
FlowBoundaryGradeLaneSurface
BoundaryFlowPrimitiveEvidence
StrippedBoundarySideSurface
FlowSurface structural construction/read
BoundarySurface structural construction/read
BoundaryFlowSurface paired primitive construction/read
CarrierAxisSurface primitive evidence construction
CarrierBoundarySurface structure-first adapter construction
RelationFlowSurface structure-first adapter construction
BoundaryFlowLaneBasis structure-first legal-lane reads
BoundaryFlowValueBasisRead structural value-basis read
BoundaryFlowUnitShape primitive-basis construction
BoundedWholeSurface
primitive-first summary read precedence
occupied-lane primitive evidence surfaces
stripped boundary side evidence for P/N no-flow lanes
```

Still experimental:

```text
core carrier axis/face split
```

The safe direction is to keep adding inspectable evidence and only then reduce
the remaining adapter surfaces.

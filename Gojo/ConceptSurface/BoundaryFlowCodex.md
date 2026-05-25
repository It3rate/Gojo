# Boundary And Flow Codex

This note defines a small boundary/flow vocabulary for concept surfaces. The
goal is to separate two questions that are easy to collapse:

```text
boundary/domain:
  what kind of position surface is addressable?

flow exposure:
  what kind of calibrated tension/change route is present?
```

The two axes are independent. Each axis has two primitive sides and therefore
four combinations. Together they produce sixteen useful boundary/flow states.

This should be implemented as inspectable Gojo structure, not as a word enum.
The labels below are compact design names for local reads over boundary and
flow surfaces.

The lower-grade bootstrap model is detailed in
`FlowBoundaryGradeCodex.md`. That note treats primitive flow as `F` or `I`,
primitive boundary as `P` or `N` cut over a flow face, and this document's
sixteen boundary/flow states as summary reads over present, absent, or composed
structure.

## Boundary Axis

The boundary axis answers:

```text
does this position surface have a boundary on the left,
on the right, on both sides, or on neither side?
```

Read two structural presences:

```text
left boundary present
right boundary present
```

The four combinations are:

| Label | Left boundary | Right boundary | Meaning |
| --- | --- | --- | --- |
| `PN` | false | false | no boundary on either side; full positive/negative position surface. |
| `P` | true | false | left-bounded positive surface; `uint`-like amount, radius, count, forward distance. |
| `N` | false | true | right-bounded negative surface; deficit, retreat, distance-from-goal, negative-only amount. |
| `S` | true | true | bounded whole segment; whole-instance surface, interval, percent/ratio object. |

The compact read order is:

```text
(leftBoundary, rightBoundary)

FF -> PN
TF -> P
FT -> N
TT -> S
```

`S` means a bounded segment or whole. It is not "nothing." It is the domain
where whole-instance partitioning belongs.

### Boundary Meanings

`P` is the familiar positive-only domain:

```text
0..+
```

It is useful for count, length, radius, accumulated amount, fill level, or any
quantity that has one origin boundary and grows away from it.

`N` is the dual of `P`:

```text
-..0
```

It is useful for deficits, countdowns, retreat distance, remaining distance to
a goal, or negative-only accounting. It should not be treated as a hack around
ordinary positive numbers. It is a valid domain by itself.

`PN` is the ordinary full signed position surface:

```text
-..0..+
```

It has neither a left nor right boundary. Positions may be positive or
negative, but without flow exposure those are just positions. Segment overlap,
union, intersection, and geometry placement can live here without importing
ordinary value-flow arithmetic.

`S` is a bounded whole:

```text
left boundary .. right boundary
```

It is the home of whole-partition concepts: percent, interval, selected range,
valve opening, bridge span, finite field, bounded tolerance, and non-count
whole surfaces. A partition of `S` divides this whole instance; it is not a raw
unit count.

## Flow Axis

The flow axis answers:

```text
which calibrated flow/tension faces are exposed?
```

Read two structural presences:

```text
forward flow present
inverse flow present
```

The four combinations are:

| Label | Forward flow | Inverse flow | Meaning |
| --- | --- | --- | --- |
| `Ø` | false | false | no flow exposure; position, support, read-only, partition, overlap, or placement. |
| `F` | true | false | forward calibrated flow only. |
| `I` | false | true | inverse/opposed calibrated flow only. |
| `FI` | true | true | both calibrated flow faces explicitly present. |

The compact read order is:

```text
(forwardFlow, inverseFlow)

FF -> Ø
TF -> F
FT -> I
TT -> FI
```

`Ø` flow does not mean the structure is meaningless. It means the structure is
not exposing calibrated tension/change direction. It may still expose
positions, partitions, ratios, trolleys, overlaps, boundaries, and reads.

## Why Boundary And Flow Are Separate

A position surface can be addressable without being a calibrated flow surface.
For example:

```text
football field geometry
interval overlap
rectangle coordinates
percent of an opening
selection range
```

These can use positions and partitions without claiming that a positive tick
routes tension in a particular direction.

Conversely, flow is a calibration or tension-route claim. It says how change
enters, leaves, routes, or opposes through the structure.

```text
boundary/domain = where positions can be
flow exposure   = how change/tension is calibrated
```

This distinction lets Gojo express:

- positive-only numbers
- negative-only numbers
- signed position lines
- bounded whole segments
- no-flow position math
- one-way flow
- inverse one-way flow
- explicit two-way flow

without collapsing them into one sign marker.

## Relationship To Carrier-Like Axes

The boundary/flow model treats the axis/domain and the traversal/transfer face
as separate pieces of evidence:

```text
axis / position surface
  identity, partitions, anchors, topology

flow face
  forward reservoir or inverse reservoir

flow exposure
  Ø, F, I, or FI
```

The `FI` case should usually be two connected faces, not one value collapsed
into a net scalar. If both forward and inverse participation exist at once,
Gojo should preserve opposed participation and tension until an explicit read
cancels, projects, or settles it.

The current concept-surface layer uses two narrower hosts:

```text
CarrierBoundarySurface(root)
  selected carrier-like axis
  BoundarySurface(...)
  optional partition / domain witness

RelationFlowSurface(root)
  selected relation / traversal surface
  FlowSurface(...)
  optional gate / capacity witness
```

This is the preferred placement for the current concept-surface layer:

```text
boundary/domain belongs with the carrier-like axis
flow exposure belongs with the relation/traversal surface
```

`CarrierAxisSurface` remains as a bundled convenience for cases where a note or
test wants to package both around one selected carrier:

```text
CarrierAxisSurface(root)
  selected carrier
  BoundarySurface(...)
  FlowSurface(...)
```

These helpers make the distinction explicit:

```text
CarrierBoundarySurface
  what boundary/domain the carrier-like axis exposes: P, N, PN, or S

RelationFlowSurface
  which traversal/transfer faces this relation exposes: Ø, F, I, or FI
```

Dualing an axis surface swaps boundary sides and flow exposure:

```text
P:F   -> N:I
N:I   -> P:F
PN:F  -> PN:I
S:Ø   -> S:Ø
S:FI  -> S:FI
```

The selected carrier-like axis does not change during this read. The dual
surface is new explicit evidence around the same selected axis. If Gojo later
splits carrier axis from carrier face in core, `CarrierBoundarySurface` and
`RelationFlowSurface` are the migration path because they already keep those
pieces distinct.

Lane basis is not carrier-owned. `BoundaryFlowLaneBasis` can attach the same
legal lane shape to any selected structure:

```text
BoundaryFlowLaneBasis(root)
  BoundaryFlowSurface
  selected structure
  explicit legal lane surfaces
```

The selected structure can be a carrier axis, but it can also be a unit-shape
surface, a bounded whole, a bridge opening, or any other inspectable Gojo
structure. This matters because some lane reads are tick-character reads rather
than live carrier-balance reads:

```text
unit-shape resolution surface
  selected by S:None
  legal lane = whole/no-flow

bounded whole segment
  selected by S:None
  value 2 on lane S:None reads as a whole-partition style value
  no CarrierAxisSurface is required
```

Lane basis can also travel as a unit-shape descriptor:

```text
UnitShape.DescribedAtom(
  BoundaryFlowLaneBasis(
    selected tick source,
    boundary = P,
    flow = F))
```

That is the current best route for tick-character meaning. The carrier can keep
its unsigned live value, while the unit shape says how one tick should be read
when this concept surface asks about boundary/flow lanes.

For complete boundary-flow values, the split is:

```text
unit shape descriptor
  possible lanes

value surface
  occupied lane
```

Example:

```text
Carrier.UnitShape descriptor:
  BoundaryFlowLaneBasis(PN:F)
  legal lanes = PF, NF

value:
  magnitude = 6
  occupied lane = NF

read:
  -6f
```

The same unsigned carrier magnitude could occupy `PF` or `NF`, but it must say
which one. If the carrier unit shape has no lane-basis descriptor, the unit
shape read supplies the low-resolution default:

```text
boundary = S
flow = None
legal lane = S:None
```

So a plain carrier does not have "no possible lanes"; it has a default
whole/no-flow basis. A `PF` value on that carrier is readable, but illegal
against the default basis.

The normal carrier-backed value does not duplicate the basis:

```text
value surface
  magnitude point on carrier C
  occupied lane = NF
  basis slot = unresolved

carrier C unit shape descriptor
  BoundaryFlowLaneBasis(PN:F)

complete value read
  resolved basis = PN:F
  occupied lane = NF
  value = -6f
```

Complete value reads resolve basis in this order:

```text
1. explicit UnitShape lane-basis descriptor
2. explicit value/host/witness basis
3. UnitShape default S:None basis
```

That keeps `UnitShape` as the normal home when it says something specific,
while still allowing route and target surfaces to carry a local host basis when
that local structure is the thing being modeled. The explicit value/host basis
can be useful as a snapshot, route target, or conflict record, but it is not the
ordinary home for a carrier's tick basis.

If the occupied lane is not legal for the resolved basis, the read still
succeeds and exposes lane tension:

```text
resolved basis = PN:F
occupied lane = NI

read:
  value = -6i
  legal = false
```

Because every carrier has a unit shape and the unit-shape read defaults to
`S:None`, "no lane basis found" is not the normal incomplete state anymore.
The ordinary tension case is now an occupied lane that is illegal against the
resolved basis, for example `PF` against `S:None`.

So the emerging split is:

```text
BoundaryFlowLaneBasis
  lane grammar for a selected structure

BoundaryFlowLaneTransitionSurface
  directed evidence that one occupied lane can move into another

CarrierAxisSurface
  carrier-hosted convenience using that lane grammar

BoundaryFlowValueSurface
  one unsigned magnitude occupying one lane against a basis
```

This lets the unsigned live carrier value remain small while lane meaning is
expressed by surrounding structure when needed.

## Lane Transitions

Possible lanes and lane transitions are separate.

```text
BoundaryFlowLaneBasis(PN:F)
  legal lanes: PF, NF

BoundaryFlowLaneTransitionSurface(PF -> NF)
  tension may move from positive-forward into negative-forward
```

The first surface says a value is allowed to occupy either lane. The second
surface says an overflow, underflow, or accepted transfer may actually cross
from one occupied lane into the other. Without the transition, a read can still
see the target lane as legal, but settlement leaves the crossing as residue.

Direction is explicit. `PF -> NF` does not imply `NF -> PF`.

```text
two-way signed scalar:
  BoundaryFlowLaneTransitionSurface(PF -> NF)
  BoundaryFlowLaneTransitionSurface(NF -> PF)

one-way signed drain:
  BoundaryFlowLaneTransitionSurface(PF -> NF)
  no reverse transition
```

This matches the lower Gojo relation behavior: ordinary route settlement uses
the ordered relation faces, and reversible traversal is represented by both
directions being present. Projection reads may search either matching frame,
but that is a read convenience, not a claim that one physical settlement route
is automatically reversible.

The normal reusable home for conventional tick behavior is the unit shape:

```text
meter-like UnitShape descriptors:
  BoundaryFlowLaneBasis(PN:F)
  BoundaryFlowLaneTransitionSurface(PF -> NF)
  BoundaryFlowLaneTransitionSurface(NF -> PF)

carrier A with that UnitShape:
  value 5PF + delta -8 -> 3NF

carrier B with that same UnitShape:
  value 2NF + delta +5 -> 3PF
```

No value had to duplicate the transition rule. The values only carried their
magnitude and occupied lane; the shared UnitShape supplied the possible lanes
and the lane-crossing structure.

`BoundaryFlowUnitShape` is only a construction helper for this descriptor
bundle. For example:

```text
BoundaryFlowUnitShape.SignedForward()
  creates UnitShape.DescribedAtom(...)
  descriptor 0 = BoundaryFlowLaneBasis(PN:F)
  descriptor 1 = BoundaryFlowLaneTransitionSurface(PF -> NF)
  descriptor 2 = BoundaryFlowLaneTransitionSurface(NF -> PF)
```

If that helper disappeared, the UnitShape descriptors would still carry the
meaning. This keeps it Gojo-native: the helper builds inspectable structure;
it does not become a hidden unit-kind classifier.

Carrier axis helpers can also expose transition evidence. `SignedForward`
currently carries the conventional `PF -> NF` and `NF -> PF` transition pair
as part of the surface, while a bare `BoundaryFlowLaneBasis(PN:F)` only says
both lanes are legal.

## Duals And Flow Phase Steps

There are several distinct dual operations that should remain visible as
separate boundary and flow evidence.

Boundary dual:

```text
PF <-> NF
PI <-> NI
P  <-> N
S  -> S
```

Flow dual:

```text
PF <-> PI
NF <-> NI
F  <-> I
None -> None
```

Full lane dual:

```text
PF <-> NI
PI <-> NF
```

When the structure only exposes an unordered availability set, `PN` still reads
as `PN` after dualing, and `FI` still reads as `FI`. If the structure preserves
ordered evidence, the dual can be read at higher resolution:

```text
PN -> NP
FI -> IF
```

The value changes when its occupied lane changes:

```text
value:
  5NF = -5f

boundary dual:
  5PF = 5f
```

The i-like operation is not treated as a geometric turn, because the same
ordered phase step can appear on one-dimensional directed segments and other
non-circular structures. It is a flow phase step over signed flow lanes:

```text
PF -> PI -> NF -> NI -> PF
```

Equivalently:

```text
flow face duals every step
boundary side duals every second step
```

That keeps these operations separate:

```text
BoundaryFlowLane.DualBoundary()
  flips P/N only

BoundaryFlowLane.DualFlow()
  flips F/I only

BoundaryFlowLane.Dual()
  flips both boundary and flow

BoundaryFlowLane.TryStepFlowPhase(...)
  advances PF -> PI -> NF -> NI -> PF
```

No-flow lanes do not phase-step:

```text
S:None
  no forward/inverse flow face to step through
```

Useful coordinate notation:

```text
 3   forward face, positive site
-3   forward face, negative site
 3i  inverse face, positive site
-3i  inverse face, negative site
```

The `i` notation is only a thinking aid. The graph should represent this by
carrier face plus signed carrier location.

## Legal Lane Basis And Occupied Values

Boundary-flow exposure describes the legal lane basis for a selected structure,
usually through its unit shape or host surface. It does not by itself choose
where an individual value sits.

```text
unit shape / host surface:
  says which lanes are legal

value surface:
  carries an unsigned magnitude
  attaches one occupied lane marker
```

This is close to ordinary integer encoding:

```text
int-like carrier:
  legal boundary sides = P and N

value:
  magnitude = 5
  occupied side = N
  read = -5
```

With flow included, the occupied lane also records the exposed flow face:

```text
magnitude 5 on P,F -> +5f
magnitude 5 on N,F -> -5f
magnitude 5 on P,I -> +5i
magnitude 5 on N,I -> -5i
```

The `i` suffix is only a notation for inverse-flow lane occupancy. It is not a
claim that inverse flow is the same thing as imaginary number structure,
although they may rhyme structurally because both are opposed to an ordinary
forward scalar read.

The legal lanes are the cross product of the exposed boundary sides and the
exposed flow faces:

| Exposure | Legal occupied lanes |
| --- | --- |
| `P:None` | `P:None` |
| `P:F` | `PF` |
| `P:I` | `PI` |
| `P:FI` | `PF`, `PI` |
| `N:None` | `N:None` |
| `N:F` | `NF` |
| `N:I` | `NI` |
| `N:FI` | `NF`, `NI` |
| `PN:None` | `P:None`, `N:None` |
| `PN:F` | `PF`, `NF` |
| `PN:I` | `PI`, `NI` |
| `PN:FI` | `PF`, `PI`, `NF`, `NI` |
| `S:None` | `S:None` |
| `S:F` | `SF` |
| `S:I` | `SI` |
| `S:FI` | `SF`, `SI` |

So `N:FI`, `PN:F`, and `PN:I` are all ordinary carrier surfaces:

```text
N:FI
  negative-only domain
  forward and inverse flow faces
  valid lanes: NF, NI

PN:F
  full signed domain
  forward flow face only
  valid lanes: PF, NF

PN:I
  full signed domain
  inverse flow face only
  valid lanes: PI, NI
```

The single-lane cases are also valid and important:

```text
P:F
  valid lane: PF
  a value can infer PF if no explicit value-lane marker is present

N:I
  valid lane: NI
  a value can infer NI if no explicit value-lane marker is present
```

Only single-lane surfaces may infer a value lane. Multi-lane surfaces should
not pretend the value has chosen a lane:

```text
PN:F
  valid lanes: PF, NF
  magnitude 5 alone is unresolved
  magnitude 5 + lane NF reads as -5f

PN:FI
  valid lanes: PF, PI, NF, NI
  magnitude 5 alone is even lower resolution
```

That is the Gojo-native version of sign. A negative value is not a negative
`ulong`; it is an unsigned magnitude occupying the negative boundary side of a
surface that admits that side. Likewise an inverse-flow value is not a second
number stored inside the carrier; it is the same magnitude occupying an inverse
flow lane.

If a value chooses a lane not present in the carrier basis, the mismatch should
remain visible as tension, residue, or failed settlement:

```text
surface:
  P:F

value:
  magnitude 5
  lane NI

read:
  magnitude is visible
  occupied lane is visible
  lane is not legal for this basis
```

This preserves the distinction between:

```text
carrier allows PN:FI
  many possible occupied lanes

value occupies NI
  one selected lane

read requests ordinary signed forward scalar
  may project, reject, or leave residue depending on the visible lane
```

## Lane-Aware Reads And First Settlement Experiment

The first integration step should not rewrite `Carrier.Value`. Current carrier
state can remain an unsigned magnitude holder while concept-surface structure
adds the lane evidence needed to read that magnitude.

The executable shape is:

```text
BoundaryFlowValueSurface
  magnitude point
  occupied lane marker
  optional explicit host/witness basis
  residue / tension witness
```

A local read can project that structure into ordinary notation without owning
the meaning:

```text
magnitude 5 + lane PF ->  5f
magnitude 5 + lane NF -> -5f
magnitude 5 + lane PI ->  5i
magnitude 5 + lane NI -> -5i
```

The notation is only a read. The source remains the unsigned magnitude plus the
visible lane marker.

Single-lane bases can infer a missing value lane:

```text
P:F
  legal lanes: PF
  magnitude 7 can infer PF

N:I
  legal lanes: NI
  magnitude 7 can infer NI
```

Multi-lane bases cannot infer a chosen value lane:

```text
PN:F
  legal lanes: PF, NF
  magnitude 7 alone is unresolved
```

The first lane-settlement experiment applies a signed delta as a local read
over this structure. It is not core carrier settlement yet.

```text
PN:F value 5PF, delta -8
  target read = -3f
  NF is legal
  PF -> NF transition is present
  settled value = 3NF
  residue = 0

PN:F value 5PF, delta -8
  target read = -3f
  NF is legal
  no PF -> NF transition is present
  settled value = 0PF
  residue = -3

P:F value 5PF, delta -8
  target read = -3f
  NF is not legal
  settled value = 0PF
  residue = -3
```

This is the behavior we want before touching core:

```text
surfaces with legal target lanes and visible transition evidence can cross
surfaces without the target lane or transition keep visible residue/tension
```

## Lane-Aware Route Projection

The next integration slice treats a lane value as something that can move
through an ordinary projection relation. This is still not core carrier
settlement. It is a visible route-adjacent surface:

```text
BoundaryFlowValueRoute
  source value surface
  projection relation
  target lane basis
  target value surface
  residue / tension witness
```

The projection relation handles the coordinate read:

```text
source frame -> target frame
```

The lane basis handles admissibility:

```text
target basis allows projected lane?
```

Example: a signed-forward value can scale across two carriers:

```text
source basis:
  PN:F

source value:
  5NF
  read as -5f

route:
  source -10..10 -> target -100..100

target basis:
  PN:F

target value:
  50NF
  read as -50f
  residue 0
```

If the route projects a value into a lane the target does not expose, the
rejected lane stays visible:

```text
source basis:
  PN:FI

source value:
  5NI
  read as -5i

target basis:
  PN:F

target value:
  5NI
  illegal against target basis
  residue / tension = -5 on target
```

This matters because it keeps these separate:

```text
projection relation
  can move a coordinate from one frame to another

target lane basis
  decides which boundary/flow lanes can actually receive it

residue/tension
  records the projected participation that had no legal lane
```

The route helper therefore does not act like a global classifier. It assembles
an inspectable route surface and reads local structure:

```text
value structure says the occupied lane
route structure says how coordinate magnitude maps
target basis says what can be received
residue says what could not settle
```

When the route target is a carrier with a lane-aware unit shape, the route can
resolve its target basis from that carrier instead of requiring a duplicate
host basis:

```text
target carrier UnitShape descriptors:
  BoundaryFlowLaneBasis(PN:F)
  BoundaryFlowLaneTransitionSurface(PF -> NF)
  BoundaryFlowLaneTransitionSurface(NF -> PF)

BoundaryFlowValueRoute.TryProject(source, projection, target carrier)
  reads target basis from target.UnitShape
  creates target value against the resolved basis
```

The transition descriptors are not consumed by projection. Projection only
asks where the incoming coordinate lands and whether the target lane is legal.
Lane transitions matter when accepted flow is later settled into an existing
target value.

## Lane-Aware Transfer Acceptance

Projection is still not the same as accepted transfer. A projected route can
produce a candidate target value that the target basis cannot receive, or can
produce more than a finite target capacity allows.

The transfer surface separates those reads:

```text
BoundaryFlowValueTransfer
  route surface
  accepted target value
  blocked target value
  capacity / gate witness
```

Full acceptance:

```text
route target:
  50NF
  legal against PN:F

transfer:
  accepted = 50NF
  blocked  = 0NF
```

Illegal lane blocking:

```text
route target:
  5NI
  illegal against PN:F

transfer:
  accepted = absent
  blocked  = 5NI
  residue  = -5 on target
```

The transfer does not invent a legal accepted lane when the projected lane is
illegal. This is important: a target may expose `PF` and `NF`, but an incoming
`NI` is not silently turned into `NF`. The lane mismatch remains visible.

Capacity-limited partial acceptance:

```text
route target:
  50PF
  legal against P:F

capacity witness:
  30 on target

transfer:
  accepted = 30PF
  blocked  = 20PF
  residue  = 20 on target
```

This gives Gojo three distinct route-adjacent surfaces:

```text
projection
  where the source reads on the target frame

acceptance
  what the target can receive locally

residue/tension
  what remains visible after the local receive read
```

That separation matches the larger Gojo rule: tension is information, not a
hidden exception.

## Target Settlement Step

After transfer acceptance, the next surface records how the accepted value
settles into an existing target value. This is still a concept-surface
prototype. It does not mutate `Carrier.Value`.

```text
BoundaryFlowTargetSettlement
  target before value
  transfer transition
  target after value
  residue / blocked evidence
  TimeStepSequence witness
```

The target settlement uses the target value's resolved basis. A legal accepted
value can move the target across lanes only when the target basis admits the
result and a directed lane transition is visible on the target UnitShape or the
host basis:

```text
target before:
  20PF

accepted transfer:
  50NF

target basis:
  PN:F

transition evidence:
  PF -> NF

target after:
  30NF
```

If `PN:F` is present but `PF -> NF` is not, the target lane is legal but the
crossing does not settle. The target remains at the closest held lane and the
unsettled part is visible as residue or blocked evidence.

This is the reusable UnitShape path:

```text
target carrier UnitShape descriptors:
  BoundaryFlowLaneBasis(PN:F)
  BoundaryFlowLaneTransitionSurface(PF -> NF)
  BoundaryFlowLaneTransitionSurface(NF -> PF)

target before:
  magnitude 20
  occupied lane PF
  no duplicated basis on the value

accepted transfer:
  50NF

settlement:
  resolves PN:F and transitions from target.UnitShape
  writes target after = 30NF
```

Blocked transfer does not invent an accepted value or update the target:

```text
target before:
  20PF

blocked incoming:
  5NI
  illegal against PN:F

target after:
  20PF

residue:
  blocked 5NI
```

The settlement surface also writes a `TimeStepSequence` witness:

```text
initial:
  target before

step 0..1:
  before     = target before
  transition = transfer surface
  after      = target after
  residue    = blocked or unresolved evidence
```

This gives the current integration chain:

```text
BoundaryFlowValueSurface
  occupied lane + magnitude

BoundaryFlowValueRoute
  projection into a target frame

BoundaryFlowValueTransfer
  accepted/blocked split

BoundaryFlowTargetSettlement
  target before/after + timeline
```

## Settlement Runs

A single target settlement step is useful, but routes usually arrive as a
sequence of accepted or blocked transfers. The settlement-run surface chains
those local steps without mutating core carrier state:

```text
BoundaryFlowTargetSettlementRun
  initial target value
  final target value
  ordered transfer surfaces
  ordered residue surfaces
  combined TimeStepSequence
```

Each step in the combined timeline is still ordinary time-step structure:

```text
step n..n+1:
  before     = target value before this transfer
  transition = transfer surface
  after      = target value after this transfer
  residue    = blocked / unresolved evidence for this transfer
```

Example: two legal positive transfers into a positive target:

```text
initial target:
  10PF

transfer 1:
  accepted 20PF

transfer 2:
  accepted 10PF

final target:
  40PF

timeline:
  step 0..1: 10PF -> 30PF
  step 1..2: 30PF -> 40PF
```

The run helper can read totals from the timeline transitions:

```text
accepted magnitude = 30
blocked magnitude  = 0
step count         = 2
```

Mixed accepted and blocked transfers preserve per-step blocked residue:

```text
initial target:
  10PF on PN:F

transfer 1:
  accepted 20PF

transfer 2:
  incoming 5NI
  illegal against PN:F

final target:
  30PF

timeline step 1 residue:
  blocked 5NI
```

The important property is that the run does not summarize away tension. The
final target is readable, but every transition and every blocked lane is still
inspectable in the timeline.

## Shared Capacity Across Competing Routes

Sequential settlement is not enough for target-local capacity. If two routes
arrive at the same target at the same resolution step, they should compete for
one visible capacity witness instead of each receiving its own independent
capacity.

The shared-capacity surface records:

```text
BoundaryFlowSharedCapacityTransfer
  shared capacity witness
  ordered route candidates
  ordered transfer surfaces
  ordered residue surfaces
  unused capacity witness
```

Only legal projected lanes compete for capacity. Illegal projected lanes remain
blocked, but they do not consume target capacity:

```text
route A:
  projected 30PF
  legal

route B:
  projected 30PF
  legal

shared capacity:
  40

accepted:
  A = 20PF
  B = 20PF

blocked:
  A = 10PF
  B = 10PF

unused capacity:
  0
```

If one incoming lane is illegal:

```text
route A:
  projected 30PF
  legal

route B:
  projected 5NI
  illegal against PN:F

shared capacity:
  40

accepted:
  A = 30PF
  B = absent / 0

blocked:
  A = 0PF
  B = 5NI

unused capacity:
  10
```

This keeps the capacity question local to the target while still preserving
per-route lane residue:

```text
capacity decides how much legal incoming flow can be received
lane basis decides which incoming lanes are legal
blocked residue records what could not enter
unused capacity records what remained available
```

The current allocation is fair by route demand, matching the spirit of the
runtime route settlement splitter:

```text
capacity is distributed across active legal routes
routes whose demand is satisfied leave the active set
remaining capacity continues through the remaining active routes
```

## Target-Local Capacity

A bare capacity point is useful for first tests, but it is too detached for a
real target surface. A capacity belongs to a site: a bridge throat, slot,
collector, reservoir, or target value. The target-local capacity surface keeps
that attachment visible.

```text
BoundaryFlowTargetCapacitySurface
  target site
  target lane basis
  ConceptCapacity
    passage/site
    whole capacity point
    partition/resolution witness
    overflow witness
```

The capacity point remains ordinary numeric structure. The new surface does not
turn capacity into a code-side property; it makes the local attachment
traversable:

```text
target value:
  0PF on target axis P:F

target capacity:
  site = target value
  basis = target axis P:F
  capacity = point(target carrier, 40)

incoming routes:
  30PF
  30PF

shared read:
  capacity = 40 from the target-local capacity surface
  accepted = 20PF + 20PF
  blocked = 10PF + 10PF
```

Target-local capacity construction also uses complete basis resolution. A
target value can therefore be a normal carrier-backed occupied value:

```text
target carrier UnitShape:
  BoundaryFlowLaneBasis(PN:F)

target value:
  0PF
  basis slot unresolved

BoundaryFlowTargetCapacitySurface.TryCreate(target value, 40)
  resolves PN:F from target.UnitShape
  attaches capacity to that target basis
```

The same shared-capacity transfer can still read a bare point for low-resolution
experiments, but when the capacity witness is target-local it also checks that
the incoming routes are local to the same target carrier and compatible basis:

```text
capacity target:
  carrier A, basis P:F

route target:
  carrier B, basis P:F

read:
  rejected before capacity allocation
```

That rejection is intentionally different from lane blocking. Lane blocking
means "this route reached the target but the occupied lane is not legal there."
Target mismatch means "this capacity witness does not belong to that target."
Future work can make that mismatch a tension surface instead of a failed helper
read.

Target capacity can also carry a partition surface:

```text
target capacity:
  capacity = 60
  partition count = 3
  selected partition = 2
```

This is intentionally not a valve enum. It says the whole receiving capacity is
being read through three addressable parts. A later, higher-resolution surface
can decide whether those partitions mean closed/low/high, detents on a knob,
rough opening percentage, or a manufacturing tolerance band.

## Remaining Capacity

Target capacity by itself is the whole receiving amount. A target value can
already occupy part of that amount. Remaining capacity is a separate local read
that keeps the whole, effective opening, occupancy, remaining amount, overflow,
and partition residue visible:

```text
BoundaryFlowRemainingCapacitySurface
  target capacity surface
  current target value
  remaining capacity point
  overflow point
  effective capacity point
  partition residue point
```

Example:

```text
target value:
  15PF

target capacity:
  whole = 40

remaining read:
  effective = 40
  occupied = 15
  remaining = 25
  overflow = 0
  partition residue = 0
```

If the target capacity has a selected partition, the selected partition limits
the effective receiving capacity without hiding the original whole:

```text
target capacity:
  whole = 60
  partition count = 3
  selected partition = 2

target value:
  15PF

remaining read:
  whole = 60
  effective = 40
  occupied = 15
  remaining = 25
  overflow = 0
  partition residue = 0
```

If the current target already exceeds the whole capacity, the remaining read
does not hide that:

```text
target value:
  45PF

target capacity:
  whole = 40

remaining read:
  effective = 40
  occupied = 45
  remaining = 0
  overflow = 5
```

For partitioned targets, overflow is measured against the effective capacity:

```text
target capacity:
  whole = 60
  partition count = 3
  selected partition = 2

target value:
  45PF

remaining read:
  whole = 60
  effective = 40
  occupied = 45
  remaining = 0
  overflow = 5
```

Shared transfer can use this remaining-capacity surface as its capacity witness.
That changes the meaning from "this target can receive 40 in this pass" to
"this target has 40 total capacity, 15 already present, so only 25 can enter
now."

```text
target before:
  15PF

remaining capacity:
  25

incoming routes:
  30PF
  30PF

accepted:
  13PF
  12PF

blocked:
  17PF
  18PF

target after settlement:
  40PF
```

This also clarifies the carrier/value distinction. A carrier can host many
values. They only compete for one capacity when relation structure attaches
them to the same target site or same capacity witness. Without that local
structure, shared carrier identity is just a shared numeric substrate, not a
claim that every value on the carrier contributes to one balance.

The partition read is deliberately low resolution. `selected partition = 2`
over `partition count = 3` means "the selected opening admits two of three
addressable parts of this whole." It does not decide whether those parts are
valve detents, throttle settings, gear stops, manufacturing tolerance buckets,
or a UI slider. Those readings can be built as higher-resolution structures on
top of the same partition evidence.

### Subsequent Steps

The next steps should stay incremental:

1. Explore whether `CarrierAxisSurface` should remain a concept-surface helper
   or become a core-adjacent axis/face split.
2. Decide where target mismatch should settle: failed helper read, explicit
   tension surface, or blocked-capacity residue.
3. Only after those behaviors are stable, consider core changes to value lanes,
   carrier locations, or carrier axis/face structure.

## Boundary Plus Flow Matrix

The sixteen combinations are:

| State | Meaning | Example uses |
| --- | --- | --- |
| `PN:Ø` | full signed position surface with no flow exposure | geometry coordinates, interval union/intersection, graph placement, segment boolean operations. |
| `PN:F` | signed position surface with forward calibrated flow | directed displacement, affine read, time-like axis, one-drive direction over signed positions. |
| `PN:I` | signed position surface with inverse calibrated flow | mirrored axis, opposed drive basis, reverse read over signed positions. |
| `PN:FI` | signed surface with both flow faces exposed | reversible mechanics on signed coordinates, exchange/cancellation systems, two opposed drives. |
| `P:Ø` | left-bounded positive position with no flow exposure | static length, radius as placement magnitude, read-only count, nonnegative support extent. |
| `P:F` | positive-only domain with forward flow | `uint`, accumulated amount, odometer, fill level increasing, forward-only capacity. |
| `P:I` | positive-only domain with inverse flow | draining positive capacity, spending budget, reverse gear over a nonnegative amount. |
| `P:FI` | positive-only domain with both fill and drain | tank level, queue length, inventory, adjustable preload, nonnegative reservoir. |
| `N:Ø` | right-bounded negative position with no flow exposure | left-of-anchor placement, deficit position, back margin, countdown region as geometry. |
| `N:F` | negative-only domain with forward flow | recovering deficit toward zero, countdown progress, moving from behind toward goal. |
| `N:I` | negative-only domain with inverse flow | accumulating deficit, retreat, increasing distance from goal. |
| `N:FI` | negative-only domain with both routes | debt/error that can deepen or be paid down, backlog below target. |
| `S:Ø` | bounded whole with no flow exposure | percent, interval, bounded selected range, bridge span as a whole, football field geometry. |
| `S:F` | bounded whole with forward flow | one-way finite capacity, progress through a bounded span, valve opening read in one direction. |
| `S:I` | bounded whole with inverse flow | reverse finite capacity, countdown over a bounded span, drain/read from far end. |
| `S:FI` | bounded whole with both flow faces exposed | explicit two-way bridge, bidirectional slot/track, reversible conduit, two-lane exchange through a finite whole. |

## Low Resolution: Calibration Versus Partition

Low resolution can mean two different things:

```text
calibrated estimate
  measure in feet instead of inches
  one tick means a coarser calibrated unit

whole partition
  divide this particular whole into 3 or 100 parts
  one part means a fraction of this instance
```

This difference matters for concept surfaces.

Example:

```text
3 ticks
  three units of participation

3 partitions of S
  one bounded whole divided into three parts
```

If a bridge opening is represented by `3 ticks`, that is a count or amount. If
it is represented by `3 partitions`, it is a coarse control surface over the
whole opening, such as closed/partial/open or low/medium/high.

`S` is the natural domain for whole partitioning.

## Unspecified, None, And Open

Do not encode "unspecified" as a primitive separate from absence unless there
is structure that makes it visible.

The useful distinctions are:

```text
absent
  no boundary or flow surface is present

Ø
  the surface explicitly reads as no boundary/no flow for the axis in question

open
  an attachment site or placeholder exists for future boundary/flow structure

blocked
  no-flow or no-passage is witnessed by capacity, boundary, residue, or tension
```

For example:

```text
bridge with no directed flow read
  S:Ø or related no-flow passage read

bridge with two explicit directions
  S:FI

bridge with open flow site
  passage + open attachment/controller slot

bridge with blocked crossing
  passage + capacity/residue/tension witness
```

## Football Field Example

Football-field math is not a primitive boundary/flow state. It is a composed
structure over the primitives.

Base field:

```text
field whole:
  S:Ø
  bounded whole partitioned into yard marks
```

The field itself has two endpoints and partitions, but no single intrinsic
flow. It is a bounded whole used for position and partition.

Team reads:

```text
team A read:
  origin = A goal line
  positive flow toward B goal line

team B read:
  origin = B goal line
  positive flow toward A goal line
```

These are two opposed calibration reads over the same bounded whole:

```text
ARead = Segment(field, AGoal, BGoal) -> Segment(AProgress, 0, 100)
BRead = Segment(field, BGoal, AGoal) -> Segment(BProgress, 0, 100)
```

Possession is a controller that selects the active read:

```text
Possession
  selected team read
  ball position
  predicted progress / yards-to-goal
  residue if unresolved
```

The same physical ball position can read differently:

```text
ball at one point on field
  through ARead -> A progress
  through BRead -> B progress
```

So the implementation should not force football fields into primitive `S:FI`.
The cleaner structure is:

```text
S:Ø bounded whole
+ two opposed calibration reads
+ possession/controller surface
```

## Implementation Plan

The first implementation pass lives in `Gojo.ConceptSurface` and keeps the
source of meaning in relation structure. The compact helper values below are
local reads over that structure.

### 1. Add Compact Value Types Or Helpers

The boundary axis can be represented by two booleans:

```csharp
public readonly record struct BoundaryExposure(
    bool LeftBoundary,
    bool RightBoundary);
```

The flow axis can also be represented by two booleans:

```csharp
public readonly record struct FlowExposure(
    bool ForwardFlow,
    bool InverseFlow);
```

These should be helper/read values, not the meaning-owning source. The source
should be relation structure.

Useful derived reads:

```csharp
BoundaryExposure.Label  // PN, P, N, S
FlowExposure.Label      // None, F, I, FI
```

The code uses `None` as the ASCII flow label for the no-flow state. The docs
use `Ø` where the visual distinction is useful.

### 2. Add Relation Builders

The first builders are:

```csharp
BoundarySurface.Create(root, exposure, wholeOrAxis)
FlowSurface.Create(root, exposure, selected)
BoundaryFlowSurface.Create(root, boundarySurface, flowSurface)
CarrierBoundarySurface.Create(root, carrier, boundary)
RelationFlowSurface.Create(root, relation, flow)
```

These should return ordinary `Relation` groups.

### 3. Add Local Readers

Readers should inspect the relation structure and return helper values:

```csharp
BoundarySurface.TryRead(surface, out BoundaryExposure exposure, out IElement wholeOrAxis)
FlowSurface.TryRead(surface, out FlowExposure exposure, out IElement selected)
BoundaryFlowSurface.TryRead(surface, out BoundaryExposure boundary, out FlowExposure flow)
```

If the structure is absent, the reader can fail. If the structure explicitly
stores `false,false`, the reader returns `PN` for boundary or `Ø` for flow.

### 4. Add Lane Basis And Value Occupancy

Boundary/flow exposure should expose legal lanes without requiring a separate
word classifier:

```csharp
BoundaryFlowLaneBasis.LegalLanes(boundary, flow)
BoundaryFlowLaneBasis.TryInferSingleLane(boundary, flow, out lane)
```

The legal-lane read is deterministic:

```text
Boundary lanes:
  P  -> P
  N  -> N
  PN -> P, N
  S  -> S

Flow lanes:
  None -> None
  F    -> F
  I    -> I
  FI   -> F, I
```

Then use the cross product. A carrier with `PN:FI` admits four occupied lanes:

```text
PF, PI, NF, NI
```

Values should be represented as:

```text
unsigned magnitude point
occupied lane marker
optional explicit host/witness basis
residue witness
```

In code, this is the role of:

```csharp
BoundaryFlowLaneSurface
BoundaryFlowValueSurface
```

The helper can report whether the occupied lane is legal for a local basis, but
it should not hide incompatibility:

```text
P:F value on NI
  magnitude survives
  lane marker survives
  read says incompatible
```

### 5. Keep Open Separate

Open/unresolved flow should not be encoded as a fifth flow state. Use existing
open structure:

```text
Placeholder
ConceptAttachment.OpenOutlet
ConceptAttachment.OpenInlet
ConceptGate
ConceptCapacity
```

This preserves:

```text
Ø flow
  explicitly no exposed flow

open flow
  unresolved future flow site
```

### 6. Keep Axis/Face Evidence Explicit

Current pass:

- represent boundary/flow states in ConceptSurface helpers
- use `CarrierBoundarySurface` to package carrier/domain boundary exposure
- use `RelationFlowSurface` to package relation/traversal flow exposure
- keep `CarrierAxisSurface` as a composed convenience for local notes/tests
- treat `FI` as a surface containing both faces or both routes
- treat `Ø` flow as no exposed flow relation

Later core exploration:

```text
split carrier axis from carrier face
```

Until that deeper split is justified by tests, concept surfaces should build
the explicit evidence they need instead of reading a carrier face as a hidden
source of flow meaning.

### 7. Add Tests Around Structural Reads

The first tests prove:

- `BoundaryExposure(false,false)` reads as `PN`
- `BoundaryExposure(true,false)` reads as `P`
- `BoundaryExposure(false,true)` reads as `N`
- `BoundaryExposure(true,true)` reads as `S`
- `FlowExposure(false,false)` reads as no-flow / `Ø`
- `FlowExposure(true,false)` reads as `F`
- `FlowExposure(false,true)` reads as `I`
- `FlowExposure(true,true)` reads as `FI`
- `S:Ø` can be partitioned as a bounded whole
- `S:FI` contains explicit forward and inverse flow surfaces
- carrier boundary/domain can be read without any flow exposure
- relation flow can be read without any carrier boundary/domain exposure
- football-field math is built as `S:Ø` plus two opposed reads and a controller

- `N:FI` yields `NF` and `NI`
- `PN:F` yields `PF` and `NF`
- `PN:I` yields `PI` and `NI`
- single-lane `P:F` and `N:I` surfaces can infer an occupied value lane
- a value can carry magnitude `5` while separately occupying `NI`
- an incompatible value lane stays inspectable instead of being silently coerced

### 8. Avoid Word Classification

Do not implement:

```csharp
IsBridge()
IsFunnel()
IsFootballField()
```

Instead, expose local reads:

```csharp
TryReadBoundaryExposure(...)
TryReadFlowExposure(...)
TryReadPartition(...)
TryReadReadFocus(...)
TryReadGate(...)
```

Words can remain documentation-level interpretations over those structures.

## Summary

The proposed primitive system is:

```text
Boundary:
  P, N, PN, S
  encoded by left/right boundary presence

Flow:
  F, I, FI, Ø
  encoded by forward/inverse flow exposure

Combined:
  16 boundary/flow states
```

The two axes should remain independent. Boundary describes the position domain.
Flow describes calibrated tension/change exposure. More advanced concepts such
as bridge traffic, football fields, filters, valves, and gauges are composed
structures built from these primitives plus reads, partitions, gates,
controllers, residue, and timelines.

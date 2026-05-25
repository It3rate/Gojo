# Gojo Structural Transform Relations

Gojo settlement now lives on the physical carrier/relation world.

There is no structural transform reader, contribution surface, route table, or
external balancing surface in the current core. A carrier owns value, signed
tension, and the relations attached to its local world. When carrier tension or
value changes, the connected relation world settles from inside that world.

## 1. Primitive Shapes

Gojo still treats these as relation shapes rather than wrapper types:

```text
point   P(C,k)   = L(C,k) -> C
segment S(C,a,b) = P(C,a) -> P(C,b)
link             = element -> element
```

A point is a carrier location related to its carrier. A segment is two points
related on one carrier. A transform is just another relation whose sides may be
points, segments, or larger relation surfaces.

Carrierless expressions such as `2 -> 5` remain conceptual for now. They are
floating relation shapes that can later be grounded on a carrier.

## 1A. Inspection And Settlement

The same directed relation can be touched in two different ways:

```text
inspection flow = read a location through a perspective surface
settlement flow = let carrier value/tension strike an accounting surface
```

Inspection preserves the relation's own ordered frame:

```text
S(C,a,b) reads L(C,x) as (x - a) / (b - a)
```

So `S(C,5,15)` reading `L(C,28)` gives `2 3|10`, and
`S(C,10,0)` reading `L(C,23)` gives `-1 3|10`.

Settlement still follows carrier flow. `P(C,10) -> P(C,0)` is a return surface
that hardens `23` into local value `3` and positive tension `20`. Changing the
inspection perspective changes the read; it does not change the native carrier
flow that creates settlement tension.

## 2. Carrier State

Each carrier instance can hold:

```text
Id        = unique physical identity; ordinary construction always allocates it
IsInverse = local orientation marker for a dual carrier face, not scalar sign
Value     = positive participation currently accounted on that carrier
Tension   = signed participation the carrier has not locally accounted for
```

`Carrier.Unknown` is the one carrier with `Id = 0`. User-created carriers do
not receive caller-supplied IDs. Two structurally identical carriers with
different IDs are physically different carriers.

Carrier value and tension are not points on the carrier line. They belong to
the carrier instance. Relation structure such as `P(C,10)` tells the carrier
what it can account for.

Input enters a carrier as tension. The carrier absorbs what it can into
`Value`. Any positive residue remains positive tension. Any below-origin
residue remains negative tension. Relations may then route that tension into
other carriers.

## 2A. Structural Participation

`CarrierLocation` is only a coordinate:

```text
L(C,5)
```

The tick value inside a location may be anonymous or shared:

```text
r = TickRef(10)
L(C,r)
L(C*,r)
```

Anonymous `L(C,10)` locations compare by carrier identity and tick count, but
they do not move together. Reusing a `TickRef` means the grounded locations are
the same coordinate reference on different carrier faces. If `r` changes from
`10` to `12`, both `L(C,r)` and `L(C*,r)` read at `12`.

Computation does not branch on whether a location is anonymous or shared.
Projection, reading, accounting, and settlement consume the exposed tick value.
Reference identity is only structural: it says which carrier sites move
together when the coordinate itself is perturbed.

A point is a relation that participates at that coordinate:

```text
P0 = L(C,5) -> C
```

Two points may have the same coordinate without being the same point. A segment
can share a point by holding the same relation instance:

```text
S = P0 -> P1
```

If `P0` moves, `S.0` moves because it is the same physical relation. A fresh
`P(C,5)` does not move merely because it is coincident.

Structural perturbation is separate from carrier value/tension. The target
surface decides which sites participate. The perturbation shape decides the
transform:

```text
L(C,+3)                     = translate matching carrier locations by +3
P(C,2) -> P(C,3)            = scale participating locations by 3:2
S(C,0,2) -> S(C,0,3)        = the same scale read through segment basis
```

Targeting one point changes that point. Targeting a relation surface walks its
participating child relations and changes their coordinates. Inexact structural
scale currently leaves the structure unchanged rather than inventing hidden
fractional positions; later this should become structural tension.

When a structural perturbation reaches a shared tick reference, the tick
reference moves, not just the local carrier face. This gives an explicit way to
say two carrier faces reuse the same unit coordinate:

```text
r0 = TickRef(0)
r1 = TickRef(10)
u  = S(P(C,r0),  P(C,r1))
u* = S(P(C*,r0), P(C*,r1))
```

Moving `r1` changes both surfaces. This is not a lineage tag or metadata
shortcut; it is shared numeric structure.

`SameTickRefAs` is therefore diagnostic/structural introspection. It is not a
loop breaker, not a settlement clock, and not a replacement for recomputing a
connected world after carrier tension changes.

## 3. Local Accounting

A bare carrier span from origin to a point is a forward projection/basis
surface:

```text
L(C,0) -> L(C,10)
```

If `C.Value` is set to `11`, the carrier keeps that state:

```text
C.Value   = 11
C.Tension = 0
```

The forward relation may later support a projection read, but it does not
constrain the carrier by itself.

A reverse same-carrier location relation is a return surface rather than the
same capacity read:

```text
L(C,10) -> L(C,0)
```

If `13` enters `C`, the carrier settles to:

```text
C.Value   = 3
C.Tension = 10
```

A point reset relation is grouped carry:

```text
P(C,10) -> P(C,0)
```

If `23` enters `C`, the carrier settles to:

```text
C.Value    = 3
C.Tension  = 20
```

A same-site point relation clamps:

```text
P(C,10) -> P(C,10)
```

If `23` enters `C`, the carrier settles to:

```text
C.Value    = 10
C.Tension  = 13
```

These are ordinary relation shapes attached to the carrier. There is no stored
boundary kind.

Forward same-carrier relations aligned with carrier flow are not boundaries by
themselves. Return and stop shapes are the current hardening surfaces.

## 4. Routing

When a cross-carrier relation ends at an origin receiver, the source side can
route positive carrier tension into the receiver carrier:

```text
P(C0,10) -> P(C1,0)
```

Adding `37` to `C0` settles as:

```text
C0.Value    = 7
C0.Tension  = 0
C1.Value    = 3
```

The compact form is local source accounting followed by routing through the
source unit.

When the receiver point is nonzero, the relation is currently a projection
surface rather than a carry route:

```text
P(C0,2) -> P(C1,3)
```

This says `2` ticks on `C0` project as `3` ticks on `C1`. A value of `6` on
`C0` reads as:

```text
L(C1,9) -> L(C0,0)
```

The left side is the projected read, and the right side is unresolved
source-basis residue. For non-exact division, the residue remains on the
original carrier:

```text
P(C0,3) -> P(C1,1)
project L(C0,10) on C1 = L(C1,3) -> L(C0,1)
```

Signed receiver or source sites make signed ratios:

```text
P(C0,1)  -> P(C1,-1)  gives subtraction-like projection
P(C0,-2) -> P(C1,3)   gives a negative ratio
P(C0,-2) -> P(C1,-3)  gives a positive ratio
```

The relation does not mutate either carrier. It is a read surface until a
later crystallized layer exposes that projection as carrier state.

The explicit boundary route is also valid:

```text
(P(C0,10) -> P(C0,0)) -> P(C1,0)
```

If the boundary exists first, positive tension can remain on `C0`. When the
outer route is later attached, that existing tension can be absorbed by `C1`.

Ordinary scalar negative locations live on the same carrier:

```text
Dual(L(C,k)) = L(C,-k)
```

That is distinct from `C_inverse`, which is a dual carrier face. Negative
tension mirrors positive tension. If a source carrier has below-origin tension
and is explicitly related to an inverse-oriented point receiver, the receiver
can absorb that tension as positive participation on that dual face:

This is an executable foothold for the later dual-flow / imaginary layer. It
is not part of the completed ordinary scalar math step.

```text
P(C0,10) -> P(C1_inverse,0)
```

If `C0.Tension = -23`, this relation settles as:

```text
C0.Tension       = -3
C1_inverse.Value = 2
```

An inverse carrier does not absorb anything merely because `IsInverse = true`,
and a scalar read from `C0` does not see `C1_inverse` merely because it is
inverse-oriented. The relation makes the negative-tension route active.

## 5. Dual-Face Values

When dual carrier faces are explicitly related, visible values can cancel
through that relation before residue remains on either side:

```text
P(C0,1) -> P(C1_inverse,0)
```

If `C0.Value = 5` and `C1_inverse` receives `7`, settlement leaves:

```text
C0.Value         = 0
C1_inverse.Value = 2
```

This is the first executable foothold for opposed-face carrier assemblies. It
does not replace ordinary scalar negation; scalar negation is signed
`CarrierLocation` structure on one carrier. Carrier `Value` storage itself
remains non-negative, with unresolved below-origin participation held as signed
tension.

## 6. Removed Surface Machinery

The old `ContributionSurface`, `ContributionReads`, and
`StructuralTransformReads` files have been removed. They were useful while the
model was still immutable, but they kept too much meaning in an external
machine that inspected structure and decided what it meant.

The current rule is simpler:

```text
carrier.Attach(relation)
carrier.Add / Remove
carrier settles its connected relation world
```

There is currently no direct public value assignment. A change enters as
tension through `Add`, `Remove`, or slot-style `Set`. `Set` computes the
difference from the current visible carrier value and feeds that difference as
tension. It does not force `Value`; the connected structure decides what
becomes visible value and what remains unresolved.

The default output surface is likewise slot-shaped:

```text
ValueSlot   = L(C, Value)
TensionSlot = L(C, Tension)
```

`ValueSlot` is the natural read of the carrier. `TensionSlot` is available, but
it is explicit inspection of unresolved internal pressure, not part of the
ordinary value output.

Fractional or grouped reads are explicit output ports over that value slot, not
a second carrier storage mode:

```text
S(C,0,10) -> P(U,1)
```

Reading `ValueSlot = L(C,27)` through this port gives:

```text
L(U,2) -> L(C,7)
```

That is the structured value `2 7|10`. Without such a basis port, the carrier
output remains native ticks. A reverse/reset surface such as `S(C,10,0)` may
participate in settlement, but it is not automatically the carrier's default
fractional read unless a port explicitly chooses that perspective.

Input ports use the same relation in the opposite direction:

```text
P(U,1) -> S(C,0,10)
```

Reading `L(U,2)` through this port gives `L(C,20) -> L(U,0)`, and the carrier
then receives a slot-style set toward `20`. A tick-level port is:

```text
P(U,1) -> P(C,1)
```

Current single-slot import commits the projected target and keeps any residue
as tension on the residue carrier. For `P(U,3) -> S(C,0,10)`, importing
`L(U,1)` yields `L(C,3) -> L(U,1)`: `C` receives the representable target
while `U` carries the unresolved residue. A richer port can later expose that
residue as an ordinary slot rather than internal pressure.

The input can be another carrier's value slot:

```text
source.ValueSlot = L(S,3)
P(S,1) -> S(C,0,4)
```

Importing through that port sets `C` toward `12`. This is a value read routed
into an input slot, not a drain of source material. Inexact forwarding keeps
the unrepresented residue as tension on the source/residue carrier.

A multi-slot port is a relation-shaped group of those same imports:

```text
(P(Sx,1) -> P(Sy,1)) -> (S(Cx,0,10) -> S(Cy,0,4))
```

An input value shaped as:

```text
L(Sx,3) -> L(Sy,2)
```

sets `Cx` toward `30` and `Cy` toward `8`. The tree shape is validated first,
so mismatched input cannot partially set the target. This is still not
consolidation or crystallization; it is multiple exposed slots moving together
through one port surface.

Known gaps:

- extent/stretch projection needs to be reintroduced internally
- material settlement for nonzero receiver projections
- refill and borrow
- branch and merge negotiation
- conserved split versus observation fanout
- inverse-flow / imaginary operation behavior beyond the first dual-carrier
  foothold
- opposed loop and cycle behavior
- carrierless floating relation expressions
- support/filter routing through settlement rather than separate boolean logic

These are not contradictions. They are intentionally left out until the same
small carrier/relation/tension mechanism can express them from inside the
connected world.

## 7. Guardrail

Carriers hold positive accounted participation and signed unresolved tension.

Relations bind the meaning of carrier sites.

If a behavior needs an external reader to decide what structure means, it does
not belong in the kernel yet.

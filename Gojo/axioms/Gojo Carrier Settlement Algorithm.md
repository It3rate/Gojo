# Gojo Carrier Settlement Algorithm

This note writes the current carrier-tension settlement loop as pseudo code.
The goal is not to create a second interpreter. The goal is to see which parts
still exist only because the host number system is weaker than the Gojo number
system we are building.

In an ideal Gojo-native runtime, adding tension to a carrier would already know
how to account through carrier-local boundaries, signed locations, relation
order, and routed tension. The pseudo code below is the current executable
shadow of that behavior.

Sections 1 through 7 are the current ordinary scalar math step. Sections 8 and
9 document an executable dual-carrier foothold for later inverse-flow /
imaginary work; they are not ordinary scalar negation.

## 1. Carrier Input

All input enters a carrier as signed tension.

```text
receive(carrier C, signed input):
    C.Tension += input
    absorb-local-tension(C)
    settle-connected-world(C)
```

`input` may come from a user action, a previous unresolved frame, a route from
another carrier, a future sensor, or a trolley-like traversal. The source does
not change the first step.

Direct value assignment is not a primitive operation. A request to move a
carrier from one visible value to another is represented as a slot-style
delta perturbation:

```text
request-value(C, desired):
    receive(C, desired - C.Value)
```

The current executable foothold exposes this as `Carrier.Set(...)`. It is not
a force write. If attached structure cannot account for the requested value,
the result may settle to another visible value with remaining tension.

A stronger future command such as "make this carrier read 3" should likewise
act by adding/removing tension, adjusting relations, or repeating perturbation
until the structure stabilizes. It should not bypass carrier accounting by
writing value directly.

The default single-carrier output port exposes two slots:

```text
value-slot(C)   = L(C, C.Value)
tension-slot(C) = L(C, C.Tension)
```

The value slot is the ordinary settled read. The tension slot is deliberate
internal inspection of unresolved participation. A normal output read should
not silently combine them.

A structured output port may read the value slot through an explicit basis:

```text
port = S(C,0,10) -> P(U,1)
project value-slot(C = 27) through port = L(U,2) -> L(C,7)
```

This is the executable foothold for `2 7|10`. The carrier still stores `27`
native ticks. The port exposes a whole slot and residue slot by reading those
ticks through relation structure.

Single-slot input uses the same shape in reverse:

```text
input-port = P(U,1) -> S(C,0,10)
project L(U,2) through input-port = L(C,20) -> L(U,0)
request-value(C, 20)
```

A native tick input port is just a unit-to-unit frame:

```text
P(U,1) -> P(C,1)
```

The current executable helper is `TrySetFrom(port, input)`. It commits the
projected target and leaves any projection residue as tension on the residue
carrier. For example:

```text
P(U,3) -> S(C,0,10)
project L(U,1) = L(C,3) -> L(U,1)
```

The carrier `C` receives a slot-style set toward `3`, while `U` keeps the
unrepresented `1` as tension. A later multi-slot port can expose that residue
as a normal output slot instead of leaving it as internal pressure.

Another carrier's value slot can be used as the input:

```text
source.ValueSlot = L(S,3)
input-port       = P(S,1) -> S(C,0,4)
target.TrySetFrom(input-port, source)
```

This reads the source value through the target input port and sets the target
toward `12` native ticks. This is value forwarding/observation, not material
transfer; the source value is not drained. If the projection leaves source-side
residue, that residue remains as tension on the source carrier.

Multi-slot ports are the same relation shape lifted one level:

```text
source-surface = P(Sx,1) -> P(Sy,1)
target-surface = S(Cx,0,10) -> S(Cy,0,4)
port           = source-surface -> target-surface
input          = L(Sx,3) -> L(Sy,2)
```

Each paired leaf slot imports through its corresponding relation frame:

```text
Cx receives target 30
Cy receives target 8
```

The whole shape is checked before anything commits. If one slot is incompatible,
no target slot is changed. Relation-shaped input means "treat this as grouped
slots"; carrier-location input means "treat this as one value through the
current frame."

## 1A. Downward Delta Normalization Target

The current executable pass mostly settles tension upward through return/carry
relations. The expected next extension is a downward normalization pass for
ratio-coupled layers.

The guiding idea is:

```text
stable values do not create tension merely by disagreeing
changes create tension
upper-layer tension can move down through inverse ratio relations
only representable quanta move; the rest remains tension
the lowest reachable non-crystallized layer absorbs what it can
ordinary upward settlement then re-expresses the changed base value
```

For example:

```text
P(C0,2) -> P(C1,3)
C1 receives +1 tension
```

Moving that tension downward through the inverse `3:2` relation may move `2`
ticks toward `C0` while leaving `1` tick of unresolved tension at `C1`. This is
not yet proven, but it is the first candidate rule. A resolution/crystal layer
may later account for smaller quanta.

This is different from ratio-mismatch tension. The relation does not wake up
because `C0` and `C1` have different stable values. It wakes because new
tension appeared somewhere in the connected structure.

## 2. Local Absorption

A primitive carrier face stores positive accounted value. Its signed tension is
unaccounted pressure.

```text
absorb-local-tension(C):
    if C.Tension > 0:
        C.Value += C.Tension
        C.Tension = 0
        return

    if C.Tension < 0:
        draw = abs(C.Tension)

        if C.Value >= draw:
            C.Value -= draw
            C.Tension = 0
            return

        C.Tension = -(draw - C.Value)
        C.Value = 0
```

This step says only what an unconstrained carrier can account for by itself.
It does not know about boundaries yet.

## 3. World Settlement

After local absorption, the connected relation world repeatedly applies its
relation shapes until a pass changes nothing.

```text
settle-connected-world(start):
    world = all carriers connected to start by attached relations

    repeat until quiet:
        snapshot = carrier states in world

        for each attached relation R in world:
            apply-relation(R)

        if carrier states equal snapshot:
            stop

        world = all carriers connected to start by attached relations
```

This is currently a quiet loop triggered by each public `Add`, `Remove`, or
`Set`. That means one public perturbation settles before the next public
perturbation enters the world. This is only the executable foothold.

The dynamic target is per-time-step settlement:

```text
time-step:
    collect all perturbations for this step
    apply them as carrier tension
    settle the affected connected worlds together
    leave unresolved participation as tension for later steps
```

Ordered change is still important, but it should be represented by explicit
sequential operations when order matters. Independent perturbations in the same
world should eventually be able to participate in the same settlement step.

## 4. Relation Application

A relation is applied by reading the carrier sites visible on its left and
right sides.

```text
apply-relation(R = A -> B):
    leftSites  = carrier locations inside A
    rightSites = carrier locations inside B

    if either side contains no carrier sites:
        no action

    if either side spans more than one carrier:
        no action in the current pass

    if left and right are on the same carrier face:
        apply-local-relation(left, right)

    else if left and right are on dual carrier faces
         and right is origin point:
        cancel-visible-dual-face-values(left.carrier, right.carrier)
        route-negative-tension(left, right)

    else if right is origin point:
        route-positive-tension(left, right)
```

This is still deliberately narrow. Multi-carrier surfaces, segment-to-segment
stretch, fanout, merge, support filtering, and crystallized reads should be
added by strengthening this same shape read, not by introducing unrelated
route objects.

## 5. Inspection Projection

Projection is a read, not settlement. It reads a carrier location through the
ordered frame exposed by a relation side:

```text
frame(P(C,k))     = 0 -> k
frame(S(C,a,b))   = a -> b
project L(C,x) through S(C,a,b) = (x - a) / (b - a)
```

When a relation connects two frames, projection preserves both ordered frames:

```text
project(source location):
    relative = source.location - source.frame.start
    projected = target.frame.start
              + relative * target.frame.extent / source.frame.extent
    residue remains as unreduced source-basis numerator
```

Examples:

```text
S(C,0,10)  -> P(U,1) reads L(C,23) as L(U,2)  -> L(C,3)
S(C,5,15)  -> P(U,1) reads L(C,28) as L(U,2)  -> L(C,3)
S(C,10,0)  -> P(U,1) reads L(C,23) as L(U,-1) -> L(C,3)
```

This does not mutate either carrier. It is perspective observation through a
surface.

## 6. Local Same-Carrier Relations

Same-carrier local behavior is directional.

```text
apply-local-relation(left, right):
    source = greatest left-side site
    target = smallest right-side site

    if source == 0 and target > 0:
        no mutation; this is a forward projection/basis surface
        return

    apply-local-accounting(C, source, target)
```

Directional local accounting:

```text
apply-local-accounting(C, source, target):
    if source == 0:
        no action

    if source < target:
        no action in the current pass

    if C.Value <= target:
        no action

    if source == target:
        local = min(C.Value, source)
    else:
        width = source - target
        local = target + ((C.Value - target) mod width)

    excess = C.Value - local
    C.Value = local
    C.Tension += excess
```

Concrete same-carrier reads:

```text
L(C,0)  -> L(C,k)  = forward projection/basis surface; no mutation by itself
L(C,k)  -> L(C,0)  = return/wrap to origin
L(C,k)  -> L(C,k)  = stop/clamp at k
P(C,k)  -> P(C,0)  = carrier-bound return/wrap
P(C,k)  -> P(C,k)  = carrier-bound stop/clamp
S(C,a,b) where a < b = forward ordered extent, not point carry by itself
S(C,a,b) where a > b = backward ordered extent, not dual face by itself
```

The important correction is that wrap/reset comes from relation order, not from
whether the sites were written as points instead of locations.

## 7. Positive Tension Routing

Same-oriented origin receivers can absorb positive source tension through the
source unit. A nonzero receiver site is a projection/basis relation in the
current executable pass, not a material carry route.

```text
route-positive-tension(source, receiver):
    if receiver.site != 0:
        no mutation

    unit = source.width if source has width else source.maxSite

    if unit == 0:
        no action

    if source is a local boundary:
        apply-local-accounting(source.carrier, source.maxSite, source.minSite)

    if source.carrier.Tension <= 0:
        no action

    received = source.carrier.Tension / unit
    remainder = source.carrier.Tension mod unit

    if received == 0:
        no action

    source.carrier.Tension = remainder
    receive(receiver.carrier, received)
```

Example:

```text
P(C0,10) -> P(C1,0)
C0 receives 37

C0 local accounting: Value 7, Tension 30
route by 10: C1 receives 3, C0.Tension = 0
```

By contrast:

```text
P(C0,2) -> P(C1,3)
C0 receives 6

C0 remains Value 6, Tension 0
C1 remains Value 0, Tension 0
```

That relation says that `2` ticks on `C0` project as `3` ticks on `C1`.
The projection read is executable as relation-shaped read state, but it does
not mutate either carrier:

```text
project(L(C0,6), C1) through P(C0,2) -> P(C1,3)
    = L(C1,9) -> L(C0,0)
```

Reading the same relation backward gives division:

```text
project(L(C1,9), C0)
    = L(C0,6) -> L(C1,0)
```

Non-exact projection keeps the source-basis residue on the original carrier:

```text
project(L(C0,10), C1) through P(C0,3) -> P(C1,1)
    = L(C1,3) -> L(C0,1)
```

Signed sites carry scalar sign:

```text
P(C0,1)  -> P(C1,-1)  gives subtraction-like projection
P(C0,-2) -> P(C1,3)   gives a negative ratio
P(C0,-2) -> P(C1,-3)  gives a positive ratio
```

This is still a read, not material routing. It becomes a crystallized carrier
value only when a later layer deliberately exposes that read as a carrier
surface.

## 8. Deferred Dual-Face Tension Routing

Ordinary scalar negation is `L(C,-k)` on the same carrier. That is separate
from carrier duality.

Dual-origin receivers can also absorb negative source tension as positive
value on their own carrier face when an explicit relation connects them.

```text
route-negative-tension(source, receiver):
    if receiver.site != 0:
        no mutation

    unit = source.width if source has width else source.maxSite

    if unit == 0:
        no action

    if source.carrier.Tension >= 0:
        no action

    available = abs(source.carrier.Tension)
    received = available / unit
    remainder = available mod unit

    if received == 0:
        no action

    source.carrier.Tension = -remainder
    receive(receiver.carrier, received)
```

Example:

```text
P(C0,10) -> P(C1*,0)
C0 receives -23

C0 local absorption: Value 0, Tension -23
route by 10: C1* receives 2, C0.Tension = -3
```

The receiver does not receive a negative scalar value. It receives positive
participation on the dual carrier face. A scalar read from `C0` will not see
`C1*` unless a later relation or crystallized read makes that higher-grade
assembly visible.

## 9. Deferred Dual-Face Visible Value Cancellation

When explicitly related dual carrier faces both hold visible value, their
shared amount can cancel before residue remains.

```text
cancel-visible-dual-face-values(A, B):
    shared = min(A.Value, B.Value)

    A.Value -= shared
    B.Value -= shared
```

This is the current foothold for higher-grade opposed-face assemblies. It is
not ordinary scalar sign and is not yet full crystallization.

## 10. Current Missing Rows

The current algorithm intentionally leaves these rows unresolved or partial:

- crystallized ratio read for `P(C0,k) -> P(C1,m)` where `m > 0`
- material settlement for nonzero receiver sites
- `source < target` same-carrier hardening relations
- relation-to-relation stretch where both sides preserve extent
- segment-to-point endpoint movement
- segment-to-segment basis projection
- refill/borrow from a higher carrier back into a lower carrier
- multi-carrier receiver surfaces
- branch, merge, observation, support, and filter routes
- crystallized reads that expose one tick interface over many carriers
- carrierless floating relation expressions

These are not reasons to add operation tags. They are the next places where the
same carrier-local accounting, relation order, duality, reversal, and tension
routing need to become more innate.

## 11. Desired Direction

The pseudo code should shrink over time.

The target is not a larger dispatcher. The target is that the carrier/relation
number system itself makes these operations natural:

```text
apply input tension
carrier absorbs what its structure can explain
unexplained participation remains as signed tension
relations expose compatible receivers
receivers absorb through their own structure
repeat until quiet or until remaining tension has no route
```

When the algorithm needs a new named branch, first ask what numeric relation
shape would make that branch physically visible.

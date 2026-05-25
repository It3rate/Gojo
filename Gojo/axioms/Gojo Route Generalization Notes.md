# Gojo Route Generalization Notes

This note used to describe transitional helper APIs such as
`TransferThrough`, `ObserveThrough`, `GatherThrough`, and the later external
structural-settle reader.

Those APIs have now been removed from Gojo. Route-like behavior should live in
carrier/relation structure and settle inside the connected carrier world.

## Current Executable Route Shapes

The current ordinary scalar executable shapes are intentionally small:

- runtime route discovery
  - first reads transient direct-route candidates from relation structure
  - reads finite target intake from local clamp structure such as
    `P(receiver,k) -> P(receiver,k)`
  - builds temporary source groups and target states for one settlement pass
  - does not store route offers, target capacity, or allocation as durable
    Gojo meaning

- `L(source,0) -> L(source,k)`
  - forward projection/basis surface
  - does not harden value or create tension by itself

- `P(source,k) -> P(source,0)`
  - same-carrier reset boundary
  - hardens local value to residue
  - exposes unabsorbed crossing energy as carrier tension

- `P(source,k) -> P(receiver,0)`
  - compact counter route
  - creates source tension through the boundary unit
  - routes quotient-like tension into the receiver carrier
  - leaves local residue on the source carrier

- `(P(source,k) -> P(source,0)) -> P(receiver,0)`
  - explicit counter route
  - can absorb carrier tension produced before the route existed

- multiple equivalent `P(source,k) -> P(receiver,0)` routes
  - are coordinated as one runtime settlement choice when they share the same
    source carrier, boundary, residue target, and unit
  - split the carried quotient across direct receiver routes instead of giving
    all source tension to whichever attached relation happens to settle first
  - leave the source residue/tension exactly as the boundary accounting
    requires
  - this allocation is transient runtime scratch, not durable Gojo structure

- local clamp-limited direct receivers
  - a receiver with `P(receiver,k) -> P(receiver,k)` exposes finite positive
    intake up to that clamp point for this runtime pass
  - a direct route into that receiver transfers only the ticks the receiver can
    keep without leaving positive target tension
  - if other equivalent routes are available, rejected ticks are redistributed
    to those receivers; otherwise they remain as source tension
  - if several source carriers feed the same finite receiver in one settlement
    pass, they negotiate against the same target capacity instead of each
    seeing a fresh receiver
  - this is a first backpressure foothold, not a complete target-capacity model

- `P(source,k) -> P(receiver,m)` where `m > 0`
  - ratio projection surface
  - does not clear source value or mutate receiver value in the current pass

## Deferred Dual-Carrier Foothold

These shapes are executable but belong to the later inverse-flow / imaginary
layer, not to ordinary scalar negation:

- `P(source,k) -> P(receiver_inverse,0)`
  - negative tension route
  - routes source negative tension into an inverse-oriented receiver

- `P(source,1) -> P(receiver_inverse,0)`
  - opposed visible-value balancing
  - cancels related positive values before leaving residue

These are relation shapes, not tags. A carrier settles them because they are
attached to its world.

## Removed Surface Machinery

The following helper layers were deliberately removed:

- route sequence helpers
- residual route helpers
- direct carrier-location transfer helpers
- direct carrier-location observation helpers
- direct carrier-location gather helpers
- contribution surfaces
- external structural-settle readers
- boundary projection helpers that returned quotient/residue outside the
  carrier world

The removed tests mostly tested helper APIs against themselves. New tests
should attach relation structure to carriers, perturb carrier values, and then
inspect carrier value and tension.

## Remaining Gap

Extent/stretch projection, refill/borrow, loop hardening, same-carrier cycles,
branch/merge, explicit grouped route contexts, richer target backpressure, and
dynamic balancing are not solved by the removed helpers. They should come back
only as structural relation shapes that carriers can settle internally.

The pressure to watch for is API shape: if a behavior needs another named
method before it can be discovered, try to encode the behavior in carrier
locations, points, segments, and relation surfaces first.

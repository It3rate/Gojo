# Gojo Implementation Plan

Gojo is the current Core4 numeric kernel direction. `Core4.Kernel` and older
projects remain preserved for comparison, but new kernel work should happen in
Gojo.

For the current pseudo-code view of carrier input, local accounting, relation
application, and tension routing, see `Gojo Carrier Settlement Algorithm.md`.
For the port/fusion direction, see
`Gojo Ports Fusion And Operational Reads.md`.
For the unit-shape and crystallization direction, see
`Gojo Unit Shape And Crystallization.md`.
For the speculative tension-to-structure direction, see
`Gojo Structure Through Tension.md`.

The current model is:

- carriers are physical mutable sites with unique IDs
- carriers store positive value and signed unresolved tension
- scalar sign is a read of signed locations on the same carrier; carrier
  value storage itself remains positive
- carrier duality creates an opposed-flow / higher-grade carrier face, not the
  ordinary negative side of the scalar carrier
- relations bind carrier sites into points, segments, boundaries, routes, and
  transforms
- carriers have native unit shapes for their ticks; ordinary relation unit
  shape is calculated from carrier unit shapes and relation surface context
- relations have physical identity; structural equality is still useful for
  tests and read comparison, but identity decides shared participation
- settlement happens inside the connected carrier/relation world
- behavior should come from numeric relation shape, not tags, helper objects,
  contribution tables, or external settle readers

Current step boundary:

- ordinary scalar math uses signed locations on one carrier, point/carry
  boundaries, ratio projection, backward ratio reads, and structural
  perturbation
- dual-carrier / inverse-flow behavior is only an isolated foothold for the
  later higher-grade or imaginary layer; it must not be treated as ordinary
  scalar negation
- crystallization now has an executable foothold: operation surfaces can derive
  reusable unit shapes, create carriers with those shapes, and project current
  compatible operation reads onto those carriers by delta

## Phase 1. Carrier Reads

In place:

- carrier identity
- automatic unique carrier IDs, with `Carrier.Unknown` fixed at `Id = 0`
- `Carrier.Unknown` can serve as a wildcard frame for discovery-oriented port
  reads/imports
- carrier locations
- shared `TickRef` coordinates for explicit same-unit references across
  carrier faces
- same-carrier positive reads
- same-carrier negative reads through signed `CarrierLocation` ticks
- orthogonal reads as zero
- inverse carrier reads are invisible to the scalar carrier unless an explicit
  relation/crystallized read connects them
- direct carrier-held `Value` and signed `Tension`
- `IElement.Dual`, implemented by carriers, carrier locations, and relations
- structural `Reverse` for relation traversal order
- relation identity separate from shape equality
- anonymous equal locations are value-equal but independent; locations built
  from the same `TickRef` share physical coordinate state
- reads and projections use tick value, not tick-reference identity
- shape-driven structural perturbation:
  - signed carrier location perturbations translate matching carrier sites
  - a translated shared `TickRef` moves each grounded carrier face that uses it
  - relation-shaped ratio perturbations scale participating sites exactly
  - inexact scale is left unresolved rather than rounded

## Phase 2. Internal Structural Settlement

In place:

- `carrier.Attach(relation)` attaches relation structure to the connected
  carrier world
- `Add`, `Remove`, and slot-style `Set` feed signed tension and settle that
  world automatically
- `Set(target)` means `receive(target - current visible Value)`; it is not a
  force write
- `ValueSlot` exposes settled carrier value as `L(C, Value)`
- `TensionSlot` exposes unresolved tension as an explicit internal read
- explicit basis ports can project a value slot into grouped whole/residue
  slots, e.g. `27` through `S(C,0,10) -> P(U,1)` reads as `2 7|10`
- single-slot input ports are the same relation shape in reverse, e.g.
  `P(U,1) -> S(C,0,10)` maps `2` to native carrier target `20`
- native tick ports use `P(U,1) -> P(C,1)`
- current single-slot imports commit the projected target and preserve any
  projection residue as tension on the residue carrier
- structural imports commit by retargeting the exposed structural coordinate;
  if that coordinate is a shared `TickRef`, every grounded carrier face sees
  the same move
- a carrier can import another carrier's `ValueSlot` through a compatible
  input port; this forwards a value read without draining source value
- relation-shaped multi-slot ports can import matching relation-shaped values;
  all slots are validated before any target is changed
- structural ports can import matching relation-shaped values into existing
  points, segment endpoints, and shared `TickRef` coordinates without creating
  a new basis or touching carrier value state
- relation-shaped structural input is decomposed only when the receiving
  surface is group-shaped; frame-readable structural input is treated as an
  atomic basis/span value
- ports reference existing relation surfaces; a same-shaped lookalike is a
  separate surface and must not be used to reinterpret or manufacture hidden
  internal structure
- port groups are the current object-boundary foothold; operations should act
  on exposed port surfaces rather than raw carrier internals
- operation surface reads over port groups are executable as a foothold:
  point output folds projected operands additively, segment output folds them
  multiplicatively, and unresolved operand residues stay visible in relation
  shape
- carrier unit shape is executable as a foothold: default carriers get fresh
  linear atom shapes, carriers can share a unit shape without sharing identity,
  inverse carriers share the forward unit shape, relation unit shape is
  calculated from frame/port/operation shape, and `TryCrystallize` creates a
  carrier with the derived shape without projecting value
- carrier unit shape is fixed after carrier creation; `UnitShape.Surface`
  exposes a snapshot for inspection, so perturbing that read surface cannot
  mutate the crystallized basis
- carriers with shared unit shape keep independent `Value` and `Tension`; unit
  compatibility is not shared storage
- crystal value projection is executable as a foothold:
  `TryCrystallize(input, out carrier, out read)` and
  `TryProjectCrystallizedValueFrom(input, carrier, out read)` bind an
  operation's output frame onto a compatible crystal carrier and feed that
  carrier with the projected target value
- concrete port frames require carrier identity, while `Unknown` source/output
  frames let arbitrary carriers try the relation during discovery
- input enters as signed tension and the carrier absorbs what it can into
  positive value before relations route the residue
- a forward local span such as `L(C,0) -> L(C,10)` gives projection/basis
  structure without hardening the carrier
- a reverse local span such as `L(C,10) -> L(C,0)` gives return/wrap
  accounting
- same-carrier point relations perform reset/clamp accounting
- cross-carrier point receivers route source positive tension

Current stable examples:

- unbounded carrier input keeps its value
- bare carrier addition/subtraction adjusts accounted value, and underflow
  remains signed tension
- slot-style `Set` adjusts by delta from the current visible value, then
  settles through attached structure
- default output reads return settled value only; unresolved tension is visible
  through an explicit tension slot
- `L(C,0) -> L(C,10)` leaves value `11` as value `11` with no tension
- `L(C,10) -> L(C,0)` turns value `13` into value `3` plus tension `10`
- `P(C,10) -> P(C,0)` turns `23` into value `3` plus tension `20`
- `P(C,10) -> P(C,10)` clamps `23` to value `10` plus tension `13`
- `P(C0,10) -> P(C1,0)` turns `37` into `C0 = 7`, `C1 = 3`
- chained point/carry relations express decimal and binary-style positional
  counters by changing only the boundary unit
- `P(C0,2) -> P(C1,3)` is a ratio projection surface, not a carry route; the
  projected read is executable through `Relation.Project(...)`
- exact and inexact ratio projection preserve quotient plus unreduced residue;
  signed locations express scalar subtraction-like reads without inverse
  carriers
- offset and reversed segment frames project from their first endpoint rather
  than sorting endpoints
- `(P(C0,10) -> P(C0,0)) -> P(C1,0)` can absorb positive tension after the
  boundary has already produced it
- `(portA -> portB) -> P(Result,1)` reads projected port operands additively
- `(portA -> portB) -> S(Result,0,1)` reads projected port operands
  multiplicatively
- additive operation unit shape collapses compatible operands and preserves
  incompatible operands as unreduced relation shape
- multiplicative operation unit shape preserves repeated participation, e.g.
  `length * length` reads as two participations of the length unit shape
- current crystal value projection preserves operand residue as read residue
  rather than mutating source carrier tension
- incompatible additive operation shapes can crystallize as unit shape, but
  current value projection refuses to flatten them into one scalar

Deferred dual-carrier foothold:

- inverse-oriented point receivers can route source negative tension
- related opposed carriers can cancel visible values before leaving residue
- these behaviors are tested separately from ordinary scalar math because
  full inverse-flow / imaginary operation semantics are not complete yet

Removed:

- `ContributionSurface`
- `ContributionReads`
- `StructuralTransformReads`
- tests that treated settlement as an external read over a structure surface

## Phase 3. Consolidation

Next cleanup targets:

- review `RelationReads` and keep only projections that remain useful as local
  reads over already-existing relation structure
- keep support/span reads as projections until they can participate in
  internal settlement
- avoid adding replacement helper APIs for old removed route concepts
- keep structural-port targeting narrow: it should expose existing structure,
  not manufacture ad hoc basis surfaces during import

## Phase 4. Missing Structural Mechanics

Add only when the numeric shape is clear:

- extent/stretch projection inside the carrier/relation world
- multi-component crystal value surfaces for unreduced additive shapes
- refill and borrow
- branch and merge negotiation
- conserved split versus observation fanout
- inverse-flow / imaginary operation behavior beyond the first dual-carrier
  foothold
- opposed loop and cycle behavior
- route hardening
- carrierless floating relation expressions
- support/filter routing through settlement rather than separate boolean logic

For branch/fanout, keep the material/observation distinction structural:
material transfer drains or distributes participation, while observation can
copy a read without consuming the observed carrier. Avoid flags if relation
shape can carry the distinction.

## Migration Rule

Only copy or preserve old Kernel code when it still follows the Gojo model.

Prefer deleting a helper over keeping a second meaning system alive.

If a concept is difficult to express, extend the numeric carrier/relation
format rather than falling back to enums, opaque tags, or ad hoc metadata.

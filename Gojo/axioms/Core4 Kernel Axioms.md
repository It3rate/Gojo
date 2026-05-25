# Core4 Kernel Axioms

This file describes the current `Core4.Kernel.Core` implementation.

The source of truth for this pass is the code in `Core4.Kernel/Core` and the
behavior locked by `Core4.Kernel.Tests/Core/RelationTests.cs`. Older notes are
preserved under `axioms/historical`; they remain valuable design lineage, but
they should not override the behavior stated here.

For the newer carrier, positive-flow, cross-read, resolution, approximation,
overflow, and crystallized tick direction, see:

- `Core4 Carrier Read And Resolution Axioms.md`
- `Core4 Boundary Overflow Axioms.md`

## 0. Current Scope

The kernel is the small numeric substrate extracted from the bootstrap
experiment. It currently contains:

- `IElement`
- `Measure`
- `Relation`
- `RelationFold`
- `Carrier`
- support span, support read, and support projection helpers

It does not yet contain explicit site, pin, frame, freeze, attachment,
traversal, trolley, seam, or runtime tension-settlement types. Those concepts
may be read through the current structures only where the code makes that read
available.

## 1. Element Participation

An `IElement` is anything that can participate in ordered relation and expose a
partition surface.

The current required surface is intentionally small:

- `PartitionCount`
- `Partitions()`

This keeps the primitive substrate weak. Meaning is discovered by folding,
projecting, composing, and inspecting relations instead of by assigning a large
taxonomy of named element kinds.

## 2. Measure As Carrier-Neutral Coordinate

A `Measure` is a raw signed coordinate. By default it belongs to
`Carrier.Unbound`, which means it is carrier-neutral structure rather than a
claim already attached to physical or semantic carrier meaning.

The current construction surface is:

- `new Measure(raw)` = unbound measure
- `Measure.Unbound(raw)` = explicit unbound measure
- `Measure.On(carrier, raw)` = measure attached to a carrier

A lone `Measure` is allowed, but it is not a segment, not a basis, and not a
flow by itself. It has:

- no implicit origin
- no partitions
- no structural claim side

Raw sign remains part of the substrate coordinate. Claim direction comes from
relation structure, not from whether the raw measure is positive or negative.

`Measure.Offset` is the current temporary way to model a sliding read surface.
Unbound measures read through the offset because they have no carrier-local
landmarks. Carrier-attached measures do not read through the offset by default,
because their positions may already be meaningful on that carrier. Fixed
measures are internal readings that do not slide. This is not yet a full frame
system.

`Carrier.Primary` remains available for an explicitly generic concrete carrier,
but it is no longer the default for bare measures.

## 3. Relation As Ordered Primitive

A `Relation` is the primitive ordered structure:

- `Left`
- `Right`

It is itself an `IElement`.

`Relation` does not store grade, role, denominator, operation kind, origin, or
semantic tags. Those readings emerge when another structure folds or projects
the relation.

The current open and claim ladder is:

- `Relation.Open()` = `0/0` open site
- `Relation.Claim(value)` = `value -> open`
- `Relation.ClaimOpposed(value)` = `open -> value`

`Compose` and `Apply` share one mechanism: if exactly one side contains open
sites and the other side is closed, the first open site is filled. Otherwise a
new peer relation is created.

## 4. Open, Unbound, And Resolved Readings

The kernel currently reads relation state through this numeric ladder:

- `0/0` = open or coincident structure with no partition matter
- `m/0` = unbound magnitude, point claim, or unresolved attachment potential
- `m/u` = magnitude read through a resolved basis-like denominator

This ladder is not a separate boolean or symbolic state system. It is encoded
by the numeric relation surface.

Open-site count is derived from structure. Nested open sites are traversed
structurally; no object stores an open-site count.

## 5. Fold As Current Reading

`RelationFold` is an external reading of two elements. It stores:

- the original `Left` and `Right`
- `First`
- `Second`
- `Reduced`
- `Collapsed`
- `Projected`

Raw measure pairs fold to current extent and orientation:

- equal raw measures -> `0/0`
- different raw measures -> `abs(delta)/sign(delta)`

The denominator of a raw measure pair is therefore the current primitive
orientation, not a stored tick count. Tick traversal is exposed through
partitions.

Fold reads live relation leaves when it is created, but its own `First`,
`Second`, `Reduced`, `Collapsed`, and `Projected` readings are fixed outputs of
that fold instance.

## 6. Partition Surface

Partitions are the current inspectable flow surface of an element.

A lone `Measure` has no partitions. A folded or related structure can expose
`PartitionStep(Start, Width)` values. Width sign carries flow direction.

For a nonzero fold surface:

- `Second == 0` uses unit width `1`
- otherwise `abs(Second)` is the unit width
- the first partition carries the leading remainder when the magnitude does
  not divide evenly
- structural claim side determines where one-sided claims start

This keeps intermediate states inspectable as ordered numeric spans instead of
collapsing immediately to a scalar read.

## 7. Append And Smear

The kernel does not implement ordinary algebraic operators as primitive cases.
It has one fold mechanism with different readings:

- unpartitioned with unpartitioned appends magnitude
- unpartitioned with partitioned resolves or presents the magnitude through the
  partitioned side
- partitioned with partitioned smears the partner contribution over the host
  partition surface

This is why direct resolved-with-resolved fold is multiplication-like in the
current tests, while point-claim accumulation is addition-like.

## 8. Preserve Before Collapse

`Reduced`, `Collapsed`, and `Projected` are distinct.

`Reduced` keeps the richest available structure for the current fold. Open
structure is preserved when it would otherwise be flattened too early. Closed
composite children are also preserved when they still carry resolved structure.

When two closed branch composites meet, the fold preserves the four branch
contributions before lower projection:

- aligned channel: `AA`, `BB`
- orthogonal channel: `AB`, `BA`

This preserved contribution lattice is more primitive than a complex-number or
directed-segment formula. Cancellation, netting, and scalar reads are later
collapses.

`Collapsed` lowers by one structural grade where possible. `Projected` repeats
that lowering until no further structural collapse is available. The kernel
intentionally stops before a destructive ratio-only floating point read.

## 9. Carrier And Crystallized Tick Surface

A `Carrier` is a small numeric identity:

- `Id`
- `Domain`

The current built-ins are:

- `Carrier.Unbound`
- `Carrier.Primary`

`Carrier.Unbound` is the carrier-neutral floor for floating structure. A
structure on this carrier may still have internal numeric shape, but it has not
yet been pinned to a carrier-local meaning.

An uncalibrated carrier may still have meaningful carrier-local landmarks. For
example, a temperature carrier can contain a water-freezing site and a
water-boiling site even before a Celsius-like scale is attached. What is
missing is not carrier meaning; what is missing is a reusable calibrated read
that can report values such as `0` or `100`.

A carrier can be crystallized from an element. Crystallization records:

- source structure
- variable source
- optional frozen source
- ordered tick surface
- variable tick surface
- frozen tick surface

The tick surface is an ordered list of carrier participations with signed
repeat counts. Sign currently reads as expansive or contractive participation.
Repeated carrier occurrences remain visible unless an explicit surface chooses
to compress exact repeats.

Carrier projection is intentionally shallow. `Carrier.TryProject` accepts only
inputs whose carrier occurrence surface matches the crystallized surface in
order. Positive signed entries multiply. Negative signed entries divide. A
mismatch rejects projection instead of guessing.

## 9A. Attachment And Scaling Direction

The likely next frame mechanism has three levels:

- unbound structure with internal numeric shape
- origin-only attachment, where one structure site is pinned to one carrier site
- origin-plus-unit attachment, where a structure relation is pinned to a
  carrier relation and can be reread through that carrier scale

The kernel does not yet implement this attachment object. The intended
Core4-native direction is that scaling should come from a relation between an
unbound basis and a carrier-local basis, not from an opaque scale tag.

If a structure represents seven unit repetitions and the attached unit changes,
unfrozen dependent structure should be reread through the new unit. Frozen
landmarks or invariants should resist that reread and produce compatible motion
or tension instead. Orthogonal participation should not be guessed by special
case; it should be rediscovered by the same frame relation, support projection,
and tension mechanisms once those are explicit.

Under this view, a calibration scale is usually an unbound structure first. It
attaches its origin and unit to meaningful positions on an uncalibrated carrier.
The carrier landmarks and the calibration scale are then coupled, but not the
same object:

- the carrier can hold the freezing and boiling landmarks
- the unbound scale can attach `0` to freezing and `100` to boiling
- the boiling landmark can be discovered through that scale as `100`
- the raw temperature carrier still has no direct scalar value until read
  through some attached calibration
- In this system, C and F both could be the same calibration structure, 
  just origins attached at different positions, and units attached to 
  create different lengths.

Changing or replacing the calibration scale should not move the carrier
landmarks by default. It should change the read produced by that calibration.
Moving a frozen carrier landmark is a different act and should produce
constraint response or tension in attached calibrations.

The newer resolution direction treats precision and approximation as
attachments to crystallized carrier ticks. A precise read such as `23 4|10 C`
preserves multiple carrier-layer tick surfaces instead of reducing to a decimal
projection. An approximate read groups native ticks without inventing hidden
finer precision.

## 10. Support Reads Are Lower Projections

Support is not a separate boolean algebra.

The support layer first discovers directed spans on a carrier, then builds
state spans with two numeric slots:

- reverse
- forward

From those slots it can project:

- support
- tension
- exclusive support
- forward unclaimed flow
- reverse unclaimed flow
- occupancy and larger-flow reads

The read equations are themselves represented as relation-shaped truth tables
over reverse/forward coefficient pairs. This keeps boolean-like reads as lower
projections over richer flow/support structure.

## 11. Canonical Direction

The kernel should prefer:

- relation structure over operation tags
- numeric open/unbound/resolved states over enums
- calculated readings over stored summaries
- structural side over raw measure sign
- preserved contribution lattices over premature scalar formulas
- carrier tick surfaces over opaque unit metadata

When a concept is hard to express in the current structure, the preferred move
is to extend the numeric structure, not to attach symbolic bookkeeping.

## 12. Current Gaps

The following are intentionally not settled by the kernel yet:

- explicit pinned site and frame-shift structure
- origin-only and origin-plus-unit attachment of unbound structure to carriers
- frame scaling when an attached unit changes
- full freeze and attachment commitment
- runtime hardening and relaxation of relation denominators
- rich repeat lineage for powers, roots, loops, and transformed reuse
- boundary laws for reverse/forward support slots
- path, traversal, trolley, seam, clamp, reflection, and closure behavior
- exact arithmetic beyond the current integer rounding surface
- carrier-aware collapse beyond the current shallow numeric gather

These gaps should remain visible. They are not failures of the kernel; they are
the next places where Core4-native numeric structure needs to grow.

## 13. Historical Corrections

The historical notes contain several useful ideas that are not exactly what the
current kernel implements.

The most important corrections are:

- Some older axioms say an extent requires first freezing a fixed origin. The
  kernel currently allows raw ordered measure pairs to fold directly into an
  extent-like floating reading. Stable origin, pin, and frame-shift are not
  explicit yet.
- Earlier kernel text treated a bare `Measure` as if it lived on
  `Carrier.Primary`. The current kernel treats bare measures as
  `Carrier.Unbound`; carrier-local meaning must be explicit.
- Some older bootstrap text says a raw relation such as `10 -> 20` folds to
  `10/10`. The current kernel folds raw measure pairs to `abs(delta)/sign`, so
  `10 -> 20` reads as `10/1` and `20 -> 10` reads as `10/-1`.
- Earlier experimental relation notes describe signatures, merge scopes,
  commitments, transforms, aspects, and multiplicity expansion. The kernel
  extracted here does not contain those fields. Its relation stores only
  `Left` and `Right`.
- Older site notes describe `Site`, `Pin`, `Freeze`, `Basis`, and `Attachment`
  as explicit construction stages. The kernel currently represents only the
  numeric relation surfaces that can support those ideas later.
- The broader multidimensional axioms describe area, complex rays, angles,
  self-fusion powers, and nonlinear units. The kernel currently preserves
  aligned and orthogonal contribution structure and shallow signed tick
  surfaces, but it does not yet implement full angle, ray, power, or root
  semantics.
- Some notes imply total length or cancellation should fall out directly from
  fused opposed units. The current kernel preserves contribution structure
  first; cancellation is a later collapse or support projection.

These corrections make the extracted code the present center of gravity. The
historical notes remain design pressure, not executable law.

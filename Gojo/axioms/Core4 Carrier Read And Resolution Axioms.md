# Core4 Carrier Read And Resolution Axioms

> Status: historical / partially superseded for Gojo. The current Gojo rule is
> that ordinary scalar negation is a signed `CarrierLocation` on the same
> carrier, e.g. `Dual(L(C,5)) = L(C,-5)`. `Carrier.Dual()` returns a dual
> carrier face, which is not automatically visible as scalar negative when read
> from `C`. Same-lineage duplicate carriers are also superseded; current Gojo
> carriers are unique instances, and relation structure carries sameness. See
> `Gojo Duality And Relation Settlement Table.md`.

This note records the current architectural direction for carriers,
crystallized ticks, cross-carrier reads, resolution, approximation, and
partition overflow.

It is more forward-looking than the current `Core4.Kernel.Core` code. The code
still uses signed `long` reads in several places. This note states the target
model we are converging on so later implementation work has a stable center of
gravity.

For the boundary and overflow route layer implied by this model, see:

- `Core4 Boundary Overflow Axioms.md`

## 1. Carrier Positivity

All carrier-local storage should be read as positive-flow storage.

A carrier has:

- a native origin
- a native tick
- a native direction
- positive tick counts along that direction
- provenance for how that tick was crystallized

Negative is not primitive carrier storage. A negative value appears when a
positive location on one carrier is read through an opposed carrier or opposed
frame.

So:

- `5` on an inverse carrier is still five positive ticks on that carrier
- reading that location through the forward carrier may project as `-5`
- the sign belongs to the read relation, not to the stored location

This keeps raw substrate storage `uint`-like in spirit. Signed values emerge at
higher fused or cross-read levels.

## 2. Location And Read Are Distinct

A measure-like value has a location carrier. A read has a reading carrier or
frame.

By default, a value is read through its own carrier. It is also legal to read a
value through a different carrier.

Cross-carrier reads are not invalid. They may produce:

- an aligned value when the reader has a projection from the location carrier
- an opposed value when the reader is inverse to the location carrier
- zero when the reader is orthogonal or cannot see that carrier
- unresolved tension when the read relation is partially compatible but
  under-specified

For example, if the positive `X` carrier reads a value located on the `Y`
carrier and no projection relation connects them, the read is `0`. That is a
legal and expected read: the `X` carrier sees no `X` participation in the `Y`
location.

## 3. Crystallized Carrier As Unit

A carrier is not merely a label or holder of metadata. It is a crystallized
math object.

Crystallization means:

- take a stable structure
- treat that structure as one native tick
- create a new positive-flow carrier whose tick is that structure
- optionally preserve provenance back to the structure that produced it

This collapses the sharp distinction between unit and value:

- a unit is a crystallized structure used as one tick
- a value is accumulated ticks across the carrier layers currently being read
- a value can later be crystallized into a new unit/carrier

Any stable structure can be crystallized if the system wants to use it as a
native tick. It may be primitive, compound, approximate, partitioned,
orthogonal, self-fused, or metaphorical.

## 4. Carrier Recursion

Carriers can be recursively built from other carriers.

Examples:

- inch carrier: one inch as native tick
- foot carrier: twelve inch ticks crystallized as one foot tick
- mile carrier: 5280 foot ticks crystallized as one mile tick
- eighth-mile carrier: one mile tick partitioned into eight and crystallized
- furlong carrier: the eighth-mile carrier used by name and provenance

Each layer is a legitimate carrier if the system chooses to crystallize it.
The difference between them is not ontology. The difference is:

- which structure was crystallized
- what provenance is preserved
- what resolution and overflow behavior is currently exposed
- what the observer intends to use as the native tick

## 5. Inversion And Directed Structure

A carrier exposes one native flow direction. Inversion creates an opposed
carrier participation rather than storing negative values on the original
carrier.

For a directed inch segment:

- `inch+` is a positive inch carrier participation
- `inch-` is an inverse inch carrier participation
- fusing `inch+` and `inch-` creates a bilateral signed higher structure

Both sides remain positive on their own carriers. Signed displacement emerges
only when they are fused or cross-read.

This also preserves provenance:

- two equal-looking carriers may be independent
- two participations may reuse the same crystallized tick
- one participation may be an inverse of another
- repeated participations may be true repeats rather than coincidental copies

The visible tick length alone is not enough to determine structural identity.

## 6. Multiplication, Division, And Addition As Tick Attachments

Operations should emerge from how a structure attaches to a carrier tick.

The carrier supplies the native `one`. The attachment supplies one numeric
relation to that tick:

- zero attachment: additive or no interior mixing
- positive attachment: expansive grouping of carrier ticks
- negative attachment: contractive partitioning of a carrier tick
- repeated positive attachments: self-fusion or power-like behavior
- inverse attachment: opposed flow participation

This is the same mechanism as resolution:

- contractive attachment exposes finer distinctions under one carrier tick
- expansive attachment groups carrier ticks into coarser discernment
- zero attachment preserves additive point-like behavior

No separate symbolic operator taxonomy is needed at the primitive layer.

## 7. Resolution Is Discernment, Not Hidden Precision

Resolution is the smallest distinction exposed by a particular read.

It is not:

- a display choice over a hidden exact value
- proof that the substrate is physically discrete
- a claim that finer structure does not exist
- permission to invent finer structure

If a read measures in whole miles, it does not claim that inches do not exist.
It only says that this read cannot distinguish inch-level differences. If a
review has five stars, it does not secretly contain a decimal review. It
contains only the distinctions the five-star structure exposes.

Finer detail may exist in the world or in another carrier, but it is not
available from the coarse read alone.

## 8. Approximation As Expansive Attachment

Approximation is not exact value plus fuzz.

An approximate read uses an expansive attachment over the native tick.

For example, "about 100 miles" with ten-mile discernment should be read as:

- mile carrier native tick: one mile
- approximation attachment: ten carrier ticks per discernment
- read count: ten approximation ticks
- read label through mile carrier: about `100`

The read does not contain `95` or `105` unless another relation explicitly
introduces that interpretation. One later projection may read the approximate
tick as a span, another as a center label, another as a distribution, and
another as tension. Those are later reads, not hidden facts in the original
approximation.

## 9. Precise Reads As Contractive Attachment

Finer reads use contractive attachment.

For Celsius with ten exposed sub-ticks per degree:

- Celsius carrier native tick: one degree Celsius
- precision attachment: ten later-layer ticks per Celsius carrier tick
- read: `23 4|10 C`

The read should stay in that structured form. Decimal projection such as
`23.4 C` destroys the explicit resolution and should not be the canonical
Core4 representation.

The carrier tick remains one degree. The precision attachment is a structure
attached to that tick.

For detailed overflow and refill behavior at these attachment boundaries, see
`Core4 Boundary Overflow Axioms.md`.

## 10. Layered Tick Reads

Values read through an attachment may expose multiple carrier layers at once.
There is no separate earlier-layer-versus-later-layer ontology. Both surfaces
are carrier layers; the relation between them is what makes one earlier and the
other dependent in this read.

- earlier carrier-layer ticks
- later carrier-layer ticks that have not crossed the boundary relation

Flow moves through the attachment. When the attachment crosses its boundary,
overflow increments or decrements the earlier carrier-layer surface. The
unoverflowed part remains visible as ticks on the later carrier layer.

So a precise Celsius read may be:

- earlier Celsius layer: `23`
- later precision layer: `4|10`
- canonical label: `23 4|10 C`

The `4|10` part is not an arithmetic afterthought and not a different kind of
thing from the `23`. It is the live state of another carrier layer bound by the
ten-part boundary relation.

## 11. Boundary Direction

Overflow direction depends on the read intention.

When accumulating:

- the later attachment layer fills
- boundary crossing overflows upward into carrier ticks

When generating or inspecting:

- carrier ticks may feed downward into attached partition structure
- the attachment exposes substructure without changing the parent tick

When grouping expansively:

- several carrier ticks fill one attachment boundary
- the coarse attachment emits only when the group boundary is crossed

The same carrier and attachment can therefore support measuring, inspecting,
and generating reads without becoming different ontologies.

## 12. Celsius, Fahrenheit, And Calibration Structures

A temperature carrier may have meaningful landmarks before it has a convenient
human scale.

For example:

- water-freezing site on the temperature carrier
- water-boiling site on the temperature carrier

A Celsius-like calibration structure can attach:

- its origin to the freezing site
- its unit length so boiling reads as `100`

A Fahrenheit-like calibration can use the same kind of calibration structure:

- its origin is pinned at a different carrier location
- its unit length is pinned differently
- the same boiling location can read as `212`

The Celsius and Fahrenheit calibration machines may therefore be structurally
the same kind of object. Their difference is attachment:

- origin placement
- unit length
- exposed resolution
- provenance

The temperature carrier landmarks do not move when a calibration changes.
The calibration read changes.

## 13. Cross-Resolution Reads

Reading a location through a different resolution carrier is legal.

For example:

- location carrier: fine mile carrier
- location: `95`
- reading carrier: ten-mile approximation carrier
- read: `10`

That `10` can later be interpreted back through the mile carrier as:

- label `100`
- a coarse segment-like compatibility region
- a distribution
- unresolved later-layer ticks or tension

Those are interpretations of the approximation carrier. They are not secret
contents of the original read.

Coarse-to-fine projection cannot invent finer detail. Fine-to-coarse projection
can intentionally discard detail or carry it as attached-layer ticks or tension.

## 14. Orthogonality And Zero Reads

Reading across unrelated carriers is legal and often reads as zero.

If the `X` carrier reads a value located only on the `Y` carrier, and no
relation projects `Y` into `X`, then the `X` read returns no visible `X`
participation.

This is not failure. It is the carrier saying:

- no aligned participation is visible here
- if this matters, create a relation that explains how the carriers meet
- otherwise the cross-read is zero

The current higher-grade fold already points in this direction by preserving
aligned and cross contributions:

- `AA`
- `AB`
- `BA`
- `BB`

Aligned reads contribute in their own flow. Cross reads expose opposition,
orthogonality, or zero depending on the carrier relation.

## 15. Relation To Signed Numbers

There are no primitive negative substrate positions.

What ordinary math calls a negative number is a read:

- a positive location on an opposed carrier
- read through the current carrier or frame

So `-5` is not stored as negative five ticks. It is five positive ticks on an
inverse participation read from the forward perspective.

When the forward and inverse carriers are fused, the resulting higher structure
may expose a signed projection. That projection is useful, but it is not the
lowest carrier storage.

## 16. Relation To Complex-Like Fold

The same pattern appears at higher grade.

When two paired carrier structures fold, the primitive result should preserve
the contribution lattice before scalar collapse:

- aligned-aligned
- aligned-opposed or aligned-orthogonal
- opposed-or-orthogonal-aligned
- opposed-opposed or orthogonal-orthogonal

This is why `AA/AB/BA/BB` preservation matters. It keeps the system from
prematurely treating cross terms as a symbolic sign rule. Sign, cancellation,
rotation, and scalar projection are later reads of preserved carrier
participation.

## 17. Unit, Value, Carrier, And Scale

Unit, value, carrier, and scale are different uses of the same crystallization
law.

- a unit is a crystallized tick used for reading
- a carrier is a positive-flow tick line whose tick has been crystallized
- a value is the visible carrier-layer tick state exposed by the current read
- a scale is an attachment structure that maps its origin and unit onto another
  carrier
- a resolution is a scale-like attachment to a carrier tick

The same structure can move between these roles as it is crystallized,
attached, read, or reused.

## 18. Implementation Pressure On The Kernel

The current kernel still uses `long` in `Measure`, `PartitionStep`, and fold
surfaces. That remains a useful projection layer, but the target model suggests
future separation of:

- location carrier
- reading carrier
- positive tick count
- inverse/opposed carrier participation
- attached carrier-layer ticks
- earlier carrier-layer overflow

`PartitionStep` should eventually mean the current exposed inspection or
overflow surface, not atoms of underlying reality.

`Carrier.TryProject` should eventually become one member of a broader read
system where values can be read across carriers, including zero reads, opposed
reads, approximation reads, and reads that preserve later-layer tick state
instead of collapsing it away.

## 19. Open Questions

These points remain unsettled:

- whether carrier-local storage should actually become `ulong` or only behave
  as if it were positive-flow storage
- the exact numeric encoding of contractive, expansive, and zero attachments
- how attached-layer ticks should carry provenance when several attachments are
  nested
- when a coarse read should project back as a label, span, distribution, or
  tension
- how to encode inverse carrier creation without adding symbolic tags
- how relation structure should express same-provenance reads now that Gojo no
  longer has same-lineage duplicate carrier instances
- how much of this should live in `Carrier`, `Measure`, `RelationFold`, or a
  new frame/read object

The guiding constraint is stable:

meaning should stay in numeric carrier structure, crystallized ticks,
attachments, overflow, attached-layer tick state, and cross-carrier reads rather
than being hidden in enums, opaque metadata, or ordinary algebraic
simplification.

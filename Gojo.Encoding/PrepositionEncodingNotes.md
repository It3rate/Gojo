# Preposition Encoding Notes

> Superseded experiment. This note records the older
> `RoleFrame + Comp + Scale + Zone + Focus + Cr + Apply` approach. It is useful
> as a contrast case, but it is not the active grammar direction. The current
> direction is to let words such as `near` be read from low-resolution Gojo
> structure, residue, and provenance rather than from a stored preposition
> concept tree.

The old premise was that prepositions should be encoded as crystallized read
shapes rather than symbolic relation labels.

The old working rule was:

```text
preposition concept =
  open role frame
  comparison or participation read
  contextual basis and resolution
  parameter scale or region
  selected focus
  qualification result
  residue/tension
```

This keeps the concept reusable without pretending it is fully applied. The
word `near`, for example, should not contain a store, house, trace, pad, or
specific distance. It should contain open roles and a focus over a comparison
scale. A phrase or domain context can bind those roles later.

## Open Roles

A preposition starts with open participation sites:

```text
figure-slot
ground-slot
basis-slot
resolution-slot
```

Those slots are real unresolved structure. They are not nullable metadata. If a
sentence, CAD rule, or local context cannot fill one, the result is still
inspectable as missing-basis or missing-resolution tension.

```text
RoleFrame(figure, ground, basis, resolution)
```

For ordinary language:

```text
store near my house

figure = store
ground = my house
basis = physical or travel distance, inferred from place context
resolution = ordinary-language rough distance
```

For CAD:

```text
trace near pad

figure = trace
ground = pad
basis = board clearance distance
resolution = manufacturing clearance rule
```

The same preposition shape can apply in both cases, but the basis changes the
meaning of the qualification.

## Shared Parameter Scale

Words such as `near`, `far`, `halfway`, and `arrived` should not each invent a
separate comparison system. They are better treated as different focus reads
over one lower structure:

```text
Comp(figure, ground, resolution)
  -> Scale(comparison, salient-landmarks)
  -> Zone(scale, high-separation)
  -> Zone(scale, middle-salience)
  -> Zone(scale, low-separation)
  -> Zone(scale, goal-settlement)
```

The zones are not necessarily equal partitions. They are salient regions. In
language, endpoint zones and midpoint zones are often more meaningful than the
less distinct spans between them.

## Focus Reads

A preposition word is a crystallized focus read:

```text
far-focus      = Focus(scale, far-zone)
halfway-focus  = Focus(scale, halfway-zone)
near-focus     = Focus(scale, near-zone)
arrived-focus  = Focus(scale, arrived-zone)

near = Cr(near-focus; context=RoleFrame)
far  = Cr(far-focus; context=RoleFrame)
```

This means `near` and `far` are separate reusable concept elements, but they
keep provenance into the same comparison scale. They are not naked labels on a
region, and they are not unrelated predicates.

## Application

Applying a preposition is a local binding plus a read:

```text
BindRoles(RoleFrame, store, my-house, ordinary-distance-basis, ordinary-distance-resolution)
Comp(store, my-house, ordinary-distance-resolution)
Apply(near, binding, comparison)
```

The application can produce a qualification, but it should preserve residue:

```text
store and house bind the open roles while the distance value remains low resolution
```

For CAD:

```text
BindRoles(RoleFrame, trace, pad, board-clearance-basis, board-clearance-resolution)
Comp(trace, pad, board-clearance-resolution)
Apply(near, binding, comparison)
```

The same focus shape now carries different tension:

```text
near can be descriptive or a clearance-rule warning in this context
```

## Labels

Labels should attach to crystallized focus reads, not to unstructured regions.

Bad:

```text
Scale(...) with label "near" stuck on one interval
```

Also bad:

```text
near, far, halfway, and arrived each rebuild their own comparison machinery
```

The old preferred structure was:

```text
shared comparison scale
  -> focus zone
  -> focus read
  -> crystallized word
  -> applied read
```

This gave the model useful redundancy, but it still made `near` a named focus
node in a definition tree. The current direction is flatter: the word should
usually be provenance for a low-resolution Gojo read, with residue preserving
what remains unresolved.

## Legacy Fixture

The retained legacy sample is:

```text
Tests/Gojo.Encoding.Tests/Fixtures/EncoderJson/concept-definitions/legacy-preposition-parameterized-focus.json
```

It covered scalar comparison prepositions only:

```text
RoleFrame + Comp + Scale + Zone + Focus + Cr + BindRoles + Apply
```

This should not be extended as the active grammar path. Boundary and region
words should instead be rebuilt from boundary, area, offset, contact, flow,
residue, and settlement evidence:

```text
inside
outside
within
around
through
against
```

Those should reuse boundary, area/anti-area, offset, contour, and flow evidence
instead of becoming a separate symbolic grammar.

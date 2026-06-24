# Preposition Encoding Notes

Prepositions should be encoded as crystallized read shapes, not as symbolic
relation labels.

The current working rule is:

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

Preferred:

```text
shared comparison scale
  -> focus zone
  -> focus read
  -> crystallized word
  -> applied read
```

This gives models useful redundancy: `near` is an independent concept seed, but
it still shares lower structure with `far`, `halfway`, and `arrived`.

## Current Fixture

The first executable sample is:

```text
Gojo.Encoding.Tests/Fixtures/EncoderJson/concept-definitions/preposition-parameterized-focus.json
```

It currently covers scalar comparison prepositions only:

```text
RoleFrame + Comp + Scale + Zone + Focus + Cr + BindRoles + Apply
```

The next likely fixture should extend the same idea to boundary/region
prepositions:

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

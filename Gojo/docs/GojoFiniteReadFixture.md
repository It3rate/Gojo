# Finite calculation and coarse publication

`Gojo.Structures.FiniteReadSurface` is a small, opt-in fixture for calculating
with limited available information. It uses guarded `long` operations without
`Int128`, `BigInteger`, an exact overflow remainder, or an exact-reader fallback.
The `long` is the host storage envelope; each receiver can select a much smaller
capacity. This fixture does not yet pack those domains into smaller lanes.

## Native structure

The capacity is a `BoundedWholeSurface`:

```text
G(axis, Point(axis,minimum) -> Point(axis,maximum), Segment(axis,0,quantum))
```

The domain must include zero and the quantum must be positive. Inputs and
outputs use a shared exposed tick basis `Segment(axis,0,1)`. Quantum selects
the publication granularity in those ticks; it does not secretly rescale them.

Publication uses the existing `QualifiedReadSurface` / `ReadWitness` spine:

```text
selected:   exposed source or operand publications
basis:      Segment(axis,0,1)
value:      frozen bounded nominal tick
variation: lower endpoint -> upper endpoint
coverage:  unresolved (no confidence inferred)
scope:     frozen capacity and quantum
excluded:  zero displacement if exact, otherwise source -> variation
provenance: native operation equation -> finite scope
```

Only the finite support reader gives the variation pair these endpoint roles.
A `Placeholder` in the lower role means the lower bound is unavailable; one in
the upper role means the upper bound is unavailable. Neither is numeric
infinity. Arbitrary placeholders elsewhere acquire no new meaning.

The source and equation remain connected for correspondence. They are not
queried to recover a lost answer. A producer can retain finer private data or
discard it; the publication does not promise a refinement route. The fixture
does not make a privacy boundary or remove externally retained source graphs.

## Selected arithmetic law

`Publish` clips to the domain endpoints. An unclipped value is rounded toward
zero to a multiple of the quantum. The support becomes a conservative rounding
cell, not an exact discarded difference. A clipped endpoint is preserved even
when it is not a multiple of the quantum.

`Add`, `Subtract` and `Multiply` consume already materialized finite
publications. They do not execute pending supplier equations. Operand nominal
ticks and any known support endpoints must fit the selected receiving domain
and axis. Missing calibration, incompatible axes or inadmissible endpoints
produce an unresolved native witness with the attempted contract attached.

Addition/subtraction propagate available interval endpoints. Multiplication
encloses closed intervals using four guarded corner products. Open product
inputs conservatively leave both endpoints open, except that an exact zero
factor establishes zero. These are support rules, not distributions. They do
not assume statistical independence or estimate probabilities.

| Calculation in `[0,15]`, quantum 1 | Published nominal | Exposed support |
|---|---:|---|
| `12 + 3` | 15 | `[15,15]` |
| `14 + 3` | 15 | `[15,?]` |
| Previous publication minus 3 | 12 | `[12,?]` |
| Two independent high-clipped publications subtracted | 0 | `[?,?]` |
| The same publication subtracted from itself | 0 | `[0,0]` |

Clipping order is observable. `(14 + 3) - 3` has nominal 12, while
`14 + (3 - 3)` has nominal 14. A compiler must preserve publication sites;
exact-algebra reassociation is not generally valid for this selected law.

For signed capacity `[-127,127]`, quantum 4, publishing 10 gives nominal 8
and support `[8,11]`; publishing -10 gives -8 and `[-11,-8]`. Publishing zero
gives nominal zero and `[-3,3]`. The cell means the service no longer
distinguishes those values, even if its supplier knows the original value.
An explicit mathematical cancellation can still establish zero before a
separate coarse publication, which can deliberately discard that specificity.

## Sufficient local reads

`QualifiedReadSurface.TryReadPublishedValue` reads only a materialized nominal
observation and its exposed basis. Unlike `TryReadValue`, it does not call
`ReadCurrent` or evaluate a connected supplier. Support is a separate question.

`FiniteReadSurface.TryGreaterThan` inspects only the published support. A
high-clipped product with support `[15,?]` settles `> 10` as true. It cannot
settle `> 20`; its nominal 15 alone is not evidence for false. The return value
distinguishes a settled Boolean answer from unresolved demand. No supplier
traversal or wider calculation is triggered by the unresolved answer.

This read-only fixture does not set carrier values, introduce physical tension,
or mutate a creature. A later explicit Gojo response can connect exposed loss
to demand, select another service, or request mutation. Those responses still
need their own structure and tests.

## Scope and verification

Focused conformance lives in `Tests/Gojo.Tests/Core/FiniteReadSurfaceTests.cs`.
The existing exact equation, projection and bounded-quantity readers keep
their exactness/range contracts. The phototaxis backend still uses those
existing laws; it has not silently switched to finite arithmetic.

Division, explicit averages, general shared-variable interval solving, lazy
arbitrary circuits, runtime budgets, automatic precision selection, packed
storage and loss-driven evolution remain future work. An average should be
built under its own finite contract; it must not silently recover a clipped
sum by consulting an exact shadow calculation.

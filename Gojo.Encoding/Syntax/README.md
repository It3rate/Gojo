# Gojo text syntax

This folder contains the first executable text surface for constructing Gojo
structure. It is an authoring and inspection notation, not a parallel object
model and not yet a canonical language.

The complete working specification is
[`GojoTextSyntax.md`](../../Gojo/docs/GojoTextSyntax.md). That document explains
the structural commitments, every currently implemented constructor, the
action-scope examples, ladder digits, and the questions that remain open.

## What the parser does

`GojoSyntax.Parse(...)` accepts a versioned text document and expands it
directly into ordinary Gojo `IElement` structures:

```gojo
gojo 1;

let $distance = carrier();
let $shared_site = tick(7);

root point($distance, $shared_site);
root span($distance, 0, 20);
```

There are no decimal literals. Values are integer ticks on a carrier. A local
`$alias` preserves identity while the source is parsed and then disappears; it
is not a crystallization. A named `?slot` becomes a shared, inspectable
`Placeholder`.

## Action and witness distinction

Possible lanes, permitted actions, and witnessed applications remain separate:

```gojo
gojo 1;

let $magnitude = carrier();
let $positive_lanes = basis($magnitude, P, FI);
let $pf = lane(PF);
let $pi = lane(PI);

let $inverse_pf = lane_action(
    $pf,
    $pi,
    $pf -> $pi,
    ?why_the_action_is_legal
);

root group($positive_lanes, $inverse_pf);
root act(
    $pf,
    $inverse_pf,
    $pi,
    ?unaccounted_change,
    ?application_provenance
);
```

`lane_action(...)` makes the directed action discoverable. `act(...)` records a
selected/action/result witness. It does not mutate a carrier. Material change
still requires explicit tension, transfer, settlement, output, or patch
structure.

## Partition-ladder example

```gojo
gojo 1;

let $fine = carrier();
let $whole = carrier(unit_of($fine));
let $carry = point($fine, 20) -> point($whole, 1);

let $raw = point($fine, 32);
let $relative = group(
    point($whole, 1),
    point($fine, 12),
    $carry
);

root group($raw, $relative, $carry);
```

The parser preserves both reads. It does not silently normalize `32` into one
whole tick plus twelve fine ticks. Such normalization is an action that should
eventually expose its accepted participation, carry, remainder, result, and
provenance.

## Executable semantic fixtures

Two small fixtures exercise the first action semantics without adding a hidden
executor:

- [`ambient-access.gojo`](../../Gojo.Encoding.Tests/Fixtures/Syntax/ambient-access.gojo)
  keeps a four-lane ambient basis while one tool exposes only `PF` and no
  inverse capability. A blocked attempt retains `PI` as residue. A wider tool
  exposes the action to that already-possible lane.
- [`preservation.gojo`](../../Gojo.Encoding.Tests/Fixtures/Syntax/preservation.gojo)
  distinguishes product observation, a same-identity before/after timeline,
  explicit fork topology, erasure without a claimed inverse, and rollback that
  depends on retained history.

The access form is:

```text
access(ambient, exposed_ports, capability_ports,
       blocked_attempt_ports, residue, provenance)
```

The history forms are:

```text
step(time, start, end, before, transition, after, residue)
timeline(time, initial[, step...])
```

These are structure constructors. `access` does not make absent mathematics
false, and `timeline` does not execute its transition.

## Next design priorities

1. **Multi-component value surfaces.** Let one value expose several connected
   components or open components without flattening them into a scalar or enum.
   Begin with slot-bound common structures while preserving the difference
   between open, absent, hidden, and blocked components.
2. **Ladder observation and settlement witnesses.** Make raw fine ticks,
   relative digits, carry, borrow, lower-resolution reads, and introduced finer
   rungs coexist without replacement. Keep a non-mutating projection separate
   from actual redistribution through carrier tension.
3. **Action composition and small algebras.** Extend the new access fixture with
   identity, composition, lossy action, mathematical inverse, and historical
   rollback reads at several scopes.
4. **Durable history.** Decide when history contains a stable point, a snapshot,
   a crystallized rung, or only a reference to mutable structure.
5. **A readable writer.** Generate stable, comprehensible source aliases from
   existing Gojo structure while keeping aliases outside the source of meaning.
6. **Numeric encoding.** Once the structural surfaces have survived these
   examples, encode the same graph in an integer-oriented representation for
   storage and transport.

The numeric form is intentionally deferred, not forgotten. It should preserve
element identity, signed locations, `long`/`ulong` carrier data, ordered
relations, unit shapes, shared tick references, open slots, residue, tension,
and provenance. It must be a compact encoding of the same Gojo structure—not a
hash of the text, a floating-point projection, or an enum table that becomes a
second source of meaning.

## Most useful feedback now

The highest-value feedback now concerns the first slot-bound component surface:

- Which common value should be the first fixture: signed integer, complex value,
  or a deliberately generic two-component structure?
- Which slots are present-but-open, and which components should be absent at a
  lower-resolution read?
- Should a common value template expose an ordered component frame, or should
  order be derived only from relations among the slot bindings?
- When a ladder value has both raw and relative reads, which structure owns the
  carry action and which residue must remain visible?
- When history must survive arbitrary retargeting, should the first durable form
  be a snapshot, a crystallized rung, or both connected by correspondence?

Spelling, alias aesthetics, a canonical writer, and the compact numeric packet
layout are lower priority until those structural questions settle.

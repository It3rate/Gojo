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

## Next design priorities

1. **Scoped actions and small algebras.** Exercise one-lane inversion,
   signed-orientation change, and full-surface orthogonal introduction. Define
   how an allowed action, a blocked action, composition, identity, and inverse
   action remain structurally discoverable.
2. **Multi-component value surfaces.** Let one value expose several connected
   components or open components without flattening them into a scalar or enum.
   This is needed for wider action scope and later electrical values.
3. **Ladder settlement and normalization witnesses.** Make raw fine ticks,
   relative digits, carry, borrow, lower-resolution reads, and introduced finer
   rungs coexist without replacement.
4. **A readable writer.** Generate stable, comprehensible source aliases from
   existing Gojo structure while keeping aliases outside the source of meaning.
5. **Numeric encoding.** Once the structural surfaces have survived these
   examples, encode the same graph in an integer-oriented representation for
   storage and transport.

The numeric form is intentionally deferred, not forgotten. It should preserve
element identity, signed locations, `long`/`ulong` carrier data, ordered
relations, unit shapes, shared tick references, open slots, residue, tension,
and provenance. It must be a compact encoding of the same Gojo structure—not a
hash of the text, a floating-point projection, or an enum table that becomes a
second source of meaning.

## Most useful feedback now

The highest-value feedback concerns structural behavior rather than punctuation:

- For each selected scope, what exactly may the inverse action preserve, move,
  expose, or introduce?
- Should a target lane or component be present before an action, or may the
  action introduce it while retaining correspondence to the prior structure?
- What is the smallest useful multi-component value: a set of occupied lanes,
  an ordered component frame, or a basis plus open component slots?
- When a ladder value has both raw and relative reads, which structure owns the
  carry action and which residue must remain visible?
- Which operation inversions are laws of a particular algebra, and which are
  broader structural operations such as dual, reversal, reciprocal, undo, or
  inverse repetition?

Spelling, alias aesthetics, a canonical writer, and the compact numeric packet
layout are lower priority until those structural questions settle.

# Gojo text syntax

This folder contains the first executable text surface for constructing Gojo
structure. It is an authoring and inspection notation, not a parallel object
model and not yet a canonical language.

The complete working specification is
[`GojoTextSyntax.md`](../../Gojo/docs/GojoTextSyntax.md). That document explains
the structural commitments, every currently implemented constructor, the
action-scope examples, ladder digits, and the questions that remain open.

The [September 2026 design review](../../Gojo/docs/GojoSyntaxReview-2026-09.md)
checks these commitments against the parser, kernel, and Blocks examples. It
led to the [local blend fixture](../../Gojo/docs/GojoLocalBlendFixture.md), which
exercises partial components, mixed contributions, local mismatch, explicit
feedback, and stale evidence. It adds no syntax constructors or general
learning policy.

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
whole tick plus twelve fine ticks. A normalization read should expose its
basis, remainder, result, and provenance. Material carry additionally needs
accepted participation and settlement evidence.

## Executable semantic fixtures

Small fixtures exercise the first action semantics without adding a hidden
executor:

- [`ambient-access.gojo`](../../Gojo.Encoding.Tests/Fixtures/Syntax/ambient-access.gojo)
  keeps a four-lane ambient basis while one tool exposes only `PF` and no
  inverse capability. A blocked attempt retains `PI` as residue. A wider tool
  exposes the action to that already-possible lane.
- [`preservation.gojo`](../../Gojo.Encoding.Tests/Fixtures/Syntax/preservation.gojo)
  distinguishes product observation, a same-identity before/after timeline,
  explicit fork topology, erasure without a claimed inverse, and rollback that
  depends on retained history.
- [`local-blend.gojo`](../../Gojo.Encoding.Tests/Fixtures/Syntax/local-blend.gojo)
  connects a partial component and partial filter to two calibrated inputs,
  an alternative mixture, a goal, focus, and response routes. Its tests execute
  prediction and signed gain perturbations, retain a stalled step and history,
  and demonstrate equal outputs that remain different structures. See the
  [execution and limits](../../Gojo/docs/GojoLocalBlendFixture.md).

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

The [explicit unit quotient read](../../Gojo/docs/GojoUnitQuotientReads.md)
adds an authored denominator/cancellation experiment with retained standards.
[`unit-quotients.gojo`](../../Gojo.Encoding.Tests/Fixtures/Syntax/unit-quotients.gojo)
uses the existing `group`, `ports`, `point`, `resolution` and binding forms.
Cancellation is an explicit witnessed read; neither parsing nor ordinary unit
compatibility silently reorders the expression.

The next [quantity fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/quantity-quotients.gojo)
adds `quotient_unit(expression)` for complete stable reciprocal/quotient units.
Ordinary groups express numeric division requests with samples, output and
standard; tests invoke the kernel reader. Signed division, unresolved domains,
calibration refusal and staged resolution retain their accounting. Capturing a
unit performs no implicit cancellation. This extends the syntax alongside the
kernel; it does not introduce a parser execution loop.

The [mathematical conformance checkpoint](../../Gojo/docs/GojoMathConformance.md)
corrects the audited general-ratio remainder and range bugs, validates positive
resolution factors, and adds an executable
[`math-conformance.gojo`](../../Gojo.Encoding.Tests/Fixtures/Syntax/math-conformance.gojo)
fixture. It covers signed arithmetic, source residue, inversions, repeated units,
coarse reads, partial patterns and explicit carry activation. It adds no syntax
constructors. Fractional source remainder is expressible with existing
`at($source,n) -> point($source,d)` structure; whole-tick imports refuse it
before mutation. Nested quotient composition, material refinement/borrow and
measurement uncertainty remain low-level priorities before broader discovery.

1. **Multi-component value surfaces.** Let one value expose several connected
   components or open components without flattening them into a scalar or enum.
   Begin with slot-bound common structures while preserving the difference
   between open, absent, hidden, and blocked components.
   The local blend fixture now exercises this with explicit local refinement.
   Extend it toward connected utility feedback and changing participation,
   without assuming a single winning binding or a universal response law.
2. **Ladder observation and settlement witnesses.** Make raw fine ticks,
   relative digits, carry, borrow, lower-resolution reads, and introduced finer
   rungs coexist without replacement. Keep a non-mutating projection separate
   from actual redistribution through carrier tension.
3. **Action composition and small algebras.** Extend the new access fixture with
   identity, composition, lossy action, mathematical inverse, and historical
   rollback reads at several scopes.
4. **History and adaptive memory.** Distinguish exact captured evidence from a
   live reference, a crystallized promise, or memory deliberately compressed
   and revised with a narrower retained-information contract.
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

Define the preservation contract during these fixtures even while the binary
layout remains deferred. The current diagnostic snapshot loses placeholder
identity and unit descriptor tables, including captured quotient standards;
it is not yet a lossless numeric counterpart to this text surface.

## Most useful feedback now

The first generic component fixture is now executable. The next questions are:

- How should feedback from neighboring spaces change connection participation
  and responsiveness while keeping their separate evidence visible?
- What supports introducing, weakening, or pruning a route without treating
  agreement between correlated sources as independent evidence?
- Which partial-pattern and focus forms deserve shorter notation after more
  than one experiment has exercised them?
- When a ladder value has both raw and relative reads, which structure owns the
  carry action and which residue must remain visible?
- Which historical promises require exact capture, and which can be preserved
  more usefully by deliberate lossy compression into a structure or algorithm?

Spelling, alias aesthetics, a canonical writer, and the compact numeric packet
layout are lower priority until those structural questions settle.

# Gojo text syntax

This folder contains the first executable text surface for constructing Gojo
structure. It is an authoring and inspection notation, not a parallel object
model and not yet a canonical language.

The complete working specification is
[`GojoTextSyntax.md`](../../Gojo/docs/GojoTextSyntax.md). That document explains
the structural commitments, every currently implemented constructor, the
action-scope examples, ladder digits, and the questions that remain open.

The current [syntax and dynamics checkpoint](../../Gojo/docs/GojoSyntaxDynamicCheckpoint-2026-09.md)
separates implemented structure and reads from the adaptive policies still to
be explored. It also proposes a source-calibration fixture for approximate
landmarks whose interpretation and spread are not yet known.

The [September 2026 design review](../../Gojo/docs/GojoSyntaxReview-2026-09.md)
checks these commitments against the parser, kernel, and Blocks examples. It
led to the [local blend fixture](../../Gojo/docs/GojoLocalBlendFixture.md), which
exercises partial components, mixed contributions, local mismatch, explicit
feedback, and stale evidence. It adds no syntax constructors or general
learning policy.

The [graphic structure review](../../Gojo/docs/GojoSyntaxGeometryReview-2026-09.md)
exercises parameters, directed segments, partial sections, Bézier construction
and frieze generators. `group_on(holder, ...)` supplies an explicit group holder
when shared authored frame identity matters; it is ordinary group structure.

The [affine construction follow-up](../../Gojo/docs/GojoAffineConstructionFixture.md)
adds selected exact interpolation, displacement and coordinate-map reads in
the kernel. Its syntax fixtures exercise curves, continuity claims and motif
correspondence with partial inputs and numeric restoration. No new parser
constructors or execution loop are required.

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

## Current implementation and next design priorities

The [explicit unit quotient read](../../Gojo/docs/GojoUnitQuotientReads.md)
adds an authored denominator/cancellation experiment with retained standards.
[`unit-quotients.gojo`](../../Gojo.Encoding.Tests/Fixtures/Syntax/unit-quotients.gojo)
uses the existing `group`, `ports`, `point`, `resolution` and binding forms.
Cancellation is an explicit witnessed read; neither parsing nor ordinary unit
compatibility silently reorders the expression.

The [quantity fixture](../../Gojo.Encoding.Tests/Fixtures/Syntax/quantity-quotients.gojo)
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
before mutation.

Subsequent fixtures now cover
[nested quotient composition](../../Gojo.Encoding.Tests/Fixtures/Syntax/quotient-composition.gojo),
[explicit material refinement and borrow](../../Gojo/docs/GojoMaterialRefinement.md),
and [conditional bounded-quantity arithmetic](../../Gojo/docs/GojoBoundedQuantityFixture.md).
They retain standards, conservation, shared causes, unresolved domains and
read residue. The bounds are supplied premises, not the universal meaning of
an approximate number.

[Version-1 numeric graph transport](../../Gojo/docs/GojoNumericFormat.md) is
implemented. It preserves the retained graph, including shared placeholders,
unit descriptors, attachment ownership, carrier state, witnesses and timelines.
All current authored fixtures and exported Blocks specimens pass two numeric
restorations in the inventory suite. The format captures a quiescent local
world; it does not serialize external processes or provide distributed identity.
Legacy diagnostic snapshots remain distinct and are refused for restoration.

The [curve replay fixture](../../Gojo/docs/GojoCurveReplayWalkthrough.md) also
exercises versioned premises, application dependencies, pending work and resume
after restoration. This supports controlled dynamic experiments, while the
driver still chooses actions and execution order.

The next priorities are:

1. **Contextual interpretation.** Preserve a reported landmark, open actual
   quantity, candidate calibrations and later feedback. Let interpretation
   become more useful without inventing a tolerance or rewriting the report.
2. **A selected local response law.** Connect prediction and feedback to an
   explicit parameter perturbation. Compare later predictions, retain
   alternatives and expose stalls and ambiguous explanations.
3. **Connected feedback.** Exercise a few local spaces with delayed, repeated
   and conflicting evidence, visible execution budgets and pending states.
   General convergence and credit assignment remain open.
4. **Changing participation and memory.** Test route admission, utility,
   weakening and recovery before promoting a pruning or compression policy.
   Deliberate loss must have an explicit retained-information contract.
5. **Further mathematical reads.** Develop soft expectations or distribution
   shapes, broader component/inverse laws and cyclic behavior when a concrete
   experiment needs them; existing exact mathematics remains conditional on
   its declared basis and domain.
6. **Authoring improvements.** Introduce reusable definitions, shorthand and a
   readable writer as repeated structures show what improves inspection.
   Aliases remain outside the source of meaning. Compact transport and a
   general executor are separate projects.

## Most useful feedback now

The foundational arithmetic is constrained by mathematics. Adaptive policy is
not uniquely determined by it. Useful feedback now concerns:

- How should feedback from neighboring spaces change connection participation
  and responsiveness while keeping their separate evidence visible?
- What supports introducing, weakening, or pruning a route without treating
  agreement between correlated sources as independent evidence?
- Which partial-pattern and focus forms deserve shorter notation after more
  than one experiment has exercised them?
- What should make a local space revise an interpretation, request a different
  observation, retain several candidates, or decide that finer detail is
  irrelevant to its question?
- Which historical promises require exact capture, and which can be preserved
  more usefully by deliberate lossy compression into a structure or algorithm?

The checkpoint proposes small fixtures for these questions. They do not require
freezing the language or choosing a universal learning policy first.

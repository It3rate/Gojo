# Native relation encodings

This is the shared home for the reusable relation encodings developed in the qualitative experiments. A finite rule, a passage, a repeated motif, or a curve is connected Gojo structure here. The C# helper constructs and reads that structure; its class name is not a stored instruction or concept label.

These mechanisms complement the compact encodings in the parent core. They are **not** an assertion that every native relation graph already has a three-bit or fixed-width equivalent. Use the existing `GojoNumericFormat` when a native graph needs a numeric, identity-preserving transport representation. A smaller execution layout requires an explicit mapping and conformance checks.

## What is included

| Mechanism | Files | Meaning carried by structure |
|---|---|---|
| Finite laws | `FiniteLaw`, `FiniteLawConformance` | Ordered input domains, explicit rows, admitted output support, consumed rows and unresolved premises. Supplied conformance obligations test properties; passing some obligations does not establish all possible laws. |
| Derived execution | `FiniteLawPlan`, `FiniteLawBatchPlan` | Tables compiled from a particular native law, retaining its participants and dependencies. The source law remains authoritative. |
| Observation and refinement | `QualitativePublication`, `ResolutionCorrespondence`, `QualitativeQuestion` | An observation's source, subject, occurrence, basis and possible outcomes; explicit refinement correspondence and a requested refinement surface when needed. |
| Proposed action selection | `FiniteChoice` | A selected candidate, chooser, sampling evidence and retained proposal. Selecting an action does not settle an uncertain fact about the world. |
| Material movement | `QualitativePacketTransport`, `ConditionalPassage` | Actual packet ownership, explicit routes, receiver sites, accepted/blocked offers, opening laws and causal history. |
| Event order and recurrence | `PhaseRecurrence`, `PhaseEventNetwork`, `EventScheduleConnection` | Reusable phase transitions, actual occurrence history, connections between exposed phase surfaces and permitted event windows. |
| Repetition | `RepeatedMotif` | A shared template, explicit variable bindings, fresh local occurrences, successor relations and seams between exposed entry/exit sites. |
| Comparison | `OrdinalReferenceFrame` | A declared order and resolution with lower/equal/higher comparisons and an explicit composition law. |
| Motion and transforms | `QualitativeMotionFrame`, `QualitativeActionAlgebra` | Ordered axes, opposed directions, shared reaches, cancellation/exchange laws, finite action maps and supplied composition/inversion obligations. |
| Geometry | `QualitativeCurve`, `QualitativeBand` | Connected sites and control words, shared interpolation parameters, endpoint tangents, ruled boundaries, cap/continuation conditions and occupancy relations. |
| Closed and repeated geometry | `QualitativeMarkedContour`, `QualitativePeriodicCell` | Ordered marks and return claims, explicit boundary correspondence, seams and winding reads. Closure alone does not promise equal turns; finite gluing does not prove an infinite embedding. |

The source comments give each grammar beside its builder and reader. All these files have the namespace `Gojo.Encoding.Core.Native` and depend only on this library and the Gojo kernel, never an experiment project.

## Where the ports are

A port is a selected, exposed participant in one of these connected contracts. There is no mandatory universal `Port` object or string identifier. Which participants it exposes, and which operations are legal, follow from the surrounding relations.

For example, the material transport grammar is:

```text
capacity  = port -> Closed(storage sites)
route     = source port -> target port
placement = packet -> (port -> storage site)
```

The same site can occur in more than one capacity row to express shared capacity. Copying its coordinates or appearance does not create the same site. Observation through `ReadPort` does not drain material; an accepted `Move` changes the packet's placement. An import is explicit and may not import a previously owned packet again.

The other surfaces serve different purposes:

| Surface | Connection | Contract |
|---|---|---|
| Conditional decision port | `(controller -> target) -> (basis -> command domain)` | A higher-level decision interface used with a phase and opening law. Merely having an allowed outcome among alternatives cannot authorize a move. |
| Phase port | Source connected to a phase-recurrence surface | Exposes phase evidence and causal order; does not manufacture elapsed time. |
| Motif entry/exit | Bound template slots exposed by an occurrence | A seam connects an exit to a following entry; it does not merge their identities or declare their physical distance. |
| Refinement port | An exposed participant requested by a question | Requests further evidence without silently changing the earlier observation. |

These are composable protocols, not synonyms. In particular, `QualitativePublication` is a richer observation/calibration contract, **not mandatory metadata for every low-level port**. Bare structural exchange can remain uncalibrated; attaching a domain name or a description is not necessary for a connection.

`QualitativePacketTransport` commits an accepted batch atomically. `ConditionalPassage` evaluates a specific conditional admission/capacity contract and retains per-offer results. Their differing admission behavior must remain explicit rather than being hidden behind one generic transfer method.

## Small example

```csharp
using Gojo.Core;
using Gojo.Patterns;
using Gojo.Encoding.Core.Native;

var source = new Placeholder();
var target = new Placeholder();
var route = new Relation(source, target);
var transport = new QualitativePacketTransport(
    StructuralConstraint.Closed(
        new Relation(source, StructuralConstraint.Closed(new Placeholder())),
        new Relation(target, StructuralConstraint.Closed(new Placeholder()))),
    StructuralConstraint.Closed(route));

var packet = new Placeholder();
var import = transport.Import(new Placeholder(), source,
    StructuralConstraint.Closed(packet));
var transfer = transport.Move(new Placeholder(),
    StructuralConstraint.Closed(new Relation(packet, route)));

// Inspect the receipts before assuming either operation was accepted.
// The transfer receipt connects its before/after states and accounting.
```

No name for the packet or either port is needed to perform or inspect this exchange. The placeholders' relation positions and shared references provide their roles.

## Certainty, resolution and implementation boundaries

- An open scope, a closed empty scope and a closed list of alternatives are different. A compiled mask cannot silently turn an open scope into empty support.
- Compiled masks address one captured law's participant order. They are not portable magnitude codes or a global dictionary. A source relation/tick change invalidates the plan until explicitly recaptured.
- `FiniteLawPlan` supports at most eight participants per domain; `FiniteLawBatchPlan` uses 64-bit masks. These are limits of those execution backends, **not limits on native concepts or relations**. Unsupported laws remain native instead of being truncated.
- The curve helper's integer convenience builder materializes repeated shared reaches and currently guards against words above 65,536 moves. That guard is an allocation bound, not a Gojo law. Numeric vector reads count those declared repetitions; the actual control words and interpolation relations remain available.
- Equality and zero in these symbolic motion/comparison reads are relative to the declared basis and resolution. They do not prove physically exact zero at unlimited resolution. Empty support, no selected participant, zero net displacement and no material must not be conflated.
- Curves use explicitly declared interpolation structure. They do not infer a unique curve from endpoints alone. A return or shared endpoint need not imply smoothness, a circle, or a particular construction history.
- Time is ordered occurrences here. A phase cycle does not automatically establish duration, equal beats, an autonomous clock or reversible history.
- Native publication, packet and phase receipts preserve evidence/history. This does not make that rich receipt grammar a required physical header on every primitive element.

## Provenance and promotion boundary

The 21 implementation files were copied from `experiments/QualitativeExecution` on 2026-11-10 with namespace changes. Existing experiments keep their original implementations. Future changes should use this shared core when the contract fits, and prototype new mechanisms locally before promoting them with tests and documentation.

The native test directory copies the corresponding focused tests, adapting namespace and dependency references. The repeated-motif tests retain a **test-only** landmark fixture to exercise local identity, stable bindings, order and seams; that fixture is not production vocabulary. Additional tests exercise name-free numeric graph round trips, stale compiled dependencies and the distinction between open and empty support.

Intentionally still experimental:

- Ornament/glyph corpora, classifiers, quality judgments, learned preferences and search policy.
- Industrial threshold/stock/reflex examples and measured supplier policies.
- Ornament interior-lattice attachment ports and crossing selectors. Their current readers depend on ornament-specific substrate and should be extracted only with their full correspondence checks.
- Detailed drawing receivers, domain-specific geometry analyses and candidate scoring systems.

Promotion establishes a reusable implementation and test boundary. It does not turn every experiment's modeling assumption into a universal primitive or claim a complete compiler between native graphs and compact ODV structures.

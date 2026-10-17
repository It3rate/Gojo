# Primitive sufficiency experiments

Status: exploratory assessment of existing implemented behavior, 2026-10-15.
The fixture adds tests only; no production code, primitive, reader, dispatcher,
or foundational rule was changed.

Follow-up: the [structural constraint experiments](GojoStructuralConstraintExperiments.md)
and [qualitative process experiments](GojoQualitativeProcessExperiments.md) now add
and exercise an explicit local interpretation law above the unchanged kernel.
The [combined assessment](GojoPreNumericAssessment.md) distinguishes those new
capabilities from this initial representation-only baseline.

The question is whether existing Gojo structure can express a moving numeric
threshold, retain uncertain support, let the comparison participate in another
relation, and express a nonnumeric three-state domain. A passing assertion about
an unsupported operation records a boundary; it does not make that operation
sufficient.

## Results

| Experiment | Already possible | Strained or missing |
| --- | --- | --- |
| Exact temperature against a moving threshold | Existing operand ports and an attached live difference output settle a signed gap as either value or unpaid draw. | A signed gap is evidence for a comparison, not an autonomous Boolean predicate. |
| Interval against a moving threshold | A retained qualified difference/separation recomputes the entire support on explicit reread. Overlap and equality remain visible. | Authored support is a premise. These tests supply neither a measurement model nor discovery of that premise. |
| Comparison relation inside a conditional | The actual difference relation is the role of a condition and binding; the response is also a relation. No surrogate Boolean carrier is needed. | The existing consumer requires a separately published numeric support contract. It does not execute the pending relation or refresh old publications. |
| Nonnumeric three-state domain | Distinct relational topologies, directed edges, open roles and competing proposals need no scalar state codes. | Binding and transition substitution do not validate domain membership or allowed edges. The state leaves remain open placeholders. |

## Exact threshold and settlement

Write the structure before interpreting its result. With `A` and `T` distinct
carriers sharing a temperature unit promise, the existing difference shorthand
expands to ordered operand ports:

```text
negative port = S(T,0,1) -> S(Unknown,0,-1)
positive port = S(A,0,1) -> S(Unknown,0,1)
difference   = (negative port -> positive port) -> P(Gap,1)
live output  = (T.ValueSlot -> A.ValueSlot) -> difference
```

The live output is explicitly attached. With `A = 24`, changing `T` by signed
tension produces:

| T | Gap.Value | Gap.Tension | Selected read |
| --- | --- | --- | --- |
| 20 | 4 | 0 | A exceeds T by four ticks |
| 26 | 0 | -2 | A is two ticks below T; the requested negative output remains unpaid draw |
| 22 | 2 | 0 | A exceeds T by two ticks |
| 24 | 0 | 0 | Equality |

The same attachment responds without reconstructing the equation or calling a
new threshold reader. The source remains at 24. This is an existing non-draining
live publication, not conserved temperature material transferred into Gap.
Negative tension here belongs to the explicitly driven output; merely observing
an ambiguous interval below does not introduce tension.

## Interval support and locality

Native reads expose value, support and basis. Both are explicitly reframed into
the same zero-origin basis `B`; shared unit shape alone does not align two
independent carrier identities.

```text
A read -> (S(A,0,1) -> S(B,0,1))
T read -> (S(T,0,1) -> S(B,0,1))
difference = qualified subtract(aligned A read, aligned T read)
separation = qualified separation(aligned A read, aligned T read)
```

These helpers already construct connected operands, equation ports, result,
support, residue and provenance. No fixture comparator calculates an answer.
For `A` supported by `[18,22]`, the same retained relation reads:

| T | Difference support | What the support establishes |
| --- | --- | --- |
| 20 | [-2,2] | Both sides remain possible |
| 23 | [-5,-1] | Every A alternative is below T |
| 17 | [1,5] | Every A alternative exceeds T |

The tests also cover exact equality, wholly positive/negative intervals and an
interval whose upper gap is zero. Bounds and previous result coordinates survive
later threshold changes. This is retention of sampled results/support, not an
archive of every live source reference.

Controls establish that an incompatible basis does not yield a comparison and
that a known nominal gap with missing support cannot establish interval
separation. Reads leave source value/tension and the observation basis carrier
unchanged. Support is deliberately authored as absolute endpoints; the tests
move the threshold, not a nominal source beyond its supplied support.

## A relation participates in a conditional

Let `R` be the qualified difference relation itself. Existing conditional
surfaces can contain it directly:

```text
condition = G(R, S(B,1,MaxTick))
rule      = G(G(condition), response relation)
policy    = G(rule)
binding   = G(R, publication of R)
selection = existing support-decision read(policy, binding)
```

Here `G` is an ordinary ordered group. With zero origin and whole-tick resolution,
the positive frame expresses strict positivity. The tests distinguish complete
admission of `[2,6]`, unresolved support `[0,4]`, and rejection of `[-4,0]`.
Equality is not strict exceedance. Rejected and unresolved condition witnesses
are asserted separately; neither is replaced with a default false value.

The qualification is significant: a pending `R` is not itself an acceptable
publication for `SupportDecisionSurface`. A test-local adapter explicitly reads
`R` and packages its calculated value/support with the consumer's finite scope
`[-100,100]`, quantum 1. It retains `R`, its earlier scope and calculation
provenance. All fixture supports fit that scope. This adapter is not a general
clipping/import law and contains no threshold arithmetic or Boolean selection.

Selection then consumes this advertised support. It does not follow the
supplier to recover precision. A retained publication continues to describe the
earlier observation after T changes; an explicit new publication and selection
are required for the new answer. No scheduler or conditional settlement law was
added to conceal this seam.

This is evidence that relations are already first-class structural participants
and computable surfaces. It is partial evidence for higher-order computation:
the consumer computes over R's published numeric read, rather than evaluating an
arbitrary relation as a condition.

## Nonnumeric states and constraints

The second fixture has no carriers or locations:

```text
first  = a -> a
second = a -> b
third  = b -> a
domain = (first -> second) -> third
edges  = (first -> second) -> (second -> third)
query  = currentSlot -> nextSlot
world  = (domain -> edges) -> query
```

`a` and `b` are identity-bearing placeholders, not numeric codes or grounded
symbolic constants. Shared occurrence and order distinguish the three shapes;
reversing the second produces the third. The fixture retains the domain and edge
references while refining the current role, leaving the next role open.

Two proposals can remain connected to a common query and to explicit outgoing
edges. They are retained as separate relation branches. Passing both to a single
binding frame instead chooses its first binding; reversing their order reverses
that choice. This helper convention supplies no evidence that one candidate is
better.

The useful failure is executable: `b -> b`, a fourth shape outside the authored
domain, can be substituted into the query without a membership rejection. An
explicit write of third from first also succeeds even though the authored edge
surface offers only first to second. `Advance` applies slot expressions; it does
not follow these state edges. Completion of a supplied role also does not ground
the placeholders inside the supplied state.

Thus the topology can carry distinctions and proposed constraints, and local
inspection can recover them. These existing binding APIs do not supply a
nonnumeric domain-admission or directed-transition enforcement law. The fixture
does not fill that gap with a switch, host-side matcher, state enum, or invented
numeric scale.

## What this justifies next

The experiments locate a useful boundary without establishing a need for a new
primitive. Numeric difference, support and relation participation already work.
The smallest unresolved questions are how an exposed relation offers its read
to a conditional consumer, and how a connected nonnumeric domain/edge surface
produces accepted, blocked or open transition evidence. Those are exploratory
local laws to specify before extending the implementation. Neither arbitrary
relations nor binding completion should silently acquire those meanings.

## Reproduction

The fixture lives in
[`ThresholdSufficiencyTests.cs`](../../Tests/Gojo.Tests/Experiments/ThresholdSufficiencyTests.cs)
and
[`NonNumericSufficiencyTests.cs`](../../Tests/Gojo.Tests/Experiments/NonNumericSufficiencyTests.cs).
Run the focused experiments, then the repository verification entry point:

```powershell
dotnet test Tests/Gojo.Tests/Gojo.Tests.csproj --filter 'FullyQualifiedName~SufficiencyTests'
dotnet test Gojo.slnx
```

Verification on 2026-10-15: all 17 new cases passed. The final solution-wide run,
including those cases, passed 2,387 tests across 11 test projects, with no failures
or skipped tests.

The tests characterize both successful reads and missing enforcement. They are
not a claim that every experiment is sufficient because the suite passes.

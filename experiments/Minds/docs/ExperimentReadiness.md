# Preparing Minds for trustworthy experiments

Status: preparation review, September 13; priorities revised September 15, 2026.
Inspected repository revision: `13596f2d4f353c952c90c7d9e7522bf6fab272a4`.

## Recommendation

The [experiment roadmap](ExperimentRoadmap.md) is now the current suggested
sequence, interspersing each experiment with its required development work.
The numbered E0–E4 proposals and audit gates below remain background design
notes; they are not additional prerequisites on that roadmap.

The [feedback-limits experiment](FeedbackLimitsFindings.md) now adds the
independent exact comparison, timing/resolution/reference controls and retained
repetitions. These are selected exploratory checks; broader learning and replay
claims remain open.

The [prediction experiment](PredictionFindings.md) now exercises prospective
issuance and later assessment on twelve fixed sequences. It preserves losses
against hold-last, missing outcomes and an independent exact comparison. The
selected family and sample window remain authored; no learned selection was
added to improve the outcomes.

Small experiments are already possible. Gojo has exact selected arithmetic,
explicit read remainders, local feedback, prospective forecasts, and restorable
numeric graphs. The first task is to make runs of those mechanisms accountable,
not to finish a general learning system.

Start with the existing two-space response policies and make what happened
visible. The [first exploration](TwoSpaceExploration.md) now runs those policies,
retains event evidence, and includes Gojo observers of the individual views,
their average, disagreement and movement. Prospective prediction can follow.

The user's September 15 priorities supersede the prerequisite language in the
original review below. Its broader audit gates remain future options:

- Decimal calculations and conversions are legitimate at declared interfaces
  to sensors, external tools and the world. A compiler guard is not urgent.
- Settlement can mean that a goal is met at a given scale while finer activity
  continues. Observe changes and remaining work, not just whether execution
  stopped. Activity reduction and crystallization into an equation are intended
  directions, not behaviors this first experiment implements.
- Exact replay is useful later. Different results across runs are information
  to examine. Retain enough observations and selected conditions to investigate
  them without requiring a perfectly reconstructible world now.
- Keep authored policies, participant responses, external inputs and observer
  reads distinguishable. This information can itself inform later settlement.
- Broader observations should also be expressible inside Gojo. A constant
  average and moving parts can both be valid readings. External checks help
  validate those observers; they need not own the system's understanding.
- Describe rules and amendments now; use full frozen protocols when appropriate
  for confirmatory experiments. Surprising valid behavior invites investigation.

Here, **neutral** means that laws, information access, policies, and assessment
rules are explicit and applied consistently, including when results disappoint.
It does not mean a system without assumptions. Locality, selected resolution,
available operations, and a finite resource budget all affect what it can do.

Three questions must remain separate:

1. **Did the mathematics and software follow their contracts?**
2. **Was the experiment valid under its declared conditions?**
3. **What did the experiment show about the hypothesis?**

A failed numerical invariant invalidates a run. A correctly executed run that
does not learn, converge, or outperform a baseline is a useful scientific result.

## What we are taking from the paper

[Levin's *Ingressing Minds*](https://www.mdpi.com/2409-9287/11/5/161)
proposes studying mathematical and behavioral patterns across embodiments,
including unfamiliar systems and different observational scales. Section 6.3
and Figure 14 discuss sorting behavior, temporary losses in sortedness, and
clustering of cells using different sorting rules. Section 7.3 proposes a
research program and judges its value by the investigations and capabilities
it enables. The paper reports no new dataset.

For this lab, those are prompts for controlled comparisons, not additional
Gojo axioms. Our proposed questions are:

| Question for Minds | What could count as evidence | What would remain unestablished |
| --- | --- | --- |
| Can connected parts handle perturbations that defeat an isolated part? | Repeatable changes under matched inputs, access, and budgets; connection-removal controls. | General intelligence or universal superiority of local systems. |
| Does another view of the same structure reveal a useful regularity? | A read chosen before evaluation improves prediction or intervention on unused cases. | A newly drawn interpretation alone is not a new computational capability. |
| Can a useful organization transfer? | The same frozen rule works through declared ports in a changed context with measured adaptation cost. | Sharing an output number alone does not establish the same internal organization. |
| Does a higher-scale model help us act? | Its proposed interventions work on held-out cases better than specified alternatives. | Merely naming a collection a mind does not establish causal power. |
| Can a research heuristic help discover useful structures? | Compared search procedures, equal budgets, all attempts counted, and unused evaluation cases. | An attractive example selected afterward does not establish discovery efficiency. |

These are operationalizations proposed here. The experiments below do not
distinguish nonphysical causation from an ordinary account of the same executed
rules. Such a claim would need competing models that predict different
observations under a specified intervention. Gojo can help expose the rules
and evidence; naming the project Minds adds no such prediction.

## Current capability and its limits

| Area | Current evidence | Limit relevant to experiments |
| --- | --- | --- |
| Exact calculations | [ExactScalar](../../../Gojo/Core/ExactScalar.cs), [projection conformance](../../../Tests/Gojo.Tests/Core/ProjectionConformanceTests.cs), [range tests](../../../Tests/Gojo.Tests/Core/NumericRangeMilestoneTests.cs). Exact rational intermediates, checked stored results, explicit source-basis residue. | Selected reads, not an arbitrary exact evaluator. Stored coordinates and tension have finite ranges. |
| Incomplete quantities | [BoundedQuantityTests](../../../Tests/Gojo.Tests/Core/BoundedQuantityTests.cs): shared-source cancellation, separate events, outward bounds, contradictory premises and zero-denominator refusal. | Conditional support bounds are not probabilities or learned confidence. |
| Local response and exchange | [Response](../../../Gojo/Structures/LocalCalibration.Response.cs), [exchange](../../../Gojo/Structures/LocalCalibration.Exchange.cs), [two-space tests](../../../Tests/Gojo.Encoding.Tests/GojoSyntaxTwoSpaceTests.cs). | Selected response strength, one free actuator per response, caller-owned histories and explicit event order. No discovered general adaptation law. |
| Prospective prediction | [Forecast implementation](../../../Gojo/Structures/LocalCalibration.Forecast.cs), [forecast tests](../../../Tests/Gojo.Encoding.Tests/GojoSyntaxForecastTests.cs). Predictions precede outcomes; failures, stale premises and later assessments survive. | The host selects the affine family, sample window and readout. Family discovery is not implemented by this fixture. |
| Restoration | [GojoNumericFormat](../../../Gojo/Core/Serialization/GojoNumericFormat.cs), [transport tests](../../../Tests/Gojo.Tests/Core/NumericFormatTests.cs). Roots, aliases, attachments and retained history survive in independently identified worlds. | Restoring a graph is not restoring the host's event queue, random generator, external environment or omitted roots. |
| Time and settlement | [Carrier](../../../Gojo/Core/Carrier.cs), [TimeStepSequence](../../../Gojo/Structures/TimeStepSequence.cs). Immediate local settlement and structured before/transition/after evidence. | A timeline does not execute a schedule. The kernel loop has no public termination report or general same-step batching. |
| Geometry and demos | Gear/CAD structures illustrate composition and refinement. | Some construction and settlement paths use floating-point calculations. A Gojo-shaped output does not certify its derivation. |

The [signal learning plan](../../../Gojo/docs/GojoSignalPatternLearningPlan-2026-09.md)
already points toward competing continuations, ambiguity, composition, driven
systems, and later response/utility. Minds should exercise that direction,
with stricter experimental records, rather than introduce a competing theory.

## Six areas to develop (broader audit, not prerequisites for exploration)

### 1. A declared and checked numeric boundary

**Finding.** A source search of `Gojo/**/*.cs` found no executable uses of the
searched floating/decimal types or transcendental calls. This is a preliminary
source audit, not a compiled dependency or data-flow proof.

The same is not true across the solution:

- [GearShapeGeometry](../../../Visualizations/Gojo.GearShapeLadder/GearShapeGeometry.cs) uses
  `double`, `Math.PI`, trigonometry and roots, then rounds coordinates in
  `FrameBasis.Point` before constructing Gojo points.
- [GraphicAreaSettlementSurface](../../../Applications/GojoCAD/structures/GraphicAreaSettlementSurface.cs)
  uses floating-point geometry and fixed numerical thresholds to select and
  construct area results. These calculations are not just pixels.
- [GraphicOrthogonalFillSurface](../../../Applications/GojoCAD/structures/GraphicOrthogonalFillSurface.cs)
  also has floating-point geometry and rounded construction paths.

These are relevant limitations of those paths, not evidence that the core
feedback/forecast fixtures require floating point. Start Minds with a direct
dependency on Gojo only. Do not use the gear demo as evidence for an entirely
Gojo-derived geometry calculation until its actual path has been addressed.

**Later audit work:**

1. List the participant's builders, readers, mutators, dependencies and input
   adapters. Record every place a host calculation can affect a decision.
2. Add a compiler-aware guard for this declared closure: primitive and aliased
   floating types, `decimal`, `Half`, inferred floating literals, conversions,
   generic math and calls into unreviewed numeric helpers. Include generated
   code and transitive project/package changes. Reflection/native/dynamic paths
   need explicit review; an unknown path cannot receive a clean certificate.
   Define a small trusted boundary for exact runtime operations such as integer
   arithmetic and the used `BigInteger` operations. Do not reject an entire
   framework assembly merely because unrelated APIs in it support floating point.
3. Test the guard with deliberately contaminated examples, including a helper
   that returns an integer after internally rounding a floating result.
4. For each scientifically relevant calculation, retain the Gojo inputs, chosen
   basis, operation/read, result and remainder. For each decision, identify its
   structural rule or label it as a selected host policy.

A token search is a useful alarm, not the final guard. A type guard also cannot
detect a hidden optimizer written entirely with integers: the structural
derivation review is a separate requirement.

**Allowed exact implementation work.** Gojo uses ordinary host integer
instructions, `Int128` and `BigInteger` to implement justified structural reads.
`ExactScalar` is an internal exact rational calculation aid; it encodes results
back into grounded relations. Reimplementing these integer instructions in a
second arithmetic engine would not improve experimental neutrality. The
important distinction is whether the connected premises justify the result,
not whether C# ever performed arithmetic.

Decimal notation is also not the issue: an exact input `1/10` may be legitimate
structural participation. It is different from silently computing an approximate
result in a host numeric type and inserting only the answer into Gojo.

**Gate.** An intentionally contaminated path is rejected; unsupported exact
operations stay unresolved; changing a plot or reference evaluator cannot
change participant actions. External measurements may enter through a declared
adapter with stated resolution and uncertainty. They must not be relabeled as
internally derived Gojo answers.

### 2. Explicit update order, settlement and observation limits

**Finding.** `Carrier.Add`, `Remove`, `Set`, and `Attach` settle immediately.
`SettleWorld` runs at most 256 passes, compares carrier value/tension snapshots,
and returns without reporting whether it reached the cap. Carrier identity
order and relation traversal affect the execution order. Some route allocation
also orders candidates by relation identity in
[Carrier.RouteSettlement](../../../Gojo/Core/Carrier.RouteSettlement.cs).

Consequences:

- A returned call does not prove convergence, zero tension, or goal completion.
- Equal snapshots at pass boundaries do not prove that nothing moved within
  the pass. They also do not compare every structural or host state component.
- Several sequential perturbations are not a simultaneous update.
- Reordering construction to obtain favorable identity order is a policy
  change, even if the final graph drawing looks the same.

**Build in two steps.** For the first response experiment, use the existing
free-actuator contract and an explicit event sequence: publish A, publish B,
deliver the captured observations, then apply each response. Record the chosen
order and retain its difference from sequential publication or delayed delivery.
That experiment does not require a new global scheduler.

Before studying recurrent attached worlds, add a small general settlement
report with pass count, changed-state evidence and distinct quiet/capped/error
outcomes. Define what state its stopping check covers. Preserve unresolved
tension; do not call all remaining tension failure. A later one-step/batched
interface must distinguish receiving changes, local absorption, route settlement
and time. Adding an observation/report must not secretly change those laws.

**Gate.** Demonstrate a quiet result with unresolved work, a budget stop, a
declared oscillating example, and order-sensitive behavior. Repeat a declared
event sequence exactly. For cycle detection, include all state affecting future
transitions, including messages and controllers; repeating one scalar is not
proof of a cycle. If complete cycle evidence is unavailable, report a suspected
repeat or budget limit, not an established attractor.

### 3. Complete run records and replay (deferred)

**Finding.** The numeric graph format is a strong starting point, but the lab
does not yet have a complete run bundle or general replay runner.

**For a future exact-replay mode:** a small versioned host record containing protocol/code hashes,
runtime and dependency versions, exact initial roots and graph bytes, selected
numeric basis/resolution, input tape, event order, interventions, budgets, and
all recorded outcomes. If there is randomness, retain its generator/version,
seed/state and realized draws or event tape; a seed alone is not portable replay.
Record setup actions too: attaching a relation may already cause settlement.

Store graph snapshots with `GojoNumericFormat`, not a new serialization of
participant concepts. JSON or similar metadata is appropriate for run hashes
and filenames; the graph remains the participant's source of meaning. Store
immutable bytes at capture points, not live references presented as past state.

**Gate.** Restore into a fresh world/process, continue from a checkpoint with
messages in flight, and obtain the same declared observations and actions.
Use an explicit identity correspondence for graph comparison. Byte equality
of separately restored graphs is not required when process identities change.
Changing original live objects must not rewrite stored evidence. Audit roots
for detached recipes, open slots and pending messages; omitted state must not
be reconstructed from a remembered expected answer.

Keep exact experiment archives separate from any later deliberately lossy
memory model. Successful replay tests only what the archive actually retained.

### 4. Separation of participant, environment, policy and evaluator

**Finding.** The present fixtures explicitly select reader calls, response
strengths, candidate families and event order in host code. That is useful
experimental control. It becomes misleading only when described as something
the participant learned or discovered.

**Build:** an information-access table for each experiment. Identify what each
participant can observe, which actions it can take, which private internals are
unavailable, and which decisions are authored. The runner can schedule declared
actions but must not search for the best next action on the participant's behalf.

For example, a fixed half response is an intervention parameter, not learned
confidence. A supplied affine family is a hypothesis class, not discovered
mathematics. An environment may contain an unreachable target. An evaluator
may know that target without giving it to the participant. If reward is later
fed back, make its source, timing and response route explicit.

**Gate.** Refuse future observations, stale plans, duplicate consumption and
lookalike identities; test that a participant cannot consult the reference
answer through a callback or shared mutable object. Reordering diagnostic
observers must not create new actions. Rename incidental labels without changing
behavior. Separately test changes to meaningful relation order; that order is
not an incidental label.

### 5. Gojo observers, independent checks and fair comparisons

The current priority is a hierarchy of local and broader Gojo observations.
The two-space observer now calculates average, disagreement, changes and
movement using existing bounded-quantity reads and calibrated composition.
None of those readings is a universal verdict. A coarse stable result can
coexist with finer movement or disagreement. Independent checks validate these
reads without requiring all future assessment to live outside Gojo.

**Finding.** A conformance assertion and a scientific measurement serve
different purposes. Existing unit tests establish many local contracts; they
do not decide which experimental score fairly measures learning or agency.

**Build:** read-only measures with definitions fixed before evaluation. Retain
raw exact counts, signed mismatches, nonnegative losses, missing outcomes and
measurement denominators. Do not let positive and negative errors cancel into
an apparently perfect predictor. A system that answers fewer cases must show
that coverage difference. Open, incompatible and unavailable reads are not zeros.

Use small exhaustive cases or independently derived exact arithmetic to check
measurements. An evaluator that invokes the same production reader is useful
for consistency, but is not an independent correctness oracle. Statistical
summaries and floating-point plots can be produced afterward in a named
evaluation boundary, with retained exact inputs and no feedback into the run.

Compare at least:

- The declared Gojo mechanism.
- A conventional exact integer/rational implementation with the same rules,
  observations, retained state and schedule. This checks behavior without a
  floating-point confound.
- A conventional implementation with the information bookkeeping Gojo makes
  explicit, when the claim concerns representation or composability.
- A controlled removal of one Gojo facility when that tests the hypothesis.
  Hiding a remainder from the evaluator and physically discarding incoming
  change are different interventions and must not be conflated.

Where useful, add a floating-point implementation as another declared condition.
It tests the effect of that approximation, not all of ordinary mathematics.
Conventional mathematics can represent exact fractions, graphs, uncertainty
and history too. Gojo's possible advantages include inspectability, reliable
composition, explicit unresolved states and cost; none requires predicting a
different answer to the same fully specified exact calculation.

**Gate.** Controls share initial problems, admissible actions and information.
Record local work, messages, memory and wall time separately; counting only
swaps or successful changes may hide expensive searches or failed attempts.
Count experimental runs as independent units, not every correlated step of
one trajectory. Predeclare how multiple measures and parameter comparisons
will be reported; retain unfavorable cases and resource costs.

### 6. A rule for changing the rules

**Finding.** Preparing an experiment will expose missing mechanics. The risk
is not adding a useful mechanic; it is repeatedly adding just the mechanic
needed to rescue the current example and then calling the outcome emergent.

**Build:** use the [protocol template](ExperimentProtocol.template.md) with a
version identifier. Record the question, selected laws, parameters, candidate
space, metrics, controls, budgets, exclusions and both favorable and unfavorable
outcomes before confirmatory evaluation. Record exploratory runs as exploratory.

If a missing law appears:

1. Save the failed or unresolved case.
2. Classify it: software defect, absent operation, poor chosen policy, invalid
   premise, or legitimate negative result.
3. For a proposed kernel addition, write its Gojo shape and independent law.
   Include cases unrelated to the desired experimental outcome, especially
   refusal, conservation and range boundaries.
4. Version the code and protocol. Results across versions belong to different
   conditions; do not silently combine them.
5. Evaluate the revised claim on unused cases. Do not keep refining against
   the same supposedly held-out examples.

**Gate.** The experiment can finish as valid with an unfavorable result. There
is a complete ledger of attempts, including stalls, invalid runs and amendments.
Unexpected patterns can motivate a new hypothesis, but their discovery trace
does not also serve as its independent confirmation.

## Proposed experiments and their prerequisites

### E0. Establish that the lab measures honestly

This is apparatus validation, not a test of cognition. Reuse the existing
projection, bounded-quantity and restoration fixtures as specifications.
Exercise observation without mutation, source-basis reconstruction, same-source
versus distinct-source uncertainty, refused fractional import, overflow and
checkpoint continuation. Include deliberately corrupted histories and a
deliberately contaminated numeric helper.

For the future strict apparatus audit: the numeric guard, capture/replay boundary
and independent checks. These are not gates on the first exploratory run.
Do not copy the entire kernel test suite into Minds. Add only tests of the new
lab boundary and failures that the original tests cannot catch there.

### E1. Agreement, disagreement and oscillation in two spaces

**Start here with focused checks.** Reuse the current local response/exchange contracts.
Freeze policies with response fractions `0`, `1/4`, `1/2`, and `1`; implement
zero as a declared no-response control if the existing response reader refuses
zero strength. Do not change that reader merely to admit the control.

Use a small predefined set of starting discrepancies, exact basis conversions,
consistent/conflicting local references, missing messages and delivery orders.
The current selected law already predicts agreement in the captured half/half
case and swapping in the full/full case. Reproducing those outcomes validates
the apparatus, not discovery of intelligence. Quantization may preserve a
nonzero disagreement; delayed delivery can change trajectories.

Measure disagreement, mismatch to independently supplied reference observations,
unapplied change, messages and attempts. Peer agreement and agreement with the
reference are separate measures, and reference observations are still premises.

For initial exploration: retain observed events and explain the selected local
attempt limits. Exact replay and a numerical dependency guard can follow later.
A general network scheduler, automatic trust and autonomous rewiring can wait.

### E2. Forecasts that can be wrong

Use the current prospective forecast path with a frozen affine family and
readout. Compare against hold-last on finite exact sequences: constant, ramp,
reversal, missing observations and unequal sample intervals. A hold-last
participant needs its own small justified structural construction if the
existing temporal API cannot express it; do not sneak it in through a scalar
fallback. A conventional hold-last reference can be evaluated externally now.

Freeze the selection of past samples. Issue every forecast before its outcome
is available. Measure exact absolute error, coverage and cost over the same
targets; retain signed mismatch separately. Later extend to competing families
along the existing signal plan. A model that fits past observations can still
fail prospectively, and that is an intended result.

Required: E0, complete issue/outcome records and a fixed loss. Automatic family
selection, confidence learning and reward attribution are not prerequisites.

### E3. Same behavior, different observations

Replay the same frozen trajectory through several predeclared read surfaces.
Ask which view predicts a future event or helps choose a useful intervention
on unused trajectories. Start with exact scalar/time reads and no geometric
solver. Count complexity of the observer as part of the proposed explanation.

Required: immutable snapshots, separate observational and intervention phases,
and at least one task on which the proposed read could fail. Additional views
must not mutate the participant. A different description of a trace is not by
itself evidence of a new skill or of a privileged perspective.

### E4. Sorting and mixed local rules

The [underlying sorting study](https://arxiv.org/abs/2401.05375) distinguishes
cells that cannot initiate moves from cells that cannot be moved at all,
uses threaded local implementations, and measures sortedness and clustering.
Consult its [authors' implementation](https://github.com/Zhangtaining/cell_research)
and pin a revision before attempting replication. A changed scheduler or
defect law is a variant, not automatically a reproduction.

Gojo can already represent sites, relations, comparisons and histories. A tested
local movement contract is still needed: occupied sites, legal neighbors,
ordering, movement of cell identity, swap atomicity, defect behavior, tie rules
and conflict handling. These must remain visible without a host-side array
sorter that writes its answers back into Gojo.

Use a conventional exact implementation first as a reference. Small exhaustive
arrays can expose unreachable target states under the declared move set.
Preserve all attempted and completed swaps and the complete trajectory. Record
the paper's measures alongside total inversions and adjacent descents, with
duplicates and boundary conventions fixed in advance. Control clustering with
identical rules carrying randomly assigned inert labels, preserving label
counts; rule labels must not be input to movement decisions.

An independent arithmetic example shows why the interpretation needs care:

```text
[2, 4, 1, 3] -> [2, 1, 4, 3]
adjacent descents: 1 -> 2
total inversions:  3 -> 2
```

Swapping one inverted adjacent pair worsens one measure while improving the
other. A dip followed by recovery therefore does not establish foresight or
planning. Test the operational claim about the trajectory first; stronger
claims need additional memory, goal or intervention controls.

Required: E0 plus an explicit movement/scheduling law and independently checked
measures. Do not require a positive sorting result before freezing those laws.

### Later: transferred skills, repair and evolving connections

These remain worthwhile, but should not be included in the first readiness gate.
They require explicit proposal/selection rules, legal structural edits, local
resource costs, action/outcome correspondence and credit over time. Memory
compression requires a declared retained promise and measurable loss.

For transferred skills, freeze the reusable operation and permitted interface
adaptations before trying a new context. Compare transfer with reconstruction
under equal budgets. For repair, distinguish restoring a local condition from
achieving a global one: degree two at every site does not imply a single ring.
For heuristic comparisons, give search policies the same construction
vocabulary and evaluation access, count all candidates tried, and use a held-out
set of tasks. A heuristic is an experimental condition, not a kernel law.

Geometry-heavy and continuous-dynamics experiments have additional requirements:
grounded constructions, supported operations, explicit approximation bounds
and resolution studies. An unsupported root or transcendental expression must
remain unresolved or gain a separately justified refinement mechanism. Finite
ticks alone do not supply exact real analysis.

## Current implementation order

1. Run the existing two-space fixture and retain observations (implemented).
2. Observe at both local and pair scales through Gojo structure (implemented).
3. Show messages, changes, plan residue and the observation window (implemented).
4. Explore unexpected behavior and additional schedules or goals.
5. Add stronger auditing, replay and formal protocols when a question needs them.

The earlier audit work breakdown remains useful as a backlog, not a dependency
chain that must finish before experiments:

| Work item | Where it belongs | Ready when |
| --- | --- | --- |
| M1: numeric closure and contamination checks | Minds preparation/validation; future test project | A hidden rounded helper is rejected; every decision-producing path has a reviewed contract. |
| M2: run bundle and replay | Minds host infrastructure over GojoNumericFormat | A fresh process can continue an in-flight checkpoint without consulting expected outputs. |
| M3: event and termination evidence | Minds event driver; a Gojo change only for missing general settlement evidence | Scheduled observations/actions and cap/quiet/invalid outcomes remain distinguishable. |
| M4: measures and controls | Separate Minds evaluation boundary | Independent small-case checks pass; missing and unfavorable outcomes remain in reports. |
| M5: first frozen E1 protocol | Minds experiment definition | E0 passes and the protocol fixes policies, cases, schedules, budgets and claims before evaluation. |
| M6: later experiment-specific prerequisites | Gojo mechanisms plus Minds protocols, separately versioned | E2/E3/E4 gates above are met without adding outcome-specific exceptions. |

`Minds.Tests` now checks the new observer and driver boundaries, including
observation without mutation, retained past readings and fractional requests.
Keep experiment reports separate from test pass/fail: conformance tests assert
contracts; scientific runs record what happened. A narrow authored event tape
is enough initially. A universal scheduler, confidence algebra, distribution
library, advanced geometry, automatic rewiring and a definition of consciousness
are not blockers for E1 or the existing affine forecast experiment.

## Review evidence and reproducibility

The baseline solution run on September 13, 2026, using .NET SDK `10.0.401`,
passed **1,653 tests**: 782 Gojo, 695 encoding, 155 CAD, and 21 gear-ladder;
zero failed or skipped. These are dated inspection results, not a permanent
inventory or evidence for an unimplemented Minds runner.

Commands used or useful when updating this review:

```powershell
dotnet test Gojo.slnx --verbosity minimal
rg -n '\b(double|float|decimal|MathF)\b|Math\.(Sqrt|Pow|Sin|Cos|Exp|Log|Round)' Gojo --glob '*.cs'
rg -n '\b(double|float|decimal|MathF)\b|Math\.(Sqrt|Pow|Sin|Cos|Exp|Log|Round)' GojoCAD Gojo.GearShapeLadder --glob '*.cs'
```

The search is deliberately documented as a preliminary audit: comments may
match, and aliases, inferred types and indirect calls need stronger analysis.
The current code/tests take precedence over earlier dated status counts.

Paper review used the published 53-page
[publisher PDF](https://mdpi-res.com/d_attachment/philosophies/philosophies-11-00161/article_deploy/philosophies-11-00161.pdf),
especially sections 6.3 and 7.3, and visually revisited Figure 14 on printed page
30. Its inspected bytes have SHA-256
`D347F3B9043C49D07DB72D1C9F2F265C8D380C23292524FC82DE48158FA017A3`.
The publisher web page was rate-limited during this pass; the locally retained
publisher PDF from the earlier review was used. The sorting article is a
separate primary source, not new data supplied by *Ingressing Minds*.

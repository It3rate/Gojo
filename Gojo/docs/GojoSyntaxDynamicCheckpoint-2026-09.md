# Syntax checkpoint: from constructed examples to local adaptation

September 6, 2026. Implementation reviewed at `3f766e5`; this document records
the checkpoint and proposes experiments. It adds no syntax, execution policy
or foundational law. Current code and tests govern implemented behavior;
the [canonical prompt](GojoSystemPrompt.md) governs the model.

**Assessment:** the syntax and selected kernel mechanisms are ready for small
dynamic experiments. We have enough structure to express the participants,
unknowns, requests, changes and evidence. Choosing useful changes, learning
interpretations and managing interacting feedback loops remain experimental.
The language is not ready to freeze, and these fixtures do not demonstrate a
general autonomous problem-solving system.

## Approximately ten is a landmark with an open interpretation

In the user's example, a source reports “about ten.” Ten is the reported
landmark. The actual quantity, expected deviation, source bias, applicability
and even the shape of a useful expectation can remain unresolved. A reader's
experience might support an expectation near two or twenty. Those are local
interpretations of the report; they do not change what was reported.

The bare landmark supplies neither hard endpoints nor a normal distribution
with a known spread. Even symmetry around ten would be an additional premise.
“Nearby” can be useful before there is an agreed metric or sharp boundary.
For a concept such as “warm,” the appropriate comparison may itself need
construction. An exact record of an imprecise report is entirely consistent.

The structural separation to exercise is:

| Participation | What it contributes | What can remain open |
| --- | --- | --- |
| Report and event | A landmark, its reporting basis and an exposed source correspondence | The actual quantity and the report's relation to it |
| Local interpretation | A proposed calibration or other relation connecting report to prediction | Bias, spread, shape, domain and whether this relation applies |
| Supporting observations | Why this space currently uses that interpretation | Reliability, overlap with other evidence and future usefulness |
| Requested view | The question, desired output detail and retained complement | Whether obtaining more detail would help |
| Feedback and revision | New participation that can challenge a prediction or its premises | Which participant should change and by how much |

These are roles in a connected experiment, not new primitive kinds or mandatory
fields on every number. A source is an exposed relation surface, and an event
needs correspondence to the quantity and time it concerns. A string saying
“biased” cannot substitute for a relation that actually changes the read.
Likewise, a numeric source identifier does not supply a calibration law.

This also separates two uses of approximation. A source can lack finer
information, or deliberately leave known detail outside the communication
focus. They can have similar outward expressions and different retained
structure. Neither automatically calls for more attention.

### Consequences for operations

Arithmetic applies to the selected quantities and relations. Adding two report
landmarks can produce an exact total of the reported landmarks; it does not by
itself establish the corresponding actual total or its uncertainty. A selected
interpretation may supply a conditional prediction while its adequacy remains
open. Revising that interpretation must preserve which version supported an
earlier prediction.

The implemented [bounded-quantity reader](GojoBoundedQuantityFixture.md) answers
a narrower question: given explicit support and calibrations, what enclosure
follows? Its bounds are conditional premises. They do not define the meaning
of “about.” Unknown support parameters can be represented by partial patterns,
but the current closed-bound reader does not thereby acquire an arithmetic for
vague or learned expectations. Refusing that particular read need not prevent
another, coarser or differently conditioned read.

Identity can settle some questions without estimating spread. Within an
additive quantity domain, removing the very same quantity cancels it even if
its value is unknown. Two uses of the same description do not establish that
identity. The existing bounded fixture tests shared-slot cancellation with
declared supports; it is not yet a general symbolic quantity reader for
support-free expressions. A future fixture should explicitly test that further
case rather than assume it follows from the parser's ability to store it.

More experience can justify a richer expectation: a source-specific
calibration, several alternatives, soft compatibility, or an explicitly chosen
distribution. Parameters of those descriptions can remain partial too. A soft
compatibility read is not automatically a probability, and a learned spread
is not a guaranteed tolerance. We should add the laws each selected read needs,
without making every approximate value carry a universal interval or bell curve.

## What the current syntax and kernel establish

The [version-1 syntax](GojoTextSyntax.md) constructs ordinary Gojo structure.
Selected kernel readers inspect that structure; the test driver currently
chooses which reader or action to invoke. [Numeric graph transport](GojoNumericFormat.md)
preserves the retained graph. These are separate capabilities.

| Area | Exercised now | Remaining boundary |
| --- | --- | --- |
| Identity and topology | Shared ticks, signed sites, ordered relations, opposed faces, explicit holders and open slots | Equal coordinates or descriptions do not establish identity; cross-packet reconnection needs correspondence |
| Numeric structure | Signed frames, explicit unit standards/cancellation, nested quotients, selected exact rational reads, coarse/fine accounting | Not a complete tensor, metric, contextual inverse or arbitrary-precision algebra |
| Partial construction | Shared placeholders, explicit bindings, partial components and independently readable coordinates | Open integer arguments need enclosing partial structures; choosing among bindings is not a discovery law |
| Uncertainty | Conditional closed bounds, shared causes, separate observation events, conservative nonlinear enclosures and conformance reads | No general landmark interpretation, soft-support arithmetic, learned confidence or distribution propagation |
| Geometry and repetition | Parameters, native line/circle promises, exact affine construction, derivative comparisons and exposed motif maps | Compact shape recognition remains ambiguous in some cases; general curvature, repetition expansion and discovery remain open |
| Change and response | Signed-tension settlement, explicit material refinement/borrow, goal/focus, parameter perturbation and retained alternatives | The blend's action selection and response schedule are supplied by its driver |
| Dependency and history | Versioned curve publication, affected applications, stale evidence, partial replay and restoration before resuming | Selected dependency scope, no general cyclic/concurrent scheduler; mutable references alone are not captured history |
| Authoring and transport | Text construction and version-1 integer graph storage, including shared unknowns, unit promises and attachments | No canonical text writer, imports, general execution engine, distributed identity or concurrent checkpoint |

Evidence comes from the syntax suites for
[mathematics](../../Gojo.Encoding.Tests/GojoSyntaxMathConformanceTests.cs),
[quotient composition](../../Gojo.Encoding.Tests/GojoSyntaxQuotientCompositionTests.cs),
[material refinement](../../Gojo.Encoding.Tests/GojoSyntaxMaterialRefinementTests.cs),
[bounds](../../Gojo.Encoding.Tests/GojoSyntaxBoundedQuantityTests.cs),
[geometry](../../Gojo.Encoding.Tests/GojoSyntaxGeometryTests.cs),
[affine reads](../../Gojo.Encoding.Tests/GojoSyntaxAffineFrameTests.cs),
[local response](../../Gojo.Encoding.Tests/GojoSyntaxLocalBlendTests.cs), and
[replay](../../Gojo.Encoding.Tests/GojoSyntaxReplayTests.cs).
The [inventory suite](../../Gojo.Encoding.Tests/NumericMilestoneInventoryTests.cs)
discovers all current authored fixtures and exported Blocks specimens and
checks their complete graphs through two numeric restorations. This establishes
transport fidelity, not complete semantic validation of every Blocks specimen.

The twelve [illustrated experiments](https://gojo.inklingai.ca/syntax/) give
useful explanations of those selected cases. Their browser controls display
checked stages; they do not execute the Gojo kernel. Representation, verified
local calculation and autonomous adaptation should remain visibly distinct
when we add further examples.

## How far into dynamics we already are

The [local blend](GojoLocalBlendFixture.md) already changes a filter parameter
through tension. It observes a mismatch, applies a selected response, records
a stalled step at the current resolution, and later uses a different response
route. An alternative mixture remains present. The tests choose those actions;
the system has not discovered the response strategy.

The [curve replay](GojoCurveReplayWalkthrough.md) already handles consequences
of a changed premise. It knows which selected applications consume changed
outputs, preserves unaffected work, and can pause while a current result is
pending. It keeps earlier evidence readable and resumes after restoring the
retained dependency context. Its replay helpers record supplied results; the
caller still performs the mathematical reads and chooses the execution order.

Together these support the next experiment: a relation changes in response to
new evidence, and the system can keep track of which conclusions depend on
which version. The next missing link is a small explicit policy that selects a
response from local evidence, with its own inspectable assumptions and failures.
It need not be a universal learning rule.

## Recommended sequence of experiments

### 1. Landmark, candidate interpretation and later feedback

Proposed fixture: `landmark-calibration.gojo` (not implemented by this document).
Use a small count-reporting source so exact arithmetic and the interpretive
uncertainty are easy to distinguish. Start with a landmark of ten and an open
interpretation. Introduce a later observation through a separate exposed port.
Its basis, event correspondence and supporting premise must be visible.

For an intentionally exact toy case, let the report be ten and the later
reference observation be two. Two selected candidate relations fit:

```text
proportional candidate: q = r / 5
offset candidate:       q = r - 8

at r = 10: both predict 2
at r = 20: they predict 4 and 12
```

These candidates are chosen experimental assumptions, not the definition of
approximation or exhaustive explanations. Their reporting and quantity bases
must be calibrated explicitly. Count-domain restrictions also remain visible:
the offset candidate's negative prediction below eight would require a domain
refusal or a revised model, not silent clipping to zero.

The first observation should leave both candidates available. A new report of
twenty makes different predictions before further feedback arrives. A later
observation near four could support the proportional candidate locally. If the
feedback is itself approximate, its interpretation needs the same care; a
hard interval should only be introduced as an explicit fixture premise.

This first fixture should establish:

- Preserving the original report while interpretations and expectations change.
- Representing missing interpretation and missing spread without inventing bounds.
- Keeping distinct sources and events distinct even when landmarks coincide.
- Retaining candidates that current evidence cannot distinguish.
- Linking a prediction to the exact candidate version used before feedback.
- Restoring the whole retained context without changing those correspondences.

The driver may initially propose the candidate relations. That gives a clean
baseline before asking a policy to construct or select them. It also lets us
test partial construction and useful predictions without a general executor.

### 2. One local adaptive response, with a declared law

Extend that fixture with one adjustable calibration and one selected local
response law. Its input is a captured prediction, later feedback and the
connection between them. Its output is a proposed parameter change, not a
claim that the parameter caused the discrepancy. Parameter changes enter the
actuator through signed tension; observing a mismatch alone does not mutate it.

First inspect the local effect of a small permitted perturbation. Record the
response basis, remaining resolution residue and whether the perturbation
improved predictions on later events. Compare against the unchanged initial
calibration and another retained candidate. Avoid judging success solely by
fitting the event that requested the update.

Exercise a constant source, changed source behavior, missing feedback, coarse
actuator steps and a case where different parameters remain indistinguishable.
Separate parameter responsiveness, prediction spread and confidence in the
model's applicability. A single scalar should not stand for all three.

### 3. Two connected local spaces, then a feedback cycle

Give two spaces different exposed evidence and local questions. Let one
publish interpretations that the other uses, then return feedback. Each space
must see only its exposed inputs and correspondence; the test harness may
observe more to evaluate the experiment, but must not secretly choose the
local answer from hidden ground truth.

Begin with explicit event ordering and a small step budget. Add delayed,
repeated and conflicting feedback. Replaying a known observation through a
second route should not manufacture independent evidence. Unknown overlap
remains a hypothesis; neither ancestry tracing nor statistical independence
should be mandatory defaults.

Before treating the cycle as a reusable execution mechanism, expose exhausted
budgets, oscillation, no-progress steps and pending dependencies. The curve
fixture's strict-current-result rule is one selected contract: an explicitly
historical, provisional or coarse result may still be useful under another.
Do not assume all connected spaces must share a clock or converge to one answer.

### 4. Change participation and eventually structure

Once fixed connections have observable utility, experiment with introducing a
candidate route, changing its participation, retaining an alternative and
removing a route that no longer earns its cost. Deliberately change the
environment and test whether a previously weak alternative can recover.

Start with explicit resource and retained-evidence contracts. Material routes
must conserve participation; observational routes need dependency accounting,
not an artificial material-drain rule. Utility, response strength and admission
capacity must not silently become interchangeable numbers. Learned grouping
of sources is revisable and does not establish exact reference equality.

Only then test deliberate history compression: what predictive relation was
retained, what observations or dependencies were lost, and which later claims
are no longer supported. Exact storage is available now; a useful forgetting
law is a separate experiment.

## Work alongside that sequence

Soft expectations and normal-shaped uncertainty deserve a separate mathematical
fixture when a concrete read needs them. Center, scale, shared causes, shape
and their unresolved parameters can be structural. Their operations need an
explicit interpretation; a normal family should not become a universal
definition of Gojo resolution or ordinary approximation. No floating-point
probability engine is required as a prerequisite for the first dynamic fixture.

Keep authoring improvements driven by repeated examples. The existing
constructors and ordinary relations should suffice for the first calibration
experiment. If role order becomes hard to inspect, document its local contract
before introducing shorthand. Shortcuts should expand to the same inspectable
structure. A readable writer and reusable definitions will become useful, but
neither needs to precede the next experiment. A generic evaluator or compiler
would force execution choices too early for this particular step.

## What counts as validation

Mathematical and structural obligations remain firm: exact results under
declared premises, domain refusal, identity preservation, explicit remainder,
material accounting where applicable, and historical/version correspondence.
Test them before and after numeric restoration, including pending states.

Adaptive claims require different evidence: predictions made before feedback,
performance on later events and changed conditions, comparison with simple
unchanged alternatives, resource cost, and visible failure or ambiguity. A
deterministic test can verify that a chosen policy was executed correctly;
that alone does not establish that the policy is useful or generally converges.
Agreement inside a closed feedback loop is not sufficient evidence of accuracy.

For each experiment, retain a short contract: exposed question and premises;
authored structure; selected read; who chooses the action; permitted mutation;
what remains unresolved; and what observation would count against the proposal.
Add an illustrated explanation once the actual fixture supports those claims.

At this checkpoint, `dotnet test Gojo.slnx --no-restore` passes all **1,284**
tests: 767 kernel, 341 encoding, 155 CAD and 21 gear-ladder tests. Sixteen authored
syntax files are discovered by the numeric inventory. This documentation pass
changes no runtime code, fixtures or public gallery behavior.

The recommended next implementation is experiment 1, followed by its single
adaptive-response extension. It connects the user's approximation insight to
the existing blend and replay mechanisms while keeping the unproven choices
small enough to inspect and revise.

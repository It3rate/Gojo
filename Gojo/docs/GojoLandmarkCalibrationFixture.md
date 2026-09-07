# Landmark interpretation and local calibration response

Status: exploratory selected contracts, September 7, 2026. This extends the
[syntax checkpoint](GojoSyntaxDynamicCheckpoint-2026-09.md). The response law
below is an experiment, not a general truth, confidence or negotiation law.

The subsequent [two-space fixture](GojoTwoSpaceFeedbackFixture.md) now connects
two such adapting calibrations through published observations, versioned
receipts and per-space histories. Its stepped demo exercises agreement,
oscillation, delayed delivery and separate local evidence.

Authored experiment: [landmark-calibration.gojo](../../Gojo.Encoding.Tests/Fixtures/Syntax/landmark-calibration.gojo).
Execution: [syntax tests](../../Gojo.Encoding.Tests/GojoSyntaxCalibrationTests.cs),
[mathematical conformance](../../Gojo.Tests/Core/LocalCalibrationTests.cs), and
[selected kernel readers](../Structures/LocalCalibration.cs) with their
[response/application extension](../Structures/LocalCalibration.Response.cs).

## A report, two explanations, and a useful next observation

Imagine a source reporting “about ten.” We record the landmark ten exactly;
we have not thereby measured the actual quantity or learned a tolerance.
Later, a second source supplies an observation of two, with an explicit
connection to the same subject. For this small experiment we choose to read
that second observation as exact. It remains an external premise.

Two candidate interpretations agree with that pair:

| Candidate | Report ten | Later report twenty |
| --- | ---: | ---: |
| Divide the reported landmark by five | 2 | 4 |
| Subtract eight from the reported landmark | 2 | 12 |

The first pair cannot distinguish them. We retain both. The next report makes
different predictions *before* its feedback arrives. An observation of four
then leaves zero mismatch for the proportional interpretation and minus eight
for the offset interpretation. No helper declares a winner, establishes a
source's honesty, or invents the range around either prediction. Below eight,
the offset model predicts a negative count; the selected count floor refuses
that result while retaining it for inspection.

The authored proportional interpretation is ordinary connected structure:

```gojo
let $proportional = group($source, $context,
    span($reported, 0, 5) -> span($quantity, 0, 1),
    point($quantity, 0), ?proportional_expectation, ?support);
```

The open expectation is a real remaining question even when a selected
calculation yields exactly two. The `$` names help us read the example; tests
rename them all and obtain the same behavior.

## One chosen response, including its failures

The adaptive candidate uses an endpoint controlled by a carrier. Ten actuator
ticks correspond to one quantity tick at the endpoint. Starting with fifty
actuator ticks gives the calibration `0..10 -> 0..5`: report ten predicts five.
The later observation of two creates a mismatch of minus three.

The focus explicitly permits changing this endpoint. For report ten, changing
the actuator by one tick changes the prediction by one tenth. A full response
therefore requests minus thirty actuator ticks. The carrier receives that draw
as signed tension and settles at twenty. Its new calibration is `0..10 -> 0..2`.
The old version still predicts five for the old report; its captured value has
not drifted along with the actuator.

On the next report of twenty, the adjusted calibration predicts four, the
unchanged initial calibration predicts ten, and the offset candidate predicts
twelve. This is a small check beyond fitting the training event, not evidence
of general learning performance. If new feedback instead says eight, a full
response changes the actuator to forty. That fits the new event and would now
predict four for the old report. The earlier success has not become a law.

With the authored half response, the same initial problem gives this trace:

| Applied steps | Actuator | Prediction for report ten | Residual against two |
| ---: | ---: | ---: | ---: |
| 0 | 50 | 5 | -3 |
| 1 | 35 | 3.5 | -1.5 |
| 2 | 28 | 2.8 | -0.8 |
| 3 | 24 | 2.4 | -0.4 |
| 4 | 22 | 2.2 | -0.2 |
| 5 | 21 | 2.1 | -0.1 |
| 6 | 21 | 2.1 | -0.1 |

These decimals are explanatory spellings of exact rational reads. At the last
step the requested correction is minus half an actuator tick, which truncates
to zero. The plan retains that half tick and its corresponding minus one
twentieth of a quantity tick. The unresolved residual is still minus one tenth.
A separately authored acceptable residual band of plus or minus one tenth
also permits stopping, but then the requested correction itself is zero. That
is a different structural reason, not a changed definition of approximation.

Finally, let one exposed reference request four and another request eight for
the same report of twenty. The chosen full response alternates the actuator
between twenty and forty. Four applications exhaust the selected budget;
the remaining mismatch and both observations remain present. This is a
counterexample to automatic convergence. The references do not themselves
adapt in this fixture, so it is not yet two mutually negotiating spaces.

## Structure before execution

The first fixture uses these ordinary ordered surfaces. These are local read
contracts, not new primitive kinds or syntax keywords.

```text
observation = group(source port, event site, sample, subject slot, context)
candidate   = group(source port, context, calibration, output floor,
                    expectation, supporting evidence)
prediction request = group(observation, candidate)
comparison request = group(prediction witness, feedback observation,
                           feedback calibration, observation correspondence)

parameter recipe = group(candidate template, open target endpoint,
                         actuator, actuator-to-endpoint calibration)
captured state = act(recipe, endpoint binding, bound candidate,
                     captured actuator tension, captured actuator value)
response request = group(captured state, comparison witness,
                         relaxation calibration, acceptable residual band, focus)
application request = group(response plan, current captured state,
                            local application history, count controller)
```

An observation's sample is an exact record of what was published. Its subject
slot connects observations believed to concern the same underlying quantity;
this is supplied correspondence, not proof of an external fact. Event identity
is separate from source identity. Candidate source and context must match the
selected observation. A missing calibration, sample or reference stays open.

A prediction is conditional on its candidate. Expectation may remain an open
slot even when the candidate computes an exact value. No tolerance, spread,
normal distribution or confidence is inferred from the landmark. The output
floor is an explicit admissibility constraint, not an uncertainty bound. A
negative count prediction remains visible when that constraint rejects it.

Calibration endpoints are exact grounded scalar reads: points, locations or
the existing rational participation form. Thus a partially constructed target
frame can bind to a fractional endpoint without fractional carrier state.
Maps use the ordinary affine law. Constant maps retain their input as residue.

Feedback is another exposed observation. Comparing it requires an explicit
calibration and a correspondence from the prediction's observation to that
feedback observation, with the same subject and context. A mismatch says what
the selected statement fails to account for; it does not assign blame.

This comparison contract requires a nonconstant feedback calibration. A
constant candidate can still make a conditional prediction, with its lost
input retained as residue; a constant feedback map would discard the very
observation selected to challenge that prediction and is refused here.

The versioned binding uses `StructuralPattern.Refine`: rebuild paths containing
bound slots, retain untouched references, and preserve shared changed relations
within one refinement. Existing `StructuralPattern.Apply` still instantiates
the relation tree. This explicit distinction matters when a source port or
candidate version is itself relation structure. Equal reconstructed structure
does not substitute for the retained source reference. Refinement operates on
acyclic relation templates and does not snapshot the entire reachable world.

## Chosen response law

Only one target endpoint is adjustable in this first recipe. If the sample is
at fraction `u` of the source frame, and one actuator tick changes that endpoint
by `k` output ticks, the selected model's sensitivity is `s = u*k`.

For feedback residual `e`, the authored relaxation calibration supplies
`0 < alpha <= 1`. Outside the authored acceptance band, request
`delta = alpha*e/s`. Inside the band, request zero while retaining the actual
residual. When sensitivity is zero and a change is required, the response is
unresolved. There is no forced step or arbitrary direction.

The plan retains the exact requested delta, its whole-tick truncation, the
derived inverse response frame, predicted remaining mismatch and both actuator
and output rounding residues. Planning is observation. Applying is a separate
signed-tension input. Mathematical policy calculations can be exact while the
policy's usefulness and premises remain uncertain.

The predicted residual assumes the proposed whole-tick change is absorbed.
Application records the actual before/after states and any unpaid draw; a new
prediction must use that actual after state. For example, asking the initial
count model to match minus two requests minus seventy actuator ticks. Fifty
are absorbed, twenty remain as negative tension, and the actual prediction is
zero. The still-impossible count has not been silently admitted.

The signed accounting is explicit in the before/after evidence:
`requested -70 = value change -50 + remaining tension -20`.

Applications require the exact current state version and a matching value and
tension snapshot. Ordered local history links each before state to the prior
after state. Repeating a delivered plan, using a stale version or exhausting
the selected count controller does not inject another delta. Reusing an old
observation in a deliberately new optimization step is allowed; its identity
remains shared and the additional attempt is not independent evidence.

This is a caller-owned, sequential state/history contract. The caller must
retain and supply the current version and history together; discarding history
or replaying an older world is not detected by a hidden global registry.
The checks validate current selected arithmetic and historical state-chain
correspondence, not the authenticity of arbitrary imported history. The count
controller limits application attempts, including zero steps. It is neither a
material resource budget nor a confidence measure. An external actuator change
requires a new capture and an explicitly selected new experiment/history.
Unrecorded external changes that return to the same scalar state cannot be
detected by these value/tension checks alone.

The first application boundary is one free forward actuator. Attached
settlement worlds need a broader mutation/rollback contract and are refused
here. An excessive negative request can leave an unpaid draw as tension.
The next capture preserves that debt; this response law does not silently
treat a pending actuator as a fresh unconstrained calibration.

For this boundary, “free” also excludes an already materialized opposed face,
whose balancing would involve another participant. There is no concurrent
mutation guarantee. Representability is checked before the single-carrier
mutation; no projection mutates a source or drains a feedback observation.

Templates and retained maps are owned as fixed premises for a version.
Capturing a state copies the actuator observations, not every reachable
mutable reference. Externally retargeting a retained calibration in place is
outside this contract. Publish changed premises as a new version, as in the
[curve replay fixture](GojoCurveReplayWalkthrough.md).

## Execution and eventual compilation

The parser constructs the experiment. A caller selects event order and invokes
prediction, comparison, response-planning and application readers. The authored
repeat connects the propose/apply request templates and their feedback slots;
the tests bind and execute those same templates.
There is no general evaluator or scheduler. Each request exposes operands,
bases, unknowns, controller, focus and permitted mutation. Numeric restoration
must preserve those connections. A future compiler can specialize these
selected laws while preserving the same version, domain and accounting checks;
this fixture supplies neither a compiler nor a claim that every graph executes.

The driver supplies synthetic reports and feedback through ports. Their exact
toy values make the checks inspectable; real feedback would bring its own
uncertainty. Which source to trust, when to explore another relation, and how
to distribute response among participants remain open policy questions.

## Fixture inventory and verification

The eight roots retain observations; candidate interpretations and a feedback
map; the adjustable recipe; policies, focus, history seed and controllers;
the response request; the application request; the repeat; and its shared
request slots. Ordinary version-1 forms suffice. No parser keyword or numeric
format version changes.

The tests cover:

- Exact affine arithmetic, reversed frames, fractional endpoints, separately
  calibrated feedback bases, degenerate source frames and overflow refusal.
- Open interpretation and expectation, underdetermined candidates, missing
  feedback, and strict source/context/subject/correspondence boundaries.
- Full and half responses, later-event prediction, changed feedback, an
  independently measured one-tick perturbation, and reversed sensitivity.
- Resolution stall versus chosen acceptance, zero sensitivity, conflicting
  demands, exhausted budgets, stale plans, repeated delivery and unpaid draw.
- Rejection of inconsistent arithmetic and mutation outside the selected
  free-actuator boundary, with the original history and physical state intact.
- Complete numeric restoration of authored graphs, a pending next plan with
  prior history, a completed conflict timeline and outstanding actuator debt.

The generic numeric inventory also discovers this fixture and checks the
complete retained graph through two restorations. Transport preserves local
correspondences inside the packet; independent restored worlds have distinct
identities. History owned by this experiment retains its fixed source/map
premises, while value/tension observations are copied.

Verification on September 7: `dotnet test Gojo.slnx` passes **1,324 tests**:
782 kernel, 366 encoding, 155 CAD and 21 gear-ladder. This adds 40 cases,
including discovery of the seventeenth authored syntax fixture by the numeric
inventory. The existing public gallery is unchanged; this step's examples are
numeric traces in this walkthrough rather than another browser demo.

This is the first two checkpoint experiments plus a bounded conflict probe.
The next experiment should connect **two adapting spaces** through explicit
publication and feedback surfaces, then exercise delayed and provisional
results. Only after those interactions are observable should we experiment
with distributing responsiveness, changing connections, or pruning history.
There is no universal blame, trust, convergence or learning rule in this pass.

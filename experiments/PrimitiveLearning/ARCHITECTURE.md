# Primitive learning: implemented structure and boundaries

This document describes the implementation in this directory, rather than a
claim about campaign results. [PROTOCOL.md](PROTOCOL.md) fixes the experimental
questions; [LITERATURE.md](LITERATURE.md) identifies the research assumptions.
The generated run manifest and results record the settings actually executed.

## What is native, and what is authored

Each learner exposes a connected Gojo `Root`. Learned references, count sites,
rules, calibration laws, choice offers, edit parents, selected bindings and
update witnesses live in those roots. The C# classes construct and inspect local
surfaces, request observations, schedule updates and settle carrier sites.
They are deliberately small authored learning laws, rather than a generic Gojo
learning engine. Removing the classes does not leave a hidden learned table as
the only representation of the model.

The host owns the external relay, deterministic episode tapes, grammar
enumeration, scheduling and artifact projections. A world publication supplies
an observation; it does not write any learner's memory. The experiment does not
invoke Jev, supply a learned brightness metric or convert tension automatically
into reward. Explicit resource evidence and declared selection costs drive the
two empirical choice learners.

`G(...)` below means a native ordered group. Field position is this local
surface's declared inspection grammar; it is not an opaque operation enum.

## External relay and observation ports

The relay authors a small task: fitting parcels should route with arriving flow,
and non-fitting parcels should route against it. A correct route publishes a
resource change of `+4`; an incorrect route publishes `-4`. An explicit hold or
absent parcel has gross change `0`. The selected paid query fee is subtracted
from that change. These are episodic consequences, not a persistent physical
resource reservoir or a demonstrated survival model.

| Port | Exposed observation | Withheld information |
|---|---|---|
| Load acquisition | Presence `0/1`, source identity, open magnitude | Private load coordinate, fit threshold, absolute zero/unit |
| Load comparison | Requested current-versus-anchor order `-1/0/+1` | Distance between acquisitions or a recovered rank metric |
| Fit probe | A later independently observed fit `0/1` tied to the exact acquisition | Threshold or load magnitude |
| Continuity context | Public binary cue `0/1` | A guarantee that the current forecast is correct |
| Previous direction | Last delivered direction and reference age in provenance | Direction hidden by an intervening failed acquisition |
| Current flow query | Direction `-1/+1`, or an open read | A successful answer when the sensor is unavailable |
| Arrival audit | Direction observed after the command, or an open read | An advance free current-flow observation |
| Actuator consequence | Signed resource change tied to the command | Permission to alter a learner internally |

Presence, unavailable acquisition, observable equality and unknown magnitude are
different structures. An unavailable sensor does not make a physically executed
route disappear. An absent parcel does not become a zero load.

`RelayWorld` keeps a private external instrument dictionary connecting opaque
sample identities to its measurements. Only its comparison callback returns
their signs. Strictly increasing private encodings preserve that order. Novel
loads insert unseen measurements between familiar acquisitions without adding
a numeric coordinate to a sample. Restoring a model therefore also requires
reconnecting the external comparison instrument to transported sample identities.

Episode tapes are independent of learner actions. Persistence describes actual
repeat probability in one context; the other context has independent direction.
The primary protocol uses repeat probability `0.05`, so that context normally
reverses. The separately labeled persistent comparison uses `0.95`. Neither
context label supplies its direction law to a learner.
The last delivered observation survives missing/absent intervals. The predictor
currently uses cue and prior direction, not the separately exposed reference age.

## 1. Adaptive ordinal distinctions

Implemented in [AdaptiveOrdinal.cs](AdaptiveOrdinal.cs).

```text
G(policy, G(ordered landmarks), decision rules, latest patch,
  G(update counter, comparison counter), G(recent receipts))

landmark = G(opaque acquisition,
             G(outcome-0 point -> count, outcome-1 point -> count))
```

The policy declares source, presence/order/consequence frames, a landmark budget,
count capacity, count-aging law and receipt budget. Prediction uses requested
comparisons in a binary search, then admits native support conditions. No load
coordinate is consulted by the learner.

Feedback is processed after the pre-admission prediction. Consistent observed
consequences can share a region. Conflicting consequences can insert an anchor
or move a retained boundary reference. Same-label runs can lose redundant
references through explicit coarsening; capacity refusal remains witnessed.
The experiment allows at most sixteen landmarks. When a landmark's count total
reaches its declared capacity, the native half-scale law ages both counts, with
truncation residue retained.

Rules can return one observed consequence, alternatives, or open. Exterior
piecewise-constant extrapolation is admitted only after both consequences have
been observed. This is an explicit learned hypothesis, not a guaranteed bound
on the physical source. A finite recent-receipt window prevents immediate
duplicate feedback; forgotten events cannot be excluded indefinitely.

## 2. Local ordinal prediction

Implemented in [OrdinalPredictor.cs](OrdinalPredictor.cs).

```text
G(policy, G(context-key -> consequence counts), context memory,
  latest patch, G(update counter, mismatch counter))
```

The relay supplies two declared context ports: continuity cue and previous
direction. Each observed context key has native counts for outcomes `-1/0/+1`.
The public constructor also supports a small same-axis history mode, but that
mode is not the relay's context construction.

Prediction builds native sums, count differences and support rules. Its authored
publication law requires total support at least four and a winning lead of at
least two over each alternative. Otherwise the point remains open. Observed
alternatives and captured counts remain inspectable even when a point forecast
is emitted. Empirical alternatives are not guaranteed sensor support, confidence
probabilities or an automatic reduction in physical uncertainty.

Later actual direction updates the matching row; mismatch claims expose
disagreement. Counts age through an explicit half-scale projection. The model
has a finite row budget. Updating a row is a local learning response to observed
evidence; mismatch alone does not cause generic reinforcement or settlement.

## 3. Empirical query selection

Implemented in [QueryLearner.cs](QueryLearner.cs).

```text
G(public axes, cost-to-benefit calibration,
  G(context rows), last decision, last feedback,
  G(recent receipts), retention policy)

context row = G(context point, G(skip offer, query offer), selector, probe phase)
offer ledger = G(count, positive benefit, negative benefit, paid cost)
```

Each public context offers skip or query. Native equations calculate a truncated
mean net score on a declared scale of 256. `OperationClaim` comparisons and
`ScopedChoice` carry selection evidence. The authored exploration schedule
samples each alternative initially and periodically probes the other alternative;
frozen deployment uses the retained empirical comparison without those probes.

Benefit and cost have separate axes. The constructor explicitly values one cost
tick as one benefit tick. `BindBenefit` and `BindCost` connect a later physical
publication to the exact selected decision; unrelated or reused evidence remains
open. Ledgers and recent receipt retention have explicit finite capacities.

In the regular composite training contract, benefit comes from a shadow route
of a **known-fitting parcel** using the chosen direction. This isolates the
direction service's usefulness from fit/program mistakes. It is an authored
local credit contract and one additional training shadow action per update.
The `whole-route-credit` control instead uses the selected composite route's
gross consequence. These contracts need not learn the same query policy.

Query scores pool lifetime evidence while the forecast service can improve.
The warmup reduces early exposure to poor forecasts; it does not make the
remaining observations stationary or prove convergence. There is no forgetting
law for these utility totals. The choice reads the public context's ledgers,
rather than the current action's support ambiguity. Fit and forecast services
still run on queried episodes. This establishes a selective paid-observation
mechanism, not a complete demand-driven refinement procedure.

The choice method has no query-price input. A changed-price frozen test measures
cost robustness of a policy trained at the original price; it does not demonstrate
price-sensitive query adaptation. The configured price increase from1 to5 crosses
the simple known-fitting task's ideal query/skip boundary in the unpredictable
context. Comparisons are reported as work, but are not priced into this resource
score by the current prototype.

## 4. Bounded native program selection

Implemented in [ProgramLearner.cs](ProgramLearner.cs).

```text
candidate = G(id point, native condition/response rules, utility ledger,
              complexity point, edit-parent witness, G(case counts))
```

The constructor enumerates all sixteen binary response assignments on fit and
flow ports. Constants and one-port candidates precede two-port edits. Each
candidate has a native parent/patch relation, rules, declared complexity and
four input-case counters. The hidden correct mapping is not consulted during
construction. All candidates exist before training; this is not unrestricted
online syntax discovery or learned library induction.

Training tries undercovered candidates on the encountered input case, then
periodically probes coverage. Actual later resource evidence credits only the
tried candidate through an explicit correspondence. A candidate needs four
observations in each case to qualify for retention. Native empirical mean
utility, with a separate complexity penalty, selects the retained program.
The penalty is rule count times the number of used ports on the score's declared
256-scale lane. Retained edits and selection claims remain inspectable.

Candidates receive different sequential experiences, input errors and fees.
Their scores are empirical estimates with recorded coverage, not a matched
counterfactual evaluation of every program on every episode. Isolated program
tests supply exact fit and flow ports; their reported return does not debit those
input-service fees. They isolate the grammar learner rather than measuring
economical end-to-end sensing.

## Composite execution and feedback

[StudyRunner.cs](StudyRunner.cs) schedules these native services:

```text
opaque sample -> learned fit hypothesis -----------+
                                                 |
cue + last direction -> learned flow hypothesis --+-> selected native program
                         |                       |          |
                         +-> query choice -> flow+          v
                                              explicit actuator hold fallback
                                                         |
                                             physical consequence publication
```

An open fit hypothesis has an inspectable response fallback to fit mode `0`.
An open forecast has an inspectable response fallback to the last witnessed
direction. Neither fallback is presented as observed sensor truth. Open program
intent is retained while the actuator issues an explicit hold; paid query fees
still apply. Native calibrations connect direction/action and resource/benefit
bases, retaining their predictions and residue.

After acting, training admits fit feedback, arrival feedback, query credit and
actual program credit through their separate contracts. Query and program training
start after the declared A/P warmup. The simultaneous-query control starts query
learning immediately. `RelayBrain.Connect` retains all four current roots
and the current connected publications. Operational witnesses are replaceable;
learned carrier sites remain authoritative.

The current integration uses sampling, native support rules, equations, claims
and scoped choices. It does not exercise every previously developed finite,
coarse-LUT or fast/slow relative-memory surface merely by including Gojo.

## Controls, transport and measurement

The runner provides isolated learned/frozen/shuffled conditions, the full four
learner on/off factorial, communication removal/inversion, always/never query,
whole-route credit, neutral actions, changed dynamics, novel loads, missing and
absent sensors, and private monotone re-encoding. A disabled learner retains its
authored initialization and receives no learning updates; it is not necessarily
an erased information port. Test phases disable learned updates while allowing
operational reads and fresh witnesses.

The frozen guard compares a declared learned-state projection, excluding
legitimate changing operational selectors/latest reads. A complete root hash
would change during valid frozen execution. Read counters and publication
coverage must be interpreted separately from learned-state mutation.

Native transport uses `GojoNumericFormat`, including shared identities,
coordinates, TickRefs, carrier state, attachments and unit surfaces. Runner
archives verify all four graph roundtrips and query/program behavior. The
connected [archive regression](../../Tests/PrimitiveLearning.Tests/NativeCompositeArchiveTests.cs)
also reconnects a declared external comparison fixture and checks all four
restored behaviors, shared ports and independent resumed updates. It does not
serialize a complete live world service for arbitrary deployment.

Resource return, route correctness, query rate and work are separate measurements.
Action coverage includes an executed hold; it is execution coverage, not useful
route success. Classification coverage excludes abstention, and covered accuracy
is conditional on publication. Overall route accuracy includes absent episodes
in its denominator; `ActiveAccuracy` excludes known absent parcels. The historical
`Stable*` and `Unstable*` result field names refer to cue `0` and cue `1`, not
to a guarantee of persistence, predictability or truth in a shifted world.
Training audits and shadows are counted separately from
deployment fees. Paired seed differences are the appropriate unit for comparisons;
pooled episode counts are not independent uncertainty estimates.

The primary and persistent studies use shared seed identities for a descriptive
protocol comparison. The persistent study is not an additional independent set
of primary replicates. Report seed-level intervals with their sample counts and
the exploratory multiplicity of the contrasts. A held-out test shift freezes
learning, so it measures transfer of the fixed policy rather than adaptation to
the new law or price.

Graph inspection counts reachable native sites and unit structures. Coordinate
counts are occurrences rather than unique storage slots, and graph counts are
not memory-byte measurements. Decision digests summarize projected choices;
they are not substitutes for native learned-state signatures or complete trace
parity.

This finite relay can demonstrate the particular learning laws, their cost
tradeoffs, transported behavior and measured interaction on the authored task.
It leaves action-dependent observation distributions, long-horizon control,
breeding, unrestricted recursive synthesis and general intelligence for later
experiments.

# Composite emergence: native structures and observation boundaries

This document describes the implemented local surfaces. The runner's manifest
records the settings actually executed. [LITERATURE.md](LITERATURE.md) explains
the research motivation; the final report interprets measured results.

The host supplies an external apparatus, pre-generated event tapes, construction
of a bounded grammar, scheduling, artifact output and inspection projections.
It does not publish the private apparatus rule as a learner input or write
learner memory. Learned values, equations, context rows, evidence counts,
choices, retained bindings and update receipts remain connected Gojo structure.
The C# helpers construct and read those structures under authored local laws.
This is an exploratory learning mechanism, not a general kernel convergence
guarantee.

`G(...)` denotes a native ordered group. Field order is the declared local
surface grammar. Display descriptions and JSON are projections, not the model.

## External apparatus

An input direction `a` is followed by response `b = a * h`. The hidden rule `h`
is either `-1` or `+1`. Fresh independent fair input directions make either
single channel's history have the same distribution under either rule, provided
its interface supplies no second channel or global consequence.

Discovery trials have a newly sampled rule. Persistent trials retain it until
an unannounced probabilistic reversal. Tape generation uses separate random
streams for rules, inputs, goals, exploratory actions, delays and availability;
controller variants share the same exogenous tape.

The world publishes signed directions through `ReadWitness` surfaces, with
explicit time coordinates. Publication order and time establish an authored
pairing opportunity; pairing itself is not a discovered concept. A missing
observation exposes a placeholder rather than a numerical zero.

For source learning, an independently issued action is followed by the actual
direction consequence `action * h`. This is later supervision through physical
behavior, not an advance label of `h`. The task consumer receives a goal and
acts under a wiring convention. A correct committed action earns `+8`, an
incorrect committed action `-12`, and an explicit hold earns `0`. A paid probe
costs the configured price in addition to that gross consequence.
Source and transfer stages count fresh sensor acquisitions without debiting a
probe fee; the refresh stage prices them. Training-event counts are scheduler
events, not counts of every candidate equation or ledger operation.

| Participant | Permitted observations | Withheld information |
|---|---|---|
| Primitive A | Its own direction publication, its ordering, its local prediction evidence | B, goal, committed action, global resource outcome, hidden rule |
| Primitive B | Its own direction publication, its ordering, its local prediction evidence | A, goal, committed action, global resource outcome, hidden rule |
| Relational source learner | Exposed A/B events, current/prior memory, exploratory action and its later direction consequence | Hidden rule, hidden task wiring, advance consequence |
| Service consumer | Published feature coordinate and public goal, later resource outcome bound to its decision | A/B ancestry, hidden rule or wiring |
| Refresh learner | Public availability, coarse age, previous outcome class, later resource outcome bound to its decision | Hidden rule, switch announcement, raw A/B |
| Analyst | Recorded world reference and all saved inspection artifacts | No restriction; this read is not a learner input |

## 1. Single-channel primitives

[SensorPrimitive.cs](SensorPrimitive.cs) wraps the existing native ordinal
predictor with a deliberately narrow contract:

```text
G(channel axis, predictor root, current public observation,
  last own-channel forecast, bounded observation identities)
```

Each primitive trains only on later observations of its own channel. It
publishes a captured direction and time, and may retain its own local forecast.
Neither primitive receives the consequences used to select the relational
service or task consumer. Its inability to recover the apparatus rule follows
from this complete information boundary, rather than a deliberately weak
classifier alone.

The relational service consumes current direction publications, not the
primitives' forecasts. The independent fair streams intentionally provide no
predictive route to the hidden rule; this experiment does not establish a
task advantage from primitive forecast learning itself.

An eight-event identity window rejects immediate duplicate publications. It is
finite memory, not lifetime duplicate protection. Forecast support and open
reads remain distinguishable from the fresh observed direction.

## 2. Relational source learning

[RelationService.cs](RelationService.cs) initially offers four single-source
equations: current A, current B, previous A and previous B. The equations,
source selections and temporal offsets live in the graph:

```text
source = G(exposed input port, temporal offset)

candidate = G(native equation, ordered sources, prediction rows,
              prequential correctness ledger, complexity point)

prediction row = G(G(feature point, exploratory-action point),
                   G(negative-outcome point -> count,
                     positive-outcome point -> count))

model = G(candidates, selected-index point -> retained candidate,
          G(trial count, expansion count, retained edit))

root = G(contract, model, operational memory, recent receipts,
         forgotten-receipt count)
```

Input acceptance retains current and previous publications through explicit
operational memory. A response older than the current input remains an open
read. The memory-removal control withdraws retention of A across the later B
arrival; it does not turn a missing sample into zero.

Each candidate predicts the later consequence from its feature and the genuine
exploratory action. Prediction rows require at least four observations and a
winning count lead of at least two; otherwise the publication stays open.
Observed minority outcomes remain in the count structure. These are empirical
alternatives, not calibrated probabilities or guaranteed physical bounds.

Scoring is prequential: prediction is read before admitting that trial's
consequence. Correct predictions add `+1`; wrong or unanswered predictions add
`-1`. All available candidates receive the same observed exploratory outcome.
This is parallel prediction evaluation, not physically executing every
candidate's separate action.

After 64 credited trials, inadequate single-source performance can trigger one
bounded expansion. The authored grammar constructs product, sum and difference
for each pair among the four current/prior sources, producing up to 22 total
candidates in this implementation. Native equations carry the operation;
ordinary descriptive strings do not choose it. Candidate order is shuffled by
a declared seed to reveal ordering sensitivity. Scores include retained
prediction-row complexity; candidates need a minimum evidence count to qualify.

The apparatus rule is available from the same-trial product. The same-trial
sum and difference can also distinguish it through three feature states.
Thus product selection is not uniquely required for success. The study tests
selection of a useful relation inside a supplied grammar, rather than invention
of an operator or hierarchy.

### A physically narrow publication

The source's outward feature witness contains a fresh boundary-event carrier,
the captured feature coordinate and public time. Its equation and A/B premises
remain connected to a producer-side derivation witness. They do not cross the
consumer port. The consumer captures that exposed coordinate and identity
without retaining the source learner's private graph.

This boundary matters because merely passing fewer C# arguments would not
withhold raw information if a consumer could traverse it through the native
witness's provenance. Privacy tests must inspect reachability of the actual
advertised surface.

### Frozen learned structure and operational state

`LearnedRoot` copies the candidate rows, ledger values, selected binding and
compact retained edit into native parameter structure. A deployed service can
continue receiving A/B events and updating its operational current/prior memory
while leaving those learned parameters unchanged. Frozen evaluation therefore
does not mean that observation memory or the latest read stops moving.

## 3. A fresh consumer using a frozen service

[ServiceConsumer.cs](ServiceConsumer.cs) supplies a native local choice market.
The service consumer sees feature and goal. The flat comparison receives A, B
and goal through the same choice-learning law.

```text
row = G(context points, G(action -1 / ledger, action +1 / ledger),
        probe phase, selector, scoped choice)

ledger = G(observation count, positive resources, negative resources)

root = G(context axes, legal frames, choice axis, action offers,
         resource / score / count axes, sparse rows, learning law,
         forgotten-receipt count, update count, aging evidence,
         current decision / feedback / recent receipts)
```

Native signed utility scores use separate positive and negative accumulators,
an explicit 256-scale mean read, comparison claims and `ScopedChoice` selection.
Each action is tried twice initially in an encountered context, then the other
offer is periodically probed on a 16-update phase. Later physical resource
feedback must be bound to the actual selected decision before updating that
offer's ledger. A finite identity receipt window rejects recent duplicate
credit.

Context rows are created from advertised coordinates while learning. An unseen
frozen context keeps empty evidence and uses the declared first-action fallback;
it does not secretly add a trained row. Open input produces an open intention,
which the runner must account for separately from successful actions.

Changing the task wiring reverses the correct action correspondence while
retaining the apparatus's relational signal. Source freezing plus fresh
consumer training tests service reuse into that changed consequence law. It is
not transfer to an unrelated physical environment. Source pretraining and
target learning budgets are separate quantities.

## 4. A manager of freshness

[RefreshLearner.cs](RefreshLearner.cs) reuses the same native market with
probe/reuse offers and three context axes: availability `0/1`, age bucket `0..3`
and prior outcome class `-1/0/+1`. It receives no private switch flag. The
runner's explicit operational memory supplies the currently retained feature
and observed outcome; native manager evidence retains its learned utility.

The market credits immediate physical net resources. It is a finite contextual
choice learner, not a Bellman backup, discounted future-value learner or an
automatically discovered option hierarchy. A successful manager can choose a
useful refresh policy within this contract. Its lifetime estimates age when a
ledger reaches count capacity; this is local finite forgetting, not an inferred
physical noise law.

In the delayed-history arm, delivery to operational outcome memory is delayed
by five decisions including next-tick delivery. Manager utility credit is still
immediate while training. Frozen evaluation performs no utility updates. This
arm changes the available outcome-history context; it does not delay every
feedback route. Initial no-prior-outcome and observed neutral gross resource
both publish the coarse class 0. The memory's `outcomeKnown` carrier keeps those
states separate internally, while the advertised contract intentionally
coarsens them.

Exact action, goal and success/failure can identify the previous apparatus rule
in this binary world. The outcome-memory comparison measures this alternative
route. At switching probability `q`, its ideal one-step accuracy is `1-q` and
gross return is `8-20q`, apart from initialization. Always probing with price
`c` gives ideal return `8-c`. The one-step economic boundary is `q=c/20`.
These are analyst benchmarks from the authored world law, never labels fed to
the manager. Learned refresh behavior need not beat a comparator that receives
equivalent information for free.

## Finite evidence, retained history and transport

Choice ledgers have explicit count/resource boundaries. At count capacity 64,
the native half-scale read ages count, positive and negative totals with
truncation residue retained. Retained receipts capture context, chosen action,
observed resource and before/after ledger coordinates rather than storing old
whole worlds. Lower prediction-count aging is separately inspectable in its
native update evidence. Forgetting remains a declared loss of historical
detail, not a promise of exact reconstruction of discarded episodes.

Choice aging occurs per action in each context when it reaches 64 observations.
A rarely tried offer may therefore retain old evidence much longer than its
frequently chosen competitor. The count phase forces an alternate probe every
16 credited decisions within a context, rather than every 16 world steps.

Native archives use `GojoNumericFormat`. Signature checks and post-restore
behavior tests verify retained model transport, while explicit shared-axis
correspondences reconnect world publications to restored input ports. A saved
model does not serialize a complete external simulator or every forgotten
training event. Graph counts measure reachable sites, not managed-memory use;
transport bytes are a distinct artifact measurement.

## Operational claim boundary

The implemented hierarchy and available operations are authored. Its strongest
possible result in this pass is learned selection of a relational service,
reuse through a boundary that physically withholds raw ancestry, and a second
learned decision about freshness. The individual sensor interfaces cannot
perform the joint task, while an unrestricted flat model with both inputs can.
Neither restriction nor successful composition proves subjective comprehension,
unpredictable emergent goals or open-ended hierarchical invention.

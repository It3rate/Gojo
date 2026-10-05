# Failure-triggered subscriptions: first campaign

Short subscriptions are workable as connected Gojo structures. They can buy
fresh outputs, preserve uncertainty, expire, cancel, renew and reactivate. The
mixed-pattern runs found useful help and reused dormant connections when a
pattern returned. The supplied attention policy is not yet reliable: one
wandering-input run repeatedly bought and exported a harmful prediction despite
retaining evidence of the harm. Cancellation alone did not teach avoidance.

This is a useful mechanism experiment, not evidence of a trained market or a
general improvement in prediction. No core files or earlier experiments changed.

## What was frozen and measured

The initial [protocol](PROTOCOL.md) was committed as `060aaa8`. The completed
implementation and exact executable choices were committed as `d69391f` before
the first campaign. The retained campaign has no tuned replacement runs.

There are 24 runs of 241 arrivals: four policies, three scenarios and two
seed/order combinations. Each run has 233 resolved outcomes and seven unknown
outcomes among 240 prior opportunities; its final forecast is still pending.
Missing arrivals consume subscription opportunities without creating accuracy
evidence. All outcomes used to select a future forecast precede its target.

The seeker predicts continuation of the latest observed difference. Four nearby
sources offer that same prediction, sign reversal, linear extrapolation of the
difference, and zero change. Here sign reversal means numerical negation, not a
reciprocal or the contextual operation of changing paired faces. The duplicate
source is an explicit redundancy control. Recipes, neighborhood, funding and
initial attention laws are supplied; actors receive no scenario labels or future
samples. The two seeds use different neighbor rotations, so the effects of input
variation and source order cannot be separated in this campaign.

Subscriptions last one or five shared input opportunities, with at most two
active. Failure of the actual exported prediction opens a search of at most two
local advertisements. Forecasts, contract boundaries, source publications,
receipts, conditional evidence, policy parameters and payment relationships are
native Gojo structure. C# schedules this small population and reads reports.

## Results

The table sums both seed/order combinations. Error is accumulated absolute error
of the single forecast committed before each outcome. Cost is whole-system work,
inspection, activation and cancellation expenditure. Delivery fees move between
participants and are not an additional whole-system expense. Lower is better
for both measures, but they are different units.

| Policy | Mixed: error / cost | Wandering: error / cost | Unpredictable: error / cost |
|---|---:|---:|---:|
| Seeker alone | 823 / 472 | 432 / 472 | 1,468 / 472 |
| One-opportunity help | 454 / 1,942 | 1,032 / 2,708 | 1,417 / 2,952 |
| Five opportunities, no prospective term | 522 / 1,740 | 1,777 / 2,607 | 1,398 / 2,223 |
| Five opportunities, prospective term | 572 / 1,888 | 1,763 / 2,621 | 1,347 / 2,250 |

On mixed input, five-opportunity subscriptions without the prospective term cut
error by 36.6% compared with the seeker alone. Compared with one-opportunity help,
they reduced inspections from 358 to 244 (31.8%) and expense by 10.4%, while
increasing error from 454 to 522. This is an observable accuracy/cost tradeoff,
not a universal win for longer contracts. Assistance remains considerably more
expensive than the unaided seeker. The supplied calibration of four credits per
avoided error tick does not establish that these subscriptions are economically
worthwhile across scenarios.

This policy rewards absolute-error relief. Direction agreement is also reported,
but does not independently strengthen connections. Choosing among directional,
scale and task-specific utility remains a future attention/training decision.

Prospective evidence changed decisions, but its value was inconsistent. It
improved error in five of six paired runs, while its larger loss on mixed/47
made the combined mixed result worse. Two context ranges, distinguished only by
the sign of the initiating prediction error, pool situations with very different
useful suppliers. The quotes estimate aggregate relief conditional on resolved
outcomes; they neither promise five consecutive successes nor predict input
availability.

In the unpredictable scenario, zero-change prediction can improve on an
inappropriate continuation baseline. Its success is not evidence that a source
learned to predict independent future random input. Two short seed/order
combinations provide exploration evidence, not a statistical generalization.

## What the connections demonstrate

Every mixed five-opportunity run had all four routes inactive at arrival 143,
after the flat phase, and connections reopened at arrival 144. In mixed/47 with
prospects, the first alternation reduced self-error 172 to exported error 14;
the returning alternation reduced 162 to 14. The sign-reversal source was
selected 40 and 38 times respectively. Similar revival occurs without prospects:
this supports reusable dynamic connections, not a unique benefit from the quote.

Dormancy retains route identity and bounded evidence. It does not keep an old
answer live: publication after revival requires a new calculation. Useful terms
can renew without new shopping; a correct assisted answer does not trigger help
seeking merely because the unaided forecast would have failed.

The duplicate source earned exactly zero conditional relief in every run. Its
valuation is honest, but filtering is weak: across the campaign it still received
1,123 paid deliveries and supplied 711 selected forecasts. Knowing that help is
redundant does not yet reliably stop buying it.

## The main failure: buying known harmful help again

In wandering/47 with prospects, self-error was 210 and exported error was 1,552.
The sign-reversal source received 201 paid deliveries. Among its 200 evaluated
outcomes, none helped, 194 harmed and six were neutral. It accumulated signed
conditional relief of -1,372 and was nevertheless selected 194 times. In arrivals
49-80 alone, self-error 31 became exported error 259, alongside 32 cancellations.

The reason is visible in the frozen policy. Negative evidence remains in the
native counters, but its contribution to the choice score is clipped to zero.
The highest-ranked current paid source supplies the actual export even when all
scores are zero. The self-prediction is used only when no paid output is
available. Cancellation can therefore lead to another purchase of the same bad
source, with a fixed route order breaking export ties. This is a poor supplied
attention rule, not missing harm evidence or an invalid trial.

Past success can also outlive its relevance. In mixed/47, the sign-reversal
source's negative-error cohort held +292 relief by arrival 184 and still +250
after the later harmful zigzag period. Adding that prospective term increased
changing-slope error from 38 to 54 and later zigzag error from 121 to 165 compared
with the matching run without prospects. Context refinement and the useful
lifetime of evidence matter alongside subscription length.

## Operational cost and retained structure

The final retained states contain 229-376 carriers and 2,065-3,901 relations.
Compressed current checkpoints are 33,156-63,238 bytes. Finished observation
frames are released; sparse reports and one replaceable continuation are retained.
This bounded final state does not imply cheap execution.

Across 5,784 arrivals, measured native steps allocated 21,385,107,336 bytes
cumulatively and took about 63.8 seconds. Report aggregation and frame release
allocated 766,584,680 bytes and took about 2.47 seconds, about 3.46% of those two
allocation categories combined. Native execution therefore dominates this
measured allocation, rather than later display or analysis. Cumulative allocation
is not peak live memory, and these timings are not a controlled benchmark.

The final checkpoint/inspection measurements add 6.85-34.27 ms and 3.72-6.17 MB
per run. These are separate from the step/report measurements. The runner still
visits the supplied actors and uses supplied transfer calibrations. Two local
advertisement reads do not establish runtime scalability to a large population.

## Verification

- Release build: zero warnings or errors.
- Six prospective-offer check groups and ten subscription check groups passed.
  These cover future-only evidence, live publication dependence, uncertainty,
  expiry, paid cancellation, affordability, redundancy, local bounds, conservation,
  revival and native continuation; no check requires a campaign winner.
- The repository solution passed 1,752 tests.
- All 24 runs conserved currency and reconciled expense categories. None exceeded
  two advertisement reads per search or two active subscriptions.
- An independent CLI continuation audit matched 72 uninterrupted arrivals against
  40 plus a resumed 32, including the native graph, input generator and semantic
  totals. The checkpoint crossed two active contracts. Extra uncheckpointed report
  bytes were removed on resume; a report shorter than its checkpoint was rejected
  without padding or modification. Timing/allocation diagnostics are excluded
  from semantic equality.

## Next experiment suggested by the evidence

Keep the seeker itself as an explicit competing choice. Distinguish paying to
learn about a source from allowing its unproved output to replace a currently
better answer. Give adverse local evidence a way to suppress immediate repurchase,
while permitting bounded retrials when the tension or context changes. Test these
changes against the same controls and new held-out streams rather than selecting
only favorable cases.

That creates a smaller, clearer target for later attention or genetic training:
when to inspect, which offer to test, whether to use its output, how long to stay,
and when old evidence has become unhelpful. Native sites already expose horizon,
prospective weight, cancellation and renewal thresholds, capacity, prices, costs
and funding. Readable outcomes include improvement, harm, unresolved coverage,
utilization and expense. Their present settings are authored, not evolved.

There is no new Gojo kernel capability in this change. It demonstrates an
experiment composed from current capabilities, with supplied orchestration.
Automatic crystallization, private-internals sales, freely reusable learned
primitives, autonomous recipe growth and learned attention remain future work.

## Retained evidence

- [Comparison of all 24 runs](results/first-campaign/comparison.json)
- [Observer-only aggregation script](Summarize.ps1)
- [Harmful wandering run](results/first-campaign/wandering-47-h5-p1/summary.json)
- [Its sparse observation windows](results/first-campaign/wandering-47-h5-p1/windows.jsonl)
- [Mixed/47 with prospective evidence](results/first-campaign/mixed-47-h5-p1/summary.json)
- [Mixed/47 without prospective evidence](results/first-campaign/mixed-47-h5-p0/summary.json)

In report arrays, sources are ordered as duplicate continuation, sign reversal,
extrapolation and zero change. Context arrays put negative triggering error before
positive triggering error. Source coverage can include unresolved opportunities
without delivery, and a delivered forecast can still be pending. Phase windows
attribute an outcome to its arrival phase, including forecasts made just before
the boundary. These details matter when interpreting counts or phase totals.

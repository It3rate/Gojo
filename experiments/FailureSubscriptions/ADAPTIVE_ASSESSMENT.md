# Local reviews and adaptive duration: first comparison

Negative local evidence now changes shopping and actual output selection. Short
contracts can grow from observed usefulness, and a buyer can accept less than a
source offers. The longer comparison finds useful behavior, particularly from
keeping paid exploration separate from the actual answer. It also exposes a
remaining cost problem: periodic retries of consistently unhelpful sources.

The [protocol](ADAPTIVE_PROTOCOL.md) was first committed as `9770d16`; the completed
implementation and executable choices were frozen as `170aa68` before this
campaign. All 24 original runs are retained. There were no tuned replacement runs.

## Implemented mechanism

Each connection keeps four recent resolved outcomes for each supplied failure
context. Source-attached public reviews remain available when a buyer lacks its
own evidence. Own and public evidence are not added as independent votes. Both
retain the baseline against which improvement was measured; this is conditional
usefulness, not a universal quality rating. This fixture has one buyer and four
sources, so it does not demonstrate a crowd review market or population-wide
reputation discovery.

A finished term with newly resolved evidence and a nonpositive local review
postpones another purchase in that context by eight arrivals. A different context
can still be explored. The history is not erased when a retry becomes eligible.
Missing input does not add harm or renew a quality penalty by itself.

A paid source can produce a trial output while the seeker's own answer is used.
The self-prediction has zero marginal gain and wins nonpositive ties. Trial
payment alone supplies no evidence that the output should be trusted.

Each source also exposes eight positions of conditional future-usefulness
evidence. A supported prefix requires two resolved observations and positive
total relief at each position. It may offer one further exploratory position;
a nonpositive frontier has a retry delay. Offers distinguish supported and trial
extent. A request can shorten an offer, with a captured deadline and tariff.
Under the fixture's one-credit activation and two-credit delivery fee, two
opportunities quote five credits, while five quote eleven. Inspection is a
separate expense. Actual delivery fees are paid only when a calculation is
delivered, not for missing inputs or a canceled tail.

Focused checks establish accepting two of five, unchanged terms after a price
change, correct two-opportunity expiry, and no invented evidence for the other
three. The campaign uses requester cap eight against provider maximum eight, so
it does not independently exercise buyer shortening.

All reviews, quotes, intervals, publications, payments and comparisons are Gojo
structure. Host code supplies scheduling, recipes and the initial policy. There
are no new kernel changes, learned attention rules, copied private models or
permanently free learned primitives in this iteration.

## Longer comparison

There are 24 runs of 1,025 arrivals, totaling 24,600. Seeds 101 and 307 use the same
source order. Each policy receives the same stream and finite 1,048,576-credit
funding pool. Matched policies have equal resolved/unknown outcome coverage.
All currency, expense and subscription-opportunity accounting reconciles.
No run exceeds two observed local visits or two active subscriptions.

The table sums both seeds. Error is accumulated absolute error of the actual
precommitted export. Cost is system spending on work, inspection, activation
and cancellation; delivery fees are internal transfers. These are different
units, and lower is better for both.

| Policy | Recurring: error / cost | Wandering: error / cost | Unpredictable: error / cost |
|---|---:|---:|---:|
| Seeker alone | 3,298 / 2,026 | 1,894 / 2,016 | 6,134 / 2,016 |
| Original attention, fixed five | 3,463 / 7,482 | 2,019 / 10,152 | 5,636 / 10,535 |
| Guarded attention, fixed five | 1,978 / 5,465 | 1,895 / 8,141 | 5,786 / 8,300 |
| Guarded attention, adaptive 1-8 | 2,076 / 4,805 | 1,914 / 6,468 | 5,745 / 7,923 |

Guarded fixed terms reduced recurring-pattern error by about 40% versus the
seeker alone, while wandering error was essentially unchanged at a much higher
cost. On unpredictable input, the original policy achieved lower error than
either guarded policy but spent more. Improvement against the continuation
baseline does not establish prediction of independent future randomness: zero
change can be a better central estimate.

Across the six cases, adaptive terms cost 19,196 versus 21,906 for guarded fixed
terms, while error was slightly worse, 9,735 versus 9,659. This bundles adaptive
duration with a larger possible cap, eight instead of five. It is not an isolated
estimate of either change's causal effect, or a general accuracy improvement.

## What changed in shopping and duration

Guarded fixed terms recorded 2,503 visits suppressed by local retry evidence;
adaptive terms recorded 4,440. Those visits consumed the bounded local visit
budget without paying to inspect the source again. Zero marginal-gain duplicate
outputs were selected 1,324 times by original attention and zero times by either
guarded policy. The duplicate was still purchased: 1,314 deliveries with guarded
fixed terms and 809 with adaptive terms. Trust filtering improved more than trial
allocation.

Adaptive accepted durations were:

| Opportunities | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Terms | 2,805 | 292 | 128 | 119 | 87 | 33 | 53 | 105 |

One-opportunity terms make up 77.4% of the 3,622 contracts. There were 2,718 contracts
with no fully supported prefix: they were explicit trials. Larger terms did
emerge, so the system was not permanently trapped at its cold one-opportunity
offer. This is adaptation of commitment length from evidence, not discovery of
cycle phase or a calibrated probability of remaining correct.

Compared with guarded fixed terms, adaptive terms increased activations from
2,722 to 3,622 but reduced cancellations from 1,922 to 182 and abandoned opportunities
from 5,332 to 555. Paid shadow-delivery fees fell from 12,146 to 7,728. Shorter terms
therefore reduced wasted commitments while creating more frequent activations.

## Remaining weaknesses exposed by the runs

In the returning two-opportunity direction blocks, both recurring seeds purchased
44 one-opportunity trials: eleven neutral duplicate outputs and 33 harmful
alternatives. None was exported, and actual error matched the seeker alone.
Each case still spent 258 credits during that interval. Fixed retry delays limit
the frequency of futile exploration, but do not learn that repeated identical
trials are no longer worth buying.

Longer offers can also retain appeal after the input changes. In recurring/307,
the long-direction phase purchased sixteen seven/eight-opportunity terms,
canceled 17 contracts and abandoned 91 opportunities. Historical positive prefixes
are not current phase knowledge. Public duration history still accumulates under
only two error-sign contexts, and renewal retains the original triggering context.

Recovery was mixed. During the final returning alternation, recurring/307 had
adaptive error 28 versus guarded fixed error 44, with 92 inverse outputs selected.
Recurring/101 instead had adaptive error 106 versus guarded fixed error 30. These
are short exploratory comparisons with two seeds, not a stability guarantee.

The current utility rewards absolute-error relief. Direction agreement is a
reported diagnostic, not a separate reward. Prices, retry delay, the four-sample
review structure and provider prefix law are supplied. No negotiation learns a
new unit price, and no learner yet chooses the best utility definition or context.

The next useful attention target is the trial budget: longer waits after repeated
uninformative outcomes, renewed interest when context or an offer changes, and
spending that reflects expected improvement rather than perpetual eligibility.
Duration should eventually be conditioned on more informative structural state;
turning this ladder into a claim about remaining streak length would be premature.

## Operational resources and offline use

Final retained states contain 746-1,156 carriers and 4,547-9,885 relations. These
bounds are also the observed checkpoint-sampled peak range, not a measurement
of every transient graph. Current compressed checkpoints occupy 74,681-161,438
bytes. The larger base includes the explicitly retained review and duration
ladders; completed observation frames are still released.

Native steps allocated 84,237,429,192 bytes cumulatively and took 249.37 seconds
across 24,600 arrivals. Report aggregation/frame release allocated 3,285,404,656
bytes and took 11.16 seconds. These are allocation churn and instrumented wall
time, not peak live memory or a controlled benchmark; verification ran alongside
part of the campaign. Bounded retained structure does not yet mean inexpensive
execution. The funding pool never approached exhaustion; its smallest final
balance was 1,041,070 credits.

The [runner instructions](README.md) now cover configurable long runs, sparse or
aggregate-only reports, optional per-arrival profiling, finite funding and
checkpoint/resume. For a low-reporting individual run:

```powershell
dotnet experiments/FailureSubscriptions/bin/Release/net10.0/FailureSubscriptions.dll --output experiments/FailureSubscriptions/obj/long-local --scenario recurring --seed 401 --horizon 8 --steps 10000 --window 0 --checkpoint 1000 --profile 0
dotnet experiments/FailureSubscriptions/bin/Release/net10.0/FailureSubscriptions.dll --resume experiments/FailureSubscriptions/obj/long-local --steps 10000
```

The second command adds arrivals rather than setting a total. Resume freezes
the source/build/protocol and run settings. Original v1 campaign checkpoints
require their original implementation. A campaign interruption can be continued
per run; this is not yet a resumable population-training scheduler.

## Verification and retained evidence

- Release build: zero warnings and errors; all 30 focused groups passed.
- Repository solution: 1,752 tests passed.
- Frozen CLI audit: uninterrupted 160 arrivals matched 80+32+48, crossing two active
  contracts, populated local reviews and duration evidence at the middle save.
- Aggregate-only/profile-off 160 matched 112+48 and the ordinary run, including the
  native graph, input state and semantic totals. Only timing/allocation and
  checkpoint-cadence diagnostics were excluded. Report files stayed empty.
- Extra uncheckpointed report bytes were repaired. Truncated reports and nonempty
  output destinations were rejected without silently replacing existing work.

The [comparison table](results/adaptive-first-campaign/comparison.json), individual
summaries, sparse windows and current native continuations are retained. The
[aggregation script](SummarizeAdaptive.ps1) verifies a complete common-fingerprint
campaign before summarizing it. Accepted opportunities reconcile as observed
resolved/unknown opportunities plus canceled remainder plus current remainder;
unpurchased tails do not appear as observations.

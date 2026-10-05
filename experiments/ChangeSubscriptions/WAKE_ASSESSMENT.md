# Bounded wake trials: useful mechanism, weak selection rule

The mechanism works, but this campaign does not justify enabling it by default.
It bought useful predictions earlier after a sustained change and preserved all
prior negative evidence. It also bought extra bad predictions when two matching
coarse observations were followed by a reversal. Overall usefulness fell slightly
relative to escalating delays, while observation work increased.

The protocol was declared in `5c8e541`; the implementation and final accounting
clarifications were frozen in `7581514` before the campaign. No thresholds or
fixtures were tuned after examining the results. All changes are confined to
this experiment; no kernel or earlier experiment was changed.

## What was added

Each use can attach a separate coarse observer during its ordinary retry delay.
A changed cell confirmed at a second distinct observation grants at most two
early paid forecasts in that delay epoch. A new notification is not a successful
prediction. The actual later outcomes enter the existing four-outcome review.
The grant itself cannot change prior evidence, the penalty rung or the deadline.

The anchor, candidate, confirmation progress, allowance, epoch, accounting and
actual publication attachments are connected native Gojo structure. Current
sites are reused; there is no growing archive of successful input values.
The host schedules arrivals and local helpers. The two-confirmation rule, grain
four, two-forecast allowance and retry ladder remain supplied policy, not learned
structure or an autonomous event scheduler.

The cheap watcher disconnects when ordinary service becomes eligible. Unknown
input, return to the anchor or a skipped observer arrival cancels unused tokens.
Restoring validity alone cannot manufacture a new trial. Native transport retains
unfinished candidates, remaining tokens and pending paid forecasts.

## Matched results

Eighteen runs processed 6,135 arrivals. The table shows the amount use's total
benefit over its own zero-change reference. Positive means lower total error
than that reference; these are magnitude-loss units, not probabilities or money.

| Trace | Fixed delay 2 | Escalating to 32 | Escalating + wake | Early amount forecasts |
|---|---:|---:|---:|---:|
| Steady reversal | -192 | -46 | -46 | 0 |
| Fine variation and missing input | 483 | 483 | 483 | 0 |
| Usefulness returns | 502 | 374 | 388 | 4 |
| Unpredictable 113 | -404 | -117 | -122 | 12 |
| Unpredictable 271 | -334 | -111 | -121 | 13 |
| Coarse chatter and paired reversals | 20 | 112 | 97 | 4 |

When usefulness returned, the first early forecast arrived on the second new
observation, 21 observations before the ordinary escalating retry would have
started. All four early forecasts in that trace were helpful, contributing eight
units of benefit. Total benefit improved by fourteen because the added outcomes
also changed subsequent review timing. This remains modest recovery: the two
160-observation useful phases still skipped 50 and 48 opportunities, versus
52 and 52 without waking. Fixed delay skipped none during those phases but paid
for substantially more harmful service in the other phases.

Chatter exposed the limitation. One-observation coarse boundary alternation
produced no early forecasts. However, a cell can persist across a change in fine
scale, and paired reversals can confirm a direction just before it reverses.
The chatter trace's four early amount forecasts contributed **-22** benefit.
Its final useful phase recovered slightly better, but not enough to offset that
harm: the trace fell from 112 to 97 overall.

Across the six traces, the wake policy issued 54 early forecasts from 37 grants.
All 54 received later resolved outcomes; their direction benefit was -5 and
their amount benefit was -66. The two uses have different bases and should not
be added into one utility score. Missing early outcomes and canceled pending
tokens were exercised by focused checks; the campaign's missing-input trace
did not itself trigger an early trial.

| Across six traces | Fixed delay 2 | Escalating to 32 | Escalating + wake |
|---|---:|---:|---:|
| Total paid forecasts | 3,570 | 2,658 | 2,622 |
| Direction benefit | 1,356 | 1,370 | 1,346 |
| Amount benefit | 75 | 695 | 679 |

Fewer total purchases do not establish success. The amount use actually bought
19 more forecasts; direction participation accounted for the overall decline.
For example, extra early losses
on one unpredictable trace changed later delay timing and reduced participation.
The two random traces provide counterexamples to this wake heuristic, not
evidence that their finite positive scores elsewhere predict randomness.

## Work and retained structure

The wake runs performed 1,388 dormant revision checks, 499 change notifications,
108 initial synchronizations and 118 follow-up inspections. The steady-reversal
amount watcher alone made 264 revision checks and 12 synchronizations without
earning a single trial. This is a clear candidate for future attention pressure.

Revision-only polling does not inspect the selected coordinate on silence.
Token-gated permission checks do inspect current premises and are pure reads.
These activity counters are not an exhaustive processor-cost ledger. Native
source computation and publication settlement still run on every arrival.

The 78 retained-graph measurements remained bounded. Escalating controls had
208 carriers and 1,812-1,818 relations; wake worlds had 276 carriers and
2,198-2,208 relations. The extra 68 carriers and approximately 390 relations
belong to live policy, attachment and accounting structure, not per-sample copies.
Each final wake world encoded to about 87 KB before compression; the six ZIPs
totaled 164,442 bytes.

For the same 2,045 arrivals per policy, cumulative native allocation rose from
4.08 GB for escalating controls to 5.50 GB with waking. Native scheduled work
took about 5.1 versus 8.0 seconds in this run. Reporting allocated another
2.16 versus 4.14 GB and took about 2.5 versus 5.3 seconds. These are cumulative
allocation and sequential wall-time observations, not peak memory or a controlled
performance benchmark. This implementation demonstrates no computation saving.

Before the audit report, retained campaign artifacts totaled about 6.0 MB, of
which 5.16 MB was optional per-arrival diagnostic JSON. Those reports are never
read by actors and are not the system's working memory.

## Verification and interpretation

- 31 focused check groups passed, including all 21 original groups.
- `dotnet test Gojo.slnx` passed all 1,752 tests.
- The independent host-side auditor made 236,828 comparisons across all raw
  rows. All paid and early ledgers reconcile as resolved, unknown or pending.
- It verified causal frozen forecasts, loss arithmetic, one grant and at most
  two issues per epoch, physical watcher lifecycle, and early eligibility.
- Replaying reviews from actual outcomes verified that permission itself never
  changes the review block, rung or deadline.
- All ten repeated original controls matched the previous campaign's final,
  phase and per-arrival semantics. Native checks also verified independent
  continuation after transport, including a live partially consumed grant.

The next useful pressure is on the **wake route's own subsequent usefulness**.
Currently a route can repeatedly buy bad early predictions whenever a fresh
delay epoch permits it; ordinary source ratings react, but the route does not
learn that its change cue is unhelpful. Its native early-outcome counters provide
a starting point for slow strengthening or fading, with some exploration left
available. That should also make a quiet, repeatedly unproductive watcher costly
to maintain. This remains a proposed next experiment, not an implemented claim.

I would retain this as an optional bounded exploration mechanism. The evidence
supports its boundedness and inspectability as a policy, and shows that coarse
stability alone is insufficient to select worthwhile retries. It does not yet
support a larger market, learned attention or a claim of autonomous adaptation.

Evidence: [protocol](WAKE_PROTOCOL.md), [run manifest](results/wake-campaign/manifest.json),
[comparison](results/wake-campaign/comparison.json), [audit](results/wake-campaign/audit-result.json).
Run `python experiments/ChangeSubscriptions/audit_wake_campaign.py` from the
repository to repeat the diagnostic audit. That script independently checks the
native results; it never supplies actor decisions or executes the campaign.

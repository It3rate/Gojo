# First change-subscription campaign

The experiment gives useful evidence for separate utility at separate reads and
for keeping current service independent of change notifications. Increasing
retry resistance limits repeated harm, but substantially postpones renewed
usefulness. It is not uniformly better than a short fixed delay.

Protocol commit: `c85ca7a`. Implementation frozen before campaign: `f099801`.
The retained [comparison](results/first-campaign/comparison.json) contains all
15 runs and 4,788 arrivals. No outcome was discarded, no policy was tuned after
inspection, and no campaign was rerun. The [manifest](results/first-campaign/manifest.json)
records the exact source/build and protocol fingerprints.

## Separate uses of one source

On the steady/reversing trace, all policies kept the direction connection:
351 resolved subscribed outcomes, 285 helpful, two harmful and 64 neutral.
Its direction error was 5 versus the zero-change reference's 288. There were
352 paid forecasts including one pending at the end, but only four change
notifications and one initial synchronization.

The amount read of that SAME source was harmful. Its policy changed separately:

| Amount use, steady/reversing trace | Unthrottled | Fixed delay 2 | Escalating to 32 |
|---|---:|---:|---:|
| Paid uses | 352 | 256 | 76 |
| Helpful resolved outcomes | 0 | 0 | 0 |
| Harmful resolved outcomes | 287 | 192 | 44 |
| Actual amount error | 579 | 480 | 334 |
| Reference error | 288 | 288 | 288 |
| Net predictive improvement | -291 | -192 | -46 |

Thus the source was neither globally accepted nor globally condemned. Escalating
resistance saved many harmful uses without pruning useful direction. It still
made harmful trials and remained worse than its amount reference. That retained
exploration is visible, rather than presented as success merely because it is
less harmful than the control.

The reference is intentionally simple. A consumer already tracking the latest
direction would find this direction outlet redundant. These results demonstrate
partial utility relative to a declared local reference, not uniquely valuable
prediction, learned primitives, or a competitive multi-source market.

## Changes, stable service and interrupted input

On `grain-gap`, all three policies behaved identically. The direction surface
published three revisions, the fine amount surface 209, and the four-tick coarse
surface four. Fine fluctuations therefore did not force coarse notifications.
The focused checks additionally verify the exact retained source residue:
reading five at grain four gives one target tick and one remaining source tick;
reading seven gives one target tick and three remaining source ticks.

Each consumer recorded 206 resolved outcomes, one missing paid outcome and one
pending forecast, from 208 paid uses. Six explicit missing arrivals did not become
six incorrect predictions. Missing input revoked current readability while the
held value remained inspectable. The first returning input could not bridge the
gap; a second consecutive input restored a next-observation forecast.

The amount reader stayed connected despite 23 harmful outcomes, because its
blocks were useful overall: 128 helpful, 55 neutral, total improvement 483.
An isolated mistake therefore did not automatically create a retry penalty.

Notifications are current-state synchronization. The host checks each arrival in
this campaign; the helper can coalesce unobserved revisions and does not promise
delivery of every intermediate transition. Its `Validations` diagnostic counts
arrival/read attempts, including unavailable input, not verified truths. Paid,
resolved and unknown outcomes are separate counters.

## The cost of increasing resistance

`applicability-return` changes from small to larger steps without changing the
predictor. The amount outlet becomes useful, then loses and regains usefulness:

| Amount use, changing applicability | Unthrottled | Fixed delay 2 | Escalating to 32 |
|---|---:|---:|---:|
| Paid uses | 512 | 448 | 264 |
| Actual amount error | 1,026 | 970 | 1,098 |
| Net predictive improvement | 446 | 502 | 374 |

Escalating resistance eventually fell from rung five back to zero in each useful
phase, but skipped 52 uses in EACH 160-arrival useful phase. Its lower work counter
therefore came with worse prediction than both other policies. Even after a good
block, resistance decreases gradually and the connection pauses again until its
shorter boundary. The recovery delay is a real cost of the supplied rule.

The unpredictable fixtures provide a less favorable prediction environment:
amount improvement remained negative for every policy. Escalation reduced it
from -618 to -117 for seed113, and from -510 to -111 for seed271. Direction
improvements were small and mixed across policies: (-5,-11,1) and (19,18,20).
Two finite deterministic traces provide no evidence of predicting randomness.

Across all five traces, paid uses were 3,168 / 2,780 / 2,078 for unthrottled /
fixed2 / escalating32. This comparison counts modeled access units, not economic
profit or actual work saved. Direction loss and amount loss have different bases
and are not added into one global accuracy score.

## Representation, resource use and verification

The retained graphs ranged from 200 to 208 carriers and 1,748 to 1,818 relations.
They stayed bounded at phase boundaries; the smaller fixed-delay graph has a
shorter native ladder. Focused checks also compare the reusable operational graph
after 80 and 240 observations. Current receipts and optional captured witnesses
are distinct from a growing archive of successful values.

Allocation remains heavy. Across the 4,788 arrivals, scheduled native work
allocated 10,069,047,680 bytes cumulatively and the per-arrival report path
allocated another 4,628,891,912 bytes. Recorded wall times were about 15.65 and
6.44 seconds respectively. These are exploratory measurements, not controlled
benchmarks or peak-memory figures. Fewer notifications have not removed the
per-arrival source calculations, observer checks and temporary witness creation.

The final native transports were 67,431–70,188 bytes before ZIP compression.
The full campaign artifacts occupy about 2.9 MB, mostly optional JSON arrival
reports; the 15 compressed native worlds total 330,960 bytes. This is a bounded
working representation with expensive temporary calculation and inspection,
not an ever-growing retained prediction database.

Verification passed:

- Release build without warnings; 21 focused check groups.
- All 1,752 repository solution tests.
- Separate ratings, negative/neutral/missing distinctions, aggregate tolerance
  of occasional errors, exact coarse residue and physical detach/reconnect.
- Frozen prediction causality, same-arrival/query idempotence, stale shared
  envelope rejection, independent native continuation, and gap recovery with
  a partial evidence block.
- All 30 final use ledgers reconcile paid = evaluated + unknown + pending,
  evaluated = helpful + harmful + neutral, and benefit = reference error - error.
- Matched policies have identical external opportunities, reference losses and
  public source revision counts. Reports never feed policy decisions.
- An independent read-only audit of all 4,788 arrival records also reconciled
  consecutive-input deltas, causal prior forecasts, losses and summary totals.

All additions are in this native experiment project. No Gojo kernel or earlier
experiment changed. The connected structures contain the meaning, but the source
recipe, chosen reads, comparator and retry law are supplied, and the host still
schedules their application. Complete silence is not automatically diagnosed;
input availability is an explicit fixture event. Direct callers of the retry
helper must not replay the same paid receipt as new evidence.

## Recommended next step

Keep the separate live read, validity and notification surfaces. The clearest
missing mechanism is a cheap, relevant change signal that can authorize a small
earlier probe of a dormant use without erasing its negative history. Compare that
with the retained delay controls before making the retry rule more elaborate.

Costly neutral service also remains connected indefinitely once eligible; it is
accurate redundancy rather than misinformation. A later shared exploration/work
budget or consumer-cost comparison should handle that separately. After these
contracts prove reusable, move the common change/read mechanism into an active
Gojo structures surface and reduce observer allocation without losing the
inspectable basis, validity, residue and causal promise.

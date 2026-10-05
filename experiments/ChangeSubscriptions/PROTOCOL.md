# Change subscriptions: declared small experiment

This protocol is written before inspecting campaign outcomes. Preserve poor
outcomes as evidence. This is a separate experiment; it does not replace earlier
market campaigns or establish a general asynchronous kernel scheduler.

## Question and structural construction

Can different readers use one live prediction differently, retain useful stable
service without duplicate notifications, and reduce repeated harmful trials
without demanding perfect predictions or permanently excluding a source?

The reusable native source structure is:

```
two consecutive admitted levels -> delta
delta -> clamp[-1,1] -> direction
delta + 2*direction -> next-delta prediction
prediction -> identity read -> amount subscriber
prediction -> clamp[-1,1] -> direction subscriber
prediction -> projection at four source ticks per tick -> coarse observer
```

The predictor is deliberately supplied and imperfect. It follows the latest
direction but exaggerates change by two ticks. It can be useful in one selected
read and harmful in another. Neither its recipe nor the read perspectives are
learned. Source validity requires two consecutive available inputs; observations
across a missing interval are not joined as a single-step delta.

Each consumer has its own native connection, frozen pending prediction, zero
change reference, signed outcome evidence, partial block, retry rung and retry
boundary. A source's bad amount estimate cannot penalize its direction use.
Native ordered structure connects these participants. Host code only admits
fixture events, schedules local helper applications and inspects results.

Every next observation tests the prediction captured BEFORE that observation.
Magnitude loss is absolute signed-delta error. Direction loss is distance between
clamped integer directions: 0 for agreement, 1 between flat and moving, 2 for
opposition. This is an authored ordered loss, NOT classification accuracy or a
fair-coin skill score. Both consumers compare with the zero-change prediction
through their own read. Conditional benefit = reference loss - supplied loss.

## Notification and observation contracts

Publication remains readable between notifications. A finite publication event
occurs when the selected visible coordinate or its validity changes. A fine
remainder remains inspectable even when a coarse value stays unchanged. Each
source arrival has a local order; this is not a synchronized wall-clock model.

Subscribers retain actual native publication relations across detach/reattach.
Reconnect obtains an explicit current synchronization, not the full event history
missed while detached. The coarse observer remains connected for the whole run.
An initial synchronization is reported separately from a change notification.
An observer that does not inspect several revisions receives the latest state
and one notification when it next checks; this is not a lossless every-transition
subscription. The retained one-event trace deliberately does not promise that.

Reading an unchanged value neither creates a notification nor generates new
prediction evidence. Provider arrivals/validations, consumer uses, notifications,
initial synchronizations, resolved outcomes, missing outcomes and paid trials are
reported separately. Missingness here is an explicit input-availability event;
complete silence alone cannot establish failure or freshness. Source interpretation
still runs on each supplied arrival, so fewer notifications do not establish lower
total CPU cost or an autonomous event scheduler.

## Retry rule and supplied costs

The compared policies are unthrottled, capped delay 2, and escalating delay up to
32. All use the same provider, incoming observations and notification mechanism.
Each completed block has four resolved subscribed outcomes. A negative aggregate
benefit increases the rung by one; a positive aggregate decreases it by one; an
exact tie leaves resistance and its existing deadline unchanged. Unknown outcomes
do not enter the block. The enclosing arrival scheduler admits each pending
outcome once and rejects stale shared envelopes; the retry helper is not
independently idempotent against replayed paid receipts. The delay ladder is
native visible structure:

```
rung:   0  1  2  3  4  5
delay:  0  2  4  8 16 32
```

Retry progress counts available consecutive-input validation opportunities, not
notifications, queries, or explicit missing arrivals. It is a declared local
progress measure. Delay is applied after a completed nonzero block, and a
detached source can be tried again after that boundary. No oracle resets are
allowed when the fixture changes regime.

Each issued subscribed prediction records one modeled access/work unit, whether
or not a future outcome becomes available. This is a cost counter, not a full
currency market or measured CPU cost. Ties can incur cost without being labeled
harmful. This small experiment does not implement a shared exploration budget,
price negotiation, automatic primitive discovery, or learned timing.

## Fixed fixtures

Every fixture begins at level 10000. Phase names exist only in the fixture/report
adapter and are never available to the native actors.

1. **steady-reversal:** 96 increments of +1, 96 of -1, 64 of zero, 96 of +1.
2. **grain-gap:** 96 increments cycling +3,+4,+3,+5; 48 cycling +6,+7;
   six explicit missing arrivals; resume at the unchanged last level;
   64 increments cycling +3,+4. Tests fine variation, coarse cells, boundary
   crossing, validity loss and resumed support.
3. **applicability-return:** 96 increments +1, 160 of +4, 96 of -1,
   160 of -4. The same unchanged predictor becomes more useful for amount;
   the later regime is long enough to expose delayed retry/recovery.
4. **unpredictable:** two deterministic 256-increment streams, seeds 113 and 271,
   with signed step magnitudes 1 or 4. No predictive skill is presumed.

The three policies run against all five traces: 15 runs. Record phase summaries,
per-arrival diagnostic projections, and the final restorable native structure.
Keep diagnostics outside the actor input path. Run focused semantic checks and
the repository solution tests before freezing source and running the campaign.

## Required evidence and limitations

Check separate use ratings, aggregate tolerance of occasional errors, neutral
versus missing outcomes, increased retry intervals, eventual eligibility and
recovery, frozen prediction causality, live validity, no notification from reread,
coarse remainder, actual attach/detach, independent native transport continuation,
and bounded retained operational structure. No campaign metric is required to win.

Report total and phase benefit/error, paid uses, notifications, initial syncs,
unknown outcomes, penalty rungs/delays, connection changes, native graph size and
allocation/runtime separately from report work. Longer delay may save harmful
uses while delaying renewed usefulness. That tradeoff is a result, not a bug to
hide by tuning after inspection.

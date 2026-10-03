# Shared timing ladder: fixed exploratory protocol

Written before the first measurements. This extends the experiment, not the
kernel's settlement law. No general attention scheduler or sampler evolution
is claimed.

```text
source-port = group(public clock, public value axis, acquisition-stride span)
report = group(port, covered span, availability, occurrences or open slot,
               exposed endpoint value, residue)
occurrence = directed point span on the public clock
ladder = group(clock, group(explicit clock-to-rung correspondences))
forecast = group(issued-at, predicted-at, shared source evidence, rung reads)
rung-read = group(correspondence, forecast cell, projection witness,
                 structural rung with source/focus/result/residue)
pending = group(forecast, frozen demanded resolution, selected rung,
                native cadence calculation)
learner = group(port, ladder, last two public occurrences, pending,
                availability, support, demand, policy, latest assessment,
                latest report, admission cursor, cursor correspondence)
```

Two independent reporters receive the same environment. The first observes
every base tick, detects strict reversal of adjacent differences through native
equations, and publishes the accumulated **fine detector occurrence times**
every twelve ticks. This is delayed delivery of a fine payload, not a coarse
payload with hidden access to private samples. An occurrence is the detector's
emission at an observed tick, not an exact physical inflection between samples.
The second acquires only every twelve ticks. Its endpoint and private reversal
clues cannot certify which fine events occurred between samples; its public
fine occurrence collection remains open. A flat endpoint is not zero events.
The first fine report retains the detector's initial missing-difference
residue. This conservative learner does not bootstrap from that partial batch;
its known occurrences remain in the public report for inspection.

Acquisition stride and report span are explicit native spans. Pending source
events are bounded by one report window; an overrun must not silently discard
events. Public reports cannot reach the producer's private input or trace.
The learner receives only the public report, never the environment's cadence,
random seed, level-change schedule, condition name or future input.

After a complete public report, the last two observed detector occurrences
propose a recurrence interval. A native quotient selects the next predicted
occurrence strictly after current availability. No forecast is issued inside
an already received batch. The first later observed occurrence assesses the
pending forecast before that batch can teach the next one. Other occurrences
can update the two-site trace but receive no retrospectively created forecasts.

Default rung widths are 1, 3 and 12 base ticks, with a shared origin and explicit
affine correspondences. Project the common forecast into each rung's half-open
cell [lower,upper). Preserve projection residue and the cell's full directed
basis. An actual occurrence must also be strictly after issuance, so a cell
extending into the past cannot earn retrospective credit. All rungs share one
source forecast and occurrence; agreement across rungs is not independent
evidence. The frame grammar also permits nonnested widths such as 13 and 21;
this pass does not search for preferred ratios, offsets or prime strides.

Freeze one demanded rung (default width 3) in each pending issue. Its native
agreement changes a single shared support carrier by +1 or -1, bounded in
[0,8]. Rejected saturation remains inspectable without a hidden tension bank.
Other rung matches and signed early/late errors are diagnostic reads. Support
is a supplied utility trace, not confidence or a participation threshold.
All issued forecasts are exploratory candidates in this pass.

A complete empty occurrence group establishes absence only over its explicit
covered span. If no occurrence arrives before the largest forecast cell has
closed under complete contiguous coverage, assess one miss. Until then keep
the bounded issue pending. An unknown collection closes it as unassessable,
preserves unresolved evidence and clears the cadence trace: hidden cycles must
not be interpreted as one measured interval. Unknown reports do not change
support. Reports must be sequential and contiguous; duplicate, foreign and
out-of-order reports remain failed reads without changing the learner.
The copied availability is a root-version witness against the shared physical
admission cursor. Reusing an older root cannot repeat support or admit a newer
report through stale history. Unsupported demands remain unresolved and can
be retried when later demand supplies a readable rung.

Compare fixed-period, jittered-period, drifting-period and stopped-event inputs.
Use three fixed seeds (113, 907, 2029), 360 base ticks per run and twelve-tick
reports. Use the same policy throughout. Report acquisition/report counts,
public multiplicity, issued/assessed/unassessable forecasts, each rung's hits,
signed timing residue, shared support, and reachable graph sizes. The regular
and stopped conditions may be deterministic duplicates across seeds; label
them as such. Report wider-window agreement as a different task, not an
accuracy win over a narrower promise. Do not tune the policy after results.

Test pure frozen reads; common evidence across rungs; nonnested maps; exact
boundary behavior; once-only support; no retroactive batch predictions;
zero versus unknown; absence expiry; demand changes; public/private isolation;
preserved event multiplicity; bounded retention; and independent checkpoint
continuation. Leave general read pruning and adaptive sampling for a later
experiment, with this pass exposing the work that those mechanisms could save.

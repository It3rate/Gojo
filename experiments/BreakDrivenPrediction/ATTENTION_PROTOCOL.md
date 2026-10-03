# Demand-directed timing reads: fixed exploratory protocol

Written before implementation and measurements for this pass. This is a supplied
local read policy over the existing public event interface, not a learned global
attention law. Keep the earlier timing experiment unchanged as a reference.

## Native structure

Extend the bounded timing learner with a connected attention relation. Its parts
link the actual demand, support, report admission cursor and rung maps to a
bounded issue-phase carrier, a probe interval span, a dormant/available gate and
the latest selection/wake evidence. The exact positional grammar will be
documented alongside the implementation. The native relations, carrier amounts,
claims and transfers are the state; no host model or semantic enum owns it.

Each new forecast uses the same two-occurrence native cadence calculation as
before. Before constructing rung reads, traverse the available correspondences
and select the consumer's requested width. Width zero withdraws new forecasts.
Unsupported positive widths remain unresolved. Freeze demand with each issued
promise; changing demand, including withdrawing it, cannot erase an outstanding
promise or change its grading resolution.

Ordinarily construct only the demanded rung. On every fourth eligible issue,
and immediately after a resolved miss, also construct the nearest available
finer rung, if one exists. All reads share the same frozen forecast and evidence.
The extra rung is a diagnostic probe and earns no separate support credit.
Probe cadence is fixed at four issues throughout these trials. Selection uses
native width comparisons; widths need not form a nested partition. A probe
cannot extend the demanded promise's expiry or hold its verdict unresolved.
Probe results remain inspectable but do not yet learn which width to demand.

Grade the outstanding promise before learning from an incoming batch. Apply
its single bounded support change before deciding whether to issue again. If a
complete-coverage absence miss leaves support at zero, suspend new forecasts
until a fresh public occurrence arrives. Retain the bounded two-event trace and
rung maps while dormant. This rule is an experimental alternative to repeatedly
forecasting from exhausted evidence, not a rule that silence destroys memory.
Demand returning from zero re-enables work only subject to this event gate.
On wake, a new occurrence does not turn the silent interval into one measured
period. Restart the two-site learning trace with that occurrence; require two
fresh occurrences before issuing again. Old cadence stays inspectable during
dormancy, but this conservative pass does not extrapolate its phase across the
gap. Failure-triggered probe demand persists until an eligible issue uses it.

No report means no state transition. Unknown event coverage cannot establish
absence, remove support or masquerade as a complete empty observation. As in
the preceding experiment, an unknown report makes the pending issue unassessable
and clears the cadence trace because unobserved cycles cannot be bridged as one
measured interval. A complete empty reversal collection establishes only that
this detector emitted no reversal over its covered span: monotonic motion can
also emit none. The predictor receives no privileged flat/moving label.

Keep physical admission/version checks, pure reads, frozen issue witnesses,
bounded retention and independent numeric checkpoint continuation. Attention
state changes and support/cursor changes must not leave partially admitted
reports or permit stale-root replay.

## Fixed trials

Use 720 base ticks, acquisition every tick, reports every twelve ticks and rung
widths 1, 3 and 12. Seeds are 113, 907 and 2029. Only jitter uses randomness;
other seed-labelled runs are deterministic repetitions, not independent evidence.
The learner never receives the generator's labels, schedule, seed or future.

- Regular: alternating increases/decreases, five ticks per run.
- Jitter: independently supplied run lengths from four through six ticks.
- Intermittent: 120 moving ticks followed by 120 flat ticks, repeating; each
  moving block restarts a five-tick run.
- Changed cadence: five-tick runs through tick 180, flat through tick 360,
  eight-tick runs through tick 540, then four-tick runs through tick 720; restart
  a run at each phase boundary.

Consumer demand is width 3 through tick 240, zero at 241-360, width 12 at
361-480, width 1 at 481-600, then width 3 at 601-720. Apply demand before each
report is consumed. This deliberately includes changed timing, missing utility,
and a fine read during inactivity. It is not tuned after observing results.

Both learners receive exactly the same public packet, on the same source port,
with independent admission/support/control carriers. Compare the new policy to
the existing eager learner that constructs all three rungs. For the eager
reference, keep its last positive demand during consumer withdrawal and mark
that work unused; this is a cost reference, not a fictitious zero-width promise.
Report frozen promised widths separately from current demand at grading time.

Count source acquisitions, issued cells, assessed event-cell rows, forecasts,
settled hits/misses, absence misses, unused new issues, probes, dormant reports
and reactivations, plus sampled reachable graph sizes. These are structural work
proxies, not elapsed time, total native operation counts or proof of net savings.
Selection and attention witnesses themselves cost work. Source acquisition stays
fixed; this pass must not claim sensor savings or sampler evolution.

Include report-window event-presence and event-absence reference scores derived
from complete public reports. These deliberately simple host reporting baselines
measure an easier question than exact timing and must be labelled accordingly.
Intermittent observations prevent a broad next-window promise from appearing
informative solely because events occur in every report. Summarize performance
by frozen demanded width; do not pool different promises as one accuracy score.

## Checks and limits

Test actual selected-rung construction, one reward despite probes, periodic and
failure-triggered probes, frozen demand, withdrawal with an outstanding promise,
absence expiry independent of unused/probe rungs, dormant retention and wake,
unknown coverage, pure reads, stale roots, independent checkpoints and bounded
retention. Include nonnested rung widths in expiry/selection checks.

This pass supplies primitive detection, cadence, widths, demand, probe schedule
and dormancy policy. It can reveal whether skipping reads is promising and where
it causes delayed recovery. It does not discover primitives, learn consumer
utility, remove unused maps permanently, optimize acquisition, or prove the
best treatment of biological or conceptual memory.

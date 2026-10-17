# Remembered cadence and current applicability

This pass separates three local reads: what interval the structure has learned,
whether its continuation still fits the observed input, and whether a consumer
currently wants its output. They share connected evidence without becoming the
same support score or gate.

The [fixed protocol](CADENCE_PROTOCOL.md) preceded implementation and trials.
The new `--cadence` experiment compares a scoped reader with the preceding
attentive reader. Earlier commands and saved measurements remain intact.

## Native structure and behavior

The existing learner gains a cadence scope connecting its public source port,
allowed timing slack, fresh trace, remembered interval candidate, current
continuation window, availability and latest assessment. Scope and learner share
one trace relation and one availability site. The candidate retains two frozen
event grounds, the native difference equation and its read. A continuation links
that candidate to its expected next coordinate, allowed window and native
arithmetic witnesses. Removing the builder leaves this structure traversable.

The supplied window extends two public clock ticks before and after the next
expected event. This is an acceptance policy for the local hypothesis. It is
neither a measurement uncertainty estimate nor the consumer's resolution. The
choice is fixed across the trials, including inputs that it handles poorly.

Each complete batch is processed chronologically. The active window is checked
before an arriving event becomes a new ground. An incompatible event opens the
old continuation and starts a new trace from that event; two fresh events are
needed for a replacement. After the events, complete empty coverage through
the upper endpoint also opens continuation. An event exactly at either closed
endpoint is accepted before checking the remaining empty tail.

The remembered interval remains available when its phase becomes ungrounded.
The absence of a grounded phase blocks new forecasts even when historical
support is high. Conversely, withdrawing demand does not disable the local
applicability check. Unknown or partial coverage opens phase without asserting
absence or subtracting support. No delivered observation makes no transition.

Outstanding predictions retain their frozen demanded rungs and are graded first.
The scope cannot erase a promise or change its resolution. A broad promise may
succeed while the narrower applicability window breaks; these are different
claims. Scope comparisons within a delivered batch are retrospective consistency
checks and earn no prediction credit. New forecasts still precede their outcomes.

New output uses the current expected center only while it is strictly future.
If that center has passed but its allowed window remains open, the reader waits;
it cannot skip an unresolved cycle by extrapolating another future point. The
preceding attentive reader retains its quotient-based behavior as the reference.

The source interface still reports detector reversals, not the meaning of light,
flatness or a physical inflection. Monotonic input can produce the same empty
reversal collection as flat input. Acquisition remains fixed. The host supplies
the environment, report schedule, demand and measurement apparatus; native
equations, claims, connected traces and witnesses carry the model's reasoning.

## What remains supplied

This is a local applicability mechanism, not learned attention or general change
detection. The two-tick allowance, last-interval hypothesis and two-fresh-event
restart are authored rules. A new longer period can legitimately break the old
window. A lone event supplies no interval by itself. Remembering an old candidate
does not yet allow active reuse, competition or composition with another model.

Extra checks cost work even during output withdrawal. Allocation measurements
surround learner admission only; they exclude learner construction, source
processing, demand assignment and report inspection. Counts of withheld
predictions must be split into omitted hits and misses: withholding an answer
is not successful prediction of absence.

## First fixed observations

The [full measurements](results/cadence/first-run.md),
[per-report reads](results/cadence/traces.csv),
[paired forecasts](results/cadence/paired-issues.csv),
[boundary reads](results/cadence/boundaries.csv) and
[retained graph measurements](results/cadence/graph-sizes.csv) preserve the first
run. Each condition supplies 720 fine acquisitions and 60 reports to both readers.
Only jitter varies across the three seeds; the other repeated runs are duplicates,
not additional independent evidence.

| Input | Scoped hit / miss | Reference hit / miss | Reference hits / misses withheld |
| --- | --- | --- | --- |
| Regular | 48 / 0 | 48 / 0 | 0 / 0 |
| Jitter, three seeds combined | 87 / 44 | 88 / 56 | 1 / 12 |
| Stop and restart, then faster cadence | 40 / 1 | 41 / 7 | 1 / 6 |
| Period doubling | 38 / 0 | 40 / 0 | 2 / 0 |
| Long period | 27 / 0 | 27 / 0 | 0 / 0 |
| Alternating 5/13 intervals | 0 / 17 | 4 / 29 | 4 / 12 |
| Unknown coverage | 47 / 0 | 47 / 0 | 0 / 0 |

Rows other than jitter show one deterministic run. Both readers end with one
pending forecast per run except alternating intervals, which has none. Unknown
coverage also leaves one unassessable forecast per reader, excluded from hits and
misses. Across every condition there were no extra scoped forecast identities and
no outcome differences on shared forecasts. This pass changes when the reader
answers; it does not produce better replacement answers.

The restart trace demonstrates the intended separation. At report 192 the
expected event at 181 has failed. The scoped reader retains interval 5 but opens
its current continuation and stops issuing; historical support remains 7. It
neither has to forget the interval nor exhaust support to stop claiming it still
applies. When an event arrives at 369, reported at 372, that event alone leaves
phase ungrounded. The reference joins old event 176 to 369 and predicts 562. The
scoped reader waits for event 377; at report 384 it has two fresh grounds, learns
interval 8 and predicts 385. Its six omitted reference misses comprise five
additional absence misses and the spurious 562 forecast.

The same conservative rule has a cost when cadence legitimately changes. At
report 552 the old eight-tick continuation accepts event 545, then event 549
breaks its window. Requiring another fresh ground withholds the reference's
correct exact-tick prediction of 553. Period doubling loses two correct forecasts
while waiting through a past expected center and then grounding the new interval.
These are useful missed opportunities, not evidence that the input is wrong.

Jitter causes no event-window or absence-window breaks in any seed: consecutive
intervals from four to six all fit the supplied allowance. Its omissions instead
come from the stricter future-center rule. The reference sometimes jumps past an
unresolved cycle to another future point; the scoped reader waits. This removes
twelve misses but also one successful width-12 forecast. In that lost case,
report 420 has expected center 420 and a still-open window [418,422]; refusing to
guess another cycle withholds the reference's successful coarse target 424. This
does not demonstrate learned uncertainty, improved interval estimation or a
universally preferable gate.

Long-period input has many empty reports yet no applicability breaks. Unknown
coverage produces five masked-report opens plus the initial incomplete report,
no absence breaks, and no absence penalty. A complete empty interval, an unknown
interval and no new report therefore have distinct behavior.

Alternating intervals remain a clear failure. The scoped reader reports 19 event
breaks and 20 absence breaks per run, makes 17 unsuccessful forecasts and learns
no composite pattern. The reference's four successes are all at width 12; they
are valid coarse promises, even though it also fails to represent the alternation.
The scoped reader withholds those useful coarse answers as well. Its narrower
local applicability rule and the consumer's requested resolution answer different
questions; choosing the former as a gate is an authored policy with this cost.

The repeated breaks expose a more specific weakness than missing composition:
the fresh-trace reset prevents the long 13-tick interval from becoming a candidate.
The learner repeatedly builds interval 5, then treats the following long gap as
a reason to start over. A failed recurrence does not invalidate its observations.
Those grounds survive in the latest break witness, but this policy does not yet
use them to construct an alternative. Similarly, the observed pair 545,549 can
legitimately ground interval 4 even though it disproves continuing interval 8.

## Work and retained structure

Managed allocations increased on every condition except alternating intervals:
approximately 39% on regular input, 31-33% on jitter, 16% on restart, 20% on
doubling, 11% on long periods and 35% with unknown coverage. Alternating input
allocated about 14% less while producing no successful answers. Fewer issued
cells therefore do not establish an efficiency improvement.

Even regular input with zero output demand allocated about 6.36 MB in the scoped
reader versus 2.79 MB in the reference. Applicability continues to cost work when
no consumer asks for a forecast. That is deliberate in this experiment, but a
future pressure/attention policy must account for it explicitly. The fixed source
acquisition cost is unchanged and excluded from these measurements.

The retained graphs vary with the latest batch, pending promise and selected
reads. For example, the regular scoped reader ranges from 263-549 carriers and
2,382-5,039 relations at the six sampled boundaries, versus 107-475 carriers and
911-4,308 relations in the reference. These finite samples are consistent with
the retention tests; they are not a proof of a universal bound or evidence that
the representation is small. Latest-batch evidence and arithmetic construction
remain substantial costs despite the absence of an accumulated sample archive.

## Next experiment suggested by the gaps

Expose the local break witness through an independent output connection and let
another small continuation use it to propose a subsequent event. A reusable
interval structure should be able to condition on another structure's output;
alternating 5/13 should not need a named alternating-pattern special case. Retained
interval candidates could participate in that connection without automatically
recovering their old phase or inheriting predictive credit.

In particular, retain the distinction between rejecting a model's continuation
and rejecting its observed grounds. A competing structure can use a complete,
observed transition that broke the previous recurrence; it must not fabricate a
transition across unknown coverage. The existing bounded break witness connects
the old continuation and new arrival, providing a place to read that residual.

Keep a forecast frozen before the next event, compare its added utility at the
consumer's requested resolution, and withdraw connections that add no useful
prediction. Challenge the composition with alternating intervals and a shuffled
stream containing the same interval values, as well as jitter and legitimate
cadence changes. That would distinguish useful relational ordering from merely
remembering an interval vocabulary. The current pass supplies the inspectable
break and candidate surfaces; it does not yet implement this recruitment.

## Reproduction and verification

```powershell
dotnet test Tests/BreakDrivenPrediction.Tests/BreakDrivenPrediction.Tests.csproj
dotnet run --project experiments/BreakDrivenPrediction/BreakDrivenPrediction.csproj -- --cadence
```

The experiment suite passes 110 tests, including 21 new cases for closed timing
bounds, unknown coverage, demand-independent applicability, frozen promises,
pure reads, partitioned reports, failure atomicity, native checkpoint replay and
bounded retention. The initial milestone verified the existing 1,732 solution
tests with `dotnet test Gojo.slnx --no-restore`; the missing `wasm-tools` workload
prevented normal restore and browser compilation at that time.

Follow-up verification on 2026-10-03, after the user installed `wasm-tools`, closes
that blocker. With SDK 10.0.401 and workload set 10.0.401.1, normal
`dotnet test Gojo.slnx --nologo` restores successfully and passes all 1,732 tests.
The experiment suite also passes all 110 tests again with `--no-build --no-restore`.
Explicit Debug builds of `Gojo.Blocks.Browser`, `Gojo.GearShapeLadder.Browser` and
`GojoCAD.SketchViewer.Browser` pass, including native WebAssembly compilation and
linking. Blocks reports 43 warnings about obsolete Skia text APIs; the other two
builds report zero warnings. All three report zero errors. No implementation
change was needed. This verifies builds, not Release publishing, trimming or an
interactive browser session.

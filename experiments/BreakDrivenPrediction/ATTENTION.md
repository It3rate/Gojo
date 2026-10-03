# Demand-directed timing reads

This pass gives the existing event predictor a supplied local policy for
withdrawing unused computation. The consumer requests a timing resolution.
Usually only that rung is constructed and assessed; every fourth issue, or
after a miss, the nearest available finer rung is also read. Both reads belong
to the same forecast and earn one support adjustment through its frozen
demanded rung. A fine probe exposes timing detail but does not independently
earn trust or decide the consumer's next demand.

The [fixed protocol](ATTENTION_PROTOCOL.md) was committed before implementation
and trials. Run this pass with `--attention`; older experiments remain available
through their existing commands.

## Dormancy and observations

Demand zero stops new forecasts. It does not invalidate an already issued
promise, and public events still update the bounded input trace. If a complete
absence miss leaves support at zero, the attention policy closes the forecast
gate. Maps and the last two occurrences remain connected while dormant; empty
reports no longer cause repeated stale forecasts. A fresh public reversal in a
complete report opens the gate and starts a fresh two-occurrence trace. Two fresh events are needed
before predicting again, so a long silent gap cannot be mistaken for one period.

This deliberately leaves the broader question of forgetting open. Dormancy is
reversible withdrawal of forecast work, not destruction of a learned concept.
The detector and acquisition continue running. The chosen wake trigger is a
detected reversal, not arbitrary change in the source's endpoint value.

No report makes no transition. An unknown-coverage report makes the pending
promise unassessable without subtracting support. It clears cadence evidence
because hidden cycles cannot be bridged as a measured period. A complete empty
event report proves only that this detector emitted no reversals over its
covered interval. Both flat input and monotonic growth can produce that report;
the source's separate endpoint channel can still distinguish their values.

## What the comparison means

The new reader and the eager reference receive the same opaque public packets.
The reference continues constructing all three rungs and forecasting during
consumer withdrawal; that work is counted as unused. Results are separated by
the resolution frozen at issuance and whether the issue was wanted then.
Changing demand later cannot rewrite an earlier promise's value or credit.

The attentive reader expires a promise using its demanded cell. Diagnostic
probes cannot delay that verdict, even with nonnested widths. The older eager
reader waits for all cells; the fixed trials use widths 1, 3, 12 and twelve-tick
reports, so both expiry rules are checked at the same covering report boundary.
This equivalence does not hold for arbitrary widths or reporting schedules.

Constant next-report event-presence and event-absence baselines expose how much
of a broad timing score comes merely from continued activity. They ask an easier
question than fine timing and are reporting diagnostics, not hidden predictors
fed into the Gojo learner.

The policy, gate, probe phase, requests and decision witnesses are connected
Gojo carriers and relations. Native claims select reads and native arithmetic
proposes timing. The host supplies observations, schedules consumption, traverses
those local claims and applies the declared policy. Selection itself costs work:
fewer constructed rung rows do not prove lower total allocation or runtime.
Source acquisition is unchanged. Consumer utility, probe cadence and the dormant
gate law are still supplied, not learned.

## Connected read policy

The eager learner's existing twelve parts retain their roles. An attentive
learner adds a thirteenth relation with this local grammar:

```text
attention = group(issue-phase carrier, probe-period span,
                  dormant carrier, dormant 0-to-1 span,
                  outstanding probe-request carrier, request 0-to-1 span,
                  group(demand, support, admission cursor,
                        cursor correspondence, authored ladder),
                  latest transition evidence)
```

The issue phase advances only on a readable new forecast and cycles over the
probe span. Misses can leave a probe request pending through withdrawal or sleep.
Native width comparisons select the demanded map and, when due, its nearest
finer neighbor before cell construction. Unselected maps remain available for
later requests; their forecast cells and event assessments are not constructed.
This is read withdrawal, not permanent pruning of the authored vocabulary.
The positive probe span is supplied at construction; live retiming of that
span is outside this pass's fixed-policy contract.

The cursor, support, phase, dormant gate and probe-request changes enter one
whole-arrival settlement. All arithmetic and selection are staged first. The
root's copied availability must match the physical cursor, preventing an old
root from replaying a report or reopening a gate. Latest witnesses do not retain
chains of earlier learner roots. Frozen pending issues retain only their local
cadence, selected reads and decision evidence.

The allocation diagnostic surrounds each learner's observation call, including
all startup calls but excluding construction of the learner itself, source
processing, demand assignment, output inspection and graph capture. Consumers
run in a fixed order on one thread. These raw counts are useful for detecting
large policy overhead, not for claiming a controlled runtime benchmark.

## First observations

The [first measurements](results/attention/first-run.md),
[report traces](results/attention/traces.csv) and
[reachable graph sizes](results/attention/graph-sizes.csv) retain the fixed run.
No policy was tuned to these outcomes. The table shows seed 113 for deterministic
conditions and all three seeds together for jitter. Cell columns are eager to
attentive; percentages describe fewer allocated bytes during observation calls.

| Environment | Forecast cells | Event-assessment cells | Allocation reduction, all reports | Allocation reduction, positive-demand reports |
| --- | --- | --- | --- | --- |
| Regular | 177 to 59 | 174 to 58 | 16.0% | 5.3% |
| Jitter, three seeds | 531 to 201 | 522 to 196 | 13.0% | 1.7% |
| Intermittent | 177 to 51 | 84 to 19 | 27.7% | 17.3% |
| Changed cadence | 177 to 64 | 129 to 52 | 11.9% | 3.3% |

The difference between the last two columns matters. Much of the benefit comes
from actually stopping unwanted forecasts during the ten withdrawn reports.
Selective rungs alone face substantial control overhead. At width 3 the policy
is not even uniformly cheaper: jitter seeds 907 and
2029 allocated 2.6% and 2.2% more than the eager reader at that demand, despite
constructing fewer cells. Finer inspection needs to earn its own usefulness.
On regular, jittered and changed-cadence input, the wanted forecast sets and
selected outcomes stayed
the same as the eager reference. On regular input all 48 settled wanted forecasts
matched, split into ten width-1, twenty-eight width-3 and ten width-12 promises.
For jitter across seeds, those counts were eight of thirty exact-tick matches,
fifty of eighty-four width-3 matches and thirty of thirty width-12 matches.
The last result still asks an easy question: every complete jitter report had
an event, so the constant next-window presence reference also always succeeded.

Pairing the jitter forecasts by issue time, predicted point and observed event
shows why probes must remain qualified reads. Of the fifty valid width-3 hits,
twenty-two would miss at width 1. The attentive reader inspected nine of those
finer disagreements and left thirteen uninspected. It made 43 width-1 probes
among all 84 wanted width-3 forecasts. The coarse answers remain valid; the
uninspected detail is not established fine accuracy, nor automatically a defect
when the consumer requested only a coarse answer.

Intermittent input exercised actual dormancy. The attentive learner issued 34
forecasts rather than 59 and encountered 17 absence misses rather than 30. It
spent fifteen reports dormant, with three sleep transitions and two wakes; the
run ended during the third quiet block. Those counts are not an accuracy gain
on identical predictions. At tick 492, the eager reader's broad promise issued
at 480 matched the returning event at 486. The attentive reader had been dormant
and made no such promise. After receiving events at 486 and 491, at report 492
it issued its next forecast for tick 496. Saving stale work also meant losing that successful
anticipation. A wake at tick 252 occurred while demand was zero, demonstrating
that available event structure and requested output are separate gates.

The changed-cadence trial exposed another distinction. During withdrawal, the
outstanding promise was graded at tick 252, leaving support at two. No new
forecasts were issued, so absence grading stopped before the dormant gate
closed. At tick 372 the returning event at 369 joined the old event at 176,
creating a last-interval hypothesis of 193 ticks and a forecast at 562. The event
at 377 disproved it at the next report; the learner then predicted 385 correctly.
Both learners made this error. The explicit dormant-wake path avoids bridging
silence, but it does not yet handle every context break. Consumer demand,
retained success support and the applicability of an old cadence are distinct.
This result is preserved as a limitation, not repaired by changing the protocol
after the trial.

Retained structure stayed bounded, but attentive structure was not uniformly
smaller. At regular tick 480 it held 475 carriers and 4,308 relations, against
the eager reader's 403 and 3,839; at withdrawn tick 360 it held 107 and 911.
Native selection, phase and settlement witnesses can outweigh the retained
cells they replace while active. Every source still made 720 acquisitions.
Nothing here demonstrates a faster sensor, fewer samplers or lower end-to-end
runtime.

## What this suggests next

The useful result is a working, inspectable distinction between keeping a
structure available and spending work on its output. The supplied policy can
reduce allocations without multiplying evidence or rewriting earlier promises.
It also gives us concrete costs to charge against usefulness.

The next local mechanism should qualify cadence across activity boundaries
independently of demand and support. After that, probe results need an actual
consumer: compare the extra useful detail they provide with their cost, allowing
requests and probe frequency to adapt. A fixed every-four-issues probe can miss
systematic fine errors, and occasional agreement cannot certify uninspected
detail. Neither selecting a global winner nor automatically deleting dormant
structure follows from these measurements.

## Verification

```powershell
dotnet test experiments/BreakDrivenPrediction.Tests/BreakDrivenPrediction.Tests.csproj --no-restore
dotnet run --project experiments/BreakDrivenPrediction/BreakDrivenPrediction.csproj --no-build -- --attention
dotnet test Gojo.slnx --no-restore
```

All 89 experiment tests pass, including fifteen added checks for selective work,
shared credit, frozen demand, queued probes, nonnested expiry, dormant retention,
two-event restart, partial/unknown coverage, unresolved demand pressure, pure
inspection, stale roots, checkpoint continuation and bounded retention. A source
check shows flat and monotonic inputs can both produce complete empty reversal
reports. All 1,732 existing solution tests pass without restore. The ordinary
solution restore remains blocked by the missing `wasm-tools` workload for browser
projects; those targets were not validated. No production kernel code changed.

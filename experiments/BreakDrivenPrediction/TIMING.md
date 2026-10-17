# Timing predictions through a shared ladder

The experiment now makes one future prediction readable at several timing
resolutions. A coarse match can coexist with a signed finer mismatch, without
creating several independent observations or several support rewards. The
source's acquisition rate is separate from its reporting rate and from these
prediction resolutions.

The [protocol](TIMING_PROTOCOL.md) preceded the first trials. The
[measurements](results/timing/first-run.md), [per-report traces](results/timing/traces.csv)
and [retained graph sizes](results/timing/graph-sizes.csv) preserve the first
fixed-policy run. Parameters were not adjusted after seeing those results.

## Structure and current behavior

Two independent source structures receive the same changing numeric level.
One observes every base tick, composes differences and a signed product to
detect reversals, then reports fine detector occurrence times in twelve-tick
batches. The other observes only at twelve-tick boundaries. It exposes the
endpoint value and an open fine-event collection. Its local reversal clue
cannot establish which fine detector events occurred between acquisitions.
Public reports contain no path into either source's private sensor or trace.

These events mean detector emissions at observation times, not exact physical
inflections between samples. A source keeps two recent differences and at most
one bounded report window of events. Admission beyond a full window is rejected
until publication, rather than silently dropping events. Its first fine report
retains missing initial-difference evidence; this conservative learner leaves
that batch unassessed for cadence, while its known events remain inspectable.

After a complete report, the learner retains the last two public occurrence
coordinates. Their native difference proposes a period. A native quotient and
recurrence predict the next occurrence strictly after report availability.
Only one future issue is retained. The next report assesses that issue before
its observations can teach another; later events in the same batch never get
retrospective predictions. This is a supplied last-interval heuristic, not a
general pattern discovery or gradual phase-fitting algorithm.

The issue connects one frozen point and source witness to three structural
rungs. Their affine correspondences project the common point into cells of
width 1, 3 and 12 ticks, preserving source-basis residue. Each cell is
half-open [lower,upper). Assessment also requires an occurrence strictly after
issuance, so a coarse cell extending into the past cannot earn past-event
credit. Signed observed-minus-predicted timing remains available even when a
coarser cell matches.

One demanded rung is frozen with the issue. Its native verdict changes one
bounded support carrier by one tick. The other rung reads are diagnostic and
cannot multiply evidence. This pass uses width 3 demand. Support is neither
statistical confidence nor a gating rule; all forecasts remain exploratory.
Unknown coverage closes an issue as unassessable and clears its cadence trace,
without changing support. Complete empty coverage can establish a miss once
all issued cells have elapsed. Repeated or stale-root admission cannot score
the same issue again: a physical cursor and its explicit public correspondence
are checked against the root's version witness before any support changes.

Gojo relations own the source traces, cadence equations, cells, shared evidence,
support, demand and causal witnesses. The host supplies inputs, schedules reads,
applies the declared local policy and reports results. No separate host-side
fit or semantic model supplies the forecasts.

## First observations

All counts below include startup handling. A final pending forecast is not a
failure. Only jitter uses randomness; other conditions are identical across
their three seed labels and are shown once here.

| Environment | Settled forecasts | Width 1 matches | Width 3 matches | Width 12 matches |
| --- | --- | --- | --- | --- |
| Regular five-tick events | 28 | 28 | 28 | 28 |
| Jitter, three seeds | 84 | 29 | 52 | 84 |
| Gradually changing run length | 28 | 26 | 27 | 28 |
| Events stop halfway through | 28 | 13 | 13 | 13 |

The jitter case demonstrates useful resolution separation. The exact-tick
prediction often missed, while a wider read could still match. These are
different promises; 84 coarse matches are not 84 precise predictions. The
same occurrence and signed timing error remain connected across all three.

The coarse promise is particularly easy in these active streams. Its cell is
the next twelve-tick report window, and events continue arriving more often
than that. Its perfect agreement does not demonstrate fine cadence knowledge
or added value over simply expecting another event in that window. This is a
candidate for the redundant coarse read the user described, but no comparison
or pruning policy has yet established that removal.

When events stopped after tick 180, the fine source supplied complete empty
reports. Fifteen subsequent forecasts became absence misses. Support reached
zero at tick 276, but the stale cadence still issued new forecasts. The code
has not hidden this cost behind a participation score: the missing mechanism
is to stop that work and decide when to probe again.

Each fine source made 360 acquisitions and 30 reports. Each sparse source made
30 acquisitions and 30 reports. The sparse interface left fine-event coverage
open and produced no fine-event forecasts. That is a limit of this supplied
interface and learner, not proof that endpoint observations cannot support
other predictions. In a separate structural test, two interfaces ended at the
same value while the fine source retained five reversals and the sparse source
retained unknown fine multiplicity. No net change did not become no events.

Retained structure stayed bounded in the sampled prefixes and checkpoint tests.
The fine learner occupied 327-408 carriers and 3,133-3,892 relations; the sparse
learner stayed at 39 carriers and 290 relations. These fairly large structures
include native read witnesses. They do not demonstrate minimum computation:
temporary allocation and all three diagnostic rung reads still occur.

## Nonnested rungs and later optimization

Rung maps use explicit ratios on a shared clock, without requiring an integer
number of one rung inside another. A test with widths 13 and 21 preserves
their different cell boundaries. At one position neither cell contains the
other, and an observation can match the 13-tick cell while missing the wider
21-tick cell. Such disagreement is preserved; rung ordering does not invent
nesting or force agreement.

This is compatibility with the proposed 21/13 relationship, not evidence that
prime strides improve detection. Acquisition remains fixed and synchronous in
this experiment. Different origins, variable hops, sampler competition and
learned attention remain future work.

The next useful pass should put pressure on both unnecessary reads and stale
forecast work. A supplied consumer could request a resolution, compare the
incremental usefulness and cost of neighboring reads, and retain occasional
finer probes for changes. Include intermittent events and different timing
offsets so a broad next-report promise is not trivially sufficient. Intermediate
acquisition contracts should expose uncertain occurrence spans, allowing a
tradeoff between the current all-fine and endpoint-only interfaces.

The present run intentionally supplies the detector, cadence heuristic, rung
widths, demand and support law. It does not evolve new rungs, prune them, reduce
sampler count or learn a global utility function.

## Verification and reproduction

```powershell
dotnet test Tests/BreakDrivenPrediction.Tests/BreakDrivenPrediction.Tests.csproj --no-restore
dotnet run --project experiments/BreakDrivenPrediction/BreakDrivenPrediction.csproj -- --timing
dotnet test Gojo.slnx --no-restore
```

All 74 experiment tests pass, including 32 added timing cases. They cover
resolution-dependent agreement, signed error, nonnested frames, shared evidence,
frozen reads and demand, once-only credit, stale-root rejection, unknown versus
zero coverage, absence expiry, report bounds, private-state isolation, recovery
from unreadable demand, bounded retention and independent checkpoint replay.
All 1,732 existing solution tests pass without restore. The normal
`dotnet test Gojo.slnx` restore was blocked by this machine's missing
`wasm-tools` browser workload; browser targets were not validated. No production
kernel code or earlier trial behavior changed.

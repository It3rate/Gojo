# Consumer-specific prospective usefulness

This small experiment asks whether a contribution is useful to a particular
consumer beyond the prediction it already has. It also separates remembering a
short causal commitment from using an archive of successful answers.

The protocol was committed at `03d9910` before the fixed trials. See
[PROTOCOL.md](PROTOCOL.md), [the recorded results](results/first-run.md), and
[the broader usefulness discussion](USEFULNESS.md).

## What is actually connected

Three mutable input sites supply two native difference equations. A product and
qualified comparison detect an already observed reversal. On that occasion a
baseline and one supplied helper issue forecasts for the next observation:

```text
previous change a, latest change d
baseline next change = d
redundant helper next change = d
restored-scale helper next change = -a
helper contribution = helper forecast - baseline forecast
```

The helper's actual contribution flows through a retained, attachable publication
into the consumer's native sum. Receiver value and tension are both included,
so a negative correction is not lost. An issue freezes the forecasts, delivered
contribution, demand, allowed error interval and input premises. Its immediate
next outcome is connected through a native time-step span. Missing input closes
that promise unresolved; a later convenient observation cannot replace it.

For the same subsequent outcome, `OperationClaim.IntervalResponse` reads the
change in violation of the consumer's allowed error interval. Positive relief
adds bounded support through a supplied calibration; harm subtracts it. Support
can attach or withdraw the same publication. A prospective shadow comparison can
recruit the helper for a later issue, but cannot claim it helped before it was
connected. Ordinary quiet observations do not consume predictive support.

The core already supplies the required carriers, equations, live publication,
attachment/withdrawal, qualified comparisons and time-step structures. This
experiment changes no shared core. Its constructors and admission loop still
choose the candidates, eligibility policy, support calibration, scheduling and
attachment policy. It does not discover these choices autonomously. Native
equations own the calculations; waveform generation and report aggregation are
host orchestration.

## What the controls show

| Case | Observation | Interpretation |
| --- | --- | --- |
| Identical helper | No support or attachment | Duplicate predictions do not earn marginal credit. |
| Transitional turn followed by the old step size | Helper improves later forecasts and attaches | A contribution can earn use from prospective relief. |
| New larger slope persists | Helper damages a satisfied baseline; support falls and the route withdraws | Negative usefulness remains visible even when baseline error was zero. |
| Helpful pattern returns | The retained publication attaches again | Withdrawal need not erase the ability to recompute. |
| Longer known-flat gaps | Exact same ordered eligible prediction/support trace | Frequency of unrelated observations does not invent failures or credit. |
| Allowed error widened to four | Both forecasts satisfy the consumer; no support is earned | Greater precision need not have current utility. |
| Demand withdrawn during the adverse block | Route stays silent and support is retained | This policy separates use from ability; it does not charge for dormant retention. |

Across the 24 relevant outcomes of the mixed trial, baseline error outside the
allowed interval totals 64 ticks. Always using the helper would leave 32; actual
adaptive delivery leaves 16. There are two attachments and one withdrawal.
Delivered benefit exceeds always-helper benefit because withdrawal avoids the
remaining repeated damage in the adverse block. These are transparent designed
positive and negative controls, not evidence of general-purpose learning.

In particular, the ordinary zigzag and its shuffled increments are **algebraic
null controls**. Both use changes of magnitude two. On every eligible reversal,
`-a` already equals `d`, regardless of temporal order. Zero marginal benefit
there demonstrates honest handling of redundancy; it cannot establish learned
temporal discrimination. The changing-magnitude episodes supply the substantive
comparison. Their explicit unknown separators are supplied boundaries, not
learned segmentation.

The directional and absolute-scale diagnostics remain separate native reads.
This first consumer nevertheless reinforces one supplied error-interval
criterion. It does not yet have independent direction, scale or timing consumers.

## Dynamic calculation and retained evidence

The learner has three current level/presence sites, persistent operational
equations, bounded support, one outstanding issue and one latest diagnostic
outcome. It retains no chain of old root envelopes. Current forecasts are
recalculated from the live sites; changing a site changes the result without
replacing its equation. The latest outcome is not an answer lookup table and is
not used to generate the next forecast.

The pending issue is an intentional short-lived memory: it records what was
promised before the outcome. Recalculating that old promise using a subsequently
adapted structure would contaminate the comparison. Long-term replay is not
implemented here. Reconstructing a past trajectory would require retained or
resupplied driving input, or an explicitly approximate model of it.

The fixed runs retain 107–153 carriers and 1,309–1,915 relations at their final
boundaries; equal-shape repeated episodes keep the same graph size. Differences
between final counts reflect whether the final issue is ready or follows an
unknown separator, as well as the candidate's structure. These are not per-arrival
growth rates. The observer saves CSV history, selected native witnesses and final
worlds; none of these files feed the learner.

**Bounded retained structure is not cheap computation.** This implementation
still builds outcome/read evidence at every ready observation, including
irrelevant quiet observations. The first recorded pass allocated about 55 MB
during the compact restored-scale trial and 316 MB during its quiet-padded
counterpart, despite identical useful traces and retained final graph sizes.
These are cumulative process allocation readings around admissions, not live
memory, a controlled benchmark, or an estimate of irreducible Gojo cost. The
recorded report identifies the later unchanged-trial repeat used for expanded
observer counters. Its current measurements can differ slightly.

There is no computational price, storage pressure, bounded lifetime for shadow
exploration, or competition between several consumers yet. Quiet publication is
not computational dormancy. Consequently this experiment supports the proposed
memory direction but does not implement the pressure to achieve it.

## What to explore next

Keep the next increment small: connect two consumers that ask for different
aspects or resolutions, then expose a limited receiving capacity. Compare a
cheap sufficient contribution with a finer redundant one. Account separately
for prospective usefulness, active calculation and retained capacity; do not
sum incompatible quantities without an explicit conversion structure.

After that, compare recomputation against a short-lived retained read with a
visible applicability witness. Let current dependencies and actual resource
contention decide when to reuse, recalculate, suspend or forget. A dormant
structure also needs a small active trigger; it cannot recognize relevance at
zero cost. These pressures should be tested before adding a large population or
returning to elaborate timing machinery.

## Running and checking

From the repository root:

```powershell
dotnet run --project experiments/ConsumerPrediction -c Release -- --checks-only
dotnet run --project experiments/ConsumerPrediction -c Release -- experiments/ConsumerPrediction/obj/recheck-results
dotnet test Gojo.slnx
```

Use a separate output directory when repeating trials so recorded evidence is
not overwritten. The experiment's focused checks cover causal timing, signed
delivery, missing versus zero input, issue-owned obligations, actual attachment
identity, pure reads and bounded retained structure. They do not require the
supplied hypothesis to outperform its baseline. The solution checks the shared
libraries separately.

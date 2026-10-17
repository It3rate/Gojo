# Protected candidate trials: confirmation results

## What we learned

Finite seed energy let all eight runs learn the common source's newly necessary
ordinal distinction. The paired admission rule retained those useful candidates
and refused every candidate from the independent-noise source. After admission,
the eight useful candidates answered their next 16 purchased requests correctly:
**128/128**, with supported advice on every request.

However, **the existing error controller still produced the highest overall
return**. Protected trials were slower to become useful, performed poorly on
returning rare demand, and did not complete a trial for the later useful source.
The paired rule's advantage over unconditional replacement was only
**+0.007 net resource per world request**, with a descriptive interval spanning
zero. This study establishes a working paid trial and refusal mechanism; it
does not establish an improved overall learning policy.

Read the [offline review](seed-review.html) for the tables and an actual first-seed
trace. The [frozen protocol](SEED-PLAN.md), [machine analysis](results/seed-confirmation-01/analysis.json)
and [raw confirmation folder](results/seed-confirmation-01) contain the details.

## Study and verification

The fixed confirmation used fresh seeds **1000–1007**, five controllers, and
four phases of 512 tasks: **40 runs and 81,920 world requests**. Each seed's arms
shared the same action-independent world tape. The campaign finished in
**198.0 seconds**; that wall time includes artifact output and is not a native
execution benchmark. There were **zero Jev calls** and no account requirement.

The prices and world were fixed before confirmation. The existing error and
progress controllers were run through the unchanged legacy runner. The new
runner added three trial variants:

| Controller | Candidate finance and replacement law |
| --- | --- |
| Existing error | Existing error-gated learning and renewal |
| Existing progress | Existing signed-progress law, retest period 8 |
| Seed + paired | One finite seed-funded trial per source; later fee-funded work; replace only after coverage and paid paired benefit pass |
| Seed + unfiltered | Same work and exposures; promote any completed candidate, disconnecting the admission comparison and coverage requirement |
| Paired, no seed | Same paired trial law; candidate work must earn its funding from service fees from the start |

The two primary contrasts are paired versus unfiltered and paired versus no
seed. Comparisons with the legacy controllers concern complete packages,
including different reserve placement and learning lifecycles. They cannot
isolate the admission equation alone.

Verification passed:

- **2,319/2,319 tests** across 10 projects, including **119 UtilityEcology tests**
  and 21 new tests. See [solution summary](verification/seed-solution-summary.json).
- All **140 frozen production-source hashes** matched. The snapshot retains the
  exact pre-run protocol and source bytes; supporting tools and final documents
  are separately identified as captured at analysis time.
- Every task's signed currency identity matched the initial conserved capital
  plus actual physical consequences. Trace, aggregate and run gross/burn sums
  agreed. All five arms had matching tapes and request/source schedules within
  each seed.
- The strict analyzer and a separate audit reconstructed paid trial counters,
  seed lockout, retry spacing and every advanced validation comparison. See the
  [independent audit](verification/seed-independent-audit.json), which did not
  import the analyzer.
- Across the three new arms, **2,004 validation events** advanced:
  **1,566 known candidate predictions and 438 open predictions**. No open
  candidate contributed nonzero paired gain. Incumbent validation advice agreed
  with the actual issued command on all applicable events.
- The first seed's five native archives passed their declared signature,
  currency, alias, retained-anchor and paid-gate checks. The paired and unfiltered
  archives each replayed three closed native admission decisions. These are
  scoped fixtures, not complete world restarts or arbitrary novel-sample replay.

## Overall physical return

The primary endpoint includes every scheduled request: holds, startup, failed
work, pending trials and refusals. Return is **actual gross resource minus paid
burn**. Fees move existing currency between structures and are not subtracted
again. All prices are abstract experimental units, not measured CPU time or
money.

| Controller | Net/request | Gross/request | Burn/request | Action coverage | Advice coverage | Correct purchased actions |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Existing error | 2.836 | 5.323 | 2.487 | 69.0% | 65.0% | 10,711/11,300 (94.8%) |
| Existing progress | 2.731 | 5.203 | 2.472 | 69.2% | 64.5% | 10,650/11,332 (94.0%) |
| Seed + paired | 1.301 | 3.715 | 2.413 | 60.8% | 58.7% | 9,057/9,963 (90.9%) |
| Seed + unfiltered | 1.294 | 3.715 | 2.420 | 60.8% | 58.5% | 9,057/9,963 (90.9%) |
| Paired, no seed | 0.327 | 1.512 | 1.185 | 37.4% | 34.9% | 4,955/6,133 (80.8%) |

Advice coverage counts supported model publications. Action coverage also
includes the supplied fallback when advice is open. High conditional accuracy
can coexist with low coverage; unserved requests remain in the endpoint.

| Paired difference in net/request | Mean | Descriptive 95% interval | Positive seed differences |
| --- | ---: | --- | ---: |
| Paired minus unfiltered — primary | +0.007 | [−0.005, +0.019] | 6/8 |
| Paired minus no seed — primary | +0.974 | [+0.308, +1.640] | 6/8 |
| Paired minus existing error — package diagnostic | −1.534 | [−1.796, −1.273] | 0/8 |
| Paired minus existing progress — package diagnostic | −1.430 | [−1.714, −1.146] | 0/8 |

Intervals use eight independent seed differences and Student t(7), without
multiple-comparison adjustment. Requests and trials within a seed are not
extra replicates. These are descriptive uncertainty summaries for this small
benchmark, not guarantees for other environments.

Paired and unfiltered had exactly equal aggregate gross (**60,860**) and equal
aggregate correct/action counts. Paired burned **39,539**, unfiltered **39,654**:
the 115-unit aggregate saving explains their mean net difference. Individual
seed and source decisions differed and their gross differences canceled. Thus
the admission contrast demonstrated no aggregate gain in physical decisions.
It did block unsupported replacements, without yet yielding a convincing
overall-return advantage.

## Did finite startup support become useful?

| Controller | Launched | Completed | Aborted | Pending at end | Promoted | Refused | Seed-funded trial work | Fee-funded trial work |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Seed + paired | 39 | 23 | 0 | 16 | 8 | 15 | 4,650 | 1,484 |
| Seed + unfiltered | 39 | 23 | 0 | 16 | 23 | 0 | 4,650 | 1,484 |
| Paired, no seed | 2 | 2 | 0 | 0 | 2 | 0 | 0 | 331 |

The two seeded arms each launched the 32 initial trials plus seven later
fee-funded common-source trials. The paired controller admitted its eight
initial common candidates. It refused all seven common retries because their
paired benefit did not repay the new work, and all eight independent-noise
candidates. Seven completed noise blocks lacked the required known coverage;
the eighth had sufficient coverage but no benefit. No trial aborted in this
confirmation. Pending is a distinct outcome: work had not completed by the
declared end.

Each seeded arm began with 320 trial units per source, transferred from the
same conserved initial group capital. Across the eight runs, **5,590 units of
seed reserve remained unused**. All 16 pending trials were rare or later useful
sources, already in validation. Rare candidates had completed only **8–14 of
32** validation events; later useful candidates **15–26 of 32**. None of the
eight runs closed all four initial trials. Work energy was available, but it
could not create purchased exposure.

The no-seed controller launched and admitted only two trials, both for the
common source. Startup support therefore improved access to trials under this
law. That advantage is conditional on the funding placement and buyer policy;
it does not imply every protected structure becomes sustainable.

Later trials were funded only from service fees. This proves the implemented
financing separation and absence of renewed seed sponsorship. A supplier can
earn fees while the consumer's physical actions lose resource, so fee-funded
work alone is not evidence of environmental self-sufficiency. The initial
buyer capital remained available throughout this finite study.

The same fixed final phase gives a less selected late comparison:

| Controller | Final-phase whole-group net/request |
| --- | ---: |
| Existing error | 4.065 |
| Existing progress | 4.103 |
| Seed + paired | 0.678 |
| Seed + unfiltered | 0.676 |
| Paired, no seed | −0.333 |

The late seeded package remained positive on average, but substantially below
the legacy controllers. There is no evidence here for indefinite survival
without an initial consumer endowment or for universal service sustainability.

## Two actual structural decisions

### Useful ordinal candidate: seed 1000, common source

The common source initially needs only a coarse direction. After its private
law changes, the service must distinguish two ordered regions. The learner is
not told that a phase change occurred.

The first candidate launched at world tick **525**. It received 16 paid training
experiences, then both models were frozen for 32 new purchased validation
experiences. It closed at tick **691** after **103 local source requests and
48 purchases** following launch.

| Retained comparison | Result |
| --- | ---: |
| Known candidate validation reads | 32/32 |
| Paired conditional resource benefit | +320 |
| Complete candidate construction/training/validation burn | 163 |
| Admission net | +157 |
| Native decision | Accept |

The candidate's directions were captured before each validation outcome. The
paired calculation used the same events for both models under the declared
opposed-action payout correspondence. Its +320 is an estimated difference
between decision paths; it is not extra physical income.

On the next **16 purchased common requests** at ticks 701–740, the admitted
structure supplied known advice and was correct **16/16**. Actual gross was
128, allocated burn 74, and allocated net **+54**. Across all eight admitted
common candidates, the analogous fresh windows gave **128/128 correct** and
allocated net **+400**. These descriptive windows are selected after successful
admission and can contain subsequent online learning; they do not replace the
all-task comparison.

The same service later launched a fee-funded candidate at tick 816. It closed
at tick 894 with 32 known reads, **zero paired benefit and 211 burn**. The native
rule refused it and kept the useful incumbent. Its next 16 purchased requests
were also correct 16/16. A better-funded opportunity is not automatically a
reason to replace a sufficient structure.

### Rejected noise candidate: seed 1004, source 2

This source's outcomes are independent of its available sample order. The
learner receives the same acquisition and comparison interface as elsewhere,
without a noise label.

The candidate launched at tick **123** and closed at **1097**, spanning
**209 local requests and 48 purchased experiences** after launch. It obtained
only **12/32 known candidate validation reads**. Its apparent paired benefit
was **+60**, but total trial burn was **199**, giving **−139** before the
coverage requirement. The paired controller refused it for insufficient
coverage. The unfiltered arm would promote completed candidates despite these
signals.

The next 16 purchased noise requests, ticks 1127–1547, were actual incumbent
actions after refusal: **6/16 correct**, gross **−72**, allocated burn **53**,
net **−125**. This is evidence that retaining the incumbent does not solve
irreducible noise. It is not a measured fresh performance test of the rejected
candidate, which was never made the active service.

Another noise trial, seed 1000, had sufficient **24/32** known reads but **zero
paired benefit**, with **172 burn**. It was refused by the net requirement.
Only two future purchases remained, both incorrect; that incomplete follow-up
is not presented as a full fresh16 window.

## Noise, returning demand and new utility

The source and phase names below belong to evaluation. Learners receive no
quality flag, hidden desired action, phase identity or calibrated raw metric.

| Controller | Pure-noise trial promotions | Pure-noise trial work | Common change: first16 correct/requests | Rare return: first16 correct/requests | Later useful source net/request | Profitable later-useful seeds |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Existing error | 0 | 0 | 80/128 | 74/128 | −0.273 | 2/8 |
| Existing progress | 0 | 0 | 70/128 | 74/128 | +0.285 | 4/8 |
| Seed + paired | 0 | 1,508 | 64/128 | 7/128 | −1.320 | 0/8 |
| Seed + unfiltered | 8 | 1,508 | 64/128 | 7/128 | −1.320 | 0/8 |
| Paired, no seed | 0 | 0 | 64/128 | 7/128 | −0.778 | 0/8 |

Blocking all eight noise promotions is a useful safety effect of the specific
admission law. It did **not** avoid the 1,508 units already spent discovering
that those candidates failed, and it did not eliminate later purchased noise
actions. The paired pure-noise endpoint remained negative, about −1.419 per
source request. A local refusal is not an acquired general understanding that
the source is unlearnable.

The rare service's knowledge was retained during the demand gap, but the new
package had not admitted an ordinal candidate before it returned. Only 16 of
the first 128 returning requests were purchased across the eight seeds; seven
were correct. The older controllers purchased 74, all correctly answered. This
failure concerns acquiring and completing a useful structure in time, as well
as retention. Removing knowledge indiscriminately was not the remedy tested.

The later useful source was initially noisy. In the final phase the paired
package bought **132/998** requests and got **65/132** actions correct. No
candidate was promoted; all eight initial trials remained in validation.
Allocated gross was −284, burn 1,043, net−1,327. The mean seedwise rate was
−1.320, with no profitable seed. Candidate progress during a block that spans
noise and useful periods is not a timely demonstrated adaptation.

## Why the protected mechanism was slow

The law needs **48 purchased physical exposures** after launch: 16 training,
32 validation. A low-utility service explored only every eighth local request
may need roughly **384 local requests** to obtain them. That clock is slower
than the shared world clock when source traffic is rare, and a demand gap
stops it altogether.

The current service still acts while the candidate is tried, but both learning
models are frozen during validation. Long, sparse blocks preserve a clean
comparison at the cost of delayed adaptation. For example, seed 1000's noise
trial took 1,835 world ticks and 377 local requests to buy 48 exposures. Its rare
and future-useful trials never reached 32 validation events before the fixed
end. The cohort's 16 pending trials still had unused seed: this was not merely
an exhausted work reserve.

The block also compares benefits accumulated on 32 validation events against
all construction, training and validation work, without assuming any future
amortization. That cautious law refuses expensive but already equivalent
common-source retries. It may also miss a candidate whose value arrives after
the declared horizon; this study did not change the horizon to seek a better
result.

## Native implementation and supplied scaffolding

`SeededProvider` and `SeedTrial` construct connected Gojo roots for contracts,
models, purses, counters, training/evaluation participation, paid comparisons,
conditional-benefit equations, claims and admission witnesses. Native boundary
publications carry a direction or an open read. The C# runner supplies the world,
scheduling and diagnostic projections.

Every model starts with an empty interpretation, not a private magnitude plus
an estimated accuracy. Presence can support a sufficient constant response;
paid ordinal anchors can then support a useful distinction. Unknown remains
open throughout. A candidate's open validation answer follows the incumbent
decision for the conditional comparison, contributing exactly zero gain while
remaining excluded from known coverage.

The action/consequence product infers a successful direction from actual
feedback under the supplied two-opposed-actions premise. Conditional resource
comparison further uses the declared affine payout correspondence: positive
gross maps to an opposed consequence at −3/2, negative gross at −2/3. Neither
equation receives the private desired-action label. These are explicit apparatus
assumptions, not learned physics.

The construction grammar, maximum four ordinal anchors, buyer exploration,
prices, attention replenishment, trial cap and admission law are authored. All
four service bodies exist initially. This is native local learning and a paid
candidate lifecycle under supplied rules, not discovery of arbitrary new
programs, currencies, hierarchies or autonomous market laws.

The runner correctly binds the opaque acquisition to its actual command and
resource feedback. The native trial does not independently authenticate that
association against the world's private instrument map. Duplicate delivery
outside the synchronous runner would need an additional receipt/admission law.

Historical trial roots retain live candidate model carriers, which can learn
after promotion. Captured pre-feedback validation publications and count
witnesses preserve the evaluated decisions, not permanent immutable model
snapshots. Reported `learningUpdates` counts base-model updates only;
`trainingCount` counts paid candidate experiences, not successful ordinal
updates. Native functional tests cover useful learning separately.

## Development record and research boundaries

`seed-pilot-01` is a retained **excluded development failure**: source drift
prevented recovery of its exact helper version and an affine admission path
passed a witness rather than its scalar publication. The strict analyzer refused
it. Its numbers are not validated results or evidence for selecting settings.

The repaired `seed-pilot-02` used seeds 900/901 with the same predeclared settings
and passed strict auditing after functional tests were added. Its two-seed
mean net/request was 2.888 for error, 2.496 for progress, 1.249 for paired, 1.211
for unfiltered and 0.631 without seed. Those figures were descriptive only, with
no intervals and no parameter tuning before fresh confirmation.

The design borrows three limited ideas from primary literature:

- [Schmidhuber's same-history comparison](https://people.idsia.ch/~juergen/driven2008.pdf)
  motivates evaluating alternative structures on identical retained events.
  This experiment scores finite paid decision benefit, not history compression
  or universal curiosity.
- [Dawid's prequential approach](https://academic.oup.com/jrsssa/article/147/2/278/7106293)
  motivates publishing predictions before the outcome arrives. The trial does
  not inherit probabilistic forecasting guarantees.
- [CVFDT](https://ai.cs.washington.edu/www/media/papers/kdd01b.pdf) provides a
  precedent for growing an alternative while retaining an incumbent. The Gojo
  experiment does not implement that decision-tree algorithm or its bounds.

Eight seeds, one small supplied world and one candidate grammar bound the
claims. Post-admission windows are selected; source-specific costs are assigned
to request time rather than causal contribution; supplier reserves are last
observed request-time balances, not simultaneous final wallet positions. No
result establishes market optimality, arbitrary compositional intelligence or
indefinite self-sustainability.

## What to test next

Keep this mode opt-in. Preserve the existing error controller as the overall
performance comparator. The next useful question is whether a protected
candidate can **purchase a small number of its own physical probes**, paying
their execution and opportunity costs from its finite reserve, rather than
waiting exclusively for the buyer's sparse requests. This directly targets
unfinished rare/new trials; a reserve for work alone did not give them enough
experience.

A separate controlled study could compare that change with the same budget and
with a shorter validation block, while retaining fresh prediction-before-
feedback and an untouched holdout for judging selection. Another bounded test
could stop expensive retries when a candidate reproduces an already sufficient
incumbent. Each change should be tested independently against the present law;
none was added or tuned after this confirmation.

Reproduction, offline review generation and checked rollback are documented in
the [README](README.md#protected-candidate-trials-opt-in).

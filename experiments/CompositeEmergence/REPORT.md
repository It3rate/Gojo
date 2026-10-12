# Composite emergence: a learned relation becomes a reusable service

Status: fresh confirmation complete. Eight seeds, 100,352 scheduled training
events and 1,185,792 evaluation trials, completed in 724.4 seconds. All source
hashes, accounting identities and six native archive replay checks matched.

## Short summary

- **A useful higher-level distinction was learned.** All eight source learners
  retained `A * B` and achieved 100% held-out prediction and task success.
  Each isolated primitive's interface contains no information about the rule.
- **The learned distinction was reusable.** A fresh consumer learning the
  reversed action law achieved 91.1% after 16 target actions and 100% after 32.
  A flat consumer achieved 62.4% and 86.6%; a newly learning source/consumer
  achieved 49.6% and 48.9%. All capable comparisons reached 100% eventually.
- **Learning when to investigate worked unevenly.** With immediate history,
  the learned manager earned 7.045 per step versus 6.000 for always probing.
  A simpler free outcome-memory rule earned more: 7.174. With delayed history,
  the manager beat outcome memory, but did not reliably beat always probing.
- **Missing observations exposed a major failure.** Frozen managers often
  stopped reacquiring information after a failed probe. Their action coverage
  fell to 5.21% with immediate history and 0.52% with delayed history.

The supported result is learned relational composition, opaque service reuse
and some useful learned freshness decisions inside an authored hierarchy.
Open-ended hierarchy discovery or subjective comprehension is not established.

## The question we can actually test

The motivating question was whether a larger Gojo structure can do something
that its individual components cannot represent within their own context.
We made that distinction exact: two isolated direction channels each receive
no information about a hidden apparatus rule. Their matched relation can reveal
it. A new learner then acts through that relation's small published service,
and another learner decides when to pay to refresh that service.

This tests a functional hierarchy with explicit information boundaries. It
does not test subjective comprehension or whether a primitive has an opinion
about its parent. The hierarchy, ports and finite construction grammar are
authored; learning selects and calibrates useful structures inside them.

## A small world with a strong information boundary

The world privately chooses an orientation `H`, either `-1` or `+1`. A fresh
fair pulse `A` produces a later response `B = A * H`.

| Private rule | Pulse A | Response B | Their product |
|---:|---:|---:|---:|
| +1 | -1 | -1 | +1 |
| +1 | +1 | +1 | +1 |
| -1 | -1 | +1 | -1 |
| -1 | +1 | -1 | -1 |

For either rule, A alone and B alone are each equally likely to be either
direction. Their entire permitted single-channel histories remain uninformative
under the fair independent pulse law. Independent timing carries no hidden-rule
label. In the balanced population, `I(H;A)=I(H;B)=0`, while the matched pair
determines H and contains one bit about it. These identities follow from the
authored world law; we are not treating a finite empirical information estimate
as a proof. The distinction resembles joint information discussed by
[Williams and Beer](https://arxiv.org/abs/1004.2515).

The actual sensor interfaces also withhold goal, issued action and global
resource consequence. This matters: in this binary world, action, goal and
success/failure together identify the previous rule. They are legitimate later
observations for higher controllers, but would invalidate primitive isolation.

The primitives retain local ordinal predictors, but the relation consumes their
current publications rather than those forecasts. This fair-stream construction
does not establish useful task learning by each primitive predictor. The useful
learned levels tested here are relational source, action consumer and freshness
manager.

The source learner receives the two public events, its genuine exploratory
action, and that action's later physical direction consequence. It predicts
before admitting the consequence. It never receives H as a training label.
Shared timestamps and host scheduling supply the pairing opportunity; the
experiment does not discover pairing itself.

## What is learned at each level

### 1. A retained native relation

The lower learner initially has four inadequate alternatives: current A,
current B, previous A and previous B. After 64 credited trials, inadequate
prediction can trigger one expansion into pairwise native product, sum and
difference equations. There are at most 22 candidates.

Each candidate learns feature/action-to-consequence evidence in connected
native rows. Correct prequential predictions earn `+1`; incorrect or unanswered
predictions earn `-1`. Retention uses this evidence and a complexity cost.
All candidates inspect the same actual outcome; they do not each execute a
separate physical action. The selected equation and its source bindings are
retained in the Gojo graph.

The two-state product is sufficient. Same-trial sum and difference are also
sufficient through three states, so product selection is not the only possible
success. This is bounded equation selection and evidence learning, not invention
of multiplication or an unrestricted program language. The distinction from
broader reusable program synthesis is discussed in [LITERATURE.md](LITERATURE.md),
including [DreamCoder](https://people.csail.mit.edu/asolar/papers/EllisWNSMHCST21.pdf).

### 2. A fresh consumer of an opaque service

The producer publishes a captured feature coordinate, boundary event and public
time. Raw A/B carriers and the private derivation remain on the producer side.
The consumer cannot recover them by traversing the published witness's ancestry;
tests inspect the actual boundary graph, not just C# argument lists.

The consumer sees feature and goal. A sparse native market compares two legal
action offers using their later resource consequences. A correct action earns
`+8`, an incorrect action `-12`, and a hold earns `0`. Initial exploration,
periodic alternatives, finite count aging, utility reads and choice claims are
explicit local laws.

For transfer, the physical action wiring is reversed. The source is frozen,
and an independent fresh consumer learns the new correspondence. The parent
therefore uses the learned service in a changed consequence law without
relearning its raw sensor relation. This is a narrow task family, not arbitrary
environment transfer.

### 3. A learner choosing freshness

A persistent world flips its private rule without an announcement. The manager
sees only publication availability, four coarse age buckets and prior outcome
class. It learns whether to pay for a fresh probe or reuse the retained feature.
The probe price is 2 in confirmation, including unsuccessful acquisitions.

Its context/action utility learning remains immediate. The delayed-history arm
delays delivery to operational prior-outcome memory by five decisions; it does
not delay every training feedback route. During frozen evaluation, learned
parameters stay unchanged while observation memory, age and latest-read
witnesses can evolve. This is a finite contextual choice learner, not a
Bellman/value-iteration learner or automatic option discovery. The relationship
to temporal abstraction and computation selection is explained in
[LITERATURE.md](LITERATURE.md), drawing on
[Sutton, Precup and Singh](https://papersdb.cs.ualberta.ca/~papersdb/uploaded_files/470/paper_SPS-aij.pdf)
and [Hay et al.](https://proceedings.mlr.press/r10/hay12a.html).

## A concrete native calculation

Suppose an observed pulse is `A=-1` and its later response is `B=+1`.
The retained native product reads `-1 * +1 = -1`. Only that feature coordinate
crosses the service boundary. For goal `+1`, the source-task consumer's learned
market favors action `-1`.

There is concrete evidence for this correspondence in confirmation seed 200's
[consumer ledger projection](results/confirmation-01/consumer-source-learned-200.json):

| Context: feature, goal | Offer | Retained observations | Positive resources | Negative resources | Mean at scale 256 |
|---|---:|---:|---:|---:|---:|
| -1, +1 | -1 | 44 | 352 | 0 | +2,048 |
| -1, +1 | +1 | 4 | 0 | 48 | -3,072 |

The native mean is `256 * (positive - negative) / count`: here `+8` versus
`-12` in resource units. Comparison claims select the better offer. Those rows
were learned from issued actions and bound outcomes; the runner did not install
the correct action correspondence. A fresh transferred consumer must learn
the reversed law through its own evidence.

This has two different kinds of coarseness. The service emits only the
task-relevant two-state distinction. Finite evidence rows also retain both
possible outcomes, withhold an answer below their support/lead threshold, and
age counts using native half-scale reads with explicit residue. These are
empirical alternatives and admission rules, not calibrated probability bounds.
Missing readings stay open; they are not silently numerical zero. An initial
no-prior outcome and neutral gross outcome intentionally share public class 0,
while operational memory retains whether an outcome was actually known.

The semantic feature requires one binary distinction; its physical Gojo graph
and 64-bit coordinate representation are larger than one bit. This study does
not implement packed encodings or learning an unknown unit/zero calibration.

## Pilots, freeze and fresh confirmation

Three exploratory runs used seed identities 0..4. They remain available with
their historical manifests and original results.

| Pilot | Settings changed next | Observation |
|---|---|---|
| 01: one seed, short manager training | Manager training raised from 384 to 2,048 actions | Source discovery worked. The delayed-history manager failed to acquire a useful policy. |
| 02: two seeds, price 1 | Probe price raised to 2 | Both manager arms largely learned to probe every time. At hazard 0.04, ideal immediate reuse beats always probing by only 0.2 resource units. |
| 03: two seeds, price 2 | More independent confirmation seeds and longer source/test budgets | Both immediate policies used about 3% queries with about 97% correctness. Delayed policies differed: about 25% versus almost 100% queries. |

Price 2 makes the economic distinction more visible without changing the learner
law or age buckets. Transport verification was also strengthened after pilot 02
from shape checks to actual restored prediction/action/query replay and shared
pipeline aliases. Original earlier artifacts retain their original checks.

Source/settings froze on 2026-10-11 before confirmation launched at
23:51:44 UTC. The fresh confirmation uses eight seeds 200–207, 768 discovery
trials, 192 consumer-training actions, 768 frozen source/consumer test trials,
2,048 manager-training actions per history arm, and 2,048 temporal test actions
per controller/condition. Baseline reversal hazard is 0.04; shifts use hazard 0.12,
probe price 5 or missing probability 0.10 per primitive. See [PROTOCOL.md](PROTOCOL.md)
and the [run manifest](results/confirmation-01/manifest.json).

## Fresh confirmation results

All tables below use the same eight seed identities. Accuracy counts every
trial, including unanswered predictions/holds as unsuccessful; coverage is
reported separately. Descriptive intervals are two-sided 95% Student-t
intervals over seed means or paired seed differences, not over the million
individual trials. Every checkpoint remains its own group. No multiplicity
correction is applied. Identical finite-tape results can have a zero-width seed
interval; that does not guarantee perfect future performance.

The complete summaries, all paired contrasts, per-seed effects and conditional
recovery statistics are in [the generated analysis](results/confirmation-01/analysis/analysis.md)
and [machine-readable analysis](results/confirmation-01/analysis/analysis.json).

### Discovery and its negative controls

Each arm receives 768 training trials, then 768 frozen test trials per seed.

| Source arm | Correct over all trials | Prediction coverage | Correct when answered |
|---|---:|---:|---:|
| Learned expansion | 100.00% | 100.00% | 100.00% |
| No expansion | 45.75% | 90.58% | 50.59% |
| No learned updates | 0.00% | 0.00% | Unavailable |
| Shuffled outcome feedback | 46.83% | 96.50% | 48.48% |
| Shuffled communication | 49.27% | 98.18% | 50.14% |
| No retained A across B arrival | 42.25% | 84.49% | 50.21% |
| Previous-trial pairing | 48.50% | 95.36% | 50.86% |

All eight primary learners retained the two-state product after expansion to 22 candidates.
Negative controls selected several different equations and produced about chance
correctness conditional on answering. Zero performance for the unchanged model
means it withheld predictions, not that every committed answer was wrong.

The paired learned advantage over no expansion was **54.25 percentage points
[49.10, 59.39]**; over shuffled communication it was **50.73 points
[47.85, 53.61]**. The controls support useful correspondence and temporal
retention, rather than success caused by an isolated channel or arbitrary
candidate ordering.

### Action learning and causal use of the service

After 192 consumer-training actions, the learned-service, flat and fixed-sum
consumers each achieved 100% correctness and return 8. The neutral-left baseline
was 49.20%, return −2.160. Consumers of inadequate/broken learned services were
49.38–50.39%, with returns near −2.

| Learned-service deployment | Correct | Action coverage | Mean gross return |
|---|---:|---:|---:|
| Ordinary paired signals | 100.00% | 100.00% | +8.000 |
| Same-state donor publication | 100.00% | 100.00% | +8.000 |
| Opposite-state donor publication | 0.00% | 100.00% | −12.000 |
| Absent publication | 0.00% | 0.00% | 0.000 |
| B orientation inverted, calibration frozen | 0.00% | 100.00% | −12.000 |
| Wider ordered delay, up to 31 | 100.00% | 100.00% | +8.000 |
| Missing probability 0.10 per primitive | 80.65% | 80.65% | +6.452 |

Replacing the service with a valid same-state donor leaves behavior intact;
opposite-state donors invert which action is appropriate. Withdrawing the
feature produces a hold. This establishes a causal role for the exposed
distinction. It does not establish a general causal abstraction theorem.

Missing source observations preserve 100% correctness when an action is
answered. The 80.65% all-trial rate is close to the authored complete-pair
availability of `0.9 * 0.9 = 0.81`. This local failure remains honest/open;
the later manager's recovery from it is a different question.

### A new consumer reuses the frozen relation

The table gives frozen held-out correctness at each target-training checkpoint.
The reused source already paid 768 discovery trials; those are additional to
the displayed target budgets.

| Target training actions | Reused service | Fresh source + consumer | Flat A/B/goal | Fixed sum + consumer | No expansion | Shuffled communication |
|---:|---:|---:|---:|---:|---:|---:|
| 0 | 50.80% | 50.80% | 50.80% | 50.80% | 50.77% | 50.75% |
| 8 | 59.57% | 50.07% | 52.26% | 55.35% | 50.83% | 50.15% |
| 16 | 91.10% | 49.58% | 62.42% | 74.87% | 50.20% | 49.95% |
| 32 | 100.00% | 48.88% | 86.61% | 94.34% | 50.15% | 49.17% |
| 64 | 100.00% | 49.69% | 98.32% | 100.00% | 49.09% | 49.27% |
| 128 | 100.00% | 87.32% | 100.00% | 100.00% | 50.05% | 49.72% |
| 192 | 100.00% | 100.00% | 100.00% | 100.00% | 50.15% | 49.64% |

At 16 target actions, reused-service accuracy exceeded fresh-service by
**41.52 points [31.31, 51.73]**, flat by **28.68 [18.98, 38.38]**, and fixed-sum
by **16.23 [8.37, 24.09]**. All eight reused consumers first measured 100%
at 16 or 32; fixed-sum at 32 or 64, flat at 32–128, fresh-service at 128 or 192.
These are checkpoint observations, not exact first attainment times.

The learned two-state feature gives the consumer four encountered feature/goal
contexts. The fixed sum gives six; flat A/B/goal gives eight. All eight saved
source-consumer projections confirm those counts. This is a useful compression
of the decision context. It does not make the flat model incapable: all capable
arms reach the same ceiling. Fresh-source learning is also disadvantaged early
by its changing representation and the declared 64-trial expansion trigger.

### Paid refresh and reuse

Each frozen temporal evaluation has 2,048 decisions per seed. No switch
announcement or hidden rule enters the manager. A scheduled probe always pays
its fee, even if no valid pair is obtained.

**Immediate prior-outcome history, baseline hazard 0.04 and price 2:**

| Controller | Correct | Action coverage | Paid probes | Mean net return [95% seed interval] |
|---|---:|---:|---:|---:|
| Learned refresh | 96.27% | 99.95% | 10.70% | 7.045 [6.926, 7.164] |
| Always probe | 100.00% | 100.00% | 100.00% | 6.000 [6.000, 6.000] |
| Never refresh after initialization | 45.47% | 100.00% | 0.05% | −2.907 [−3.211, −2.603] |
| Free outcome memory | 95.87% | 100.00% | 0.00% | 7.174 [7.118, 7.229] |
| Probe after failure | 96.09% | 100.00% | 3.96% | 7.138 [7.087, 7.190] |
| Probe every 8 decisions | 88.21% | 100.00% | 12.50% | 5.392 [5.164, 5.620] |

Learned refresh exceeded always-probe by **1.045 [0.926, 1.164]** resource units
per step. It lost to free outcome memory on every seed: paired difference
**−0.129 [−0.238, −0.019]**. The failure-probe comparison also had a higher mean;
the paired difference was **−0.094 [−0.205, 0.018]**. The learned policy is useful
relative to indiscriminate acquisition, but not the best available controller.

**Delayed prior-outcome history, same physical world and price:**

| Controller | Correct | Action coverage | Paid probes | Mean net return [95% seed interval] |
|---|---:|---:|---:|---:|
| Learned refresh | 97.79% | 99.95% | 72.03% | 6.123 [5.975, 6.271] |
| Always probe | 100.00% | 100.00% | 100.00% | 6.000 [6.000, 6.000] |
| Never refresh after initialization | 45.47% | 100.00% | 0.05% | −2.907 [−3.211, −2.603] |
| Free outcome memory | 83.05% | 100.00% | 0.00% | 4.610 [4.463, 4.757] |
| Probe after failure | 86.60% | 100.00% | 13.45% | 5.050 [4.906, 5.195] |
| Probe every 8 decisions | 88.21% | 100.00% | 12.50% | 5.392 [5.164, 5.620] |

Here the learned manager exceeded outcome memory by **1.513 [1.283, 1.744]**.
Its always-probe difference was only **0.123 [−0.025, 0.271]**, so the study
does not demonstrate a reliable advantage over that simple expensive rule.
Five seeds probed almost every decision; three used about 24–28% probes.
Immediate policies also varied, about 4–26% probes, with one intermediate 14%
policy. The mean is not a single uniform mechanism discovered independently
by every seed.

### Distribution shifts and the failure to reacquire

| History and frozen test shift | Learned return | Always-probe return | Outcome-memory return | Learned action coverage |
|---|---:|---:|---:|---:|
| Immediate, hazard 0.12 | 5.708 | 6.000 | 5.651 | 99.95% |
| Delayed, hazard 0.12 | 5.471 | 6.000 | 0.871 | 99.95% |
| Immediate, probe price 5 | 6.724 | 3.000 | 7.174 | 99.95% |
| Delayed, probe price 5 | 3.963 | 3.000 | 4.610 | 99.95% |
| Immediate, missing signals | 0.360 | 4.482 | 7.174 | 5.21% |
| Delayed, missing signals | 0.022 | 4.482 | 4.610 | 0.52% |

Faster reversals reduce the return of stale-service policies. Frozen managers
do not receive price as a context coordinate, so price 5 simply debits their
unchanged acquisitions more heavily. Delayed policies that learned nearly
always to probe earn about 3 under that shift; no price adaptation is claimed.

Missing observations cause the most serious failure. A paid incomplete pair
withdraws feature availability. The later missing/neutral contexts were not
adequately explored during ordinary training, and frozen first-offer behavior
can keep choosing reuse while the feature remains open. The consumer then
holds; no new negative action outcome forces a useful retry. Both the learned
manager and the authored failure-probe rule can get stuck. Fixed-interval and
always-probe retry regardless, and outcome memory does not depend on those
sensors at all. These are observed behavioral differences; the explanation
follows the implemented contracts and retained ledgers rather than inferred
motivation. Retraining on missing episodes is an untested next step.

The immediate learned return loss versus always-probe under missing signals
was **−4.122 [−4.410, −3.835]**. This failure outweighs any claim of robust
general-purpose information management from the successful baseline alone.

### First success is not sustained recovery

Baseline tapes contained 673 rule switches across eight seeds.

| Controller and history | Mean first-success delay among eligible seeds | Completed intervals | Interrupted/unresolved intervals |
|---|---:|---:|---:|
| Learned, immediate | 0.892 decisions | 642 | 31 |
| Learned, delayed | 0.496 | 649 | 24 |
| Outcome memory, immediate | 1.000 | 639 | 34 |
| Outcome memory, delayed | 3.977 | 557 | 116 |
| Always probe | 0.000 | 673 | 0 |
| Never refresh | 0.000 | 334 | 339 |

The never-refresh controller's zero delay shows the limit of this statistic:
when a switch makes its ancient feature correct again, it succeeds immediately;
many other intervals never succeed before the next switch. This is not effective
recovery. Likewise, learned delayed-history under missing signals completed
only **2 of 673** switch intervals, with 671 unresolved/interrupted. Only two seeds
contribute its conditional delay estimate; six zero sentinels are excluded.
The complete analysis retains every interval count and omission.

### Structure, transport and verification

The source retained 22 candidates and a two-state selected publication on every
seed. The learned source-task runner root had 951 carriers, 6,618 relations,
6 placeholders and 2,825 coordinate occurrences. These counts include retained
search and operational evidence; they are not the minimal cost of a product.
The useful consumer contexts are 4 versus 6 for fixed sum and 8 for flat.

| Native archive from seed 200 | Bytes | Signature and actual replay |
|---|---:|---|
| Relation source | 242,135 | Matched |
| Task consumer | 71,294 | Matched |
| Immediate-history manager | 104,543 | Matched |
| Delayed-history manager | 110,305 | Matched |
| Connected immediate pipeline | 430,321 | Matched |
| Connected delayed pipeline | 433,328 | Matched |

The replay checks compare actual restored features, predictions, actions and
queries, including shared mutable port identities and independent continued
learning where relevant. They do not reconstruct a complete external world or
every discarded event.

The full repository Release test gate passed **2,175 tests**, including
**66 CompositeEmergence tests**, with no failures or skips. Independent review
found no blocking boundary, alias or frozen-parameter issue. The final analysis
checked **904 unique result rows**, **848 physical resource accounting identities**
and **123 current source hashes** with zero warnings. Source and consumer frozen
projection guards also passed during the campaign.

The campaign ran from 2026-10-11 23:51:44 UTC to 2026-10-12 00:03:48 UTC under
.NET 10.0.12. Its 100,352 scheduled training events include frozen/no-update arms;
they are not 100,352 successful parameter updates. Its 1,185,792 evaluation trials
include repeated tests across controllers, shifts and checkpoints; these are
not that many independent learned organisms. There were 904 result rows at
the seed/stage/controller/condition/checkpoint level, and eight independent
confirmation seed identities. No paid Jev calls were made.

## Costs, limits and what remains to test

Source/transfer stages count acquisitions without charging a probe fee. Only
the temporal stage prices acquisitions. Transfer curves equalize target action
budgets, while source pretraining adds 768 trials. A target-phase advantage is
not a total-training-cost advantage. The fresh-service comparator learns both
levels and encounters a changing representation. Flat and fixed-sum consumers
are important positive comparisons, rather than failures to exclude.

Training-event totals count scheduled trials/actions, not every candidate
read, ledger update or native operation. All 22 source candidates are retained
and evaluated; the learned two-state publication does not imply that the whole
search graph has been minimized. Saved source/transfer runner roots include
the common source graph even for flat/fixed-sum decisions. Their full graph
counts must not be used as necessary-controller memory comparisons. Native
graph sites, archive transport bytes and actual managed memory are different
measurements.

In the temporal task, an authored outcome-only controller has a free alternative
information route. With immediate history and reversal hazard `q`, its ideal
accuracy is `1-q` and return `8-20q`; always probing at price `c` gives `8-c`.
The one-step boundary is `q=c/20`. This arithmetic is an analyst benchmark,
not a learner input. A simpler outcome-memory controller beating a learned
paid-service manager must remain visible.

Longer-delay tests preserve sequential A-then-B scheduling and ordered matching.
They do not test interleaved events, unknown pairing, elapsed waiting costs or
general asynchronous robustness. Inverting B while freezing calibration tests
semantic failure, not orientation relearning. Same/opposite donor publications
test causal use under a fixed consumer; deployment perturbations can create
unfamiliar inputs. The retrained shuffled-communication control separately
tests useful correspondence with channel marginals preserved, in the spirit
of [causal intervention tests](https://proceedings.neurips.cc/paper/2021/file/4f5c422f4d49a5a807eda27434231040-Paper.pdf).

First success after a rule switch is weaker than sustained recovery. Interrupted
or unresolved intervals are counted separately. The runner's zero sentinel for
no completed interval is excluded from delay estimates; it does not mean instant
recovery. A conditional delay can look good even when most intervals never
complete, so its eligible-seed and completed-interval counts must accompany it.
Frozen changed-price tests
cannot establish price adaptation because current price is not a manager input.
Per-action aging at count 64 and alternate probes every 16 context updates can
leave rare offers with much older evidence. Ages 1..3 share a bucket; this is
an authored information loss which may matter to refresh timing.

## Reproduction and evidence

- [README.md](README.md): exact run, test and analysis instructions.
- [ARCHITECTURE.md](ARCHITECTURE.md): native surfaces, contracts and permitted ports.
- [LITERATURE.md](LITERATURE.md): primary sources, motivating ideas and guarantee limits.
- [PROTOCOL.md](PROTOCOL.md): controls, pilot changes and confirmation freeze.
- [Confirmation raw data](results/confirmation-01/data.json),
  [results](results/confirmation-01/results.json),
  [curves](results/confirmation-01/curves.json),
  [trace](results/confirmation-01/trace.json) and
  [native archive checks](results/confirmation-01/archive-check.json).
- [Pilot analysis](results/analysis-pilots/analysis.md): all three pilots kept
  separate; historical-source drift is explicitly reported.

The offline dashboard is an inspection projection of recorded data. It labels
the recorded first-seed learned-controller trace, hides private world fields
by default, and shows unacquired channels as "not sampled". It does not run
an alternate learning model. No paid Jev calls were made for this study.

## What I would extend next

1. **Train recovery from missing information.** Include unavailable publications
   during manager training, rather than only testing them after freezing. Keep
   failures open and let the native market learn whether another paid attempt
   is worthwhile. Separate an observed neutral outcome from no observed outcome
   only if the consumer finds that extra distinction useful. Test both ports.
2. **Learn adequacy instead of supplying one final feature resolution.** Give
   local producers several native coarse reads with different costs. Let a
   consumer request refinement only when competing action claims remain
   unresolved. Compare the resulting decisions and acquisition costs with an
   always-fine consumer. This directly tests stopping at sufficient information.
3. **Learn a grouping or temporal option.** Introduce interleaved observations
   and multiple reusable relation services. Let later evidence select which
   services to combine or retain across a longer action, with explicit missing
   bindings and complexity costs. Freeze discovered services and test a new
   consumer again. That would move beyond selecting one supplied equation in
   an authored three-level hierarchy.

These extensions should each receive paired neutral/flat controls, intervention
tests, complete cost reporting and fresh seeds. The current task is deliberately
small enough to make its native explanation inspectable; increasing complexity
should answer a new question rather than make the same result harder to read.

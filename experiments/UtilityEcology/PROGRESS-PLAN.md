# Utility ecology: error and learning progress

Status: completed; two development pilots and eight fresh confirmation seeds
retained. See PROGRESS-REPORT.md for results. The earlier study remains in
[REPORT.md](REPORT.md), with its original protocol in [PLAN.md](PLAN.md).

## Frozen confirmation selection

Both development runs are complete and retained. Pilot 01 used retest period
16, seeds 700–701, and showed reduced noise work but delayed useful re-entry.
The permitted single adjustment was applied in pilot 02 on the same paired
tapes: retest period 8. Its signed and cycle-only behavior were identical on
both seeds; that limits any claim that a progress signal caused its gains.

Mean net/world task in pilots (no confidence intervals):

| Pilot | Error only | Signed progress | Cycle only | Change interest |
|---|---:|---:|---:|---:|
| 01, retest 16 | 2.472 | 2.092 | 1.796 | 2.092 |
| 02, retest 8 | 2.472 | 2.481 | 2.481 | 2.481 |

Confirmation is frozen at **retest period 8**, with every other parameter and
the existing physical world unchanged. Eight fresh paired seeds **800–807**,
four variants, 65,536 total tasks. No adjustment based on those fresh results is
allowed in this pass. The signed-versus-cycle comparison remains primary,
including if it shows that budget scheduling explains the apparent benefit.

## Question

Can a small local observation of improving prediction help a service spend less
on persistent error while retaining its ability to learn after circumstances
change? Does that observation add value beyond an ordinary budget and periodic
retesting schedule?

The previous confirmation showed that error gating reduced unnecessary updates
under low error. Persistent noise and newly useful information remained difficult.
This study adds a bounded progress observation to the same apparatus. The world,
prices, supplied interpretation grammar and consumer contract stay fixed.

This is an authored learning controller. Its progress observation can emerge from
experience, but its windows, thresholds and spending rules are supplied. It does
not learn an optimal gate or estimate a calibrated amount of information gain.

## Native observation

Each source-specific provider retains a connected local monitor:

```text
G(exposed prediction contract,
  completed observation blocks,
  prediction/consequence claim witnesses,
  known-error and usable-prediction counts,
  signed error-change and coverage-change publications,
  comparison version and consumption witness)
```

The monitor observes the prediction made before the current model update and the
later independently witnessed consequence of the actual issued command. The
existing native retrodiction and claim supply the observation; the world supplies
no desired-action label. Monitor evidence is collected even when learning is
denied. A permission decision can use only earlier completed observations, never
the current outcome or a prediction recomputed after training.

Comparability is local: the same source port, presence context, prediction axis
and action-normalized consequence contract. There are no private sample classes,
raw magnitudes, analyst descriptions of source quality or announced world phases
at this boundary. The current world has one present-sample context per source.
The monitor does not invent finer contexts using private classifications.

### What enters a block

A block contains **16 purchased observations with a valid physical consequence
sign of `-1` or `+1`**, including observations whose offered prediction is open.
Those observations already belong to the provider's ordinary feedback stream.
Holds that are not delivered to the provider are not extra free observations.

Unsupported physical feedback produces an inspectable open refusal. It does not
enter a block or establish whether a prediction was usable. Ordinary binding and
receipt checks still apply. An open offered prediction with a valid physical
consequence does enter the block and contributes to the count of observations
without a usable prediction.

Unknown error is never inserted as error zero. Prediction availability is a
different observable: on an admitted event, the monitor can establish whether an
answer was supplied. A known zero availability count describes that observed
absence of answers; it does not claim that an unknown predicted quantity was zero.

### Error and coverage

For each completed block, retain:

- the total admitted observation count, exactly 16;
- the number of usable pre-update predictions;
- the native equality losses for usable prediction/consequence pairs;
- the open evidence for observations that do not establish a numeric error.

The native equality loss remains **0 for agreement and 2 for disagreement**.
The block's conditional mean error is published on the existing 256 scale and
remains open unless at least **8** known losses exist. Its range is therefore
0 through 512. Coverage is the usable-prediction count divided by 16, on a 256
scale, and ranges from 0 through 256.

For the older and recent completed blocks:

```text
signed error change = older mean error - recent mean error
coverage change     = recent coverage - older coverage
```

Positive error change means fewer mismatches among supported predictions.
Negative change means worsening. A numeric error-change publication requires at
least eight known losses in **both** blocks. Before that, its result is open.
Coverage change can be known independently of numeric error change.

The two blocks within a comparison are disjoint. When a later block completes,
the comparison advances to a new version. Consecutive versions can share a
completed block; they are not independent statistical tests. Reads of one version
do not create additional evidence.

### Eligibility to continue an investigation

The signed-progress arm can request a small continuation through either route:

1. **Error improvement:** signed error change is at least **128**, and prediction
   coverage has not fallen. With loss 0/2, that threshold is a 25 percentage-point
   drop in the conditional mismatch rate.
2. **Interpretation availability:** coverage change is at least **128**, equivalent
   to eight additional usable predictions in a full 16-observation block.

The second route remains available when old numeric error is open. It measures
the emergence of supported answers, not their demonstrated correctness. Later
error, actual utility and costs remain responsible for testing those answers.

The nonfalling-coverage guard prevents a service from claiming improvement merely
by withdrawing more difficult predictions. It does not make conditional error
immune to every selection effect. Different sample mixtures, acquisition histories
and learned abstention can still affect the comparison.

The monitor is retained across an ordinal refinement or renewal. The public
prediction contract has not changed, and improvement across different internal
models is part of the question. Captured earlier predictions remain earlier
predictions. A genuinely incompatible prediction contract must fail local
admission rather than silently share this history.

## Funding, cooldown and retesting

Progress supplies information about permission; it does not create currency,
rewrite an answer, add landmarks or give an unfunded service extra attention.
Eligibility is checked before the existing paid learning transfer.

The bounded-controller arms use:

| Rule | Fixed amount |
|---|---:|
| Initial bootstrap | At most 16 actual paid learning grants |
| Continuation from one eligible fresh comparison version | At most 4 paid grants |
| Scheduled retest | At most 4 paid grants |
| Retest interval | 8 paid permission inspections in confirmation (16 in pilot 01) |
| Ordinary settled low-error schedule | Existing period of 16 |

An actual paid grant is a funded learning permission. It is distinct from a
requested permission and from a model update that ultimately receives usable
training support. Unproductive paid grants still consume resources and budget.
Retest timing advances only after a paid, affordable `Decide` inspection.
Unaffordable attempts do not advance that retest cycle and remain recorded.

Each comparison version can open **at most one** continuation. One fortunate
error decrease cannot renew a budget indefinitely through repeated reads.
Unknown, plateaued or worsening evidence cannot bypass the bounded bootstrap and
retest rules in the signed arm. After spending its available investigation
budget, a service can keep answering and observing while further high-error
learning waits for a fresh eligible comparison or a scheduled paid retest.

The original low-error occasional-learning rule remains in force. Progress does
not require a useful stable service to maintain a positive error derivative, and
cooldown does not discard its learned model. The existing dormant retention
policy also remains unchanged.

Finite retesting is essential: without it, denying learning could prevent the
very improvement needed to reopen learning. It is not a guarantee of recovery.
A buyer that stops acquiring a source, an unfunded service or an inadequate
interpretation grammar can still prevent useful evidence and learning.

## Four arms

Every arm contains the same passive monitor and records its evidence. Only its
connection to permission differs. All arms use upfront fees, buyer probing every
eight source requests, ordinal renewal enabled and the opportunity gate disabled.

| Arm | Learning permission hypothesis |
|---|---|
| `progress/error-only/upfront` | Existing error gate; progress observations cannot affect permission. |
| `progress/signed/upfront` | Error gate with bounded bootstrap, continuation, cooldown and retest; fresh signed improvement or coverage gain can extend an investigation. |
| `progress/cycle-only/upfront` | Same bounded bootstrap and scheduled retest machinery, with progress signals withdrawn from continuation decisions. |
| `change/upfront` | Same bounded machinery as signed progress, but a supported error change of either sign and magnitude at least 128 can request a continuation. |

The change arm keeps the same nonfalling-coverage guard on error-change paths and
the same positive coverage-gain route. It treats deterioration as bounded
interest in recovery. It does not relabel deterioration as improvement.

The primary signed-versus-error contrast tests the complete spending controller.
The signed-versus-cycle contrast asks whether observed progress adds useful
allocation beyond generic budget throttling and scheduled retesting. Their
actual learning amounts need not match: changing that allocation is the proposed
mechanism. The change arm is a diagnostic for broader interest and false
continuations in noise, not a second primary success claim.

## Fixed world and prices

Four equal phases of **512 world tasks** remain unchanged. The evaluator can
describe their laws, but these descriptions are not learner inputs:

- A common source initially needs one response, then needs an ordinal distinction.
- A rare valuable source disappears for two phases and later returns unchanged.
- A noise source has fair consequences independent of the sampled measurement.
- A previously unhelpful source becomes ordinally useful in the final phase.

The independent deterministic tape, private monotone encodings, physical rewards
and source request probabilities are inherited from the completed study. Every
arm for a seed receives the same tape. Feedback still depends on the actual
issued action; equal tapes do not imply equal subsequent acquisition histories.

Initial buyer capital is **16,384**. Each of four supplied services costs **8**
to construct and receives **64** sponsorship units, with **32** initially placed
in attention. Initial consumer trial allowance remains **16** purchases.

| Item | Resource units |
|---|---:|
| Upfront service fee, an internal transfer | 2 |
| Purchased interpretation work | 1 |
| Acquired order comparison | 1 |
| Permission inspection | 1 |
| Granted learning work | 1 |
| Ordinal construction or reset | 16 |
| Active service upkeep per 32 world tasks | 1 |
| Parked retention | 0 |

The inspection contract covers the added monitor/gate work at the same abstract
price in all arms. These units do not measure CPU time or price every native
operation. Currency conservation must include all wallets, attention, escrow if
present and explicit cost sinks. Internal fees cannot increase group return.
Physical gross and burned work remain separate from prediction error and progress.

## Pilot and confirmation

1. Run two development seeds, **700–701**, with all four arms and phase length 512:
   eight runs and **16,384** total world-task evaluations.
2. Inspect admission and payment traces, supported comparisons, open-history
   behavior, noise continuations and cold re-entry. Preserve every pilot artifact.
3. The single predeclared policy adjustment is shortening the retest interval
   from **16 to 8** if traces show a clear funded cold-re-entry failure caused by
   the retest schedule. Poor aggregate accuracy alone does not justify it. Identify
   the acquired feedback, available capital, remaining budgets and delayed learning
   path; keep the original pilot and rerun the same development seeds separately.
4. Fix the chosen interval, save source hashes and the full arm/settings manifest,
   and run **eight fresh paired seeds, 800–807**, without further changes: 32 runs
   and **65,536** world-task evaluations.

No world, reward, buyer or price tuning is part of this comparison. Integration
faults can be repaired and regression tested during development; their failed
runs must remain distinct from evidence about the controller. No paid Jev calls
are required.

### Endpoints

The primary endpoint remains:

```text
(signed physical gross - all burned work) / all world tasks
```

Starting endowment is excluded from return. Holds, startup, failed investigations
and paid but unusable learning opportunities stay in the denominator. The two
primary paired contrasts are signed progress minus passive error, and signed
progress minus cycle-only. Report paired seed variation and descriptive intervals;
do not select a successful source, seed or phase as the overall endpoint.

Secondary observations:

- Requested permissions, funded paid grants and actual model updates separately.
- Completed windows, known-error counts, coverage, signed error change, open
  comparisons and comparison versions consumed.
- Permission trigger, remaining bootstrap/continuation/retest budget and funding
  failures, so extra work has an inspectable cause.
- Pure-noise updates, continuations, ordinal proposals, burned work and net return.
- Common-source adaptation after its unannounced change and later stable accuracy.
- Rare-source performance on return, without a use-based memory reset.
- Final newly useful source: all-task net, answered coverage and the number of
  seeds with positive attributed net. Failed discovery remains a result.

Always report all-task action coverage and interpretation support coverage beside
conditional answered accuracy. Per-source costs attributed to the current world
step are useful diagnostics, not causal marginal contributions of that service.
Private phases and law labels are permitted only in evaluator projections.

## Noise and claim boundaries

A signed change can be positive by chance. Rectifying it or taking its absolute
value creates a positive average interest signal even when nothing is learnable.
For two independent 16-observation blocks of fair binary mismatch indicators,
exact enumeration gives:

| Statistic, on a mismatch-rate scale | Expected value |
|---|---:|
| Signed older-minus-recent difference | 0 |
| Positive part of that difference | 0.069975 |
| Absolute difference | 0.139950 |

These are analytic calibration fixtures, not predictions of the running
provider's adaptive supported-error distribution. Abstention, window overlap,
selection and changing predictors make the experiment more complicated. A
threshold, coverage guard and finite continuation budget limit the consequence
of misleading evidence; they do not prove that every admitted improvement is
learning or that every noisy stream will be rejected.

The monitor measures pre-update prediction trends on later observations. It does
not compare frozen old and new models on an identical held-out set, prove that
learning caused a decrease, measure information gain, or discover an optimal
allocation. A positive result supports this bounded controller in the supplied
world. A negative result can expose insufficient evidence, false progress,
opportunity starvation or costs that outweigh its benefits. Those mechanisms
must be distinguished in traces rather than repaired using private truth labels.

## Primary literature informing the design

- [Oudeyer, Kaplan and Hafner (2007), *Intrinsic Motivation Systems for Autonomous
  Mental Development*](https://www.pyoudeyer.com/ims.pdf): local prediction-error
  trends avoid interpreting switches between incomparable activities as learning.
  Their smoothed regional observations motivate source-local histories here; this
  experiment does not reproduce their region discovery or action-selection model.
- [Baranes and Oudeyer (2013), *Active learning of inverse models with intrinsically
  motivated goal exploration in robots*](https://www.pyoudeyer.com/ActiveGoalExploration-RAS-2013.pdf):
  absolute competence change also makes deterioration interesting for recovery.
  That motivates the separate change diagnostic, with bounded spending and an
  explicit distinction between improvement and renewed interest.
- [Schmidhuber (2008/2009), *Driven by Compression Progress*](https://people.idsia.ch/~juergen/driven2008.pdf):
  compares old and new predictors/compressors on the same history. That is a
  stronger comparison than our consecutive-window trend. Re-evaluating the same
  history also requires care about training-set improvement versus generalization;
  this study deliberately retains independent pre-update physical observations.

These papers supply motivation and useful design distinctions. Their formal or
empirical results do not establish the validity of this Gojo controller; the
matched controls, native evidence checks and fresh confirmation are its test.

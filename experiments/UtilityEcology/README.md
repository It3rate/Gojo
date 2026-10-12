# Utility ecology: useful interpretation, learning gates and local payment

This is a small consolidated Gojo experiment. Four services begin with empty
interpretation models. A consumer can fund their work, use their condensed
answers, observe physical consequences, and decide whether further purchases
are useful. The study tests learning gates, paid refinement, returning demand,
and alternative payment contracts together.

The follow-up [learning-progress study](PROGRESS-REPORT.md) adds a native
completed-window observer and tests whether its information improves learning
permission beyond a budget-and-retest control. Run it with `--progress`; use
`--retest-period 8` to reproduce its frozen confirmation settings. Its results
are separate from the original study below.

The results and retained failures belong in [REPORT.md](REPORT.md). Start with
[PLAN.md](PLAN.md) for the experimental questions. This README describes the
mechanism and its limits; it does not report a confirmation outcome.

No account, network service or paid Jev call is required. The external world,
deterministic tapes, scheduling and JSON projections are C# adapters. Models,
evidence, claims, gates, payment routes and mutable reserves are connected Gojo
structure.

## Run

From `D:\source\repos\It3rate\Gojo-phototaxis`:

```powershell
dotnet run --project experiments/UtilityEcology -c Release -- --pilot --phase-length 512 --output experiments/UtilityEcology/results/my-pilot
```

The pilot uses two seed identities, starting at 500. A longer fixed confirmation
uses eight fresh seeds, starting at 600:

```powershell
dotnet run --project experiments/UtilityEcology -c Release -- --campaign --phase-length 512 --output experiments/UtilityEcology/results/my-confirmation
```

Each seed has four equal world phases: a phase length of 512 gives 2,048 steps
per experimental arm. Use a fresh output directory; nonempty directories are
refused. A run's `manifest.json` records its actual settings, arm definitions
and source hashes before execution. Keep a run's sources stable while it runs.

Supported command options are `--pilot`, `--campaign`, `--output`, `--seeds`,
`--seed-start`, `--phase-length` and `--help`. Additional economic settings and
private monotone encodings are available through the local C# runner; they are
not advertised as command options.

Verification:

```powershell
dotnet test experiments/UtilityEcology.Tests -c Release
dotnet test Gojo.slnx -c Release
```

## What starts unknown

An arriving observation is an opaque acquisition from a known source port:

```text
sample = ReadWitness(source,
                     presence frame,
                     G(open magnitude, known presence),
                     open residue,
                     source)
```

Every request in this study contains a physically present sample. Its presence
is supplied by the instrument, while magnitude and useful interpretation start
open. Thus the first learned step is finding useful meaning in an existing
signal. Sensor existence itself is not learned here.

The external apparatus has six sample classes and privately uses a shifted,
nonlinear increasing encoding. None of those metric coordinates, class numbers,
unit intervals, an absolute zero, or scenario labels appears in a sample graph.
A paid comparison between two actually acquired samples from the same source
returns only their order: lower, indistinguishable, or higher. The world refuses
unacquired, foreign and cross-source comparisons with an open read.

The progression is:

1. **No supported interpretation:** an empty presence-conditioned model returns
   an open answer. Unknown has no numeric value or assumed accuracy.
2. **Presence may suffice:** repeated physical consequences can support one
   direction for this source. The provider can answer without comparisons or
   ordinal landmarks.
3. **A useful distinction is needed:** unresolved pressure can sponsor a bounded
   ordinal model. It retains opaque anchors and learns coarse regions associated
   with useful directions.
4. **Refinement or renewal remains optional:** another paid proposal can replace
   that ordinal model if the configured arm permits it. A provider that has not
   constructed an ordinal model can keep using its sufficient coarse answer.
   After ordinal construction, this version retains the presence model for
   inspection but does not fall back to it when the ordinal answer is open.

Ordinal anchors are comparison references, not recovered numeric magnitudes.
This pass does not calibrate units or learn a full number encoding. Source
orientation, binary actuator alternatives and the physical reward contract are
supplied premises. Their presence should remain clear when interpreting the
learned distinctions.

## External world and information boundary

The names below describe analyst-visible conditions. Learners receive only a
source identity, a sample, purchased comparisons, their issued command and its
later resource consequence.

| Source | Phase 0 | Phases 1 and 2 | Phase 3 |
|---|---|---|---|
| Common | One direction always succeeds | Two ordinal regions require opposite directions | Same ordinal law |
| Rare valuable | Ordinal law | No requests | Returns with the same law |
| Noisy | Consequences independent of available samples | Same | Same |
| Future opportunity | Noisy consequences | Noisy consequences | Ordinal law becomes useful |

Request frequencies are also part of the apparatus:

| Phase | Common | Rare valuable | Noisy | Future opportunity |
|---|---:|---:|---:|---:|
| 0 | 55% | 20% | 15% | 10% |
| 1 and 2 | 65% | 0% | 25% | 10% |
| 3 | 35% | 25% | 15% | 25% |

The source identity tells the consumer which exposed service is being requested;
it does not say whether that service is learnable, valuable, changing, or rare.
No reversal or new-opportunity announcement is published.

Request selection, sample classes, noisy consequences and fallback tape draws
use separate deterministic random streams. Experimental arms reuse the same
tape. Exact tests enumerate the fair physical noise law to show that sample
class supplies no predictive information in the noisy source. Reproducible
finite pseudorandom tapes need not be exactly balanced and do not themselves
prove statistical independence.

## Native interpretation and physical feedback

`InterpretationProvider.cs` connects the source contract, presence predictor,
optional ordinal model, local gate, pressure, request clocks and last edit.
Both learned models are reused from `PrimitiveLearning`.

The consumer sees a new boundary publication containing either a supported
direction or an open result. Private sample ancestry, model counts and ordinal
landmarks remain within the producer. `EcologyRunner.cs` applies a native action
calibration to that advertised direction. When advice is unavailable, the
consumer has an explicitly supplied native alternating-direction fallback.
An unpurchased request produces an explicit hold.

`EcologyWorld.Execute` publishes the gross physical outcome of the actual
command. Ordinary correct actions produce +8 resource and incorrect actions
produce -12. The rare source has twice those consequences: +16 and -24. Holds
give 0; unsupported or unavailable commands remain open.

The learner receives no desired-action label. Under the declared physical
premise of two opposed actuator choices, it can calculate:

```text
successful direction = issued direction × sign(actual gross consequence)
```

This product is a native equation. A failed action identifies the opposed
successful direction under this specific apparatus law. A hold cannot supply
such a label. The optional ordinal model receives the derived direction through
an explicit native affine mapping to its two consequence positions.

Prediction is read before admission of that outcome. Binding connects the
physical resource event to the exact issued command; unbound feedback cannot
train the provider. The source may retain directional information at a lower
resolution than the resource amount used by the economy.

Provider feedback does not currently guarantee duplicate-event exclusion.
The synchronous runner delivers each physical event once. Asynchronous training
would need a separate bounded admission/receipt law before repeated delivery
could safely be treated as idempotent.

## Learning permission and structural proposals

`LearningGate.cs` is an authored local controller. Its native error evidence
starts open. Latest mismatch and a bounded aged error ledger can request
learning. Supported low error slows ordinary updates, with occasional scheduled
checks. Missing support can also request a test.

The opportunity rule allows a periodic funded experiment even while existing
error is low. In this runner:

- traffic means a request the buyer has chosen to purchase;
- funded demand means the buyer can afford the declared proposal amount;
- requests rejected by the buyer do not reach the provider gate;
- traffic is not restricted to uninterpreted observations;
- demand is not a learned forecast of another agent's future willingness to pay.

These proxies supply a bounded opportunity mechanism. They are not learned
value of information or autonomous discovery of a market. Learning permission
and answer support are separate: permission can investigate an open state but
cannot manufacture a supported answer.

A gate inspection costs one attention unit. A granted learning attempt costs
another. Attention is funded from a supplier reserve and accounted in the same
resource system. Attention consumption, actual admitted model updates and
answer availability are recorded separately; a paid grant does not guarantee a
valid label or successful learning.

Unmatched or open advice adds unresolved pressure. Every fourth matching
prediction relieves one unit. Pressure has a finite boundary; overflow remains
carrier tension. After at least sixteen further requests, pressure of four or
more can admit a paid proposal if the arm, gate and buyer permit it.

Each proposal burns sixteen resource units and constructs an ordinal learner
with at most four landmarks. At most eight such proposals are admitted per
provider. The presence model is retained. Renewal replaces the ordinal learner
and witnesses the previous state and edit. This is a supplied construction
grammar, with bounded paid variation; all four source-service scaffolds already
exist at initialization.

High irreducible error can keep requesting work. Finite funding bounds that
work, but exhausting a reserve does not establish that a service has recognized
noise. This implementation does not estimate learning progress within matched
contexts.

## Experimental currency contracts

`ServiceEconomy.cs` owns native buyer and supplier wallets, paid-work sinks,
pending escrows, utility ledgers, rent laws and local participation.

The baseline endowment is 16,384 resource units. Four initial bodies each cost
8 to construct. Each receives 64 units of sponsorship, of which 32 are initially
transferred into its attention account. Sponsorship and attention funding
redistribute existing resource.

| Event | Accounting rule |
|---|---|
| Service fee | 2, transferred internally |
| Purchased execution | 1, burned into an explicit sink |
| Requested comparison | 1, burned; repeated same-pair reads within one request share the paid witness |
| Gate inspection | 1 attention unit burned |
| Granted learning attempt | 1 additional attention unit burned |
| Ordinal proposal or use-based reset | 16 burned |
| Active maintenance | 1 per economy time unit |
| Parked maintenance | 0 in this study |

The scheduler advances one economy time unit after every 32 world tasks. It
parks sufficiently idle services while preserving their models. A return can
revive funded participation. The use-reset comparator additionally discards
interpretation after an idle interval and charges reconstruction. This is a
test of a particular use-based forgetting law, not a blanket claim about decay.

Two payment contracts are compared:

- **Upfront:** the fee goes to the supplier when the purchase is admitted.
- **Success paid:** the buyer funds escrow; a later positive gross outcome pays
  the supplier, and a nonpositive outcome refunds the fee to the buyer. Work
  already burned remains paid.

Success can result from the supplied fallback while advice remains open. Success
payment is consequently an attributed outcome contract, not proof of the
supplier's causal contribution. A stronger contribution model would need an
explicit counterfactual or another justified local credit contract.

Error, unmet interpretation and observed utility cannot mint currency. At every
step the runner checks:

```text
buyer + suppliers + attention + escrows + burned-work sinks
    = initial external grant + subsequent signed physical outcomes
```

Signed wallet balances include retained negative tension. Unpaid rent is
separate demand. Group return counts gross external outcomes minus burned work;
internal fees are not subtracted again. Utility quotations currently estimate
observed gross benefit. Buyer selection does not optimize a forecast of every
prospective fee, comparison, gate and construction cost.

All work prices are declared abstract units. They do not measure CPU time, native
equation operations, garbage collection, memory bytes or actual monetary cost.
Incoming sample construction, utility inspection, artifact output and some
native reads are unpriced. Computational efficiency and experimental resource
return remain separate questions.

## Comparisons and interpretation

The protocol includes initialization, always-learning, error-gated, opportunity,
payment, use-reset, communication withdrawal, renewal withdrawal and early
buyer-cutoff arms. The matched error comparator uses the same buyer exploration
period and renewal permission as the opportunity/upfront arm so that its gate
contrast can be interpreted separately.

Earlier pilots compare broader packages: opportunity arms buy exploratory
requests every eighth source request, while original error/always arms use a
period of thirty-two, and renewal permissions differ. Their differences cannot
be attributed solely to the opportunity gate. The recorded manifest is the
authority for which matched arm and periods were actually executed.

The no-interpretation-learning arm retains empty models and has no learned
advice. Its supplied fallback can still act on purchased requests. It is an
initialization/learning control, rather than a population that never acts.

Communication withdrawal removes the delivered advice while allowing private
producer interpretation and training. `Interpreted` therefore measures producer
publication availability; it does not mean the consumer received that answer.
`Answered` counts non-hold physical actions. Useful accuracy, coverage and net
return answer different questions and should be reported together.

Always learning can expose whether a selective gate saves work. Keeping versus
resetting rare services tests useful retained knowledge after a demand drought.
Changing payment contracts tests how resource distribution changes funded
participation; redistribution itself creates no group gain. Pure noisy traffic
and the later useful opportunity provide explicit possible failure cases.

Pilot artifacts remain in their original directories. Pilot 01 exposed an
equation-result/publication admission mismatch. Later pilots examined early
buyer rejection and pressure relief that could prevent refinement. The exact
source hashes and settings explain those historical runs; current source does
not retroactively repair their results. Confirmation uses fresh seed identities
600 through 607 after the final protocol is frozen.

## Artifacts, transport and reproducibility

Each arm/seed JSON contains its settings, tape digest, all-step trace, per-phase
and per-source rows, monetary accounting, work totals and native graph counts.
`run.json` records completion and elapsed wall time. `manifest.json` records the
source version and arm contracts; failed runs retain `failure.json`.

The first seed also saves connected `.gojo` execution graphs. Numeric transport
preserves carrier state, aliases and the currently retained native evidence.
JSON is an inspection projection with a different contract.

`archive-check.json` records the checks actually performed. Signature agreement,
currency agreement and shared gate aliases establish a state/structure
roundtrip. Any additional publication or decision replay must be identified
separately. Opaque ordinal samples require an external comparison instrument;
the world metric dictionary is not serialized inside model graphs. Replay on
retained anchor classes through a declared ordered fixture does not establish
future comparison behavior on arbitrary novel physical samples.

Host arm settings and the scheduler remain necessary to resume execution. A
saved native model is not a complete archived world, renderer, source-code
runtime, or population history. Native graph counts are structural inspections,
not measurements of compressed storage or active execution work.

## Research motivation

[Oudeyer, Kaplan and Hafner](https://www.pyoudeyer.com/ims.pdf) distinguish
exploration driven by prediction error from exploration driven by learning
progress within similar situations. Unpredictable noise can attract an
error-maximizing agent without improving its capabilities. That motivates the
noisy-source control and a future context-matched progress mechanism; the
current gate implements neither their complete algorithm nor its developmental
claims.

[Hay and colleagues](https://arxiv.org/abs/1408.2048) examine selecting
computations by their expected improvement to a later decision. This motivates
testing whether paid refinement is useful and stopping when a coarse service
suffices. The authored opportunity periods here do not establish optimal
metareasoning or inherit that paper's guarantees.

[Wolpert, Tumer and Swanson](https://ntrs.nasa.gov/citations/20010071848) study
how private utility contracts influence collective outcomes. Their work
motivates making local payments and contribution assumptions explicit. This
study's success-paid rule has no counterfactual marginal-contribution estimator.

The experiment can establish particular useful interpretations, work savings,
retention effects and funding failures under supplied local laws. Its bounded
grammar, four initial services and authored consumer do not establish discovery
of arbitrary service hierarchies, open-ended agency or general intelligence.

## Protected candidate trials (opt-in)

The separate `--seed-trial` mode gives a structural candidate one finite startup
allowance, then requires later candidate work to use service-earned fees. The
incumbent continues to serve while a candidate gets 16 paid training experiences.
Both models are then frozen for 32 new purchased validation experiences, with
both predictions captured before each outcome. The paired law requires at
least 24 known candidate reads and positive conditional benefit after all trial
work. Open reads stay unknown; replay comparisons create no physical income.

Seed energy is transferred from conserved buyer capital, capped at 320 per
source for one initial trial. Unused seed cannot fund retries. The authored
candidate grammar is bounded to four ordinal anchors. The existing provider,
gate, world and old runners retain their behavior when the option is absent.

The [confirmation report](SEED-REPORT.md) documents the mixed result: all eight
common candidates became useful and all noise candidates were refused, but the
existing error controller earned more overall. The admission contrast was tiny
and inconclusive; rare and later useful trials remained unfinished. Start with
the [offline review](seed-review.html) or [protocol](SEED-PLAN.md).

### Run a fresh trial

From the repository root, use an empty output directory:

```powershell
dotnet run --project experiments/UtilityEcology -c Release -- --seed-trial --pilot --phase-length 512 --output experiments/UtilityEcology/results/my-seed-pilot
```

The pilot defaults to seeds900/901. The frozen eight-seed confirmation settings
can be reproduced under a new directory:

```powershell
dotnet run --project experiments/UtilityEcology -c Release -- --seed-trial --campaign --seed-start 1000 --seeds 8 --phase-length 512 --output experiments/UtilityEcology/results/my-seed-confirmation
```

This executes all five declared controllers on the same tape within each seed.
Prices and trial settings are fixed in this study, not extra command options.
Run `--seed-trial --help` for its supported arguments. Keep production sources
unchanged during execution; preserve the manifest and exact source snapshot.

Analyze and build a review of a completed run:

```powershell
python experiments/UtilityEcology/Analyze-SeedTrials.py results/my-seed-confirmation --snapshot
python experiments/UtilityEcology/Build-SeedReview.py results/my-seed-confirmation
```

The analyzer refuses incomplete or inconsistent artifacts and checks matching
source bytes, tapes, paid trial records and currency identities. It writes
`analysis.json`, `SUMMARY.md` and an optional exact-source/supporting-file ZIP
inside the run directory. Supporting files are marked as captured at analysis
time; an existing pre-run freeze record and protocol are preserved.

The review generator writes `experiments/UtilityEcology/seed-review.html` using
the selected run's audited results and first seed's traces. It does not replace
the older `review.html` or `progress-review.html`. Open it directly in a browser;
all data are embedded, with no server, network requests or model calls needed.
Use `--output PATH` to keep several review pages. The current saved page shows
`results/seed-confirmation-01`.

### Return to previous modes or remove the opt-in implementation

Omit `--seed-trial` to run the original ecology study. Use `--progress` instead
to run the previous progress study; its frozen setting remains
`--retest-period 8`. Selecting an earlier mode requires no file changes.

A saved pre-edit entry point and change manifest live under
`backout/seed-trial-20261012/`. For a checked code rollback, first preview:

```powershell
./experiments/UtilityEcology/Backout-SeedTrial.ps1
```

The script checks every recorded current and backup hash and verifies that
targets stay inside this repository. It refuses later edits instead of
overwriting them. To apply the listed operations:

```powershell
./experiments/UtilityEcology/Backout-SeedTrial.ps1 -Apply
```

This restores the saved entry point and removes the recorded added trial code
and tests. Reports, results and unrelated work are retained. The script requires
the completed `changes.json`; the saved baseline alone is not a changes list.
The rollback fixture and full test evidence are under `verification/`.

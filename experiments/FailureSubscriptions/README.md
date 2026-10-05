# Failure-triggered subscriptions

A small Gojo experiment in buying future help at the point where a seeker's
exported prediction fails. The [protocol](PROTOCOL.md) fixes the structure,
costs, controls and first campaign before its results.

The [first campaign assessment](ASSESSMENT.md) records useful connection reuse,
the accuracy/cost tradeoff, and a substantial failure to stop buying harmful help.
All 24 original runs are retained under `results/first-campaign`.

The current extension follows the [local review and duration protocol](ADAPTIVE_PROTOCOL.md).
Recent signed evidence on a connection can suppress repeated shopping; public
source reviews are a fallback when the buyer lacks its own observations. Paid
trial outputs need not replace the seeker's own answer. Source offers distinguish
supported opportunities from one exploratory extension, and buyers may accept a
shorter duration. These are empirical commitment rules, not confidence intervals
or discovered knowledge of cycle phase.

Four nearby numerical recipes offer continuation, inversion, changing slope and
zero change. The seeker already predicts the latest difference. One recipe
duplicates that self-prediction to expose payment for redundant help. Actors see
only incoming observations and their own locally available evidence.

Subscriptions cover one or five future input opportunities. At most two are
active; a search reads at most two offers. Each prediction is committed before
its target arrives. Conditional prospective offers retain helpful, resolved and
unknown counts, signed relief, and the initiating failure context. Their projected
aggregate benefit is an estimate, not a promise of consecutive successes.

Live publications, receipts, subscription intervals, support, payments and
comparison witnesses are native Gojo structure. C# schedules the supplied policy
and observes it. Neither recipe discovery nor attention/genetic training is
implemented. Native policy/price/cost sites expose a possible future training
surface. Dormancy preserves connections and bounded evidence; a revived output
requires a new current calculation. No copy-sale or free primitive is introduced.

## Run from the repository root

```powershell
dotnet build experiments/FailureSubscriptions/FailureSubscriptions.csproj -c Release
dotnet experiments/FailureSubscriptions/bin/Release/net10.0/FailureSubscriptions.dll --checks-only
dotnet experiments/FailureSubscriptions/bin/Release/net10.0/FailureSubscriptions.dll --long-campaign --steps 1025 --output experiments/FailureSubscriptions/obj/comparison
```

The long comparison runs 24 matched trials. The command above uses 1,025 arrivals
per run (24,600 total). Omitting `--steps` uses 4,097 per run. It compares the
seeker alone, original attention with fixed five, revised attention with fixed
five, and revised attention with adaptive offers capped at eight. Three scenarios
and two seeds use the same source order. The output directory must be empty.
`--campaign` retains the original short policy matrix for additional comparisons;
the saved first-campaign evidence belongs to its original source/build commit.

A custom run can be paused after a bounded number of arrivals and continued:

```powershell
dotnet experiments/FailureSubscriptions/bin/Release/net10.0/FailureSubscriptions.dll --output experiments/FailureSubscriptions/obj/custom --scenario recurring --seed 101 --horizon 2 --attention 1 --adaptive-horizon 1 --steps 10000 --window 0 --checkpoint 1000 --profile 0
dotnet experiments/FailureSubscriptions/bin/Release/net10.0/FailureSubscriptions.dll --resume experiments/FailureSubscriptions/obj/custom --steps 10000
```

Resume requires the original source, build, protocol and manifest. It restores
native current state, subscription deadlines and pending forecasts. Checkpointed
report offsets discard trailing uncommitted bytes; a report shorter than its
checkpoint is rejected. Ctrl+C checkpoints after the current arrival. Exclusive
writer locks prevent two runners changing one directory.

`--horizon` is the buyer's maximum request (zero disables assistance), not a
provider's compulsory duration. Current provider ladders cover one to eight
opportunities. `--attention 0` restores the original selection rule;
`--adaptive-horizon 0` uses fixed terms. Delivery prices remain supplied: a shorter
term quotes fewer deliveries at the same unit fee, not an invented bulk discount.

`--window 0` keeps aggregate reports without observation windows. `--profile 0`
omits per-arrival timing/allocation instrumentation; zero timing fields then mean
unmeasured. Checkpoint inspection still reports retained graph size and snapshot
cost. `--checkpoint` sets the save interval. Resume preserves those settings.
Default funding is a finite 1,048,576-credit pool; `--resource-budget` changes it
explicitly. Inspect the remaining pool on very long runs: this runner does not
automatically replenish it or infer economic value from successful predictions.

## Evidence and reading boundaries

- `SubscriptionNetwork.cs`: native operational structures and supplied scheduling
  of their local reads, paid publications, contracts and feedback.
- `ProspectiveOffer.cs`: conditional empirical offer surfaces, captured quotes
  with residue and explicit unknown coverage.
- `DurationOffer.cs`: position-specific evidence, supported/trial extent,
  shortened requests and captured prices.
- `AttentionChecks.cs` and `DurationOfferChecks.cs`: the revised contract checks.
- `Checks.cs` and `ProspectiveOfferChecks.cs`: causal, financial, connection and
  transport checks. The checks require no particular campaign winner.
- `summary.json` and sparse `windows.jsonl`: observer totals and current reads.
- `latest.checkpoint.zip`: one native continuation and external input/report cursor.
- `latest-checkpoint-inspection.json`: retained graph size and checkpoint overhead.

Whole-system expense is drained work, inspection, activation and cancellation.
Provider delivery fees are internal transfers. Relief of the single precommitted
export is the actual improvement over the self-prediction; summing every trial
source's counterfactual relief would overstate it. A paid output can remain
unresolved or pending. Direction matches are display diagnostics only.

Timing/allocation counters separate the native step, report aggregation/frame
release, and checkpoint inspection. Modeled prices do not measure CPU cost.
Bounded local search does not establish a runtime suitable for millions of actors.
Reports also expose paid trial deliveries/fees, retained self answers, suppressed
shopping, duration histograms, canceled remaining opportunities and peak retained
graph size at checkpoint inspections. Source recipes are still supplied; no
attention training, copy sale or permanently free learned primitive is implemented.

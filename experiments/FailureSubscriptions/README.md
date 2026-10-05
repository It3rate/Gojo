# Failure-triggered subscriptions

A small Gojo experiment in buying future help at the point where a seeker's
exported prediction fails. The [protocol](PROTOCOL.md) fixes the structure,
costs, controls and first campaign before its results.

The [first campaign assessment](ASSESSMENT.md) records useful connection reuse,
the accuracy/cost tradeoff, and a substantial failure to stop buying harmful help.
All 24 original runs are retained under `results/first-campaign`.

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
dotnet experiments/FailureSubscriptions/bin/Release/net10.0/FailureSubscriptions.dll --campaign --output experiments/FailureSubscriptions/results/first-campaign
```

The campaign runs 24 matched trials of 241 arrivals (5,784 total). It compares
self alone, one-opportunity help, five-opportunity subscriptions, and the same
subscriptions using prospective evidence, on three scenarios with two seeds and
source rotations. The output directory must be empty.

A custom run can be paused after a bounded number of arrivals and continued:

```powershell
dotnet experiments/FailureSubscriptions/bin/Release/net10.0/FailureSubscriptions.dll --output experiments/FailureSubscriptions/obj/custom --scenario mixed --seed 11 --horizon 5 --prospects 1 --rotation 0 --steps 80
dotnet experiments/FailureSubscriptions/bin/Release/net10.0/FailureSubscriptions.dll --resume experiments/FailureSubscriptions/obj/custom --steps 161
```

Resume requires the original source, build, protocol and manifest. It restores
native current state, subscription deadlines and pending forecasts. Checkpointed
report offsets discard trailing uncommitted bytes; a report shorter than its
checkpoint is rejected. Ctrl+C checkpoints after the current arrival. Exclusive
writer locks prevent two runners changing one directory.

## Evidence and reading boundaries

- `SubscriptionNetwork.cs`: native operational structures and supplied scheduling
  of their local reads, paid publications, contracts and feedback.
- `ProspectiveOffer.cs`: conditional empirical offer surfaces, captured quotes
  with residue and explicit unknown coverage.
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

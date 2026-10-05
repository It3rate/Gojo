# Local service growth

An exploratory Gojo make-or-buy supply chain with bounded local discovery and
public active/qualified connection counts. The [protocol](PROTOCOL.md) was committed
before implementation trials; its final section freezes the executable policy
before the first campaign.

The fine supplier exposes its current prediction through a live port. An
intermediary pays to use that port, publishes a coarser result, and can pay to
construct an independent candidate from available raw input. Later outcomes
support promotion; promotion releases the original supplier connection. Both
the original and replacement can be bought by neighboring consumers. Public use
is counted per connected client, with no transaction-count inflation or currency
reward for popularity.

The recipe, prices, cost schedule, neighborhoods, funding and candidate threshold
are supplied. Native relations carry the interpretations, receipts, comparisons,
payments, support and discovery state. C# schedules this finite fixture; it does
not discover arbitrary recipes or establish general economic value.

## Run

From the repository root:

```powershell
dotnet build experiments/LocalServiceGrowth/LocalServiceGrowth.csproj -c Release
dotnet experiments/LocalServiceGrowth/bin/Release/net10.0/LocalServiceGrowth.dll --checks-only
dotnet experiments/LocalServiceGrowth/bin/Release/net10.0/LocalServiceGrowth.dll --campaign --output experiments/LocalServiceGrowth/results/first-campaign
```

The frozen campaign creates 21 separate runs totaling 7,560 observations. An output
directory must be empty. A smaller custom run and continuation are also supported:

```powershell
dotnet experiments/LocalServiceGrowth/bin/Release/net10.0/LocalServiceGrowth.dll --output experiments/LocalServiceGrowth/obj/custom-run --seed 11 --learning 1 --support 1 --direct 1 --steps 120
dotnet experiments/LocalServiceGrowth/bin/Release/net10.0/LocalServiceGrowth.dll --resume experiments/LocalServiceGrowth/obj/custom-run --steps 240
```

Resume requires the original source, build, protocol and manifest. It restores
live Gojo structure and pending forecasts, rather than replaying a report table.
The checkpoint records its report offset; trailing uncheckpointed rows are removed,
and a report shorter than that offset is rejected. A writer lock prevents two
runners updating the same directory. Ctrl+C checkpoints after the current arrival.

## Inspect

- `LocalDiscovery.cs`: connected advertisements, distinct client contributions,
  native count deltas and bounded neighborhood traversal.
- `ServiceNetwork.cs`: live paid dependencies, interpreted outcomes, candidate
  construction, promotion, capacity and resource transfers.
- `DiscoveryChecks.cs` and `Checks.cs`: structural contracts, including repeated
  registration, source perturbation, missing data, native continuation and release
  of finished diagnostic frames.
- `windows.jsonl`: sparse 60-arrival observer windows with requested-read errors,
  payment and search counts. These never feed selection.
- `summary.json`: accumulated measurements and the final current native state.
- `latest.checkpoint.zip`: one replaceable native continuation plus host source
  cursor and reporting totals. It contains pending predictions, not sample history.
- `latest-checkpoint-inspection.json`: retained carrier/relation counts, checkpoint
  size and the time/allocation for checkpointing plus graph inspection.

Consumer purchases can exceed evaluated paid outcomes at the end because the last
prediction is still pending; missing immediate outcomes also remain unresolved.
Whole-system cost is actual drained operating, contact, construction and shadow
work. Fees between participants are transfers and must not be counted twice.
Gross benefit is a supplied utility calibration, not money and not a confidence
estimate. Native-step timing includes local calculation witnesses; report timing
includes frame release. Checkpoint timing is measured separately.

No earlier experiments or kernel APIs are changed by this experiment.

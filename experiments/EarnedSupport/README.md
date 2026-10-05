# Earned support

An experimental native recurrence over the unchanged negotiated-port market.
Useful outcomes can fund subsequent work. The recurrence, fractional residues,
issuance, and money accounting live in connected Gojo structure. Scheduling,
external signal generation, checkpoint files, and report aggregation are host work.

[PROTOCOL.md](PROTOCOL.md) freezes the law, controls, and intended observations.
It was committed before campaign results. [ASSESSMENT.md](ASSESSMENT.md) records
the first results and limits. No previous experiment, demo, or core law is replaced.

```powershell
dotnet build experiments/EarnedSupport/EarnedSupport.csproj -c Release
dotnet experiments/EarnedSupport/bin/Release/net10.0/EarnedSupport.dll --checks-only
dotnet experiments/EarnedSupport/bin/Release/net10.0/EarnedSupport.dll --campaign --output experiments/EarnedSupport/obj/my-campaign
```

The frozen campaign contains sixteen trials of 1,440 arrivals. Use a new empty
directory. Nothing uses a network service or UI while running.

For a custom run and continuation:

```powershell
dotnet experiments/EarnedSupport/bin/Release/net10.0/EarnedSupport.dll --output experiments/EarnedSupport/obj/my-run --seed 11 --scenario periodic --gain-numerator 1 --gain-denominator 2 --steps 1440
dotnet experiments/EarnedSupport/bin/Release/net10.0/EarnedSupport.dll --resume experiments/EarnedSupport/obj/my-run --steps 1440
```

Scenarios are `periodic`, `flat-return`, and `random-sign`. Gain is an explicitly
supplied reward exchange policy, not a currency-denomination change. `--steps` on
resume means additional arrivals. Resume keeps the original source, build, protocol,
seed, scenario, gain, window, and checkpoint contract. Even equivalent source with
different bytes/builds may require a new run; automatic migration is not implemented.

Defaults: windows every 120 arrivals, checkpoints every 240. `--window` and
`--checkpoint` can set custom positive intervals on a new custom run. A stopped or
finished run flushes a partial window and replaces its current checkpoint. Ctrl+C
finishes the current arrival and saves. A process crash resumes from the last
completed checkpoint, discarding uncommitted trailing report bytes; it refuses to
pad a report that is shorter than the committed checkpoint. An exclusive directory
lock prevents simultaneous writers.

Each directory contains:

- Immutable manifest: source/build/protocol hashes and supplied policy/fixture.
- `windows.jsonl`: sparse observer summaries, never inputs to the native learner.
- `latest.checkpoint.zip`: the connected native world plus separate external
  generator position and cumulative observer counts.
- `latest-checkpoint-inspection.json`: graph size and checkpoint cost.
- `summary.json`: cumulative metrics and the last live balances/trace states.

The completed market/support frame is released after reporting; operational
equations, dynamic sites, support traces, signed residues, settlement boundary,
and outstanding predictions remain. This is not an archive of past observations.
Report windows accumulate on disk; there is currently no report rotation.

Interpret the measurements carefully:

- Direction and source-difference errors use separate consumer bases.
- Gross valuation is supplied at four credits per avoided error unit. It is an
  explicit laboratory goal, not an inferred universal value measure.
- Service outlay is internal payment turnover. `Spent` is actual modeled contact
  plus operating expenditure; adding both would double-count supplier payments.
- Native elapsed time and allocation include the support/market calculations.
  Observer costs include two aggregate reads and conservation inspection per
  arrival; checkpoint inspections are separate. Allocation is cumulative churn,
  not peak retained memory or the minimum computation needed in principle.
- A positive trace is deferred backing: it can pay during later flat observations.
  Missing outcomes neither decay nor pay. The issued counter is an accounting
  mirror, not an extra wallet.
- Support enters the shared reservoir after current admission. It is available
  from the next arrival, still constrained by the old funding, wallet, and service
  capacity limits. Supply growth alone cannot enlarge those capacities.

The old selection and price laws remain intentionally visible: an affordable
challenger can replace an incumbent without a comparative advantage; quotes can
fall but cannot rise. This experiment cannot establish competitive equilibrium,
efficient allocation, or self-regulating purchasing power on those foundations.

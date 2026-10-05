# Offline native market campaigns

This is a bounded, unattended runner for the existing negotiated-port world.
It retains a continuous native learning state and runs without a viewer or
network. The supplied three providers and four offers are experiment fixtures;
this is not yet autonomous population growth or a general training system.

Build once from the repository root:

```powershell
dotnet build experiments/MarketTraining/MarketTraining.csproj -c Release
```

Then run the built program directly, with no build, restore or network step:

```powershell
dotnet experiments/MarketTraining/bin/Release/net10.0/MarketTraining.dll --output experiments/MarketTraining/obj/my-run --seed 11 --steps 480 --window 40 --checkpoint 120
```

Defaults are seed 1, 240 arrivals, 40-arrival reporting windows and checkpoint
every 120 arrivals. `--steps` is bounded to 1..1,000,000 for each invocation.
The default directory is `experiments/MarketTraining/obj/local-run`; choose a
new directory for each independent run. A second writer cannot open the same
run directory concurrently.

Press Ctrl+C to finish the current native admission, flush the partial reporting
window, save the native checkpoint, and exit. Resume explicitly:

```powershell
dotnet experiments/MarketTraining/bin/Release/net10.0/MarketTraining.dll --resume experiments/MarketTraining/obj/my-run --steps 240
```

Resume adds the requested number of arrivals to the checkpoint position. It
keeps the original seed, window and checkpoint settings and refuses a changed
source/build, protocol or generator version. It restores native carriers,
relations, attached routes, pending obligations and mutable state, rather than
reconstructing the learner from report values. Runtime process identities are
fresh; within-world aliases and topology remain preserved.

Resume compatibility is deliberately strict: a different build or modified
source checkout can be rejected even if a human expects it to be equivalent.
This first runner does not migrate checkpoints between implementation versions.
Committed manifests and window logs preserve exact bytes because checkpoint
hashes and recovery offsets refer to them.

One atomically replaced `latest.checkpoint.zip` contains `native.gojo` and
external generator/report continuation state. The metrics file byte offset is
committed with it. After an abrupt stop, resume discards uncommitted trailing
window rows and repeats from the last saved native state. A metrics file shorter
than the checkpoint's committed offset is an error. An abrupt stop may lose
up to one checkpoint interval of progress; orderly Ctrl+C saves current progress.
This is a local file checkpoint, not a distributed or transactional database.

## What persists, and what does not

- `manifest.json`: frozen seed, settings, policy/generator identifiers,
  protocol hash, source and executing assembly fingerprints.
- `windows.jsonl`: sparse incremental aggregate windows, plus current prices
  and resource balances. Partial termination/checkpoint windows are explicit.
- `latest.checkpoint.zip`: one restorable native world and external run state.
- `latest-checkpoint-inspection.json`: latest retained graph counts, checkpoint
  time/allocation and file size, separately from native admission measurements.
- `summary.json`: current cumulative counters and outstanding obligations;
  replaced at orderly termination, not an input to learning.

No full replay frame list is retained. After reading current evidence for the
report, the runner keeps
`Group(operational structure, pending obligations, open, version)`.
Operational equations and publication relations keep their identities. Current
pending promises, frozen prices and interpretations remain available for their
next outcome. Completed diagnostic evidence is deliberately released; aggregate
counts remember missing outcomes as unresolved but cannot reconstruct their
discarded detailed witnesses. This reduces retained memory and checkpoint size;
the native admission still constructs the same arithmetic and witness structure.
The learner and cumulative report accumulators have bounded retained state;
`windows.jsonl` still grows by one compact record per reporting window on disk.
There is no automatic log rotation in this first runner.

Observer counters, fixture names, seed and future environment are outside the
native learner. They never influence its prices, selections, transfers or
feedback. No host arithmetic replaces a native learning calculation.

## Reading the measurements

Consumers are always ordered as direction and source-change. Their error ticks
use different bases and must not be added together. Error totals include both
paid service and the zero/no-service outcome over evaluated, demanded
observations. Paid-only outcomes and their unresolved obligations are counted
separately. Divide purchases by readable demanded opportunities for service
coverage; divide retained paid outcomes by evaluated paid outcomes for the
retention proportion. Current purchases need their next observation before
they can be evaluated, so outcome counts lag by one.

`SameSupplierConsecutiveServices` means the same route provided consecutive
paid observations. It does not prove the attachment stayed continuously active:
evaluation can detach it and a new negotiation can reconnect it. Switches and
releases compare the preceding issued paid route with the current service,
including routes withdrawn during evaluation.

Outcome counters are attributed to the arrival that evaluates the previous
promise. `SuppliedPhases` counts current arrivals. At a phase boundary those are
different: the first new-phase outcome evaluates a promise issued in the prior
phase. Reports do not retrospectively relabel that outcome as a same-phase test.

`Contacts` includes attempts without enough contact credit. `PaidContacts`,
quote rejections, capacity blocks, unfunded contacts and failed paid contact
expense distinguish attempted discovery from service delivery. A quote can be
both too high and capacity blocked; those counts are not disjoint.

Requested inlets of 5 and 8 credits differ from the amounts actually admitted
through native transfer witnesses. `DemandedWithSpendableCredit` reads a
positive consumer balance before its current contact/service expenses; it does
not imply a desired service was affordable. Quiet means no paid contact or
service during that observation. Neither quiet nor an empty reservoir proves
stability. The initial 4,096 credits are finite; no new energy is minted between
phases. Provider reserves can remain inaccessible to consumers after exhaustion.
`NoContactAttemptObservations` separately counts absence of discovery attempts,
including unpaid attempts that continue after wallets empty.

The external generator starts at level 100 **before** its first arrival and
applies the current increment before delivering each sample. Six 40-arrival
blocks form each cycle, with deterministic seed/position jitter and a missing
observation at zero-based cycle position 119. The missing sample still advances
the underlying environment level. Native markets receive only current samples,
current demand and funding requests, never block labels or future samples.

Native admission wall time/allocation, observer wall time/allocation, retained
graph size and checkpoint serialization are distinct diagnostics. Allocations
are process measurements, not live memory, economic resource credits, or
hardware-independent costs. No garbage collection is forced per observation.

## Focused verification and potential-utility fixtures

```powershell
dotnet experiments/MarketTraining/bin/Release/net10.0/MarketTraining.dll --checks-only
dotnet experiments/MarketTraining/bin/Release/net10.0/MarketTraining.dll --utility-probe --output experiments/MarketTraining/obj/my-utility-probe
```

Checks cover native-equivalent frame release, checkpoint continuation through
missing outcomes, generator continuity and reporting denominators. The separate
utility fixtures use authored comparable prices/costs to test whether the
unchanged local policy exhibits price and value preferences. They do not train
or alter the campaign population. See [PROTOCOL.md](PROTOCOL.md) for the frozen
first campaign and [ASSESSMENT.md](ASSESSMENT.md) for its interpretation.

# Composite emergence

Two simple direction streams jointly reveal a changing apparatus rule that
neither stream can reveal alone. A bounded Gojo learner discovers a relation,
publishes an opaque coarse service, and supplies a new action learner. A third
learner decides whether to pay for a fresh observation or reuse the service.

The source graph, native equations, sparse evidence, choices and operational
memory are authoritative. The host owns the external apparatus and scheduling.
No Jev calls are needed. See [ARCHITECTURE.md](ARCHITECTURE.md),
[PLAN.md](PLAN.md), [LITERATURE.md](LITERATURE.md), and [REPORT.md](REPORT.md).

## Run

From `D:\source\repos\It3rate\Gojo-phototaxis`:

```powershell
dotnet run --project experiments/CompositeEmergence -c Release -- --pilot
```

The output is a fresh timestamped directory under this experiment's `results`.
Open its `index.html` directly; the dashboard contains all data and works offline.
The runner prints the absolute path. Existing nonempty output directories are
refused to preserve prior evidence.

Repeat the frozen confirmation protocol with:

```powershell
dotnet run --project experiments/CompositeEmergence -c Release -- --campaign --output experiments/CompositeEmergence/results/my-confirmation
```

Use `--help` for individual budget/seed/cost options. Small runs are pilots;
their controller estimates can be unstable. The archived confirmation01 uses
eight fresh seeds200..207 with larger training and evaluation budgets.

## Inspect

The offline page has discovery/source/transfer/refresh controls, training
milestones, seed intervals, native graph descriptions and recorded decisions.
Private world references are hidden until requested. Recorded unsampled sensor
values are not presented as observations.

Artifacts include settings/source hashes (`manifest.json`), frozen result rows,
learning curves, traces, candidate evidence, consumer/manager ledger projections,
native `.gojo` archives and actual post-restore behavior checks. Load connected
`pipeline-0.gojo` or `pipeline-4.gojo` as one graph to preserve shared physical
port identities; independently restored roots do not share axes automatically.

Generate seed-paired analysis with Python's standard library:

```powershell
python experiments/CompositeEmergence/Analyze-Results.py experiments/CompositeEmergence/results/confirmation-01 --output experiments/CompositeEmergence/results/confirmation-01/analysis
```

Focused tests:

```powershell
dotnet test experiments/CompositeEmergence.Tests -c Release
```

The study demonstrates a bounded relational capability and tests its reuse.
It does not establish subjective comprehension, unrestricted program invention,
or automatic discovery of the supplied interface and learning hierarchy.

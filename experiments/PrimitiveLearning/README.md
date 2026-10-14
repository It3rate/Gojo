# Can small Gojo structures learn?

Four small learning services and a measurable composition task. All learned
models are native Gojo graphs. The external relay publishes observations and
actual consequences; it never writes learner memory. No Jev/API calls are made.

Start with [REPORT.md](REPORT.md) for findings, [ARCHITECTURE.md](ARCHITECTURE.md)
for what is computed, [PROTOCOL.md](PROTOCOL.md) for controls and pilot decisions,
and [LITERATURE.md](LITERATURE.md) for the research foundations and limits.

## Run

From the repository root:

```powershell
dotnet run --project experiments/PrimitiveLearning -c Release -- --pilot --output artifacts/primitive-learning/my-pilot
```

The default pilot has two seeds,640 training and256 held-out episodes per run.
It runs all four isolated learners and all16 integrated learning combinations.
Training/test are separate; test freezes the learned model slots and checks them.

The main campaign:

```powershell
dotnet run --project experiments/PrimitiveLearning -c Release -- --campaign --output artifacts/primitive-learning/my-campaign
python experiments/PrimitiveLearning/Analyze-Results.py artifacts/primitive-learning/my-campaign
```

The campaign defaults to eight seeds,1536 training episodes,768 test episodes,
and128 warmup episodes before query/program learning. The predictable flow
usually reverses. Compare a world where it usually repeats:

```powershell
dotnet run --project experiments/PrimitiveLearning -c Release -- --campaign --seeds 4 --persistence 0.95 --output artifacts/primitive-learning/my-persistent-comparison
```

Optional settings: `--seeds`, `--train`, `--test`, `--warmup`, `--persistence`,
`--query-price`, `--seed-start`, `--output`. Pilots use seed identities starting
at0; campaigns start at100 to separate them. The runner locates repository sources
from its executable or working directory, so Visual Studio launches from the build
directory also work. Relative output paths use the launch working directory;
use an absolute `--output` path to choose the same destination from any launcher.
Use a fresh output directory and let a run finish before rebuilding its project.
Existing saved data in that directory is replaced by a new run.

## See the results

Open the generated `index.html` directly: it is an offline dashboard with
embedded results, training curves, all learning toggles, shifts and a worked
episode trace. No server, network access or account is required.

For the recorded main study, the path is
`artifacts/primitive-learning/campaign-fresh/index.html`. Raw evidence includes
`manifest.json`, `results.json`, `results.csv`, `curves.json`, `trace.json`,
`run.json`, `archive-check.json` and native `.gojo` model archives. The manifest
records experiment-source hashes before evaluation. The analyzer writes
`analysis.json` and `analysis.md`, using seeds as replicates.

To explore the demo, start with the combined view and compare `A1P1Q1S1`
with `A0P0Q0S0`, always-query, and never-query. The four letters show which
learners could update during training. Use the primitive views to separate
answer coverage from correctness, and the world conditions to inspect frozen
transfer. The episode replay always shows the first seed's full controller;
it is independent of the result filters.

Analyze both recorded studies together:

```powershell
python experiments/PrimitiveLearning/Analyze-Results.py artifacts/primitive-learning/campaign-fresh artifacts/primitive-learning/persistent-fresh --output artifacts/primitive-learning/analysis-fresh
```

Inspect the saved query evidence:

```powershell
dotnet run --project experiments/PrimitiveLearning -c Release -- --inspect-query artifacts/primitive-learning/campaign-fresh/query.gojo
```

[GRAPH-COST.md](GRAPH-COST.md) separates active rules from retained audit
evidence. Its inspection helper reads transport tables without running or
changing the model:

```powershell
python experiments/PrimitiveLearning/Inspect-GraphCost.py artifacts/primitive-learning/campaign-fresh/program.gojo --kind program
```

## Verification

```powershell
dotnet test experiments/PrimitiveLearning.Tests -c Release
dotnet test Gojo.slnx
```

The diagnostic relay supplies independent episodes. It tests learned distinctions,
forecasts, query allocation and finite program selection; it does not establish
closed-loop swimmer behavior or unrestricted structure discovery. The16 candidate
programs and the learning laws are authored before training. Native archive tests
reconnect opaque comparison references through an explicit external instrument
fixture; a complete live world service is not contained in a model archive.

# Continuous learning and native lifecycle verification

This is the next focused diagnostic after `CompositeEmergence`. It separates
learning to retry from resource-driven retirement and funded replacement.
The original experiment and all historical results remain reproducible.

## Run

From `D:\source\repos\It3rate\Gojo-phototaxis`:

```powershell
dotnet run --project experiments/AdaptiveLifecycle -c Release -- --pilot
```

For the fixed eight-seed confirmation protocol:

```powershell
dotnet run --project experiments/AdaptiveLifecycle -c Release -- --campaign --output experiments/AdaptiveLifecycle/results/my-confirmation
```

Existing nonempty output directories are refused. Runs save settings, C# source
hashes, per-seed decision traces and evidence, first-seed native graphs, and a
deterministic lifecycle demonstration. No paid Jev calls are required.

Analyze a recorded run with Python's standard library:

```powershell
python experiments/AdaptiveLifecycle/Analyze-Results.py experiments/AdaptiveLifecycle/results/confirmation-01
```

The recorded confirmation is in `results/confirmation-01`; its `source.zip`
captures the executing C# sources and the four project files. It is a source
snapshot for the experiment, not a copy of every repository test project.

```powershell
dotnet test experiments/AdaptiveLifecycle.Tests -c Release
```

See [PLAN.md](PLAN.md) for the questions and control boundaries and
[REPORT.md](REPORT.md) for findings and the remaining gates for larger training.

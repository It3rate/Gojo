# Minds

Start with the [chronological experiment overview](../OVERVIEW.md)
for the completed studies across both `experiments/Minds/` and `experiments/`, their results
and conclusions. It distinguishes this notebook's recorded experiments from
the proposed roadmap and the later research.

The public notebook is at [gojo.inklingai.ca/minds](https://gojo.inklingai.ca/minds/).
The [experiment roadmap](docs/ExperimentRoadmap.md) places the work needed
immediately before each proposed experiment. It is the current suggested
sequence; the readiness review below retains the wider audit backlog.

Minds runs small experiments with connected Gojo systems and makes their
behavior visible. Its first example is two local estimators exchanging views.

The retained runs below were recorded in September 2026. They are preserved
experimental records, separate from later projects under `experiments/`.
Checking the runner against the current kernel does not replace those records;
use a new output directory for a new run.

Experiment 03 now tests predictions issued before observations arrive. Read the
[findings](docs/PredictionFindings.md) or open the
[prediction viewer](results/predictions/20260916-001834/index.html).
The fixed line rule wins on ramps and loses to hold-last on the alternating
and step sequences. Missing outcomes remain unscored. All twelve cases match
the independent exact calculation; this is parameter fitting, not learned
model selection.

```powershell
dotnet run --project experiments/Minds -- --experiment3
```

This writes a new directory under `experiments/Minds/results/predictions`, including the
protocol and tapes captured before execution, before/after event journals,
all outcomes, the exact reference and the viewer. An optional second argument
chooses an empty directory. `results/predictions/publication.json` explicitly
selects which captured run is published. Existing captures are not overwritten.

Experiment 02 has now run 24 feedback conditions in three fresh worlds each.
Read the [findings](docs/FeedbackLimitsFindings.md) or open the
[feedback-limits viewer](results/feedback-limits/20260915-230957/index.html).
All 72 runs matched the independent exact reference at the recorded events
and round observations. This checks the selected contract, not an advantage
over conventional arithmetic.

```powershell
dotnet run --project experiments/Minds -- --experiment2
```

This writes a new timestamped directory under `experiments/Minds/results/feedback-limits`.
An optional second argument chooses an empty output directory. Each run retains
both implementations, the case parameters and event tape. Publication uses
the explicitly selected directory in `results/feedback-limits/publication.json`;
it does not silently switch to the newest run. The
[selected rules](docs/FeedbackLimitsProtocol.md) were written before evaluation.

From the repository root:

```powershell
dotnet run --project experiments/Minds -- experiments/Minds/results/two-space
dotnet test Tests/Minds.Tests/Minds.Tests.csproj
```

Open [the step viewer](results/two-space/index.html), read
[the generated results](results/two-space/Results.md), or inspect
[the selected setup and interpretation](docs/TwoSpaceExploration.md).
The run also writes `trace.json` with exact fractions and decimal display values.
Pass a different output directory to retain another run for comparison.

## What runs

Five fresh instances of the existing two-space fixture explore half, full and
quarter responses, a different message schedule, and a selected response to
local reference observations. The driver uses the existing Gojo calibration,
publication, receipt and response readers. It imports the existing authored
fixture through `Gojo.Encoding`; participant meaning remains in Gojo structure.

The observer is also built from Gojo reads. It retains each estimate, their
average, disagreement, local changes, total movement and reference mismatch.
The average can remain unchanged while the parts move. Observations and action
evidence are connected through a Gojo timeline. The browser displays those
captured results, with a choice of paired rounds or individual events.

These are exploratory observations, not learned response policies or a claim
that the system permanently stops when the observation window ends. Movement
measures change in estimates, not physical energy. Automatic idling, learned
selection of responses and crystallization into a replacement equation remain
future work.

## Current priorities

Observe first. External decimal calculations are acceptable through declared
interfaces. Exact replay and compiler-level numeric guards are useful future
tools. Different results are a reason to inspect the conditions, not automatically
a failed experiment. Keep external inputs, authored policy and actual participant
responses distinguishable, and preserve surprising outcomes.

The [readiness review](docs/ExperimentReadiness.md) retains the broader research
backlog with these priorities applied. The [protocol template](docs/ExperimentProtocol.template.md)
is available for later confirmatory work.

`Minds -> Gojo.Encoding -> Gojo`, with a direct Gojo reference for structural
observations. Gojo does not depend on Minds. No kernel semantics are changed by
this experiment. Decimal conversion in the report is a one-way display adapter.

For solution-wide verification:

```powershell
dotnet test Gojo.slnx
```

To rebuild and check the static notebook from those retained records:

```powershell
node Visualizations/Gojo.Demos.Web/tools/build-minds.mjs
node Visualizations/Gojo.Demos.Web/tools/build-minds.mjs --check
node experiments/Minds/tools/check-publication.mjs
node experiments/Minds/tools/check-limits-viewer.mjs
node experiments/Minds/tools/check-predictions-viewer.mjs
```

The viewer checks execute the controls against captured data using a small DOM
facade. They do not render browser layout. Git preserves result-file bytes so
line-ending conversion does not invalidate the recorded protocol checksums.

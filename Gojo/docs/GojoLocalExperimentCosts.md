# Local experiment costs

Status: a measured local cost landmark for the equivalence-revision fixture,
not a benchmark of general ornament recognition or neural model training.
Measured on 20 October 2026. The source is
[EquivalenceCostProbe.cs](../../experiments/QualitativeFrieze/EquivalenceCostProbe.cs);
the recorded samples are
[local-costs.json](../../experiments/QualitativeFrieze/results/evidence-selection/local-costs.json).

## What runs locally

The C# command constructs native Gojo structure, selects among hypotheses,
executes acknowledged output and independently reads that output. The
[equivalence-revision entry point](../../experiments/QualitativeFrieze/EquivalenceRevisionExperiment.cs)
then writes SVG, HTML, diagnostics and a native evidence archive. Opening the
gallery displays saved results; it does not start a background computation or
run the C# engine in the browser.

The executable study is
[EquivalenceRevisionStudy.Build](../../experiments/QualitativeFrieze/EquivalenceRevisionStudy.cs).
Selection is in
[ProvisionalEquivalence](../../experiments/QualitativeFrieze/ProvisionalEquivalence.cs),
actual drawing runs through
[UnitArrangementNode](../../experiments/QualitativeFrieze/UnitArrangementNode.cs),
and the study publishes actual topology and actual stroke participation for
separate output reads. The cost probe uses these same implementations.

This benchmark makes no remote model calls, uploads no training data and uses
no GPU. It does not fit a policy. The fixture contains authored candidates,
publication schedules and context changes, so its timing is not evidence of
learned generalization.

## Reproducing the measurements

From the repository root:

```powershell
dotnet run --configuration Release --project experiments/QualitativeFrieze -- --equivalence-costs --repetitions 9 --output artifacts/equivalence-local-costs.json
```

To regenerate the inspected gallery separately:

```powershell
dotnet run --configuration Release --project experiments/QualitativeFrieze -- --equivalence-revision
```

The probe's timers start inside the process. SDK compilation and process
launch are outside the measured intervals, including when `dotnet run` builds
first. Avoid running the solution tests or another substantial workload at the
same time when comparing timings.

The recorded machine had an AMD Ryzen 7 5700G with eight physical cores and
16 logical processors, approximately 31.9 GiB of total physical RAM,
Windows x64 build 26200, .NET SDK 10.0.401 and runtime .NET 10.0.12.
These describe the measurement environment, not a hardware recommendation.

## Measured results

One construction sample rebuilds the entire study: four cases, ten stages,
eight selected-output executions and one additional exemplar execution. It
includes native candidate assessment, acknowledged output construction and
independent output reads. The retained study has 84,826 reachable native
participants.

| Phase | Samples | Median elapsed | Median aggregate process CPU | Median managed allocation |
| --- | ---: | ---: | ---: | ---: |
| Warm study construction and reads | 9 | 81.99 ms | 109.38 ms | 50.19 MB |
| Warm full native archive and verification | 3 | 629.36 ms | 796.88 ms | 139.46 MB |

The nine construction samples ranged from 61.03 to 108.25 ms. The first
construction in the process took 212.18 ms; it includes the first-use JIT work
encountered by the study. It is not a measurement of cold operating-system or
command startup. Tiered compilation, garbage collection and other process
activity can affect both first and later samples.

The archive phase operates on an already built study. It captures the complete
reachable evidence graph, serializes JSON, parses and restores it, verifies the
restored participant count, and compresses the JSON with smallest-size gzip.
Its first execution took 824.88 ms. The resulting archive was 9,233,608 bytes
of UTF-8 JSON and 743,779 bytes compressed. Neither phase includes HTML/SVG
generation, output file I/O or policy fitting.

The process's lifetime peak working set was 257,622,016 bytes, approximately
245.7 MiB. A separate 20 ms sampler observed a maximum private allocation of
265,478,144 bytes, approximately 253.2 MiB. These maxima cover both phases;
they are not per-stage memory requirements. Sampling can miss brief peaks.
During capture, the native study, serialized JSON, restored graph and
compression buffers can coexist. During a construction, the previous study
can remain reachable until the new one replaces it.

Allocation in the table is cumulative managed allocation, including temporary
objects that are collected. It is not live RAM consumption. Aggregate process
CPU includes runtime and sampling threads, so it can exceed elapsed wall time.
The sampler also introduces a small amount of measured overhead.

## What these results do and do not predict

The immediate observation is that full evidence capture and verification took
about 7.7 times the study construction time in this fixture. Saving every
intermediate world can therefore cost more than executing and reading it.
This does not justify discarding evidence needed by the selected read or an
explicit replay contract. It does justify choosing that contract deliberately.

Ten stages of this authored study are not ten independent training examples.
They share definitions, dependencies and deliberate revision histories. The
recorded JSON includes a normalization to 32,000 same-sized stages; that is
only arithmetic applied to this fixture. It must not be described as the
measured cost of 32,000 independent training episodes. New construction
families, larger graphs, wider search, more receivers and different policies
can change the cost substantially.

For a new workload, measure a batch of its actual episodes and record
generation, evidence queries, receiver execution, independent assessment,
policy updates and artifact retention separately. A useful planning estimate
is the measured cost per matching episode multiplied by the requested count,
with policy-training passes and evaluation runs added separately. Report the
workload and assumptions alongside the estimate.

## Infrastructure direction

Dataset generation and native structural search are CPU workloads in the
current experiment. Small policy fitting can also be a local CPU workload.
Large neural models are a separate decision: their training cost depends on
model size, sequence or graph size, batch size, optimization steps and the
evaluation schedule. A large number of examples alone does not establish a
GPU requirement or a need to rent cloud infrastructure.

For the next experiments, the useful infrastructure is a resumable local
runner: bounded batches, deterministic seeds, construction and policy version
references, progress counters, checkpoints and a stream of evaluation results.
That supports overnight runs and later movement to larger machines without
changing the native evidence contract. A saved seed alone is not a complete
replay archive; it also needs compatible executable definitions and retained
inputs, or the explicit qualification that only a coarser reconstruction is
promised.

Before expanding compute, prioritize these changes:

1. Stream compact training projections with correspondence back to their
   native inputs and outcomes. Retain full native archives for selected audit
   samples, counterexamples and replay checkpoints rather than every row.
2. Reuse stable executable programs and reader definitions across episodes.
   Keep source instances, observation events and actual output identities
   distinct. A new policy action need not reconstruct the whole four-case
   demonstration.
3. Use focused transport tests and periodic restoration checks to validate
   archives. Do not make complete serialize/restore/compress work a mandatory
   operation for every training decision unless the experiment requires it.
4. Profile bounded matching, query selection and local reads before adding
   caches. Any compiled index remains derived from native structure, with
   correspondence and revision dependencies preserved.
5. Add CPU workers after streaming and checkpoint recovery work. Measure their
   contention and memory use; the present probe does not demonstrate parallel
   scaling.

The independent output reader remains part of evaluation. Replacing it with a
candidate's intended label would make training cheaper by removing the check
the experiment is supposed to perform.

Move to hosted workers when an actual run exceeds the available local time,
memory or recovery budget, or when repeatable distributed experiments become
useful. Consider GPU infrastructure after selecting and profiling a model that
benefits from it. Current measurements support continuing small local
experiments; they do not establish the eventual cost of a general recognizer.

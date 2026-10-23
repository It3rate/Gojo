# Faithful reconstruction from budgeted Quick Draw evidence

Status: exploratory checkpoint, 23 October 2026. This follows the
[acquisition pilot](README.md) and the user's acceptance of its fifty drawings.
The [canonical Gojo prompt](../../Gojo/docs/GojoSystemPrompt.md) remains
authoritative. The historical [handoff](handoff.md) is background, not a claim
that the present pipeline already recovers concepts or intended ideal shapes.

The objective in this pass is to regenerate approximately **the observed
drawing**, including its orientation, aspect, acquisition order, pen lifts,
gaps and nonideal geometry. A left-facing crescent should remain left-facing.
An observed asymmetric triangle is not replaced by an ideal triangle. The
conversion is a data intake boundary for later work on interpretation, defaults,
completion, skill and error. It is not itself a model of those phenomena.

The implemented Python adapter extracts several nested evidence budgets and
trains a small local curve prior. The detached receiver gets quantized endpoint
and profile hints, plus learned coefficients. Private source samples and fitted
control points are retained only in the audit/evaluator path. No category name,
recognition response, country, source ID or ideal template is a model feature.

## Data and separation of roles

The fifty accepted drawings remain development/review fixtures. The three
earlier incomplete challenge drawings are reserved too. Neither the fifty
source IDs nor the fifty-three reserved geometry signatures enter training.

The bounded learning corpus contains 2,000 raw drawings: 400 each from square,
circle, triangle, octagon and moon. The sampler skips the first 2,000 source
lines per category used for the earlier curation, then takes the next 400
encoding-valid records. It does not filter by `recognized`, geometric quality,
expected shape, decoration or visual fit. In this run no candidates were
rejected. Consequently the uncurated moon stratum includes drawings beyond
plain crescents; the category is a sampling stratum, not geometric truth.

Each source has an audit row with its actual file, byte offset and length,
source line, source-line SHA-256, source ID, category stratum and partition.
The source files and training records stay under `D:\_quickDraw`.

Partitions use a deterministic hash of a coarse geometry-group signature:
eight equal-arclength samples per stroke quantized to 1/32 of the drawing's
maximum extent. Stroke order and direction remain part of that signature.
Duplicate source IDs are excluded; a reserved development signature is also
excluded. A geometry group can occupy only one partition. The resulting split
has 1,418 training drawings, 204 validation drawings and 378 test drawings.
All 2,000 group signatures happened to be distinct in this bounded corpus.

This grouping catches some translated, uniformly scaled or near-identical
traces. It is **not comprehensive near-duplicate detection**: a different
stroke order, direction or sampled outline can evade it. The bounded source
prefix and small number of strata also limit generalization claims. This is
not an estimate of the entire Quick Draw population.

There are three distinct review selections:

- The accepted fifty, excluded from model training and selection.
- Ten uncurated test sources, two per stratum, hash-selected independently of
  fit quality for a small native review. They do not replace evaluation on all
  378 test sources.
- Two deliberately selected failures: the unique source IDs attaining the
  worst held-out sampled RMS at each requested budget. Their manifest says
  explicitly that these are diagnostic failures, not an unbiased sample.

## Ordered extraction and nested budgets

The numerical boundary is
[faithful_pipeline.py](tools/faithful_pipeline.py), reusing the original
fitter's aspect-preserving normalization and cubic least-squares helper.

```text
exact acquisition retained separately
  -> chosen translation/uniform-scale observation basis
  -> source-ordered, nested stroke spans
  -> quantized endpoint/profile evidence
  -> detached packet + learned local parameter proposal
  -> native construction and independent output read

private audit:
  source spans, sample order/time, fit targets, residuals, refinement history
```

Normalization subtracts the drawing's minimum x/y and divides both axes by
the same maximum extent. There is no rotation, reflection, independent axis
scaling or canonical category pose. The original x-right/y-down convention
remains the presentation basis. The private audit retains the origin and
scale needed to relate the normalized read to the acquisition.

Each nonempty stroke initially contributes one span, including a one-point
stroke. Empty strokes remain empty. To increase a budget, the adapter splits
the current span with the largest sampled cubic residual at an interior source
sample; path length breaks equal-residual ties. Existing source boundaries are
never removed. Every new span records its parent and original source indices.
The retained budgets are 3, 6, 12 and 20 sections per drawing.

A requested budget is not permission to merge pen strokes. Its effective
minimum is the number of nonempty recorded strokes. Conversely, a drawing
cannot be split beyond its sample intervals. Both the stroke floor and the
sample-capacity shortfall are reported. In the test set the number of drawings
whose stroke count exceeded requested budgets 3/6/12/20 was 44/25/8/3. One
test drawing exhausted source intervals before budgets 12 and 20. Among the
accepted fifty, one eight-stroke example exceeds budgets 3 and 6, and one
example has only seventeen available intervals at budget 20.

Every section is represented by a cubic candidate in this first receiver.
Collinear controls can express a line; no transmitted category or degree tag
declares that a section is conceptually an edge or curve. This does not yet
learn the distinction between a noisy straight edge and an intended curve.

The segmentation is **offline**. It can inspect the entire recorded drawing
to choose a normalization and section boundaries. Preserving acquisition order
and forbidding references to unavailable constructions does not make this an
online predictor of the person's next pen movement or mental construction.

## The public numerical hint contract

A normalized drawing extent is 1,024 coordinate ticks. Section start and end
positions are rounded to those ticks. At one-quarter, one-half and
three-quarters of the observed section's arclength, profile offsets relative
to the published start are rounded to multiples of sixteen ticks. Thus
endpoint coordinates have 1/1024 resolution; interior profile offsets have
1/64 resolution. This is deliberately a **measured coarse profile interface**,
not a purely qualitative semantic description.

The JSON bridge is a transport adapter, not canonical meaning:

```text
packet envelope:
  id                       evaluator join only; not native evidence
  coordinateQuantum=1024
  ordered strokes
    ordered sections
      start=[x,y]
      end=[x,y]
      features=[1024, endDx, endDy,
                quarterDx, quarterDy, halfDx, halfDy,
                threeQuarterDx, threeQuarterDy]
```

The endpoint differences repeat information already present in start/end.
The constant feature is a declared unit; its learned coefficient is zero.
There are ten distinct geometric scalar fields per section before sharing
adjacent endpoints: two endpoint pairs and three profile pairs. A budget of
three sections therefore does **not** mean three scalar facts, three total
relations or three bytes. The JSON also carries repeated feature values and
array/field overhead. Expanded native audit archives are a different storage
product again.

No source point arrays, source timestamps, original IDs, category labels,
private fitted controls, fitted degree labels or teacher residuals are
imported as public hints. The separate `private-audit.ndjson` retains the
acquisition correspondence, exact source ordinal spans, source time endpoints,
normalized samples, private fit and refinement parents. The original raw
records remain available for complete timing and exact acquisition replay.

Nearby or equal positions do not by themselves weld sites. A section boundary
within a stroke corresponds to the actual shared acquisition event; a pen
lift preserves independent events. A source return to its starting coordinate
does not introduce a semantic closure relation. No synthetic construction
lines or inferred forward dependencies are introduced in this pass.

## Learned prior and fixed comparison

The learned target is the endpoint-fixed cubic least-squares fit to the
**observed** section, parameterized by its source arclength. Three-point spans
use an equivalent degree-elevated quadratic; lines, dots and degenerate spans
have explicit fallbacks. No regular polygon, circle, crescent, horizontal or
vertical template supplies target coordinates.

The fitted model has eight independent scalar coefficients: a four-input to
two-output map shared by x and y. Inputs in one axis are the endpoint and
three profile offsets. Outputs are the two cubic control offsets. Sharing
the same coefficients on both axes, with no intercept and no cross-axis
terms, is algebraically equivalent to training with all signed reflections
and x/y exchanges. It prevents the parameter model from learning an accidental
preferred handedness or screen direction. This property is tested directly;
it is not an inference about whether a person draws left-handed or right-handed.

All budgets of a source stay in its partition. Each training drawing and
budget has equal total fitting weight; its section occurrences divide that
weight. The 1,418 training drawings yield 59,309 section occurrences across
the four budgets. Those occurrences are correlated views of the sources, not
59,309 independent drawings.

Ridge values 0, 0.00001, 0.001, 0.01, 0.1 and 1 were compared using only the
204 validation drawings. The selected value was 0.01; nearby candidates were
almost tied. Neither the test split, accepted fifty, nor transfer fixtures
were used for coefficient fitting or parameter selection.

The resulting coefficients are quantized to integers at a scale of one
million. They are stored as four rows of nine values for the public feature
vector. For each control component the receiver computes

```text
offset = truncate_toward_zero(sum(weight[i] * feature[i]) / 1_000_000)
control = published_start + offset
```

Published endpoint coordinates remain fixed. Negative coefficients and signed
profile offsets are retained. The Python evaluator uses the same integer
truncation law as the native receiver. The model is a small learned parameter
proposal, not a learned uncertainty distribution or a classifier.

Three comparisons use the same source partition:

- **Fixed profile cubic:** ordinary endpoint-fixed least squares through the
  three published profile samples, quantized to the same coefficient grid.
  This is the meaningful untrained comparison using the same full hints.
- **Straight endpoint segment:** ignores the profile hints. It shows the cost
  of losing curvature and retracing, not a fair measure of learning alone.
- **Private fitted cubic:** uses the unavailable full source fit. This diagnoses
  the limit of the selected segmentation and grammar; it is not public receiver
  performance or a mathematical bound for every alternative metric.

## Measured results

The adapter metric compares each original sample with its generated section
at that source section's arclength parameter. Each actual acquisition sample
is counted exactly once, including distinct samples at equal coordinates.
Shared section boundaries are not counted twice. Errors are normalized by the
source drawing's maximum extent. Means below are the mean of per-drawing RMS
values, with each drawing equally weighted.

This is a sampled correspondence metric. It is not a global Hausdorff bound,
a perceptual score, a topology test or a measure of recovered intention.
Native output is evaluated separately so these numbers cannot substitute for
execution/readback verification.

### Untouched test partition: 378 drawings

All values in the following table are percentages of maximum source extent.

| Requested sections | Learned mean RMS | Fixed mean RMS | Line mean RMS | Private fit mean RMS | Learned p90 RMS | Worst learned sample |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 3 | 4.447785 | 4.477648 | 31.912977 | 4.213006 | 7.339532 | 101.333401 |
| 6 | 1.469419 | 1.478089 | 12.684702 | 1.247183 | 2.770722 | 110.235904 |
| 12 | 0.668435 | 0.676110 | 3.525474 | 0.371832 | 0.682482 | 26.716832 |
| 20 | 0.491464 | 0.500065 | 1.578511 | 0.163895 | 0.479933 | 26.326883 |

The improvement over the fixed full-hint cubic is modest: approximately
0.67%, 0.59%, 1.14% and 1.72% relative reduction in mean RMS. The learned
model beats that fixed comparison on 215/378, 209/378, 262/378 and 315/378
drawings respectively. It does not improve every drawing or every tail
statistic. These descriptive results do not establish a population-level
benefit or a calibrated probability of correctness.

### Accepted development fixtures: 50 drawings

| Requested sections | Learned mean RMS, % | Fixed mean RMS, % | Mean effective sections | Mean hint JSON bytes |
| ---: | ---: | ---: | ---: | ---: |
| 3 | 4.132166 | 4.205031 | 3.10 | 323.42 |
| 6 | 0.965454 | 0.972252 | 6.04 | 566.26 |
| 12 | 0.440749 | 0.451027 | 12.00 | 1040.64 |
| 20 | 0.391367 | 0.400569 | 19.94 | 1658.52 |

JSON byte counts cover the compact public packet body, excluding the external
source-ID envelope, model, original acquisition and private/native audits.
These are measured transport sizes, not a claim of entropy-optimal compression.
For the full test set the corresponding mean packet sizes were 382.13, 606.30,
1063.03 and 1675.30 bytes. The higher minimum stroke counts explain part of
the difference at small requested budgets.

### Frozen transfer: five more complex strata, 25 drawings

Five visually reviewed examples each of house, star, fish, envelope and umbrella
were selected independently of fit success by
[select_transfer.py](tools/select_transfer.py). The corpus preserves open
joins, asymmetry, multiple strokes, overlapping paths, curved parts and modest
detail. These are curated transfer fixtures, not representative prevalence
samples. The existing model is frozen; encoding this batch performs no training.

| Requested sections | Learned mean RMS, % | Fixed mean RMS, % | Learned p90 RMS, % | Worst learned sample, % |
| ---: | ---: | ---: | ---: | ---: |
| 3 | 6.564461 | 6.624837 | 9.645818 | 23.872630 |
| 6 | 1.930961 | 1.947333 | 3.052371 | 17.753936 |
| 12 | 0.554950 | 0.561341 | 0.651798 | 4.818442 |
| 20 | 0.426015 | 0.436977 | 0.478937 | 1.231894 |

One transfer drawing has four strokes, exceeding budget three. No requested
transfer budget exceeds the available sample intervals. Lower mean error with
additional evidence is useful, but does not demonstrate conceptual recognition
of houses, fish or umbrellas: the receiver never receives those names.

### Failures remain visible

The largest low-budget test failure is source `4941781475000320` in the
triangle prompt stratum: four strokes, 296 samples. At budget six the learned
RMS is 27.263368% and maximum sampled error is 110.235904% of source extent.
The private cubic fit itself has RMS 20.530547%. The current low-budget
partition/grammar is already inadequate before the sparse-hint model is asked
to reproduce it. Budget six is slightly worse than budget three for the
learned output in this case; greater evidence count is not a monotonic quality
guarantee under changing local fits and finite hints.

Source `6202587260911616` in the moon prompt stratum has thirty nonempty strokes.
It therefore retains thirty sections at every requested budget. Its learned
RMS is 9.315143% at budgets twelve and twenty, with a 26.326883% maximum sampled
error. The private fit's RMS is already 9.064982%. This exposes why preserving
pen lifts must remain separate from a nominal global section budget. Visual
inspection shows many small detail strokes consuming that floor while the
main crescent contours receive too few sections. Future allocation should
account separately for point-like events, path complexity and retained detail;
silently dropping those acquired events is not a faithful reproduction fix.

Both sources are retained in `failure-review`, selected explicitly because
they fail. They are not removed from aggregate results. No coordinate clipping
or hidden replacement with a category template makes their outputs appear
successful.

## Native execution and gallery

The receiver is implemented in [SequentialEvidence.cs](SequentialEvidence.cs)
and [NativeReconstruction.cs](NativeReconstruction.cs). It adds a measured
evidence interface built from ordinary native relations, carrier locations,
placeholder sites and closed scopes. It is not a new semantic shape taxonomy.

The native packet introduces its coordinate/order basis and origin, then
stroke events and section events in acquisition order. Each section exposes
an ordered `from -> to` passage, an anchor to the origin or immediately previous
endpoint, its start offset, endpoint displacement, and ordered profile readings.
Within a continuous stroke the next section shares the actual prior endpoint.
A pen lift, nonzero start offset, or return to equal coordinates remains a
distinct read rather than silently creating a contact. The reader rejects
future anchors, reordered events, opposed sites and incompatible units.

The current vocabulary deliberately permits only the origin/previous-endpoint
reference. Arbitrary earlier-site references and explicit construction-guide
introduction are **not yet implemented**. No imaginary guide is inferred or
referenced. Adding those later requires a prior introduction event and a
dependency on its actual exposed site; a category name or lookalike coordinate
must not make an unavailable line exist. Appending later instructions leaves
the earlier construction unchanged in a fixed basis, as tested. The full-trace
normalization and segmentation still remain offline.

At the adapter boundary, consecutive zero-offset sections in one stroke declare
that their boundary is the same acquired event. The extractor establishes
this from the shared source index. This is an input contract, not permission
to infer identity from coordinate equality in arbitrary data. Independent
touching traces require separate stroke inputs in this limited vocabulary.

Frozen coefficients become native `Equation.Sum` weighted ports followed by
the coefficient-scale projection. The whole sum is divided once; signed
truncation remainder stays in the prediction witness. These are non-draining
numeric observations, not carrier value assignments or material transfers.
Control offsets are explicitly translated into the drawing basis. Their
actual producer tick references become the generated curve controls; copied
equal coordinates do not establish that dependency. Each finite cubic has
the existing `TransformSurface.Interpolate` construction DAG, with introduced
operands and an ordered parameter interval. Its public section and model
citations remain reachable from the program.

[ReconstructionRunner.cs](ReconstructionRunner.cs) exercises the transport
boundary: serialize/restore the model, serialize/restore each evidence packet,
generate only from those restored roots, then serialize/restore the generated
program before exporting its controls for evaluation. Input sample arrays and
private fitted controls are unavailable to that receiver. Generic numbered
archive names and roots avoid a category-name dispatch; the external ID is
used only to join outputs to private evaluation sources.

This run verified **348 detached packet/program round trips**: 200 accepted
development, 40 hash-selected test, 100 transfer and 8 deliberate failure
packets. Every exported control matched the independent integer Python
equation result exactly. Expanded graphs remain under the corresponding
external `native` directory, alongside `model.gojo.json`, `decoded.ndjson`
and a node-count summary.

There is still a specific native boundary to complete: acquisition-to-fitted-span
correspondence, normalization, sample times and cross-budget parents are kept
in `private-audit.ndjson`, **not yet connected as native acquisition/fit
witnesses**. Public hints and their generated programs are native; the whole
source-to-fit audit is not. Exact source acquisition remains available from
the preceding importer. This limitation must not be hidden by calling an
external JSON annotation canonical provenance.

The independent [output review](tools/review_reconstruction.py) reads controls
from restored native programs. It samples each cubic into 64 segments and
compares 129 equal-arclength positions per corresponding nonempty stroke:

- Ordered arc RMS compares traversals at equal path fractions. It can expose
  reversal/retracing loss without mistaking cubic speed parameterization for
  geometric error. It does not measure recorded timing.
- Symmetric sampled RMS measures point-to-path distance in both directions,
  restricted to the matching stroke. It measures silhouette fit but can miss
  reversed traversal, so it is reported alongside the ordered read.

Both are per-drawing measurements with equal weight per nonempty stroke,
followed by an equal-drawing mean. No rotation/reflection alignment is applied.
They are sampled diagnostics, not certified bounds. The evaluator rejects
changed stroke/empty-event counts and noncontiguous within-stroke section
sequences, rather than inventing a segment across a gap. It validates one-to-one
source/output IDs. All current extracted section sequences are contiguous;
pen lifts and the gap between an open stroke's end and start remain intact.

| Requested sections | Accepted 50: ordered RMS, % | Accepted 50: geometric RMS, % | Transfer 25: ordered RMS, % | Transfer 25: geometric RMS, % |
| ---: | ---: | ---: | ---: | ---: |
| 3 | 3.406 | 2.518 | 7.213 | 3.610 |
| 6 | 0.919 | 0.660 | 2.489 | 1.269 |
| 12 | 0.501 | 0.334 | 0.682 | 0.415 |
| 20 | 0.593 | 0.304 | 0.577 | 0.325 |

The maximum endpoint quantization error was 0.0662% of extent among the accepted
50 and 0.0646% among transfer fixtures. The worst sampled geometric deviation
at 20 sections was 1.0651% and 1.0970% respectively; these maxima concern the
review fixtures, not the full test population. Native outputs retain the
original crescent facing, skew, open gaps and decoration in visually inspected
comparison sheets. Low budgets often smooth corners into visibly wrong curves.
The accepted-set ordered mean gets slightly worse from 12 to 20 sections:
finer partitioning with fixed coarse profile resolution can introduce extra
small bends and alter relative path length. Nested **source correspondence**
is guaranteed here; monotonic geometry or topology fidelity is not.

Open the local [review index](D:/_quickDraw/gojo-reconstruction/review.html).
Each gallery has a category filter, source overlay, budget selector and drawn
prefix slider. The slider exposes section order, not pen speed. The index
separates the approved fixtures, hash-selected held-out sources, transfer
fixtures, and deliberately selected failures. Static comparison sheets and
compact numerical summaries are also retained in [results/reconstruction](results/reconstruction).

## What this checkpoint establishes and leaves open

Implemented by the adapter and its focused tests:

- A label-blind, source-ordered extraction route with nested span correspondence.
- Detached coarse endpoint/profile packets and a separate private target audit.
- An actually trained, frozen, finite local curve prior with held-out evaluation.
- An equivalent fixed-profile comparison and visible difficult failures.
- Exact signed-reflection/axis-exchange behavior of the integer decoder.
- Explicit stroke floors, sample-capacity saturation, and retained empty/dot
  events in the extractor.
- Frozen inference on additional drawings without retraining or modifying the
  source model.

The current importer validator for training/frozen NDJSON expects nonempty
recorded stroke arrays; the lower-level extractor and native acquisition can
represent empty/singleton events. That adapter admission policy is not a law
that such acquisition states are invalid.

Still exploratory or unimplemented:

- Conceptual segmentation into sides, arcs, tips, joints and later construction
  uses; these are currently fitted local spans.
- Inference of intended vertical/horizontal/parallel relationships.
- Gap filling, dash interpolation, decoration filtering and deliberate closure
  alternatives.
- Learned uncertainty/error likelihoods and proposal diversity. Current residual
  distributions are descriptive evaluation, not a calibrated error prior.
- Handedness, skill, ability gap, intent or the original drawer's mental model.
- Explicit hypothetical construction scaffolds and references to earlier
  construction surfaces beyond the retained stroke continuity/order.
- Native acquisition-to-span witnesses and native cross-budget correspondence;
  those links currently live only in the private evaluator audit.
- A causal next-stroke model and a history-blind/order-aware learning ablation.
- Semantic compression that can replace multiple local profiles with a shared
  structural explanation while keeping its source correspondence and loss.

The compression ladder is useful data for that later work. It should not yet
be described as hallucination toward an ideal shape: the fit targets and
learned loss refer only to the observed traces. A later completion or
regularization experiment must declare its assumptions, keep the unmodified
observation available, and distinguish completed participation from observed
participation. Lower-resolution evidence remains a valid, limited read.

The next useful experiments, while keeping observed targets, are:

1. Compare adaptive profile resolution and section allocation at equal scalar
   or byte budgets. This will distinguish poor span allocation from lost hint
   detail and test why 20 sections can sometimes be worse than 12.
2. Add explicit native acquisition/span and coarse/fine witnesses. Preserve
   alternative fits, their source windows, quantization basis and residuals
   without exposing private targets to a receiver.
3. Replace a single local proposal with a small calibrated set of line/curve
   proposals. Evaluate held-out residual quantiles and coverage before calling
   them learned error priors. Keep observed deviations separate from inferred
   mistakes or decoration.
4. Add earlier exposed-site references with a strict introduction order; compare
   a history-aware predictor against shuffled-history and local-only baselines.
   Explicit hypothetical guides must appear before their consumers.
5. Only then test gap completion, dash interpolation and shared parallel/axis
   explanations as declared alternatives. Score preserved observations and
   inferred completion separately; ideal templates remain outside the present
   reproduction objective.

## Reproduce and inspect

Run from the repository root. Python 3.10 and Pillow were already available.
NumPy 2.2.6 was installed only in `D:\_quickDraw\gojo-python`; no system-wide
package install or additional dataset download was needed. Use
`--python-packages <path>` for a different local NumPy installation. Only
learning and the numerical comparison require NumPy; native execution uses
the saved integer coefficients.

```powershell
python -m pip install --target D:\_quickDraw\gojo-python numpy==2.2.6
python experiments/quickDraw/tools/faithful_pipeline.py --output D:\_quickDraw\gojo-reconstruction
python -m unittest discover -s experiments/quickDraw/tools -p 'test_*.py' -v
```

The default training run reads 400 records per category after a 2,000-line
prefix skip. `--source`, `--development`, `--challenges`, `--skip`,
`--per-category` and `--output` make these choices explicit on another machine.
The default output path must be chosen explicitly when retaining separate
experiments; rerunning regenerates that output's artifacts.

Frozen conversion of additional drawings:

```powershell
python experiments/quickDraw/tools/select_transfer.py candidates
python experiments/quickDraw/tools/select_transfer.py select
python experiments/quickDraw/tools/faithful_pipeline.py --encode-input D:\_quickDraw\gojo-reconstruction\transfer\selected.ndjson --model D:\_quickDraw\gojo-reconstruction\model.json --output D:\_quickDraw\gojo-reconstruction\transfer\run
```

Native reconstruction and review of any emitted packet directory:

```powershell
dotnet run --project experiments/quickDraw -- --reconstruct --input D:\_quickDraw\gojo-reconstruction\packets.ndjson --model D:\_quickDraw\gojo-reconstruction\model.json --output D:\_quickDraw\gojo-reconstruction\native
python experiments/quickDraw/tools/review_reconstruction.py --input D:\_quickDraw\gojo-reconstruction --native D:\_quickDraw\gojo-reconstruction\native --output D:\_quickDraw\gojo-reconstruction\review
dotnet test Gojo.slnx
```

For transfer or failure review, substitute that batch's packet directory for
`--input`, and use its `native` and `review` subdirectories. The native CLI takes
the packet file; the review tool takes its containing directory. Put the same
`model.json` beside a review batch to enable independent integer parity checking.

The focused checks include 14 new C# tests for native identity, chronology,
signed remainder, restoration and producer dependency; the full Python suite
contains 42 passing tests, including 8 independent hint/target boundary tests
and 8 independent metric tests. `dotnet test Gojo.slnx` passed all **5,343 tests
across 12 projects**, with no failures or skips. Detailed verification and
artifact hashes are recorded in
[verification.json](results/reconstruction/verification.json).

Expanded corpus and audit products remain external; compact models, summary
tables and static comparison sheets are retained in the repository.

| External path under `D:\_quickDraw\gojo-reconstruction` | Meaning |
| --- | --- |
| `corpus.ndjson` | Bounded raw learning inputs, not the entire source corpus |
| `corpus-manifest.json` | Source offsets/hashes, strata, geometry groups and partitions |
| `model.json` | Learned finite coefficients and training provenance summary |
| `fixed-model.json` | Untrained profile interpolation coefficients |
| `metrics.json`, `summary.json` | Full per-case and aggregate adapter metrics |
| `packets.ndjson` | Only the 200 accepted-development packet rows |
| `private-audit.ndjson` | Their samples, fitted targets, source spans and refinement history |
| `heldout-review` | Forty packets from ten hash-selected test sources |
| `transfer/run` | One hundred frozen packets from twenty-five reviewed transfer drawings |
| `failure-review` | Eight frozen packets from two deliberately selected difficult test sources |

The native decoder must receive only `packets.ndjson` and a model. Supplying
the private audit would invalidate a claim of reconstruction from detached
hints. Render/evaluation joins can use the outer IDs without making them part
of the native evidential structure.

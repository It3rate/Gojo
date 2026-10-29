# Learning local settlement responses

Status: implemented exploratory fixture, 28 October 2026. This compares a context
table, linear regression and a small neural function at the same receiver
boundary. All three learn useful choices on a scalar teaching task. The neural
function has the best local test result, but regression transfers better to the
coupled constructions. Prediction quality on the teaching task does not establish
useful closed-loop behaviour.

The experiment extends the [authored ensemble fixture](Ensemble.md). It changes
one local proposal: the bound applied when both signs of request arrive at a
public port. Existing admission, receiver capacities, source observations,
construction review and required-path checks remain in charge of actual change.

## Native function representation

The operational representation is ordinary connected Gojo structure:

```text
G(input bases, output bases,
  G(G(input domains, G(layers), training support, source)),
  G(available request-bound intervals), training receipt)

layer = G(neurons)
neuron = G(bias location, G(input-to-output affine frames),
           output interval, output quantum)
```

The context table uses sixteen disjoint input domains and constant outputs.
Regression uses one affine layer. The neural function composes two affine
layers, with eight hidden nodes bounded to `[0,4]`. One reader executes all
three forms; no model-name switch selects an execution law. Input and output
bases, maps, node bounds, quanta and source connections remain native.

The six inputs are finite reads of one actual public request group:

| Input | Selected read |
| --- | --- |
| Peak magnitude | Maximum absolute request, divided by 128. |
| Net in median direction | Net request projected into the median's direction, shifted and scaled into `[0,1]`. Zero net reads halfway. |
| Minority participation | The smaller of positive and negative request counts, divided by total count. |
| Net proportion | Absolute net request divided by total absolute request. |
| Median proportion | Absolute median request divided by the peak. |
| Group size | Request count divided by eight. |

The context retains its signed partitions, ordered median participants,
comparison claims, sum equations, normalizations and projection witnesses.
Reflection and permutation leave these particular features unchanged. Neither
property is claimed for arbitrary Gojo structures or observers.

Request projection uses a 256-part read, followed by feature resolution
`1/1024`. Features deliberately clip to `[0,1]`; lost participation remains in
their witnesses. The preprocessor's selected host-supported scope is at most
4,096 requests of magnitude at most 4,096 ticks. Unsupported reads stay open.
This computation scope is distinct from statistical training coverage.

Imported coefficients have a `1/4096` grid and retain a half-tick preimage
through an explicit finer-to-coarser calibration. This is a lossy import of the
fitter's parameters, not an exact archive of its floating values. Nodes retain
their pre-clipping result, clipping remainder and whole-node rounding remainder.
Outputs use a `1/4096` read of relative one-step relief.

The scores are comparative utility estimates, not probabilities. The selector
retains all six alternatives, explicit comparison calibrations and ordering
claims. Equal scores prefer the existing twelve-tick response. A missing model
read retains its unknown result and uses that response as a separate fallback.

Each receiver passage connects the function explicitly:

```text
G(actual public port, fallback bound, directional participation guards,
  G(function surface, function-bound-to-port calibration))
```

The selected action does not directly edit a construction. Its actual bound
qualifies synthetic requests, which still pass through current-publication
checks, receiver-local routes, grouped capacity and quantum admission. Existing
allocation witnesses account for blocked, deferred and remaining participation.

## Teaching task and independent splits

Training uses synthetic local consensus problems, with whole problems kept in
separate training, validation and test sets. Each has three to eight signed
requests with both directions present. The generator mixes distant outliers,
opposed groups, small noise and mixed magnitudes. These generator families are
not model inputs.

For each problem, the teacher compares the same six bounds, in explicit tie
priority: `12,4,8,24,48,96`. Its receiver applies one quarter of the bounded
request sum, clips the step to eight ticks, and admits whole ticks through
`Carrier.ReceiveTogether`. For requests `r_i` and bound `c`:

```text
movement(c) = truncate(clip(sum(clip(r_i,-c,c))/4, -8,8))
before      = sum(abs(r_i))
after(c)    = sum(abs(r_i - movement(c)))
relief(c)   = (before - after(c)) / before
```

The actual interval claims, response calibration, movement receipt, before and
after audits, normalization and source-basis remainder are retained. The
published target has resolution `1/4096`; it does not silently discard its
normalization remainder. Six counterfactuals from one problem are six targets
for one training example, not independent acquisitions.

The full run contains:

| Split | Whole problems | Generator amplitude | Seed |
| --- | ---: | ---: | ---: |
| Training | 2,048 | Up to 128 ticks | 3109 |
| Validation | 384 | Up to 128 ticks | 3739 |
| Test | 768 | Up to 128 ticks | 4517 |
| Wider requests | 384 | Up to 256 ticks | 5501 |

Request multisets and their reflections cannot cross splits. The full run
also excludes the earlier 384-problem development pilot and uses fresh seeds.
No transfer-world outcome participates in fitting or model selection.

There are 12,288 training counterfactuals and 21,504 across all 3,584 problems.
This is algorithmic supervision, not human-labelled imagery or church-part
recognition. The wider-request test is an amplitude change within this family;
its normalized errors should not be read as absolute physical error.

Only requests at the actual public port enter the learned context. The model
cannot inspect a receiver's private construction, desired final coordinates,
category, test outcome or the global diagnostic. Admission, construction
review and required-path checks remain with the existing native machinery.

## Fitting and selection

| Estimator | Fitted representation | Selected fitting law |
| --- | --- | --- |
| Context table | Up to 96 output values in sixteen authored context bins. | Per-bin mean utility; bins with fewer than four examples use the global training mean. Counts and fallback source remain retained. |
| Regression | Six affine outputs from six inputs, including biases: 42 values. | Ridge regression with penalty `0.001` on non-bias coefficients. |
| Neural function | Six inputs, eight bounded positive hidden nodes and six outputs: 110 values. | Minibatches of 64, Adam step `0.01`, moments `0.9/0.999`, epsilon `1e-8`, non-bias penalty `0.0001`. |

All parameters are fitted offline in C# over derived numeric reads, then
imported into native function structure. This does not implement gradient
training through Gojo carrier tension or discover a neural architecture.

Neural seeds `11,29,47` use at most 600 epochs, validation checkpoints every ten
epochs and a 100-epoch patience budget. A checkpoint is kept by validation
prediction error. After finite export, initialization selection uses validation
missed relief, with prediction error as the tiebreaker. Test results do not
choose a seed or checkpoint.

The selected seed is 11 at epoch 190. Its validation missed relief is 0.229
percentage points, versus 0.237 and 0.262 for the other initializations. Seed 29
has lower validation prediction error but makes slightly worse choices under
that selection criterion. Both measurements remain in the review.

Measured fit plus native training/validation checks took about 0.84 seconds for
the table, 0.69 for regression, and 2.70 for the selected neural initialization.
These are elapsed times on this run, excluding data generation and the coupled
evaluation; they are not hardware-independent performance guarantees.

## Local test results

Missed relief is the difference from the best available bound on that problem.
It is averaged after normalizing by each problem's starting disagreement. Lower
is better. Best-response rates include ties at the declared finite resolution.

| Policy | Test missed relief, percentage points | Test mean relief | Best-response rate | Wider requests: missed relief, percentage points |
| --- | ---: | ---: | ---: | ---: |
| Fixed twelve ticks | 0.879 | 5.511% | Not evaluated as a predictor | 0.552 |
| Context table | 0.396 | 5.993% | 80.3% | 0.232 |
| Regression | 0.386 | 6.003% | 81.2% | 0.199 |
| Neural function | 0.269 | 6.120% | 87.1% | 0.150 |

All three improve local response selection on average relative to this fixed bound. The
neural function is best on the selected local task. This claim concerns that
task and supervision; it does not establish a better general settlement law.

Native restoration reproduces predictions exactly. On 32 validation problems,
the largest sampled difference from the floating fitted function is about
0.000409 for regression and 0.000477 for the neural function. These sampled
checks are distinct from the retained coefficient preimages and finite losses;
they are not global error bounds. No separate floating table evaluator is
reported for this measurement.

## Transfer to coupled constructions

The frozen functions are installed in the wall, roof and opening fixture.
There are twelve deterministic arrangements, with and without the conflicting
opening opinion: 120 trajectories of forty numerical rounds. Every learned
variant has the same authored response mixture, capacities and structural review
as the fixed local-negotiation baseline. Direct projection is an additional
control with a different response-component mix.

The diagnostic is the original calibrated known-demand sum, not recognition
accuracy or physical energy:

| Policy | Cooperative: final | Cooperative: mean during run | Conflicting: final | Conflicting: mean during run |
| --- | ---: | ---: | ---: | ---: |
| Direct projection | 40.17 | 77.39 | 315.50 | 352.52 |
| Fixed local negotiation | 47.92 | 103.43 | 225.67 | 286.36 |
| Context table | 140.50 | 163.85 | 345.25 | 364.93 |
| Regression | 46.50 | 113.76 | 217.25 | 292.97 |
| Neural function | 162.08 | 184.38 | 380.58 | 394.04 |

Regression has slightly lower final disagreement than the matched fixed policy,
but worse average disagreement during the run. It takes longer to reach that
compromise. The direct control is best on cooperative final disagreement;
regression is best on conflicting final disagreement. The locally strongest
neural predictor transfers poorly here.

Four visual controls keep the neural policy active: permitted eaves remain at
zero demand without motion, fixed receivers retain 440 demand, unknown
permission remains unresolved and prevents complete-score structural review,
and a fixed wall leaves its source-fit disagreement visible. Learned response
selection does not erase these states or bypass required support.

## Count coverage probe

After inspecting transfer failures, a separate post-hoc probe restricts the
neural query domain to request counts observed in the saved training data:
three through eight. It reuses the same affine layers and coefficients. Outside
that domain, the unresolved model read is retained and the fixed twelve-tick
proposal is used. This is an authored scoped-use policy, not learned confidence.

The probe uses two already inspected seed-zero worlds, so it is not a fresh
held-out performance claim:

| Input | Fixed final demand | Raw neural | Count-qualified neural | Raw model contexts outside observed counts |
| --- | ---: | ---: | ---: | ---: |
| Cooperative | 48 | 156 | 156 | 1 / 147 |
| Conflicting | 223 | 359 | 359 | 0 / 151 |

The count qualification changes neither final outcome. It rejects the first
count-mismatch explanation for these two replays. It does not rule out other
distribution shifts or establish why every neural choice fails.

## Interpretation and next experiment

The scalar teacher treats every request as a point preference and scores its
distance after one receiver moves. Actual messages are synthesized corrections
from interval claims, shared actuators, projected obligations and trend filters.
A zero correction from a satisfied permitted interval does not imply a point
preference that the receiver must stay still. Equal request values do not
establish equivalent causes, slack, independence or downstream response.

This mismatch is a concrete reason to investigate the teaching objective and
available feedback before increasing model size. It remains an explanatory
hypothesis; the count probe alone does not prove it.

The next controlled comparison should teach from bounded counterfactuals of
the actual connected receiver and its public error services. Keep entire
worlds together in train/validation/test splits, and measure both immediate and
several-round outcomes. Useful added inputs include observed accepted movement,
quantum or capacity deferral, remaining permitted slack, shared response and
recent help or harm. Obtain these through exposed services or feedback, rather
than reading a neighbour's private definition.

The fixed policy remains the default. These learned functions are optional
experiment participants, and this work does not install a model in QuickDraw
Studio or retrain on its converted church corpus. It also does not decide
whether neural models are necessary in the larger system. It demonstrates that
constant cells, regression and a small neural function can share an executable
Gojo representation, while their usefulness still depends on what they were
taught and how they connect.

These estimators share supervision. Combining them later must preserve their
shared source and related errors rather than count their predictions as
independent new evidence.

## Run and retained artifacts

From the repository root:

```powershell
dotnet run --project experiments/ConstructionMachines -- response-learn D:/_quickDraw/data/ensemble-learning-2026-10-28-v1
```

This generates data, fits and exports models, verifies the native local
results, runs the coupled transfer and count probe, and writes the reviews.
Use `--quick` after the folder for a small development run in a separate folder.
Fitting and experiment selection are deterministic within the stated code and
runtime; no online model service or external ML package is required.

To rerun only transfer with the saved functions, use `response-transfer`.
Use `response-coverage` for the two-case count probe. Use `response-verify`
to recompute the local results from native artifacts, or `response-review`
to refresh only the HTML from existing observations. Each takes the same output
folder as its second argument.

The folder contains:

* `review.html`: learning results, the local decision playground and transfer summary.
* `transfer.html`: fourteen visual transfer worlds with per-port alternatives.
* `coverage.html`: the separately labelled count probe.
* `learning.json`, `observations.json`, `coverage.json`: derived review data.
* `models/`: three fitted native functions and the count-qualified function.
* `data/`: native teacher packets in batches of 64, with an active-file index for generated runs. Obsolete files are not selected merely because they remain in the folder.
* `native/`: sixteen complete visual worlds with initial and final state and retained timelines.
* `benchmark/`: 110 repeatable initial-world/final-read packets, without a promise of complete intermediate history.
* `native-verification.json`: native-only recomputation counts and metrics.

Imported archives are independently identified local worlds. Queries rebind
feature publications through explicit basis projections; equal numeric values
or matching directory labels do not create shared native sites.

All sixteen saved visual worlds restore and continue consistently. Native-only
verification reads all 56 teacher archives, checks 3,584 distinct reflection
classes, excludes development problems, checks teacher movement and source
remainder accounting, and reproduces all three models' reported metrics.
Browser verification covers 96 local problem/policy views, 837 recorded world
observations, 1,754 displayed model decisions and all 126 referenced world
archives. It also checks unchanged acceptance geometry, unsupported-model
fallbacks, keyboard navigation, offline operation and mobile overflow.

The construction project passes 138 tests, including 21 focused learning
tests. The solution-wide run passes 5,545 tests across thirteen projects with
no failures or skips. Focused checks cover finite loss, false basis reads,
competing domains, data separation, unchanged source state, native restoration,
public-context isolation, retained unknown permissions, count qualification
and the recorded port decisions.

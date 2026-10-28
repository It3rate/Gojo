# Settlement with several small response rules

Status: implemented exploratory fixture, 28 October 2026. This exercises a
bounded subset of the [settlement design proposal](SettlementEvidence.md).
All constructions, observations and response laws are authored. No neural
network is trained or invoked.

The fixture asks whether combining small response rules is useful, what local
communication adds, and when parameter response needs a different construction.
Its diagnostics report agreement under selected obligations, not recognition
accuracy or physical energy. A blend is allowed to lose to an individual rule.

## Connected operational structure

Three native constructions expose scalar ports: a rectangular wall, a roof
joint and a rectangular opening. Their private parameters move through the
existing `NegotiatingMachine` routes and admitted carrier tension. The wall and
opening use retained offset constructions. The roof retains an actual three-edge
joint and a required foot-to-hub path.

Each obligation has this retained shape:

```text
G(support correspondence,
  G(G(input port, input-to-feature calibration, response calibration)),
  permitted interval, feature axis, influence, response rules)

response rule = G(error-and-trend equation, clipping interval, budget share)
```

The feature is a sum of calibrated public scalar reads. Its native interval
claim exposes the current value, signed nearest permitted correction, violation
and actual ordered excess interval. For left overhang, for example, the feature
is `roof-left - wall-left`, permitted from `-8` through the selected domain end.
The error structure therefore retains the unwanted span and its source ports,
rather than only a red label or a free adjustable error value.

The ordinary case has fourteen obligations:

* Left and right permitted overhang.
* Left and right eave attachment.
* Roof centring and observed roof span.
* Observed ridge height.
* Observed left, right and top of the wall.
* Left and right containment of the opening.
* Opening centring and observed opening location.

The observations are synthetic permitted bands. Their nominal references are
guides, not an exact drawing every participant must copy. A quiet control keeps
eight-tick eaves without forcing roof endpoints onto wall endpoints.

## Response rules and communication

Every obligation computes three possible responses from the same evidence:

| Rule | Retained operation |
| --- | --- |
| Direct projection | Nearest permitted feature correction. |
| Bounded steps | The same feature correction clipped to `[-12,12]`. |
| Trend-aware response | Feature correction minus half the last observed feature change, with its declared clipping interval. Startup selects zero trend explicitly. |

The three are related affine filters with clipping, not a survey of every
settlement algorithm. Their equation surfaces, previous feature samples,
intervals and shares remain native structure. Each response is projected back
through explicit passages to the participating public ports. The chosen
least-norm coefficient calibration is authored response knowledge.

The three single-rule controls use shares `6/6,0,0`, `0,6/6,0` and `0,0,6/6`.
The equal blend uses `2/6` each. Thus combining rules does not triple their total
influence or turn their related outputs into three independent observations.
Receivers retain the same parameter capacities and quanta across these controls.

A fifth policy adds local negotiation to the equal blend. A passage is an
actual public port connected to a permitted request interval. If both signs of
request arrive at that port, each request is clipped to `[-12,12]`; agreed signs
pass unchanged. This law sees only the local request group and its passage.
It does not inspect desired final geometry, categories or the diagnostic score.
It leaves all opinions represented rather than choosing a winning sender.

The condition is retained as two ordered direction frames with required count
intervals, rather than a hidden policy flag:

```text
G(public port, S(port-basis,-12,12),
  G(S(port-basis,0,+1) -> S(positive-count-basis,1,4096),
    S(port-basis,0,-1) -> S(negative-count-basis,1,4096)))
```

Each count claim retains its selected requests and the whole local group. Both
claims must resolve within their declared intervals before clipping applies.
Direction and request magnitude remain in the port basis. Each participation
count has its own basis and claim, separate from the request-magnitude bound.

The withheld part is retained separately from the synthetic request admitted
to the receiver. This is an attenuation of proposed change, not conserved
material already transferred. Each actual actuator admission subsequently
retains bounds blocking, step deferral, quantum remainder and candidate rejection
under the existing numerical engine.

These particular filters and the twelve-tick gate are authored choices. A later
model can supply response proposals or calibrations through the same public
contract; it must not bypass admission or write private parameters directly.

## Captured rounds and effects

Each round captures all public publications before any receiver responds.
Obligations read that snapshot and publish explicitly synthetic target signals.
The router groups requests at actual receiver ports. Receiver-local routes
combine requests sharing an actuator before applying its capacity and quantum.
Admitted changes enter through `Carrier.ReceiveTogether`, and public sites are
then explicitly republished.

The next read records each obligation's observed help or harm through
`OperationClaim.IntervalResponse`, against the same permitted interval. Several
requests can jointly cause a change; this witness does not attribute the result
to one particular rule. A repeated read alone creates neither motion nor memory.

The common audit sums magnitudes through explicit feature-to-diagnostic
calibrations. It counts a declared support correspondence once, even in the
deliberately unpooled-response control. Signed errors in different obligations
cannot cancel to a false zero. Open permitted regions remain unresolved and
the known-demand sum is explicitly partial.

The response rules are not guaranteed to descend this audit on every round.
It is a shared outcome measure, not a hidden global optimizer or a proved
Lyapunov function. Forty rounds are a budget, not a convergence detector.
Whole-tick quanta can leave meaningful disagreement without admitted motion.

## Structural proposals under continuing demand

The controller observes opposed vertical requests addressed to the roof's
public eaves and ridge. The magnitude is a qualified local read with retained
fractional residue. Three successive rounds at magnitude six or more can
authorize a review, within a separate review budget. This persistence is an
effort policy, not independent evidence or confidence.

Each review compares eight additional local rounds of the current construction
with eight of each candidate, holding neighbour publications fixed. The fork
contains the roof's own construction and response routes; other participants'
private definitions and actuators are absent. Error participants answer
hypothetical queries through their public service. Such query bindings cannot
enter a live receiver as genuine publication events.

The candidates release the eave-height dependency, deform in the opposed
direction, or release eaves while removing support. Native claims require at
least three units of advantage over continuing the baseline and no increase in
unresolved required paths. All obligations remain in the comparison. Deleting
a support edge cannot conceal its outstanding path demand.

An accepted rewrite retains the current public coordinates and ports. Its
preview's future coordinates are not installed. Subsequent ordinary responses
must realize any benefit. This fixture permits one accepted rewrite per run;
unsuitable candidates may use up the review budget while leaving the current
construction and disagreement visible.

The chosen review scope contains all active obligations of this small scene.
Consequently, an unbound permission blocks a complete-score structural review,
although other numerical responses continue. Scoping a review to its affected
obligations is a useful later comparison, not a general law implemented here.

## Cases and variation grid

The review includes the five policies on ordinary and conflicting inputs,
plus controls for a fixed roof construction, permitted eaves, pooled and
unpooled copies, fixed receivers, unknown permission, unsuitable alternatives
and a fixed wall.

The grid runs twelve deterministic placements/proportions with and without an
additional conflicting opening opinion: 24 input scenes and five policies,
120 trajectories of 40 numerical rounds. No weights are fitted to that grid.
The seed-zero visual runs are reused in its totals. This is a controlled
mechanism study, not a sampled population estimate or an ML generalization test.

The conflicting opinion asks for the opening beyond the wall. Its disagreement
remains part of the audit. The system cannot label it an outlier and erase it
for a free improvement. Local negotiation can limit its requests while retaining
the fact that the opinion is still unsatisfied.

The support-copy controls use an explicitly declared shared correspondence.
Pooling does not infer statistical independence or discover correlated causes.
The unpooled control intentionally allows the copies to amplify their requests,
while the independent audit continues to count their shared support once.

## Observed results

The generated review contains eighteen visual worlds and 753 captured
observations, including separate construction-acceptance and declined-review
observations. The following are means over the twelve input arrangements in
each condition, after forty numerical rounds. Lower means less disagreement
under this fixture's declared diagnostic; it does not establish a more correct
interpretation of an actual drawing.

| Response policy | Cooperative: final | Cooperative: during run | Conflicting opinion: final | Conflicting opinion: during run |
| --- | ---: | ---: | ---: | ---: |
| Direct projection | 40.17 | 77.39 | 315.50 | 352.52 |
| Bounded steps | 55.25 | 141.34 | 250.50 | 338.00 |
| Trend-aware response | 39.50 | 79.15 | 314.67 | 353.02 |
| Equal blend | 45.83 | 91.55 | 315.50 | 362.55 |
| Blend plus local negotiation | 47.92 | 103.43 | 225.67 | 286.36 |

The equal blend does not outperform the individual rules. Local negotiation
has the lowest final disagreement on every conflicting arrangement, while the
trend-aware rule is lowest, including ties, on every cooperative arrangement.
The negotiated blend also responds more slowly on the cooperative inputs.
The useful result is a conditional tradeoff, not a universal winner.

Selected seed-zero controls make the failure modes concrete:

| Control | Initial known demand | Final known demand | Structural outcome |
| --- | ---: | ---: | --- |
| Ordinary equal blend | 440 | 43 | Releases the eave dependency on pass 3, retaining support. |
| Same blend, fixed construction | 440 | 68 | Cannot release the dependency; disagreement remains. |
| Permitted eaves | 0 | 0 | No motion or review; eaves remain eight ticks. |
| Conflicting opinion, negotiated blend | 592 | 223 | Retains the conflicting opinion and its unsatisfied demand. |
| Five declared copies, pooled | 592 | 307 | Matches the single-opinion equal-blend run. |
| Five copies, unpooled | 592 | 457 | Repeated requests distort the outcome under the same audit. |
| Fixed receivers | 440 | 440 | No admitted motion; requests remain represented. |
| Unknown permitted region | 440, partial | 68, partial | One unresolved condition remains; no complete-score review. |
| Unsuitable alternatives | 440 | 68 | Uses two reviews; accepts neither offered alternative. |

Construction acceptance does not itself move the geometry or lower demand.
All thirteen accepted rewrites preserve their public coordinates at that
observation. Later ordinary rounds realize the changes. The support-removal
candidate remains in the report with its failed required-path claim.

## Run and artifacts

From the repository root:

```powershell
dotnet run --project experiments/ConstructionMachines -- ensemble D:/_quickDraw/data/ensemble-settlement-2026-10-28-v1
dotnet test Tests/ConstructionMachines.Tests/ConstructionMachines.Tests.csproj
```

To refresh only the HTML presentation from the existing recorded observations:

```powershell
dotnet run --project experiments/ConstructionMachines -- ensemble-review D:/_quickDraw/data/ensemble-settlement-2026-10-28-v1
```

The folder contains an offline `review.html`, derived `observations.json`,
compressed native worlds under `native/`, and benchmark input/result packets
under `benchmark/`. The browser plays captured observations and runs no
settlement. Error spans and attachment gaps are presentation projections of
the retained comparisons; a shaded region does not assert an implemented area
Boolean or building-physics law.

Each visual archive contains the final executable world with its retained
timelines, a separately captured initial world and the round budget. Generation
restores every visual world from its gzip-compressed Gojo numeric archive and
executes a further round. Benchmark packets contain a repeatable initial native
world, final witnessed read and budget; they deliberately do not promise full
intermediate history. Display names and JSON coordinates do not own the mechanism.

## Verification

Seventeen focused ensemble tests cover ordered excess intervals, response
direction, permitted eaves, unknown conditions, equal influence, pure reads,
foreign-basis rejection, private-preview isolation, continuing-baseline review,
required paths, receiver blocking, support copies, observed help and harm,
allocation remainders, native negotiation guards and restored execution across
a construction change. The construction project passes 117 tests. The full
solution run passes 5,524 tests across thirteen projects, with no failures or
skips; the latest guard-proof refinement also passes the focused project run.

Generation restores and continues all eighteen visual worlds from native
archives. Browser checks exercise all 753 observations, compare each displayed
total with its unique-support obligations, check all thirteen acceptance
observations for unchanged coordinates, and exercise navigation, playback,
highlighting, retained selection and mobile layout. All 128 referenced native
archives are present. The offline review makes no external requests.

## The learning question

This experiment tests how far authored constructions and communication can
settle a known problem. It does not establish whether a malleable Gojo network
is equivalent to a neural network, or whether neural components will be useful
for broader tasks. Representation, inference, proposal discovery and learning
are separate questions.

The next useful learning comparison would keep the same public interfaces and
observation budget while substituting a simple context table, small regression
model or small neural model for one local proposal or weighting function.
Compare held-out arrangements, useful structural choices, remaining disagreement
and computational work. A model could also help discover reusable construction
machines, after which those machines could execute without the model. That
possibility remains an experimental question.

Equal blending and local negotiation are hypotheses to test, not guarantees
that a society of weak rules beats every specialist. The report retains losing
cases and the tradeoff between cooperative and conflicting inputs.

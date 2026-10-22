# Weighted evidence over one hundred native shapes

Exploratory above-kernel study, 22 October 2026. This round applies the
[finite evidence calculation](GojoEvidenceCalculationFixture.md) to the
existing [100-shape reconstruction corpus](GojoRelationalShapeStudy.md).
It tests whether an explicit, revisable likelihood model gives useful
selection behavior. It does not establish calibrated probabilities or train
a shape recognizer.

Open the [comparison gallery](../../experiments/QualitativeFrieze/results/weighted-shapes/gallery.html),
[single-page overview](../../experiments/QualitativeFrieze/results/weighted-shapes/overview.html)
or [recorded summary](../../experiments/QualitativeFrieze/results/weighted-shapes/summary.json).

The mathematical calculation and the supplied statistical model have different
statuses. Normalizing finite nonnegative prior participation multiplied by
likelihood factors has an exact answer. Whether these factors describe the
reliability of the shape evidence is an empirical question. This experiment
deliberately tests several supplied estimates before attempting to learn them.

## Comparison with a fixed proposal pool

Each independently executed source is published as anonymous joint, spatial,
equivalence, symmetry and context streams. Source coordinates, teacher edge
partners, family names and intended repairs are unavailable to the receiver.
The two profiles are the full evidence packet and the existing sparse spatial
packet. They ask the same reconstruction question with different available
information.

For each source and profile, the bounded existing proposal mechanism runs
once. Its resulting pool is then frozen. The ordinal baseline and every
weighted policy select from that same pool; a policy cannot obtain a more
favorable proposal budget or consult the teacher when it changes its weights.
The existing proposal mechanism can use its own ordinal feedback while
building this pool. This experiment therefore isolates reranking within an
authored, baseline-influenced proposal distribution, rather than comparing
independent generators.

| Policy | Prior participation | Evidence channel | Purpose |
| --- | --- | --- | --- |
| Ordinal baseline | Existing lexicographic ranking | Existing critic counts | Retained comparison, not a probability model. |
| Uniform weighted | Equal participation per candidate | Main supplied reliability estimates | Effect of the family likelihoods. |
| Compact weighted | Reciprocal description cost | Main supplied reliability estimates | Separate effect of a compactness preference. |
| Uniform mild | Equal participation per candidate | Milder reliability estimates | Sensitivity to the supplied channel. |
| Uniform flat | Equal participation per candidate | Every report likelihood is one half | Neutral arithmetic control. |

The uniform flat policy must leave the selected candidate distribution uniform.
Its displayed winner, if one is needed, follows an explicit deterministic tie
rule and does not indicate evidence in favor of that candidate. A visibly
different choice under a tie is not a discovered distinction.

A posterior row is a selection share **conditional on this finite pool and
this channel model**. It is not the probability that the exact source has been
recovered, that a candidate is the only valid interpretation, or that a shape
belongs to a named category. Candidates outside the pool receive no represented
participation. A missing good realization remains a proposal-coverage failure.
The implemented pool collapses identical connectivity and anonymous role
placement up to one common positive repeated-unit scale. Its derived key
divides direction-participation counts by their common greatest divisor;
doubling every unmeasured gap therefore does not produce another model. Each
collapsed model receives one equal prior share in the uniform policies. All
original proposal routes remain attached to that model in the native pool,
so repeated search routes do not multiply its prior participation. This is a
specific equivalence read, not automatic quotienting under every mirror,
rotation, alternate construction or conceptual interpretation.

## One report per evidence family

Local native critics retain each supported, contradicted or open witness.
They assess the received packet against a candidate's generated structure;
they do not compare its image to the teacher. The likelihood adapter groups
these witnesses into six families, grounded on their exposed evidence surfaces:

| Family | Native evidence |
| --- | --- |
| Joints and components | Anonymous local rays, opposition/perpendicular reads and connected component participation. |
| Relative locations | Published before, after and equal-projection comparisons. |
| Exact equivalence | Shared unmeasured length and angle promises. |
| Symmetry | Exposed role and passage correspondences under selected actions. |
| Interior turns | Retained refinement and interior-turn context. |
| Coarse angles | Revisable similar-angle hypotheses. |

Family names are presentation labels for these connected surfaces, rather
than a class-specific decoder for triangles, elbows or stars.

Multiplying every check as if it were an independent observation would
overstate support. Many checks share a corner, displacement, correspondence
or premise. This first adapter instead constructs one summary report per
family. Exact duplicate witnesses count once within that family. Merely
similar witnesses do not thereby become the same event.

For a family with distinct assessed witnesses, assign each witness a noisy
compatibility response:

```text
supported      -> h
contradicted   -> 1-h
open           -> 1/2

family likelihood = mean of these responses
```

For reliability h and counts s, c and u, this is
`(h*s + (1-h)*c + u/2)/(s+c+u)`. A family uses one selected reliability in this
round. A family with no advertised checks across the pool supplies no factor.
It is not a successful check and is not an automatic likelihood of zero.
The exact adapter deduplicates native witness identities. If the same witness
appears in conflicting assessment lanes, it becomes unresolved in the channel
read while the original offered lanes remain available in its receipt.

This mean has a narrow statistical interpretation: analytically marginalize
one uniformly selected distinct witness from that family through the supplied
compatibility channel. The experiment need not actually sample that witness.
It does not assert that the complete set of family facts was independently
generated with this joint likelihood. A family with one hundred correlated
supported checks therefore does not become one hundred factors. The number
of distinct checks can still affect the mean when their outcomes differ;
grouping alone does not discover an ideal effective sample size.

The main channel uses `15/16` for joints/components and spatial reports,
`14/16` for exact equivalence, symmetry and interior turns, and `9/16` for
coarse angle hypotheses. The mild comparison uses `12/16` for the first five
families, while coarse angles remain `9/16`. The flat control uses `8/16`
everywhere. These values are supplied finite partitions, not learned
frequencies, measured confidence intervals, or distances between ordinal
rungs. Replacing the estimates changes an explicit premise.

An open witness remains open in its native assessment. Its `1/2` channel
prediction says that this adapter has no directional discrimination from it.
It does not claim a half probability for the underlying shape fact. In a
mixed family, an open response draws the family likelihood toward one half;
that is part of this chosen model and should be inspected rather than hidden.

Between families, this round explicitly supplies approximate conditional
independence given a candidate. Shared geometry makes that assumption
questionable even after aggregation: a position change can simultaneously
affect joint, symmetry and extent evidence. Distinct family identities do not
prove independence. Their factors and independence premise are retained so
later calibration, grouping or conditional models can replace this assumption.

## Defaults, goals and cost stay separate

Observed support and opposition supply the evidence likelihoods. A weak
closed-area expectation, a preference against incidental overlap, a requested
completion and a compact description are different inputs. They must not
silently become more observed shape facts.

The compact comparison supplies unnormalized prior participation `1/(1+cost)`.
The cost is the sum of the horizontal and vertical gap counts after dividing
out their common positive repeated-unit scale, plus the number of assumed
passages. It counts the candidate's own native participation under this basis;
it does not measure physical length or declare an unusual aspect ratio
impossible. Closure expectations already influence the shared proposal pool,
and the baseline keeps its existing crossing/overlap preferences. These are
not reimported as observed facts by the weighted channel. No additional
default or goal prior is fitted in this round. A requested bridge remains a
construction hypothesis, not evidence that the source contained the bridge.

Equal priors and compact priors therefore answer slightly different selection
questions. A lower-cost candidate can become preferable without becoming a
better fit to the supplied observations. Report evidence disagreement and
prior preference separately so that this difference remains inspectable.

## Native calculation and retained receipts

The adapter uses the existing statistical frame and partition grammar rather
than introducing another scalar scoring engine:

```text
anonymous receiver packet
  -> fixed candidate scope
  -> candidate-local native assessments
  -> distinct witness scopes and reliability partitions
  -> one observed compatibility report per family
  -> candidate-conditioned likelihood partitions
  -> explicit between-family independence premise
  -> prior multiplied by admitted likelihood factors
  -> normalized selection shares and selected realization

teacher correspondence -> post-selection audit only
```

The fixed scope, priors, offered reports, assessments, calibration premises,
factor history and posterior expressions remain connected native relations.
Exact rational reads and displayed percentages are derived diagnostics.
Reliability fractions describe unmeasured participation in the selected
channel; they introduce no length or angle calibration into the packet.

Orientation and response direction retain their existing separate meanings.
Opposition to a candidate claim is not a reversed stroke, and an unknown
likelihood is not an exact zero. Positive noise-channel probabilities keep
contradicted candidates available for later revision. Exact event aliases
must not multiply support, while an unrelated event still requires a supplied
dependence premise before its factor can be admitted.

This comparison is observational. A native read residual or a low selection
share does not by itself inject physical carrier tension, alter the source,
or establish which premise should be changed. A later feedback action needs
its own exposed route and settlement witness.

Every retained case receives a native archive containing its pool, assessment
and calibration receipts and final statistical states. Each final state is
also round-tripped independently through compact native transport and read
again without a teacher. The archive is evidence of the retained comparison,
not a promise to replay every discarded policy intermediate.

All 100 case archives are generated locally. Repository history retains five
review archives, R006, R042, R052, R056 and R059; the remaining bulk is ignored
and can be regenerated with the command below. The public gallery and summary
cover the complete run. This deliberately separates an inspectable review
sample from a claim that every large native archive is committed.

## Implementation and execution

- [Compatibility channel](../../experiments/QualitativeExecution/CompatibilityEvidence.cs)
  reads the connected assessment and calibration, deduplicates witness
  identities, and publishes the exact unmeasured likelihood partition.
- [Shape weighting adapter](../../experiments/QualitativeFrieze/Composition/ShapeEvidenceWeighting.cs)
  freezes the common pool, exposes its six evidence ports and applies the four
  supplied prior/channel plans through `StatisticalEvidence`.
- [Experiment runner](../../experiments/QualitativeFrieze/Composition/WeightedShapeExperiment.cs)
  executes the comparisons, withholds teacher correspondence until selection,
  captures connectivity audits and verifies native transport.
- [Independent source corpus](../../experiments/QualitativeFrieze/Composition/RelationalShapeCorpus.cs)
  creates the same 100 native sources used in the relational study.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --weighted-shapes
dotnet run --project experiments/QualitativeFrieze -c Release -- --weighted-shapes --budget 64 --source R006,R042,R052,R056,R059 --output artifacts/weighted-shape-review
dotnet test Gojo.slnx
```

The default budget is 64 proposals per source/profile. Sparse and full packet
comparisons share the source corpus, but each profile has its own frozen pool;
they do not claim to receive identical evidence or proposals. Within one
profile, all four weighted readings and the ordinal baseline use exactly the
same collapsed alternatives. A posterior tie is broken by the existing
ordinal rank, followed by proposal index, and remains displayed as a tie.

## Results

The retained run covers **100 sources, two packet profiles and four weighted
plans**, each compared with the same ordinal baseline in its frozen pool.
The 64-proposal budget per source/profile requests **12,800 attempts**:
**9,401** candidates are assessed and **3,399** inconsistent ordinal layouts
are blocked before candidate construction. Collapsing common-scale duplicate
proposals leaves **2,702 distinct normalized samples** across the 200 pools.

The calculation and native archive phase takes **436.78 seconds**, and the
complete CLI run takes **445.82 seconds** on this local machine. The 100 local
native archives total **152,245,657 bytes**. All **800 final native statistical
states** preserve their exact posterior fractions after an independent
round-trip without teacher information. This is a retained experiment runtime,
not a general performance benchmark.

| Packet | Policy | Exact source connectivity / 100 | Changed choices | Higher hard-conflict count | Cases with tied maximum shares |
| --- | --- | ---: | ---: | ---: | ---: |
| Sparse | Ordinal baseline | 58 | — | — | — |
| Sparse | Uniform weighted | 61 | 18 | 15 | 48 |
| Sparse | Compact weighted | 60 | 33 | 22 | 0 |
| Sparse | Uniform mild | 61 | 17 | 14 | 49 |
| Sparse | Uniform flat | 58 | 0 | 0 | 100 |
| Full | Ordinal baseline | 99 | — | — | — |
| Full | Uniform weighted | 99 | 3 | 2 | 69 |
| Full | Compact weighted | 99 | 8 | 8 | 0 |
| Full | Uniform mild | 99 | 3 | 2 | 69 |
| Full | Uniform flat | 99 | 0 | 0 | 100 |

An exact source-connectivity candidate exists in **61/100 sparse pools** and
**99/100 full pools**. Uniform weighted and mild selection recover all of
those available sparse connectivity matches; the uniform policy gains three
over the baseline without losing another exact match. That is a narrow
recovery result. **Every one of those three gains has more contradicted hard
checks than its baseline**, and no weighted policy lowers the selected hard
conflict count in any case. Family averaging makes explicit tradeoffs that
the lexicographic baseline would not permit; it has not established general
improvement in reconstructed geometry.

The compact prior gains the same three sparse exact-connectivity matches but
loses one previously exact match, R098, where it leaves four rays unpaired.
It selects more unpaired rays than the baseline in eleven sparse cases,
compared with two for uniform weighted and one for mild. Its unique maxima
are therefore evidence of a decisive supplied preference, not evidence of
greater certainty about the source. The neutral control preserves the full
uniform distribution and baseline tie-break in all 200 pools.

The main and mild channels agree on the three sparse connectivity gains and
the three full-profile changes, but differ on one additional sparse choice.
The mean top share on sparse pools is about **0.232** for the main channel and
**0.191** for mild. Lowering the supplied reliability spreads selection mass
without creating observations or changing empirical support. Neither value has yet been calibrated
against the frequency of a defined correct-answer event.

The focused suite passes **70 tests**: the existing 52 statistical-law tests,
ten new compatibility-adapter tests covering **189 exact oracle
combinations**, and eight new shape-integration tests. These test native
partitions, witness identity and conflict handling, family grouping, neutral
controls, compact scale normalization, frozen-pool behavior and transport.
They establish the stated calculation contract, not statistical calibration
of the selected reliability values.

Solution-wide `dotnet test Gojo.slnx` passes **5,091 tests**, with no failures or
skips. The final Release build has no warnings or errors. Browser verification
passes **806 states and 4,868 local references**, checking all **4,764 SVGs**,
the 800 displayed comparison contexts and all 100 locally generated native
archives, with zero errors. It includes 1,166 displayed flat-control checks
that preserve prior mass, plus desktop/mobile inspection of changed designs.

### Cases worth inspecting

- **R006, square:** 58 proposal routes collapse into the same selected model
  rather than giving it 58 prior shares. In the sparse pool it starts at
  `1/3` and ends at `2527/4457`. An open symmetry read remains visible;
  the other families support the candidate. The number of proposal routes
  explains the search, rather than serving as acquired statistical support.
- **R042, two translated elbows:** uniform weighting changes the sparse
  choice from four matched source passages to ten of twelve, while hard
  conflicts rise from 16 to 44. The joint family improves from `121/200` to
  `53/64` and exact equivalence from `89/160` to `113/160`; symmetry falls from
  `29/37` to `377/616`. The changed choice is an inspectable compromise among
  family channels, not a fully recovered object. The visible selected sample
  contains broad overlapping rectangles and an assumed crossbar, rather than
  the intended pair of slender elbows.
- **R052, four slender triangular lobes:** all twelve source passages replace
  a sparse baseline with one match and eighteen unpaired rays. Joint support
  improves from `191/368` to `15/16` and interior-turn support from `1/2` to
  `7/8`, but symmetry falls from `397/752` to `275/628`. Hard conflicts rise
  from **34 to 77**. The four triangular components close, but their locations
  and proportions do not reproduce the design. This is the clearest warning
  against calling a recovered edge inventory a successful overall realization.
- **R056, square inside square:** the sparse baseline has no passages and
  sixteen unpaired rays, so it contradicts only two current hard checks.
  Weighting selects all eight source passages with no missing ray, strongly
  improving joint, equivalence and interior-turn evidence, while introducing
  24 symmetry conflicts. The two visible contours lie side by side instead of
  one inside the other. Missing structure must not look automatically better
  merely because it leaves many checks open; conversely, completing incidence
  does not justify the resulting geometry.
- **R059, two unequal inset rooms:** the sparse choice improves from two of
  twelve source passages with twenty unpaired rays to all twelve with none.
  Hard conflicts change from **7 to 8**. The joint family improves from
  `479/944` to `15/16`, and the exact-equivalence and interior-turn families
  improve from `1/2` to `7/8`; the remaining disagreement still needs a
  structural correction rather than another claim of certainty. The three
  contours do not preserve the source's containment arrangement.

These three connectivity gains are **not faithful design-geometry gains**.
The sparse stream omits relevant placement and containment information, and
the generated realizations also contradict some supplied checks. The next
learning target must therefore evaluate query-specific relations and
realization quality, rather than optimizing the edge-inventory score alone.
Reliable exact observations may need a distinct legal or proof lane, while
soft hypotheses use a noisy likelihood lane. In either approach, a conflict
must remain visible; assigning a high likelihood must not disguise a known
contradiction as a satisfied constraint.

Full-profile changes R022, R054 and R099 preserve their exact source
connectivity but can change the selected gap realization. R094 remains a
different connectivity with no faithful candidate available in its full pool;
its reranking cannot restore an absent proposal. These examples separate
changes of realization, changes of connectivity and missing candidate coverage.

Source-edge agreement is an audit of recovery, not the policy's training
target in this run. A teacher-different candidate may satisfy the communicated
concept, and matching every endpoint partner does not prove geometric
equivalence or satisfaction of every local claim. Inspect alternative
realizations and unresolved claims rather than using one global success rate
as their substitute.

A sharply peaked selection distribution is also conditional on the pool. A single dominant
row can be wrong if the pool excludes a better answer or the channel model is
misspecified. Conversely, a broad distribution can correctly preserve several
allowed realizations. This round must not claim calibrated uncertainty from
posterior sharpness alone.

## From this fixture to a training regimen

The current purpose is to validate the calculation and expose what its inputs
need to learn. A larger experiment should preserve this separation between
proposal coverage, evidence quality, prior preference and calibration.

1. Generate deterministic native source programs and record their recipe
   parameters, executed graph, observable streams and private audit controls.
   Include valid alternative realizations, damaged or intentionally open
   shapes, conflicting reports and capability-limited unknowns.
2. Split by source recipe and structural family before fitting. Variants of
   one template, mirrored copies and sibling perturbations must stay in the
   same split. Reserve an unseen-family set in addition to held-out variants
   from known families.
3. Freeze receiver packet and query contracts. Record which relation surfaces
   the receiver was allowed to inspect; exclude source coordinates, intended
   answers and private provenance from features, proposal selection and
   ranking. The teacher can supply labels only in the training or audit lane.
4. Evaluate proposal coverage before reranking. If no generated candidate
   satisfies the relevant packet, a ranking weight cannot solve the case.
   Keep a fixed-pool comparison when testing new weights, and a separate
   generator comparison when testing proposal changes.
5. Fit channel reliability and dependency structure using the training split
   only. Start with a few family-level parameters and proper held-out report
   predictions; do not turn a hundred correlated local checks into a hundred
   independent training samples. Compare the authored channel, learned
   channel and neutral control at equal proposal budgets.
6. Validate probability calibration separately from ranking. Use held-out
   log loss or Brier score only for a defined event with appropriate labels,
   and inspect reliability by evidence family, missingness and conflict level.
   Top-choice recovery, constraint satisfaction and rank of acceptable
   alternatives answer different questions and should be reported separately.
7. Test incremental revision: acquire one new report, preserve the prior
   candidate scope, then inspect which odds change and why. Include replayed
   reports, correlated restatements, honest unknowns and evidence opposing a
   strongly preferred candidate. A low-resolution publication must not create
   new acquisitions or erase the earlier state.
8. Only then expand the model: learned structural priors, adaptive question
   selection, recipe mutation or generation feedback. Each expansion needs
   a separate held-out comparison and an account of computation and retained
   uncertainty, rather than an assertion that a sharper posterior is better.

Thousands of generated cases are useful once these event, privacy and split
contracts are stable. They can estimate whether local evidence transfers to
new combinations and whether the family independence approximation is too
strong. They cannot by themselves validate a likelihood law if the target
labels are merely that same law's previous decisions.

## Present boundaries

This is authored Bayesian-style reranking over pre-existing native candidates.
There is no learned interpolation from evidence ratios, no model of uncertainty
in the likelihood estimates, no posterior over engine capability, and no
calibrated probability that a teacher reconstruction is correct. Candidate
generation remains a bounded mechanism with explicit missing-order and
composition limits. Its output is a sampled realization of communicated
relations, rather than a measured source outline to restore.

The exact compatibility adapter also inherits the statistical fixture's
finite native partition capacity: a reduced likelihood denominator above
65,536 is left open rather than rounded or silently enlarged. The experiment
runner requires complete declared factors and stops if such a read remains
open. Scaling the number of examples is different from allowing unbounded
complexity inside one packet. The archive size also motivates an explicit
retained-review policy for larger training runs, rather than assuming that
all detailed proposal and assessment history should always be kept.

The useful result of this round can be a positive, neutral or negative change
in recovery. An interpretable failure still identifies whether a likelihood,
prior, dependency approximation, query scope or missing proposal is responsible.
That diagnosis is the basis for a larger training task, rather than a reason
to hide disagreement in a single unexplained confidence score.

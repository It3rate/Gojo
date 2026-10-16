# Bounded learning of qualitative frieze generators

Status: implemented experimental checkpoint, 2026-10-16. This follows the
[authored generator checkpoint](GojoQualitativeFriezeExperiment.md). Reproducible
run results are below. The experiment stays above
the kernel and uses the existing phase, event, motion and sampling machinery.

## Question and scope

Can bounded feedback improve a small qualitative generator while preserving
inspectable rules about its legality, complexity, spacing and symmetry? Can
the same candidate be assessed through a coarse structural read and a separately
supplied measured interpretation without making the latter its hidden meaning?

The user requested hard limits on retracing and generator complexity, rightward
periodic progression, an optional crossing policy, and goals for spacing,
coverage and symmetry. These have different evidential requirements:

| Requirement | Structural foothold | Further qualification |
| --- | --- | --- |
| Bounded generator | A finite capacity scope bounds selected phase participation | Count the generator, rather than its observation window or accumulated history |
| Rightward period | Positive horizontal and neutral vertical net participation | Individual strokes may move left; uncertain sums retain extent obligations |
| No retracing | Adjacent opposite strokes can prove a local violation | All older and translated-neighbor overlaps require the global segment read |
| Crossing policy | Separate crossing, contact and positive-length overlap witnesses | Allowing crossings never permits retracing |
| Spacing and coverage | Occupied finite probes and adjacency shells | These reads do not establish continuous area or metric distance |
| Symmetry | A selected transform must preserve the exposed support | Ink symmetry does not require the causal program to run in the same order |

This is a test of declared feedback mechanisms and limited search. A successful
score is evidence about the selected objectives and interpretations. It is not
a proof of human appeal, general creativity or the wider Gojo thesis.

## Native structural rules

[`FriezeStructuralRules`](../../experiments/QualitativeFrieze/FriezeStructuralRules.cs)
connects the fixture, motion frame, explicit positive-reach design premises,
finite complexity capacity and local obligations. Reports preserve passed
claims, counterexamples and unresolved claims with their source reads.

```text
positive reach premise = reach -> (origin -> distinct positive site)
phase participation = component phase-surface -> admitted phase participant
complexity = Closed(phase participations) -> Closed(capacity slots)
period obligation = (horizontal axis -> forward face) -> (vertical axis -> neutral)
local retrace obligation = opposites -> (exchange -> positive reach premises)
```

The cap counts admitted phase participants across distinct component phase
surfaces. A phase participating in two different component surfaces contributes
twice; two views of the same surface do not create two generators. Changing
sampling frequency or running additional periods does not make the generator
more complex under this particular read. It is not a complete memory or
runtime cost model.

The evaluator selects the first completed control return and its committed
samples. Retained applications must account for the selected period's motion;
an uncommitted contribution cannot silently disappear at the boundary. A
completed control period can still contain unknown motion and therefore an
unresolved displacement.

Rightward progression concerns the period's net displacement. Positive
horizontal participation with neutral vertical participation suffices at this
read. A leftward stroke within the period is legal. Opposed terms with
different unmeasured reaches may require an explicit order or sum-threshold
obligation before their sign is known. A phase return alone establishes neither
spatial closure nor a fixed elapsed duration.

The local retrace rule consumes committed sample deltas, not accumulated
positions or an axis projection of the entire motion history. Under the stated
positive-reach premise, adjacent opposite axis-only strokes overlap even when
their extents differ. A nonzero perpendicular stroke between them changes the
line and prevents that immediate-overlap inference. Known zero pauses do not
move the path off its current line. The last nonzero stroke of a completed
period is also compared with the first stroke of the next copy.

Exact opposite diagonal vectors can prove retracing. Opposite diagonal
quadrants alone leave a collinearity or ratio obligation. Synchronous opposite
contributions can cancel into one zero sample, while sampling them separately
can expose two retracing strokes. The chosen sample-path contract remains part
of the certificate.

These local rules deliberately do not certify the absence of all retracing.
For example, `right, up, left, down, right` can overlap an older edge even
though every adjacent turn passes. A candidate needs the global check before
the full no-retracing requirement is admitted.

## Global measured refinement

[`FriezeMeasuredRules`](../../experiments/QualitativeFrieze/FriezeMeasuredRules.cs)
reads the selected periodic ink under an explicit calibration. It compares
actual stroke support, including relevant translated period neighbors, and
distinguishes:

- Positive-length collinear overlap: retracing, rejected by every profile.
- Straight-through ink crossing: admitted or rejected by the profile's policy.
- Point contact: retained separately, including ordinary joins.

A proper ink crossing is determined from the continuous support at a periodic
site: at least two noncollinear lines have occupied rays in both directions.
It remains a crossing when its arms happen to end at that sampled site.
Subdivision must not turn the same X-shaped ink into apparently harmless
segment-endpoint contacts. A T contact or an ordinary join does not provide
two such through-lines and stays a separate contact read.

This crossing policy is narrower than all possible interleavings of bent
trajectories. Those require retained path continuations and a separate
interleaving test. A junction with an X and additional terminating branches
belongs to the crossing scope; the contact count includes only sites that do
not meet the crossing criterion. All incident rays remain in the witness.
The no-crossing profiles forbid this straight-through ink read, rather than
certifying every possible future curved or bent-strand crossing definition.
Geometric coincidence remains an observation and does not fuse source site
identities or create a material connection.

The read preserves actual source strokes, translations and intersection
geometry. Collinearity, coverage and transformed support use their selected
measured basis. Work exhaustion or incompatible input remains unresolved;
absence of a reported violation in an unfinished read is not a pass.

The current source boundary accepts whole calibrated source ticks on its
declared axes. Fractional source-point coordinates remain unresolved rather
than being rounded into that grid. Internal intersections and symmetry maps
can still retain exact native fractions. To supply lengths such as one third,
select an explicit finer source unit and calibration that represents them as
whole source ticks; this is a refinement of the interpretation, not a hidden
floating-point fallback.

Measured evidence can settle an obligation left open by the qualitative read.
That is a conditional result for the selected calibration. It must not be
reported as proof that every positive assignment to the original unmeasured
reaches is safe.

## Finite occupancy, spacing and center reads

The topology-facing objective uses a finite advertised probe space, occupied
participation and adjacency.
[`FriezeCoverage`](../../experiments/QualitativeFrieze/FriezeCoverage.cs) supplies
which probes detect realized ink within the selected radius of one observer
metric tick. Its explicit metric maps select that observer's interpretation;
they do not equate unrelated source units by default.
[`QualitativeCoverage`](../../experiments/QualitativeFrieze/QualitativeCoverage.cs)
then reads the publication through the declared finite topology; it need not
receive private measured coordinates.

Nearest-shell reads express successive adjacency from selected occupied or
boundary participation. Their shells are graph relations at the probe
resolution, rather than an implicit Euclidean ruler. If several probes are
equally central under the chosen shell read, the result remains a plateau of
candidates. Choosing one by enumeration order would add information that the
read did not establish.

Coverage means occupied probes under this service contract. It is not an area
integral, a guarantee that all unsampled regions are filled, or a theorem about
all finer resolutions. A gap or center read depends on the supplied probe
domain, its boundary and the selected occupancy. Those premises remain part
of the observation. The radius-based probe footprint also does not give
zero-width ink a nonzero-area or space-filling promise.

Co-occupancy can be a soft density or spacing signal. Several strokes may
meet at an ordinary join or be detected by the same coarse probe without
sharing a positive-length segment. Such co-occupancy is not itself the hard
retracing violation.

The comparison keeps the vertical probe band fixed at `[-8,8]`.
The initial interpretation uses horizontal/vertical reference spans `H=4,V=4`.
The secondary `H=6,V=4` interpretation repeats the hard geometry checks; it
does not recompute occupancy utility or rerank the archive. This positive
horizontal rescaling is an affine aspect-ratio sanity check of the retained
geometry and calibration correspondence. It is not independent evidence for
arbitrary changes to individual reaches. These spans are explicit supplier
parameters, rather than distances stored in the qualitative generator.

## Symmetry and periodic observation

The group inspiration is the usual discrete frieze setting: translation in
one direction, with possible reflection, glide reflection and half-turn
symmetries. There are seven frieze groups; a half-turn is the only nonidentity
rotational symmetry compatible with a frieze group. See
[David Eck's symmetry notes](https://math.hws.edu/eck/js/symmetry/symmetry-info.html)
and [Vicky Neale's Oxford frieze examples](https://people.maths.ox.ac.uk/outreach/neale/Frieze_groups.html).

Here a measured symmetry test transforms the ink support and checks coverage
in both directions, retaining unmatched portions. The transform can preserve
the pattern while reversing or otherwise changing its traversal. A causal
history, directed stroke sequence and unlabelled ink set are different reads.
Labels, color or direction could break a symmetry that this unlabelled support
test accepts.

Wrapping a periodic probe domain identifies equivalent positions for that
observation. It is a quotient of the chosen translation construction, not an
additional frieze symmetry group or an inferred physical connection between
otherwise distinct sites. A finite cropped picture alone does not establish
infinite repetition; the generator and complete control-return premise supply
the selected periodic construction.

## Bounded feedback search

Candidates keep the existing event/phase graph, frame and reach identities.
An edit swaps existing phase-to-word bindings through an explicit alternative
output law. The old candidate and its evidence remain available; an edit does
not retroactively alter the old run. This grammar permits bounded changes to
what a phase emits, rather than arbitrary new algorithms.

Phase count and succession, event timing/trigger structure, sampling routes,
reach identities and the word bag are fixed for each profile. The search
changes word order; it does not learn new phases, durations, reach amplitudes,
weights or primitive algebra. The optional horizontal/vertical measurement
spans remain supplied interpretations.

The hard-rule stage precedes preference ranking. The experiment retains each
candidate, its edit or source, structural report, measured refinement and
objective observations. Unresolved evidence is not silently promoted to a
hard-rule success. Finite candidate and execution budgets bound the search;
the feedback changes which declared candidate is retained for subsequent edits.

[`FriezeLearning`](../../experiments/QualitativeFrieze/FriezeLearning.cs)
exposes the initial output law, a word bag with repeated participation, a
generic pair-exchange template, the phase and archive capacity scopes and the
proposal budget in `SearchSurface`. Permutations preserve that authored bag;
the search does not introduce new words or change their multiplicities.

`ArchiveHistory` records the actual feedback memory: archive before, proposal,
archive after, dropped entries and adjacent rank comparisons, including the
discard boundary. The archive has 12 slots. A comparison between one parent
and its proposal is retained separately; it is not substituted for archive
admission, because a proposal worse than its parent can still enter the
archive. Subsequent parents come from the actual retained archive.

The profiles are:

| Profile | Emission family | Crossing policy | Initial search bound |
| --- | --- | --- | --- |
| Orthogonal | Axis-aligned words | Straight-through crossings rejected | 240 candidates |
| Meander | Axis-aligned bag including one westward word | Straight-through crossings rejected | 240 candidates |
| Diagonal | Combined-axis words allowed | Straight-through crossings allowed | 240 candidates |

Every profile retains the no-retracing and rightward-period requirements and
a cap of 12 phase participations. The profile names are presentation; their
grammar, policies and objective evidence must remain connected in the model.
The exact executed candidate counts belong in the run results.

The meander name describes the authored bag. There is no additional meander
reward. The initial bags contain eight, nine and eight phase bindings for
the orthogonal, meander and diagonal profiles respectively. Their permutations
already preserve the bag's net horizontal/vertical participation; the search
learns useful orderings under the stated rules rather than discovering
rightward progression from an unrestricted grammar.

An optional numeric Gojo equation can rank supplied observations of occupancy,
spacing and symmetry. The current utility penalizes empty probes,
co-occupied probes and clearance spread, and rewards verified symmetry reads.
Complexity remains a hard phase cap and is not a soft utility term. The numeric
weights and resulting values belong to this measured utility interpretation.
They do not choose hidden numeric positions for the source or turn a failed
hard rule into a pass.

The interpreted cost is:

```text
4096 * (empty probes / probes)
  + 4096 * (multiply occupied probes / probes)
  + 1024 * (center-shell spread / (greatest shell + 1))
  - 32 * verified symmetry-kind count
```

These fixed terms are connected through native `Equation.Sum` and
`Equation.WeightedPort` structure. The actual ranking uses their selected
integer-tick projection; the displayed expression does not promise an
unrounded rational score. Each prediction and its residue remain in the
utility witness. Shell indices are derived ordinal observations of adjacency,
and the symmetry count covers the tested operation kinds that verified; it
is not the number of elements in an infinite symmetry group. No weight is
learned in this checkpoint.

Archive ordering first compares hard-failure severity and then utility for
admitted candidates. The hard count is an execution-dependent selection read,
not a universal total of independent violations: an early missing-period
failure can contribute one while its structural report retains several
obligations, and measured checks can expose further counterexamples. Skipped
suppliers remain unqueried. Nonzero measured height is also an explicit
admission condition, preventing a straight baseline from winning the filling
objective.

All 720 generator/trace pairs retained zero carriers and zero carrier locations,
checked with identity-visited traversal. Realization, measured rule witnesses and
numeric utility are deliberately separate structures and can contain both.
The diagnostics record this boundary for every candidate.

This is feedback search over an authored grammar and authored local algebra.
It does not discover the primitive laws, infer the grammar from examples,
learn an unknown physical environment or establish a general learner.

## Results and verification

The recorded run used 240 distinct candidates per profile, seed 431 (successive
profile seeds 431, 432 and 433), a 12-entry elite archive and the fixed
interpretations above. Each deliberately clustered initial bag failed a hard
rule. All three searches found a hard-admitted generator. Lower cost is better;
rejected candidates are unranked, so their sentinel execution cost is excluded
from comparisons.

| Family | Hard-admitted candidates | First admitted generation | First admitted cost | Retained cost | Coarse rejections | Coverage queries |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Orthogonal | 62/240 | 2 | 3604 | 3244 | 178 | 62 |
| Meander | 1/240 | 144 | 3946 | 3946 | 197 | 1 |
| Diagonal, crossings allowed | 135/240 | 2 | 4133 | 3820 | 78 | 135 |

Generations start at zero. The meander result is an instructive limit: this
seed needed the 145th evaluation to find one legal program and found no further
soft improvement within the budget. A 120-candidate check did not find it;
the regression therefore exercises the recorded 240-candidate default. A
separate exhausted-budget test checks that failure remains failure.

The orthogonal search produced a square wave with translation, vertical mirror,
half-turn and glide support. Its clearance-center spread fell from two graph
shells to zero. The meander retained translation and half-turn symmetry. The
diagonal search retained a diamond-on-baseline motif with translation and a
vertical mirror; its clearance spread fell from six to four shells. The
retained probe-empty fractions were 66.2%, 54.4% and 68.4% respectively. These
are improved or admitted finite probes, not fully filled strips or a claim
that whitespace and overlap reached zero.

The crossing-allowed profile admitted 30 crossed candidates. The gallery also
shows the first and best of that subset: costs 4349 and 3888, with one proper
crossing per construction period and no retracing. The overall diagonal winner
has no proper crossing; permission to cross is not a reward for crossing.

Across the 720 proposals, 453 (62.9%) stopped at the structural stage. Only 198
needed the coverage objective. The 465 measured reads comprise 267 primary
reads and 198 secondary aspect checks. These counts demonstrate avoided
refinement work, not a wall-clock benchmark or a speedup over another learner.
Every admitted candidate had zero pending measured/coverage obligations under
its selected interpretation and passed both hard geometry aspect checks.

Visual review exposed and corrected a real rule failure: a line crossing at a
sample boundary was previously counted as several T contacts. The continuous
support-ray test now rejects it and is regression-tested under subdivision,
reversal and periodic seams. That correction made legal meanders much rarer;
the results above use the corrected rule.

Native edit and archive histories are returned by the API. The JSON is their
derived diagnostic projection, not a serialization of the entire native graph.
The recorded identity-visited history sizes were 1,945,138, 991,067 and
3,974,063 elements. The feedback archive is bounded; retaining every detailed
counterexample and numeric witness still has substantial memory and traversal
cost. A compact evidence plan remains future work.

Verification: `dotnet test Gojo.slnx -c Release --no-restore --verbosity minimal`
passed all 3,042 tests across 12 test projects, including 612 qualitative
execution tests. This checkpoint adds 79 cases covering the structural rules,
exact periodic geometry, native coverage, supplier boundaries and learning.
The generated SVG and PNG preview were visually checked after the crossing fix.

Reproduce the bounded run with:

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --learn --trials 240 --seed 431 --output experiments/QualitativeFrieze/results/learning
```

The output targets are the [learning SVG](../../experiments/QualitativeFrieze/results/learning/gallery.svg),
[HTML](../../experiments/QualitativeFrieze/results/learning/gallery.html) and
[diagnostics](../../experiments/QualitativeFrieze/results/learning/diagnostics.json).
The [PNG preview](../../experiments/QualitativeFrieze/results/learning/gallery-preview.png)
shows the checked gallery; HTML toggles expose probe occupancy and the selected
stroke radius.

## Remaining boundaries

The main unresolved questions concern how much of the global segment and
spacing analysis can be answered through stronger qualitative order
constraints, how candidate edits should extend beyond fixed output bindings,
and how to retain useful feedback without unbounded histories. A small finite
search can demonstrate those mechanisms at one scale; it does not settle their
general efficiency or the utility of the chosen aesthetic proxies.

Useful next experiments are explicit phase-rotation proposals with preserved
start/landmark correspondence, qualitative order constraints that can replace
some global measured queries, and continuation-aware intersection rules.
The rare legal meander gives a concrete search counterexample for the first;
the bent-interleaving regression marks the current crossing boundary. Curves,
learned reach extents, changed event schedules and learned objective weights
remain outside this checkpoint.

The present search retains complete candidate traces, geometry witnesses,
probe publications and archive history. That provenance can be much larger
than the generator it explains. Retained node counts and memory cost should
be reported separately from source complexity; no compact long-run memory or
execution-speed improvement is established by this experiment.

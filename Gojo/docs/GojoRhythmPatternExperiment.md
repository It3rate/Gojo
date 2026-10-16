# Layered rhythms and thick diagonal patterns

This implemented experiment composes qualitative phase sources, reads their
actual contributions, and supplies an exact measured square-brush footprint.
It adds descriptive process features and comparisons inspired by rhythm
research. The source stays pre-numeric; no kernel primitive was added.

The later [rhythm-to-frieze checkpoint](GojoRhythmFriezeExperiment.md) uses
these onset ideas to choose whole turning phrases, including backward reaches,
nested keys and connectors, rather than only rise/return excursions.

The accompanying [research note](GojoPatternFitnessResearch.md) reviews six
primary studies and distinguishes empirical findings from the proposed Gojo
fitness features. No human preference predictor has been trained or validated.

## Run and inspect

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --rhythms
```

Outputs:

- [Interactive gallery](../../experiments/QualitativeFrieze/results/rhythms/gallery.html)
- [Main figure](../../experiments/QualitativeFrieze/results/rhythms/gallery.svg)
  and [preview](../../experiments/QualitativeFrieze/results/rhythms/gallery-preview.png)
- [Thin, thick and stretched interpretations](../../experiments/QualitativeFrieze/results/rhythms/styles.svg)
  and [preview](../../experiments/QualitativeFrieze/results/rhythms/styles-preview.png)
- [Feature counterexamples](../../experiments/QualitativeFrieze/results/rhythms/controls.svg)
  and [preview](../../experiments/QualitativeFrieze/results/rhythms/controls-preview.png)
- [Diagnostic projection](../../experiments/QualitativeFrieze/results/rhythms/diagnostics.json)

The JSON is a diagnostic projection, not a restorable graph. The figures use
whole native footprint proofs, then explicitly selected periodic copies and a
three-period display window. Display coordinates and colours are derived views.

## What runs together

```text
actual root phase
  -> explicit routes to separate phase cycles
  -> bounded FIFO accepted-event cascade
  -> actual phase/output/motion applications
  -> one selected root sample
  -> formal additive cancellation and position
  -> selected measured path
  -> filled brush support and presentation
```

Several contributors share the same sampling boundary. They do not execute on
parallel CPU threads, and the grouping does not establish physical simultaneity.
Every accepted event and application retains its actual order. The source
lane diagrams are observations of the contributors to one cursor. They do not
create additional independent material cursors. Separate gallery examples run
independently; the earlier [layer experiment](GojoPatternLayerExperiment.md)
also demonstrates an explicit common-frame observation of separate traces.

`FriezeFixture.LayeredCycles` explicitly connects every root phase to every
offered layer port. Slow motion and pauses belong to each actual phase chain;
the runner does not schedule them with a host modulo counter. The library's
`PeriodRounds` is a derived LCM for this full-fanout construction. Native trace
tests independently establish the actual first complete control return.

## Sources and their retained interpretation

The first source combines:

- a shared rightward reach on each root event;
- a sixteen-phase slow layer: four repeated upward participations, seven
  explicit pauses, four downward participations, seven pauses;
- a two-phase fast layer alternating one upward and one downward participation.

The vertical contributions share an actual unmeasured reach, so their opposed
participation can cancel under the existing motion law. A second example
changes the fast layer's starting phase in the same graph. A third adds an
eight-phase middle layer with four up phases followed by four down phases.
Cancellation can create flat steps even though more sources participate.

The two pulse examples start from actual slot-to-hit/rest relations. A finite
law maps each hit to an upward leg and a return leg; a rest maps to two explicit
empty words. The actual source slot, both target phases, finite-law reads and
resulting fixture output rows stay connected. The examples are sixteen-slot
regular pulses and an eight-slot 3–3–2 grouping. Declared anchor subsets are
premises about meter, not a detected listener beat.

The horizontal participation gives those pulse legs an ordered drawing
interpretation. It does not supply seconds, tempo, an audio waveform, pitch or
instrument identity. Those can be introduced through additional explicit
adapters. Chord progressions need actual voice/pitch/context structure before
any harmonic-pleasure model would apply.

## A diagonal's filled support

`DiagonalRibbonSupplier` supplies one selected geometric law:

```text
nib = (actual-extent -> ordered-x/y-spans) -> actual-four-corners
sum = actual-stroke-endpoint -> (actual-nib-corner -> translated-sum-point)
polygon = ((actual-stroke -> actual-period-translation) -> nib) ->
          (ordered-hull-vertices -> (ordered-edges -> all-eight-sums))
```

The filled footprint is the convex sweep of the selected axis-aligned square
along the actual sampled segment. Horizontal, vertical and all diagonal
directions use this same law. Width is the full width of the square's axes.
A diagonal therefore has a different perpendicular thickness from an
axis-aligned stroke. This is an explicit square brush, not a claim of constant
Euclidean perpendicular width.

Odd widths retain exact half-tick corners. BigInteger intermediates protect
orientation and hull arithmetic; unrepresentable stored coordinates remain
unresolved. The polygon reader checks source identity, actual nib corners,
every endpoint/corner sum, ordered hull vertices and actual boundary edges.
The interpretation reader also checks trace, calibration, measured period,
requested width and nib-extent provenance. A zero-length sample becomes an
explicit square dab under this law, even though the centerline reader omits
zero-length lines.

This supplier preserves whole polygons and their overlaps. It does not claim
an exact finite-cell area partition, union boundary or whitespace medial axis.
The old rectilinear cell supplier keeps its original contract. Feeding diagonal
support into the field-rule experiments will require exact clipping/arrangement
structure or a declared approximate cell read with retained boundary uncertainty.

## Descriptive features and fitness

`FriezeWordFeatures` groups actually emitted ordered words through native
move correspondences. Frame face, reach identity and word multiplicity remain
visible. Equivalent words do not identify their distinct samples or histories.
It records actual cyclic transitions and possible continuations under a
declared **current-word-only** context.

```text
word = actual-sample -> actual-ordered-applications
reuse-group = representative-word -> actual-word-correspondences
transition = (actual-before-word -> actual-after-word) -> (before-group -> after-group)
continuation = actual-group -> actual-transitions-from-that-group
```

Multiple successors mean that the current word alone does not determine the
next one. They are not probabilities, entropy, physical randomness or human
surprise. The full phase recurrence can remain completely deterministic.
Known legality violations and unresolved legality are separate from feature
completeness: a known retracing cycle still has readable word transitions.
Unknown net displacement can likewise coexist with a known emitted sequence.

Cross-port face comparisons retain actual sample/application/port pairs,
shared axes and same-direction or actual opposed-direction premises. These
read alignment at a selected event context. They do not establish equal
amplitude, net motion, independent sources, or aesthetic goodness.

The selected main interpretations give:

| Source | Root period | Source phases | Emitted word classes | Changed-word transitions | Current-word contexts with alternatives |
| --- | ---: | ---: | ---: | ---: | ---: |
| Slow square + fast triangle | 16 | 19 | 4 | 16 | 1 |
| Fast phase opposed | 16 | 19 | 4 | 16 | 1 |
| Slow + middle + fast | 16 | 27 | 6 | 16 | 2 |
| Sixteen-slot regular pulses | 32 | 33 | 3 | 12 | 1 |
| Grouped 3–3–2 pulses | 16 | 17 | 3 | 9 | 1 |
| Folded key with the same brush | 7 | 7 | 5 | 6 | 2 |

All selected centerlines have zero positive-length overlap and zero crossings
under the chosen calibration. The first two have a verified half-turn; the
three-timescale example retains translation but fails that half-turn read.
These are centerline properties, not automatic certificates for the filled
support. All four supplied width/aspect interpretations preserve the source.

An important aggregation counterexample appears in the first two rows. Each
has four same-face and four opposed-face cross-port application pairs. A
total alignment count cannot distinguish their phase origins. Conditioning on
the actual slow upward versus downward event can: one phase agrees at the
rise and opposes at the fall; the other does the reverse. Context belongs to
the feature's basis, rather than being detail to average away automatically.

The controls retain further counterexamples:

- Constant drift has one word, no changed-word transitions and every tested
  centerline symmetry. A reward for simplicity and symmetry alone can select it.
- `A B A B` and `A A B B` have the same word counts but four versus two changed
  transitions, and zero versus two current-word contexts with alternatives.
- Regular pulses return their full controller state after 32 roots, while their
  selected centerline repeats after eight roots. Control identity and shortest
  observed pattern period remain distinct.
- Colours are assigned locally to emitted-word classes. The same hue in two
  examples does not assert exact word identity. Lane colours identify the
  displayed actual ports, under a separate presentation mapping.

As a visual assessment, I find the slow-plus-fast examples retain a clearer
large envelope than the three-timescale example; the latter makes a more
stepped silhouette. Colour helps inspect contributions but can emphasize joins
that are less salient in the monochrome read. These are an AI critic's
contextual observations, not measurements of human liking. The gallery keeps
both interpretations so those judgments can be challenged.

## Research and next selection step

The research supports testing recurrence, reuse, local change, coordination,
symmetry, gap structure and metrical conflict as separate contextual reads.
For example, [Witek et al.](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0094446)
found the highest average groove ratings at moderate syncopation in their
drum-loop experiment. [Mather et al.](https://www.nature.com/articles/s41598-023-47835-w)
found substantial individual differences in joint symmetry/complexity
preferences. Neither result sets a universal Gojo fitness optimum.

The next training experiment can offer a small edit set—phase changes,
contribution routing, shared-reach multiplicity, word substitution and view
parameters—while keeping hard legality, several descriptive objectives and an
external pairwise preference separate. That permits useful authored profiles
and later personal preference learning without treating maximal regularity or
random variation as universally good. The full seven-part feature observer,
audio input adapter and learned aesthetic model remain future work.

## Verification

All **43 new focused cases pass**. They cover actual phase returns, onset and
anchor provenance, causal contributions, direction-conditioned pair scopes,
word reuse and continuation, known violations versus uncertainty, diagonal and
zero-length sweeps, half-tick corners, signed periodic copies, overflow/budget
obligations and forged or omitted proof rejection.

`dotnet test Gojo.slnx -c Release --no-restore --verbosity minimal` passes
**3,177 tests across twelve test projects**, including 747 qualitative cases.
The main, style and counterexample figures were visually inspected; the
combined HTML has 36 unique clip identifiers with no missing references.

No kernel behavior was changed. The combined native source/analysis graph has
76,708 element identities, **zero numeric carriers and zero numeric locations**.
Measured brush and display witnesses remain separate structures. These counts
include retained process evidence, not only compact source programs; no memory
compression or computation-speed claim follows from this experiment.

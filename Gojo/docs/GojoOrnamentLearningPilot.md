# Ornament learning pilot

This is a first persistent symbolic learner above the existing qualitative
execution layer. It learns native boundary routes and their subroutes from
executed examples. It does not yet learn an entire ornament grammar.

The earlier experiments share substantial machinery: `OrnamentProgram`,
native publications, junction and interior readers, revisable templates,
rule packs, and query-selection experiments. They have not been continually
updating one common learned model. Most construction vocabularies, defaults,
recognition questions and scoring laws were authored. This pilot makes that
distinction explicit and introduces a portable learned model that another run
can actually load.

## Run and inspect

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-learning --count 4096
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-learning --count 1536 --family-holdout --output experiments/QualitativeFrieze/results/ornament-learning-family
node experiments/QualitativeFrieze/tools/check-ornament-learning.cjs
```

Use `--seed N` for a different deterministic stream, `--examples N` to bound
the inspectable gallery, and `--model PATH` to evaluate a previously saved
model without retraining. A loaded model is frozen. Incremental training and
distributed checkpoint merging are not implemented by this pilot.
`--refresh-gallery` rebuilds the presentation from the saved model and
evaluation rows without changing training or metrics. An arbitrary external
checkpoint does not establish training/test separation; its provenance is
explicitly unverified unless this invocation performs the training itself.

The [gallery](../../experiments/QualitativeFrieze/results/ornament-learning/gallery.html)
separates teacher constructions, receiver observations and sampled learned
proposals. Its [overview](../../experiments/QualitativeFrieze/results/ornament-learning/overview.html)
shows selected successes, ambiguous cases and failures. Dataset descriptors,
per-row evaluation, model checksum and aggregate counts are saved alongside
the portable `model.gojo.json.gz` checkpoint.
`retained-episodes.gojo.json.gz` additionally preserves the inspected receiver
streams, proposed correspondence witnesses, and selected-hypothesis-to-native
execution links. It excludes teacher recipes and omitted-complement audits.

## Dataset and information boundary

The deterministic curriculum has twelve authored starting constructions and
eight edit schedules: retained shape, removed closing edge, local excursions,
divider, nested contour, repeated excursions, exterior spur, and excursion
with an opening. Native transforms vary orientation, reflection, repeated
uncalibrated participation and translation. The full demonstration can execute
one to four units through the existing program engine.

Every sample provides several exposed streams of partial passages and sparse
landmark bindings. Some have deliberately conflicting bindings; others omit
the basis promise or leave inventory open. Teacher graphs, omitted-complement
audits, construction histories, family names and discovered teacher boundaries
are not receiver inputs. A complete-cue control occurs in part of the stream.
Non-control samples retain at least one passage missing from the union of the
ordinary streams. These are synthetic curricula, not independent samples of
historical ornament styles.

The main partition keeps every size/orientation/reflection sibling of a
recipe/edit combination together. It allows different edits of a common
starting construction across partitions. Exact and coarse prototype overlap
are consequently reported, rather than presenting this as new-family
generalization. A second, stricter protocol reserves entire families: hexagon
and sawtooth for validation; star and cross for testing. Those choices are
fixed before results are inspected.

## What is learned

Training uses native interior discovery to obtain cyclic side routes. A
canonical route preserves its actual uncalibrated words and shared reach
identities while allowing a changed starting vertex, traversal direction,
quarter-turn and reflection. One source episode contributes at most one
exposure to a given prototype, regardless of repeated cells in that scene.

Two-to-four-edge windows supply open route fragments with native occurrence
maps. Fragment support counts matching positions and source exposures; a
large count does not automatically establish recurrence across independent
scenes. The window proposal rule itself is authored. No labels such as
"square", "notch" or "rosette" supervise this memory.

The saved model contains executable native route structure, exposure
occurrences and fragment correspondences. At most 32 detailed source audit
witnesses are retained while the corpus streams through training. The
forgotten complement remains an explicit open surface. This prevents a
nominally compact model from quietly retaining the complete training corpus.

## Evidence, hypotheses and generation

The receiver follows advertised degree-two paths, preserving actual shared
site identities. It attempts to fit multiple streams under one common
orientation/reflection and one injective site correspondence. A partial stream
can end between the corners of a merged learned side; failed corner-only
matching then leaves an explicit interior-placement obligation open. It does
not establish a completed embedding. A clear contradictory observation
cannot be overridden by a frequent prototype. Search cuts and unknown
uncalibrated extents remain open.

`FromSource` expects an already public observation. A supplier must publish
once and then partition the public packet, retaining aliases across streams.
The matcher does not sanitize private execution provenance on its caller's
behalf. The curriculum and transfer probes both honor this boundary.

The current matching projection is deliberately coarse: native directional
routes and their shared sites. It does not establish agreement of every
length, calibration, figure/ground interpretation or source ownership. Its
loss is a native open surface. "Admissible" means compatible in that selected
read, not a complete reconstruction or a calibrated likelihood.

Empirical prototype and fragment support rank compatible alternatives. Those
ranking allocations are not probabilities. Baselines use the same admissible
dictionary with canonical ordering, exposure frequency alone, or a seeded
random order. A first-stream ablation measures what the other streams add.
The model is frozen before either validation or test examples are read.

Generation executes the selected stored native route through the existing
unit/drawing engine, then runs the independent native interior reader over
the new publication. Additional local edit probes refine a real learned chord
while preserving its net displacement and parent correspondence. That edit
policy is authored; the pilot does not claim it was learned.

## Evaluation and limits

Teacher agreement measures whether a proposed route matches *one discovered
teacher boundary*. It is not a whole-scene reconstruction score. Several
admissible completions can be legitimate interpretations of partial evidence.
Targets without a readable closed boundary are reported separately, never
converted into negative concept labels. Exact native-word agreement and
coarse directional-route agreement have separate counts.

The principal checks are partition isolation; usable scenes versus requested
rows; seen versus unseen route keys; compatible hypothesis count; conflict
abstention; teacher preference at ranks one and three; first-stream and
ranking ablations; independent generated closure; and native checkpoint
transport with unchanged rankings. Timing and peak process memory are saved
to guide larger runs; summary timing/memory snapshots precede final gallery
and retained-episode serialization. Six existing Bourgoin programs are also
probed without training on them. Their independently discovered local
dictionary coverage is reported separately from a whole-scene hypothesis.

The main outstanding gap is composition. A dictionary of individual boundary
routes cannot necessarily explain a grid, a divided cell, or several linked
motifs as one hypothesis. Strictly unseen routes cannot be recovered by pure
whole-route retrieval. Learning fragments is a first ingredient; assembling
them with shared seams, disposition and defaults is a distinct next step.

Other gaps are learning which evidence to request, learned default revision,
conditional edit policies, a common objective across recognition and program
generation, calibrated confidence, and evaluation on independently encoded
ornaments. Existing rule packs and query-selection studies are mechanisms to
connect, not capabilities automatically acquired by increasing dataset size.

The immediate progression is therefore: establish measured retrieval and
abstention baselines; add compositional route/program proposals; enforce
native seam and crossing obligations; train query and edit selection with
held-out outcomes; then scale the curriculum and move to independently
encoded historical examples. Multiple motif, disposition, construction and
figure/ground readings must remain distinct connected evidence streams.

An important next training objective is query-conditioned selection. The
present frequency prior is estimated from route exposure, not fitted to the
success of completions under partial messages. Among compatible alternatives,
the route-fit reward is often identical. More exposures alone need not improve
that ordering. Fit proposal/query/edit selection only on training episodes;
keep structural laws and the independent critic outside that fitted policy.

## Recorded results · 21 October 2026

The main run has 4,096 descriptors from 96 recipe/edit groups. Its 2,859
training rows yielded 2,263 usable scenes and 3,744 observed boundaries. The
saved model has 175 native prototypes, 25 coarse directional route types and
896 mined fragments. Validation has 809 rows from 19 held-out groups; test
has 428 rows from 10 groups. Descendants are correlated variants, not thousands
of independently invented ornament designs.

| Held-out test read | Result |
| --- | ---: |
| Rows with a readable teacher boundary | 316 |
| Learned top choice agrees with a teacher coarse route | 70 / 316 |
| Exposure-only ordering | 70 / 316 |
| Fixed canonical ordering | 30 / 316 |
| Seeded random ordering | 33 / 316 |
| Top three include a teacher coarse route | 79 / 316 |
| Unconflicted rows with a familiar coarse teacher route | 197 |
| Intended route remains unrefuted in that cohort | 147 / 197 |
| Intended route ranks first in that cohort | 70 / 197 |
| Exact familiar, single-boundary hypotheses retained | 47 / 49 |
| Deliberately conflicting test rows: abstentions | 120 / 120 |

"Unrefuted" includes open correspondence obligations. The two exclusions in
the 49-case cohort are indices 3147 and 3339: a diamond plus an exterior spur,
presented with full cues. The teacher reader discovers the diamond and memory
contains it, but one boundary cannot explain the additional branch. These are
direct targets for read focus with an explicit carried complement.

Across validation and test, all 348 deliberately conflicting cases abstained.
All 774 selected native route executions had independently discoverable closed
boundaries. That establishes executable closure, not preservation of every
input fact. The fragment contribution did not change top-choice agreement
beyond exposure frequency. Multi-stream top-choice agreement was lower than
first-stream agreement; extra observations can reject a teacher-preferred
whole-scene simplification, and the current matcher lacks a sufficiently
selective local focus.

The stricter 1,536-descriptor rerun reserves whole construction families. Its
test partition has 256 rows from two families, with 192 readable teacher
boundaries. Only two top choices agree; none of the 160 unseen coarse teacher
routes are recovered. The two agreements concern shared familiar local
structures, not newly composed star/cross programs. This is the expected
limitation of an episodic whole-route dictionary, now measured explicitly.

Six actual, untrained Bourgoin programs provide 53 readable local boundaries;
21 have coarse dictionary coverage and four have exact native-word coverage.
Coverage is calculated after independent native face discovery, not supplied
as a scene-level learned answer. Bricks and diagonal-tied meshes transfer
some local routes; honeycomb and serif-I fields expose missing vocabulary or
different basis choices. The latter can also exceed the 128-route read budget.

The main run trained in 29.7 seconds and reached its pre-gallery summary in
171.3 seconds. The strict-family run trained in 10.3 seconds and reached its
summary in 79.8 seconds. These include native model transport checks and six
Bourgoin probes; they are single local runs, not throughput benchmarks. Peak
working sets before gallery serialization were approximately 1.01 and 1.04 GiB.
The main model gzip is 951,736 bytes; its retained receiver episodes occupy
3,868,156 compressed bytes. No GPU, remote model or cloud training service was
used. Earlier low training-phase memory snapshots are not lifetime peak RAM.

Verification: `dotnet test Gojo.slnx` passed 4,927 tests during development;
the final 27 focused learning tests passed after the remaining fixes and
denominator additions. Browser/data checks cover 52 main and 28 family-holdout
states, 278 SVG files and all retained native archives, with no browser errors.

The immediate learning priorities are local occurrence focus plus remainder,
compositions that connect motifs through shared seam obligations, and ranking
trained from query/outcome episodes. Scaling the exposure count alone has no
demonstrated route to those missing abilities. This pilot provides the dataset
boundary, frozen evaluations, portable model and native explanations needed
to test those next mechanisms.

## Source

- [Corpus](../../experiments/QualitativeFrieze/Learning/OrnamentLearningCorpus.cs)
- [Native memory and matching](../../experiments/QualitativeFrieze/Learning/OrnamentLearningMemory.cs)
- [Training and evaluation](../../experiments/QualitativeFrieze/Learning/OrnamentLearningExperiment.cs)
- [Measured gallery adapter](../../experiments/QualitativeFrieze/Learning/OrnamentLearningGallery.cs)
- [Native interior discovery](GojoNativeInteriorDiscovery.md)
- [Reusable rule packs](GojoReusableRulePacks.md)
- [Earlier query-selection training](GojoEvidenceSelectionExperiments.md)

# Sparse communication, concrete samples and independent reads

This experiment tests a small version of a sender–receiver loop. The sender
cares about selected structural properties, publishes a subset of those hints,
and leaves the rest unspecified. A receiver selects from a finite library of
executable constructions. A separate reader examines a fresh publication of
the chosen output. Only then does the evaluator compare that evidence with the
sender's retained requirements and propose an additional hint.

The distinction matters: a successful receiver selection is not proof that the
sender's concept survived. The output needs its own evidence. Conversely,
several different samples can satisfy the same partial description.

Open the [interactive gallery](../../experiments/QualitativeFrieze/results/communication-loop/gallery.html)
or the [complete before/after overview](../../experiments/QualitativeFrieze/results/communication-loop/overview.html).

## Recorded small run

The library contains fourteen acknowledged constructions: seven topologies at
two different unmeasured extent choices. Two published preference orders use
the same library. Every selected program is then executed afresh; actual
public-port correspondences replay its additional seam declarations. The
reader receives a fresh observation of this new execution, rather than the
cached library graph or its earlier inferred occurrences.

| Idea retained by sender | Initial transmitted clues | Initially admissible outputs | Hints after repair | Admissible outputs after repair |
| --- | --- | ---: | ---: | ---: |
| One actual four-arm junction | Horizontal and vertical endpoint patterns | 6 | 3 | 2 |
| A closed four-edge contour | One two-ray corner pattern | 4 | 2 | 2 |
| A connected alternating walk | One diagonal endpoint pattern | 4 | 2 | 2 |

Both preference orders initially choose a plausible construction that fails
the sender's retained topology requirement. Adding one previously unsent,
actually refuted pattern clause repairs all six trials. Earlier samples,
failure witnesses, sent messages and repairs remain inspectable.

This result is deliberately modest. The repair explicitly communicates the
initially withheld target pattern. It demonstrates preservation of that now
explicit promise through independent execution and reading. It does not
demonstrate success on a still-untransmitted question, learned decoding or
generalization beyond the library.

An exhaustive ablation checks all sixteen subsets of the available clauses
(eight for the junction, four each for contour and walk), each against all
fourteen library outputs. A message is sufficient only if its admitted set is
nonempty and **every** admitted output satisfies the sender's requirement.
Testing only the preferred sample would allow a receiver-prior shortcut.

Each case has a single minimum-cardinality sufficient message: its one
topology clause. Earlier endpoint/corner hints become redundant and can be
omitted. Both extent choices remain admissible. This supplies a concrete
finite-search baseline for future hint-selection training: repair need not
mean indefinitely accumulating detail.

The one-clause messages contain 197, 205 and 198 reachable native participants
respectively, including the declared shared frame and scope structure. These
are verbose structural witnesses rather than a compact wire format. The full
capture retains 245,335 participants and zero numeric carriers or locations;
its compressed archive is 2,176,310 bytes. Restoring that archive preserves its
reachable participant count. Message counts alone exclude the substantial
shared library and reconstruction work.

## Native contract

```text
pattern = declared heading frame -> reusable role graph
role graph = (frame -> (closed site roles -> closed edge roles))
             -> (distinguished centres -> ordered routes)
message = Closed(required pattern surfaces) -> Closed(forbidden pattern surfaces)

receiver context = exposed executable library -> ordered sampling preference
candidate read = (message -> output observation) ->
                 (supported -> (refuted -> unresolved))
selection = context -> (retained candidates -> selected executable)

execution -> actual graph -> history-blind observation -> induced patterns
comparison = evaluator-only requirements -> independently induced output
repair = (previous message -> failed requirement witness) -> revised message
```

The closed message scopes inventory the transmitted hints. They do not declare
every omitted property false, free, or unavailable. Explicit forbidden patterns
are separate from omission. A required pattern asks for an occurrence; it does
not require the whole output to be identical to that pattern.

Sites, edges, headings, distinguished centres and ordered routes carry the
pattern's meaning. Names and signatures are diagnostic or derived search
indexes. A successful comparison needs explicit role and edge correspondences
and actual output occurrence bindings. No noun label supplies recognition.

## Boundaries

The receiver is a bounded library sampler, not a general constraint solver or
a trained generative model. Its preference order is an exposed prior, not a
probability distribution or a discovery of the sender's intended answer.

The independent reader receives actual incidence and coarse headings, with
fresh identities and without source paths, reach magnitudes, intended labels,
or generating history. Rendering does not feed recognition. The evaluator can
retain the sender's intent and external provenance, but the reader cannot
consult those to manufacture a match.

Pattern correspondence permits reordered star roles/edges and cyclic-start or
reversed-traversal alternatives for paths. It retains the actual endpoint,
heading and occurrence mappings; it does not equate mirror, rotation and frame
reversal. The shared frame correspondence here maps like named presentation
directions. Arbitrary changes of basis need their own supplied transform.

A failed commitment is an observational claim remainder. The sender's explicit
repair law publishes another clause; it does not create carrier tension,
mutate the observed graph or silently settle a physical transfer.

Pattern discovery covers the configured junction stars, short simple walks
and bounded simple cycles. A demonstrated absence is absence under that
declared grammar and complete bounded search. Exhaustion leaves absence open.
It is not absence of every possible interpretation. A closed cycle does not
establish a metric square, a filled face or border ownership. Coarse headings
do not establish equal lengths, right angles or tangency.

## Toward a larger training experiment

Keep the engine and evidence reader distinct from policies that choose hints,
propose constructions, ask questions or request refinements. The first useful
learned policy would choose which structural hint to send next, with a finite
search baseline. There is no need for an adversarial discriminator initially.
The evaluator can ask explicit held-out structural questions.

A practical next batch would start with about 1,000 native construction
templates, four receiver libraries and eight sample/context choices per
template: roughly 32,000 output episodes before repairs. This is a proposed
scale, not a run already performed. Begin with deterministic exhaustive or
greedy hint selection as the baseline, then fit a small policy to those
decisions. Compare a model-free search policy and a learned policy on the
same held-out construction families and compute budget before choosing a
larger model architecture.

Stream those episodes and reuse the receiver definitions. Keep compact
training projections with explicit correspondence to the native evidence;
retain full witnesses for failures and a sampled audit set. The present full
archive is intentionally verbose, so multiplying its storage and repeated
analysis by 32,000 would be a poor execution plan. Record retained and omitted
evidence separately instead of promising exact replay from a lossy record.

Keep a second set of questions that is never eligible for transmission. For
example a local junction hint can be assessed against a larger connected-walk
question, or a contour message against a requested seam edit. Performance on
those questions is stronger evidence of reusable understanding than the
current run's explicit target-clause repair. Native failures can become new
counterexamples rather than a scalar reward alone.

Generate a dataset from independent construction families: continuous paths,
separate sides, parallel rails, tiled units and explicit joins. Vary repetition,
orientation, proportions, topology and presentation independently. Split by
construction family and receiver library, not merely by rendered sample, so
near copies of a recipe cannot leak across training and evaluation.

Candidate policy actions can publish one selected pattern, withdraw a hint,
add a forbidden alternative, request an exposed read, or stop. Each action
should retain its source, comparison witness, before/after message and any
unresolved obligations. Generation policies can later propose native path or
connection edits; the same reader can assess their resulting samples.

Measure these separately before choosing any scalar training objective:

- satisfaction of required and forbidden commitments on fresh output;
- unresolved questions and unsearched work;
- admissible samples retained and diversity across constructions;
- transmitted native participation and the shared receiver-library cost;
- proposal, execution and analysis work;
- number and size of repairs, including failed or unnecessary repairs;
- preservation under held-out questions and requested modifications.

A sender can otherwise win by sending the complete construction, while a
receiver can appear successful by always selecting a memorized sample. Budget
limits, hint ablations, unfamiliar receivers and construction holdouts expose
those shortcuts. Desired freedoms also need explicit tests: satisfying more
constraints is not automatically better communication.

This is an engineering test of selective structural communication. Analogies
with mental images, biological vision or language motivate it but are not
findings about how those systems work.

## Reproduce and verify

```powershell
dotnet run --project experiments/QualitativeFrieze -- --communication-loop
dotnet test Tests/QualitativeExecution.Tests --filter "FullyQualifiedName~Communication"
node experiments/QualitativeFrieze/tools/check-communication-loop.cjs
dotnet test Gojo.slnx
```

The focused checks cover fresh output execution, public-port seam replay,
independent reader boundaries, retained alternatives, native archive message
transport, forbidden clauses, reordered equivalent patterns, incomplete search
and cycles outside the supported grammar. A two-edge multigraph cycle is not
searched by this motif reader; its absence stays open.

Browser checks inspect every receiver selection, all local references and
desktop/mobile layout. The archive, JSON diagnostics, individual SVGs,
interactive gallery and static overview remain local artifacts in this round.

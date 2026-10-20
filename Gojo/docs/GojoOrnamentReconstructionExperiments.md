# Ornament reconstruction, read focus and compact communication

This experiment asks how much of an ornament's useful meaning can survive a
small structural description when the receiver already has capable native
generators and readers. It separates matching the visible arrangement,
preserving relations between motifs, retaining a drawing recipe, and satisfying
a selected question. These can agree independently.

The result is an implemented experimental layer above the existing kernel.
Native recipes survive independent transport and execution. Small descriptions
can select useful interpretations from a finite repertoire, but neither a
small message nor agreement on the available questions establishes complete
reconstruction. The study exposes those differences rather than treating one
recognition score as the meaning of the ornament.

- [Interactive study](../../experiments/QualitativeFrieze/results/ornament-reconstruction/gallery.html)
- [All candidate panels](../../experiments/QualitativeFrieze/results/ornament-reconstruction/overview.html)
- [Compressed diagnostic table](../../experiments/QualitativeFrieze/results/ornament-reconstruction/diagnostics.json.gz)
- [Native reconstruction archive](../../experiments/QualitativeFrieze/results/ornament-reconstruction/reconstruction.gojo.json.gz)

## Corpus and evidence boundaries

There are **26 structural families and 416 executed specifications**. Each
family has four quarter-turn bases, two uncalibrated reach recipes, and one or
two translated occurrences: `26 × 4 × 2 × 2 = 416`. These are not 416 unrelated
pattern classes. One reach recipe repeats horizontal participation twice; it
changes the optional screen proportions without publishing a measured length.
The second occurrence is independent. It does not acquire a connection merely
by resembling or approaching the first.

The small repertoire includes square contours made as one path, four joined
sides, or four unjoined sides; a diamond; triangular cells; a joined cross,
declared crossing, and unspecified encounter; T, Y and H arrangements; zigzags,
square waves, staircases, returning meanders, bricks, a grid, a honeycomb
fragment, a six-ray star, lozenges and basket-like ribbons. Five larger connected
compositions add opposed meanders, a diamond mesh with subordinate flags,
alternating axial and diagonal compartments, selected grid seams forming L/T
interiors, and lozenge lobes tied by an outer contour.

The repertoire draws on the existing Bourgoin B01/B02/B03/B04/B09/B16/B18/B20/
B21/B25/B27 vocabulary and constructed contrasts. These are small experimental
fragments and elaborations, not additional claims of exact engraving fidelity.
The [Bourgoin encoding study](GojoBourgoinEncodingStudy.md) describes the source
reconstructions on which that vocabulary is based.

Every source executes a real `UnitArrangementProgram` through acknowledged
native drawing actions. Explicit public-port declarations propose joins;
formal-word equality admits or blocks them. Only fixture authoring chooses
which equal landmarks should be connected. The receiver does not infer a join
from equal screen coordinates.

Four surfaces remain separate:

| Surface | Available information | Deliberately absent |
| --- | --- | --- |
| Public observation | Fresh sites and segments, incidence, directional participation, advertised continuation and crossing relations | Position words, reach amounts, author path grouping and execution ancestry |
| Mechanical service | Fresh group identities, actual observed segments in emitted order, observed end-to-start closure | Private path identities, start words and receipts |
| Executable recipe | Native program, actual public-role join pairs, actual path/chord crossing selectors | A required fixture ID or authoring factory |
| Evaluator/audit | Source-to-output correspondences, accepted receipts, recipe provenance, withheld answers and comparisons | No automatic permission to expose this evidence through the receiver's observation port |

The word *visible* in the gallery means the advertised structural observation.
It is not a bitmap or an image parser. A diagram may look like a square while
its published endpoints remain unjoined. Declared crossing information is also
an observation family: the experiment does not scan every rendered intersection
and manufacture a crossing claim.

The renderer interprets formal words using screen distances. No screen position,
pixel, calibrated angle, `Carrier`, or `CarrierLocation` enters these public
recognition surfaces or messages. Repetition of an unmeasured reach remains
inspectable structure; pre-numeric does not mean that finite participation
cannot be counted by a diagnostic reader.

## Native descriptions and reusable queries

The main connected forms are:

```text
observation = (frame -> sites) -> (segments -> (continuations -> crossings))
mechanical group = fresh identity ->
                   (Closed(actual ordered observed segments) ->
                    Closed(observed first site -> observed last site closure))
input = observation -> Closed(mechanical groups)

recipe = actual program ->
         (Closed(public-role join pairs) ->
          Closed((actual path -> actual chord) -> (actual path -> actual chord)))

claim = actual query -> (Closed(positive query) -> Closed(negative query))
message = Closed(claims)
focus = Closed(query basis) -> (Closed(selected queries) -> Closed(complement))
context = Closed(candidate preference) -> Closed(available queries)
```

The vocabulary has **60 source-free native questions**:

| Service | Questions | What it distinguishes |
| --- | ---: | --- |
| Visible motifs | 24 | Incidence stars, short paths, and closed three-, four-, and six-edge loops with directional participation |
| Whole mechanical paths | 20 | A complete advertised drawing group, including paths longer than the local discovery bound |
| Joint occurrences | 10 | Distinct motif footprints, contact, shared edges, disjointness, multiplicity, and selected endpoint/internal-site contact |
| Crossings | 3 | Advertised encounters between axial or diagonal segments |
| Continuations | 3 | Advertised horizontal, vertical or diagonal continuation |

Templates are compiled from independently executed exemplars and renamed into
one independent pattern frame with fresh site and edge roles. Actual exemplar
bindings are kept outside the templates. Derived signatures remove duplicate
questions; native relation structure remains the source of their meaning.

A joint question binds all its roles in one certified assignment. Two separate
existence replies cannot establish that the same two motifs met each other.
Distinctness concerns actual edge footprints: reversing a traversal does not
create another occurrence. Likewise, a shared edge is one actual boundary
participating in two reads, not two coincident drawn segments.

The mechanical service supplies information the public graph cannot recover.
One turning square and four separately drawn, joined sides can have the same
public contour. They answer differently when asked whether a whole closed
mechanical path exists. Four unjoined sides also lose the public closure.

Retained assessments are cached receipts for an actual query and observation
event. They do not silently transfer answers to a fresh output. Nor does this
cache automatically carry a selected motif binding across different questions;
that stronger operation is the explicit
[retained-match mechanism](GojoRetainedMatchExperiments.md). Whole-path grouping,
joint assignment, retained assessment, and cross-question binding are different
contracts.

## Reading and regenerating a description

The receiver has three advertised capability profiles: motif-only; relational,
which adds joint occurrences and encounters; and mechanical, which admits all
60 questions. The four goals select local forms, relations, mechanical
construction, or the combined vocabulary. A focus carries its unselected
complement without asserting it. Success on one focus is permitted to leave
differences elsewhere.

Preference is explicit context. *Compact* prefers fewer output edges and groups;
*reverse* reverses the admitted selection order. *Withheld family* removes all
variants of the sender's family from the offered repertoire. This tests the
effect of a missing generator; it is not a statistical train/test split.

Messages contain actual Yes/No clauses. Missing clauses make no commitment.
Missing capabilities and unfinished reads remain Open and possible under either
answer. They are not counted as demonstrated agreement or as refutation.
Malformed or refused contracts are Blocked. A robustness claim requires a
nonempty settled admission set and that **every still-possible candidate**
settle the selected goal. A convenient selected success alone is weaker.

Selected generators execute again and publish a new observation event. One
fresh result per selected recipe is deliberately reused for later non-draining
questions. The evaluator compares both its question profile and a separate
bounded correspondence of the complete incidence/heading graph, optionally
including ordered crossing and continuation relations. Matching the 60 answers
can therefore succeed while the full graph differs.

`TryGenerate(nativeRecipe, out sample, out witness)` also executes an imported
recipe without its original `Sample`, source fixture ID or authoring factory.
It reads the exact existing program shape, verifies and rebinds the standard
motion frame, preserves path/chord/port aliases, preflights the exposed scopes,
then executes and checks the local joins. Unknown scope tails remain Open;
foreign selectors and impossible joins remain Blocked. Its witness connects
the imported recipe, frame/rebinding proofs, actual execution, admitted output,
and unresolved or blocked causes. It does not invent a new kernel primitive or
a general geometric-frame solver.

The independent import controls regenerate all **416 transported provider
recipes**, preserving **416 strict relation graphs and all 24,960 advertised
answers**. The imported generator receives the native recipe rather than a
source `Sample`; its presentation-only fixture factory deliberately throws if
called. Focused tests additionally verify that generated structure and its
proofs contain no aliases into the original pre-transport world.

## Fixed-pool description search

The first study prepares all candidate/query receipts, then uses a derived
`BigInteger` mask backend for many finite comparisons. Masks project actual
native receipts; native evaluation and subset-projection tests check the same
admissions and possible alternatives. A bit here selects a candidate. It is not
a probability or an invented truth label.

For each source and capability profile, the sender prepares a pool of up to ten
settled clauses, balancing query services and reduction per structural cost.
The baseline compares empty, visible-only, relational-only, mechanical-only,
mixed, greedy and exhaustive-search messages. Exhaustive search covers every
subset of that pool, with a configurable ceiling of twelve clauses.

The exhaustive selector is an **evaluator-side oracle**: it can inspect which
candidate profiles satisfy the withheld goal. The receiver sees only the
published clauses and its offered capabilities. A reported *minimum* is a
minimum native cost among sufficient subsets of that particular pool, goal and
repertoire. When no robust subset exists, the same row records a fallback;
`robust = false` must not be read as a sufficient minimum. It is not a global
optimum over all descriptions or an automatically learned encoding.

The complete baseline run used 416 candidate specifications and 416 fresh
sender executions. It made **104,832 judged decisions**, enumerated
**15,335,424 subsets**, and generated **222 fresh selected output events**.
Preparation took 15.6 seconds and evaluation 139.3 seconds on this local run.
These timings include a prepared finite repertoire; they are not comparisons
against general model training or a numeric Gojo execution engine.

For the combined 60-question goal and compact preference, the best finite-pool
rows give:

| Advertised receiver services | Selected goal survived | Robust description | Same full incidence | Same ordered relations | Mean clauses |
| --- | ---: | ---: | ---: | ---: | ---: |
| Motifs | 250/416 | 180/416 | 150/416 | 112/416 | 3.30 |
| Motifs and relations | 284/416 | 206/416 | 182/416 | 148/416 | 3.06 |
| All services, including mechanics | 348/416 | 278/416 | 194/416 | 168/416 | 2.74 |

The means include fallback rows. All ten pooled clauses with full capabilities
used 720.1 native participants on average; the exhaustive choice used 300.2.
The equal success totals for those two strategies show that many clauses could
be removed within this pool. The remaining failures also show that a pool
chosen for inexpensive discrimination can omit a decisive distinction.

Removing the sender's whole family reduced combined-goal selected successes to
28/416, 32/416 and 36/416 for the same three capability profiles. A compact
description is conditional on an adequate receiver repertoire. It cannot
produce a missing construction simply by naming fewer properties.

The stronger full-observation baseline recovered incidence in 416/416 selected
outputs. Including the advertised ordered relations recovered those relations
in 416/416 as well. However, only 360/416 of those selected outputs preserved the
mechanical question profile. Full public topology still does not specify how
the drawing was constructed.

The main candidate and sender matrices contained no unfinished answers at the
chosen 4,096-proposal capacities. Open behavior is exercised separately through
restricted capabilities, partial publications, invalid/stale requests and
capacity controls; the absence of Open rows in this settled corpus is not a
proof that uncertainty has disappeared.

## Active inquiry and removing redundant hints

The dialogue round removes the fixed-pool oracle from query selection. The
receiver starts with its own repertoire, focus and available query services. It
chooses a question using worst-case remaining-candidate reduction per native
query cost. Candidates with unfinished predictions stay on both branches. Only
after that decision does it ask the opaque supplier.

A request binds an actual receiver state, supplier port, query and advertised
event. Replies publish Yes, No, Open or Blocked with opaque source/revision
identities. Source observations, programs, answer matrices and certifying
witnesses remain in a separate audit surface. The policy receives no concealed
source answer vector or source family identifier.

The dialogue stops when all remaining possibilities have a known common focus
profile, no informative advertised question remains, the possible set becomes
empty, or the request budget is exhausted. Consensus means agreement at the
selected vocabulary and repertoire, not discovery of the uniquely correct
ornament. A separate judge checks whether that consensus actually matches the
sender's goal.

When settled consensus exists, a backwards pass tries withdrawing expensive
clauses. It keeps a removal only if a nonempty admission set remains and every
possible interpretation still has the same inferred focus profile. Main-run
pruning is greedy; optional exhaustive pruning is bounded to twelve clauses.
The receiver may need more questions during discovery than it needs to retain
afterward. The transcript preserves those discarded questions and the reason
their removal was acceptable.

This policy is fixed, with event-local assessment and decision caching. It is
not a trained transformer, genetic search, market, or learned aesthetic model.
It provides actual communication traces and counterexamples from which such
training could later be developed.

The complete dialogue run took **77.3 seconds**, made **14,976 inquiry
episodes**, and judged both original and pruned messages for **29,952 results**.
All 416 candidate recipes were first regenerated from the transported native
provider archive. Selected outputs used 150 fresh events, explicitly reused
for non-draining comparisons.

For the combined goal with compact preference:

| Receiver services | Selected goal survived | Robust description | Same full incidence | Same ordered relations | Mean retained clauses |
| --- | ---: | ---: | ---: | ---: | ---: |
| Motifs | 254/416 | 192/416 | 154/416 | 116/416 | 5.52 |
| Motifs and relations | 300/416 | 244/416 | 198/416 | 158/416 | 5.74 |
| All services, after pruning | 416/416 | 416/416 | 216/416 | 184/416 | 3.93 |

Each profile reached 416/416 robust, correct results for the goal it fully
advertised: local forms for the motif receiver, relations for the relational
receiver, and mechanics for the mechanical receiver. The more demanding
combined goal exposed the missing distinctions of the smaller services.

With full capabilities, the receiver asked 6.28 questions on average before
settled combined-goal consensus. Backwards pruning reduced the retained message
to 3.93 clauses, withdrawing 978 clauses across the 416 compact combined-goal
episodes. Standalone native message cost fell from 499.6 to 365.2 participants
on average. These are costs of retained descriptions, not proof that those
discarded questions were unnecessary during discovery.

The square contrasts illustrate this: two executions with the same contour
follow a common sequence of visible questions, then differ on a question about
one whole single-segment drawing group. A further joint question distinguishes
independent loop occurrences. A mechanical or relational reply can therefore
change an interpretation without requiring measured proportions or pixel
analysis.

The most important counterexample comes from the withheld-family condition.
With full capabilities and the combined goal, **all 416 dialogues reached a
known focus consensus within the offered repertoire**, but only **76 selected
outputs matched the sender's goal; 340 were wrong**, with no unresolved goal
answers. The receiver's remaining candidates agreed, yet the sender was outside
that explanation. This demonstrates a failure of the closed repertoire and
stopping policy, not a contradiction in the native read laws.

Current question selection favors splitting existing candidates. When every
candidate predicts the same answer, that question has no discrimination gain
and may never be asked. Future inquiry should also test shared predictions
against the supplier, expose an inadequate-repertoire remainder, and permit a
new construction or read. Agreement among current candidates cannot establish
that those candidates cover the source.

Both source and candidate answer vectors were eagerly assessed for the
independent critic. Sparse request counts measure **communication and
inference**, not avoided supplier computation. The reported native-read counters
cover the receiver candidate bank; they are not a total count of source reads,
fresh-output judgments, graph comparisons and execution work.

## Costs, retained evidence and limits

`Message.NativeCost` counts distinct reachable native participants, including
shared aliases, in the actual message. It is neither a byte count nor a count of
human conceptual units. Query definitions, receiver recipes, focus, prior
context, live execution state and proof histories are separate costs.

The complete 60-query codebook contains **3,853 native participants**. If these
exact query identities are already shared, a closed message adds `8 × clauses
+ 2` participants beyond them. Independent worlds still need an explicit
codebook correspondence; this arithmetic is not a proposed wire format or an
eight-bit claim representation. Compact messages exploit shared engine
knowledge rather than making its cost vanish. For the pruned full-capability,
compact-context combined-goal dialogue, the corresponding mean incremental
message is 33.4 participants, alongside that 3,853-participant shared codebook.

The union of all **416 imported executable recipes contains 446,510 native
participants**. This is another separately scoped shared cost. Prepared
assessments, request transcripts, live outputs and audit histories add their own
participation; they must not be hidden in a claim that only the final clauses
were needed.

For these small examples, the complete published observation with its relations
costs 276.2 native participants on average, whereas the standalone pruned
combined-goal message costs 365.2. The question-based description is therefore
not an automatic storage win for an individual small graph. Its distinct value
is selecting relevant commitments and retaining mechanical distinctions that
the public graph omits. Smaller incremental messages become possible under a
shared codebook and capable receiver, with those shared costs and correspondences
accounted for explicitly.

The 32-read cache probe returned identical results with and without retained
receipts: 12.4 ms for fresh reads versus 2.3 ms cached, with 16 fresh inductions
and 32 cache hits. This is a small event-local probe. It does not establish
general computation avoidance, automatic lazy discovery, or hardware-level
bit-packed execution.

The native reconstruction archive retains candidate publications, complete
provider recipes and a bounded selection of full source/message/output proofs.
Its 1,195,995 reachable participants survived transport with aliases preserved.
The full trial table is a diagnostic projection, not a native archive of every
rejected subset. Dialogue transcripts similarly retain selected actual reads
and an explicit open proof-computation scope rather than pretending to capture
every prepared matrix entry.

Current established distinctions include shared identity versus coincident
position; a cycle versus a filled face; observed contour versus whole path;
independent existence versus joint occurrence; match agreement versus complete
graph correspondence; and a successful selected output versus a robust
description. Reversal and cyclic start shifts are admitted by the whole-path
read. Encounter queries are unordered even though their original ordered rows
remain in witnesses and the stricter graph judge.

This study does not establish arbitrary recursive pattern discovery, metric
isometry, curve or fill interpretation, figure/ground segmentation, foreground
or over/under, optical recognition, automatic cross-event tracking, or learned
selection of a new generative grammar. Failed read claims do not automatically
become material carrier tension. An explicit response connection would be
needed for that use.

The next useful extensions are reusable learned substructures, selecting or
growing generators when the repertoire is inadequate, richer arrangement and
continuation questions where profiles collide, and explicit correspondence
between coarser and finer descriptions. Retained joint bindings can supply a
stable subject for those later questions. Query utility can then be trained
from the recorded successes, unnecessary hints, and counterexamples instead of
from a single global classification label.

A training record should preserve the communicated clauses, engine capability
and preference context, actual inquiry transcript, fresh generated output,
per-focus judgment, and differences outside that focus. The sender's complete
answers belong to training/evaluation evidence, not automatically to the
receiver. Future generalization tests should hold out generative recipes and
compositions, rather than distributing equivalent rotations and reach variants
across training and evaluation and calling that discovery.

## Reproducing and inspecting the study

```powershell
# Complete fixed-pool baseline: all candidates, all sources, ten-clause pools.
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-reconstruction --pool 10

# Active inquiry over the transported provider world from that baseline.
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-reconstruction --dialogue

# Revalidate native import controls and refresh the combined report.
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-reconstruction --refresh-report

# Small inspection run in a separate directory.
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-reconstruction --candidates 32 --targets 4 --pool 8 --output artifacts/reconstruction-small

dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter FullyQualifiedName~OrnamentReconstruction
node experiments/QualitativeFrieze/tools/check-ornament-reconstruction.cjs
dotnet test Gojo.slnx
```

Implementation entry points are
[cases and native generation](../../experiments/QualitativeFrieze/OrnamentReconstructionCases.cs),
[source-free vocabulary](../../experiments/QualitativeFrieze/OrnamentReconstructionVocabulary.cs),
[native reader and communication surfaces](../../experiments/QualitativeFrieze/OrnamentReconstruction.cs),
[derived projection](../../experiments/QualitativeFrieze/OrnamentReconstructionProjection.cs),
[finite search](../../experiments/QualitativeFrieze/OrnamentReconstructionSearch.cs),
[study runner](../../experiments/QualitativeFrieze/OrnamentReconstructionStudy.cs),
[active dialogue](../../experiments/QualitativeFrieze/OrnamentReconstructionDialogue.cs),
and the independent
[graph judge](../../experiments/QualitativeFrieze/OrnamentReconstructionGraphRead.cs).

At this checkpoint, `dotnet test Gojo.slnx` passed all **4,660 tests across 12
assemblies**. The independent artifact checker recomputed admissions, possible
alternatives and outcomes for all **134,784 decisions**, checked **2,496 SVGs**,
and exercised **36 desktop/mobile browser states** with no browser errors or
overflow failures. These checks validate this executable and its saved run;
they do not establish general recognition outside the experimental grammar.

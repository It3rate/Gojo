# Qualitative recognition of motifs and disposition

Experimental checkpoint, 2026-10-18. This round recognizes small recurring
fragments and larger arrangements from a deliberately restricted segment
observation. The recognizer receives incidence, coarse headings and separately
published continuation or crossing observations. It receives no generating
program, authored face list, source identity, position word or uncalibrated
reach. Several interpretations can retain the same participation, each with
its own matches, obligations and remainder.

This follows [ornament discovery](GojoOrnamentDiscovery.md) and
[ornament perturbations](GojoOrnamentPerturbations.md). Those experiments could
ask questions of supplied cells, construction history and formal positions.
Here the useful question is whether a recognizer can recover structure after
those advantages have been withheld. This checkpoint concerns recognition;
reapplying its candidates after perturbations belongs to the next round.

Open the [recognition gallery](../../experiments/QualitativeFrieze/results/qualitative-recognition/gallery.html),
or regenerate the retained study from the repository root:

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --qualitative-recognition
```

An optional `--output DIRECTORY` selects a separate output folder.

## The observation boundary

The native observation grammar is:

```text
sites = Closed(fresh site roles)
segment = fresh identity -> ((from role -> to role) -> heading scope)
continuation = shared role -> (actual first segment -> actual second segment)
crossing = actual first segment -> actual second segment
observation = (fresh frame -> sites) ->
              (segments -> (continuations -> crossings))
```

A heading contains one or two directions in the observation's own two-axis
frame. Two directions mean simultaneous participation, such as forward and up.
They establish neither a diagonal slope nor equal contributions on the two
axes. Repetition of a direction in the source word is deliberately forgotten.
The recognizer cannot recover segment length from this surface.

`QualitativeRecognitionPublisher` performs the selected lossy publication. It
reads an exposed source topology and endpoint bindings, calculates the formal
displacement of each actual passage, and publishes its justified directions in
a fresh frame. It gives sites and segments fresh identities. The source face
scope is never consumed, even when supplied by the construction.

The adapter retains two different surfaces:

| Surface | Available to recognition | Purpose |
| --- | --- | --- |
| `Observation.Surface` | Yes | Fresh incidence, coarse headings, declared continuation and crossing observations |
| Publisher maps, word reads and full `Surface` | No | External source correspondence for rendering, evaluation and provenance |

The maps attach a source site to its fresh role and a source passage to its
actual observed segment row. They are outside the observation graph: traversing
the recognition input cannot reveal their source endpoints. The observation is
not a claim that the forgotten measurements or history never existed. It is a
contract to reason without them.

An unavailable or incomparable displacement keeps its segment and an open
heading. It does not remove the passage or publish an empty, known direction.
Known prefixes of open site and passage scopes remain readable, while the open
boundary prevents exhaustive incidence claims.

## Joining and passing over remain different

Shared endpoint identity establishes a join. Two separately identified roles
do not join because their rendered positions agree. A declared crossing
connects two actual observed segments as a nonjoining observation; it does not
introduce a new shared site. A joined X therefore has a shared role, whereas
an overpass can retain four independent endpoints.

Opposite coarse diagonal headings are insufficient to establish a straight
continuation. The adapter publishes that stronger observation only when its
selected source word law can justify opposite outward rays. Single-axis rays
can be opposed without equal lengths or equal reach identities. For mixed-axis
rays, every symbolic extent profile must have the same opposite proportional
multiplicity. Two diagonal words can share the same coarse NE and SW labels
yet fail this stronger question.

These proofs remain in the external publication harness. Recognition consumes
the resulting declared observation, rather than reopening the finer source.
A foreign passage or a claimed overpass between already joined segments cannot
publish a crossing occurrence; its failed input remains visible in the adapter.
No renderer intersection test supplies topology or continuation to recognition.

## Small motifs are reusable role graphs

`QualitativeMotifInduction` proposes three structural forms:

| Form | Retained definition | What an occurrence must preserve |
| --- | --- | --- |
| Junction neighbourhood | Distinguished central role and all observed incident segments | Actual shared centre, outward headings and complete advertised incidence |
| Short simple walk | Ordered segment roles and successive endpoint roles | Actual connected traversal and heading sequence |
| Bounded simple cycle | Closed ordered walk with distinct intermediate roles | Actual cycle closure, segment occurrences and heading sequence |

The definition contains fresh role variables, endpoint relations, heading
participation and an ordered route where appropriate. Every occurrence binds
those roles to actual observation sites and actual segment rows. Family names,
signature strings and renderer labels are diagnostic projections of this
structure; they do not supply its meaning.

Walk reversal and cycle start/reversal are normalized for grouping equivalent
traversals. The selected heading frame remains fixed. Rotation, arbitrary
change of basis and deformation equivalence are separate questions, not
automatically granted by canonicalization.

A discovered cycle is not a discovered bounded face. Other edges or sites can
lie inside its drawing, and a geometric embedding has not been inferred from
headings. A three-edge cycle supports a triangular circuit interpretation;
it does not establish metric angles, equal sides or a filled triangular region.

The same segment can participate in many motifs and in larger arrangements.
Repeated-family coverage is the union of actual segment participation, rather
than a count that duplicates it for every overlapping occurrence. Uncovered
segments remain in an explicit remainder.

## Disposition describes larger flow arrangements

`QualitativeDispositionInduction` first groups observed headings with their
opposites and follows connected uses of each selected family. These strands
describe qualitative directional flow. Heading agreement alone does not turn
them into metric straight lines.

It retains several combinations of two or three direction families as separate
bases. Each basis keeps its selected segments, channel strands, local
commutation evidence, closed triangular circuits, branching alternatives,
finite boundaries, unselected segments and unsettled obligations. There is no
single noun label that makes one candidate the grid and discards the others.

A local commutation compares two observed two-step traversals from the same
actual role. When both orders end at the same actual role, their common arrival
is witnessed. Equal rendered positions of different arrival roles do not
settle that question. Available paths that end at different roles leave an
explicit unsettled obligation.

The reader also proposes cyclic two-heading grammars after observing a local
transition. Its phase cycle can describe a zigzag or an alternating transport
channel without pretending its two headings form one straight direction.
Such a channel can coexist with a second family in another disposition
candidate. This is bounded phase-two recognition, not arbitrary periodic
grammar induction.

This permits a coarse grid-like read of a richer triangular arrangement. A
two-family candidate can preserve its useful strands while leaving another
direction family unselected. Triangular circuits that partly use that omitted
family remain explicit extra closure evidence. A wider basis can account for
those circuits without making the narrower read historically false.

An unexpected triangle is unexpected relative to the chosen basis. It does not
mean that the source is defective. Nor does it prove that the larger basis is
the most useful explanation: a task concerned with strips or transport may
prefer a narrower read. Candidate selection requires a declared question and
comparison law.

Selected support means admission by the candidate's heading families. The
retained strands, phase routes, branches and commutations qualify what those
admitted segments actually establish. Selecting every segment is therefore
insufficient to prove a regular lattice or one successful periodic flow.

## Connecting motifs with disposition

`QualitativeRecognitionReadings` connects every retained basis candidate to
the actual local motif occurrences. It classifies an occurrence as contained
within the selected segment support, straddling its boundary, or outside it.
Each classification retains the actual inside and outside segment partition,
along with the family and occurrence reads. Shared motif participation is
united once when calculating coverage, and selected segments left uncovered by
contained motifs remain explicit.

Containment here is membership in a selected segment set. It is not geometric
containment inside a rendered polygon. These are different questions even when
the screen makes both interpretations seem natural.

The helper also retains support-extension correspondences. A candidate can
extend another's selected support only through an actual strict superset, with
no unsettled commutations or competing continuations in the richer candidate.
The witness keeps their common segments, introduced segments, shared basis
roles and differing basis roles. This relates two interpretations without
claiming that their whole grammars are equivalent, that one logically entails
the other, or that the larger one is automatically preferable.

Standalone phase rhythms are displayed alongside basis candidates, but this
round's native motif partitions and support extensions are supplied only for
the basis candidates. An unassessed relationship is not displayed as a proven
zero relationship.

## Results on ten published fields

The retained gallery uses hand-authored finite fields as an authoring and
evaluation harness. The inducer receives their fresh observation, not these
fixture names or geometric recipes. The following counts come from
[the retained diagnostic reads](../../experiments/QualitativeFrieze/results/qualitative-recognition/diagnostics.json).

| Field | Segments | Local families | Retained local occurrences | Main distinction |
| --- | ---: | ---: | ---: | --- |
| R01 square grid | 31 | 36 | 282 | Two channel families retain all segments and 12 commuting circuits |
| R02 sheared grid | 31 | 36 | 282 | The same incidence supports a different heading basis |
| R03 diamond lattice | 31 | 36 | 282 | Two diagonal families retain the grid incidence without metric diamond promises |
| R04 triangular lattice | 43 | 119 | 886 | Two-family partial grids coexist with a three-family reading and 24 triangular circuits |
| R05 alternating offset rows | 40 | 64 | 387 | A phase-two transport read accounts for alternation that a single family leaves outside its basis |
| R06 zigzag strips | 36 | 8 | 102 | Alternating routes are recovered without supplying or discovering any cycles |
| R07 hexagonal mesh | 38 | 27 | 204 | Nine six-edge circuits coexist with three heading families and no triangular circuits |
| R08 grid and isolated diamonds | 79 | 49 | 438 | Grid cycles and isolated diamond cycles remain separately discoverable |
| R09 two local organizations | 57 | 153 | 827 | Distinct connected components support shared fragments and competing partial dispositions |
| R10 larger triangular field | 289 | 83 | 500 | Budget cuts preserve a useful prefix while explicitly withholding an exhaustive motif result |

R01 through R03 each contain 12 four-edge circuits, alongside 17 larger
six-edge perimeter circuits. Their equal occurrence counts do not imply that
their fixed-frame motif definitions are identical, nor that metric squares,
sheared parallelograms and diamonds have become indistinguishable to every
possible observation.

R04 gives the useful partial-understanding case. One two-family basis admits
31 of 43 segments and retains 12 commuting circuits. Its 12 unselected
segments participate in 24 cross-basis triangular circuits. The three-family
basis admits all 43 segments, retains 29 commuting circuits and contains those
24 triangles. The narrower candidate remains an inspectable useful read with
a specific remainder; it is not discarded for failing to account for the
whole source.

In R05, a horizontal-plus-single-diagonal basis admits 30 of 40 segments and
leaves 10 outside it. The two alternating vertical-transport headings support
a standalone phase rhythm over 20 actual segments. Combining that phase
channel with the horizontal family admits all 40 and retains 16 commuting
circuits, with no branching or endpoint mismatches in that read. A separate
three-family basis also remains available. Phase-two support is established
here without solving general periodic-layout recognition.

R06 supplies the complementary check: its two-heading rhythm supports all 36
segments, while local induction returns no cycles. R07 has nine six-edge
cycles and no triangular circuits. Its three-family admission has zero
commuting circuits, so merely counting heading families cannot establish a
triangular lattice. Local motif evidence changes what a coarse disposition
can sensibly mean.

R08 exposes the observation's spatial limit. It recovers one family of 12
four-edge grid circuits and another family of 12 isolated diamond circuits.
The recognizer does not know that the small diamonds sit inside the grid's
rendered chambers. That containment requires an additional embedding or
relative-placement observation. No such fact is inferred from their shared
presentation.

R09 similarly preserves the disconnected components and their recurring
fragments, but their left-right placement is withheld. The current candidate
vocabulary combines at most three heading families, whereas this field uses
four. Several partial arrangements therefore coexist; the presentation's
placement of the components is not a recovered global layout recipe.

R10 reaches both configured limits: 5,000 motif expansions and 500 retained
occurrences. It records 2,651 unsearched cuts and no open input observations.
The retained prefix contains no cycles, despite the source's many actual
triangles; walks are visited before cycles. That absence is a search limit,
not negative evidence against triangles. The independent disposition read
still retains 180 triangular circuits and 251 commuting circuits in its
three-family basis. One unfinished reader need not erase another reader's
already supported result.

## Remainder and uncertainty stay local

The native reads distinguish:

| Evidence | Meaning |
| --- | --- |
| Accepted occurrence | Actual participants satisfy the retained role graph or route |
| Unselected participation | Known observed segments outside this candidate's basis or recurring coverage |
| Competing continuation | More than one actual continuation is available |
| Unsettled commutation | Available alternative orders end at different actual roles |
| Open heading | Direction cannot be settled from the published observation |
| Finite boundary obligation | A needed continuation is unavailable in the advertised field |
| Unsearched participation | A declared search budget ended before further proposals were expanded |

A finite patch cannot establish that a missing outside continuation is a
defect. Open observations cannot establish absence. A budget cut is not a
negative match result. These distinctions matter before surprise detection:
otherwise an image boundary, lost measurement and a broken regularity could
all be mislabeled as the same anomaly.

The user's useful intuition of residual tension is represented here by read
remainder and unsettled obligations. No physical participation is transferred
and no carrier tension is introduced. A later adaptive mechanism can connect
those observations to a response route; the read itself supplies no such route.

## Bounded search and the learning boundary

The motif reader exposes proposal slots, retention slots and selected maximum
walk and cycle sizes as native scopes. Actual expansions and retained proposals
consume witnessed slots; unfinished expansion surfaces remain available.
Disposition likewise retains a budget, spent proposal witnesses and unsearched
alternatives. Its slots bound proposed bases and phase grammars, rather than
the initial graph read or fixed local triangle enumeration. Finite limits
constrain the selected search, rather than silently asserting that no other
interpretation exists.

A complete search means its supplied scope and configured proposal family have
been examined. It does not mean that every candidate explains the whole field,
that larger motifs do not exist or that a best interpretation has been found.
The motif reader currently visits junctions, then walks, then cycles. A small
budget can therefore bias the retained prefix; its saved frontier identifies
unfinished participation without promising exact resumable search state.

This checkpoint induces role graphs from observations using supplied search
laws. It does not train neural weights, evolve a genetic population or learn a
universal prior over pattern usefulness. A small recurring fragment can become
a candidate reusable description, but neither recurrence nor coverage alone
establishes that it predicts unseen structure or compresses a broader corpus.

The current search proposes channel questions from observed headings and
transitions. Its remainder explains why a candidate is partial, and native
support-extension witnesses connect it to alternatives that account for more
participation. That remainder does not yet schedule the next question or learn
a repair rule. Presentation order favours commuting evidence and smaller
remainders for browsing; every candidate remains available, with no inferred
global winner.

The next useful experiment can preserve these candidates, perturb an actual
observed passage or heading, and reapply local obligations. Recognition can
then be evaluated separately from discrepancy localization. Subsequent grammar
learning can compare held-out prediction, retained coverage, competing
continuations, definition participation and exceptions without erasing the
underlying evidence.

## Implementation boundary

The mechanisms live in the qualitative experiment above the kernel:

- `experiments/QualitativeFrieze/QualitativeRecognitionObservation.cs`
- `experiments/QualitativeFrieze/QualitativeMotifInduction.cs`
- `experiments/QualitativeFrieze/QualitativeDispositionInduction.cs`
- `experiments/QualitativeFrieze/QualitativeRecognitionReadings.cs`

The source adapter may inspect finer formal source words to make its declared
publication. The recognizers inspect only that publication. The renderer can
use the external map to present a candidate on the original field, while colour,
stroke width and screen coordinates remain presentation choices.

No domain-specific kernel primitive, calibrated numeric carrier or image
segmentation backend is required for these reads. Exact lengths, arbitrary
rotation equivalence, inferred faces, metric planarity, automatic surprise
selection and general learned concept discovery remain separate capabilities.

## Verification

- 64 new focused tests pass, including absence of source ancestry in both the
  published observations and their derived motif, disposition and partition
  witnesses. Negative cases retain unknown headings, malformed identities,
  nonjoining crossings, branches and unsearched participation.
- `dotnet test Gojo.slnx` passes all 3,774 tests, including 1,344 qualitative
  execution tests. The final focused run also checks the additional derived
  witness blindness assertion and the completed display integration.
- The Release command regenerates all ten fields, their native diagnostic
  projections, SVG field diagrams and interactive gallery.
- Headless Edge checks cover motif and occurrence selection, alternate
  dispositions, commuting-circuit overlays, explicit search cuts, local links,
  standalone SVG parsing, and desktop/mobile layout. No script or SVG parsing
  errors, broken links or horizontal viewport overflow were observed.

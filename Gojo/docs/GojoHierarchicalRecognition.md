# Hierarchical qualitative recognition and requested refinement

Experimental checkpoint, 2026-10-18. A local motif reading can now become a
participant in a larger pattern, and an unresolved larger reading can request
additional observations through two successive exposed boundaries. The same
mechanism preserves contrary answers, competing alternatives and unavailable
capacity. These are structural observation and interpretation mechanisms above
the kernel, rather than new material settlement primitives.

Open the [interactive gallery](../../experiments/QualitativeFrieze/results/hierarchical-recognition/gallery.html).
This round extends [recognition evidence and placement](GojoQualitativeRecognitionFirmness.md).

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --hierarchical-recognition
```

An optional `--output DIRECTORY` selects another artifact directory. The
renderer receives display positions from the fixture. The grammar and query
mechanisms receive native participants, ordered connections, published symbol
alternatives and actual obligations.

## The question this round tests

Can a larger description remain a useful local interpretation without seeing
the internals of each component, while still requesting the observations needed
to settle its particular unresolved claims?

The demonstration has four connected levels:

1. Segments, actual joins and qualitative headings support local motif reads.
2. Ordinal placement and explicit passage envelopes locate those motifs in
   rectangular chambers. Shared boundaries justify chamber adjacency.
3. A finite phase grammar reads motif sequences along actual chamber paths.
4. A row becomes a fresh participant labelled by its reusable phase pattern
   and starting phase. Another grammar reads the sequence of these row labels.

The lower motif, a row's disposition and the arrangement of rows remain
different structures. Their connections carry selected observations, rather
than giving every reader access to every lower-level instance.

For a checkerboard, this allows one question to read alternating diamond and
square motifs along a row, and another to read alternating row alignments.
Neither concept is represented by the word “checkerboard.” The reusable
definition is the actual native phase and successor structure, with its symbol
definitions and occurrence correspondences.

## Seven controlled examples

The dimensions below describe stimulus authoring. They are not numerical
coordinates supplied to the recognizers. Training and held-out rows are chosen
before either phase grammar is induced. The missing local readings are also
selected by the experiment harness in advance.

| Case | Stimulus and question | Training / held-out rows | Missing local readings / new query capacity | Intended diagnostic | Observed result |
| --- | --- | --- | --- | --- | --- |
| H01 | 8 by 8 alternating diamonds and squares, with alternating row alignment | 4 / 4 | 3 / 3 | A row question requests missing cells, then supports the upper alternating arrangement | Three new cell queries; one upper phase reading supported, none open |
| H02 | 12 by 12 diamond, square and triangle sequences, with three recurring row alignments | 6 / 6 | 6 / 6 | Three-phase structure should remain readable at both levels | Six new cell queries; three-phase grammars at both levels; one upper phase reading supported |
| H03 | 12 by 8 field using diamond, square, triangle and eight-spoke junction prototypes | 4 / 4 | 4 / 4 | Four lower phases should support two recurring row alignments | Four new cell queries; lower period four and upper period two; one upper reading supported |
| H04 | Checkerboard with a hidden triangle where the learned sequence expects a square | 4 / 4 | 3 / 3 | Accept the actual local answer and retain a counterexample against the larger claim | Three new cell queries; both upper starting-phase readings refuted, none left open |
| H05 | The same four-phase stimulus as H03, with insufficient local query capacity | 4 / 4 | 4 / 1 | One observation may arrive; unpaid questions must remain blocked and unresolved | One new cell query; three lower requests blocked; one upper reading remains open |
| H06 | A checkerboard chamber contains both a square and a triangle under a one-unit question | 4 / 4 | 1 / 1 | Preserve competing local alternatives instead of choosing a convenient answer | One new cell query; the competing alternatives leave the same upper reading open |
| H07 | The same fine field as H06, read through a four-edge-circuit question | 4 / 4 | 1 / 1 | A narrower question can support another valid reading while the extra triangle remains in the fine source | One new cell query; the narrower question supports the upper alternating reading |

H04 does not ask the system to invent a repaired program from the contrary
held-out example. H05 changes capacity rather than the stimulus. H06 and H07
change the question applied to the same fine structure. They make the role of
context explicit without claiming that one reading is the universal definition
of the ornament.

## What is learned, and what is already supplied

`QualitativePatternGrammar` performs bounded structural induction over selected
training paths. It enumerates periods from one to four, asks whether two actual
traversals can participate in the same phase cycle, and retains the resulting
native candidates. Separate seed and retention slots bound this search.
Unsearched alternatives remain explicit.

```text
token node = actual occurrence identity -> Open/Closed(possible definitions)
token link = actual proof identity -> (actual from node -> actual to node)

phase pattern = Closed(phase identity -> expected definition) ->
                Closed(phase identity -> successor identity)

training witness = actual selected path and window ->
                   actual link proofs and paired phase occurrences
```

Every retained pattern is frozen before held-out evaluation. Each possible
starting phase remains a separate read. The evaluator rejects overlap between
selected training node identities and selected held-out node identities.
Re-evaluation after refinement uses the same held-out identities and frozen
patterns.

This is a small symbolic learning algorithm. It does not train a neural model,
use a pretrained transformer as the recognizer, or perform genetic or market
selection. Its primitive vocabulary and allowed phase search are already
available when sequence induction starts.

In particular, the experiment induces the local motif vocabulary and chamber
layout from the complete fine field before partitioning the sequence-training
rows. The held-out claim therefore concerns sequence and row-program
generalization within that vocabulary. It is not an end-to-end test of learning
unseen primitive shapes without prior exposure. Native identity separation also
does not establish statistical or physical independence of observations.

## Alternatives are part of the token observation

A token's symbol scope describes alternative possible definitions under a
chosen question. It is not a list of simultaneous asserted properties.

| Published alternatives | Read against expected A |
| --- | --- |
| Closed(A) | Supported |
| Closed(A, B) | Unresolved between alternatives |
| Closed(B), or a known closed empty answer | Refuted |
| Open scope, including an open scope with known candidates | Unresolved |

For example, H06 contains two actual motifs. The current question asks which
single unit should occupy a chamber role, so those valid local findings become
competing alternatives for that role. A different question could represent
both participating motifs together; that composition is not silently inferred
by this single-role reader.

An open prefix is not a conjunction. Seeing B among an open list of possible
definitions does not establish that A is impossible. Conversely, a closed
negative result is useful evidence and must not be mistaken for a service
that could not answer.

## Lifting a row through a narrow boundary

`QualitativePatternLadder` gives each actual lower path a fresh row participant
and an exposed port. It derives an upper adjacency only when actual supplied
cell links connect every ordered member of one row to the corresponding member
of another. Coincident display locations cannot provide those links.

```text
private binding = actual lower path -> fresh upper row
private adjacency witness = actual upper link ->
                            actual paired cells and lower link proofs

public row definition = frozen phase pattern -> actual starting phase identity
public row port = fresh upper row -> fresh port
```

The public row definition retains the phase pattern and alignment needed by
the next level. It does not retain the actual lower cells, their positions,
the row's full match history or the fine segment observation. The supplier's
separate audit surface preserves those correspondences.

The current public row question advertises the framed prototype library and
answer role. The detailed two-traversal coverage and description-preference
witnesses stay on the supplier's audit surface. Its reply is therefore a
supplier assertion under that interface, rather than an independently replayable
proof available to the upper receiver. The upper reader does not inspect the
private dominance calculation or assume a universal minimum-description law.

Before publishing a closed row answer, the reader requires supported local
obligations and at least two actual occurrences of every phase in each retained
preferred interpretation. Merely matching a short prefix cannot establish a
complete repetition under this question.

Some learned programs describe exactly the same expected sequence on the same
observed path, such as a two-phase cycle and a doubled four-phase spelling of
it. A local description-cost comparison can prefer the smaller one. The native
dominance witness retains the identical node bindings, expected participation,
obligation states and actual path proofs, together with paired description
participants and the excess description. Other candidates remain on the audit
surface. This preference is scoped to the selected evidence and question; it
does not establish a unique universal grammar or prove future extrapolation.

## A real downward request and upward answer

`QualitativeEvidenceRefinement` receives a higher evaluation, actual node-to-port
routes and finite local services. It does not create questions from renderer
positions or copy an expected symbol into an answer.

```text
cause bridge = actual upper request -> actual lower evaluation
request = (actual evaluation -> (actual unresolved obligation -> expected question))
          -> ((actual node -> actual port) ->
              (actual route -> advertised service publication))

allocation = actual service capacity slot -> actual request
reply = (actual request -> actual port) ->
        (actual node publication -> (actual revision -> actual evidence))

correspondence = actual prior node row -> actual accepted publication row
```

Only actual members of the evaluation's demand scope authorize requests. A
copied result-shaped obligation does not. Known supported and refuted local
reads do not authorize a query merely to obtain a more convenient answer.

In this study, an unresolved upper row observation calls the row service. That
service evaluates the corresponding lower cell path and passes the actual
upper request as the cause of a lower refinement round. Actual unresolved cell
obligations can then call the chamber service. The revised lower observation
is evaluated again, and the row service publishes its actual resulting framed
definition or remaining alternatives.

```text
upper unresolved row claim
  -> exposed row port and local capacity
  -> lower row evaluation
  -> actual unresolved cell obligation
  -> exposed chamber port and shared cell-query capacity
  -> actual local motif answer
  -> revised cell observation and row evaluation
  -> coarse row publication
  -> revised upper observation and evaluation
```

The upper reply carries the row service's assertion under its exposed contract.
The full nested lower trace stays on a separate audit surface. The experiment
can inspect that trace without giving the upper recognizer direct access to
the lower world.

## Capacity, refusals, reuse and history

Each service has its own finite scope of capacity slots, shared by its exposed
ports. An attempted supplier read consumes one actual slot. Refusal, malformed
answers and stale replies still represent work attempted; they do not silently
restore that slot. A blocked request performs no supplier read.

| Outcome | Native distinction and effect |
| --- | --- |
| Accepted | A publication addresses the actual requested node and port under the advertised revision; it can contradict the expectation |
| Reused | Another request refers to the same actual cached publication under the same service revision; no new slot or independent observation is created |
| Blocked | No unique exposed route, no unique supplier or no remaining legal service capacity; the question remains open |
| Rejected | Explicit refusal, malformed or misaddressed publication, or an answer that cannot refine an already closed finite alternative scope |
| Stale | The actual reply revision differs from the advertised requested revision; it does not update the observation |

A closed empty symbol scope is an accepted negative classification. A closed
empty publication scope is a refusal. An accepted open or still-ambiguous answer
can contribute observations while leaving the requesting obligation unresolved.

Replies replace the corresponding published token row while preserving its
identity and explicit old-to-new correspondence. Unchanged rows and existing
link proofs remain available. An open inventory is not silently closed by
refinement. The prior evaluation and observation remain retained.

Repeated questions about one node can arise from different phase candidates.
The service cache preserves one actual publication for that observational
revision. Reuse does not multiply node occurrences, evidence or capacity.
A genuinely changed supplier observation or finer service contract needs an
explicit new revision or service publication. The wrapper checks the revision
asserted by its adapter; it does not inspect private supplier state to discover
changes automatically. A cached open answer is not automatically retried when
more lower-level knowledge or additional capacity becomes available elsewhere;
that change needs its own revision and dependency policy.

History is an ordered qualitative timeline. Each event connects its before
knowledge state, request/route/allocation/reply witness, after knowledge state
and remaining obligations. Actual next-step links preserve order. The final
fold connects the collected observations to the revised token graph. This
timeline introduces no measured clock, duration or numeric scheduler.

The current round reads a finite batch of obligations from an evaluation
snapshot. It does not re-plan every remaining query after each reply. Early
stopping and utility-directed query allocation remain further experiments.

## Observation boundaries and implementation work

The fixture retains the authoring recipe, source motion words, coordinates,
full fine observation and ordinal proofs. Those are available for rendering
and audits, but they are absent from the exposed cell and row interfaces.
Shared prototype definitions are intentionally visible: they provide the native
meaning that a higher reader can reuse.

The ordinal supplier has a faster path for source words whose selected-axis
participation uses one actual shared reach. Host counts propose equality classes
and order; the native word reader verifies every equality class and adjacent
strict-order link before publication. The retained proof chain supports the
resulting order. Different or unresolved reaches retain the more general read.
This is an optimization of the supplier, not a newly inferred metric supplied
to the grammar.

Chamber containment continues to depend on two ordinal projection observations,
a validated rectangle circuit and explicit whole-passage endpoint-box envelopes.
Those envelopes are supplier premises, not consequences of seeing endpoints.
Known local counterexamples can reject a containment question without evaluating
every positive obligation. Positive containment still requires its complete
local evidence.

All remainders in this experiment are observational residue, competing
interpretations, blocked requests or open obligations. They are not automatically
material `Carrier.Tension`. No external physical value change is introduced by
asking a question or reading a new observation. A future response that changes
the source would require an explicit material or structural action route.

## Measured outcomes and verification

The generated [diagnostics](../../experiments/QualitativeFrieze/results/hierarchical-recognition/diagnostics.json)
record the following outcomes. S/O/R counts supported, open and refuted
**candidate/path/starting-phase reads**. They are not counts of independent
physical samples or defective rows. Refuted alternate alignments are normal
when another alignment is supported.

| Case | Lower periods / upper periods | New cell queries | Reused publications | Upper S/O/R before | Upper S/O/R after | Lower S/O/R after |
| --- | --- | ---: | ---: | --- | --- | --- |
| H01 | 2 and 4 / 2 | 3 | 6 | 0/1/1 | 1/0/1 | 12/0/12 |
| H02 | 3 / 3 | 6 | 12 | 0/3/0 | 1/0/2 | 6/0/12 |
| H03 | 4 / 2 | 4 | 4 | 0/2/0 | 1/0/1 | 4/0/12 |
| H04 | 2 and 4 / 2 | 3 | 6 | 0/1/1 | 0/0/2 | 9/0/15 |
| H05 | 4 / 2 | 1 | 4 | 0/2/0 | 0/1/1 | 1/3/12 |
| H06 | 2 and 4 / 2 | 1 | 2 | 0/1/1 | 0/1/1 | 9/3/12 |
| H07 | 2 and 4 / 2 | 1 | 2 | 0/1/1 | 1/0/1 | 12/0/12 |

Reuse in this table includes both query levels. In H02, for example, eighteen
upper requests need only six actual row-service calls: twelve requests reuse
the corresponding publications. Those six row reads cause six actual new
cell observations. In H01, duplicated lower two- and four-phase descriptions
produce several questions about the same missing cells; the six reuses add no
new cell observations.

H04 is a useful negative result rather than an unsuccessful query. Its actual
triangle answer is accepted, and the formerly open alternating claim becomes
refuted. H05 separates this from lack of evidence: three lower requests are
blocked by capacity and their dependent upper interpretation remains open.
H06 shows that spending a slot does not necessarily remove uncertainty. H07
then changes the role question, rather than deleting the competing triangle
from the fine field.

The retained Release gallery-generation run took **10.602 seconds** for all seven
sessions. This is an illustrative end-to-end run, not a controlled performance
benchmark or a comparison with another execution layer.

Separate debug diagnostics for local containment recorded:

| Stimulus | Construction diagnostic (s) | All-cell query diagnostic (s) | Full containment reads | Early native exclusions |
| --- | ---: | ---: | ---: | ---: |
| 8 by 8 | 0.217 | 0.284 | 64 | 8,128 |
| 12 by 12 | 0.964 | 1.057 | 144 | 41,328 |

These diagnostic readings illustrate where work is avoided. Most candidate
motifs can be excluded from a chamber by a small actual endpoint-order
counterexample. The positive local cases still require their full containment
proofs. The counts do not claim that an unchecked heuristic replaced those
proofs, and the debug times are not an isolated optimization benchmark.

Focused verification passed **81 new tests**: fourteen arrangement cases, six
fixture cases, three ordinal-supplier cases, fifteen evidence-refinement cases,
twenty-one grammar cases, twelve ladder cases and ten integration cases.
The two focused runs overlap on grammar tests; their raw totals must not be
added as independent cases.

Desktop and mobile browser checks passed for the gallery controls, with zero
horizontal overflow, script errors or broken local links. The initial and
question stages show their own observations and demands; only the final stage
reports completed refinement requests and replies. The retained
[preview](../../experiments/QualitativeFrieze/results/hierarchical-recognition/hierarchy-preview.png)
was visually inspected alongside desktop and mobile views.

Solution-wide verification passed: **3,909 tests**, zero failures or skips,
including **1,479** qualitative execution tests. The command was
`dotnet test Gojo.slnx --no-restore`, using the already restored solution.

## What remains open

This establishes a bounded composition experiment, not a general full-plane
ornament recognizer. In particular:

- Rectangle chambers, the chamber focus, the primitive alphabet and selected
  traversal paths are part of the experimental setup. The system does not
  discover every useful decomposition or question automatically.
- The sequence search is limited to periods one through four and explicit
  finite capacities. Longer periods, nested local grammars, arbitrary branching
  layouts and alternative traversal choices need additional experiments.
- Row ordering in this demonstration requires one unbranched native row chain.
  The code preserves general branching evidence, but the session harness refuses
  to choose one convenient chain through a branching arrangement.
- A lifted row label is a deliberate read of the lower structure. Other motif,
  flow, field or overlapping interpretations can remain valid without being
  represented by that particular token.
- Exact symbol identity is the present correspondence law. Independently built
  isomorphic prototypes, transformed viewpoints and transfer to another domain
  need explicit structural matching or correspondence rather than text labels.
- Closed results are relative to the advertised finite prototype library and
  selected question. They do not establish that no other pattern exists.
- Query allocation follows actual open obligations and finite service capacity,
  but does not yet rank questions by information gain, description improvement,
  utility or learned cost.
- Recognition does not repair a contrary motif, learn a new program from the
  held-out answer, or identify the physical cause of a discrepancy. It preserves
  the particular failed obligations so a later process can act on them.
- No statistical confidence, aesthetic optimum, independent-sample guarantee or
  universal wallpaper symmetry classification follows from these results.

The next useful extension is to compare several native decompositions and
traversals on the same field, preserve their different residues, and evaluate
which requested observations improve held-out explanations. More elaborate
generation and learning can then reuse these interfaces rather than bypassing
their uncertainty and provenance.

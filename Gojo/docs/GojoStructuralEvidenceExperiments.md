# Structural questions beyond span changes

Status: implemented exploratory extension of the evidence-selection fixture.
No kernel primitives change. The [interactive gallery](../../experiments/QualitativeFrieze/results/structural-evidence/gallery.html)
shows six cases, all alternatives and the successive imported replies.

## The question this experiment asks

The earlier rectangle diagrams isolated two different mechanics. Provisional
equivalence tested whether a shared-span default could be withdrawn while a
coarser closed-contour statement remained valid. Learned acquisition tested
which exposed span publication would distinguish a finite candidate library.
Neither experiment trained a visual rectangle recognizer or changed the
physical source from a square into a rectangle.

This round asks whether the acquisition policy works when the uncertainty
concerns topology and headings instead of span participation. Can it ask about
closure, an actual shared site or a connected sequence, accept a scoped reply,
and retain the remaining alternatives? A second question is whether it knows
when its advertised evidence cannot distinguish the generating recipes.

The receiver is given executable candidate constructions and exposed question
ports. It does not invent the candidates. It receives neither the complete
source graph nor an SVG/bitmap. Descriptive captions suggesting a human coarse
reading are presentation; they are not extra native evidence. The gallery's
initial state therefore shows admitted alternatives, not an already inferred
shape. Source images are collapsed for inspection after the run.

## Six structural cases

| ID | Alternatives and selected source | Distinguishing evidence |
| --- | --- | --- |
| S01 | Closed triangle, open chevron, open three-edge turn, square | Actual closed three-edge cycle |
| S02 | Shared four-arm cross, T, two nonjoining crossing lines | Shared-site incidence; negative replies reject the joined alternatives |
| S03 | T, joined cross, diagonal Y | Three-arm heading participation |
| S04 | Connected zigzag, separate alternating strokes, square wave | Actual connected three-edge diagonal walk |
| S05 | Diagonal zigzag, separate strokes, axial square wave | Horizontal/vertical participation separated into consecutive edges |
| S06 | One closed turning path, four independently drawn sides joined through ports | Both expose the same contour; recipe remains ambiguous |

Each source executes freshly and separately from its candidate examples through
`UnitArrangementNode`. Intentional joins are admitted through actual exposed
ports. Coordinate equality never welds sites. A fresh
`QualitativeRecognitionPublisher` strips position words, span detail and
construction ancestry from the observation used for matching.

The crossing source has an explicit crossing declaration. The current inquiry
distinguishes it by rejecting shared incidence among the supplied alternatives.
It does not positively discover the crossing's location, infer foreground or
choose an over/under ordering. Likewise, short walks reveal local continuity and
turn sequence; they do not establish an infinite translation period or exact
repetition cadence.

S06 deliberately contains a square. Its role is different from the old span
tests: it demonstrates that identical exposed results can preserve different
mechanical constructions without making either one false. Asking about output
topology cannot recover unadvertised construction history.

## Native inquiry and disclosure boundary

The implementation is
[StructuralEvidenceInquiry.cs](../../experiments/QualitativeFrieze/StructuralEvidenceInquiry.cs):

```text
query = source-free native pattern -> request seam
candidate = public output observation -> Closed(available output)
request = actual receiver-before -> actual query port
reply = request -> ((source -> observation event) ->
        (Closed(supported query) -> (Closed(refuted query) ->
        (Closed(open query) -> scoped proof))))
assessment = candidate -> (support -> (refutation -> (open -> blocked)))
selection = public hypotheses -> (published replies ->
            (candidate assessments -> (eligible -> conflicts)))
step = receiver-before -> (public feature read ->
       (authorized reply -> receiver-after))
```

The query pattern is an actual role graph, including incidence, coarse heading
participation and a selected star, walk or cycle structure. A name such as
"triangle" is not the source of meaning. Ordinary `CommunicationAssessment`
matches that structure against a history-blind native observation.

A positive answer needs an actual certified occurrence. The public reply
contains one selected occurrence, not all occurrences or the supplier's complete
inventory. A negative answer requires completed search within the relevant
bounded grammar. An exhausted search or a valid pattern outside that grammar
returns open. Public replies exclude the full source observation; complete
proofs remain on a separate supplier audit surface.

The receiver admits only replies to actual pending requests at its own query
ports. Copied ports, cross-receiver replies and replay do not import evidence.
These boundaries do not make a supplier infallible: two suppliers can publish
opposing claims for one actual observation event, leaving a connected conflict
and blocked interpretation.

All operations here are non-draining observations. Refutation and open reads do
not automatically introduce carrier tension or mutate the source drawing.

## Unchanged policy, wider content

[StructuralEvidenceStudy.cs](../../experiments/QualitativeFrieze/StructuralEvidenceStudy.cs)
reproduces the previous 48 span-training episodes with the same seed and
request budget. The resulting five coefficients exactly match the previous
recorded run. No structural examples are used to fit or tune them.

The five feature slots preserve their earlier scales: intercept, fraction of
compatible candidate pairs distinguished, overlap with supplied query context,
payload size divided by two, and known payload fraction. Here one query pattern
is one advertised payload item. Native feature rows explicitly correspond to
the earlier policy basis. The structural patterns themselves do not become
numeric span codes.

Query context is supplied: the example identifies the questions relevant to its
selected purpose. This tests reuse of an acquisition preference, not autonomous
discovery of which concepts matter. Each strategy also receives a valid broader
question first in S01–S05. Its predictions agree across that case's candidates,
so asking it cannot distinguish those alternatives. The fixed order baseline
therefore has an explicitly easy inefficiency; the disagreement rule is the
stronger comparison.

The recorded 24 normal episodes give:

| Strategy | Uniquely resolved / 6 | Open / 6 | Total requests | Mean requests |
| --- | ---: | ---: | ---: | ---: |
| Fixed question order | 5 | 1 | 13 | 2.167 |
| Seeded random | 5 | 1 | 12 | 2.000 |
| Candidate disagreement | 5 | 1 | 8 | 1.333 |
| Transferred span-trained policy | 5 | 1 | 8 | 1.333 |

The one open normal case is S06, where retaining both recipes is the intended
result. The transferred policy again matches the simple disagreement rule;
there is no demonstrated learning advantage over that rule. Six authored cases
are useful mechanism checks, not a statistical benchmark for arbitrary shapes.

Three further controls preserve different unsettled states: zero source search
budget returns open replies, opposing reports of the same event retain a
literal conflict, and absent receiver capabilities block all candidate outputs.

## Evaluation and reading the diagrams

After acquisition, a separate evaluator compares the source's and each
candidate's bounded existential motif vocabularies in both directions. This
uses the same native matching law on additional source/candidate patterns;
"independent" means isolated from policy inputs, not a second unrelated
recognizer. A unique candidate with an unresolved acquired prediction is not
certified as resolved.

Vocabulary agreement does not prove graph isomorphism, motif multiplicity,
global disposition, exact geometry or ancestry. All full-source comparisons
and audit surfaces stay outside the receiver and its feature reads.

The gallery makes the interpretation process visible:

- Stage zero shows all admitted constructions. Later buttons show a question,
  a supplier reply and which candidates remain compatible.
- Stage zero is the actual baseline before any source reply in this run. It is
  not a future placeholder. The receiver already knows its candidate library
  and exposed question ports, but has not yet asked the source anything.
- Each card states its own predicted answer separately from whether it remains
  possible. Source **No** and candidate **No** agree; that candidate is not
  ruled out. Neutral cards remain possible, red cards are ruled out, and green
  indicates a sole remaining candidate rather than an affirmative answer.
- "Cycle" means a closed loop returning to its starting site. The question
  banners use "closed loop with exactly three/four edges" to distinguish this
  from an open path containing the same number of edges.
- Solid copper dots mark actual shared sites. Hollow endpoint circles mark
  free endpoints. Paired hollow purple markers distinguish separate site
  identities at one displayed position.
- A dashed purple ring marks an explicitly declared nonjoining crossing; it
  supplies no depth ordering.
- The source panel is for human inspection after the run. The engine does not
  read the rendered diagram.

Supplier induction currently computes its bounded source inventory eagerly.
Fewer questions establish more selective communication, not avoided source
computation. Demand-driven local source reads remain a useful next extension.
Other useful next tests are genuinely ambiguous partial input, changed source
evidence with preserved prior candidates, uneven query cost and learning which
questions or hypotheses to propose. Scaling motif composition and disposition
recognition remains separate from these small acquisition fixtures.

## Reproduction and verification

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --structural-evidence
dotnet test Tests/QualitativeExecution.Tests --filter "FullyQualifiedName~StructuralEvidence"
node experiments/QualitativeFrieze/tools/check-structural-evidence.cjs
dotnet test Gojo.slnx
```

The focused tests cover native pattern answers, all six executed cases,
port/request identity, bounded absence, unsearched and unsupported reads,
one-occurrence disclosure, conflict and capability states, source/evaluator
isolation, policy reuse and both-direction vocabulary checks. Browser checks
exercise every stage under all four strategies, local assets and desktop/mobile
layout. The complete audit archive restores 2,360,754 native participants with
zero measured numeric participants and reproduces the same policy coefficients.
It retains exact proof histories for this small run; it is not a compact
production training-data format.

The recorded solution verification passed all 4,509 tests, including the 24 new
focused checks. Browser verification covered six cases, 27 episodes, all four
question strategies, three controls and 28 local references, with no browser
errors or horizontal overflow at desktop and mobile widths.

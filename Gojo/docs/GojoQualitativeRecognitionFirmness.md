# Qualitative recognition evidence and placement

Experimental checkpoint, 2026-10-18. Recognition now separates heading
admission from demonstrated route and circuit participation, retains local
counterexamples against preserved motifs, and can locate line motifs inside
rectangular interval chambers through an explicit ordinal observation. The
claims are firm relative to their connected premises; none selects a unique
global interpretation.

Open the [interactive gallery](../../experiments/QualitativeFrieze/results/recognition-firmness/gallery.html).
It follows [the first recognition round](GojoQualitativeRecognition.md).

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --recognition-firmness
```

An optional `--output DIRECTORY` selects another artifact directory. Positions
remain in the external renderer. Core recognition receives only the published
segment observation; placement receives an additional declared observation
surface, described below.

## Six local laws

| Read | What establishes it | What remains outside the claim |
| --- | --- | --- |
| Direction admission | Actual segment heading participates in a selected channel basis | Membership alone establishes neither a repeated flow nor a grid |
| Connected flow | Actual ordered uses join at the same endpoint roles; a constant run contains at least two passages | Matching projected positions do not join distinct roles |
| Two phase flow | At least four actual uses follow the declared two-state phase cycle, with witnessed transitions | It is a bounded repetition question, not arbitrary period discovery |
| Commuting circuit | Both two-step orders end at the same actual role and contain four distinct sites and passages | Local commutation does not prove a unique complete global grid |
| Strict chamber placement | Two ordinal projection orders, a validated axis rectangle circuit, and explicit whole-passage interval envelopes | Endpoint placement alone does not constrain an unseen passage; no general polygon or filled face is inferred |
| Retained occurrence | The original role graph and its bindings still satisfy their current local obligations | A failed occurrence does not erase the reusable pattern or unrelated occurrences |

The minimum route lengths select how much recurrence this reader asks to see.
They are visible repeated participation, not statistical confidence or proof
that a physical process will continue forever.

An actual false commutation was found during this round. Two heading-labelled
paths could share their first arrival and then their final arrival, using only
three actual sites. Endpoint agreement alone had admitted that collapsed
construction. The induction mechanism now keeps it as an unsettled obligation
instead of a four-site commuting proof. The evidence reader independently
checks the required distinct participants.

## Admission and qualified evidence

`QualitativeDispositionEvidence` reads the existing disposition captures. Each
candidate connects its actual channels to qualified route, commuting-circuit
and triangular-circuit witnesses. Diagnostic counts are projections of these
native relations; there is no confidence field or semantic score.

```text
route proof = (actual channel -> actual retained route) ->
              (actual segment support -> actual route transitions)
circuit proof = actual commuting witness ->
                (four actual segments -> four actual sites)
candidate evidence = actual candidate ->
                     (channel evidence ->
                     (route proofs and support ->
                     (circuit proofs and support ->
                     (triangle proofs and support ->
                     (unique support -> (admitted only -> outside)))))
```

The implemented ordered surface also retains single-edge routes, blocked
proofs and unresolved input. Its exact readers verify actual capture
membership and the corresponding native participants. A copied result-shaped
object cannot supply an observed proof.

The three participation groups are disjoint:

```text
observed segments = qualified support + admitted only + outside this basis
```

Qualified support is the union of these selected local proof families, not
material transferred into several consumers. A segment appearing in ten
overlapping proofs remains one supported observed participant.

The gallery keeps the evidence families separate. A field can contain long
routes without commuting cells, or triangular circuits without an appropriate
grid interpretation. Even full union coverage does not establish which
description best predicts new observations or serves a particular question.

Two useful counterexamples expose this distinction:

- **F01, coincident ends without joins.** Its 31 displayed passages have the
  same projected arrangement as the square-grid example, but each passage owns
  separate endpoint roles. A direction basis admits all 31; none belongs to a
  longer connected route, triangle or commuting circuit. The renderer shows
  hollow, separated endpoints so the missing joins remain inspectable.
- **F02, an isolated admitted diagonal.** The alternating-row field gains one
  separate diagonal passage. The horizontal-plus-two-phase basis admits 41
  segments. Qualified evidence supports the original 40, leaving the extra
  segment admitted but unsupported. A three-direction interpretation can
  coexist with that phase interpretation.

## Adding ordinal placement observations

The first round deliberately discarded relative placement between disconnected
components. The new supplier publishes order and projection equality while
continuing to withhold coordinate values, differences, lengths and source
history.

```text
projection order = actual observation axis -> ordered scope of buckets
bucket = Closed(actual observed sites sharing this projection)

passage envelope = actual observed segment ->
  ((horizontal axis -> (first endpoint -> second endpoint)) ->
   (vertical axis -> (first endpoint -> second endpoint)))
```

Adjacent buckets do not promise equal spacing. Equal projection does not
establish shared identity, physical contact or a join. Each supplied passage
envelope declares that the whole passage support remains within the product
of its two endpoint intervals. The declaration is an explicit premise of the
selected line-support model, not a result inferred from its endpoints or the
renderer drawing a straight line.

The external publisher compares formal source words through the existing
motion frame, then maps its conclusions onto the fresh observation sites.
Its source words and comparison provenance remain outside `Placement.Surface`.
The publisher supplies no passage envelopes unless the fixture author explicitly
declares them. The native receiver can also accept ordinal observations from
another supplier without consulting those source words.

The local containment reader requires four distinct, connected corner roles,
four distinct passages, two projection levels on each axis, and matching
single-axis boundary headings. It verifies that these form the selected
rectangular product of intervals. Every inner endpoint must be strictly
between both pairs of bounds. Whole-support acceptance additionally requires
valid envelopes for every outer and inner passage.

Known endpoint equality with a boundary is retained as touching in projection;
known outside endpoints remain outside. Missing or contradictory order,
unavailable participants and missing envelope premises stay unresolved or
blocked. This is a conditional interval-containment read, not a general
geometric embedding, face finder, collision detector or filled-region algebra.

## The same motifs under different arrangements

The placement experiment reuses the same actual segment observation, motif
definitions and twelve isolated diamond occurrences in every state. Only the
supplemental order observations and declared passage constraints change.

The arrangement question supplies one motif slot per each of twelve selected
chambers. This is a chosen goal. Empty and competing slots are outcomes relative
to that goal and this finite induced motif family, not universal judgements
about an ornament.

| State | Strict whole-support assignments | Empty slots | Competing slots | Unresolved slots | Definitely unplaced motifs |
| --- | ---: | ---: | ---: | ---: | ---: |
| P01 ordinal order and envelopes | 12 | 0 | 0 | 0 | 0 |
| P02 one motif moved into its neighbour's chamber | 12 | 1 | 1 | 0 | 0 |
| P03 one motif straddles a chamber boundary | 11 | 1 | 0 | 0 | 1 |
| P04 second projection order withheld | 0 | 0 | 0 | 12 | 0 |
| P05 passage envelopes withheld | 0 | 0 | 0 | 12 | 0 |

P02 contains two separately identified motifs with equal rendered positions.
They remain two candidates, not one merged participant. Their local motif
definitions stay valid while the arrangement question exposes competition.

P05 still has twelve positive endpoint-interior queries. It has zero positive
whole-support assignments: an unseen bowing passage could leave the endpoint
box. The distinction is why the additional envelope premise is substantive.

Each chamber query keeps all twelve positive, negative or unresolved motif
comparisons in `Considered`. Empty-slot claims therefore retain the finite
candidate scope and their negative evidence, rather than depending on a host
counter whose reasons have disappeared. Renderer positions and external source
provenance remain outside this claim surface.

## Reapplying preserved motifs

`QualitativeRecognitionRevalidation` retains the original induced role graphs
and their actual occurrence bindings. Unchanged actual segment rows can be
reused directly. Replacement rows require an explicit correspondence from the
old actual row to the current actual row; an equal identifier alone does not
authorize an unobserved replacement.

```text
segment correspondence = Closed(actual prior row -> actual current row)
check = (prior family -> prior occurrence) ->
        (current observation ->
        (supported obligations ->
        (refuted obligations ->
        (unresolved obligations -> actual dependencies))))
```

The smaller probe changes one passage of one diamond. All twelve original
diamond candidates remain available:

| State | Supported | Refuted | Unresolved |
| --- | ---: | ---: | ---: |
| V01 original observation | 12 | 0 | 0 |
| V02 one heading changes | 11 | 1 | 0 |
| V03 heading unavailable | 11 | 0 | 1 |
| V04 passage absent from a closed inventory | 11 | 1 | 0 |
| V05 passage absent from an open inventory | 11 | 0 | 1 |

The larger local catalogue also retains junction and walk candidates. A
junction star promises complete advertised incidence, so a new actual incident
passage can refute that star. A walk or cycle is an existential claim about its
retained passages: an unrelated extra branch does not erase it. Open inventory
cannot prove that an expected passage has disappeared or that no extra branch
exists. Refutations and unresolved obligations remain attached to their
particular bindings and dependencies.

An open heading can still refute a match when its known prefix contains a
direction excluded by the expected closed heading. A compatible known prefix
leaves the missing remainder unresolved. Reversed uses transform the known
directions through the frame's actual opposites while retaining the open tail.
Incomplete evidence therefore need not force an entirely undecided read when
a local counterexample is already present.

These are bounded revalidation reads. They neither search for all replacement
occurrences nor repair the motif, learn a new pattern, or identify a physical
cause of discrepancy. A contradiction can concern the observation, proposed
binding or retained question as well as the source process.

## Remaining boundaries

The implemented mechanisms strengthen exact local claims. Choosing among
several useful global interpretations remains exploratory. In particular:

- Residue and counterexamples retain reasons for another question, but do not
  yet allocate the next search or discover a repair law.
- The phase reader still asks bounded two-phase questions. Larger periods,
  hierarchical grammars and held-out transfer need separate experiments.
- Placement uses two ordered projection surfaces and explicit passage
  constraints. Arbitrary polygons, skew chambers, recovered faces and spatial
  relations without those observations remain open.
- Projection-order validity is conservative: a contradiction in an advertised
  axis can leave that axis unresolved beyond the immediate offending site.
- No probability model follows from multiple candidates or missing detail.
  Observational remainder stays residue or an open obligation; it does not
  introduce physical carrier tension without a response route.

The mechanisms remain above the kernel in `experiments/QualitativeFrieze`.
No kernel primitive, numeric measurement or image classifier was added.

## Verification

- All 118 focused recognition, motif and disposition tests pass. This includes
  54 new cases covering distinct-site commutation, qualified support accounting,
  native source blindness, order and envelope premises, local refutation,
  open observations, aliases and ambiguous replacements.
- `dotnet test Gojo.slnx` passes all 3,828 tests, including 1,398 qualitative
  execution tests.
- The Release command regenerates seven evidence fields, five placement states
  and five retained-candidate states across the original local motif families.
- Headless Edge checks all placement and selected-diamond result counts,
  evidence selection, retained before views, step controls, chamber-slot
  overlays, local links, SVG parsing and desktop/mobile layout. No script
  errors, broken links, invalid SVG or horizontal viewport overflow occurred.

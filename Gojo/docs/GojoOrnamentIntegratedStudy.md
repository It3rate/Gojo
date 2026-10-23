# Ornament integrated with a retained mesh

## Design guidance for the first integrated pass

These are proposed design directions, based on reading the twelve native
sources in
[`OrnamentSubstrateCorpus.cs`](../../experiments/QualitativeFrieze/Composition/OrnamentSubstrateCorpus.cs)
and inspecting their existing finished-ornament previews. They are not claims
that an integrated motif generator or its attachment reads already work.

The main opportunity is **larger growth from fewer meaningful roots**. The
existing wave, lens and scallop frames have enough room for a leaf or fan to
shape an entire compartment. A small independent flower at the compartment's
centre does not use that opportunity. A large motif can still be subordinate:
its flow, boundary spacing, colour contrast and repetition can support the
mesh even when its occupied area is substantial.

Use the same historical guidance retained in the
[substrate study](GojoOrnamentSubstrateStudy.md): follow parent lines, make
gradual tangent transitions where selected, divide the overall form before
enriching it, and let colour distinguish related parts. These remain contextual
design targets. They do not establish a universal aesthetic score.

| Retained source | Proposed integrated motif | Root and direction | Floret or focus landmark |
| --- | --- | --- | --- |
| **S001 — Unbroken plain weave** | Embroidered ribbons with a broad paired-leaf growth opening into alternate square pockets. Keep the two ribbon directions legible. | An exposed strand landmark between crossings; the leaf's basal vein starts along the strand before curving into the pocket. | A compact collar on selected **front-strand** crossings. It must not merge the two crossing threads. |
| **S002 — Two-over two-under twill** | Longer compound leaves following the longer exposed two-over runs, rather than one flower in every small square. | An exposed run and its phase-change landmark; main leaves point along that run and smaller leaves turn into its neighbouring spaces. | Selected phase changes, with an alternating pattern sparse enough to retain the twill's diagonal tendency. |
| **S003 — Diagonal braid field** | Paired long lancets or a winged leaf growing into a diamond opening. The dominant ribs follow the diagonal braid. | A selected diagonal strand near an opening's corner, with a short root following its actual direction. | A small lozenge-oriented blossom at alternate crossing landmarks; retain the underpass cue. |
| **S004 — Three-family triangular net** | A large three-leaf palmette occupying selected triangular pockets. Its outer leaves echo the two inclined sides. | A triangle vertex or an exposed side landmark; the fan opens towards that triangle's interior. | A small six-part floret may mark an actual six-arm junction. An interior motif should respond to the triangle rather than impose an unrelated fourfold centre. |
| **S005 — Continuous softened waves** | Alternating broad leaves growing above and below the wave, with a larger terminal bud or small three-lobed flower on the repeat. | Actual curve section endpoints and their tangent ports; root direction follows the wave before the leaf opens into the broad inter-row space. | A full-period or selected branch-termination landmark, rather than a mark at every bend. |
| **S006 — Opposed waves and ogee lenses** | Large almond-shaped palmettes or a paired leaf spray filling most of each lens. Alternate their rooted ends. | An actual lens-end site; one rib follows the lens axis while two outer leaves echo the opposed curved boundaries. | A small blossom at selected shared lens ends. The large interior leaves carry the detail focus. |
| **S007 — Scalloped half-drop courses** | A pendant fan with a stem leading from a scallop valley into the pocket, its outer leaves spread beneath the curved arch. | A shared valley landmark, with the fan arranged in the local dropped-row frame. | A small flower at selected valley/row landmarks. Keep the large fan connected by its visible stem instead of suspending a star in the centre. |
| **S008 — Sinuous foliate cables** | One generous compound leaf branch per wave period, alternating left and right, with curved veins and a restrained terminal flower. | An actual wave endpoint and tangent, extending briefly with the parent before separating into leaves. | The branch terminal or selected repeating root. Avoid a second independent flower array beside the cable. |
| **S009 — S-shaped counterchange cells** | A corner-rooted leaf whorl: two broad leaves echo neighbouring S boundaries and turn towards the cell interior. | A selected cell corner and the ordered directions of its two incident edges. Adjacent cells may reverse the whorl. | A small floret at selected four-way mesh sites. Interior accents should remain connected to the corner growth. |
| **S010 — Tab-and-recess compartments** | A broad curled leaf that follows a tab or recess, paired with a second leaf spreading into the larger chamber. | A tab shoulder or a genuine interior corner, with its direction taken from the boundary rather than a global upright axis. | Selected four-way corners or tab landmarks. Keep enough of each tab/recess exposed to preserve the interlocking shape. |
| **S011 — Bowed triangular counterchange** | A three-leaf fan or curved trefoil filling selected triangular faces; outer leaves follow the bowed sides. | A bowed side's endpoint with a known tangent, opening into the selected face. | Small florets at selected mesh junctions; replace the independent four-petal interior mark with a form responding to the three-sided compartment. |
| **S012 — Interlocking oblique feather cells** | An oblique feather fan or paired acanthus-like leaf whose ribs follow both local cell directions. | A tab shoulder or oblique corner, retaining its actual local frame. Alternate the handedness with the cell rhythm. | Small florets at selected oblique four-way junctions. An upright centre motif should not compete with the cell's slant. |

The first pass should favour a recognisable silhouette, one connected main
vein or branch, a pair or small group of leaves, and at most one floret. Fine
veins and edge echoes can enrich those existing leaves. Adding separate motifs
at every available landmark would produce another competing pattern.

## Attachment and interpretation requirements

The mesh should remain the actual source structure, including strand identity,
width, sides, crossing order, cells and repeat correspondence. An enriched
design should carry that source as retained participation and add selected
attachments; it should not reconstruct a similar-looking mesh from new paths.

For every motif root, retain the actual source section or junction, the exposed
landmark and direction read, the motif's root, and the correspondence that
places or orients it. A curved endpoint can expose a known tangent. A point
inside a straight section can expose a declared landmark without splitting the
section into a new topological joint. An interior curved attachment needs its
own tangent or unresolved direction read; it must not obtain an unrecorded
direction from the renderer.

A flower at a crossing is an **ornamental attachment to a selected participant**,
not evidence that the crossing threads join. A leaf touching a cell boundary
is likewise different from a topological weld. These distinctions matter most
in S001–S003, where concealing every underpass with a flower would destroy the
weave interpretation even if the original path relations survived privately.

The proposed colour roles are a coherent mesh family, a related but more
prominent leaf mass, a restrained floret accent and a separating edge where
needed. The detail view can give leaves greater contrast while retaining enough
mesh to show their growth. Area, width and hue all affect that balance; a
successful ordinal contrast test alone is not evidence that the design reads
well.

## Inspection questions for the generated pass

- Can the eye trace each large motif back to a specific parent line or landmark?
- Does the motif's main rib agree with the source's local flow before departing?
- Do its broad leaves shape the interior space rather than float inside it?
- Does the same mesh remain recognisable with the enrichment hidden or subdued?
- Are the weave's over/under gaps, the scallop's half-drop, the triangular pocket
  and the tabbed cell's interlock still visible where they define the source?
- Are repeated florets marking actual related events, with enough quiet space
  between them to avoid a second unconnected grid?

These are practical review questions for this round, rather than established
automatic perception tests. The implementation and measured outcomes should be
added after the native attachments and generated examples have been inspected.

## Implemented pass and review

The [before/after gallery](../../experiments/QualitativeFrieze/results/ornament-integrated/gallery.html)
and [whole sheet](../../experiments/QualitativeFrieze/results/ornament-integrated/overview.html)
retain all twelve source meshes and add a replacement detail layer. Before and
after use the same palette and viewport. The connection view exposes the parent
paths, motif spines, roots and source direction glyphs; the substrate remains
available as an overlay.

The earlier decoration is carried in the native source/complement relation.
The new output selects the original mesh envelopes and curves by identity,
then adds the new detail. It does not reconstruct a similar mesh. The original
weave strands, crossing orders, cell cycles, repeat landmarks and paired
boundary proofs remain available through that retained source.

| New ID | Implemented refinement |
| --- | --- |
| I001 | Larger opposed leaf pairs grow into alternating square pockets. Small collars attach to selected front strands at crossings. |
| I002 | Longer leaves follow the exposed twill directions, with restrained collars at selected recurring sites. |
| I003 | Paired lancets turn into diamond interiors; four-part collars follow the diagonal braid directions. |
| I004 | Broad two-leaf corner fans respond to triangular spaces; six-part florets mark selected source junctions. |
| I005 | Paired leaves grow above and below the wave from actual section endpoints, with occasional root blossoms. |
| I006 | Broad almond-like blades reuse the actual upper/lower lens curves as one boundary, replacing the floating interior half-leaves. |
| I007 | Large paired fans begin at the two scallop valleys and open towards the chamber, with only occasional landmark florets. |
| I008 | Larger branches grow from the actual S-cable endpoints and retain their forward or opposed tangent relation. |
| I009 | Broad blades follow two incident S-shaped boundaries, creating a corner-rooted whorl; florets occupy selected four-way sites. |
| I010 | One generous blade grows from each tab shoulder into its chamber. A second trial blade was removed after visible overlap. |
| I011 | Larger paired corner leaves echo the bowed triangular sides, replacing independent four-petal interior marks. |
| I012 | One oblique blade grows from each tab shoulder; its first control follows the opposed shoulder tangent and then turns into the chamber. |

The first visual review caught two useful failures. The initial I003 lancets
lay mostly along the braid rather than changing the open diamonds, so their
tips were redirected inward. Initial I010/I012 paired leaves crossed near their
tips or straddled the tab recess, producing outline loops. A single larger
shoulder-rooted leaf gave a clearer result. These adjustments came from visual
inspection, not an automatic collision or beauty classifier.

I012 exposed another useful failure: a nonzero side offset can run almost
along the curve through its middle and still look like a thin stroke. The
final single blade uses a more transverse midsection profile and a shorter
initial control while retaining the same opposed root tangent. It has a broad
filled middle without crossing the tab recess. Root tangency alone is therefore
insufficient as a band-quality check; transversality along the span is a useful
future rule-pack target.

The triangular fans and the affine/oblique collars follow their local context;
the pass does not claim every floret has exact equal-angle, equal-length or
metric rotational symmetry. The table above records the implemented designs,
which differ in detail from some of the initial proposals.

## Native attachment contract

The added local surface exposes an actual source endpoint or a selected
straight-interior occurrence:

```text
port = (frame -> source envelope) ->
       (actual endpoint/landmark selection ->
         (position -> (tangent -> native difference receipt)))

endpoint selection =
  Closed((actual section -> actual longitudinal endpoint marker) ->
         (actual site -> actual position word))

interior selection = (lattice -> actual landmark) -> Closed(optional crossing event)

attachment = (mesh port -> actual motif-start port) ->
             (position difference ->
               ((mesh tangent -> motif tangent) -> motion basis))
```

The tangent is the actual retained control-displacement direction read, not a
calibrated polynomial speed. Endpoint markers distinguish the start and end of
a closed curve even when both use the same physical site and position. A valid
source spine can still expose a port when its width is unknown. A supplied
interior curved position does not gain a tangent from the renderer: that port
remains open in this fixture.

The attachment reader reports incidence, forward direction, opposed direction
and shared site separately. Its `Smooth` property means incident centre-path
roots with forward tangency. An opposed tangent can still be geometrically
collinear while retaining the opposite flow. Neither establishes side-boundary
tangency, region union or material transfer. A gap can coexist with matching
directions, and a conflicting site alias does not make a noncoincident root
incident.

For a straight crossing occurrence the source strand is never split. The
landmark selects its actual position and source participant; it supplies no
new topological weld. Florets at a weave crossing attach to one strand and do
not join the crossing threads to each other.

Each new profile application connects its reusable local controls and offsets
to an actual source port. Additive receipts consume the port's exact position
word, and the generated curve consumes the returned control words. The local
attachment read checks its tangent against the exposed parent direction.
Curve-following blades go further: their spine is the actual source section,
with a zero offset on one side and a selected inward profile on the other.
Their silhouette is consequently related to the parent curve by construction.

```text
integrated = source ->
  ((mesh selection -> carried old decoration) ->
    (output unit ->
      (Closed(attachments) ->
        (Closed(actual motif groups) -> Closed(profile applications)))))
```

The native root preserves these connections. The source names, design IDs,
cached array indexes, SVG paths and colour names are presentation or authoring
conveniences. They are not the definition of incidence, direction, shared
sites or the resulting curves. The finite design recipes are authored, not
learned or autonomously discovered.

## Validation and limits

Fifteen attachment tests check real endpoint operands and marker order,
forward/opposed/corner reads, gap/site contradictions, unknown widths,
straight-interior incidence, unresolved curved interiors, and rejection of
equal-coordinate or equal-tangent substitutions that lack the actual source
receipts. Six integration tests check the twelve designs, mesh preservation,
carried old decoration, actual generated control consumption and unchanged
weave order. Native mesh selection uses the source graph even if its diagnostic
generation array is removed.

The final twelve designs contain **374 incident root connections**: 257 forward
tangent, 95 opposed tangent and 22 corner attachments. There are **316 shared
endpoint roots** and **58 straight-interior incidence roots**. The latter do
not weld the long source strands. These counts describe the supplied native
construction; they are not probabilities of visual success.

The full solution run passed **5,284 tests across twelve assemblies**. After the
last I012 width/profile refinement, all **21 focused attachment/integration
tests passed again**. The retained final generation took about 170 seconds
while local verification was active; this is not a throughput benchmark.
Final browser validation passed all twelve designs, sixty SVGs, twelve native
archives, 69 browser states and 88 local references, with zero errors. It also
checks that before/after viewports and presentation faces match. The expanded
native review packets occupy about 61.4 MB compressed because the retained
source and construction/attachment receipts remain present.

Local centre-path evidence is stronger than the previous independent motif
placement. It is still not a proof that the whole design has no overlap, that
all side boundaries join smoothly, or that the detail is perceptually dominant
in the intended way. Visual inspection remains necessary. There is no learned
aesthetic scoring, automatic leaf-clearance solver or live propagation of an
arbitrary parent edit through all motifs in this pass.

Source/control participation is unmeasured common-reach structure. Counts and
relative words are used, but there is no physical length calibration. Floating
point normalisation in the connection panel only draws bounded tangent glyphs;
it does not supply the native tangent judgement.

## Code and reproduction

- [Endpoint/interior ports and attachment reader](../../experiments/QualitativeFrieze/Composition/OrnamentMotifAttachment.cs)
- [Retained mesh selection and rooted motif recipes](../../experiments/QualitativeFrieze/Composition/OrnamentIntegratedCorpus.cs)
- [Generation, audit and rendering](../../experiments/QualitativeFrieze/Composition/OrnamentIntegratedExperiment.cs)
- [Attachment tests](../../Tests/QualitativeExecution.Tests/OrnamentMotifAttachmentTests.cs)
- [Integration tests](../../Tests/QualitativeExecution.Tests/OrnamentIntegratedTests.cs)
- [Saved results](../../experiments/QualitativeFrieze/results/ornament-integrated/summary.json)

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-integrated
dotnet test Tests/QualitativeExecution.Tests/QualitativeExecution.Tests.csproj --filter 'FullyQualifiedName~OrnamentMotifAttachmentTests|FullyQualifiedName~OrnamentIntegratedTests'
node experiments/QualitativeFrieze/tools/check-ornament-integrated.cjs experiments/QualitativeFrieze/results/ornament-integrated/gallery.html
dotnet test Gojo.slnx
```

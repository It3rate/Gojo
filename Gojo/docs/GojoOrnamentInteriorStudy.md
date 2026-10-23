# Ornament built within mesh compartments

## Purpose and historical basis

This exploratory pass replaces the preceding one- or two-leaf attachments with
compound patterns that occupy the mesh's spaces. The objective is an interior
composition with its own centre, branches, secondary landmarks and reserved
ground. Physical attachment to a lattice line is one useful relationship; it
does not by itself make a satisfying space-filling design.

Lewis F. Day's *Planning of Ornament*, collected in *Ornamental Design* (1890),
provides a direct construction precedent. [Plate 38](https://archive.org/details/ornamentaldesign00dayl_0/page/n266/mode/1up)
develops parallel ribs into fans and radial divisions, parallel bands into
concentric arcs, and combines rings with rays. [Plate 39](https://archive.org/details/ornamentaldesign00dayl_0/page/n270/mode/1up)
uses a central rosette, branching scrolls and secondary flowers to articulate
an entire disc. Its upper-right example is especially relevant. The
[accompanying text, pp. 37–44](https://archive.org/stream/ornamentaldesign00dayl_0/ornamentaldesign00dayl_0_djvu.txt)
describes working from either centre or sides, adapting subdivisions to the
enclosing shape, and considering the interspaces as well as the lines.

Owen Jones's [original *Grammar of Ornament* propositions](https://archive.org/stream/grammarornament00Jone/grammarornament00Jone_djvu.txt)
offer complementary guidance. Proposition 7 proceeds from general form through
subdivision to enrichment. Propositions 10–12 balance straight, inclined and
curved lines and favour traceable growth and tangent junctions. Propositions
14, 16, 29 and 32 distinguish parts with colour, support small saturated accents
with broader quieter masses, and separate contrasting fields with edging.
These are historical design heuristics, not universal perceptual laws. The
new recipes adapt their construction ideas rather than copy particular plates.

## Intended twelve-case pass

Each case keeps the original source mesh. The table records design intentions;
the executable corpus and final review determine what was actually achieved.
Every compound interior should have several participating strokes or boundaries,
rather than one isolated leaf. Construction lines need not all remain visible
in its finished reading.

| Case | Retained mesh | Interior construction and intended emphasis |
| --- | --- | --- |
| F001 | Plain weave | A four-part centre rosette with straight ribs and bowed petal boundaries in selected square pockets. Retain visible underpasses. |
| F002 | Twill | An elongated palmette with a central rib and paired curved branches; its repeated mass follows the twill's diagonal tendency. |
| F003 | Diagonal braid | A lozenge-framed rosette with diagonal spokes and secondary lobes. The interior masses, rather than braid collars, carry the focus. |
| F004 | Triangular net | A three-part fan or trefoil with centre-to-corner ribs and curved lobes responding to the three-sided compartment. |
| F005 | Softened waves | Opposed tangent branches establish a larger interior pocket, then support a compound flower or fan within it. |
| F006 | Ogee lenses | A lens-centred palmette with several ribs, enclosing curves and an intermediate crown; boundary tangency informs the new scaffold. |
| F007 | Half-drop scallops | A broad valley-rooted fan with several rays and repeated lobes. Keep the dropped courses legible around the filled spaces. |
| F008 | Foliate S-cables | A branching interior spray, using cable tangents for departure and secondary landmarks for terminal flowers. |
| F009 | S-shaped cells | A centre whorl or rosette framed by curved branches derived from the cell's incident directions. Alternate its emphasis with the ground. |
| F010 | Tab-and-recess cells | A compound central flower with edge-directed buds and connecting ribs. Preserve enough tab and recess to identify the interlock. |
| F011 | Bowed triangles | A three-part interior growth combining straight radial structure and curved lobes related to the bowed boundaries. |
| F012 | Oblique feather cells | An oblique interior medallion with multiple ribs and side lobes. Present the mesh with constant-width strokes and use even contour outlines. |

The family should include centre growth, tangent departure and perpendicular
departure from edges, with both straight and curved construction. No individual
design needs to demonstrate every mechanism. Perpendicular here is a geometric
relationship justified in the chosen local frame, rather than the general
Gojo meaning of an orthogonal outlet.

## Native construction and interpretation

The new `OrnamentInteriorFrame` surface selects either an actual ordered cell
cycle or an explicit scaffold of actual source sections and exposed ports.
A cycle preserves endpoint-incidence witnesses and shared-site closure. A
scaffold retains its selected ports without asserting that those ports bound
a closed face. The distinction remains available to the reader.

Its centre is an endpoint-average construction landmark. Additive receipts
consume the actual selected endpoint or port words; equal groups of the
surviving common-reach participation supply the centre word and agreement
witnesses. This is not an area centroid or a discovered proof of containment.
An unavailable exact partition remains open.

The composition grammar is:

```text
source mesh -> actual cycle or exposed scaffold -> witnessed centre
            -> local interior construction -> reusable compound motif
            -> transform and actual anchor word -> application and output

result = source -> (retained mesh -> carried prior decoration)
                -> (interior frames -> motif templates -> applications)
                -> (output -> correspondence and unresolved participation)
```

Reusable local motifs are applied through `OrnamentBandComposition.Place`.
Filled petals use an actual construction-guide section as their spine. The
local guide is therefore an input to the petal band, rather than merely a
similar-looking sibling curve. Guide and ornament applications retain that
shared source; their separately placed output sites are not implicitly welded.
The application consumes the actual anchor word and native direction
substitution, transforms its spine and side-offset words, and retains source,
output, additive receipts and site correspondence. A visual resemblance or an
equal coordinate does not establish a shared site. Template reuse does not
weld independent motif instances.

The exact source mesh envelopes, curves and crossing order are retained; the
previous motifs are carried in the complement rather than silently deleted
from provenance. New centre-based ornament may use the mesh as a placement
and construction structure without following every boundary. It must still
respect the visible flow and the intended reserved spaces.

Colour is a selected presentation focus. One reading can favour the interior
flower masses while quieting the mesh; another can favour alternating ground
compartments and make the ornament subordinate. Constant contour outlines,
flat fields and clearly separated colour roles are preferred in this pass.
Ribbons and deliberate leaf tapers remain available, but the F012 mesh should
not acquire a calligraphic thick-to-thin appearance.

## Review and limits

### Optical placement refinement

An endpoint-average or repeat-cell centre is a construction landmark, not
necessarily the useful visual centre of a curved compartment. The current
refinement keeps that evidence and adds authored placement adjustments through
the existing native sum/application receipts. It does not estimate a geometric
area centroid, discover a weighted centre, or add a hidden numeric area solver.
The offsets are unmeasured common-reach words in the same local frame.

- **F003:** shorter petals separate neighbouring silhouettes. A native scope
  selects the ornament as recessed participation; its quieter display is drawn
  before the indigo mesh so the braid remains the foreground reading.
- **F007:** lower the fan focus by three native reach participations and apply
  the compound to all five actual scallop courses. The top row is complete,
  with clearance between the leaf tips and the arches.
- **F008/F009:** select boundary stems into the guide template, excluding them
  from the output unit. Their actual mesh root, tangency, shared site and
  arithmetic receipts remain inspectable; this is a read selection rather
  than deletion of the mesh participant.
- **F010:** move the focus up and left and use a diagonal native substitution.
  Its two direction images are `(1,1)` and `(-1,1)`. This changes the orientation
  and scale together; it is not an independently calibrated metric rotation.
- **F011:** retain the leftward bias but lower the flower in the darker face
  phase and raise it in the lighter phase. The motif foci form a new alternating
  disposition over the original horizontal landmark courses. Both the old
  substrate correspondence and the phase-specific anchor sums remain available.
- **F012:** move the focus left, retaining the same even outlines and compound.

These are direct, inspectable controls. A future learned optical-focus rule
could propose the same kinds of adjustments through exposed evidence, but the
present changes are authored in response to visual review.

Visual review should ask whether each interior has a clear dominant mass,
several related parts, intelligible reserved ground and a relationship to its
compartment. Detail should enrich those parts rather than form another array
of disconnected marks. Inspect joins, neighbouring repeats, weave visibility
and the silhouette at a distance as well as close up.

This is an authored construction experiment. It does not implement generic
face discovery, a containment or collision solver, learned aesthetic scoring,
or an inference that every chosen scaffold is a true bounded interior. A
successful local placement or tangency read cannot establish those broader
properties. Numerical rendering and colour choices remain presentation
adapters over the native construction.

## Verification and results

The executable pass generates **12 designs with 231 interior compounds**.
Each reusable compound contains **9–16 native strokes**, with boundary stems
retained separately as guides. The palmettes in F002, F005 and F007 have the strongest filled
silhouette in this review. F004/F011 use larger three-part growth fitted to
oppositely oriented triangles. F006 adds diagonal petals and stronger interior
colour; F008/F009 retain actual boundary-rooted tangent stems as construction
guides, outside the finished ornament. F012 uses even mesh strokes and flat
petal masses. These are design judgements, not learned
scores or claims of universal preference.

There are **100 actual closed-cycle selections and 131 explicit scaffolds**.
The equal-partition read supplies 143 exact endpoint-average centres. The
remaining cycles keep their closure evidence while leaving the unavailable
average open. Their ornament uses a declared corner-relative construction
focus with actual sum receipts. F009 deliberately uses its repeat-corner
construction even though an average is available; F007 offsets an available
centre to suit the scallop pocket. No missing partition is silently rounded.

The earlier design intentions are broader than the implemented mechanism:
there is no generic curved-edge interior tangent discovery. F008/F009 consume
an actual endpoint position and its exposed tangent as the branch's first
control; their attachment reads distinguish incidence, forward tangency and
shared sites. Edge-normal/radial alignment in the other motifs is authored in
their selected local frame rather than discovered by a general geometric
solver. The selected full boundary and the new compound both remain available.

Initial generation took **287.5 seconds** in the recorded run while other verification
was active; this is not a throughput benchmark. The native archives retain the
previous design, selected mesh, carried earlier decoration, interior reads,
reusable guide/profile sources and all placements. They occupy about 154 MiB
compressed across the initial twelve cases. The optical-placement refinement
generated in **308.9 seconds** with concurrent solution verification and uses
**155.8 MiB** of compressed native archives. Display SVGs are derived views.

Strict final browser/asset verification passed: **60 SVGs, 12 native archives,
83 browser states, 89 local references and no browser errors**. All twelve
designs were reviewed as contact sheets, including the corrected triangular
placements and constant-width F012 mesh.

The optical-placement refinement also passed all 83 browser states and all
twelve native archives. Its seven changed case images match the visually
reviewed preview byte-for-byte; all 20 F007 fans and the F003 background read
are present in the final files.

Solution verification passed **5,307 tests in twelve assemblies** before the
last visual and boundary-stem refinements. All **24 focused tests passed on the
final code** (16 interior-frame tests and 8 corpus tests). The new tests cover exact source
identity, closure versus coincidence, signed and cancelled participation,
unavailable partitions, forged dependencies, native anchor consumption,
reusable seeds, actual guide-spine reuse and boundary tangent attachment.

After the optical-placement and guide-selection changes, full solution
verification passed **5,308 tests in twelve assemblies**, including the 24
interior-frame/corpus tests. The branch test now proves that valid construction
evidence remains in the guide template while the stem is excluded from the
output unit and the actual mesh participant remains present.

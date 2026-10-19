# Bourgoin atlas: reusable construction and finite curved profiles

Experiment, 2026-10-19. Twenty additional source-based skeletons, B15–B34,
and B08.B's angled ribbon junctions extend the previous fifteen examples.
The [comparison gallery](../../experiments/QualitativeFrieze/results/bourgoin-patterns/gallery.html)
contains them all. The [single-sheet atlas](../../experiments/QualitativeFrieze/results/bourgoin-patterns/overview.html)
and standalone SVG show all thirty-six together.

## What the descriptions mean

| Statement | Origin | What it establishes |
| --- | --- | --- |
| Recipe description | Authored explanation of the generating declaration | How we intended to construct this interpretation of the source |
| Structure and execution read | Computed from the imported native graph and its accepted output | Actual shared identities, paths, ports, admitted construction witnesses, seams and unresolved participation |
| Fidelity and limits | Authored assessment against the cited source | Which visual relationships are reproduced, simplified or omitted |

The prose does not generate the picture. Removing names and book references
from the packet does not remove the executable construction. Conversely, a
successful run cannot certify that our interpretation of an engraving was right.

`BourgoinConstructionRead` reads exact direct construction evidence rather
than inferring meaning from recipe IDs or mining arbitrary descendants. It
checks actual source-image substitutions and placements, coarse/fine
replacements and endpoint proofs, and ordered corner-to-port correspondences.
Unrecognized evidence and rejected/open claims remain separate. An absent
admitted prototype does not mean that a path has no meaningful structure.
For example, B10 admits two path refinements and no seed-image rows; its
thirty-four retained selector/prefix entries remain available as other evidence.
That is a limit of this particular reader, not thirty-four failed paths.

## What is reused, and which layers communicate

```text
retained contour prototype / coarse scaffold
  -> actual substitutions, placements and optional chord refinements
  -> composite motif: executable sibling paths + exposed landmark roles
  -> ornament program: phase rules + motif/transform bindings + period words
  -> sampled occurrences: separate cursors, sites and application witnesses
  -> field seam read: exposed endpoint agreements + accepted/blocked joins
```

Every sampled occurrence references its selected motif definition. Within a
motif, recipes built with `CorrectedImage` also reuse actual contour prototypes.
B02 has one rectangle prototype and four executable images; B08.B has one
two-chord prototype and four images. B13/B14 each reuse one closed chamber in
four placements. Occurrence-local sites remain independent until declared
relations admit contact.

This is more than a flat bag of unrelated coordinates, but it is not yet a
general recursively invoked child-program grammar. Sibling paths inside one
motif execute sequentially through drawing/reset/jump actions. The present
source library generates complete units before the field reads their ports.
Retained construction histories do not, by themselves, provide live
bidirectional propagation or neighbour-driven motif renegotiation. The earlier
adaptive motif experiments exercise proposal/commit feedback separately.

These recipes demonstrate shared definition, finite layered derivation,
phase/layout selection and local seam admission. They do not claim that every
descriptive layer is an independently running agent.

## Reusable laws emerging from the source exercise

These are experimental construction tools over existing primitives, not new
kernel kinds of ornament:

| Mechanism | Current encoding | Examples |
| --- | --- | --- |
| Ordered path or contour | Start word, ordered chords, actual selectors and closure claim | Every case |
| Shared prototype image | Actual source contour → executable path, with transform receipts and placement read | B02, B08.B, B13–B30, B32–B34 |
| Two-direction disposition | Two native period words and finite phase cycles | Mesh, bond, chamber and ribbon families |
| Extra direction family | Another explicit path family through the same frame and contacts | B16–B18 |
| Staggered bond | Course displacement with advertised half-side contacts | B01, B19 |
| Finite substitution orbit | Ordered phase identities and actual word-return witness | B20, B31 |
| Boundary refinement | Actual coarse chord → ordered fine chain, with zero displacement difference | B10, B11, B30 |
| Negative chamber as generator | Closed chamber prototype whose placed boundaries define the network | B12–B14 |
| Contact versus crossing | Explicit endpoint seam; no join invented at a stroke-interior intersection | B17/B18 versus B34 |

A grid or diamond lattice is a useful reusable starting structure. It is not
mandatory: a closed negative chamber or turning-path orbit can be a better
explanation. B05 and B24 give two descriptions of a related triangular/hexagonal
boundary family: hexagons leaving triangles, or triangles leaving hexagons.
Their generator identities and construction histories remain different.

The common rules are encoded as native words, substitutions, scopes, ports and
witnesses. Source-specific contours and finite proportions are still authored.
There is no automatic rule-discovery or minimum-description search in this
batch. A useful next consolidation would be a small reusable authoring library
for the verified mechanisms, followed by testing whether recognition can
recover more than one recipe for a field.

## Additional source studies

All references use the same primary
[Bourgoin scan](https://archive.org/details/thoriedelorneme01bourgoog).
Each gallery card includes its page and a reviewed source crop.

| Cases | Source family and chosen skeleton |
| --- | --- |
| B15–B18 | Figs.177, 178, 181, 183: oblique mesh, three-direction mesh and diagonal rectangle/square elaborations |
| B19–B21 | Fig.185's oblique bond, centred three-lozenge network and opposed chevrons |
| B22–B24 | Figs.187, 190, 193: alternating lozenges, offset squares and triangle-first hexagonal gaps |
| B25–B26 | Plate11 fig.3 and plate13 fig.8: six-point star junctions and hexagonal radial florets |
| B27–B28 | Plate13 figs.4 and 6: angled paired bands and faceted shell fans |
| B29–B30 | Plate17 figs.4 and 7: hooked hexagon junctions and faceted arch/scale courses |
| B31–B32 | Plate19 fig.3 and plate20 fig.3: four-turn key windows and diagonal key courses |
| B33–B34 | Plate20 fig.7 and plate21 fig.2: folded diagonal ribbons and crossed faceted loops |

Several later cases retain a coarse framework while omitting botanical
filling, shaded ribbon width or difficult subsidiary chambers. They are
qualified skeleton adaptations, not twenty claims of exact tracing.

## Curves at a declared finite resolution

B28, B30 and B34 introduce curve-like profiles as native finite chord chains.
Every visible bend is executable structure; the renderer does not secretly
smooth a straight graph. B30 also retains a coarse straight span and its actual
finite chord replacement with the same endpoints. The finer prototype then
supplies repeated placed arch images. The construction read retains an explicit
bridge from that refined prototype to its admitted image evidence; it does not
mistake a source prototype for an independently executed occurrence.

The refinement preserves endpoint displacement and coarse landmark meaning.
It does not preserve straightness, length or enclosed area. It does not
establish analytic curvature, tangent continuity, exact circles, equal angular
subdivision or a general spline algebra. Those need additional declared laws.
Finite profiles are useful now because their corners and approximation remain
inspectable.

B08.B remains angular: a steep short junction edge followed by a shallower
long ribbon edge improves the source's large/small diamond relationship without
requiring a curve primitive.

Here “pre-numeric” means executable relation/placeholder participation rather
than measured numeric carriers. Repeated shared reaches still encode finite
multiplicity and selected relative proportions. These are not completely
unconstrained topological sketches or proofs of Euclidean calibration.

## Reproduction and verification

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --bourgoin-patterns
python experiments/QualitativeFrieze/research/render_bourgoin_sources.py artifacts/bourgoin-research/source.pdf
dotnet test Gojo.slnx
```

The complete native batch contains 404 sampled occurrences, 8,714 strokes and
6,862 accepted seams. All thirty-six fields complete with zero blocked or
unresolved interior seam obligations. Declared finite frontier obligations are
separate. All packets contain relation/placeholder nodes and zero numeric
participants. The narrow construction reads also report zero rejected
construction reads or open evidence scopes for these imported complete examples.

The browser check covers all thirty-six computed panels and atlas tiles, ten
adjustable before/after overlay panels, occurrence colours, ports, construction
landmarks, line weight, image decoding, local links and desktop/mobile overflow.
No browser, image, link or SVG parse errors were found. The standalone atlas
PNG and SVG were visually inspected. Original B06 ink geometry remains unchanged.

`dotnet test Gojo.slnx -c Release` passed **4,053 tests**, with zero failures
or skips across twelve assemblies. This includes forty-seven new tests of
source reuse, native archive replay, actual seam contacts, retained refinement
history and important false reads: stale substitution receipts, unrelated zero
proofs, wrong refined endpoints, nested lookalike evidence and undeclared
crossing joins. The final qualitative test assembly contains 1,623 cases.

Focused `--cases` generation now filters declarations before their archive
round trips. It still constructs the authoring catalogue, but does not
serialize/import every unselected recipe. A one-case B08.B replay was checked
after this presentation-workflow change. This is not a computation benchmark
or a claim that the retained witness format is minimal.

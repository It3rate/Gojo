# Overlapping ornament views and derived scaffolds

Implemented experiment checkpoint, 2026-10-18. V01 establishes overlapping
observation covers over one executed ornament. V02 derives new topology from
those reads and uses exposed landmark bindings to construct new patterns.
The kernel is unchanged.

Open the [interactive gallery](../../experiments/QualitativeFrieze/results/ornament-perspectives/gallery.html).
The image supplied by the user motivates the shared yellow/green/blue reads;
these fixtures are authored structural analogues, not image reconstructions.

```powershell
dotnet run --project experiments/QualitativeFrieze -c Release -- --ornament-perspectives
```

## Targets and observed structure

| Field | Source | Alternative reads | Further construction |
| --- | --- | --- | --- |
| V01-T | 60 triangular chambers, 101 unique executed edges | Three lozenge pairings: 30, 24 and 25 groups; the shifted pairings retain 12 and 10 complementary border chambers | Three incidence levels; two levels rebuilt by fresh drawing flow; 20 sibling marker units |
| V01-F | 96 triangular chambers, 154 unique executed edges | 24 courts; 15 overlapping vertex diamonds; six super-courts; four rows; one field over the super-courts | Three incidence levels; two rebuilt by fresh drawing flow; 39 sibling marker units |

Every source edge comes from an acknowledged actuator. Explicit authenticated
endpoint publications and formal displacement checks justify shared-site
joins. A complete supplied finite cellular embedding gives the source chambers.
The source is neither a renderer-owned mesh nor a collection of duplicated
triangle outlines.

For V01-F, a court has four actual triangular members and a vertex diamond has
eight. A selected court and diamond share two members. Adjacent diamonds also
share two members **within the same read**. Fifteen diamond groups contain 120
membership occurrences over 76 distinct source chambers, with 20 chambers
outside that read and 22 unordered overlapping group pairs. Groups retain
separate identities rather than merging through an overlap chain.

The parent organizations also cross. A super-court contains four actual court
reads; a row contains six of those same court reads. Their correspondence can
retain two shared courts, eight shared triangles, and each parent's exclusive
participation. A third grouping level consumes the six super-court reads. No
single exclusive ownership tree is imposed on these concepts.

## Goal catalog expectations

| Goal | Checked structural target | Qualification |
| --- | --- | --- |
| G06 Triangular chambers | Actual closed three-edge source chambers and retained shared-edge incidence | A selected Euclidean display does not prove equilateral geometry |
| G12 One scaffold two readings | Several views share the exact source; common parts, exclusive parts and complements have actual witnesses | This round includes overlaps within a view and across parent levels |
| G07 Octagon and square field | Fan D1 contains 24 four-edge cells and 15 eight-edge cells; complete interior vertices have 4-8-8 incidence | Regular octagon angles, equal edge lengths and exact visual matching are not claimed |
| G11 Sixfold star corridors | The triangular source and its first derived network expose six-edge cells | Related exploration only; the complete star-corridor goal is not established |

These are structural comparisons with the named catalog targets, not image
similarity scores or proofs of human aesthetic preference.

## Views are overlapping covers

An equivalence-class quotient would be too restrictive for this example.
Within a single view a source participant may belong to several groups.
`OrnamentView` represents a non-draining observation cover:

```text
group definition = group identity -> Closed(actual bounded source faces)
view = (actual source -> Closed(group definitions)) -> question context
group read = view + actual definition
             -> members + strokes + sites
             -> internal seams + boundary seams + cut + complement
```

The native reader validates actual bounded source membership and rejects
foreign faces, copied lookalikes, duplicate members, ambiguous identities and
open selection claims. Known empty selection is distinct from an unresolved
selection. A complete supplied read need not exhaust the source.

Three relationships remain distinct:

- shared chamber membership;
- boundary contact witnessed by actual source seams;
- a shared corner with no common chamber or boundary edge.

`OrnamentViewOverlap` keeps many-to-many pair correspondences at each of these
grades, including exclusive parts and both contexts. It never discovers
overlap by walking arbitrary shared ancestry: a common frame or provenance
root cannot make every group overlap. Repeated views of one chamber are not
independent observational evidence or duplicated material.

`OrnamentViewComposition` consumes actual lower group reads. Each parent keeps
its child scope, each child's source members, and a source-face-to-child
occurrence correspondence. The expanded source selection is a union, while
the retained occurrence witness explains shared children or shared faces.
Further composition retains those relationships through its context.

## Reads produce different networks

`OrnamentViewNetwork` supplies a fresh representative role for each actual group
read. Boundary-contact witnesses supply one edge family. Shared-membership
witnesses supply a separate overlap family. Neither operation identifies the
representative with the group's original sites.

In the fan field, courts produce 38 contact edges and no overlaps. Vertex
diamonds produce 16 contact edges and 22 overlap relations. The gallery draws
contacts as new solid construction and keeps overlaps dashed and observational.
This is a causal difference in generated topology arising from a different
question over the same source, not a colour filter over one fixed output.

An arbitrary cover network need not be planar or have bounded cells. Its
normalized construction topology has no invented face scope.

## Cells supply successive construction scaffolds

`OrnamentDerivedScaffold` implements the separate cell-incidence construction:

```text
actual bounded source cell -> fresh representative role
actual shared seam + its two oriented cell uses -> representative adjacency
actual complete interior vertex cycle -> new ordered cell boundary
```

It retains source topology, actual edge/face correspondence, corner evidence,
boundary complement, blocked claims and unresolved participation. Its result
exposes the same native topology shape that the next read consumes. It does
not need access to the original builder or its logical grid indexes.

| Source | D1 sites / edges / cells | D2 | D3 |
| --- | --- | --- | --- |
| Triangular field | 60 / 79 / 20 | 20 / 43 / 24 | 24 / 29 / 6 |
| Fan field | 96 / 134 / 39 | 39 / 82 / 44 | 44 / 54 / 11 |

The triangular field alternates six-edge and three-edge cells. The fan field
exposes four/eight-edge cells, then triangles, then a smaller four/eight-edge
interior. The finite boundary shrinks differently at each level. Retaining that
boundary evidence does not make finite duality an exact involution or promise
reconstruction from adjacency alone.

## Landmark interpretation remains separate

A representative role does not establish a midpoint or geometric centre.
`OrnamentWordLandmarks` supplies an optional equal-share interpretation in the
selected formal additive basis. It reads actual site bindings, adds their word
participation, and divides it through distinct visible share slots:

```text
actual input bindings + share slots
    -> summation and cancellation witness
    -> complete repeated-move groups -> output word
    -> indivisible remainder + unresolved participation
```

An open binding scope cannot establish uniqueness by hiding its tail. Alias
copies of one placeholder cannot count as distinct share slots. Distinct reach
identities remain distinct. A partial word with a remainder stays open rather
than rounding to a convenient location. A complete read rechecks its actual
input occurrences and summation accounting.

The source uses six repeated reach occurrences along each authored coarse
span, allowing the selected triangular landmark reads to complete without a
new measured unit. Higher levels can reuse source vertex words through actual
cell correspondences; each publication retains its consumed binding and cause.
These are formal word calculations, not calibrated numeric carrier values.

Only the renderer folds the words into screen coordinates and selects an
axial or triangular display basis. Its apparent centres and angles are not
evidence for stronger native geometric claims.

## Derived structure drives new acknowledged flow

`OrnamentNetworkConstruction` consumes exposed topology and actual representative
bindings. Each source edge compiles to an independent local path unit with
placement, input/completion surfaces, actual motion difference and drawing
receipts. Only authenticated endpoint publications for the same source role
produce join declarations. Equal-position but distinct roles stay distinct.
Isolated representatives receive explicit point-placement actions without
invented ink.

The definition connects actual input edge to local unit and actual binding to
input use. The output retains command correspondence, role/site correspondence,
accepted joins, blocked structure and packet accounts. A sibling diamond-marker
program consumes D2 bindings through explicit placement-input links. The final
ornament combines these separate constructions without silently joining their
projected crossings.

The alternative `OrnamentScaffoldExecution` retains a shared-cursor realization
with balanced native controller groups and resumable acknowledged prefixes.
The larger fixture exposed substantial history-validation cost on that route.
Independent passage machines complete the current two-field run in roughly
ten seconds on this machine. Their execution histories and action counts are
different; this is not a benchmark proving equivalent traces or a general
speed guarantee.

The captures deliberately retain extensive evidence, including old source
participation and packet histories. This checkpoint is not a compact-storage
implementation. The native traversal contains no `Carrier` or
`CarrierLocation` elements; that does not imply a small object footprint.

## Interface and source meaning

The gallery's interactive selections display precomputed native correspondences.
Yellow-only, green shared and blue-only chambers come from the pair witness,
not pixel intersections. Stripes show several actual memberships within one
read. Toggling the display does not activate, destroy or rewrite a native view.
The system can retain these views simultaneously without asserting a theory of
human attention or perception.

Dots mark actual shared branching roles. Independent projected intersections
use bridge gaps; this optional rendering calculation cannot create a native
join, face, group or material transfer.

The underlying concept definition is the connected source, question,
membership/relationship requirements, correspondence, complement and supported
operation. Names such as court, diamond and row make those definitions
referable. This establishes executable instance descriptions and composition,
not an automatic universal classifier for every square or ornament family.

## Verification and remaining experiments

The focused suite contains 58 new cases covering overlapping covers,
many-to-many correspondence, layered composition, contact/overlap separation,
repeated incidence reads, conservative word sharing, actual source actuation,
both construction routes and whole-study invariants. Tests include false and
open claims, alias identity, changed source contracts, exact packet identities,
observation without mutation, explicit producer-consumer bindings and reachable
native result witnesses.

`dotnet test Gojo.slnx` passed all 3,626 tests. Browser verification passed for
both interactive fields, within-view overlap, cross-level row/block overlap,
local links, SVG parsing and narrow-screen layout, with no script errors.

V03 remains deliberately bounded to future work: proposing groupings without
the authored cell/vertex selections, hiding generating hierarchies, testing
held-out predictions and selecting useful descriptions. The current V01/V02
readers can validate supplied proposals and derive new structure symbolically;
they do not discover the source embedding from an image or learn the supplied
grouping rules.

Bourgoin's points/lines/cells perspectives and centre/diagonal constructions
motivated the incidence experiments; see his [network discussion, printed
pp. 246–250](https://archive.org/details/thoriedelorneme01bourgoog/page/n269/mode/1up).
The [prior unit arrangement checkpoint](GojoUnitArrangementExperiment.md)
documents the publication, actuation and shared-site distinctions reused here.

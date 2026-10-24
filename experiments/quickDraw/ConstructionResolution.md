# Lowering positional resolution while retaining a drawing

This exploratory checkpoint extends the [church and alphabet construction
series](ConstructionSeries.md). It varies numeric placement independently of
the rough-to-finished drawing steps. Website publication remains deferred for
this milestone; the code and portable review are retained in GitHub.

Open `D:\_quickDraw\gojo-construction-resolution\review.html` for the local
inspector, or the [portable review](results/construction-resolution/review/index.html).
The position slider selects measured, 64-, 32-, 16- or 8-division placement.
The existing step slider still selects construction progress. Comparison shows
the same drawing and step at all five position settings; it can also compare
different drawings at one selected setting.

There are 14 churches, 26 individual capitals and one alphabet atlas: 41
selections, 205 executable programs and 945 construction rungs. The atlas is
another selection of the same letter source, not additional independent data.
The earlier four-family pilot remains in its existing review unchanged.

## What is preserved and what may move

Each finite variant retains the original source edge count, ordered controls,
curve degrees, endpoint identities, shared endpoint incidence, object and
group membership, declared closed-region boundary walks and source stroke
order. Construction rungs retain their edge membership, retirement and guide
roles. Original source order remains distinct from the authored construction
proposal; neither is recorded human drawing timing.

Original equal scalar coordinates retain their alignment. Strict ordering of
distinct endpoint and control coordinates remains strict in each axis: a
left/right or above/below interval cannot collapse or reverse. Declared mirror
and translated-copy scopes retain their relationships, while their axes,
offsets, widths and heights may change. These are scoped claims, not a claim
that every whole church is symmetric. In particular, the side-tower and annex
examples have only local symmetry claims; the two imported icons supply no
declared mirror correspondence.

The independent audit reconstructs original authored object/group/region scopes
through source-to-native edge correspondence. It does not discover objects or
turn their labels into native primitives. Endpoint closure is checked by
identity, not coordinate coincidence. Native programs retain all participating
sites and edges; separate source scopes remain audit provenance.

Aspect ratios, relative part sizes, spacing and placement can change. Preserving
axis ordering and closed walks does not prove arbitrary curved containment,
absence of new intersections, all tangent relationships or perceptual identity.
No general geometric solver or learned recognizer is claimed. Visual inspection
of the church and alphabet comparisons supplies a further, limited check.

## A finite selection, with visible exceptions

The requested quantum is `reference span / divisions`. Churches use their
512-unit drawing calibration. Letters use a 128-unit cap-height calibration,
including in the atlas; using the atlas page width would make an individual
letter artificially coarse. Thus the four requested church spacings are
8, 16, 32 and 64 units; letter spacings are 2, 4, 8 and 16 units.

Independent snapping breaks mirror relationships and can collapse small spans.
Instead, the offline authoring adapter selects finite numbers under explicit
source premises:

```text
equal scalar read          x_a = x_b       (identities remain distinct)
reflected pair             x_a + x_b = 2 * axis
translated occurrence      target - source = shared displacement
ordered distinct reads     x_a < x_b
```

Exact rational elimination calculates directions in which the selected scalar
reads may change together without breaking the declared correspondences. Each
independent read is proposed on the requested integer grid. The proposal is
accepted only if integer participation and strict source ordering remain valid.
If both nearby coarse candidates fail, that read tries a finer grid. Every such
exception retains its attempted grades and rejection reason in the audit.

This elimination belongs to the authored data adapter. It supplies a finite
calibration for this experiment; it is not Gojo settlement, an online global
solver or a discovery of drawing intention. The native program is rebuilt from
the selected finite scene and executes through the existing local affine reads.
The original fine source is not used by its receiver to repair the result.

Church rough mass bounds also receive the requested finite placement. Their
rectangles, triangles and ovals remain the existing small engines. They may
overlap, overshoot or be inset from final geometry and impose no containment
condition. Letter mass templates remain the authored shared working frames;
their path placement is the selected finite source calibration.

The requested grid is not the effective precision of every coordinate. Dependent
coordinates follow their shared relationships; protected independent reads may
need finer spacing. The inspector reports moved control-point samples, maximum
movement and the number of finer slots retained in the source scene. For an
individual letter that last count belongs to its shared alphabet source.

Adjacent settings can be identical. The alphabet already fits the 64-division
grid, so that setting changes none of its final controls. Across all selections,
14 change at 64 divisions, 27 at 32, and all 41 at 16 and 8. These are independent
finite selections against one measured reference; they do not promise monotonic
geometric error or a nested bitstream. No compression ratio or reduction in
native archive size is claimed.

This lowers selected positional detail, not the precision of every field in
the encoding. Native affine instructions can still use exact rational ratios
to reproduce the chosen finite geometry. Lowering those instruction grades,
or replacing them with ordinal ports, is a separate future experiment.

## The relationships are executable references

The finite variants use the existing native construction format. A reflected
coordinate consumes an actual earlier control and axis site:

```text
group(axis, earlier, axis) -> (L(grade,1) -> S(grade,0,1))
read: axis + (axis - earlier)
```

Alignment consumes the earlier coordinate with zero displacement. Translation
uses the actual earlier template control as origin and a shared displacement
read. These operations do not weld different sites, even when they coincide.
The thirteen existing church repeat constructions remain intact; five repeats
in individual letters and seven in the atlas now also execute through native
template-origin references. This gives 25 native repeat requests per setting.

All references remain causal. Mirror-axis reads are explicitly introduced in
the root frame before consumption. New alignment/reflection references choose
an available earlier control. Existing translated occurrences retain their
template origins. A restored native reflection test changes its source
parameter and verifies the opposite movement, distinct identity and retained
earlier observation.

Each executable archive contains actual relations, site identity, finite affine
grades, curves, rungs and repeat requests. Removing source mappings, resolution
reports and descriptive binding records leaves its result unchanged. Native
execution retains its own fractional affine residue. That execution residue is
different from the offline reference-to-finite displacement and from a rejected
coarse proposal. None of these observations automatically introduces tension.

The display selects saved native replays. It performs no coordinate snapping or
shape correction in the browser. The same source and restored-program output
are independently checked; program-only restoration is sufficient for replay.

## Results and training boundary

All 205 programs restore and replay. The independent audit checks 669,335
assertions over 945 rungs, including 165 scoped mirror claims, 125 repeat claims
and 380 closed-region walks. Counts include repeated selections such as the
alphabet atlas; they are not counts of independent observations.

Verification also passes 144 Python tests and 5,375 .NET tests across all twelve
solution test projects, including the restored reflection dependency test.

The browser audit checks 27,820 rendered paths, 14,575 comparison paths, 205
dependency selections and all executable downloads, as well as ordinary local
file loading, portable file loading and mobile layout. The retained screenshots
show the compact square-window church, side-tower church and alphabet at the
coarsest setting.

The new transition file contains 740 authored step examples. Earlier context,
next instructions and targets remain separate. Every metric variant and every
alphabet selection keeps its original source-scene split identity, so these
must stay together in later train/test partitions. No model is trained here.

This remains numeric, with fixed topology and authored preservation premises.
The next useful experiment is to publish local ordinal/relative ports and
separate allowable metric completions, then ask a receiver to choose or learn
among them. That requires explicit support for alternative completions and
their remaining uncertainty. It should not silently recover omitted detail
from this checkpoint's reference audit.

## Reproduce the checkpoint

From the repository root:

```powershell
python experiments/quickDraw/tools/construction_resolution.py --output D:\_quickDraw\gojo-construction-resolution
dotnet run --project experiments/quickDraw -- --construct --input D:\_quickDraw\gojo-construction-resolution\programs.ndjson --output D:\_quickDraw\gojo-construction-resolution\native
python experiments/quickDraw/tools/verify_construction_resolution.py --root D:\_quickDraw\gojo-construction-resolution
python experiments/quickDraw/tools/inspect_construction_ladders.py --input D:\_quickDraw\gojo-construction-resolution\native --resolution-manifest D:\_quickDraw\gojo-construction-resolution\resolution-manifest.json --default-scene church-series --output D:\_quickDraw\gojo-construction-resolution\review.html
python experiments/quickDraw/tools/inspect_construction_ladders.py --input D:\_quickDraw\gojo-construction-resolution\native --resolution-manifest D:\_quickDraw\gojo-construction-resolution\resolution-manifest.json --default-scene church-series --portable --output D:\_quickDraw\gojo-construction-resolution\review\index.html
python -m unittest discover -s experiments/quickDraw/tools -p "test_*.py"
dotnet test Gojo.slnx
$env:GOJO_CONSTRUCTION_REVIEW='D:\_quickDraw\gojo-construction-resolution\review'
node experiments/quickDraw/tools/check_construction_resolution.cjs
```

Set `GOJO_PLAYWRIGHT_MODULE` and `GOJO_BROWSER` to the installed Playwright
package and browser executable where needed. The portable build and browser
checker do not publish anything.

Retained evidence: [native/source audit](results/construction-resolution/resolution-verification.json),
[resolution manifest](results/construction-resolution/resolution-manifest.json),
[browser report](results/construction-resolution/browser-summary.json),
[recipe inputs](results/construction-resolution/programs.ndjson.gz),
[transitions](results/construction-resolution/transitions.ndjson.gz), and
[checkpoint summary](results/construction-resolution/checkpoint-summary.json).
Full execution/witness archives remain in the authorized Quick Draw folder.

# The church and alphabet construction series

Checkpoint: 24 October 2026. This extends the
[construction ladder pilot](ConstructionLadders.md) to every church and capital
letter in the existing [line-art study](LineArt.md). It is a local milestone
checkpoint, retained in GitHub. Website publication is deferred until the end
of the milestone.

The next [positional-resolution checkpoint](ConstructionResolution.md) now
executes all 41 selections at five numeric grades while retaining their
construction steps and selected structural relationships. This document and
its retained review remain the exact-source baseline.

The local inspector is `D:\_quickDraw\gojo-construction-series\review.html`.
The repository also retains a [portable review folder](results/construction-series/review/index.html)
with the displayed native reads and compressed executable programs.

| New selection | Programs | Rungs | Final source spans |
| --- | ---: | ---: | ---: |
| All twelve authored churches and both licensed church icons | 14 | 81 | 383 |
| Individual capitals A–Z | 26 | 104 | 100 |
| The alphabet on one sheet, in its original arrangement | 1 | 4 | 100 |
| New total | 41 | 189 | 583 |

The atlas repeats the individual-letter source selection; its hundred spans
are not another hundred independent observations. The combined review includes
the twelve pilot variants as well: seven display families, 53 programs and 245
rungs. These are clean authored construction controls, not Quick Draw training
results or inferred human drawing sequences.

## Small engines retain their construction

The shared authoring helpers emit ordinary native site and curve relations:

```text
earlier diagonal frame / exposed primitive output
  -> affine reads with selected integer grades
  -> actual corner, tip, center and handle sites
  -> ordered lines or finite curves sharing those sites
  -> exposed frame read for the next construction
```

The rectangle engine takes opposite corners, derives the other two by composing
their coordinate reads, and joins the four sites in a closed order. The triangle
engine takes an earlier frame and an apex fraction, derives its base and apex,
and closes three ordered edges. Both upward and downward triangles use the
same construction. The pilot roof now uses this shared triangle helper too.

The oval engine emits four finite cubic profiles with shared tips. The cross
engine emits four arms with one actual shared center. They expose their next
working frame through reads of the produced tips or arm endpoints. Triangle
frames likewise expose reads of their actual vertices. Later construction thus
consumes produced geometry, rather than merely sharing unrelated input values.

These helpers are authoring conveniences, not native operation tags. Removing
their Python names leaves the affine references, identity, ordered controls,
closure and subsequent consumption in the native program. Finite ovals remain
Bézier approximations; their names do not establish analytic circularity.

## Rough guides are positioning proposals

The churches use five or six passes: overlapping rough masses, silhouette
chords, first opening rectangles, refined profiles, repeated occurrences where
declared, and subordinate details. A dome can begin with an oval inside a tall
rectangular mass. Tower rectangles extend through roofs. An apse guide extends
beyond the final wall. No containment condition is added to these rough masses.

The source's declared window repetitions consume the actual earlier controls
through a common translated frame. Thirteen declared repeat scopes across the
new church set remain executable native repetitions. Distinct occurrences keep
distinct identities; a target cannot silently weld two source endpoints merely
because their coordinates coincide. Imported icons retain independent sites
where their source subpaths meet visually without an explicit shared endpoint.

The capitals use four passes: primitive positioning guides, an approximate path
network, shared junctions and principal paths, then final profiles and bars or
tails. A and V begin with triangles; C/G/O/Q use ovals; T/X/Y use cross guides.
S uses two rough oval lobes. Other letters use rectangular working frames.

Coarse letter vertices deliberately differ from their final positions. A later
endpoint uses its actual earlier coarse vertex as the coordinate origin, plus
an explicit measured correction through the local frame. Curves then consume
those earlier endpoint identities. For simple line-only letters, the third pass
shows small shared-site locator crosses before the final spans are added.

Every final source edge in the new set has an ancestry path to actual control
sites from earlier visible construction. Guide hiding and fading affect only
the view. The guides and the references they supply remain in the native graph.

## Source fidelity and later compression

The present measured calibration reproduces all final source coordinates,
Bézier controls and endpoint identities exactly. This supplies a controlled
baseline before lowering resolution. Numeric grades remain in the construction;
selecting a coarse rung reduces detail but does not yet provide a nonnumeric
positional read.

Each recipe's `sourceMapping` separately records the original scene hash,
selected source edge order, original stroke/subpath scopes, and site/edge
correspondence. Independent verification reloads the original drawings rather
than treating the mapping's expected coordinates as an oracle. Native execution
does not consult this mapping. Its geometry comes from the connected program.

Original source order and the new proposed construction order have separate
provenance. Neither order is presented as recorded human pen timing. The two
imported SVGs, their original licenses, revision IDs and byte hashes are pinned
under [sources/line-art](sources/line-art/manifest.json), making reproduction
independent of a fresh network fetch.

The combined transition set has 192 ordered examples. Each input contains only
the sites, parameters and instructions already introduced; the next instruction
and native target are separate fields. Two inherited pilot targets retain their
explicit failed-condition outcome. No model is trained in this checkpoint.
The transition provenance groups the atlas and its individual letters under
their common original source, so later train/test splits can keep them together
rather than treating them as independent data. This source grouping is supplied
as a split instruction; no training split has been run.

The next milestone can lower the published positional resolution while retaining
the same coarse structure and correspondence, then compare alternative
completions. That experiment must distinguish omitted detail, retained numeric
residue and deliberately discarded precision. The current exact source is a
calibration baseline, not permission for a future receiver to consult hidden
fine coordinates.

## Reproduction and verification

The checkpoint passes 5,374 .NET tests across twelve projects and 113 Python
tests. The independent source audit checks 117,416 assertions across the 41
conversions; the combined causal audit checks 33,296 assertions. Local browser
checks cover all 53 programs, 245 rungs and 6,708 rendered path reads, including
guide controls, dependency selection, mobile layout and all native downloads.

From the repository root:

```powershell
python experiments/quickDraw/tools/construction_recipes.py --series all --output D:\_quickDraw\gojo-construction-series
dotnet run --project experiments/quickDraw -- --construct --input D:\_quickDraw\gojo-construction-series\programs.ndjson --output D:\_quickDraw\gojo-construction-series\native
python experiments/quickDraw/tools/verify_construction_ladders.py --root D:\_quickDraw\gojo-construction-series
python experiments/quickDraw/tools/verify_construction_series.py --root D:\_quickDraw\gojo-construction-series
python experiments/quickDraw/tools/inspect_construction_ladders.py --input D:\_quickDraw\gojo-construction-series\native --default-scene church-series --output D:\_quickDraw\gojo-construction-series\review.html
python experiments/quickDraw/tools/inspect_construction_ladders.py --input D:\_quickDraw\gojo-construction-series\native --default-scene church-series --portable --output D:\_quickDraw\gojo-construction-series\review\index.html
python -m unittest discover -s experiments/quickDraw/tools -p "test_*.py"
dotnet test Gojo.slnx
```

`--series line-art` selects just the 41 conversions. The default `--series pilot`
continues to select the original twelve programs. `--portable` prepares files;
it performs no deployment. The ordinary local review links directly to the
executable program JSON, while the portable folder includes compressed copies.

The local browser checker serves only the selected review folder on loopback:

```powershell
$env:GOJO_CONSTRUCTION_REVIEW='D:\_quickDraw\gojo-construction-series\review'
node experiments/quickDraw/tools/check_construction_series.cjs
```

Set `GOJO_PLAYWRIGHT_MODULE` and `GOJO_BROWSER` to the available Playwright
package and browser executable if they are not supplied by the environment.

Retained evidence includes the [independent source audit](results/construction-series/series-verification.json),
[causal transition audit](results/construction-series/verification.json),
[browser report](results/construction-series/browser-summary.json),
[compressed recipe inputs](results/construction-series/programs.ndjson.gz),
[compressed transitions](results/construction-series/transitions.ndjson.gz),
and [checkpoint summary](results/construction-series/checkpoint-summary.json).
Full execution/witness archives and uncompressed data remain in the authorized
Quick Draw folder.

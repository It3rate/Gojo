# Ornament collections

The landing page shows all published studies as visual tiles. The `studies`
array in `tools/ornament-collections.json` lists the smaller interactive studies
first, with symmetry actions and the centre-line atlas at the top. The existing
`collections` follow. Preview tiles crop the viewports of recorded SVGs without
changing their geometry; generated thumbnails live in `previews/`.

`/ornaments/symmetry-actions/` turns selected pattern correspondences into
reusable native word substitutions, displacements and traversal actions.
It includes overlapping A2/B6 boundary reads, 240 captured action applications,
a bounded recurrence audit across the eighteen atlas patterns, and controls
separating geometric agreement from shared-site correspondence. The four-bit
view is a derived local codec; the connected action and its receipts retain
meaning. Settlement and original-engine inference remain outside this pass.
Its canonical prose is `experiments/QualitativeFrieze/SymmetryActionStudy.md`.
Run `--symmetry-actions`, then build with `Composition/SymmetryActionPage.py`.

`/ornaments/centreline/` breaks down all eighteen wholly straight-edged rows
from the three supplied reference sheets. It retains component selections,
native route reads, checked pair correspondences, alternative construction
hypotheses and an explicit audit of the ten curved or mixed rows.
Its canonical prose is `experiments/QualitativeFrieze/CentrelinePatternStudy.md`.
Run `--centreline-patterns` in the QualitativeFrieze project, then build the
page with `Composition/CentrelinePatternPage.py`. The supplied source sheets
are shown intact for comparison; generated centre lines are authored
idealizations, not automatic image traces or recovered construction histories.

`/ornaments/weave/` follows an authored angular braid through continuous
strands, local front/back order and shallow depth samples. It also compares
candidate continuations from exposed fragments with actual shared-site closure.
Its canonical prose is `experiments/QualitativeFrieze/AngularWeaveStudy.md`.
The `--angular-weave` experiment records the source report, and
`Composition/AngularWeavePage.py` builds the interactive page and Markdown
download. The study uses the existing qualitative structural grammar.

`/ornaments/signatures/` is the illustrated structural-reading study. It compares
six recorded line courses, six controlled changes and 18 separate signature
fields. Its window inspector exposes every retained bounded match, and its
source list records the selected and deferred rows of the supplied reference
sheets. The public figures are executed reconstructions.

The canonical prose is `experiments/OrnamentFlowPilot/PatternStudy.md`.
Regenerate its data and static page using the commands at the end of that
document. The public page includes an HTML narrative, the Markdown download,
the exact report and the associated SVG figures. `build-ornaments.mjs` retains
its entry link, and the shared deployment preparation includes its directory.

The [ornament workshop](https://gojo.inklingai.ca/encoding/explorer/?layer=37) is a live pen press with five line plates. Its [plate book](../../../experiments/PrimitiveMeasure/OrnamentWorkshop.md) includes the SVG impressions and construction notes.

`/ornaments/interior-ornament/` presents twelve compound interior designs:
flowers, fans and scrolls within woven, bowed and interlocking meshes. The
bounded edition includes all twelve final SVGs, all twelve even-outline mesh
views, an inspector, an all-design sheet, captured summaries and the study
notes. Earlier comparisons, guide overlays, full display-path records and native
archives remain in the linked research checkout. The adapter in
`tools/interior-ornament-publication.mjs` only projects the captured presentation;
it does not execute geometry or alter the SVGs. Its source is
`experiments/QualitativeFrieze/results/ornament-interior`.

`/ornaments/lattice-interiors/` adds all 100 recorded interior-first lattices.
Its bounded website edition retains the 100 coloured and 100 outline SVGs,
captured cell and repeat summaries, family/search controls, local ratings with
fingerprint-checked export/import, an all-design overview, and four rejected
interior examples. Construction stages, full diagnostics and native archives
remain in the research checkout; the publication scope states this explicitly.
The adapter in `tools/interiors-publication.mjs` projects captured facts and
does not execute geometry or alter any SVG. The complete research gallery is
at `experiments/QualitativeFrieze/results/lattice-interiors/gallery.html`.

Public entry: `/ornaments/`. The Gojo homepage links here; each collection has
a permanent child route. `/ornaments/bourgoin/` retains 36 source
comparisons, construction views, earlier SVG interpretations, native packets
and a whole-atlas view. Its linked program example is included under
`/ornaments/bourgoin/program/`.

`/ornaments/flowing-grammar/` presents the refined collection: 36 retained
skeletons with overall, intermediate and detail designs (108 designs). Its
website edition includes every current full-field SVG and every executed-unit
SVG, the all-design overview, the interior selection, the conceptual-complexity
review and diagnostic records. Source comparisons link to the existing Bourgoin
collection. Colour and geometry come from the recorded research outputs.

The website edition is deliberately bounded to fit the shared hosting quota.
Alternate chord/construction controls, native archives, large combined exports
and older review images remain in the research checkout; those controls and
links are removed together from the web edition, with an explicit scope note.
The research galleries are unchanged. Nothing is silently substituted for an
unpublished view.

## Add a collection

1. Finish and retain the study in its experiment folder.
2. Add an entry to [ornament-collections.json](../tools/ornament-collections.json).
   Array order is the index order. Choose a descriptive, stable `id`; do not
   renumber existing routes when more studies arrive. Give the source directory,
   gallery filename, catalogue (`Entries`, or the configured `countProperty`,
   supplies the displayed count), short
   description and actual SVG/image previews. Declare linked companion folders
   or documents explicitly; unpublished local dependencies fail the build.
   A bounded presentation may declare `includeFiles` and `omitLinks` regular
   expressions. `refined-compact` retains mode and executed-unit controls while
   removing the omitted curve/construction controls and links. Every omitted
   target must match an explicit rule. Other unpublished dependencies still
   fail the build. Links into another selected collection share its stable URL.
3. Rebuild and check:

~~~powershell
node Visualizations/Gojo.Demos.Web/tools/build-ornaments.mjs
node Visualizations/Gojo.Demos.Web/tools/build-ornaments.mjs --check
node Visualizations/Gojo.Demos.Web/tools/check-ornaments.cjs
~~~

Use `build-ornaments.mjs --dry-run` to inspect collection sizes without writing
generated files. Check the complete prepared production bundle against the
site's allowance before deployment, including the existing applications.

The builder copies the selected recorded files, rewrites local HTML links to
absolute public paths and adds return navigation. It does not run the kernel,
change a construction or regenerate research results. Galleries keep their
controls. Text files retain their content with LF newlines for consistent
publication across platforms; images are copied byte for byte. `publication.json`
records source paths and SHA-256 hashes after this newline normalization, before
HTML navigation and link adaptation. Native JSON data and SVG geometry are not
recomputed.
`gallery.html` remains a working alias for the collection's directory index.
Its small redirect preserves the query and fragment, including selected cases,
instead of duplicating the potentially large complete gallery HTML. Without
JavaScript, its collection link remains usable.
An explicit base URL preserves relative image URLs constructed by the original
gallery controls, including at the directory's no-trailing-slash alias.
Study notes are retained as Markdown downloads.

Edit the original study to revise a gallery, then rebuild. Generated collection
files and `index.html` are committed for static hosting. `ornaments.css` and
this README are authored here. Do not copy unrelated ongoing experiments into
a collection just because their folders exist.

## Preview and publish

The browser check uses Playwright, with `GOJO_PLAYWRIGHT_MODULE` and
`GOJO_BROWSER` overrides as in the syntax notebook. It checks navigation,
dependencies, retained artifact hashes, controls and desktop/phone layouts.
Set `GOJO_BASE_URL=https://gojo.inklingai.ca` to check the public site.
Set `GOJO_WEB_ROOT` to the prepared deployment directory to serve and check
the complete local bundle, including linked browser applications.

Use the [shared site preparation and deployment](../README.md). The complete
bundle retains the existing browser applications, syntax and Minds sections.
`/ornaments/*` is excluded from the homepage fallback, so a missing download
returns a real 404. Future collections only need a catalogue entry and rebuild;
the homepage link and deployment overlay remain the same.

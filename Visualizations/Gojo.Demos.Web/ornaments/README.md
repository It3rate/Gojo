# Ornament collections

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

Use the [shared site preparation and deployment](../README.md). The complete
bundle retains the existing browser applications, syntax and Minds sections.
`/ornaments/*` is excluded from the homepage fallback, so a missing download
returns a real 404. Future collections only need a catalogue entry and rebuild;
the homepage link and deployment overlay remain the same.

# Ornament collections

Public entry: `/ornaments/`. The Gojo homepage links here; each collection has
a permanent child route. The first is `/ornaments/bourgoin/`, with 36 source
comparisons, construction views, earlier SVG interpretations, native packets
and a whole-atlas view. Its linked program example is included under
`/ornaments/bourgoin/program/`.

## Add a collection

1. Finish and retain the study in its experiment folder.
2. Add an entry to [ornament-collections.json](../tools/ornament-collections.json).
   Array order is the index order. Choose a descriptive, stable `id`; do not
   renumber existing routes when more studies arrive. Give the source directory,
   gallery filename, catalogue (`Entries` supplies the displayed count), short
   description and actual SVG/image previews. Declare linked companion folders
   or documents explicitly; unpublished local dependencies fail the build.
3. Rebuild and check:

~~~powershell
node Visualizations/Gojo.Demos.Web/tools/build-ornaments.mjs
node Visualizations/Gojo.Demos.Web/tools/build-ornaments.mjs --check
node Visualizations/Gojo.Demos.Web/tools/check-ornaments.cjs
~~~

The builder copies the selected recorded files, rewrites local HTML links to
absolute public paths and adds return navigation. It does not run the kernel,
change a construction or regenerate research results. Galleries keep their
controls. Text files retain their content with LF newlines for consistent
publication across platforms; images are copied byte for byte. `publication.json`
records source paths and SHA-256 hashes after this newline normalization, before
HTML navigation and link adaptation. Native JSON data and SVG geometry are not
recomputed.
`gallery.html` remains a working alias for the collection's directory index.
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

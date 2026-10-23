# Gojo Demos Web Directory

Static landing page for `https://gojo.inklingai.ca/`.

The production bundle mounts the browser applications beneath this page:

- `/quickdraw/` — drawing studies, starting with capital letters and church
  drawings at `/quickdraw/line-art/`;
- `/ornaments/` — ornament collections, source comparisons and native downloads;
  start with the [collection publishing guide](ornaments/README.md)
- `/minds/` — captured Gojo experiments, step viewer, research notes and the
  experiment/development roadmap
- `/syntax/` — illustrated syntax notebook, with thumbnails, stepped examples
  and actual fixture source dialogs
- `/blocks/` — Gojo Blocks
- `/gears/` — Gojo Gear Demo
- `/gearLadder/` — Gojo Gear Shape Ladder
- `/sketch/` — GojoCAD SketchViewer
- `/core4/` — frozen archive of the earlier Core4, Core2 Patterns and Foom
  Numbers browser demos

Deploy this landing page, the four current application directories and the
Core4 archive as one Static Web Apps bundle using
`GOJO_SWA_CLI_DEPLOYMENT_TOKEN`.

The [syntax notebook](syntax/README.md) is a static website. Rebuild it using
the script below; no browser-application build is needed for an editorial
change. Run the fixture and browser checks documented there before publication.

~~~powershell
node Visualizations/Gojo.Demos.Web/tools/build-syntax.mjs
~~~

Rebuild Minds from the captured results and its roadmap source:

~~~powershell
node Visualizations/Gojo.Demos.Web/tools/build-minds.mjs
node Visualizations/Gojo.Demos.Web/tools/build-minds.mjs --check
~~~

This does not rerun experiments. To record another run, use the command in
`experiments/Minds/README.md` first. The roadmap's single prose source is
`experiments/Minds/docs/ExperimentRoadmap.json`; the build writes both its Markdown document
and the public HTML. The notes and viewer are adapted from the original Minds
artifacts. All five recorded conditions remain selectable at
`/minds/two-space/?run=half` (also `full`, `quarter`, `fresh`, `reference`).

Experiment 02 is at `/minds/feedback-limits/`. The selected dated result directory
is explicit in `experiments/Minds/results/feedback-limits/publication.json`. The build copies
all case fixtures, event tapes and both implementations' records for every
repetition, then adds the findings and protocol pages. It never selects only
the cases that agreed or silently publishes the newest run.

Experiment 03 is at `/minds/predictions/`. Its explicit selection is
`experiments/Minds/results/predictions/publication.json`; every case, before/after event
journal, reference calculation and missing outcome is published. The Minds
landing page links to each completed experiment with a short description.

Production deployment must include all four current browser applications and
the Core4 archive. Publishing any one directory alone would remove other
routes. The ignored publish-deploy-root directory is a cached bundle, not a
reproducible source checkout. Its runtime manifests can differ from the live
deployment even when the entry-page HTML matches. The Core4 runtime payloads
are intentionally kept frozen in that cache; the tracked `/core4/` index is
overlaid during preparation and the preparation step fails if the archive is
missing.

Prepare the syntax addition from the current public application manifests:

~~~powershell
node Visualizations/Gojo.Demos.Web/tools/prepare-syntax-deploy.mjs
~~~

This builds publish-deploy/syntax-site, retains the live browser loaders and
their resource hashes, fetches missing or changed assets, verifies compressed
counterparts, and overlays the syntax directory, home page and site config.
It excludes obsolete fingerprinted WASM files absent from the current runtime
manifests from the staged bundle, keeping the cache and frozen Core4 archive
intact. This prevents old application versions from consuming the hosting quota.
It also overlays Minds and the ornament collections, so subsequent publications
retain `/minds/` and `/ornaments/`.
It retains the Quick Draw study category and its selected downloads at `/quickdraw/`.
It writes a backup and a public-asset hash manifest to the OS temporary folder.
The cached smooth, performance and six-rung variants were not deployed when
this gallery was added; their runtime endpoints return 404. The preparation
script checks these endpoints and stops if an additional live route needs
to be included. A home-page fallback is not evidence that an application exists.

After the full bundle has been assembled, use the existing deployment token
through the CLI environment (never print it or put it on the command line):

~~~powershell
$env:SWA_CLI_DEPLOYMENT_TOKEN = $env:GOJO_SWA_CLI_DEPLOYMENT_TOKEN
npx --yes @azure/static-web-apps-cli deploy Visualizations/Gojo.Demos.Web/publish-deploy/syntax-site `
  --swa-config-location Visualizations/Gojo.Demos.Web/publish-deploy/syntax-site `
  --env production --app-name gojo --resource-group inkling --no-use-keychain
~~~

The preparation script performs the local assembly; the deploy command above
publishes it. Verify the live gallery, prior application entry points and
preserved manifest hashes after publication. /syntax/* is excluded from the
root fallback so a missing example or source file does not masquerade as the
home page.

The same exclusion applies to `/minds/*`. Verify the Minds index, roadmap,
five viewer selections and downloadable observations after publishing.

## Quick Draw studies

The `/quickdraw/` category links to studies through the ordered catalogue in
`tools/quickdraw-studies.json`. Each entry has a stable child route, title,
description and explicitly prepared public source directory. Add further
studies to that catalogue without changing existing URLs or the homepage link.

The alphabet and church inspector starts the category at `/quickdraw/line-art/`.
Its source is `experiments/quickDraw/results/line-art/public`. That directory's
`public-files.json` selection lists its entry page and native downloads;
unselected research files are not copied to the site.

The construction study at `/quickdraw/construction/` uses
`experiments/quickDraw/results/construction/public`. It contains four authored
families, twelve parameterized runs and 56 rungs, with every compact executable
program available for download. The larger execution/witness archives remain in
the research data folder. The inspector's embedded gzip payload contains only
captured native reads; the browser neither reconstructs recipes nor invents
dependencies. Use `tools/check-construction.cjs` for its rung, guide, reference,
condition and download checks. Its `restored-native-construction` publication
contract verifies restored/replayed flags and selected archive links before copying.

~~~powershell
node Visualizations/Gojo.Demos.Web/tools/build-quickdraw.mjs
node Visualizations/Gojo.Demos.Web/tools/build-quickdraw.mjs --check
node Visualizations/Gojo.Demos.Web/tools/check-quickdraw.cjs
~~~

Use `--dry-run` to validate the selection and report its size without writing.
The builder checks restored-scene flags, local-path leakage, archive links and
gzip integrity, then records source and published hashes in `publication.json`.
Its only study HTML adaptation is a `/quickdraw/<id>/` base URL so the route works with or
without a trailing slash. Geometry and native archives are retained unchanged.
Standalone `.json.gz` downloads do not require an uncompressed counterpart.
The inspector's download button saves decoded `.gojo.json` bytes, handling both
ordinary gzip files and Azure's automatic HTTP gzip decoding. Browser checks
verify the actual saved filename and contents as well as all stored archives.
The optional `readSurface: "restored-native-inspector"` catalogue entry enables
native scene/archive checks; future static studies can use their own captured
presentation without pretending to contain this inspector format. The browser
check uses the existing `GOJO_PLAYWRIGHT_MODULE`, `GOJO_BROWSER` and
`GOJO_BASE_URL` overrides to verify local or production navigation, alphabet
reads, archive hashes and phone layout.
Set `GOJO_WEB_ROOT` to the prepared bundle directory to check the assembled
deployment locally before uploading it.

Run the shared preparation and production deployment above after the inspector
checks pass. Verify the alphabet, hover reads and native downloads on the live
route, along with the previous application routes. `/quickdraw/*` is excluded
from the homepage fallback so a missing download returns a real 404.

The 23 October 2026 alphabet publication has 21 category files (912,114 bytes).
Its complete production bundle is 261,317,702 bytes, leaving 826,298 bytes under
the last verified 262,144,000-byte allowance. Include future studies in the full
bundle measurement before publication.

Adding the construction study brings the complete prepared bundle to
261,718,196 bytes, leaving 425,804 bytes under that allowance. The category has
two studies, with all 29 selected native downloads retained. The captured
construction read is compressed inside its standalone HTML; each native
program is stored once as gzip rather than publishing the larger execution
witnesses as well.

## Growing the ornament series

The collection catalogue and permanent `/ornaments/<id>/` routes let new
studies join the index without changing old links or the homepage. Keep each
study's captured results separate, and publish only selected completed studies.
See [the authoring guide](ornaments/README.md) for the entry format and checks.

On 19 October 2026, Azure reported a 262,144,000-byte (250 MiB) content limit
for this site. The complete accepted bundle was 241,884,697 bytes after removing
obsolete cached runtime assemblies, leaving about 20 MB. This is a hosting
constraint, separate from the collection layout. Before publishing a large
series, measure the whole bundle and either increase the site's allowance or
move large image, SVG and native-packet downloads to dedicated asset storage.
Keep the collection URLs stable and update the publication adapter and integrity
checks to use the chosen asset location. No hosting-plan change was made for
the first collection.

The refined collection is published at `/ornaments/flowing-grammar/`, with all
108 main designs and 108 executed-unit SVGs. Its compact publication adapter
retains mode and unit selection and omits unavailable archive/history controls
together with their links. On 19 October 2026 the complete prepared bundle
with this addition was 257,403,979 bytes, within the 262,144,000-byte allowance.
The existing applications, frozen Core4 archive, Minds, syntax and original
Bourgoin collection remain in the same deployment.

The interior-first lattice collection is added at `/ornaments/lattice-interiors/`.
Its compact publication adapter includes all 100 final coloured and outline
views, captured facts, ratings, overview and rejected examples, while retaining
the larger construction/native archives in the research checkout. The selected
collection adds about 3.9 MB. The complete production bundle must still be
measured on each publication; its remaining capacity is now below 1 MB under
the last verified allowance. No hosting-plan change is needed for this edition.

The twelve compound interior ornament designs join the catalogue at
`/ornaments/interior-ornament/`, with a finished-design sheet and a retained-mesh
view for every design. This bounded publication keeps large history and native
archives in the linked research checkout. Existing `gallery.html` aliases now
redirect to their canonical collection pages while preserving query and fragment;
this avoids storing duplicate large gallery pages. All previous collection
routes, downloads and browser applications remain included. Measure the whole
prepared bundle before every deployment; the documented 250 MiB allowance still
applies.

The complete prepared bundle for this publication is 260,405,197 bytes,
leaving 1,738,803 bytes under that allowance. The alias reduction retains the
previous URLs and makes this edition smaller than the preceding deployment.

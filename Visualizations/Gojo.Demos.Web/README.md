# Gojo Demos Web Directory

Static landing page for `https://gojo.inklingai.ca/`.

The production bundle mounts the browser applications beneath this page:

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

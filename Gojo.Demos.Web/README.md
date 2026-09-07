# Gojo Demos Web Directory

Static landing page for `https://gojo.inklingai.ca/`.

The production bundle mounts the browser applications beneath this page:

- `/syntax/` — illustrated syntax notebook, with thumbnails, stepped examples
  and actual fixture source dialogs
- `/blocks/` — Gojo Blocks
- `/gears/` — Gojo Gear Demo
- `/gearLadder/` — Gojo Gear Shape Ladder
- `/sketch/` — GojoCAD SketchViewer

Deploy this landing page and the four application directories as one Static
Web Apps bundle using `GOJO_SWA_CLI_DEPLOYMENT_TOKEN`.

The [syntax notebook](syntax/README.md) is a static website. Rebuild it using
the script below; no browser-application build is needed for an editorial
change. Run the fixture and browser checks documented there before publication.

~~~powershell
node Gojo.Demos.Web/tools/build-syntax.mjs
~~~

Production deployment must include all four current browser applications.
Publishing the syntax directory alone would remove other routes. The ignored
publish-deploy-root directory is a cached bundle, not a reproducible source
checkout. Its runtime manifests can differ from the live deployment even when
the entry-page HTML matches.

Prepare the syntax addition from the current public application manifests:

~~~powershell
node Gojo.Demos.Web/tools/prepare-syntax-deploy.mjs
~~~

This builds publish-deploy/syntax-site, retains the live browser loaders and
their resource hashes, fetches missing or changed assets, verifies compressed
counterparts, and overlays the syntax directory, home page and site config.
It writes a backup and a public-asset hash manifest to the OS temporary folder.
The cached smooth, performance and six-rung variants were not deployed when
this gallery was added; their runtime endpoints return 404. The preparation
script checks these endpoints and stops if an additional live route needs
to be included. A home-page fallback is not evidence that an application exists.

After the full bundle has been assembled, use the existing deployment token
through the CLI environment (never print it or put it on the command line):

~~~powershell
$env:SWA_CLI_DEPLOYMENT_TOKEN = $env:GOJO_SWA_CLI_DEPLOYMENT_TOKEN
npx --yes @azure/static-web-apps-cli deploy Gojo.Demos.Web/publish-deploy/syntax-site `
  --swa-config-location Gojo.Demos.Web/publish-deploy/syntax-site `
  --env production --app-name gojo --resource-group inkling --no-use-keychain
~~~

The preparation script performs the local assembly; the deploy command above
publishes it. Verify the live gallery, prior application entry points and
preserved manifest hashes after publication. /syntax/* is excluded from the
root fallback so a missing example or source file does not masquerade as the
home page.

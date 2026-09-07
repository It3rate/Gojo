# Gojo syntax notebook

Public entry point: [gojo.inklingai.ca/syntax/](https://gojo.inklingai.ca/syntax/).

Twelve small illustrated experiments cover shared sites, parameter frames,
partial coordinates, Bézier construction and replay, join continuity, frieze
maps, bounded quantities, clearance, material refinement, local response and
unit standards. Each has a thumbnail, everyday context, selectable steps, a
syntax dialog and links to the exact fixture tests and supporting documentation.

The browser presents selected, recorded scenarios. It does **not** execute the
Gojo kernel or interpret the syntax. SVG coordinates, decimal display values,
navigation and editorial data belong to this presentation boundary. The
fixture's Gojo structure and selected kernel readers retain the meaning.

## Authoring and rebuilding

- Edit [the catalogue](../tools/syntax-catalog.json) for explanations, selected
  display values and source references.
- Edit [illustrations.js](illustrations.js), [gallery.js](gallery.js) or
  [gallery.css](gallery.css) for presentation and interactions.
- Edit canonical syntax under
  [Gojo.Encoding.Tests/Fixtures/Syntax](../../Gojo.Encoding.Tests/Fixtures/Syntax)
  when the actual experiment needs to change. The gallery's fixture copies
  are generated, never an independent syntax source.
- Rebuild committed HTML pages, SVG thumbnails, fixture copies and data.js:

~~~powershell
node Gojo.Demos.Web/tools/build-syntax.mjs
node Gojo.Demos.Web/tools/build-syntax.mjs --check
dotnet test Gojo.slnx
~~~

The builder has no package dependencies. Serve Gojo.Demos.Web as a static
directory to preview /syntax/; pages use ordinary directory index routes.
Each step can be linked directly using ?step=0, ?step=1, and so on.

[GojoSyntaxGalleryTests](../../Gojo.Encoding.Tests/GojoSyntaxGalleryTests.cs)
compares published numeric data against the selected reads, before and after
numeric transport, and checks copied source text against the canonical fixtures.
The original fixture suites cover stronger structural and unresolved cases:
identity, correspondence, domain refusal, freshness, residue and retained history.

The browser check uses Playwright with a local-only temporary HTTP server:

~~~powershell
node Gojo.Demos.Web/tools/check-syntax.cjs
~~~

Set GOJO_PLAYWRIGHT_MODULE to a bundled Playwright package directory if it is
not locally resolvable, and GOJO_BROWSER to an installed Edge/Chromium binary
if needed. GOJO_SCREENSHOTS selects an output directory; the default is
GojoSyntaxGalleryQA in the OS temporary directory. Checks cover every step,
desktop and phone layouts, filters, URL history, dialog focus, source text,
copying and asset availability. Visually inspect representative screenshots
as well; overflow checks alone cannot assess the illustrations.

## Selecting future examples

Start with a small, concrete question and a real syntax fixture. Prefer an
example where a few observable stages reveal a distinction: shared versus
equal, unknown versus zero, observation versus transfer, acceptable difference
versus failed equality, or historical versus current evidence. Explain which
action is chosen by the driver and which response the kernel actually supplies.
Preserve useful partial states instead of only showing the completed scalar.

For this initial collection, carrier-native circle/line promises, 3D bases,
lane access, conserved capacity sharing, denominator domains and deliberate
history compression were considered for later pages. Some have existing tests;
others need a tighter fixture or additional laws. The twelve selected pages
give a compact introduction without representing this inventory as exhaustive.

Deployment is described in [the site README](../README.md). Keep the existing
browser applications in the deployment bundle when adding or updating /syntax/.

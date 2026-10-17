# Gojo syntax notebook

Public entry point: [gojo.inklingai.ca/syntax/](https://gojo.inklingai.ca/syntax/).

Eighteen illustrated experiments have selectable steps, syntax panels and links
to their source fixtures and documentation. Six featured examples lead from
contour construction through calibrated rotation, a grounded affine basis,
Bézier construction, frieze patterns and curve replay. Twelve further examples
in geometry, identity, uncertainty, units and response are in the compact Extras section.

Seven complete walkthroughs are published from the original pages in
[Gojo/docs/examples](../../../Gojo/docs/examples). The original compact curve-replay
overview stays at `/syntax/curve-replay/`; its full walkthrough is at
`/syntax/curve-replay/walkthrough/`. The featured card opens the full walkthrough.

The browser presents selected, recorded scenarios. It does **not** execute the
Gojo kernel or interpret the syntax. SVG coordinates, decimal display values,
navigation and editorial data belong to this presentation boundary. The
fixture's Gojo structure and selected kernel readers retain the meaning.

## Authoring and rebuilding

Use short, descriptive titles that name the subject or operation, such as
"Circle construction" or "Bézier curve replay". Keep headings factual and avoid
promotional slogans, rhetorical questions and attempts to draw the reader in.

- Edit [the catalogue](../tools/syntax-catalog.json) for explanations, selected
  display values and source references.
- Edit [the collection](../tools/syntax-walkthroughs.json) for featured order,
  walkthrough titles, source pages and thumbnail stages. Edit the original
  walkthrough HTML for its behavior. Publishing rewrites repository links,
  adds notebook navigation and supplies a syntax panel when one is absent;
  it does not fork the original renderer or numeric trace.
- Edit [illustrations.js](illustrations.js), [gallery.js](gallery.js) or
  [gallery.css](gallery.css) for presentation and interactions.
- Edit canonical syntax under
  [Tests/Gojo.Encoding.Tests/Fixtures/Syntax](../../../Tests/Gojo.Encoding.Tests/Fixtures/Syntax)
  when the actual experiment needs to change. The gallery's fixture copies
  are generated, never an independent syntax source.
- Rebuild committed HTML pages, SVG thumbnails, fixture copies and data.js:

~~~powershell
node Visualizations/Gojo.Demos.Web/tools/build-syntax.mjs
node Visualizations/Gojo.Demos.Web/tools/build-syntax.mjs --check
dotnet test Gojo.slnx
~~~

When a walkthrough illustration changes, refresh its committed SVG thumbnail
from the actual renderer before rebuilding (requires Playwright, with the same
environment options as the browser check below):

~~~powershell
node Visualizations/Gojo.Demos.Web/tools/capture-walkthrough-thumbnails.cjs
node Visualizations/Gojo.Demos.Web/tools/build-syntax.mjs
~~~

The builder has no package dependencies. Serve Gojo.Demos.Web as a static
directory to preview /syntax/; pages use ordinary directory index routes.
Each step can be linked directly using ?step=0, ?step=1, and so on.
Assets and internal gallery links use explicit /syntax/ paths because Azure
can serve a directory index without redirecting a URL to add its trailing slash.
Browser checks cover both forms and explicit index.html URLs for every page.

[GojoSyntaxGalleryTests](../../../Tests/Gojo.Encoding.Tests/GojoSyntaxGalleryTests.cs)
compares published numeric data against the selected reads, before and after
numeric transport, and checks copied source text against the canonical fixtures.
The original fixture suites cover stronger structural and unresolved cases:
identity, correspondence, domain refusal, freshness, residue and retained history.

The browser check uses Playwright with a local-only temporary HTTP server:

~~~powershell
node Visualizations/Gojo.Demos.Web/tools/check-syntax.cjs
~~~

Set GOJO_PLAYWRIGHT_MODULE to a bundled Playwright package directory if it is
not locally resolvable, and GOJO_BROWSER to an installed Edge/Chromium binary
if needed. GOJO_SCREENSHOTS selects an output directory; the default is
GojoSyntaxGalleryQA in the OS temporary directory. Checks cover every step,
desktop and phone layouts, filters, URL history, dialog focus, source text,
copying, the featured order, compact extras, walkthrough controls, the main-page
link and asset availability. Set `GOJO_BASE_URL=https://gojo.inklingai.ca` to run
the same checks against production after deployment. Visually inspect representative screenshots
as well; overflow checks alone cannot assess the illustrations.

## Selecting future examples

Start with a small, concrete question and a real syntax fixture. Prefer an
example where a few observable stages reveal a distinction: shared versus
equal, unknown versus zero, observation versus transfer, acceptable difference
versus failed equality, or historical versus current evidence. Explain which
action is chosen by the driver and which response the kernel actually supplies.
Preserve useful partial states instead of only showing the completed scalar.

Further candidates include 3D bases, lane access, conserved capacity sharing,
denominator domains and deliberate history compression. Some have existing
tests; others need a tighter fixture or additional laws. The collection is an
introduction rather than an exhaustive inventory.

Deployment is described in [the site README](../README.md). Keep the existing
browser applications in the deployment bundle when adding or updating /syntax/.

# Gojo Primitive Explorer in the browser

The browser target compiles the same explorer and primitive sources as the desktop application. It uses Avalonia WebAssembly, with HTML notes opened in a new tab. No separate arithmetic implementation is introduced.

Publish the complete existing Gojo site, preserving its other applications:

```powershell
dotnet publish Visualizations/Gojo.PrimitiveExplorer.Browser -c Release -o Visualizations/Gojo.PrimitiveExplorer.Browser/publish-browser
python -m pip install markdown==3.8.2
python Visualizations/Gojo.Demos.Web/tools/build-encoding.py
node Visualizations/Gojo.Demos.Web/tools/prepare-syntax-deploy.mjs
```

Use the Azure deployment command in `../Gojo.Demos.Web/README.md`. The prepared bundle includes `/encoding/`, `/encoding/explorer/` and `/encoding/notes/`. Markdown remains the authoring source. The generator follows linked Markdown inside this repository, converts tables and anchors, and preserves explicit external-repository/unavailable-reference labels where a source cannot be published. Generated notes must not be edited by hand.

For browser checks, install Playwright in an isolated tool directory and expose it through `NODE_PATH`, then run `node Visualizations/Gojo.Demos.Web/tools/check-encoding.cjs`. The check uses installed Microsoft Edge, verifies runtime startup and the actual notes button opening a new tab, and captures a screenshot in the OS temporary directory. Set `GOJO_BASE_URL=https://gojo.inklingai.ca` to check production.

The explorer is a desktop-sized inspector; smaller screens can scroll the canvas. The HTML notes use responsive layouts.

Published 1 November 2026 at https://gojo.inklingai.ca/encoding/. Verified the production explorer and notes popup in Edge, all 5,816 Release solution tests, and matching runtime manifests for the seven preserved browser applications/archive entries. The final complete deployment bundle was 231,986,223 bytes. Notes remain authored in Markdown; 59 linked documents are generated as HTML.

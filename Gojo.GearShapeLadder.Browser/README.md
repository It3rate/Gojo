# Gojo Gear Shape Ladder Browser

Avalonia WebAssembly host for the `Gojo.GearShapeLadder` desktop demo.

Live at <https://gojo.inklingai.ca/gearLadder/>.

The browser renderer caches a derived vector projection of each structural
ladder rung. The Gojo and GojoCAD relations remain canonical; only repeated
graph traversal is removed from the animation frame path.

Publish with:

```powershell
dotnet publish .\Gojo.GearShapeLadder.Browser\Gojo.GearShapeLadder.Browser.csproj -c Release -o .\Gojo.GearShapeLadder.Browser\publish-browser -p:UseSharedCompilation=false
```

The published `wwwroot` directory is mounted at `/gearLadder/` in the combined
Gojo Static Web Apps bundle.

# Gojo Gear Shape Ladder

This desktop demo combines the existing gear projection idea with a six-rung
structural shape ladder:

```text
one sized circle
  -> two sized circles
  -> coupled pitch circles + straight developed surface
  -> triangular teeth + triangular repeat
  -> flat-tipped teeth + flat-tipped repeat
  -> sampled involute flanks + sampled involute repeat
```

Every gear outline and lower developed profile is ordinary GojoCAD frame
structure. The six states are carried by `StructuralLadderSurface`,
with explicit focus, transform-application, correspondence, and timeline
evidence between adjacent rungs.

Motion uses the observational gear correspondence already supported by Gojo:

```text
motion source -> driver perimeter       1 : 1
driver perimeter -> driven perimeter    1 : -1
```

The driven angle is an observational projection. This demo does not claim that
the current kernel performs automatic physical tooth-contact settlement. Tooth
counts rebuild a new inspectable geometry ladder, and involute flanks are
precomputed into vector segments before they are rendered.

Run locally with:

```powershell
dotnet run --project Gojo.GearShapeLadder
```

Render one rung without opening a window with:

```powershell
dotnet run --project Gojo.GearShapeLadder -- --smoke-render=gear-ladder.png --stage=5
```

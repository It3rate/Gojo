# Primitive runtime viewer

This local Avalonia/Skia inspection window drives the experimental compact
runtime in `../PrimitiveRuntime`. The timer supplies a pulse; the captured native
transition law chooses the next supported state. The viewer does not compute a
trajectory, interpolate endpoints, or supply missing transition rules.

```powershell
dotnet run --project experiments/PrimitiveRuntime.Viewer -c Release
```

Use **Play**, **Single step**, or **Reverse travel** to offer inputs. **Reverse
observer** changes the view without changing the construction. **Open binding**,
**Alternatives**, and **Blocked input** expose different unresolved cases;
**Reset** restores the initial B/forward binding on the same runtime occurrence.

The teal endpoint is shared by two connections. The amber endpoint starts with
the same landmark binding, but has independent identity and stays at B. Ghosted
teal marks are alternatives for one site, not simultaneous copies of it.

The default view is unmeasured: equal screen spacing communicates order only.
Selecting a calibrated read adds the explicit landmark correspondence and a
quantum of 1, 2, or 4 source ticks. The read displays retained source residue;
choosing it does not move the construction or inject tension.

Headless inspection is available without opening a window:

```powershell
dotnet run --project experiments/PrimitiveRuntime.Viewer -c Release -- --steps=2 --render=D:/_quickDraw/_work/primitive-runtime/motion.png
dotnet run --project experiments/PrimitiveRuntime.Viewer -c Release -- --steps=2 --quantum=4 --reverse-observer --render=D:/_quickDraw/_work/primitive-runtime/reversed.png
dotnet run --project experiments/PrimitiveRuntime.Viewer -c Release -- --alternatives --render=D:/_quickDraw/_work/primitive-runtime/alternatives.png
```

Inspection snapshots, text, pixels, and UI allocations are presentation costs.
Use the runtime's separate diagnostic runner for computation measurements.

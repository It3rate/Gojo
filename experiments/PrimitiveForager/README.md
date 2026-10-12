# Primitive forager

A consolidated diagnostic for primitive numerical information: unavailable
versus absent acquisitions, relative direction, fast/slow context, explicit
orientation, coarse support, conservative lookup arithmetic, and selected
amount precision. It is an authored native controller, not a learning system.

## Run

From `D:\source\repos\It3rate\Gojo-phototaxis`:

```powershell
dotnet run --project experiments/PrimitiveForager -c Release -- --ticks 1536
python -m http.server 4326 --bind 127.0.0.1 --directory artifacts/primitive-forager
```

Open <http://127.0.0.1:4326/>. The replay runs entirely from saved output; it
does not execute policy in JavaScript. Ctrl+C stops the preview server.

For a short infrastructure check:

```powershell
dotnet run --project experiments/PrimitiveForager -c Release -- --quick --output artifacts/primitive-forager-smoke
```

`--ticks` accepts32..4096; `--quick` uses two seeds and at most128 ticks. The
default uses four seeds, nine matched controllers, and five additional backend,
private-scale, and orientation checks. No paid services or Jev calls are used.

## Inspect

- `report.md`: aggregate outcomes, diagnostic encounters, utilization and costs.
- `results.json`: all complete recorded trajectories, including proof runs.
- `index.html`: complete matched seed7 replays and all full-controller seeds,
  plus the aggregate comparison and diagnostic results. Duplicate backend and
  private-scale runs are represented by their verified comparisons.
- `*.gojo`: final observer banks, native controller/laws, memory and witnesses.

Use episode buttons to jump to the first use of a capability. Green evidence
cards participated in the selected action; a read occurring alone does not
establish utility. Coarse readings remain visible beside any finer response.
The source location is observer context and hidden until explicitly revealed.

To refresh the display and prose from recorded results without rerunning:

```powershell
dotnet run --project experiments/PrimitiveForager -c Release -- --render-existing artifacts/primitive-forager
```

This preserves saved trajectories, native archives and recorded signatures.

Light has no exposed numeric origin, unit, distance between landmarks, maximum
or minimum. Its scores represent weighted comparison signs, with mixed zero
distinct from all-equal. The body instrument supplies separate, explicitly
calibrated energy/heat units. Conditional four-tick wait allowances reserve
energy16 and heat20; declared query fees are included within the energy margin.
No outcome counter writes controller memory.

The detailed native comparison observer still allocates many witnesses and
queries retained references eagerly. Only body precision and spatial ordering
are selected on demand. This demonstration is meant to expose useful
information boundaries; it is not a compact runtime benchmark.

See `Gojo/docs/GojoPrimitiveForagerFixture.md` for the native contracts.

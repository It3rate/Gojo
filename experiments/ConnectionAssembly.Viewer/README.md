# Connection assembly replay

A separate Avalonia / native Skia observer for the fixed connection-assembly
experiment. It contains no learner, Gojo equations, waveform generation or link
inference. Its JSON objects exist only at the rendering boundary.

From the repository root, open a recorded replay:

```powershell
dotnet run --project experiments/ConnectionAssembly.Viewer -- --replay=experiments/ConnectionAssembly/results-lifecycle/changing-patterns.json
```

Play / pause, step, scrub, or jump between the recorded phase labels. Playback
advances through 12 observations per second; that is presentation speed, not the
learner's physical clock. The trace shows at most 180 past arrivals, rescales to
their observed range, and leaves unknown or nonconsecutive input as gaps. A known
scalar remains visible during an incomplete native read such as warmup.

Render a PNG without starting Avalonia or opening a window:

```powershell
dotnet run --project experiments/ConnectionAssembly.Viewer -- --replay=experiments/ConnectionAssembly/results-lifecycle/changing-patterns.json --render=experiments/ConnectionAssembly/results-lifecycle/replay-frame.png --frame=240
```

`--frame` is a **zero-based array index**, not the source step. It is clamped to
the replay's range. The PNG is 1480 × 1000 and uses the same renderer as the UI.

The six numbered nodes show the recorded raw recognition, request, received
help, unresolved pressure, engagement and accounted read. Incomplete native
readings have an open-read badge; their actual cleared ports remain visible.
Zero is no pulse, not certified absence. Node numbers map directly
to `nodeNames` and the array positions used by the recorded links.

Read the two times separately:

- Ports and receipts describe the **current arrival through the old gates**.
- Support and gates are **after feedback, available for the next arrival**.
- A dashed purple probe is a newly available link; it earns no help on its
  discovery frame. Teal lines show useful receipts; blue lines show other
  receipts. Dotted withdrawals show a link that just closed.
- The right-hand ledger reports the actual directed support, gate and receipt
  values. Closed idle slots are visually hidden but are not claimed to have
  been removed from the native graph. Support controls line weight; this is
  display scaling, not a confidence probability.

The lifecycle follow-up physically detaches expired publications from
settlement, while retaining their dormant relation templates in the graph.
The original preattached-gate replay is still available under `results`.

The input phase names and the complete replay history belong to the observer.
The learner does not receive them. This displays recognition and local help,
not future prediction or independent evidence from the six shared-input readers.
Optional native graph counts are measurements at that frame; a missing count is
not interpolated or carried forward.

The JSON contract is the runner's `title`, six `nodeNames`, and sequential
`frames` with `step`, `phase`, nullable `value`, `complete`, six `nodes`, and
directed `links`. Nodes expose `id`, `raw`, `request`, `help`, `pressure`,
`engagement`, and `accounted`; links expose `from`, `to`, `support`, `gate`,
`receipt`, `proposed`, `useful`, and `released`. Optional counts are
`nativeRelations` and `nativeCarriers`. Loading validates the report shape;
it does not judge or repair the experiment's readings.

The project has no Gojo dependency and does not join the kernel solution. It
uses Avalonia 11.3.17 and SkiaSharp 2.88.9 with Windows native assets. No XAML,
browser script, remote service or separate visualization framework is involved.

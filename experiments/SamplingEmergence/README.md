# Sampling before measurement

A small console lab for an exploratory Gojo sampling surface. It separates
signal presence, witnessed ordering, finite landmarks, and later metric
calibration. The browser presentation reads generated results; it performs no
learning or semantic computation.

## Run

From the repository root:

```powershell
dotnet run --project experiments/SamplingEmergence -c Release -- --output artifacts/sampling-emergence --budget 8
```

Open `artifacts/sampling-emergence/index.html` directly in a browser. No service
or account is required. The page includes its data and can be copied with the
other outputs for inspection on another device.

For a smaller or different input sequence:

```powershell
dotnet run --project experiments/SamplingEmergence -c Release -- --output artifacts/sampling-small --budget 2 --values "0,12,12,47,3,18,0,30"
```

This console fixture caps `--budget` at 1 through 32 landmarks to keep its
saved examples small; the native helper accepts budgets through 240.
`--values` accepts up to 64 signed
32-bit integers; the transformed sequence must also fit that range. Its zero
values mean no signal. The distance scenario always uses its own two-landmark
budget. Output is deliberately small and deterministic apart from generation
time and process-local native identities.

## Three scenarios

1. **Irregular signal:** absence, repeated readings, and 15 distinct present
   values. With the default eight-landmark budget, later observations can
   occupy an interval between anchors instead of adding another anchor.
2. **Rescaled signal:** private fixture values become `100 × value + 1000`.
   Presence is preserved from the original sequence: an original absence
   remains absent even though its private reference value becomes 1000. The
   runner checks that native ordinal projections match the original scenario.
3. **Tiny distance increase:** acquire 90-mile and 110-mile anchors, then
   100 miles and 100 miles plus 1 mm. Both middle samples share the same
   interval. A comparison with the preceding present sample detects an
   increase but supplies no measurement of its size.

Each scenario that has a present sample ends with a separate explicit metric
stage. These are manually chosen fixture values, not instrument measurements.
The distance stage retains `1 mile = 1,609,344 mm` as connected calibration
evidence and publishes integer-millimetre coordinates. The two middle values
then read 160,934,400 mm and 160,934,401 mm.

## Where the meaning lives

The learner's native shape is:

```text
state = G(source, policy, landmarks, visits, metric-or-open, last-witness)
sample = ReadWitness(source, presence-frame,
                     G(open-magnitude, presence-location), open, source)
visit = ReadWitness(sample, G(policy, previous-landmarks),
                    G(region, direction), residue, requested-comparisons)
metric = G(common-basis, G(sample -> qualified-publication, ...))
```

The fixture's private `Dictionary<Relation,int>` supplies only locally
requested comparisons: candidate below, indistinguishable from, or above an
anchor. `Observe` receives samples and these native comparison witnesses,
never the private numbers. Native ordering decides anchor order and membership;
neither the renderer nor the runner sorts anchors by private magnitude.

At the explicit final stage, the supplier exposes finite qualified publications
on a common `S(axis,0,1)` basis. Their domain is
`[-int.MaxValue, int.MaxValue]`, with quantum 1. Open sample magnitudes stay open;
calibration supplies new evidence through bindings to the original acquisition
identities. Finite clipping and loss retain their own support qualifications,
with no hidden wider arithmetic rescue. No nominal size series or equal-spacing
assumption is inferred.

A same-anchor bucket means a matched comparison class, **not** a known numeric
magnitude. An interval is a valid coarse membership read. Absence has no order
region. A null direction means that no direction read is available; it is not
automatically zero.

The landmark budget limits anchor count, **not total graph memory**. This small
fixture retains every visit and its comparison evidence, so its native archives
grow even after the anchor budget fills. Adaptive forgetting is future work.

## Outputs

- `index.html`: interactive presentation with scenario, step, and playback
  controls. Private fixture references are hidden until explicitly displayed.
- `results.json`: inspection projections of native states. `metricValues` come
  from native finite publications and are empty before calibration.
- `report.md`: a concise summary, results, and limitations.
- `<scenario>-<step>.gojo`: complete native state transport using
  `GojoNumericFormat`, including retained history and open slots. It does not
  include the external oracle dictionary. Every generated archive is decoded
  and checked for retained acquisition groups, open sample magnitudes,
  landmark identity, and any calibrated values, support, and sample bindings.

`sampleCount`, `historySamples`, `landmarkCount`, and `comparisonCount` are read
from the current native groups. Archive sizes are the actual generated byte
lengths. `nominalAvailable` remains false because this lab does not construct a
nominal size-series model. JSON labels and event numbers are display adapters;
they do not replace native sample identity.

The default run produces 20 acquisitions for each irregular signal, 8 anchors,
53 requested comparisons, and 7 coarse interval visits. The distance scenario
uses 4 acquisitions, 2 anchors, 6 comparisons, and 2 coarse interval visits.
These results demonstrate a local structural contract, not autonomous discovery
of a metric, probability distribution, or learning rule for forgetting.

# Sampling before measurement

This experimental fixture starts with an open magnitude. A supplier exposes only
whether a signal is present and, when asked, how two present acquisitions compare.
The learner can retain landmarks and local regions without receiving the private
numbers that the synthetic supplier uses to answer those questions.

## What emerges

1. **Before a sample:** magnitude, unit and spacing remain open.
2. **Presence:** the 0/1 read belongs to a presence basis. One means that a signal
   exists; it does not mean one unit of signal amplitude. No signal is not a
   measured amplitude of zero.
3. **Comparison:** a supplier witness can establish below, indistinguishable or
   above for a particular pair. This is order within that service, not distance.
4. **Local regions:** distinguishable acquisitions become remembered references.
   Other observations can match a reference, fall between references, or occupy
   an outer region with an open boundary. The references need not be equally
   spaced in any later measurement basis.
5. **Metric publication:** a separately exposed common unit basis and qualified
   measurements can add numerical spacing. More samples alone do not discover
   that calibration. Earlier acquisitions retain their open magnitude slots.

Language such as “a little” or “a lot” could be a consumer's read of these regions
in a particular context. It would not be a universal numeric code. A useful
“normal” reference needs an exposed context or an additional learned mechanism;
an ordinal middle reference is not an established mean, median or normal range.
Likewise, the first and last remembered references are not the world's absolute
minimum and maximum.

## Native structure

`SamplingSurface` assembles existing Gojo ordered groups and `ReadWitness`
relations. The structure, rather than a host object containing semantic fields,
is the persistent state:

```text
G(source,
  G(presence unit point, signed order frame, landmark capacity point),
  G(sorted landmark acquisitions),
  G(chronological visit witnesses),
  open metric OR G(common unit basis, G(acquisition -> qualified publication)),
  latest witness)
```

An acquisition is a witness whose result connects an open magnitude slot and a
0/1 presence coordinate. An order answer names the exact candidate and anchor
acquisitions, and exposes their signed comparison frame. A visit connects:

```text
selected acquisition
  -> G(native region, direction witness or open slot)
  with provenance G(requested comparison witnesses)
```

Matching a landmark publishes `landmark -> landmark`. An interior region is
`lower reference -> upper reference`; an outer region keeps one endpoint open.
The direction witness compares with the preceding present acquisition, even if
that prior acquisition could not be assigned a region. Absence stays in history
without entering magnitude ordering.

Acquisitions from a different source or presence basis are rejected locally.
Malformed, missing or contradicted required comparison evidence leaves an
unresolved visit and the previous landmarks intact. The supplier promises a
consistent comparable ordering; the fixture checks requested neighboring roles,
not every possible comparison in a global solver. Here “indistinguishable” is
equality in the exposed ordering service, not physical identity or exact metric
equality.

## Deliberately small policy

The first distinguishable samples fill a finite landmark budget. Once full, the
fixture keeps those references and publishes coarser gaps rather than silently
replacing them. This is a basic admission policy, not utility driven clustering,
adaptive repartitioning or a learned probability distribution.

The helper accepts 1–240 landmarks; the console fixture caps its diagnostic runs
at 32 landmarks and 64 samples. Default: eight landmarks and 20 acquisitions.
The budget limits anchors only. All acquisition and comparison witnesses remain
retained, so total graph storage still grows. A production learner would need an
explicit retention or crystallization mechanism if that growth is undesirable.

The C# adapter's private dictionary is the synthetic comparison instrument. It
answers order requests using fixture numbers. Those numbers do not enter the
learner's ordinal state. JSON, labels and browser drawing are inspection
projections. Revealing the separate reference plot changes no learner input.

## Tiny changes and coarse arithmetic

The distance fixture retains only two references: 90 miles and 110 miles. Both
100 miles and 100 miles plus 1 mm occupy the same native gap. A separate witness
still establishes that the latter is above the former. At that stage the learner
does not know the increment's size or the references' units.

This demonstrates preserving direction while a coarse read stays unchanged. It
does not implement arithmetic over all qualitative regions. A later arithmetic
law must expose the bases that make an operation meaningful: order supports
comparison, calibrated additive quantities support addition, and dimensionless
ratios require an appropriate relation between their operands. A region label
or ordinal rank must not be used as a numeric operand.

The final fixture stage explicitly exposes integer millimetre publications and
a calibration relation for one mile to 1,609,344 mm. The existing finite-read
contract supplies capacity and quantum; it does not rescue lost information with
a wider exact calculation. The original acquisitions are connected to these new
publications, rather than rewritten as if they had always contained a metric.

## Reproduce and inspect

From the repository root:

```powershell
dotnet run --project experiments/SamplingEmergence -c Release -- --output artifacts/sampling-emergence --budget 8
python -m http.server 4322 --bind 127.0.0.1 --directory artifacts/sampling-emergence
```

Open `http://127.0.0.1:4322/`. The replay offers an irregular signal, a monotonic
rescaling of that signal, and the distance example. Advance through observations;
measurement becomes available only at the final, explicitly calibrated stage.
Each stage also has a native `.gojo` archive. See the generated `report.md` for
actual counts and `results.json` for the presentation projection.

The default trial produced eight landmarks from 17 present acquisitions (20
total), 53 comparison queries and seven visits assigned to coarse intervals.
Its ordinal projections were identical after changing every private present
value to `100 * value + 1000`. The distance trial used six comparisons; both
interior observations retained the same gap while the last direction read was
positive. No unresolved visits occurred. The final native archives were 140,969
bytes and 29,814 bytes respectively, including retained history and the explicit
metric publications. These diagnostic archive sizes are not packed numeric
payload sizes or estimates of a compact production representation.

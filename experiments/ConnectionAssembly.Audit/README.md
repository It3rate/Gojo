# Connection assembly allocation audit

This is an observer diagnostic. It creates a network, supplies 240 small zigzag
arrivals, and counts reachable physical identities through
`GojoEncodedSnapshot.CaptureGraph`. It then supplies one more arrival and
compares retained identities. It does not change the learner or the input
policy. Counts include attached worlds and unit surfaces, not only the direct
relation tree. Allocation measurement covers one admission, excluding graph
capture and reporting.

Run from the repository root:

```powershell
dotnet run --project experiments/ConnectionAssembly.Audit/ConnectionAssembly.Audit.csproj -c Release
```

The project references the current experiment and kernel. Results after later
changes are therefore expected to differ from this baseline. The node and
witness section labels inspect the experimental ordered layouts; they are not
a general kernel classifier.

## Baseline measured on 2026-10-04

`baseline-counts.txt` was collected from the Release assemblies produced for
the first fixed run at commit `35481ea`, before the connection-removal changes in this round. A
temporary diagnostic referenced those assemblies directly so this baseline
could be measured without mixing ongoing kernel edits into it. The committed
diagnostic uses ordinary project references for subsequent runs.

| Reachable selection at arrival 240 | Carriers | Relations | Distinct attached relations |
| --- | ---: | ---: | ---: |
| Complete current root | 3,990 | 40,378 | 42 |
| Root participants except latest frame witness | 217 | 2,294 | 42 |
| Additional structure reachable only through latest witness | 3,773 | 38,084 | 0 new |

The smaller selection is not a minimal native network: it still includes the
current constructed feature equations, node wrappers, trace, policies and
links. The 42 attached executable relations are 30 gated product publications,
six consumer sums, and six pressure boundaries.

The latest witness explains 94.3% of retained relations. Its nonoverlapping
contributions, counted by adding sections to the base selection in their
authored order, are:

| Latest-witness section | Additional carriers | Additional relations |
| --- | ---: | ---: |
| Input sample | 1 | 6 |
| Admission and completeness evidence | 17 | 160 |
| Node samples and evidence | 367 | 3,461 |
| Link samples and evidence, after shared node evidence | 1,681 | 16,532 |
| State transitions and settlement evidence | 1,705 | 17,893 |
| Outer witness and world scaffolding | 2 | 32 |
| Total | 3,773 | 38,084 |

The repeated next-trace participant adds nothing because it is already in the
base selection. Section order matters for incremental accounting where one
section shares another's evidence. The full output also gives independent
section reachability; those independent numbers must not simply be summed.

Large subcomponents include 8,130 relations for 30 support updates; 7,512 for
30 leave-one-helper-out calculations (including shared accounting evidence);
and 16,991 across the four target-delta construction/read groups. These
subcomponents have overlapping ancestry and are diagnostic breakdowns, not
additional totals.

Of the full graph, 3,291 carrier identities are ordered-group anchors and
22,971 relation identities belong to ordered-group scaffolding. An ordered
group is native connected structure, not an opaque host record, but its anchor,
origin, member attachments and ordered chain expand even a small evidence
record into multiple primitive relations. These counts are not thousands of
independent sensors or observers.

## Reconstruction versus persistence

Moving from arrival 240 to 241 retains the same overall counts and the same
raw-port and link identities, but replaces 3,827 carrier identities and 38,733
relation identities. Only 163 carrier identities and 1,645 relation identities
are shared between the roots. One admission allocated approximately 14.7 MB
(two isolated measurements: 14,658,416 and 14,679,024 bytes; runtime allocation
can vary).

The live carriers, support, gates, receipt ports, and the 42 attachments are
reused. The experiment rebuilds feature equations, node/world wrapper groups,
and most calculation/claim/feedback evidence at every arrival. `NativeMath`
also copies equation/input relations to make each read independent and keep
captured coordinates from drifting. It retains only the latest witness in the
current root; there is no growing linked history of all prior observations.
The host replay file separately records projected observations for viewing.

Consequently the user's reconstruction concern is correct. The retained graph
is bounded, but much of it is recreated each arrival and then discarded. This
is an experiment implementation choice, not a Gojo requirement that physical
structures be immutable.

## Smallest useful improvement direction

Existing kernel mechanisms already distinguish mutable physical sites from
stable promises: carriers receive tension, `TickRef.Set`/`Move` retarget shared
coordinates, relation perturbation moves participating sites, and structural
port import retargets exposed coordinates. Stable `UnitShape` promises and
historical witnesses deliberately have different lifetime requirements.

The next allocation experiment can construct feature, target-delta, support
and ablation equations once around exposed dynamic sites; update those sites
at an explicit admission boundary; and preserve bounded current result and
residue evidence. Historical evidence should be captured only when its
retention is part of the experiment, and must not silently mutate as those
sites move. Full archival replay and compact adaptive state are different
contracts. Reducing witness retention alone would lower the retained graph,
but would not fix construction churn unless the reusable equations/read path
are also changed.

This audit makes no mutability or witness-retention changes itself.

## First run with actual connection withdrawal

`current-lifecycle-counts.txt` records the same diagnostic against this round's
connection-removal implementation. The retained carrier/relation counts and
the per-arrival replacement counts are unchanged. The attached relation count
is 12 at creation and 18 after 240 zigzag arrivals, rather than 42 throughout.
One measured admission allocates 11,360,288 bytes. This single admission is
illustrative, not a general performance benchmark.

Physical withdrawal reduces the active settlement work. It does not remove
the experiment's retained candidate links or latest calculation evidence, so
it should not be described as fixing the large retained graph or reconstruction
churn.

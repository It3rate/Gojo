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

The diagnostic now also selects `AssemblyNetwork.Operational(root)` and records
its identity and reachable instruction identities separately from the admission
envelope and selected historical samples. Node relation and feature-constraint
identities are compared across arrivals, in addition to raw ports and links.
This distinguishes a reused equation from a reconstructed equation that merely
publishes through the same carrier. Counts of attached relations can still
change as a reusable publication joins or leaves settlement.

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

## Improvement direction at the original audit

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

## Reusable dynamic equations

`current-dynamic-counts.txt` records the same Release diagnostic after the
operational equations were moved into the persistent network. Features,
accounting, helper ablations, bounded support/fade updates and target-delta
instructions are now constructed once. Explicit input and instruction sites
move at admission; pure numeric reads do not rewrite them. Missing input
collapses a connected projection basis, so the equation itself becomes
unreadable instead of reusing a stale coordinate as a new observation.

| Reachable selection at arrival 240 | Before reuse: carriers | After reuse: carriers | Before reuse: relations | After reuse: relations |
| --- | ---: | ---: | ---: | ---: |
| Complete current root | 3,990 | 1,600 | 40,378 | 24,116 |
| Root participants except latest frame witness | 217 | 938 | 2,294 | 14,699 |
| Additional structure reachable only through latest witness | 3,773 | 662 | 38,084 | 9,417 |

The larger persistent selection is intentional: it now owns the reusable
instructions that previously lived in freshly rebuilt per-arrival evidence.
The latest witness supplies about 39 percent of retained relations rather
than 94 percent. There are still 18 distinct attached relations at this
boundary: 12 fixed relations and six currently supported helpers. Persistent
instructions are not all attached settlement observers.

Between arrivals 240 and 241, 663 carrier identities and 9,423 relation
identities are replaced, versus 3,827 and 38,733 in the previous path. The
actual node, constraint, link and operational-root identities remain stable.
Every original operational relation is still reachable. The operational
selection gains 180 reachable relations by arrival 240 because supported
publications join its attached world; those publications already existed in
the original finite candidate neighborhood. A relation newly reachable from
one diagnostic selection is not necessarily newly allocated.

One measured admission allocated 5,906,744 bytes, compared with 11,360,288 bytes
in the lifecycle run, approximately 48 percent less. This measures allocation,
not peak retained memory, and one arrival is not a general speed benchmark.
Some temporary read, claim, settlement and selected-witness construction
remains. The rewrite removes routine reconstruction of the operational
equations; it does not establish zero-allocation observation or a general
optimized execution backend.

The experiment now passes 16 focused check groups. Additional structural
proofs show that one instruction responds to changed signed input coordinates;
an existing bound can narrow and restore the same node's recognition scope;
partial and missing native inputs remain unreadable at the appropriate local
scope; pure reads leave encoded live state unchanged; and failed reads before
settlement, including overflow and an ordinary collapsed basis, restore the
original admission sites. Selected historical numeric results and comparison
bounds remain fixed while the identified live network changes.

Those historical samples deliberately retain references to live instruction
and participant identities. They are not a promise to freeze every reachable
carrier or reproduce an entire earlier world. A caller needing that stronger
contract must explicitly capture it. The recognition/help policy remains
supplied; persistence and mutability do not demonstrate autonomous discovery
of composite concepts.

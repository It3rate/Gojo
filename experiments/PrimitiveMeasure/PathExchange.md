# Two private paths, one public loop

Can two owners establish that their paths form a loop while keeping the paths private? Connect A's exit to B's entrance and B's exit to A's entrance. Each owner reports whether its part can be followed from entrance to exit. Reverse a step inside B. A's report still holds. B first withdraws its old answer, then reports a break when its inspection reaches the reversed step. The host learns that the loop is broken without tracing either owner's private path.

Open [layer 19](https://gojo.inklingai.ca/encoding/explorer/?layer=19). Begin with ordinary pairs, choose the reversal, and advance four beats. Repeat with vertex chains. The dark panel shows the public reads followed by an inspector view into B. Only the public reads and seams participate in the host's calculation.

## What the two arrays mean

| Ordinary pairs | Vertex chain |
| --- | --- |
| `[0,1] [1,2] [2,3] [3,4]` | `[0,1,2,3,4]` |
| Each occurrence stores two references. Two consecutive rows must agree on their join. | Step i reads fields i and i+1. Its neighbour uses the very same field at their join. |

The existing resolution bucket holds the site observations. A separate structural array references those sites. Grouping occurs inside one owner at one resolution in this experiment. The vertex sequence may repeat sites. It may also end at its first site. One layout supports open paths, retraced paths and loops. A claim about a simple outline, an angle or a filled area would need further evidence.

Position saves work because it carries a specific promise. A single vertex run lets the owner compare its two ends to its advertised inlet and outlet. Independently stored pairs can earn the same promise by inspection. Explicit packing reads every authored pair, checks each interior join and writes one vertex per step plus the initial vertex. An incompatible join leaves the original rows usable. Successful packing retains site and occurrence correspondence, existing branches, the public surface and any completed verdict. A partial inspection restarts in the new layout.

## Exact bytes for the four-step example

There are six private sites including optional side destination 5. Site references and cut positions each take three bits here. Fields start at the low bit. Each table pads only its final byte. Choose the corresponding layout to inspect it in the playground.

| Table | Fields | Hex bytes |
| --- | --- | --- |
| Ordinary pairs | `0,1 / 1,2 / 2,3 / 3,4` | `48 A4 8D` |
| Same authored route packed as vertices | `0,1,2,3,4` | `88 46` |
| Initially selected range | cuts `[0,4]` | `20` |
| Selection with step 2 reversed | cuts `[0,2] [3,2] [3,4]` | `D0 34 02` |
| Optional side connection | local sites `[2,5]` | `2A` |
| Site observations, fixed O with owned V then D | six positive nonzero entries | `55 05` |
| After changing the D bit at site 2 | same six sites | `75 05` |
| Host seams | two-bit public roles `[1,2] [3,0]` | `39` |

A cut is the position between two successive edge occurrences. Thus `[0,2]` traverses occurrences 0 and 1. Decreasing cuts traverse in reverse. The third step originally read B2 → B3. The new middle run reads B3 → B2. The preceding run ends at B2 and the middle run starts at B3. A comparison of the outermost endpoints alone would miss that break. Reading across the selected runs exposes it. Repairing the original traversal lets the three ranges coalesce again.

The separate side connection refers to B2 inside the same owner. Its attachment preserves the selected walk and its cached proof. Changing the D bit changes an observation. A passage could use that observation to control flow. This reader asks about connectivity and has no such guard.

## How the answer crosses the boundary

The positive-context support mask publishes `02` for continuous, `01` for broken, and `03` while either remains possible. The host checks its two ordered seam rows and uses the existing bitmap reader for the conjunction of the owners' claims. An absent required seam proves this proposed loop is open. The host itself exposes the same read-port interface, so another reader can consume its result.

The integration test connects this whole-loop port to the guard of an ordinary primitive passage. With the proof withdrawn, the consumer holds its activation. A confirmed break keeps it waiting. Repair and revalidation let the activation pass on the next beat. The consumer needs one guard input throughout.

| Beat | Change or local work | A support | B support, pairs | B support, chain |
| ---: | --- | --- | --- | --- |
| 0 | Both previously checked | 02 | 02 | 02 |
| 1 | Reverse B's third use | 02 | 03 | 03 |
| 2 | Give B one inspection | 02 | 03 | 03 |
| 3 | Give B one more inspection | 02 | 03 | 01 |
| 4 | Give B one more inspection | 02 | 01 | 01 |

With both seams connected, the host reads closed, unresolved, then broken. B invalidates its cached answer and cursor when it commits the structural edit. The host polls the two current public answers each time. The experiment uses a single-threaded order of edit, local work, then public read. A host retaining its own historical answer across edits would also need a freshness protocol. A direct port read here is an observation. Whole-charge transfer across owners is a separate operation.

## Work for 16,384 internal steps

Run `dotnet run --project experiments/PrimitiveMeasure -c Release -- --path-exchange` for the small and larger cases. Each owner has 8,192 steps. The experiment reverses B's step 4,096 after both claims have settled.

| Count for two owners | Ordinary pairs | Vertex chains |
| --- | ---: | ---: |
| Authored topology payload | 57,344 bytes | 28,676 bytes |
| Initial local inspections | 16,384 pairs | 2 runs |
| B inspections after reversal | 4,097 pairs | 2 runs |
| A inspections after B's edit | 0 | 0 |
| Fresh full step walk to the first break, A then B | 12,289 steps | 12,289 steps |
| Edit work in this case | 1 range read, 3 written | 1 range read, 3 written |
| Selected ranges of both owners after edit | 15 bytes | 15 bytes |

Each host poll of the fully wired loop also reads two seam rows and two claim ports, then evaluates their conjunction. The seam rows occupy one byte. Topology payload excludes those ranges, seams, values, scratch space and runtime objects. Inspections count logical operations. A pair inspection decodes a pair row and a chain inspection reads two endpoint fields. Absolute speed depends on their implementation and the cost of maintaining the representation. The previous ring benchmark includes a separate signed-use reference for each edge and performs additional boundary analyses, so its bytes and times describe a different workload.

The cold result isolates the saving from guaranteed adjacency. Reusing A's answer isolates the saving from local invalidation. A single owner could maintain equivalent local summaries. With frequent edits the number of range descriptors and the work of maintaining them can grow. This fixture scans the existing ranges when it splices and coalesces them.

## Where the next distinction lies

A branch can name B2 without disturbing the through route. Making B2 choose between two continuations requires a routing law and a different question at the surface, such as “can this input reach either outlet?” A T, Y, or perpendicular rendering alone gives no preference or allocation. The experiment leaves automated promotion, subdivision and selection of an outlet open.

A host can treat a whole path as one participant for a particular read. That is one level of abstraction. Geometric dimension requires separate evidence about independent directions. A thousand joined paths could still describe travel along one line.

# Refinement compute comparison

Recorded 2026-10-16T16:32:24.9193726+00:00; source base c263482c90a60215b1e12bf5004a9f8f241d4be9 plus current working tree.
Release; normalized source SHA-256: D683379CE44D6381C89D43033B0FF0961B7B696D7FB1F3E4D9CF031D2E328B29.
.NET 10.0.12; Microsoft Windows 10.0.26200; X64; 16 logical processors; server GC=False.
Single thread; 3 samples; medians below, raw samples in comparison.json.

| Workload | Mode | Cells × rounds | Execution ms median [min, max] | Cell-rounds/s | Bytes/cell-round | Setup ms median | Audit ms median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| recurrent-ring | qualitative-native-witness | 32 × 256 | 862.75 [845.61, 996.45] | 9,495.21 | 263,091.73 | 1.56 | 0.02 |
| recurrent-ring | qualitative-compiled-witness | 32 × 256 | 52.00 [49.65, 54.53] | 157,552.10 | 25,355.20 | 1.52 | 0.02 |
| recurrent-ring | qualitative-batched-masks | 32 × 256 | 3.14 [0.38, 3.53] | 2,605,929.51 | 0.00 | 1.52 | 0.01 |
| recurrent-ring | numeric-native-witness | 32 × 256 | 541.87 [541.27, 867.12] | 15,118.11 | 117,676.50 | 0.52 | 0.02 |
| recurrent-ring | numeric-compiled-compute | 32 × 256 | 97.96 [33.63, 100.91] | 83,628.27 | 10,407.49 | 2.74 | 0.17 |
| recurrent-ring | qualitative-native-witness | 128 × 256 | 3,374.35 [3,371.87, 3,869.53] | 9,710.90 | 263,095.19 | 1.44 | 0.01 |
| recurrent-ring | qualitative-compiled-witness | 128 × 256 | 218.04 [194.69, 224.42] | 150,284.08 | 25,356.14 | 1.49 | 0.01 |
| recurrent-ring | qualitative-batched-masks | 128 × 256 | 1.11 [1.08, 2.10] | 29,648,932.32 | 0.00 | 1.50 | 0.01 |
| recurrent-ring | numeric-native-witness | 128 × 256 | 4,581.93 [4,510.82, 4,713.15] | 7,151.57 | 113,758.19 | 1.56 | 0.02 |
| recurrent-ring | numeric-compiled-compute | 128 × 256 | 141.72 [140.07, 149.25] | 231,208.65 | 10,070.74 | 16.63 | 0.24 |
| recurrent-ring | qualitative-native-witness | 512 × 256 | 21,317.51 [14,573.45, 22,458.08] | 6,148.56 | 263,082.25 | 2.61 | 0.03 |
| recurrent-ring | qualitative-compiled-witness | 512 × 256 | 1,422.78 [920.78, 1,474.74] | 92,123.86 | 25,352.61 | 1.83 | 0.04 |
| recurrent-ring | qualitative-batched-masks | 512 × 256 | 5.57 [3.87, 5.93] | 23,540,229.89 | 0.00 | 2.83 | 0.03 |
| recurrent-ring | numeric-native-witness | 512 × 256 | 30,527.10 [26,171.30, 32,242.77] | 4,293.63 | 113,306.66 | 37.16 | 0.08 |
| recurrent-ring | numeric-compiled-compute | 512 × 256 | 1,137.13 [1,023.90, 1,242.64] | 115,265.22 | 10,049.41 | 106.66 | 1.56 |
| structural-coverage | qualitative-coverage-native | 16 × 16 | 78.55 [77.41, 105.40] | 3,259.00 | 608,337.00 | 1.96 | 0.00 |
| structural-coverage | qualitative-coverage-compiled-ordinal | 16 × 16 | 101.63 [78.72, 105.17] | 2,518.90 | 581,863.19 | 1.99 | 0.00 |
| structural-coverage | measured-coverage-native | 16 × 16 | 103.12 [100.92, 150.30] | 2,482.46 | 848,096.00 | 2.07 | 0.00 |
| structural-coverage | measured-coverage-compiled-ordinal | 16 × 16 | 101.31 [90.97, 119.74] | 2,526.96 | 824,344.00 | 2.24 | 0.00 |

Source graphs (relations / placeholders / carriers / boxed locations; reachable relation graph, not a heap census):

- recurrent-ring, qualitative-native-witness, 32 cells: 2247 / 627 / 0 / 0.
- recurrent-ring, qualitative-compiled-witness, 32 cells: 2247 / 627 / 0 / 0.
- recurrent-ring, qualitative-batched-masks, 32 cells: 2247 / 627 / 0 / 0.
- recurrent-ring, numeric-native-witness, 32 cells: 12844 / 163 / 515 / 5060.
- recurrent-ring, numeric-compiled-compute, 32 cells: 12844 / 163 / 515 / 5060.
- recurrent-ring, qualitative-native-witness, 128 cells: 7143 / 2067 / 0 / 0.
- recurrent-ring, qualitative-compiled-witness, 128 cells: 7143 / 2067 / 0 / 0.
- recurrent-ring, qualitative-batched-masks, 128 cells: 7143 / 2067 / 0 / 0.
- recurrent-ring, numeric-native-witness, 128 cells: 51340 / 643 / 2051 / 20228.
- recurrent-ring, numeric-compiled-compute, 128 cells: 51340 / 643 / 2051 / 20228.
- recurrent-ring, qualitative-native-witness, 512 cells: 26727 / 7827 / 0 / 0.
- recurrent-ring, qualitative-compiled-witness, 512 cells: 26727 / 7827 / 0 / 0.
- recurrent-ring, qualitative-batched-masks, 512 cells: 26727 / 7827 / 0 / 0.
- recurrent-ring, numeric-native-witness, 512 cells: 205324 / 2563 / 8195 / 80900.
- recurrent-ring, numeric-compiled-compute, 512 cells: 205324 / 2563 / 8195 / 80900.
- structural-coverage, qualitative-coverage-native, 16 cells: 8897 / 2753 / 0 / 0.
- structural-coverage, qualitative-coverage-compiled-ordinal, 16 cells: 8897 / 2753 / 0 / 0.
- structural-coverage, measured-coverage-native, 16 cells: 11777 / 2897 / 432 / 656.
- structural-coverage, measured-coverage-compiled-ordinal, 16 cells: 11777 / 2897 / 432 / 656.

- One process, single-thread execution; build configuration recorded; construction/capture and execution measured separately; raw samples retained.
- Identical qualitative circuit/checksums for native witnesses, compiled witnesses and batched masks. Different answers and detailed dynamics are expected in the numeric counterpart.
- The qualitative ring executes ten native finite-law applications per cell-round. Compilation derives atomic tables, including nine stock-transition identities, from those same laws.
- Numeric ring executes five equation reads, two numeric conditional selections, stock settlement, and explicit signal/previous/command delta imports. Persistent neighboring ports feed subsequent rounds. Its compiled lane derives arithmetic/interval plans from native equations/policies and still uses kernel tension settlement.
- Batched masks omit per-event witness allocation/history and validate source dependencies once per stage batch. This is a different evidence cost, reported separately; it retains the source laws and supports selected witness reconstruction.
- Numeric stock retains clamp pressure; qualitative stock rejects blocked pulses. This benchmark is a resource comparison of corresponding models, not a claim of behavioral equivalence or an isolated arithmetic-speed comparison.
- Audit costs after execution are separate: qualitative decodes current state and one final law read; numeric decodes current state and reconstructs/verifies a final cell's native equation/policy evidence. Neither reconstructs an unretained full history.
- Coverage repeats bounded witnessed episodes including patterns, resolution, refinement, laws and event schedules. Measured coverage adds calibrated numeric interpretation; only ordinal evaluation is compiled in that suite.
- Allocated bytes are managed allocations on the executing thread, not retained memory or process peak. No learning, full creature physics, result-quality score or speed threshold is used.

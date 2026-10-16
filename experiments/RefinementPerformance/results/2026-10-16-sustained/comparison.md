# Refinement compute comparison

Recorded 2026-10-16T16:34:37.6585731+00:00; source base c263482c90a60215b1e12bf5004a9f8f241d4be9 plus current working tree.
Release; normalized source SHA-256: D683379CE44D6381C89D43033B0FF0961B7B696D7FB1F3E4D9CF031D2E328B29.
.NET 10.0.12; Microsoft Windows 10.0.26200; X64; 16 logical processors; server GC=False.
Single thread; 3 samples; medians below, raw samples in comparison.json.

| Workload | Mode | Cells × rounds | Execution ms median [min, max] | Cell-rounds/s | Bytes/cell-round | Setup ms median | Audit ms median |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| recurrent-ring | qualitative-batched-masks | 512 × 8192 | 162.33 [139.72, 284.95] | 25,838,291.37 | 0.00 | 4.40 | 0.07 |
| recurrent-ring | numeric-compiled-compute | 512 × 8192 | 31,849.93 [31,303.51, 32,008.11] | 131,689.56 | 9,804.42 | 122.04 | 1.75 |

Source graphs (relations / placeholders / carriers / boxed locations; reachable relation graph, not a heap census):

- recurrent-ring, qualitative-batched-masks, 512 cells: 26727 / 7827 / 0 / 0.
- recurrent-ring, numeric-compiled-compute, 512 cells: 205324 / 2563 / 8195 / 80900.

- One process, single-thread execution; build configuration recorded; construction/capture and execution measured separately; raw samples retained.
- Identical qualitative circuit/checksums for native witnesses, compiled witnesses and batched masks. Different answers and detailed dynamics are expected in the numeric counterpart.
- The qualitative ring executes ten native finite-law applications per cell-round. Compilation derives atomic tables, including nine stock-transition identities, from those same laws.
- Numeric ring executes five equation reads, two numeric conditional selections, stock settlement, and explicit signal/previous/command delta imports. Persistent neighboring ports feed subsequent rounds. Its compiled lane derives arithmetic/interval plans from native equations/policies and still uses kernel tension settlement.
- Batched masks omit per-event witness allocation/history and validate source dependencies once per stage batch. This is a different evidence cost, reported separately; it retains the source laws and supports selected witness reconstruction.
- Numeric stock retains clamp pressure; qualitative stock rejects blocked pulses. This benchmark is a resource comparison of corresponding models, not a claim of behavioral equivalence or an isolated arithmetic-speed comparison.
- Audit costs after execution are separate: qualitative decodes current state and one final law read; numeric decodes current state and reconstructs/verifies a final cell's native equation/policy evidence. Neither reconstructs an unretained full history.
- Coverage repeats bounded witnessed episodes including patterns, resolution, refinement, laws and event schedules. Measured coverage adds calibrated numeric interpretation; only ordinal evaluation is compiled in that suite.
- Allocated bytes are managed allocations on the executing thread, not retained memory or process peak. No learning, full creature physics, result-quality score or speed threshold is used.

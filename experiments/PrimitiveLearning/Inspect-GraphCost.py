"""Read-only reachability costs for PrimitiveLearning's native numeric archives.

This reads the documented v2 storage tables. It does not restore, settle, edit,
or execute Gojo, and is not a replacement semantic model. Named fields below
are the explicitly selected local experiment-root layouts in the C# builders.
"""

import argparse
import collections
import json
from pathlib import Path
import struct


class Archive:
    NAMES = {1: "carriers", 2: "tickRefs", 3: "locations", 4: "relations", 5: "unitShapes", 6: "placeholders"}

    def __init__(self, path):
        self.path = Path(path)
        self.data = self.path.read_bytes()
        self.offset = 0
        self.edges, self.bytes, self.relations, self.locations, self.ticks, self.values = {}, {}, [], [], [], []
        magic, version = self.take("<Ii")
        if magic != 0x4F4A4F47 or version != 2:
            raise ValueError("Select a complete Gojo numeric format v2 archive.")
        counts = self.take("<7i")
        minimum = sum(n * row for n, row in zip(counts, [5, 46, 16, 8, 18, 17, 5]))
        if any(n < 0 for n in counts) or minimum > len(self.data) - self.offset:
            raise ValueError("Invalid or truncated native table lengths.")
        self.roots = [self.ref() for _ in range(counts[0])]
        for i in range(counts[1]):
            start = self.offset
            _, _, _, value, _, unit, opposite, live_value, live_tension, n = self.take("<Q??Qqiiiii")
            if n < 0:
                raise ValueError("Negative carrier attachment count.")
            attachments = [self.take("<i") for _ in range(n)]
            self.values.append(value)
            self.add((1, i), [(5, unit), (2, live_value), (2, live_tension)] +
                     ([(1, opposite)] if opposite >= 0 else []) + [(4, r) for r in attachments], self.offset - start)
        for i in range(counts[2]):
            _, tick = self.take("<Qq")
            self.ticks.append(tick)
            self.add((2, i), [], 16)
        for i in range(counts[3]):
            carrier, tick = self.take("<ii")
            self.locations.append((carrier, tick))
            self.add((3, i), [(1, carrier), (2, tick)], 8)
        for i in range(counts[4]):
            self.take("<Q")
            left, right = self.ref(), self.ref()
            self.relations.append((left, right))
            self.add((4, i), [left, right], 18)
        for i in range(counts[5]):
            start = self.offset
            self.take("<Q")
            surface, n = self.ref(), self.take("<i")
            if n < 0:
                raise ValueError("Negative unit descriptor count.")
            self.add((5, i), [surface] + [self.ref() for _ in range(n)], self.offset - start)
        for i in range(counts[6]):
            self.take("<i?")
            self.add((6, i), [], 5)
        if self.offset != len(self.data):
            raise ValueError("Trailing bytes after native tables.")
        if any(edge not in self.edges for edges in self.edges.values() for edge in edges):
            raise ValueError("A native table reference is outside the archive.")
        self.table_counts = dict(zip(["roots"] + list(self.NAMES.values()), counts))

    def take(self, fmt):
        length = struct.calcsize(fmt)
        result = struct.unpack_from(fmt, self.data, self.offset)
        self.offset += length
        return result if len(result) > 1 else result[0]

    def ref(self):
        return self.take("<Bi")

    def add(self, node, edges, length):
        self.edges[node], self.bytes[node] = edges, length

    def point(self, node, holder, tick):
        if node[0] != 4:
            return False
        left, right = self.relations[node[1]]
        return right == holder and left[0] == 3 and self.locations[left[1]][0] == holder[1] and self.ticks[self.locations[left[1]][1]] == tick

    def group(self, node, length=None):
        if node[0] != 4:
            raise ValueError("The selected field is not an ordered relation group.")
        left, holder = self.relations[node[1]]
        if holder[0] != 1:
            raise ValueError("The selected group has no carrier holder.")
        result, pending, active = [], [] if self.point(left, holder, 1) else [(left, False)], set()
        while pending:
            node, leaving = pending.pop()
            if node[0] != 4:
                raise ValueError("The local group has a non-relation branch.")
            if leaving:
                active.remove(node)
                continue
            if node in active:
                raise ValueError("The local group has a cyclic branch.")
            active.add(node)
            pending.append((node, True))
            left, right = self.relations[node[1]]
            if self.point(left, holder, 0):
                result.append(right)
            else:
                pending.extend([(right, False), (left, False)])
        if length is not None and len(result) != length:
            raise ValueError(f"Expected {length} local fields; found {len(result)}.")
        return result

    def reach(self, roots):
        seen, pending = set(), list(roots)
        while pending:
            node = pending.pop()
            if node not in seen:
                seen.add(node)
                pending.extend(self.edges[node])
        return seen

    def stats(self, roots):
        nodes = self.reach(roots)
        counts = collections.Counter(kind for kind, _ in nodes)
        return {"reachableTransportRowBytes": sum(self.bytes[node] for node in nodes),
                "rows": len(nodes), **{name: counts[kind] for kind, name in self.NAMES.items()}}

    def report(self, kind):
        if len(self.roots) != 1:
            raise ValueError("The local experiment archives have exactly one root.")
        fields = self.group(self.roots[0], {"program": 20, "query": 12, "composite": 5}[kind])
        result = {"archive": str(self.path.resolve()), "kind": kind, "transportBytes": len(self.data),
                  "tableCounts": self.table_counts,
                  "tableBytes": {name: sum(size for (table, _), size in self.bytes.items() if table == kind)
                                 for kind, name in self.NAMES.items()},
                  "fullGraph": self.stats(self.roots),
                  "measurement": "Unique reachable native table-row bytes; component unions overlap. Header/root selector bytes are excluded from reachability costs.",
                  "diagnosticCuts": "Root-field omissions are reachability calculations only. They neither produce a valid replacement learner nor preserve omitted audit semantics."}
        if kind == "program":
            candidates, selected = self.group(fields[10], 16), self.values[fields[11][1]]
            receipts = self.group(fields[17])
            retained = [self.group(self.group(receipt, 5)[4], 6)[5] for receipt in receipts]
            audits = [self.group(witness, 5) for witness in retained]
            decisions = [self.group(self.group(receipt, 5)[4], 6)[0] for receipt in receipts]
            decision_fields = [self.group(self.group(decision, 5)[4], 8) for decision in decisions]
            result.update({"selectedCandidate": selected, "receiptCount": len(receipts),
                           "selectedPolicy": self.stats([self.group(candidates[selected], 6)[1]]),
                           "allCandidatesAndEditParents": self.stats([fields[10]]),
                           "activeGrammarLedgersAndSelectors": self.stats(fields[:15] + [fields[19]]),
                           "latestDecision": self.stats([fields[15]]), "latestFeedback": self.stats([fields[16]]),
                           "receiptUnion": self.stats([fields[17]]), "latestRetentionAudit": self.stats([fields[18]]),
                           "allRetainedGlobalAudits": self.stats(retained),
                           "allRetainedScoreBundles": self.stats([audit[1] for audit in audits]),
                           "allRetainedCoverageAndImprovementClaims": self.stats([audit[3] for audit in audits]),
                           "retainedScoreCount": sum(len(self.group(audit[1])) for audit in audits),
                           "retainedClaimCount": sum(len(self.group(audit[3])) for audit in audits),
                           "allModeInputProvenance": self.stats([decision[3] for decision in decision_fields]),
                           "allDirectionInputProvenance": self.stats([decision[4] for decision in decision_fields]),
                           "withoutReceiptRootField": self.stats([field for i, field in enumerate(fields) if i != 17])})
        elif kind == "query":
            result.update({"receiptCount": len(self.group(fields[10])), "activeContextRowsAndLedgers": self.stats([fields[7]]),
                           "latestDecision": self.stats([fields[8]]), "latestFeedback": self.stats([fields[9]]),
                           "receiptUnion": self.stats([fields[10]]),
                           "withoutReceiptRootField": self.stats([field for i, field in enumerate(fields) if i != 10])})
        else:
            names = ["adaptiveRoot", "predictorRoot", "queryRoot", "programRoot", "latestPublications"]
            result["componentUnions"] = {name: self.stats([field]) for name, field in zip(names, fields)}
        return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path)
    parser.add_argument("--kind", choices=["program", "query", "composite"], required=True,
                        help="Select the exact local C# experiment-root layout, not a global graph classifier.")
    args = parser.parse_args()
    print(json.dumps(Archive(args.archive).report(args.kind), indent=2))

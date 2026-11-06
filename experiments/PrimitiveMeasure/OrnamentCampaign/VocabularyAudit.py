"""Post-hoc census of bound circuitry in a frozen primary manifest. No episodes are run.

Usage: python VocabularyAudit.py PRIMARY/frozen.json VERIFICATION_FOLDER
Only the manifest is read. The two output files are independently reproducible.
"""
import argparse
import csv
import hashlib
import json
from collections import Counter, defaultdict
from pathlib import Path


SENSOR = ("zero", "one", "current", "previous", "next", "back-gap", "ahead-gap",
          "back-longer", "ahead-longer", "reflection", "seed", "protected",
          "balance", "infill", "mirror", "seam")
WRITER = ("zero", "one", "wire", "previous-wire", "last-commit", "phase", "current", "stock")
METHODS = ("parameter", "open", "random", "evolve")


def decode(hex_program):
    """Read the little-endian, individually padded EndpointRows blocks."""
    block = bytes.fromhex(hex_program)
    if len(block) < 2 or block[0] != 1 or block[1] & 0xc0:
        raise ValueError("Unknown program header")
    ns, nw, flags = 1 + (block[1] & 3), 1 + ((block[1] >> 2) & 3), block[1] >> 4
    offset = 2

    def fields(count, bits):
        nonlocal offset
        size = (bits * count + 7) // 8
        raw = block[offset:offset + size]
        offset += size
        word = int.from_bytes(raw, "little")
        if len(raw) != size or word >> (count * bits):
            raise ValueError("Bad endpoint table or padding")
        return [(word >> (bits * i)) & ((1 << bits) - 1) for i in range(count)]

    laws = fields(ns + nw, 4)
    s = fields(2 * ns, 5)
    w = fields(2 * nw, 4)
    links = fields(2 * nw, 2)
    sr = [(s[2*i], s[2*i+1], laws[i]) for i in range(ns)]
    wr = [(w[2*i], w[2*i+1], laws[ns+i], links[2*i], links[2*i+1]) for i in range(nw)]
    if offset != len(block) or any(max(row[:2]) >= len(inputs)+i
                                   for rows, inputs in ((sr, SENSOR), (wr, WRITER))
                                   for i, row in enumerate(rows)):
        raise ValueError("Invalid length or local binding")
    return flags, sr, wr


def bits_key(rows):
    return "/".join(".".join(f"{x:x}" for x in row) for row in rows)


def connected(rows, root, base):
    """Select the addressed row and all earlier rows supplying its two inputs."""
    seen = set()
    def visit(i):
        if i in seen:
            return
        seen.add(i)
        for port in rows[i][:2]:
            if port >= base:
                visit(port-base)
    visit(root)
    return [(i, *rows[i]) for i in sorted(seen)]


def bool_table(rows, names):
    """Assign all used native endpoints as independent known bits; honor tied addresses."""
    ports = sorted({p for row in rows for p in row[:2] if 2 <= p < len(names)})
    states = 1 << len(ports)
    full = (1 << states) - 1
    values = [0] * len(names)
    values[1] = full
    for bit, port in enumerate(ports):
        values[port] = sum(1 << n for n in range(states) if (n >> bit) & 1)
    for a, b, law, *_ in rows:
        x, y = values[a], values[b]
        masks = (~x & ~y, x & ~y, ~x & y, x & y)
        values.append(full & sum(mask & full for k, mask in enumerate(masks) if law & (1 << k)))
    return ports, states, full, values[len(names):]


def action_key(outputs, ports, inputs):
    """Remove input lanes on which the selected output is constant on all known assignments."""
    essential = [bit for bit in range(len(ports))
                 if any(o != outputs[i ^ (1 << bit)] for i, o in enumerate(outputs))]
    table = []
    for value in range(1 << len(essential)):
        index = sum(((value >> j) & 1) << bit for j, bit in enumerate(essential))
        table.append(outputs[index])
    return ",".join(inputs[ports[bit]] for bit in essential) + "=" + "".join(table)


def verify_read(rows, names, ports, outputs):
    """Independent scalar traversal of each known assignment checks the bulk census read."""
    for index, expected in enumerate(outputs):
        local = [0] * len(names)
        local[1] = 1
        for j, p in enumerate(ports):
            local[p] = (index >> j) & 1
        actual = "-"
        for row in rows:
            a, b, law = row[:3]
            value = (law >> (local[a] + 2 * local[b])) & 1
            local.append(value)
            if len(row) == 5 and value and row[3] != row[4]:
                actual = f"{row[3] * 4 + row[4]:x}"
                break
        if names == SENSOR:
            actual = str(local[-1])
        if actual != expected:
            raise ValueError("Bit-parallel and individual bound circuit reads differ")


def signatures(program):
    flags, sr, wr = decode(program)
    yield "program-bytes", program
    for role, rows in (("sensor", sr), ("writer", wr)):
        yield role + "-exact-owner", f"f{flags}/" + bits_key(rows)
    export_key = bits_key(connected(sr, len(sr)-1, len(SENSOR)))
    yield "sensor-export-exact-bindings", export_key
    yield "sensor-export-backed-frame", f"f{flags}/" + export_key
    for i, row in enumerate(wr):
        if row[3] != row[4]:
            guard_key = bits_key(connected(wr, i, len(WRITER)))
            yield "passage-guard-exact-bindings", guard_key
            yield "passage-guard-backed-frame", f"f{flags}/" + guard_key

    ports, states, _, sensed = bool_table(sr, SENSOR)
    export = ["1" if sensed[-1] & (1 << i) else "0" for i in range(states)]
    verify_read(sr, SENSOR, ports, export)
    yield "sensor-export-known", action_key(export, ports, SENSOR)
    ports, states, full, guards = bool_table(wr, WRITER)
    requests = ["-"] * states
    eligible = full
    for row, guard in zip(wr, guards):
        a, b = row[3:]
        if a == b:
            continue
        selected = eligible & guard
        for i in range(states):
            if selected & (1 << i):
                requests[i] = f"{4*a+b:x}"
        eligible &= ~guard
    verify_read(wr, WRITER, ports, requests)
    yield "writer-first-request-known", action_key(requests, ports, WRITER)


def audit(manifest, out):
    data = json.loads(manifest.read_bytes().decode("utf-8-sig"))
    content = data["Content"]
    digest = hashlib.sha256(json.dumps(content, separators=(",", ":")).encode()).hexdigest().upper()
    if digest != data["LockHash"]:
        raise ValueError("Primary finalist content digest differs")
    winners = content["Winners"]
    groups = defaultdict(set)
    lookup = {}
    for winner in winners:
        key = f"{winner['Goal']}/{winner['Method']}/{winner['Replicate']:02}"
        if key in lookup:
            raise ValueError("Duplicate island")
        lookup[key] = winner
        for signature in set(signatures(winner["GenomeHex"])):
            groups[signature].add(key)
    out.mkdir(exist_ok=True, parents=True)
    fields = ["read", "signature", "winner_slots", "distinct_genomes",
              *METHODS, *[m+"_genomes" for m in METHODS], "goals", "members"]
    evidence = []
    for (kind, sig), members in sorted(groups.items()):
        by_method = {method: [lookup[k] for k in members if lookup[k]["Method"] == method]
                     for method in METHODS}
        record = {"read": kind, "signature": sig, "winner_slots": len(members),
                  "distinct_genomes": len({lookup[k]["GenomeHex"] for k in members}),
                  "goals": ",".join(sorted({lookup[k]["Goal"] for k in members})),
                  "members": " ".join(sorted(members))}
        for method, rows in by_method.items():
            record[method] = len(rows)
            record[method+"_genomes"] = len({w["GenomeHex"] for w in rows})
        evidence.append(record)
    with (out/"vocabulary-census.csv").open("w", encoding="utf-8", newline="") as stream:
        writer = csv.DictWriter(stream, fields)
        writer.writeheader()
        writer.writerows(evidence)
    report = {
        "input_sha256": hashlib.sha256(manifest.read_bytes()).hexdigest().upper(),
        "lock_hash": digest, "winner_slots": len(winners),
        "distinct_genomes": len({w["GenomeHex"] for w in winners}),
        "arm_counts": dict(Counter(w["Method"] for w in winners)),
        "physical": "program-bytes compares actual entire blocks. exact-owner compares homologous decoded fields, row order, frame and delay flags; these fields occupy several packed tables. exact-bindings includes the root and its recursively addressed input rows with physical row offsets. backed-frame additionally requires the same frame and delay flags. The writer guard comparison is conditional on reaching its priority, and includes its material roles.",
        "known": "sensor-export-known and writer-first-request-known enumerate known inputs to that owner. Binding names and ordered material roles are preserved. Fixed zero/one ports retain literal values. All other port values vary independently, including combinations infeasible in the world. Addresses that alias retain a single input. Inputs listed before '=' are low-to-high binary address bits; '-' means idle. Each writer character is the nibble 4*from+to, with roles [stock,current,previous,next].",
        "scope": "Known reads use owner-relative inputs, before frame orientation or optional delay on the wire. They describe one isolated sensor read or first material request on a fully observed tick. Missing/uncertain support, early refusal, budgets, blocked transfers and feedback across time may distinguish machines with matching known reads. Raw members list distinct finalists; *_genomes removes exact duplicate genotypes.",
        "scaffolding": "Open-loop always supplies the same inert sensor and disconnects wire inputs. Parameter supplies one back-longer/ahead-longer sensor and fixed writer bindings, priority and paired laws. All methods receive the port bank, mirror and ordinal span observations, two-owner interface, passage roles, priority and material law. This audit counts structures chosen within those priors. It does not identify a learned vocabulary, a causal benefit, or a reusable runtime instruction.",
        "byte_duplicates": [r for r in evidence if r["read"] == "program-bytes" and r["winner_slots"] > 1],
        "repeated_distinct_genome_signatures": {
            kind: sum(1 for row in evidence if row["read"] == kind and row["distinct_genomes"] >= 2)
            for kind in sorted({r["read"] for r in evidence})},
        "repeated_full_grammar_signatures": {
            kind: sum(1 for row in evidence if row["read"] == kind and
                      len({lookup[k]["GenomeHex"] for k in row["members"].split()
                           if lookup[k]["Method"] in ("evolve","random")}) >= 2)
            for kind in sorted({r["read"] for r in evidence})},
        "census_sha256": hashlib.sha256((out/"vocabulary-census.csv").read_bytes()).hexdigest().upper(),
    }
    examples = sorted(groups.get(("sensor-export-known","reflection=10"), set()))
    report["example_reflection_complement"] = {
        "read": "When reflection has a known bit, publish its complement. The two local circuits can differ under missing support, frame orientation, or multi-beat flow.",
        "members": [{"island": k, "genome": lookup[k]["GenomeHex"],
                     "flags": decode(lookup[k]["GenomeHex"])[0],
                     "bound_sensor_rows": decode(lookup[k]["GenomeHex"])[1]} for k in examples]}
    (out/"vocabulary-census.json").write_text(json.dumps(report, indent=2)+"\n", encoding="utf-8")
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("out", type=Path)
    args = parser.parse_args()
    audit(args.manifest, args.out)

"""The finite program carries its promises without source-oracle metadata."""
import copy
import unittest

from authored_line_art import alphabet_scene
from construction_alphabet import _construct, LETTERS
from construction_churches import _build_church, church_sources
from construction_resolution import connect_preserved_reads, _rough_plans
from construction_resolution_geometry import coarsen_scene
from verify_construction_ladders import evaluate


class ResolutionProgramTests(unittest.TestCase):
    def test_reflection_is_a_live_dependency_after_finite_selection(self):
        source = church_sources()[10]
        coarse, _ = coarsen_scene(source, 8)
        program = connect_preserved_reads(_build_church(coarse, _rough_plans(source["id"], 8)), source, coarse)
        binding = next(row for row in program["resolutionBindings"] if row["read"] == "reflection")
        before, deps, _ = evaluate(program)
        self.assertIn(binding["source"], deps[binding["site"]])
        anchor = next(s for s in program["sites"] if s["id"] == binding["axisSite"])
        anchor["x"]["numerator"] += 8
        after, _, _ = evaluate(program)
        self.assertNotEqual(after[binding["axisSite"]][0], before[binding["axisSite"]][0])
        self.assertEqual(after[binding["site"]][0] + after[binding["source"]][0],
                         2 * after[binding["axisSite"]][0])
        self.assertNotEqual(binding["site"], binding["source"])

    def test_replay_does_not_consult_fine_source_or_audit_metadata(self):
        source = alphabet_scene()
        coarse, _ = coarsen_scene(source, 8)
        program = connect_preserved_reads(_construct(coarse, LETTERS, True), source, coarse)
        expected = evaluate(program)
        stripped = {key: value for key, value in copy.deepcopy(program).items()
                    if key in {"id", "viewBox", "parameters", "sites", "edges", "rungs", "constraints", "repeats", "circular"}}
        self.assertEqual(evaluate(stripped), expected)
        self.assertEqual(len(stripped["repeats"]), 7)
        edge_map = {e["id"]: e for e in stripped["edges"]}
        sites = {s["id"]: s for s in stripped["sites"]}
        for repeat in stripped["repeats"]:
            for first, second in zip(repeat["source"], repeat["instances"][1]):
                for a, b in zip(edge_map[first]["controls"], edge_map[second]["controls"]):
                    for axis in ("x", "y"):
                        self.assertEqual(sites[b][axis]["origin"], a)

    def test_coincident_import_endpoints_remain_distinct_after_alignment_reads(self):
        source = next(s for s in church_sources() if s["id"] == "tabler-church")
        coarse, _ = coarsen_scene(source, 8)
        program = connect_preserved_reads(_build_church(coarse, _rough_plans(source["id"], 8)), source, coarse)
        mapped = program["sourceMapping"]["siteMap"]
        self.assertEqual(len(mapped), len(set(mapped.values())))
        pairs = [(a, b) for i, a in enumerate(source["sites"]) for b in source["sites"][i + 1:]
                 if (a["x"], a["y"]) == (b["x"], b["y"])]
        self.assertTrue(pairs)
        values, _, _ = evaluate(program)
        for a, b in pairs:
            self.assertNotEqual(mapped[a["id"]], mapped[b["id"]])
            self.assertEqual(values[mapped[a["id"]]], values[mapped[b["id"]]])


if __name__ == "__main__":
    unittest.main()

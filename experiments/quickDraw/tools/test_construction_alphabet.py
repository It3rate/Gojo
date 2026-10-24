"""Source parity and causal construction checks for the complete alphabet."""
from __future__ import annotations

import copy
import unittest

from authored_line_art import alphabet_scene
from construction_alphabet import LETTERS, build_alphabet
from verify_construction_ladders import evaluate


class AlphabetConstructionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scene = alphabet_scene()
        cls.programs = build_alphabet()
        cls.by_id = {p["id"]: p for p in cls.programs}

    def test_every_capital_has_an_independent_program_and_an_atlas_occurrence(self):
        singles = [p for p in self.programs if p["family"] == "alphabet-series"]
        self.assertEqual([p["variantLabel"] for p in singles], list(LETTERS))
        self.assertEqual(len({p["id"] for p in self.programs}), 27)
        self.assertEqual(sum(len(p["rungs"]) for p in self.programs), 108)
        atlas = self.by_id["alphabet-atlas"]
        self.assertEqual(atlas["viewBox"], self.scene["viewBox"])
        self.assertEqual(len(atlas["sourceMapping"]["edgeMap"]), 100)
        self.assertEqual(len(atlas["sourceMapping"]["siteMap"]), 118)
        self.assertEqual(set().union(*(set(p["sourceMapping"]["edgeMap"]) for p in singles)),
                         set(atlas["sourceMapping"]["edgeMap"]))

    def test_all_final_control_values_match_the_authoritative_source(self):
        for program in self.programs:
            with self.subTest(program=program["id"]):
                values, _, _ = evaluate(program)
                native_edges = {e["id"]: e for e in program["edges"]}
                mapping = program["sourceMapping"]
                for source_id, expected in mapping["expectedSites"].items():
                    self.assertEqual(values[mapping["siteMap"][source_id]], expected)
                for source_id, expected in mapping["expectedControls"].items():
                    edge = native_edges[mapping["edgeMap"][source_id]]
                    self.assertEqual([values[p] for p in edge["controls"]], expected)
                self.assertEqual(set(program["rungs"][-1]["visible"]),
                                 set(mapping["edgeMap"].values()))

    def test_source_endpoint_aliases_survive_every_incident_path(self):
        source_edges = {e["id"]: e for e in self.scene["edges"]}
        for program in self.programs:
            native_edges = {e["id"]: e for e in program["edges"]}
            mapping = program["sourceMapping"]
            self.assertEqual(len(set(mapping["siteMap"].values())), len(mapping["siteMap"]))
            for source_id, native_id in mapping["edgeMap"].items():
                source, native = source_edges[source_id], native_edges[native_id]
                self.assertEqual(native["controls"][0], mapping["siteMap"][source["from"]])
                self.assertEqual(native["controls"][-1], mapping["siteMap"][source["to"]])
        q = self.by_id["alphabet-q"]
        joint = q["sourceMapping"]["siteMap"]["Q-tail-joint"]
        final = set(q["rungs"][-1]["visible"])
        self.assertEqual(sum(joint in e["controls"] for e in q["edges"] if e["id"] in final), 4)

    def test_earlier_landmarks_are_consumed_and_absolute_sites_stay_at_page_basis(self):
        for program in self.programs:
            with self.subTest(program=program["id"]):
                evaluate(program)  # Rejects missing references and future-rung reads.
                sites = {s["id"]: s for s in program["sites"]}
                for source_id, source_site in program["sourceMapping"]["siteMap"].items():
                    self.assertEqual(sites[source_site]["at"], 2)
                    self.assertEqual(sites[source_site]["x"]["origin"], "coarse-site-" + source_id)
                    self.assertEqual(sites[source_site]["y"]["origin"], "coarse-site-" + source_id)
                literals = [s["id"] for s in sites.values()
                            if isinstance(s["x"], int) or isinstance(s["y"], int)]
                self.assertEqual(literals, ["page-origin", "page-extent"])
                for index, rung in enumerate(program["rungs"]):
                    self.assertFalse(set(rung["visible"]) & set(rung["guides"]))
                    prior = program["rungs"][index - 1]["visible"] if index else []
                    self.assertEqual(set(rung["selected"]), set(prior) - set(rung["visible"]))

    def test_coarse_patterns_are_approximations_and_curves_have_visible_bends(self):
        for letter in LETTERS:
            program = self.by_id["alphabet-" + letter.lower()]
            values, _, _ = evaluate(program)
            mapping = program["sourceMapping"]
            self.assertTrue(any(values["coarse-site-" + source_id] != expected
                                for source_id, expected in mapping["expectedSites"].items()), letter)
            self.assertNotEqual(set(program["rungs"][0]["visible"]),
                                set(program["rungs"][1]["visible"]))
        d = self.by_id["alphabet-d"]
        values, _, _ = evaluate(d)
        self.assertGreater(values["coarse-bend-D-bowl"][0], values["coarse-site-D-top"][0])
        self.assertEqual(len([e for e in d["edges"] if e["id"].startswith("coarse-path-D-bowl-")]), 2)

    def test_perturbing_earlier_mass_moves_the_later_letter_through_its_references(self):
        source = self.by_id["alphabet-a"]
        original, _, _ = evaluate(source)
        changed = copy.deepcopy(source)
        frame = next(s for s in changed["sites"] if s["id"] == "A-mass-frame-a")
        frame["x"]["numerator"] += 16
        moved, _, _ = evaluate(changed)
        final_sites = source["sourceMapping"]["siteMap"].values()
        self.assertTrue(any(original[site] != moved[site] for site in final_sites))
        self.assertEqual(source["edges"], changed["edges"])

    def test_source_order_is_retained_without_claiming_human_pen_order(self):
        atlas = self.by_id["alphabet-atlas"]
        mapping = atlas["sourceMapping"]
        self.assertEqual(mapping["sourceEdgeOrder"], [e["id"] for e in self.scene["edges"]])
        self.assertEqual(mapping["sourceStrokeOrder"], [s["edges"] for s in self.scene["strokes"]])
        self.assertIn("authored construction proposal", mapping["orderQualification"])
        for program in self.programs:
            self.assertEqual(program["constraints"], [])
            self.assertTrue(program["rungs"][-1]["guides"])


if __name__ == "__main__":
    unittest.main()

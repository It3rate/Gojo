"""Source correspondence checks for the complete church ladder repertoire."""
from __future__ import annotations

import unittest

from construction_churches import build_churches, church_sources
from verify_construction_ladders import evaluate


class ChurchConstructionTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.sources = {scene["id"]: scene for scene in church_sources()}
        cls.programs = {row["id"]: row for row in build_churches()}

    def test_all_fourteen_sources_finish_with_exact_controls_and_endpoint_identity(self):
        self.assertEqual({f"church-{i:02d}" for i in range(1, 13)} |
                         {"lucide-church", "tabler-church"}, set(self.programs))
        for key, program in self.programs.items():
            with self.subTest(key=key):
                positions, _, _ = evaluate(program)
                source = self.sources[key]
                mapping = program["sourceMapping"]
                edges = {row["id"]: row for row in program["edges"]}
                self.assertEqual(len(source["sites"]), len(set(mapping["siteMap"].values())))
                self.assertEqual(set(mapping["edgeMap"].values()), set(program["rungs"][-1]["visible"]))
                for edge in source["edges"]:
                    actual = edges[mapping["edgeMap"][edge["id"]]]
                    self.assertEqual(edge["controls"], [positions[site] for site in actual["controls"]])
                    self.assertEqual(mapping["siteMap"][edge["from"]], actual["controls"][0])
                    self.assertEqual(mapping["siteMap"][edge["to"]], actual["controls"][-1])

    def test_declared_repetitions_consume_the_actual_previous_controls(self):
        for key, program in self.programs.items():
            edges = {row["id"]: row for row in program["edges"]}
            sites = {row["id"]: row for row in program["sites"]}
            for repeat in program["repeats"]:
                with self.subTest(key=key, repeat=repeat["id"]):
                    source_edges, target_edges = repeat["instances"]
                    for source_id, target_id in zip(source_edges, target_edges):
                        self.assertLessEqual(edges[source_id]["at"], edges[target_id]["at"])
                        for source_site, target_site in zip(edges[source_id]["controls"], edges[target_id]["controls"]):
                            self.assertEqual(source_site, sites[target_site]["x"]["origin"])
                            self.assertEqual(source_site, sites[target_site]["y"]["origin"])

    def test_imported_coincident_sites_remain_distinct_and_guides_have_no_containment(self):
        tabler = self.programs["tabler-church"]
        mapping = tabler["sourceMapping"]
        # The source cross-stem ends at the same coordinate as the roof apex,
        # but separate SVG paths never asserted a shared joint.
        self.assertEqual(mapping["expectedSites"]["site-010"], mapping["expectedSites"]["site-014"])
        self.assertNotEqual(mapping["siteMap"]["site-010"], mapping["siteMap"]["site-014"])
        for program in self.programs.values():
            self.assertFalse(program["constraints"])
            self.assertTrue(all(frame["qualification"].startswith("rough positioning guide")
                                for frame in program["constructionFrames"]))
            for previous, current in zip(program["rungs"], program["rungs"][1:]):
                self.assertNotEqual(previous["visible"], current["visible"])


if __name__ == "__main__":
    unittest.main()

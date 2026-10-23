"""Construction guarantees for the authored line-art adapter."""
import copy
import unittest
import xml.etree.ElementTree as ET

import authored_line_art as art


def incidences(scene, site):
    return [e for e in scene["edges"] if site in (e["from"], e["to"])]


class AuthoredLineArtTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.churches = art.church_scenes()
        cls.diagnostics = art.diagnostics()

    def test_all_fixtures_have_valid_linked_construction(self):
        self.assertEqual((len(self.churches), len(self.diagnostics)), (12, 3))
        for scene in self.churches+self.diagnostics:
            self.assertTrue(art.validate(scene))
            used = {e[k] for e in scene["edges"] for k in ("from", "to")}
            self.assertEqual(used, {s["id"] for s in scene["sites"]})
            self.assertIn("not recorded human", scene["drawOrder"])

    def test_region_closure_requires_identity_not_coordinate_equality(self):
        scene = copy.deepcopy(self.churches[0])
        edge = scene["edges"][0]
        end = next(s for s in scene["sites"] if s["id"] == edge["to"])
        scene["sites"].append({**end, "id": "lookalike-site"})
        edge["to"] = "lookalike-site"
        with self.assertRaisesRegex(ValueError, "actual shared endpoint"):
            art.validate(scene)

    def test_shared_door_base_sites_have_actual_three_ray_incidence(self):
        for scene in self.churches:
            for site in ("outline-s1", "outline-s2"):
                edges = incidences(scene, site)
                self.assertEqual(len(edges), 3)
                self.assertTrue(any(e["id"].startswith("door-") for e in edges))

    def test_rope_contact_retains_distinct_objects_and_sites(self):
        scene = self.diagnostics[0]
        sites = {s["id"]: s for s in scene["sites"]}
        contact = scene["contacts"][0]["sites"]
        self.assertNotEqual(*contact)
        self.assertEqual((sites[contact[0]]["x"], sites[contact[0]]["y"]),
                         (sites[contact[1]]["x"], sites[contact[1]]["y"]))
        self.assertEqual([len(incidences(scene, s)) for s in contact], [2, 1])
        branch, rope = scene["objects"]
        self.assertFalse(set(branch["edges"]) & set(rope["edges"]))
        self.assertEqual(scene["crossings"], [])

    def test_projected_crossing_does_not_create_an_incident_site(self):
        scene = self.diagnostics[1]
        crossing = scene["crossings"][0]
        edges = {e["id"]: e for e in scene["edges"]}
        a, b = (edges[e] for e in crossing["edges"])
        self.assertFalse({a["from"], a["to"]} & {b["from"], b["to"]})
        self.assertFalse(any([s["x"], s["y"]] == crossing["site"] for s in scene["sites"]))
        self.assertEqual(len(incidences(scene, "joined-center")), 4)

    def test_o_is_closed_cycle_with_smooth_two_edge_seams(self):
        scene = self.diagnostics[2]
        row = next(r for r in scene["regions"] if r["id"] == "O-loop-region")
        self.assertEqual(len(row["boundary"]), 4)
        for i in range(4):
            attached = incidences(scene, f"O-loop-s{i}")
            self.assertEqual(len(attached), 2)
            rays = []
            for edge in attached:
                controls = edge["controls"] if edge["from"] == f"O-loop-s{i}" else edge["controls"][::-1]
                rays.append([controls[1][d]-controls[0][d] for d in (0, 1)])
            self.assertEqual(rays[0][0]*rays[1][1]-rays[0][1]*rays[1][0], 0)
            self.assertLess(sum(a*b for a, b in zip(*rays)), 0)

    def test_k_has_exactly_one_opposite_pair(self):
        scene = self.diagnostics[2]
        attached = incidences(scene, "K-center")
        rays = [[e["controls"][-1][d]-e["controls"][0][d] for d in (0, 1)] for e in attached]
        pairs = [(a, b) for i, a in enumerate(rays) for b in rays[i+1:]
                 if a[0]*b[1]-a[1]*b[0] == 0 and sum(x*y for x, y in zip(a, b)) < 0]
        self.assertEqual((len(attached), len(pairs)), (4, 1))

    def test_mirror_and_repeat_claims_are_exact_not_visual_guesses(self):
        self.assertTrue(any(r["kind"] == "repeat" for s in self.churches for r in s["correspondences"]))
        scene = copy.deepcopy(self.churches[0])
        edge = next(e for e in scene["edges"] if e["id"] == "window-left-e1")
        edge["controls"][1][0] += 1
        with self.assertRaisesRegex(ValueError, "exact control comparison"):
            art.validate(scene)

    def test_all_edges_have_explicit_object_and_template_or_detail_scope(self):
        for scene in self.churches+self.diagnostics:
            edge_ids = {e["id"] for e in scene["edges"]}
            for scope in ("objects", "groups"):
                memberships = [e for row in scene[scope] for e in row["edges"]]
                self.assertEqual(set(memberships), edge_ids)
                self.assertEqual(len(memberships), len(edge_ids))
            self.assertTrue(all(g["role"] in ("template", "detail") for g in scene["groups"]))

    def test_plain_svg_contains_only_unfilled_black_source_geometry(self):
        for scene in self.churches+self.diagnostics:
            root = ET.fromstring(art.svg(scene))
            ns = {"s": "http://www.w3.org/2000/svg"}
            group = root.find("s:g", ns)
            self.assertEqual(group.attrib["fill"], "none")
            self.assertEqual(group.attrib["stroke"], "#000")
            self.assertEqual(len(root.findall(".//s:path", ns)), len(scene["edges"]))
            self.assertEqual(root.findall(".//s:text", ns), [])


if __name__ == "__main__":
    unittest.main()

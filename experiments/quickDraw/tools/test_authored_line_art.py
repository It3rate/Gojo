"""Construction guarantees for the authored line-art adapter."""
import copy
import string
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
        # Retained as a historical distinction test, outside the active collection.
        scene = art.rope_branch_diagnostic()
        sites = {s["id"]: s for s in scene["sites"]}
        contact = scene["contacts"][0]["sites"]
        self.assertNotEqual(*contact)
        self.assertEqual((sites[contact[0]]["x"], sites[contact[0]]["y"]),
                         (sites[contact[1]]["x"], sites[contact[1]]["y"]))
        self.assertEqual([len(incidences(scene, s)) for s in contact], [2, 1])
        branch, rope = scene["objects"]
        self.assertFalse(set(branch["edges"]) & set(rope["edges"]))
        self.assertEqual(scene["crossings"], [])

    def test_active_alphabet_has_every_letter_once_with_separate_connected_scope(self):
        scene = self.diagnostics[0]
        self.assertEqual(scene["id"], "diagnostic-alphabet")
        self.assertNotIn("diagnostic-rope-branch", [s["id"] for s in self.diagnostics])
        self.assertEqual([o["label"] for o in scene["objects"]], list(string.ascii_uppercase))
        edges = {e["id"]: e for e in scene["edges"]}
        groups = {g["label"]: set(g["edges"]) for g in scene["groups"]}
        used_sites = set()
        expected_cycles = dict(A=1, B=2, D=1, O=1, P=1, Q=1, R=1)
        for obj in scene["objects"]:
            letter = obj["label"]
            members = [edges[e] for e in obj["edges"]]
            sites = {e[k] for e in members for k in ("from", "to")}
            self.assertFalse(used_sites & sites, letter)
            used_sites.update(sites)
            reached, pending = set(), [next(iter(sites))]
            while pending:
                site = pending.pop()
                if site in reached:
                    continue
                reached.add(site)
                for edge in members:
                    if site in (edge["from"], edge["to"]):
                        pending.extend((edge["from"], edge["to"]))
            self.assertEqual(reached, sites, letter)
            self.assertEqual(len(members)-len(sites)+1, expected_cycles.get(letter, 0), letter)
            self.assertEqual(groups[f"Letter {letter}"], set(obj["edges"]))
            counters = [r for r in scene["regions"] if all(b["edge"] in obj["edges"] for b in r["boundary"])]
            self.assertEqual(len(counters), expected_cycles.get(letter, 0), letter)
            self.assertTrue(all(not r["figure"] for r in counters))
        self.assertEqual(used_sites, {s["id"] for s in scene["sites"]})
        self.assertEqual(len(scene["regions"]), 8)

    def test_alphabet_junctions_are_actual_tangent_incidence(self):
        scene = self.diagnostics[0]
        # These expected degree/opposed-pair counts are construction obligations,
        # independent of the display names or any native classifier dispatch.
        expectations = {
            "A-bar-left": (3, 1), "A-bar-right": (3, 1), "B-waist": (4, 1),
            "H-left-middle": (3, 1), "H-right-middle": (3, 1),
            "I-top-middle": (3, 1), "I-bottom-middle": (3, 1),
            "K-joint": (4, 1), "P-middle": (3, 1), "Q-tail-joint": (4, 2),
            "R-middle": (4, 1), "T-joint": (3, 1), "X-joint": (4, 2), "Y-joint": (3, 0),
            **{f"O-{part}": (2, 1) for part in ("top", "right", "bottom", "left")},
        }
        for site, expected in expectations.items():
            rays = []
            for edge in incidences(scene, site):
                controls = edge["controls"] if edge["from"] == site else edge["controls"][::-1]
                rays.append([controls[1][d]-controls[0][d] for d in (0, 1)])
            opposed = 0
            for i, a in enumerate(rays):
                for b in rays[i+1:]:
                    if a[0]*b[1] == a[1]*b[0]:
                        dot = sum(x*y for x, y in zip(a, b))
                        self.assertLess(dot, 0, f"{site}: no cooriented duplicate ray")
                        opposed += 1
            self.assertEqual((len(rays), opposed), expected, site)

    def test_q_split_preserves_the_oval_and_shares_tail_junction_identity(self):
        from fractions import Fraction

        scene = self.diagnostics[0]
        edges = {e["id"]: e for e in scene["edges"]}
        original = edges["O-lower-right"]["controls"]
        first = edges["Q-lower-right-before"]["controls"]
        second = edges["Q-lower-right-after"]["controls"]
        offset = [first[0][d]-original[0][d] for d in (0, 1)]

        def evaluate(controls, t):
            current = [[Fraction(v) for v in p] for p in controls]
            while len(current) > 1:
                current = [[(1-t)*a[d]+t*b[d] for d in (0, 1)] for a, b in zip(current, current[1:])]
            return current[0]

        # Exact rational samples distinguish subdivision from a visually similar arc.
        for step in range(17):
            t = Fraction(step, 16)
            expected = [v+delta for v, delta in zip(evaluate(original, t), offset)]
            observed = evaluate(first, 2*t) if t <= Fraction(1, 2) else evaluate(second, 2*t-1)
            self.assertEqual(observed, expected)
        connected = {e["id"] for e in incidences(scene, "Q-tail-joint")}
        self.assertEqual(connected, {"Q-lower-right-before", "Q-lower-right-after", "Q-tail-inner", "Q-tail-outer"})
        loop = next(r for r in scene["regions"] if r["id"] == "Q-counter-1")
        self.assertFalse({b["edge"] for b in loop["boundary"]} & {"Q-tail-inner", "Q-tail-outer"})

    def test_alphabet_viewport_is_preserved_and_used_for_control_validation(self):
        scene = self.diagnostics[0]
        self.assertEqual(ET.fromstring(art.svg(scene)).attrib["viewBox"], "0 0 1200 1000")
        self.assertGreater(max(s["x"] for s in scene["sites"]), 512)
        self.assertGreater(max(s["y"] for s in scene["sites"]), 512)
        self.assertTrue(art.validate(scene))
        clipped = copy.deepcopy(scene)
        edge = next(e for e in clipped["edges"] if len(e["controls"]) > 2)
        edge["controls"][1][0] = 1201
        with self.assertRaisesRegex(ValueError, "declared integer viewport"):
            art.validate(clipped)

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

"""Focused structural and rejection checks for the intentionally bounded import."""
import math
import unittest

from import_line_art_svg import arc_cubics, import_svg, parse_path


def svg(paths, extra="", fill="none"):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" '
            f'fill="{fill}" stroke="black" {extra}>' +
            "".join(f'<path d="{path}"/>' for path in paths) + '</svg>').encode()


class BoundedSvgImportTests(unittest.TestCase):
    def test_coordinate_equality_does_not_weld_independent_subpaths(self):
        scene = import_svg(svg(["M0 0L12 0", "M12 0L12 12M12 12L0 12"]), "test")
        a, b, c = scene["edges"]
        self.assertNotEqual(a["to"], b["from"])
        self.assertNotEqual(b["to"], c["from"])
        self.assertEqual(a["controls"][-1], b["controls"][0])
        self.assertEqual(len(scene["strokes"]), 3)
        for key in ("objects", "regions", "groups", "correspondences", "crossings", "contacts"):
            self.assertEqual(scene[key], [])

    def test_only_close_command_asserts_closed_site_identity(self):
        scene = import_svg(svg(["M0 0L12 0L0 0", "M0 0L12 0Z"]), "test")
        a, b, c, d = scene["edges"]
        self.assertNotEqual(a["from"], b["to"])
        self.assertEqual(c["from"], d["to"])
        self.assertEqual(a["to"], b["from"])
        self.assertEqual(c["to"], d["from"])

    def test_relative_moves_implicit_lines_and_document_order(self):
        parsed = parse_path("m2 3 4 0h2v-2m1 1l2 2")
        self.assertEqual([x["op"] for x in parsed], ["M", "L", "H", "V", "M", "L"])
        self.assertEqual(parsed[-1]["controls"], [(9.0, 2.0), (11.0, 4.0)])
        scene = import_svg(svg(["M2 3L6 3", "M1 1L2 2"]), "test")
        self.assertEqual([x["svgPathIndex"] for x in scene["source"]["edgeCorrespondence"]], [0, 1])
        self.assertEqual(scene["edges"][0]["controls"][0], [43, 64])

    def test_arc_quarter_circle_and_reverse_preserve_direction(self):
        curve, = arc_cubics((1, 0), (0, 1), 1, 1, 0, 0, 1)
        k = 4 / 3 * math.tan(math.pi / 8)
        expected = [(1, 0), (1, k), (k, 1), (0, 1)]
        for actual, target in zip(curve, expected):
            for a, b in zip(actual, target):
                self.assertAlmostEqual(a, b, places=12)
        reverse, = arc_cubics((0, 1), (1, 0), 1, 1, 0, 0, 0)
        for actual, target in zip(reverse, reversed(curve)):
            for a, b in zip(actual, target):
                self.assertAlmostEqual(a, b, places=12)

    def test_arc_radii_correction_zero_radius_and_degenerate_end(self):
        curves = arc_cubics((0, 0), (4, 0), 1, 1, 0, 0, 1)
        self.assertEqual(len(curves), 2)
        self.assertEqual(curves[0][0], (0, 0))
        self.assertEqual(curves[-1][-1], (4, 0))
        self.assertAlmostEqual(curves[0][-1][0], 2)
        self.assertAlmostEqual(curves[0][-1][1], -2)
        self.assertEqual(arc_cubics((0, 0), (4, 0), 0, 1, 0, 0, 1), [[(0, 0), (4, 0)]])
        self.assertEqual(arc_cubics((0, 0), (0, 0), 1, 1, 0, 0, 1), [])

    def test_subdivision_continuity_is_explicit_and_controls_match_sites(self):
        scene = import_svg(svg(["M10 18a2 2 0 0 1 4 0"]), "test")
        self.assertEqual(len(scene["edges"]), 2)
        self.assertEqual(scene["edges"][0]["to"], scene["edges"][1]["from"])
        sites = {x["id"]: [x["x"], x["y"]] for x in scene["sites"]}
        for edge in scene["edges"]:
            self.assertEqual(edge["controls"][0], sites[edge["from"]])
            self.assertEqual(edge["controls"][-1], sites[edge["to"]])

    def test_rejects_hidden_fill_transform_unsupported_geometry(self):
        for data in [svg(["M0 0L1 1"], fill="black"),
                     svg(["M0 0L1 1"], extra='transform="translate(4,0)"'),
                     svg(["M0 0L1 1"], extra='opacity="0"'),
                     svg(["M0 0C1 1 2 2 3 3"]),
                     svg(["M0 0L1 1"]).replace(b'<path', b'<circle')]:
            with self.subTest(data=data), self.assertRaises(ValueError):
                import_svg(data, "test")

    def test_rejects_malformed_commands_and_nonbinary_arc_flags(self):
        for path in ["L1 1", "M0", "M0 0A1 1 0 2 0 3 3", "M0 0L1@2", "M0 0Z1 1", "M0 0L1e999 0"]:
            with self.subTest(path=path), self.assertRaises(ValueError):
                parse_path(path)


if __name__ == "__main__":
    unittest.main()

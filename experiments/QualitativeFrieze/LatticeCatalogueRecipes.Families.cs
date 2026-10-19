namespace Gojo.Experiments.QualitativeFrieze;

public sealed partial class LatticeCatalogueRecipes
{
    private Specification Describe(int family, int v)
    {
        var k = v + 1;
        return family switch
        {
            0 => new("Paired boxes", $"Unequal paired boxes · split {k}", "L00",
                "Two closed box participants replace four independent side lines. Their unequal widths alternate along each course; actual endpoint ports join their common side.",
                (16, 0), (0, 8), () =>
                {
                    var s = new Sketch(a);
                    s.Coarse(s.Polygon((0, 0), (k, 0), (16, 0), (16, 8), (k, 8), (0, 8)), false);
                    s.Polygon((0, 0), (k, 0), (k, 8), (0, 8));
                    s.Polygon((k, 0), (16, 0), (16, 8), (k, 8)); return s;
                }),
            1 => new("Shifted courses", $"Released running bond · slip {k}", "L03",
                "The old side constraints are released, then alternate course joints are installed at a shared but unmeasured displacement. The courses expose the shifted contacts as real vertices.",
                (16, 0), (0, 8), () =>
                {
                    var s = new Sketch(a);
                    s.Line((0, 0), (k, 0), (16, 0)); s.Line((0, 4), (k, 4), (16, 4));
                    s.Line((0, 8), (k, 8), (16, 8));
                    s.Line((0, 0), (0, 4)); s.Line((k, 4), (k, 8)); return s;
                }),
            2 => new("Merged chambers", $"Alternating merged chambers · gap {k}", "L03",
                "One joint is absent on the lower course and two unequal subchambers replace the upper course. Merging and splitting are explicit path withdrawal and insertion, not a display-only omission.",
                (16, 0), (0, 8), () =>
                {
                    var s = new Sketch(a);
                    s.Line((0, 0), (k, 0), (8, 0), (16, 0));
                    s.Line((0, 4), (k, 4), (8, 4), (16, 4));
                    s.Line((0, 8), (k, 8), (8, 8), (16, 8));
                    s.Line((0, 0), (0, 4)); s.Line((k, 4), (k, 8)); s.Line((8, 4), (8, 8)); return s;
                }),
            3 => new("Triangular chambers", $"Alternating diagonal chambers · inset {k}", "L06",
                "A middle vertical partition and opposed diagonal ties subdivide neighbouring chambers. Every tie ends at an advertised side contact, so triangular and four-sided cells coexist.",
                (16, 0), (0, 8), () =>
                {
                    var s = new Sketch(a);
                    s.Polygon((0, 0), (8, 0), (16, 0), (16, k), (16, 8), (8, 8), (0, 8), (0, k));
                    s.Line((8, 0), (8, k), (8, 8));
                    s.Line((0, 0), (8, k)); s.Line((8, k), (16, 0)); return s;
                }),
            4 => new("Offset crossed chambers", $"Four-way offset chamber · hub {k}", "L07",
                "The diagonal families meet at one explicitly shared hub which slides inside the cell. The boundary still exposes the same four corners, while unequal triangular regions make this more than an affine recolouring.",
                (12, 0), (0, 12), () =>
                {
                    var s = new Sketch(a); s.Polygon((0, 0), (12, 0), (12, 12), (0, 12));
                    s.Line((0, 0), (k, 5), (12, 12)); s.Line((12, 0), (k, 5), (0, 12)); return s;
                }),
            5 => new("Chamfered cells", $"Octagons and diamond gaps · cut {v}", "L12",
                "The selected square corners are replaced by diagonal bridge chords. The five cut depths keep the period fixed, changing the relation between the octagonal cells and their interstitial diamond gaps.",
                (14, 0), (0, 14), () =>
                {
                    var s = new Sketch(a); s.Polygon((v, 0), (14 - v, 0), (14, v), (14, 14 - v),
                        (14 - v, 14), (v, 14), (0, 14 - v), (0, v)); return s;
                }),
            6 => new("Complementary keyed cells", $"Complementary keyed tile · tooth {v}", "L00",
                "Opposite boundaries reuse the same stepped profile through a translation. A protrusion on one cell is the neighbouring cell's recess; the box constraint is reformulated rather than merely augmented.",
                (16, 0), (0, 12), () =>
                {
                    var s = new Sketch(a); s.Polygon((0, 0), (5, 0), (5, v), (10, v), (10, 0), (16, 0),
                        (16, 4), (16 - v, 4), (16 - v, 8), (16, 8), (16, 12),
                        (10, 12), (10, 12 + v), (5, 12 + v), (5, 12), (0, 12),
                        (0, 8), (-v, 8), (-v, 4), (0, 4)); return s;
                }),
            7 => new("Zigzag courses", $"Joined zigzag courses · rise {v}", "L05",
                "Each horizontal course becomes two opposed oblique chords. Vertical connectors join their endpoints across courses; the rise changes without changing the repetition words.",
                (12, 0), (0, 8), () =>
                {
                    var s = new Sketch(a); s.Line((0, 0), (6, v), (12, 0));
                    s.Line((0, 0), (0, 8)); return s;
                }),
            8 => new("Stepped courses", $"Stepped interlocking courses · rise {v}", "L00",
                "A course is rewritten as horizontal advances separated by up and down steps. No immediate reversal occurs: a perpendicular segment separates opposed horizontal or vertical motion.",
                (16, 0), (0, 8), () =>
                {
                    var s = new Sketch(a); s.Line((0, 0), (2, 0), (4, 0), (4, v), (8, v), (8, 0), (12, 0), (12, -v), (16, -v), (16, 0));
                    // Join from the middle of a horizontal run. A connector
                    // at the course endpoint would share only part of the
                    // next row's last vertical span without a declared split.
                    s.Line((2, 0), (2, 8)); return s;
                }),
            9 => new("Square-wave chambers", $"Square-wave chambers · plateau {k}", "L00",
                "The course separates horizontal and vertical action into alternating runs. The plateau width changes independently of its height, leaving a family of unequal but repeating stepped compartments.",
                (16, 0), (0, 8), () =>
                {
                    var s = new Sketch(a); s.Line((0, 0), (k, 0), (k, 4), (14 - k, 4), (14 - k, 0), (16, 0));
                    s.Line((0, 0), (0, 8)); return s;
                }),
            10 => Herringbone(v),
            11 => Honeycomb(v),
            12 => Trihexagonal(v),
            13 => LozengeTriads(v),
            14 => Stars(v),
            15 => Frets(v),
            16 => Ribbons(v),
            17 => InsetDiamonds(v),
            18 => HexagonFans(v),
            19 => Basket(v),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
    }

    private Specification Herringbone(int v)
    {
        var u = v + 1; const int t = 2;
        return new("Herringbone assemblies", $"Herringbone domino assembly · aspect {u}", "L14",
            "Four rectangle images form a herringbone fundamental cell. Horizontal and vertical orientation alternate through actual quarter-turn substitutions and translation receipts; the two oblique periods repeat the assembled topology.",
            (2 * u, 2 * t), (2 * u, -2 * t), () =>
            {
                var s = new Sketch(a); var prototype = s.Polygon((0, 0), (1, 0), (2, 0), (2, 1), (1, 1), (0, 1));
                s.Paths.Remove(prototype); s.Closed.Remove(prototype);
                var horizontal = a.Map(a.Word(u, 0), a.Word(0, t));
                var vertical = a.Map(a.Word(0, t), a.Word(-u, 0));
                s.Image(prototype, horizontal, (0, 0), true);
                s.Image(prototype, horizontal, (u, -t), true);
                s.Image(prototype, vertical, (2 * u, t), true);
                s.Image(prototype, vertical, (3 * u, 0), true); return s;
            });
    }

    private Specification Honeycomb(int v)
    {
        var width = 3 + v; const int flank = 2; const int halfHeight = 3;
        return new("Honeycomb cells", $"Six-sided honeycomb · straight span {width}", "L06",
            "A six-edge contour replaces the triangular focus. Opposite side words provide the repeating neighbours, preserving a six-sided cell topology while the horizontal span varies independently of the slanted flanks.",
            (width + flank, halfHeight), (0, 2 * halfHeight), () =>
            {
                var s = new Sketch(a); s.Polygon((0, 0), (width, 0), (width + flank, halfHeight),
                    (width, 2 * halfHeight), (0, 2 * halfHeight), (-flank, halfHeight)); return s;
            });
    }

    private Specification Trihexagonal(int v)
    {
        var flank = v + 1; const int h = 4;
        return new("Hexagons and triangular gaps", $"Vertex-linked hexagons · flank {flank}", "L06",
            "Six-edge contours meet at their vertices rather than sharing every side. Three-sided gaps emerge between neighbouring contours; the diagonal reach changes independently of the vertical component.",
            (4 * flank, 0), (2 * flank, 2 * h), () =>
            {
                var s = new Sketch(a); s.Polygon((0, 0), (2 * flank, 0), (3 * flank, h),
                    (2 * flank, 2 * h), (0, 2 * h), (-flank, h)); return s;
            });
    }

    private Specification LozengeTriads(int v)
    {
        var w = v + 3; const int f = 2; const int h = 4;
        return new("Three-chamber lozenges", $"Three-part honeycomb chamber · centre {v}", "L06",
            "Three four-sided compartments reuse one central landmark and alternating outer corners. The central landmark shifts inside the hexagonal envelope without changing the envelope periods; all three siblings publish that same meeting position.",
            (w + f, h), (0, 2 * h), () =>
            {
                var s = new Sketch(a); var centre = (X: v, Y: h);
                var p = new[] { (0, 0), (w, 0), (w + f, h), (w, 2 * h), (0, 2 * h), (-f, h) };
                s.Coarse(s.Polygon(p), false);
                for (var i = 0; i < 6; i += 2) s.Polygon(centre, p[i], p[(i + 1) % 6], p[(i + 2) % 6]);
                return s;
            });
    }

    private Specification Stars(int v)
    {
        const int r = 10; var n = v + 1; const int d = 7;
        return new("Star and cross compartments", $"Eight-point star network · neck {n}", "L12",
            "A fourfold star contour replaces the chamfered boundary. Cardinal tips meet neighbours through actual exposed endpoints, while diagonal points and variable necks create a second family of negative compartments.",
            (2 * r, 0), (0, 2 * r), () =>
            {
                var s = new Sketch(a); s.Polygon((0, r), (n, d), (d, d), (d, n), (r, 0), (d, -n), (d, -d), (n, -d),
                    (0, -r), (-n, -d), (-d, -d), (-d, -n), (-r, 0), (-d, n), (-d, d), (-n, d)); return s;
            });
    }

    private Specification Frets(int v)
    {
        var depth = v + 2;
        return new("Detached returned frets", $"Detached opposed frets · return {depth}", "L09",
            "An open returned fret and its mirror repeat as separate strands. Their disconnection is intentional, not an unproven join; opposing horizontal motion is separated by vertical turns, so no line immediately retraces itself.",
            (20, 0), (0, 12), () =>
            {
                var s = new Sketch(a); var p = s.Line((0, 0), (0, 8), (10, 8), (10, 2), (depth, 2), (depth, 6), (8, 6));
                s.Image(p, a.Map(a.Word(-1, 0), a.Word(0, -1)), (18, 8), false); return s;
            });
    }

    private Specification Ribbons(int v)
    {
        var lift = v + 1;
        return new("Interleaved returning ribbons", $"Returning oblique ribbon · lift {lift}", "L10",
            "A long returning strand combines diagonal runs with offset plateaus. Its second image is a translated sibling; the two strands remain distinct where projected crossings have no shared endpoint role.",
            (16, 0), (0, 16), () =>
            {
                var s = new Sketch(a); var p = s.Line((0, 0), (4, lift), (4, lift + 3), (8, lift + 3),
                    (8, lift), (12, 0), (12, -3), (16, -3), (16, 0));
                s.Copy(p, (0, 8)); return s;
            });
    }

    private Specification InsetDiamonds(int v)
    {
        const int r = 9; var n = v + 1;
        return new("Nested diamond chambers", $"Nested diamond chambers · inset reach {n}", "L07",
            "An inner diamond is connected to the four actual cardinal vertices of its outer cell. The inner reach varies at a fixed period; the four surrounding quadrilateral compartments stay connected through public landmark roles.",
            (2 * r, 0), (r, r), () =>
            {
                var s = new Sketch(a); s.Coarse(s.Polygon((0, r), (r, 0), (0, -r), (-r, 0)), true);
                s.Polygon((0, n), (n, 0), (0, -n), (-n, 0));
                s.Line((0, r), (0, n)); s.Line((r, 0), (n, 0));
                s.Line((0, -r), (0, -n)); s.Line((-r, 0), (-n, 0)); return s;
            });
    }

    private Specification HexagonFans(int v)
    {
        const int width = 8; const int f = 3; const int h = 6;
        return new("Six-way chamber fans", $"Six-way honeycomb fan · hub {v}", "L06",
            "The six-sided envelope is retained as a compartment boundary and six spokes meet at one interior hub. Moving that hub changes the fine triangular cells while the coarser honeycomb remains an exact retained construction.",
            (width + f, h), (0, 2 * h), () =>
            {
                var s = new Sketch(a); var points = new[] { (0, 0), (width, 0), (width + f, h),
                    (width, 2 * h), (0, 2 * h), (-f, h) };
                s.Coarse(s.Polygon(points), true); foreach (var p in points) s.Line(p, (v + 1, h)); return s;
            });
    }

    private Specification Basket(int v)
    {
        var shortSide = v + 1; var longSide = shortSide + 4; var pitch = (longSide + shortSide) / 2;
        return new("Basket rectangle assemblies", $"Alternating basket rectangles · gap {shortSide}", "L14",
            "Four translated images of one long rectangle alternate horizontal and vertical orientation. Their split side contacts bound square gaps; the gap-to-bar proportion changes through actual repeated reach participation.",
            (2 * pitch, 0), (0, 2 * pitch), () =>
            {
                var s = new Sketch(a);
                var p = s.Polygon((0, 0), (2, 0), (shortSide + 2, 0), (longSide, 0),
                    (longSide, shortSide), (shortSide + 2, shortSide), (2, shortSide), (0, shortSide));
                s.Copy(p, (pitch, pitch), true);
                var quarter = a.Map(a.Word(0, 1), a.Word(-1, 0));
                s.Image(p, quarter, (2 * pitch, -2), true);
                s.Image(p, quarter, (pitch, shortSide), true); return s;
            });
    }
}

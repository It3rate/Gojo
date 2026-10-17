"""Authored line-only reference drawings; these are not Gojo execution.

Coordinates choose one readable illustration of each skeleton.  They are not
constraints on a future uncalibrated structural implementation.
"""

import math


def _square(x, y, half):
    return [(x - half, y - half), (x + half, y - half),
            (x + half, y + half), (x - half, y + half)]


def _midpoint(a, b):
    return ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2)


def nested_courts(c):
    """An axial field, diamond mediation, and a smaller axial court."""
    xs, ys, half = (82, 240, 398), (73, 187), 43
    for y in ys:
        c.line((25, y), (455, y), role='guide', width=1)
    for x in xs:
        c.line((x, 20), (x, 240), role='guide', width=1)

    # Bridges terminate at the actual diamond/square shared sites.
    for y in ys:
        for left, right in zip(xs, xs[1:]):
            c.line((left + half, y), (right - half, y), width=2)
    for x in xs:
        c.line((x, ys[0] + half), (x, ys[1] - half), width=2)

    for y in ys:
        for x in xs:
            diamond = [(x, y - half), (x + half, y),
                       (x, y + half), (x - half, y)]
            c.poly(_square(x, y, half), closed=True, width=2)
            c.poly(diamond, role='second', closed=True, width=2)
            c.poly(_square(x, y, half / 2), closed=True, width=2)
            # The four inner corners lie on diamond edges, not at its tips.
            for point in diamond:
                c.dot(point, r=2.5)


def triangular_chambers(c):
    """Seven parent triangles, each exposing the same middle subdivision."""
    side = 108
    height = side * math.sqrt(3) / 2
    bottom = 230
    parents = [(x, bottom) for x in (78, 186, 294, 402)]
    parents += [(x, bottom - height) for x in (132, 240, 348)]

    for x, base in parents:
        top = (x, base - height)
        left, right = (x - side / 2, base), (x + side / 2, base)
        a, b, d = _midpoint(top, left), _midpoint(left, right), _midpoint(right, top)
        c.poly([top, left, right], closed=True, width=2.2)
        c.poly([a, b, d], role='second', closed=True, width=1.9)

        # One unselected, finer partition keeps the hierarchy inspectable
        # without turning every available scaffold edge into the motif.
        if x == 240:
            child = (top, a, d)
            c.poly([_midpoint(child[0], child[1]),
                    _midpoint(child[1], child[2]),
                    _midpoint(child[2], child[0])],
                   role='guide', closed=True, width=1)

    # Neighbouring parent chambers really meet at these shared vertices.
    for x in (132, 240, 348):
        c.dot((x, bottom - height), r=2.8)
    for x in (132, 240, 348):
        c.dot((x, bottom), r=2.5)


def herringbone(c):
    """A rotated domino skeleton, using a consistent herringbone tiling.

    Each repeated pair contains a 2-by-1 bar and a 1-by-2 bar.  Translation
    vectors (2, 2) and (-1, 1) tile the integer scaffold without overlaps;
    their four cells have distinct (x+y) mod 4 classes.  The geometry is a
    reference illustration, not a numeric model used by the experiment.
    """
    step = 34 / math.sqrt(2)

    def point(x, y):
        return (240 + step * (x - y), 46 + step * (x + y))

    edges = {}
    joints = set()
    for i in range(-2, 4):
        for j in range(-10, 11):
            dx, dy = 2 * i - j, 2 * i + j
            bars = [([(0, 0), (2, 0), (2, 1), (0, 1)], 'ink'),
                    ([(2, 0), (3, 0), (3, 2), (2, 2)], 'second')]
            for vertices, role in bars:
                poly = [point(x + dx, y + dy) for x, y in vertices]
                if not all(23 <= x <= 457 and 19 <= y <= 241 for x, y in poly):
                    continue
                for index, (a, b) in enumerate(zip(poly, poly[1:] + poly[:1])):
                    # Repeated shared outlines are drawn once.  Exact lattice
                    # endpoints, rather than rounded screen proximity, key them.
                    ia = vertices[index]
                    ib = vertices[(index + 1) % len(vertices)]
                    key = tuple(sorted(((ia[0] + dx, ia[1] + dy),
                                        (ib[0] + dx, ib[1] + dy))))
                    edges.setdefault(key, (a, b, role))
                joints.add((2 + dx, 1 + dy))
    for a, b, role in edges.values():
        c.line(a, b, role=role, width=2)
    # Sparse declared T sites show bar interlocking rather than adding texture.
    for x, y in sorted(joints):
        px, py = point(x, y)
        if 105 < px < 375 and 75 < py < 185 and (x + y) % 4 == 3:
            c.dot((px, py), r=2.5)


def star_corridors(c):
    """Two staggered rows of sixfold courts joined at radial tips."""
    spacing = 120
    row_height = spacing * math.sqrt(3) / 2
    top = 130 - row_height / 2
    bottom = 130 + row_height / 2
    centres = [(100, top), (220, top), (340, top),
               (160, bottom), (280, bottom), (400, bottom)]
    outer, inner = 36, 18
    links = []
    for i, a in enumerate(centres):
        for b in centres[i + 1:]:
            if abs(math.hypot(b[0] - a[0], b[1] - a[1]) - spacing) < 0.001:
                links.append((a, b))
                c.line(a, b, role='guide', width=1)
    for x, y in centres:
        points = []
        for k in range(12):
            angle = k * math.pi / 6
            radius = outer if k % 2 == 0 else inner
            points.append((x + radius * math.cos(angle),
                           y + radius * math.sin(angle)))
        c.poly(points, closed=True, width=2)

    for a, b in links:
        vx, vy = (b[0] - a[0]) / spacing, (b[1] - a[1]) / spacing
        start = (a[0] + outer * vx, a[1] + outer * vy)
        end = (b[0] - outer * vx, b[1] - outer * vy)
        c.line(start, end, role='second', width=2)
        c.dot(start, r=2.4)
        c.dot(end, r=2.4)


EXTRA_PATTERNS = {
    'nested_courts': nested_courts,
    'triangular_chambers': triangular_chambers,
    'herringbone': herringbone,
    'star_corridors': star_corridors,
}

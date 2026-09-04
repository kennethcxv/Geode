"""
Geode Empire V6 material pipeline: tileable PBR texture sets generated deterministically (periodic gradient noise,
domain warping, cellular pitting) and written as Unity-ready PNGs.

Run headlessly through Blender's Python (numpy ships with it; the image writer is Blender's):

    ./Tools/blender.sh --background --python Tools/Blender/gen_textures.py -- [only a,b,c] [size 1024] [out <dir>]

Each material family produces: <name>_albedo.png (sRGB), <name>_normal.png (tangent space, OpenGL +Y, linear),
<name>_mask.png (R metallic, G occlusion, A smoothness; linear); <name>_height.png lands in Tools/Blender/Output/textures for review.
Default output: Geode/Assets/GeodeEmpire/Textures/Generated (production; the Editor's TextureImportRules sets normal /
linear / max-size on import). Sizes stay at or under 1024 (8 GB M2 budget); everything is seeded, re-runnable, identical.
"""
import os
import sys
import math

import numpy as np

try:
    import bpy
    HAVE_BPY = True
except Exception:  # plain python: still useful for previews via PIL if present
    HAVE_BPY = False

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Geode", "Assets", "GeodeEmpire", "Textures", "Generated"))


# ---------------------------------------------------------------------------------------------------------------------
# periodic noise (every function tiles over [0,1)^2 for the given integer period)
# ---------------------------------------------------------------------------------------------------------------------

def _hash_grid(period, seed):
    rng = np.random.RandomState(seed & 0x7FFFFFFF)
    ang = rng.uniform(0.0, 2.0 * math.pi, size=(period, period))
    return np.cos(ang), np.sin(ang)


def _fade(t):
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0)


def perlin(size, period, seed, x=None, y=None):
    """Tileable 2D gradient noise in [-1, 1]. x, y: optional warped coordinates in [0, 1) with the same shape."""
    if x is None:
        ys, xs = np.mgrid[0:size, 0:size].astype(np.float64)
        x = xs / size
        y = ys / size
    gx, gy = _hash_grid(period, seed)
    px = x * period
    py = y * period
    x0 = np.floor(px).astype(np.int64)
    y0 = np.floor(py).astype(np.int64)
    fx = px - x0
    fy = py - y0
    x1 = (x0 + 1) % period
    y1 = (y0 + 1) % period
    x0 %= period
    y0 %= period

    def dot(ix, iy, dx, dy):
        return gx[iy, ix] * dx + gy[iy, ix] * dy

    n00 = dot(x0, y0, fx, fy)
    n10 = dot(x1, y0, fx - 1.0, fy)
    n01 = dot(x0, y1, fx, fy - 1.0)
    n11 = dot(x1, y1, fx - 1.0, fy - 1.0)
    u = _fade(fx)
    v = _fade(fy)
    nx0 = n00 + u * (n10 - n00)
    nx1 = n01 + u * (n11 - n01)
    return (nx0 + v * (nx1 - nx0)) * 1.4142


def fbm(size, period, seed, octaves=5, lacunarity=2.0, gain=0.5, x=None, y=None, ridged=False):
    total = np.zeros((size, size)) if x is None else np.zeros_like(x)
    amp = 1.0
    norm = 0.0
    p = period
    for o in range(octaves):
        n = perlin(size, p, seed + 131 * o, x, y)
        if ridged:
            n = 1.0 - np.abs(n)
            n = n * n
        total += amp * n
        norm += amp
        amp *= gain
        p = int(round(p * lacunarity))
    return total / norm


def warp(size, period, seed, strength=0.08):
    """Domain-warped coordinates: returns (x, y) in [0,1) tiling with the same period."""
    ys, xs = np.mgrid[0:size, 0:size].astype(np.float64)
    x = xs / size
    y = ys / size
    wx = fbm(size, period, seed + 7, octaves=3)
    wy = fbm(size, period, seed + 19, octaves=3)
    return (x + strength * wx) % 1.0, (y + strength * wy) % 1.0


def worley(size, period, seed, jitter=0.9, x=None, y=None):
    """Tileable cellular noise: returns (F1, F2, cell id) with distances in cell units. x, y: optional warped coords."""
    f1, f2, cid, _, _ = worley_vec(size, period, seed, jitter, x, y)
    return f1, f2, cid


def worley_vec(size, period, seed, jitter=0.9, x=None, y=None):
    """Tileable cellular noise with the offset to the nearest seed: (F1, F2, cell id, dx, dy) in cell units."""
    rng = np.random.RandomState((seed * 7919) & 0x7FFFFFFF)
    cx = rng.uniform(0.0, 1.0, size=(period, period)) * jitter + (1.0 - jitter) * 0.5
    cy = rng.uniform(0.0, 1.0, size=(period, period)) * jitter + (1.0 - jitter) * 0.5
    if x is None:
        ys, xs = np.mgrid[0:size, 0:size].astype(np.float64)
        x = xs / size
        y = ys / size
    px = x * period
    py = y * period
    ix = np.floor(px).astype(np.int64)
    iy = np.floor(py).astype(np.int64)
    f1 = np.full(px.shape, 9.0)
    f2 = np.full(px.shape, 9.0)
    cid = np.zeros(px.shape, dtype=np.int64)
    vx = np.zeros(px.shape)
    vy = np.zeros(px.shape)
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            jx = (ix + ox) % period
            jy = (iy + oy) % period
            fx = cx[jy, jx] + (ix + ox)
            fy = cy[jy, jx] + (iy + oy)
            ddx = px - fx
            ddy = py - fy
            d = np.sqrt(ddx * ddx + ddy * ddy)
            closer = d < f1
            f2 = np.where(closer, f1, np.minimum(f2, d))
            cid = np.where(closer, jy * period + jx, cid)
            vx = np.where(closer, ddx, vx)
            vy = np.where(closer, ddy, vy)
            f1 = np.where(closer, d, f1)
    return f1, f2, cid, vx, vy


def cell_random(cid, seed, period):
    """A random value per cell id in [0,1)."""
    rng = np.random.RandomState((seed * 104729) & 0x7FFFFFFF)
    table = rng.uniform(0.0, 1.0, size=period * period)
    return table[cid % (period * period)]


# ---------------------------------------------------------------------------------------------------------------------
# helpers
# ---------------------------------------------------------------------------------------------------------------------

def normalize01(a):
    lo, hi = float(a.min()), float(a.max())
    return (a - lo) / max(1e-6, hi - lo)


def height_to_normal(height, strength=1.0):
    """Tangent-space normal (OpenGL +Y up) from a tiling height field in [0,1]; strength scales the slope."""
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5 * height.shape[1]
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5 * height.shape[0]
    # image row 0 is the top when saved; Blender/Unity store rows bottom-up, we flip at save time consistently
    nx = -dx * strength
    ny = -dy * strength
    nz = np.ones_like(height) * height.shape[0] * 0.5
    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    nx /= length
    ny /= length
    nz /= length
    return np.stack([nx * 0.5 + 0.5, ny * 0.5 + 0.5, nz * 0.5 + 0.5], axis=-1)


def curvature_ao(height, radius=3, strength=1.6):
    """Cheap ambient occlusion: cavities (below the local mean) darken."""
    h = height
    acc = np.zeros_like(h)
    n = 0
    for oy in range(-radius, radius + 1):
        for ox in range(-radius, radius + 1):
            if ox == 0 and oy == 0:
                continue
            acc += np.roll(np.roll(h, oy, axis=0), ox, axis=1)
            n += 1
    mean = acc / n
    ao = 1.0 - np.clip((mean - h) * strength * 4.0, 0.0, 1.0)
    return np.clip(ao, 0.35, 1.0)


def mix(a, b, t):
    return a * (1.0 - t) + b * t


def color(rgb):
    return np.array(rgb, dtype=np.float64)


def tint(base, variation, amount):
    """base colour (3,) modulated by a scalar field in [-1,1]: brightness variation."""
    v = 1.0 + amount * variation[..., None]
    return np.clip(base[None, None, :] * v, 0.0, 1.0)


def srgb(lin):
    lin = np.clip(lin, 0.0, 1.0)
    return np.where(lin <= 0.0031308, lin * 12.92, 1.055 * np.power(lin, 1.0 / 2.4) - 0.055)


# ---------------------------------------------------------------------------------------------------------------------
# material families
# ---------------------------------------------------------------------------------------------------------------------

def mat_rind_weathered(size, seed=1):
    """Weathered geode rind micro-detail (the mesh carries the macro shape): a botryoidal skin of rounded knobs at two
    scales with dark creases between them, chalky pale dome tops, iron-stained patches, sharp little pits with dry rims,
    and hairline cracks in a few patches. Relief is real: about 4 mm of height over an 11 cm tile."""
    period = 6
    x, y = warp(size, period, seed, 0.06)
    patches = normalize01(fbm(size, period, seed + 1, octaves=4, x=x, y=y))              # slow tonal patches
    grain = fbm(size, period * 16, seed + 4, octaves=3, gain=0.6)                          # fine matrix grain
    # botryoidal domes: every cell a rounded knob (spherical profile), a few cells left low so the skin is not a grid
    f1a, f2a, ca = worley(size, period * 3, seed + 2, jitter=0.85, x=x, y=y)
    f1b, f2b, cb = worley(size, period * 7, seed + 8, jitter=0.9, x=x, y=y)
    ra = cell_random(ca, seed + 3, period * 3)
    rb = cell_random(cb, seed + 9, period * 7)
    rad_a = 0.55 + 0.35 * ra
    rad_b = 0.5 + 0.3 * rb
    dome_a = np.sqrt(np.clip(1.0 - (f1a / rad_a) ** 2, 0.0, 1.0)) * (0.55 + 0.45 * (ra > 0.15))
    dome_b = np.sqrt(np.clip(1.0 - (f1b / rad_b) ** 2, 0.0, 1.0)) * (rb > 0.3)
    # pits: small sharp holes with a dry pale rim, scattered
    f1p, f2p, cp = worley(size, period * 12, seed + 11, jitter=1.0)
    rp = cell_random(cp, seed + 12, period * 12)
    pit_r = 0.25 + 0.3 * rp
    pits = np.clip(1.0 - f1p / pit_r, 0.0, 1.0) ** 1.4 * (rp > 0.7)
    rim = np.clip(1.0 - np.abs(f1p - pit_r) * 9.0, 0.0, 1.0) * (rp > 0.7) * 0.5
    cracks = np.clip(1.0 - np.abs(fbm(size, period * 3, seed + 5, octaves=4, x=x, y=y)) * 14.0, 0.0, 1.0)
    cracks *= (normalize01(fbm(size, period, seed + 6, octaves=2)) > 0.72)
    height = normalize01(0.2 + 0.5 * dome_a + 0.22 * dome_b + 0.04 * grain - 0.28 * pits + 0.02 * rim - 0.04 * cracks + 0.05 * (patches - 0.5))
    crease = np.clip(1.0 - (dome_a * 0.7 + dome_b * 0.5), 0.0, 1.0)
    base = color((0.46, 0.42, 0.35))
    dark = color((0.22, 0.19, 0.16))
    iron = color((0.52, 0.33, 0.18))
    pale = color((0.66, 0.62, 0.55))
    albedo = mix(base[None, None, :], iron[None, None, :], (patches ** 2)[..., None] * 0.7)
    albedo = mix(albedo, dark[None, None, :], (crease ** 1.5)[..., None] * 0.7)
    albedo = mix(albedo, pale[None, None, :], np.clip(dome_a * dome_a * (0.5 + 0.5 * dome_b), 0.0, 1.0)[..., None] * 0.55 + rim[..., None] * 0.5)
    albedo = albedo * (1.0 + 0.14 * grain[..., None]) * (1.0 - 0.5 * pits[..., None]) * (1.0 - 0.5 * cracks[..., None])
    rough = 0.8 + 0.12 * pits - 0.1 * dome_a * dome_b + 0.04 * grain - 0.05 * rim + 0.06 * crease
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.6, 0.98), metallic=0.0,
                normal_strength=16.0, ao=curvature_ao(height, 5, 1.5))


def mat_fracture_fresh(size, seed=2):
    """Fresh fracture: conchoidal ripple sets from several impact points, crisp ridges between them, small chip facets,
    a pale dry surface a shade lighter than the rind with a fine crystalline grain."""
    period = 6
    ys, xs = np.mgrid[0:size, 0:size].astype(np.float64)
    x = xs / size
    y = ys / size
    rng = np.random.RandomState(seed)
    ripples = np.zeros((size, size))
    for k in range(5):
        cx, cy = rng.uniform(0, 1), rng.uniform(0, 1)
        dxm = np.minimum(np.abs(x - cx), 1.0 - np.abs(x - cx))
        dym = np.minimum(np.abs(y - cy), 1.0 - np.abs(y - cy))
        d = np.sqrt(dxm * dxm + dym * dym)
        ring = np.sin(d * (55.0 + 18 * k) + rng.uniform(0, 6.28))
        ring = np.sign(ring) * np.abs(ring) ** 0.6                                        # crisper crests
        ripples += ring * np.exp(-d * 3.0) * rng.uniform(0.6, 1.0)
    ripples = normalize01(ripples) - 0.5
    f1, f2, cid = worley(size, period * 3, seed + 11, jitter=0.95)
    facet = cell_random(cid, seed + 12, period * 3)                                       # each cell a tilted facet
    facet_edge = np.clip((f2 - f1) * 4.0, 0.0, 1.0)
    chips = np.clip(1.0 - f1 * 2.4, 0.0, 1.0) ** 2 * (cell_random(cid, seed + 15, period * 3) > 0.82)
    grain = fbm(size, period * 20, seed + 13, octaves=2, gain=0.55)
    ridges = fbm(size, period * 2, seed + 14, octaves=4, ridged=True)
    facet_soft = np.clip(facet_edge * 1.5, 0.0, 1.0) * (normalize01(fbm(size, period, seed + 16, octaves=2)) > 0.6)   # facets only in patches
    height = 0.5 + 0.18 * ripples + 0.07 * (facet - 0.5) * facet_soft + 0.10 * (ridges - 0.5) - 0.2 * chips + 0.04 * grain
    height = normalize01(height)
    base = color((0.70, 0.66, 0.58))
    dark = color((0.44, 0.40, 0.34))
    warm = color((0.62, 0.52, 0.40))
    albedo = mix(base[None, None, :], warm[None, None, :], np.clip(facet, 0, 1)[..., None] * 0.3)
    albedo = mix(albedo, dark[None, None, :], np.clip(chips * 1.5 + (1.0 - facet_edge) * 0.12 * facet_soft, 0, 1)[..., None])
    albedo = albedo * (1.0 + 0.1 * grain[..., None]) * (1.0 - 0.12 * (0.5 - ripples)[..., None])
    rough = 0.6 + 0.15 * chips + 0.06 * grain - 0.12 * np.clip(ripples, 0, 1) + 0.04 * (1.0 - facet_edge) * facet_soft
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.42, 0.9), metallic=0.0,
                normal_strength=10.0, ao=curvature_ao(height, 2, 1.2))


def mat_cavity_wall(size, seed=3):
    """Chalcedony cavity wall / mineral rim: botryoidal bumps, banded transition, satin sheen."""
    period = 7
    x, y = warp(size, period, seed, 0.05)
    f1, f2, cid = worley(size, period * 3, seed + 21, jitter=0.7)
    bumps = np.clip(1.0 - f1 * 1.25, 0.0, 1.0)                                     # botryoidal domes
    bumps = np.sqrt(bumps)
    bands = 0.5 + 0.5 * np.sin(fbm(size, period, seed + 22, octaves=3, x=x, y=y) * 9.0)   # agate-like banding
    grain = fbm(size, period * 10, seed + 23, octaves=3)
    height = normalize01(0.5 + 0.35 * bumps + 0.04 * bands + 0.03 * grain)
    grey = color((0.62, 0.60, 0.58))
    blue = color((0.55, 0.58, 0.62))
    white = color((0.80, 0.79, 0.77))
    albedo = mix(grey[None, None, :], blue[None, None, :], bands[..., None] * 0.6)
    albedo = mix(albedo, white[None, None, :], (bumps ** 3)[..., None] * 0.5)
    albedo = albedo * (1.0 + 0.08 * grain[..., None])
    rough = 0.42 - 0.12 * bumps + 0.05 * grain + 0.06 * bands
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.25, 0.7), metallic=0.0,
                normal_strength=8.0, ao=curvature_ao(height, 3, 1.3))


def mat_druse(size, seed=17):
    """Druse floor: a mosaic of tiny crystal terminations, each cell a six-sided pyramid at its own height and turn,
    glassy and near-white (the shell shader tints it in the mineral's colour where a carpet grows). About 1.5-2 mm
    per crystal at the shader's cavity scale."""
    period = 4
    n = period * 5
    f1, f2, cid, dx, dy = worley_vec(size, n, seed + 1, jitter=0.95)
    r1 = cell_random(cid, seed + 2, n)
    r2 = cell_random(cid, seed + 3, n)
    r3 = cell_random(cid, seed + 4, n)
    ang = r1 * math.pi / 3.0
    ca, sa = np.cos(ang), np.sin(ang)
    rx = dx * ca - dy * sa
    ry = dx * sa + dy * ca
    # hexagonal metric: the largest of three axis projections gives six flat facets meeting at the apex
    dh = np.maximum(np.abs(rx), np.maximum(np.abs(rx * 0.5 + ry * 0.8660254), np.abs(rx * 0.5 - ry * 0.8660254)))
    radius = 0.5 + 0.35 * r2
    pyramid = np.clip(1.0 - dh / radius, 0.0, 1.0)
    grain = fbm(size, period * 24, seed + 5, octaves=2, gain=0.5)
    height = normalize01(pyramid * (0.55 + 0.45 * r3) + 0.3 * r2 + 0.015 * grain)
    white = color((0.86, 0.86, 0.88))
    grey = color((0.62, 0.62, 0.64))
    warm = color((0.8, 0.78, 0.74))
    albedo = mix(white[None, None, :], grey[None, None, :], (1.0 - r3)[..., None] * 0.5)
    albedo = mix(albedo, warm[None, None, :], (r1 > 0.7)[..., None] * 0.35)
    albedo = albedo * (0.8 + 0.3 * pyramid[..., None])                        # bases in shadow, tips bright
    rough = 0.16 + 0.12 * (1.0 - pyramid) + 0.06 * (r2 - 0.5) + 0.03 * grain
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.08, 0.4), metallic=0.0,
                normal_strength=18.0, ao=curvature_ao(height, 3, 1.4))


def mat_agate_band(size, seed=4):
    """Polished agate: fine concentric banding with warped edges, glassy."""
    period = 5
    x, y = warp(size, period, seed, 0.10)
    field = fbm(size, period, seed + 31, octaves=4, x=x, y=y)
    bands = 0.5 + 0.5 * np.sin(field * 26.0)
    fine = 0.5 + 0.5 * np.sin(field * 92.0 + 1.3)
    grain = fbm(size, period * 12, seed + 32, octaves=2)
    height = normalize01(0.5 + 0.02 * bands + 0.01 * fine + 0.01 * grain)
    a = color((0.55, 0.36, 0.24))
    b = color((0.86, 0.78, 0.66))
    c = color((0.32, 0.22, 0.18))
    albedo = mix(a[None, None, :], b[None, None, :], bands[..., None])
    albedo = mix(albedo, c[None, None, :], (fine ** 4)[..., None] * 0.35)
    rough = 0.12 + 0.05 * grain + 0.03 * fine
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.06, 0.3), metallic=0.0,
                normal_strength=0.35, ao=np.ones((size, size)))


def mat_painted_steel(size, seed=5):
    """Machine-painted sheet steel: fine orange-peel paint, faint tonal variation, a few soft scuffs. Contact wear
    (edges, handles, table) is not baked into the tile: it comes from the geometry side, so the tile stays clean."""
    period = 9
    peel = fbm(size, period * 8, seed + 41, octaves=3, gain=0.55)
    tone = fbm(size, period, seed + 45, octaves=3)                                 # slow tonal drift
    scuff = np.clip(fbm(size, period * 3, seed + 42, octaves=4, ridged=True) - 0.55, 0.0, 1.0) * 1.2
    scuff *= (normalize01(fbm(size, period, seed + 46, octaves=2)) > 0.72)          # only in a few patches
    height = normalize01(0.5 + 0.02 * peel - 0.01 * scuff)
    paint = color((0.30, 0.36, 0.32))
    albedo = tint(paint, tone, 0.06) * (1.0 + 0.03 * peel[..., None]) * (1.0 + 0.08 * scuff[..., None])
    rough = 0.38 + 0.05 * peel + 0.18 * scuff
    metallic = np.zeros((size, size))
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.3, 0.65), metallic=metallic,
                normal_strength=0.35, ao=np.ones((size, size)))


def mat_cast_iron(size, seed=6):
    period = 8
    grain = fbm(size, period * 8, seed + 51, octaves=4, gain=0.6)
    sand = normalize01(fbm(size, period * 20, seed + 52, octaves=2))
    f1, f2, cid = worley(size, period * 6, seed + 53)
    pores = np.clip(1.0 - f1 * 2.2, 0, 1) ** 2 * (cell_random(cid, seed + 54, period * 6) > 0.75)
    height = normalize01(0.5 + 0.06 * grain + 0.03 * sand - 0.12 * pores)
    base = color((0.25, 0.25, 0.26))
    albedo = tint(base, grain, 0.18) * (1.0 - 0.3 * pores[..., None])
    rough = 0.72 + 0.08 * sand + 0.1 * pores
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.55, 0.95), metallic=np.full((size, size), 0.85),
                normal_strength=0.9, ao=curvature_ao(height, 2, 1.0))


def mat_brushed_stainless(size, seed=7):
    period = 8
    ys, xs = np.mgrid[0:size, 0:size].astype(np.float64)
    lines = fbm(size, period * 24, seed + 61, octaves=2)
    lines = 0.7 * fbm(size, period * 48, seed + 62, octaves=1, x=(xs / size) % 1.0, y=np.full((size, size), 0.37)) + 0.3 * lines
    smudge = fbm(size, period, seed + 63, octaves=3)
    height = normalize01(0.5 + 0.02 * lines + 0.01 * smudge)
    base = color((0.62, 0.62, 0.61))
    albedo = tint(base, lines, 0.06) * (1.0 + 0.03 * smudge[..., None])
    rough = 0.32 + 0.06 * np.abs(lines) + 0.08 * np.clip(smudge, 0, 1)
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.2, 0.55), metallic=np.full((size, size), 1.0),
                normal_strength=0.35, ao=np.ones((size, size)))


def mat_aluminium(size, seed=8):
    period = 8
    grain = fbm(size, period * 14, seed + 71, octaves=3)
    oxide = normalize01(fbm(size, period * 2, seed + 72, octaves=3))
    height = normalize01(0.5 + 0.015 * grain)
    base = color((0.70, 0.71, 0.72))
    albedo = tint(base, grain, 0.05) * (1.0 - 0.08 * oxide[..., None])
    rough = 0.38 + 0.1 * oxide + 0.04 * grain
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.3, 0.6), metallic=np.full((size, size), 0.95),
                normal_strength=0.3, ao=np.ones((size, size)))


def mat_rubber(size, seed=9):
    period = 10
    micro = fbm(size, period * 20, seed + 81, octaves=2)
    dust = normalize01(fbm(size, period * 2, seed + 82, octaves=3))
    height = normalize01(0.5 + 0.02 * micro)
    base = color((0.09, 0.09, 0.09))
    albedo = tint(base, micro, 0.15) + 0.04 * dust[..., None]
    rough = 0.8 + 0.08 * micro - 0.05 * dust
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.65, 0.95), metallic=0.0,
                normal_strength=0.4, ao=np.ones((size, size)))


def mat_hardwood(size, seed=10):
    """Plank hardwood: three planks across the tile, fine straight grain with a gentle cathedral wobble, pores, soft
    tonal variation between planks and a hint of wear. Reads as timber at 30 cm, not as a zebra pattern."""
    period = 6
    ys, xs = np.mgrid[0:size, 0:size].astype(np.float64)
    x = xs / size
    y = ys / size
    planks = 3
    plank_id = np.floor(y * planks).astype(np.int64)
    rng = np.random.RandomState(seed)
    plank_tone = rng.uniform(-0.12, 0.12, size=planks)[plank_id]
    plank_shift = rng.uniform(0.0, 1.0, size=planks)[plank_id]
    # grain runs along x: many fine lines whose spacing drifts slowly, plus a cathedral figure from a low-frequency warp
    wob = fbm(size, period, seed + 91, octaves=3) * 0.35 + fbm(size, period * 3, seed + 94, octaves=2) * 0.08
    phase = (y + plank_shift) * 60.0 + wob * 2.2 + x * 0.6
    rings = 0.5 + 0.5 * np.sin(phase * 2.0 * math.pi)
    rings = rings ** 3.0                                                            # thin dark lines, wide light latewood
    fine = fbm(size, period * 40, seed + 92, octaves=1)
    pores = np.clip(fine, 0.35, 1.0) - 0.35
    pores = pores * (rings > 0.5)                                                   # pores sit in the darker bands
    seam = np.clip(1.0 - np.abs((y * planks) % 1.0 - 0.5) * 2.0 * 30.0, 0.0, 1.0)   # thin dark gaps between planks
    wear = normalize01(fbm(size, period, seed + 93, octaves=3))
    height = normalize01(0.5 - 0.02 * rings - 0.02 * pores * 3.0 - 0.08 * seam + 0.005 * fine)
    light = color((0.58, 0.40, 0.24))
    dark = color((0.42, 0.27, 0.15))
    albedo = mix(light[None, None, :], dark[None, None, :], (rings * 0.8)[..., None])
    albedo = albedo * (1.0 + plank_tone[..., None]) * (1.0 - 0.35 * pores[..., None] * 3.0) * (1.0 - 0.6 * seam[..., None])
    albedo = albedo * (1.0 + 0.06 * (wear - 0.5)[..., None])
    rough = 0.5 + 0.1 * rings + 0.15 * pores * 3.0 + 0.2 * seam - 0.12 * np.clip(wear - 0.6, 0, 1) * 2.5
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.3, 0.8), metallic=0.0,
                normal_strength=0.8, ao=np.ones((size, size)))


def mat_plywood(size, seed=11):
    period = 4
    ys, xs = np.mgrid[0:size, 0:size].astype(np.float64)
    x = xs / size
    y = ys / size
    wob = fbm(size, period, seed + 101, octaves=3, x=x, y=y)
    figure = 0.5 + 0.5 * np.sin((x * 1.5 + 0.6 * wob) * 2.0 * math.pi * 3.0)
    figure = figure ** 2.2
    fuzz = fbm(size, period * 30, seed + 102, octaves=1)
    height = normalize01(0.5 + 0.01 * figure + 0.01 * fuzz)
    light = color((0.72, 0.58, 0.40))
    dark = color((0.55, 0.42, 0.27))
    albedo = mix(light[None, None, :], dark[None, None, :], figure[..., None]) * (1.0 + 0.05 * fuzz[..., None])
    rough = 0.62 + 0.08 * figure + 0.05 * fuzz
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.5, 0.8), metallic=0.0,
                normal_strength=0.35, ao=np.ones((size, size)))


def mat_cardboard(size, seed=12):
    period = 8
    fibre = fbm(size, period * 18, seed + 111, octaves=2)
    flutes = 0.5 + 0.5 * np.sin(np.mgrid[0:size, 0:size][1] / size * 2.0 * math.pi * 48.0)
    flutes = flutes ** 3 * 0.35
    height = normalize01(0.5 + 0.02 * fibre + 0.01 * flutes)
    base = color((0.60, 0.46, 0.30))
    albedo = tint(base, fibre, 0.12) * (1.0 - 0.06 * flutes[..., None])
    rough = 0.85 + 0.05 * fibre
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.75, 0.95), metallic=0.0,
                normal_strength=0.4, ao=np.ones((size, size)))


def mat_leather(size, seed=13):
    """Full-grain leather: soft irregular pebbling (blurred cells folded into noise), fine creases, a worn sheen on the
    high spots. Reads as hide at 30 cm, never as a honeycomb."""
    period = 9
    x, y = warp(size, period, seed, 0.06)
    f1a, f2a, ca = worley(size, period * 7, seed + 121, jitter=1.0)
    ra = cell_random(ca, seed + 124, period * 7)
    peb = np.clip(1.0 - f1a / (0.45 + 0.55 * ra), 0, 1)
    peb = peb * peb * (3.0 - 2.0 * peb)                                           # rounded domes
    # soften the cells into one another with two noise scales so no edge stays straight
    soft = fbm(size, period * 4, seed + 126, octaves=3, x=x, y=y)
    fine = fbm(size, period * 22, seed + 127, octaves=2)
    grain = np.clip(0.55 * peb + 0.3 * (soft * 0.5 + 0.5) + 0.15 * (fine * 0.5 + 0.5), 0.0, 1.0)
    creases = np.clip((f2a - f1a) * 2.2, 0, 1)
    wear = normalize01(fbm(size, period, seed + 122, octaves=3))
    height = normalize01(0.5 + 0.08 * grain - 0.04 * (1.0 - creases) * (1.0 - peb) + 0.02 * fine)
    base = color((0.31, 0.18, 0.10))
    albedo = tint(base, grain - 0.5, 0.2) * (1.0 - 0.12 * (1.0 - creases)[..., None] * (1.0 - peb)[..., None]) * (1.0 + 0.14 * (wear - 0.5)[..., None])
    rough = 0.62 - 0.16 * grain + 0.1 * (1.0 - creases) * (1.0 - peb) - 0.14 * np.clip(wear - 0.6, 0, 1) * 2.5
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.32, 0.75), metallic=0.0,
                normal_strength=0.7, ao=curvature_ao(height, 2, 0.8))


def mat_felt(size, seed=14):
    period = 10
    fuzz = fbm(size, period * 24, seed + 131, octaves=2)
    nap = fbm(size, period * 3, seed + 132, octaves=3)
    height = normalize01(0.5 + 0.03 * fuzz + 0.01 * nap)
    base = color((0.10, 0.12, 0.22))
    albedo = tint(base, fuzz, 0.2) * (1.0 + 0.1 * nap[..., None])
    rough = 0.92 + 0.04 * fuzz
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.85, 0.99), metallic=0.0,
                normal_strength=0.5, ao=np.ones((size, size)))


def mat_concrete(size, seed=15):
    period = 6
    x, y = warp(size, period, seed, 0.03)
    aggregate = fbm(size, period * 10, seed + 141, octaves=3, x=x, y=y)
    f1, f2, cid = worley(size, period * 9, seed + 142)
    pores = np.clip(1.0 - f1 * 2.4, 0, 1) ** 2 * (cell_random(cid, seed + 143, period * 9) > 0.6)
    stain = normalize01(fbm(size, period, seed + 144, octaves=3))
    height = normalize01(0.5 + 0.04 * aggregate - 0.1 * pores)
    base = color((0.52, 0.51, 0.48))
    albedo = tint(base, aggregate, 0.1) * (1.0 - 0.18 * stain[..., None]) * (1.0 - 0.2 * pores[..., None])
    rough = 0.8 + 0.1 * pores - 0.08 * stain
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.6, 0.95), metallic=0.0,
                normal_strength=0.8, ao=curvature_ao(height, 2, 1.0))


def mat_plaster(size, seed=16):
    period = 6
    trowel = fbm(size, period * 3, seed + 151, octaves=3, gain=0.6)
    fine = fbm(size, period * 20, seed + 152, octaves=1)
    height = normalize01(0.5 + 0.03 * trowel + 0.01 * fine)
    base = color((0.80, 0.74, 0.60))
    albedo = tint(base, trowel, 0.05) * (1.0 + 0.03 * fine[..., None])
    rough = 0.88 + 0.04 * fine
    return dict(height=height, albedo=np.clip(albedo, 0, 1), roughness=np.clip(rough, 0.8, 0.97), metallic=0.0,
                normal_strength=0.5, ao=np.ones((size, size)))


MATERIALS = {
    "rind_weathered": mat_rind_weathered,
    "fracture_fresh": mat_fracture_fresh,
    "cavity_wall": mat_cavity_wall,
    "druse": mat_druse,
    "agate_band": mat_agate_band,
    "painted_steel": mat_painted_steel,
    "cast_iron": mat_cast_iron,
    "brushed_stainless": mat_brushed_stainless,
    "aluminium": mat_aluminium,
    "rubber": mat_rubber,
    "hardwood": mat_hardwood,
    "plywood": mat_plywood,
    "cardboard": mat_cardboard,
    "leather": mat_leather,
    "felt": mat_felt,
    "concrete": mat_concrete,
    "plaster": mat_plaster,
}


# ---------------------------------------------------------------------------------------------------------------------
# writing
# ---------------------------------------------------------------------------------------------------------------------

def _save(path, rgba, is_srgb):
    """rgba: (h, w, 4) floats in [0,1], row 0 = top of the image."""
    h, w = rgba.shape[:2]
    if HAVE_BPY:
        img = bpy.data.images.new(os.path.basename(path), width=w, height=h, alpha=True, float_buffer=False)
        # Blender stores rows bottom-up
        flat = np.ascontiguousarray(rgba[::-1, :, :]).astype(np.float32).ravel()
        img.pixels.foreach_set(flat)
        img.update()
        # written as-is (8-bit PNG): sRGB maps are already encoded, data maps are linear; changing the colourspace
        # setting on a generated image reloads it from disk (blank), so it is left alone
        img.filepath_raw = path
        img.file_format = "PNG"
        img.save()
        bpy.data.images.remove(img)
    else:
        from PIL import Image
        Image.fromarray((np.clip(rgba, 0, 1) * 255.0 + 0.5).astype(np.uint8), "RGBA").save(path)


def write_material(name, size, out_dir):
    m = MATERIALS[name](size)
    height = m["height"]
    albedo = m["albedo"]
    rough = m["roughness"] if isinstance(m["roughness"], np.ndarray) else np.full((size, size), float(m["roughness"]))
    metallic = m["metallic"] if isinstance(m["metallic"], np.ndarray) else np.full((size, size), float(m["metallic"]))
    ao = m["ao"]
    normal = height_to_normal(height, m.get("normal_strength", 1.0))
    ones = np.ones((size, size))
    _save(os.path.join(out_dir, f"{name}_albedo.png"), np.dstack([srgb(albedo[..., 0]), srgb(albedo[..., 1]), srgb(albedo[..., 2]), ones]), True)
    _save(os.path.join(out_dir, f"{name}_normal.png"), np.dstack([normal[..., 0], normal[..., 1], normal[..., 2], ones]), False)
    # URP Lit mask convention: R metallic, G occlusion, B (detail mask, unused), A smoothness
    _save(os.path.join(out_dir, f"{name}_mask.png"), np.dstack([metallic, ao, ones, 1.0 - rough]), False)
    # the height field is review material, not a runtime asset: it goes to the scratch folder beside the tool
    review_dir = os.path.join(HERE, "Output", "textures")
    os.makedirs(review_dir, exist_ok=True)
    _save(os.path.join(review_dir, f"{name}_height.png"), np.dstack([height, height, height, ones]), False)
    return dict(name=name, size=size, rough=(float(rough.min()), float(rough.max())), metallic=(float(metallic.min()), float(metallic.max())))


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    only = None
    size = 1024
    out_dir = DEFAULT_OUT
    i = 0
    while i < len(argv):
        if argv[i] == "only":
            only = argv[i + 1].split(",")
            i += 2
        elif argv[i] == "size":
            size = int(argv[i + 1])
            i += 2
        elif argv[i] == "out":
            out_dir = argv[i + 1]
            i += 2
        else:
            i += 1
    os.makedirs(out_dir, exist_ok=True)
    names = only or list(MATERIALS.keys())
    for n in names:
        info = write_material(n, size, out_dir)
        print(f"[gen_textures] {info['name']} {info['size']}px rough={info['rough'][0]:.2f}-{info['rough'][1]:.2f} metallic={info['metallic'][0]:.2f}-{info['metallic'][1]:.2f}")
    print(f"[gen_textures] wrote {len(names)} sets to {out_dir}")


if __name__ == "__main__":
    main()

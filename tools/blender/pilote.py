"""
Pilote Turbo Pattes (DA « Atelier plein gaz ») généré par script Blender, assis en position de conduite.

    blender -b --python tools/blender/pilote.py -- --animal fox --out Assets/TurboPattes/Resources/Pilots/Fox.fbx [--preview docs/da/roxy.png]

Repère Blender : Z en haut, le pilote regarde vers -Y, origine = point d'assise sur le baquet. Unités en mètres,
à l'échelle du kart du jeu. Les zones de couleur sont des matériaux nommés (TP_Suit, TP_Trim, TP_Fur, TP_Accent,
TP_Dark, TP_Helmet, TP_Visor, TP_Eye, TP_Nose) : le jeu les remplace par la livrée du pilote.
Corps sculpté avec le modificateur Skin (squelette de sommets + rayons) lissé par subdivision.
"""
import argparse
import math
import sys

import bpy
import bmesh
from mathutils import Vector

# ------------------------------------------------------------------ paramètres par espèce

ANIMALS = {
    # head_r : rayon du crâne ; snout : (forme, longueur, rayon) — taper (museau effilé), flat (grenouille), beak, disc (groin) ;
    # (muzzle : museau court et arrondi) ; ears : (forme, hauteur, largeur, écart, inclinaison) — pointed, round, long, floppy, ou None ;
    # tail : (forme, rayon) — bushy, puff, thin, curl, ou None ; accent : zones claires/sombres du visage ;
    # bulk : carrure du buste.
    "fox": dict(head_r=0.15, snout=("taper", 0.15, 0.05), ears=("pointed", 0.17, 0.075, 0.09, 14), ear_tips=True,
                tail=("bushy", 0.12), accent="muzzle", fur=(1.0, 0.5, 0.16), accent_c=(0.97, 0.94, 0.9)),
    "panda": dict(head_r=0.16, snout=("muzzle", 0.06, 0.065), ears=("round", 0.07, 0.07, 0.1, 0), ear_mat="TP_Accent",
                  tail=("puff", 0.05), accent="patches", fur=(0.96, 0.95, 0.92), accent_c=(0.12, 0.12, 0.14), bulk=1.06),
    "frog": dict(head_r=0.15, snout=("flat", 0, 0), ears=None, tail=None, accent="belly", eyes="bulge",
                 fur=(0.4, 0.85, 0.25), accent_c=(0.95, 0.95, 0.6)),
    "rabbit": dict(head_r=0.14, snout=("muzzle", 0.07, 0.055), ears=("long", 0.25, 0.05, 0.055, 7), ear_inner=True,
                   tail=("puff", 0.065), accent=None, fur=(0.98, 0.92, 0.95), accent_c=(1.0, 0.6, 0.75)),
    "bear": dict(head_r=0.16, snout=("muzzle", 0.09, 0.075), ears=("round", 0.06, 0.06, 0.115, 0), tail=("puff", 0.04),
                 accent="muzzle", fur=(0.55, 0.35, 0.2), accent_c=(0.85, 0.7, 0.5), bulk=1.14),
    "cat": dict(head_r=0.145, snout=("muzzle", 0.06, 0.05), ears=("pointed", 0.12, 0.07, 0.085, 10), ear_inner=True,
                tail=("thin", 0.035), accent="muzzle", fur=(0.6, 0.58, 0.68), accent_c=(0.97, 0.95, 0.93)),
    "penguin": dict(head_r=0.15, snout=("beak", 0.1, 0.04), ears=None, tail=None, accent="face",
                    fur=(0.12, 0.14, 0.2), accent_c=(0.97, 0.97, 0.97), bulk=1.08),
    "pig": dict(head_r=0.155, snout=("disc", 0.06, 0.06), ears=("floppy", 0.11, 0.07, 0.095, 0), tail=("curl", 0.022),
                accent=None, fur=(1.0, 0.68, 0.72), accent_c=(0.95, 0.5, 0.55), bulk=1.1),
}

PALETTE = dict(
    TP_Suit=(0.235, 0.561, 0.569), TP_Trim=(0.851, 0.302, 0.247), TP_Dark=(0.11, 0.13, 0.12), TP_Helmet=(0.235, 0.561, 0.569),
    TP_Visor=(0.05, 0.06, 0.07), TP_Eye=(0.06, 0.05, 0.04), TP_Nose=(0.04, 0.04, 0.04), TP_Glint=(0.97, 0.95, 0.9),
    TP_Beak=(0.95, 0.6, 0.15),
)


def args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser()
    p.add_argument("--animal", default="fox")
    p.add_argument("--out", required=True)
    p.add_argument("--preview")
    return p.parse_args(argv)


# ------------------------------------------------------------------ outils

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def material(name, color):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (*color, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.6
    return m


def skin_object(name, verts, edges, radii, mat, levels=2):
    """Maillage « squelette » + Skin + Subdivision, modificateurs appliqués. radii : (rx, ry) par sommet."""
    me = bpy.data.meshes.new(name)
    me.from_pydata([Vector(v) for v in verts], edges, [])
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    ob.modifiers.new("Skin", "SKIN")
    for sv, r in zip(me.skin_vertices[0].data, radii):
        sv.radius = r if isinstance(r, tuple) else (r, r)
    me.skin_vertices[0].data[0].use_root = True
    sub = ob.modifiers.new("Sub", "SUBSURF")
    sub.levels = levels
    sub.render_levels = levels
    apply_all(ob)
    ob.data.materials.append(mat)
    smooth(ob)
    ob.select_set(False)
    return ob


def apply_all(ob):
    bpy.context.view_layer.objects.active = ob
    for m in list(ob.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)


def smooth(ob):
    for poly in ob.data.polygons:
        poly.use_smooth = True


def ellipsoid(name, center, size, mat, rot=(0, 0, 0), segments=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=segments // 2, radius=1.0, location=center,
                                         rotation=[math.radians(a) for a in rot])
    ob = bpy.context.active_object
    ob.name = name
    ob.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ob.data.materials.append(mat)
    smooth(ob)
    ob.select_set(False)
    return ob


def cone(name, base, tip, r, mat, segments=16):
    d = Vector(tip) - Vector(base)
    bpy.ops.mesh.primitive_cone_add(vertices=segments, radius1=r, radius2=r * 0.12, depth=d.length,
                                    location=(Vector(base) + Vector(tip)) * 0.5)
    ob = bpy.context.active_object
    ob.name = name
    ob.rotation_mode = "QUATERNION"
    ob.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    ob.data.materials.append(mat)
    smooth(ob)
    ob.select_set(False)
    return ob


def paint_faces(ob, mat, test):
    """Affecte <mat> aux faces dont le centre (repère objet) vérifie test(x, y, z)."""
    if mat.name not in [m.name for m in ob.data.materials]:
        ob.data.materials.append(mat)
    idx = [m.name for m in ob.data.materials].index(mat.name)
    for poly in ob.data.polygons:
        c = poly.center
        if test(c.x, c.y, c.z):
            poly.material_index = idx


def mirror_pts(pts):
    return [(-x, y, z) for (x, y, z) in pts]


# ------------------------------------------------------------------ construction

def build(animal):
    a = ANIMALS[animal]
    M = {k: material(k, v) for k, v in PALETTE.items()}
    M["TP_Fur"] = material("TP_Fur", a["fur"])
    M["TP_Accent"] = material("TP_Accent", a["accent_c"])
    parts = []

    # Combinaison : bassin, buste en V (épaules larges), bras tendus vers le volant, jambes vers les pédales.
    v = [
        (0, 0.0, 0.12),      # 0 bassin
        (0, -0.01, 0.30),    # 1 taille
        (0, -0.03, 0.50),    # 2 poitrine
        (0, -0.02, 0.70),    # 3 col
    ]
    k = a.get("bulk", 1.0)
    r = [(0.19 * k, 0.15 * k), (0.155 * k, 0.12 * k), (0.235 * k, 0.15 * k), (0.1, 0.09)]
    e = [(0, 1), (1, 2), (2, 3)]

    def chain(points, radii, start):
        nonlocal v, r, e
        prev = start
        for p, rr in zip(points, radii):
            v.append(p)
            r.append(rr)
            e.append((prev, len(v) - 1))
            prev = len(v) - 1
        return prev

    for sx in (-1, 1):
        # Bras : épaule, coude, poignet (mains sur le volant devant, ~0,55 m en avant, ~0,38 m de haut).
        chain([(0.25 * sx, -0.02, 0.585), (0.29 * sx, -0.26, 0.42), (0.18 * sx, -0.47, 0.42)],
              [(0.1, 0.095), (0.075, 0.072), (0.058, 0.055)], 2)
        # Jambes : hanche, genou relevé, cheville.
        chain([(0.11 * sx, -0.05, 0.1), (0.15 * sx, -0.42, 0.3), (0.13 * sx, -0.78, 0.1)],
              [(0.11, 0.11), (0.088, 0.085), (0.062, 0.06)], 0)
    suit = skin_object("Suit", v, e, r, M["TP_Suit"], levels=3)
    # Bandes de livrée : bande verticale sur le buste, bandes sur les flancs des bras et des jambes.
    paint_faces(suit, M["TP_Trim"], lambda x, y, z: abs(x) < 0.05 and 0.18 < z < 0.62 and y < 0)
    parts.append(suit)
    # Col montant sombre.
    parts.append(skin_object("Collar", [(0, -0.02, 0.64), (0, -0.015, 0.71)], [(0, 1)], [(0.115, 0.1), (0.1, 0.09)], M["TP_Dark"]))
    # Passepoils de livrée sur l'extérieur des bras.
    for sx in (-1, 1):
        parts.append(skin_object(f"ArmPiping{sx}", [(0.3 * k * sx, -0.02, 0.6), (0.355 * sx, -0.25, 0.43), (0.235 * sx, -0.45, 0.43)], [(0, 1), (1, 2)],
                                 [(0.016, 0.03), (0.016, 0.028), (0.014, 0.024)], M["TP_Trim"]))

    # Passepoils de livrée sur l'extérieur des jambes (relief net plutôt que faces peintes crénelées).
    for sx in (-1, 1):
        parts.append(skin_object(f"Piping{sx}", [(0.205 * sx, -0.08, 0.13), (0.235 * sx, -0.42, 0.32), (0.19 * sx, -0.74, 0.12)], [(0, 1), (1, 2)],
                                 [(0.018, 0.04), (0.018, 0.035), (0.016, 0.03)], M["TP_Trim"]))

    # Gants et bottines.
    for sx in (-1, 1):
        parts.append(skin_object(f"Glove{sx}", [(0.18 * sx, -0.44, 0.42), (0.165 * sx, -0.56, 0.43)], [(0, 1)],
                                 [(0.058, 0.055), (0.052, 0.045)], M["TP_Dark"]))
        parts.append(skin_object(f"Boot{sx}", [(0.13 * sx, -0.76, 0.11), (0.135 * sx, -0.9, 0.07)], [(0, 1)],
                                 [(0.064, 0.064), (0.055, 0.04)], M["TP_Dark"]))

    # Tête : crâne (+ museau effilé selon l'espèce, Skin), cou en fourrure.
    hr = a["head_r"]
    head_c = (0, -0.05, 0.85)
    sk, sl, sr = a["snout"]
    front = head_c[1] - hr  # avant du crâne
    if sk in ("taper", "muzzle"):
        snout_tip = (0, front - sl, head_c[2] - (0.05 if sk == "taper" else 0.04))
        tip_r = (sr * 0.55, sr * 0.45) if sk == "taper" else (sr, sr * 0.8)
        head = skin_object("Head", [(0, -0.01, 0.66), head_c, (0, head_c[1] - hr * 0.7, head_c[2] - 0.03), snout_tip],
                           [(0, 1), (1, 2), (2, 3)], [(0.075, 0.07), (hr, hr * 1.02), (hr * 0.62, hr * 0.5), tip_r],
                           M["TP_Fur"], levels=4)
    elif sk == "flat":
        # Grenouille : tête large et aplatie, bouche fendue.
        head = skin_object("Head", [(0, -0.01, 0.66), (0, head_c[1], head_c[2] - 0.02), (0, front + 0.03, head_c[2] - 0.05)],
                           [(0, 1), (1, 2)], [(0.085, 0.08), (hr * 1.18, hr * 0.85), (hr * 0.95, hr * 0.55)], M["TP_Fur"], levels=3)
    else:
        head = skin_object("Head", [(0, -0.01, 0.66), head_c, (0, front + hr * 0.3, head_c[2] - 0.03)],
                           [(0, 1), (1, 2)], [(0.075, 0.07), (hr, hr * 1.02), (hr * 0.78, hr * 0.72)], M["TP_Fur"], levels=3)

    acc = a.get("accent")
    if acc == "muzzle":  # dessous du museau et joues clairs
        paint_faces(head, M["TP_Accent"], lambda x, y, z: z < head_c[2] - 0.045 and y < head_c[1] - 0.04)
        paint_faces(head, M["TP_Accent"], lambda x, y, z: abs(x) > hr * 0.55 and z < head_c[2] - 0.02 and y < head_c[1])
    elif acc == "belly":  # gorge claire
        paint_faces(head, M["TP_Accent"], lambda x, y, z: z < head_c[2] - 0.06)
    elif acc == "face":  # masque facial clair (manchot)
        paint_faces(head, M["TP_Accent"], lambda x, y, z: y < head_c[1] - hr * 0.35 and z < head_c[2] + 0.07)
    parts.append(head)

    if sk in ("taper", "muzzle"):
        nz = snout_tip[2] + (0.012 if sk == "taper" else sr * 0.55)
        parts.append(ellipsoid("Nose", (snout_tip[0], snout_tip[1] - (0.005 if sk == "taper" else sr * 0.6), nz), (0.026, 0.02, 0.018), M["TP_Nose"]))
    elif sk == "flat":
        parts.append(ellipsoid("Mouth", (0, front + 0.035, head_c[2] - 0.06), (hr * 0.55, 0.01, 0.005), M["TP_Dark"]))
    elif sk == "beak":
        base = (0, front + 0.02, head_c[2] - 0.02)
        parts.append(cone("Beak", base, (0, front - sl, head_c[2] - 0.045), sr, M["TP_Beak"]))
    elif sk == "disc":
        # Groin : disque saillant + narines.
        bpy.ops.mesh.primitive_cylinder_add(vertices=28, radius=sr, depth=sl, location=(0, front + 0.01 - sl * 0.5, head_c[2] - 0.035),
                                            rotation=(math.radians(90), 0, 0))
        g = bpy.context.active_object
        g.name = "Snout"
        bev = g.modifiers.new("Bev", "BEVEL")
        bev.width = 0.012
        bev.segments = 3
        apply_all(g)
        g.data.materials.append(M["TP_Accent"])
        smooth(g)
        g.select_set(False)
        parts.append(g)
        for sx in (-1, 1):
            parts.append(ellipsoid(f"Nostril{sx}", (0.02 * sx, front + 0.01 - sl, head_c[2] - 0.035), (0.01, 0.006, 0.016), M["TP_Nose"]))

    # Yeux en amande, légèrement obliques (regard concentré), reflet clair, arcades sombres.
    for sx in (-1, 1):
        if a.get("eyes") == "bulge":
            # Grenouille : yeux globuleux sur le dessus, devant le casque.
            bc = (0.075 * sx, front + hr * 0.42, head_c[2] + hr * 0.42)
            parts.append(ellipsoid(f"EyeBall{sx}", bc, (0.052, 0.05, 0.05), M["TP_Fur"]))
            ex, ey, ez = bc[0] * 1.08, bc[1] - 0.046, bc[2] + 0.006
            parts.append(ellipsoid(f"Eye{sx}", (ex, ey, ez), (0.03, 0.01, 0.018), M["TP_Eye"]))
        else:
            ex, ey, ez = 0.058 * sx, head_c[1] - hr * 0.86, head_c[2] + 0.025
            if acc == "patches":  # taches du panda
                parts.append(ellipsoid(f"Patch{sx}", (ex * 1.05, ey + 0.012, ez - 0.006), (0.044, 0.02, 0.034), M["TP_Accent"], rot=(0, 28 * sx, 0)))
                ey -= 0.006
            parts.append(ellipsoid(f"Eye{sx}", (ex, ey, ez), (0.026, 0.012, 0.016), M["TP_Eye"], rot=(0, 18 * sx, 0)))
            parts.append(ellipsoid(f"Brow{sx}", (ex * 1.05, ey + 0.004, ez + 0.026), (0.034, 0.01, 0.008), M["TP_Dark"], rot=(0, -20 * sx, 0)))
        parts.append(ellipsoid(f"Glint{sx}", (ex + 0.008 * sx, ey - 0.009, ez + 0.006), (0.006, 0.004, 0.006), M["TP_Glint"], segments=10))

    # Casque ouvert à la livrée : calotte + mentonnière découpée, bande centrale, manchons d'oreilles.
    hz = 0.035 if a.get("eyes") != "bulge" else 0.06
    helmet = ellipsoid("Helmet", (head_c[0], head_c[1] + 0.025, head_c[2] + hz), (hr * 1.1, hr * 1.12, hr * 1.04), M["TP_Helmet"], segments=40)
    cut = ellipsoid("HelmetCut", (0, head_c[1] - hr * 1.0, head_c[2] - 0.035), (hr * 0.95, hr * 0.95, hr * (0.8 if hz < 0.05 else 0.95)), M["TP_Helmet"])
    boolean(helmet, cut, "DIFFERENCE")
    paint_faces(helmet, M["TP_Trim"], lambda x, y, z: abs(x) < 0.03 and z > head_c[2] + 0.05)
    parts.append(helmet)

    if a["ears"]:
        ek, eh, ew, egap, etilt = a["ears"]
        ear_mat = M[a.get("ear_mat", "TP_Fur")]
        for sx in (-1, 1):
            base = (egap * sx * 1.2, head_c[1] + 0.03, head_c[2] + hr * 0.95)
            if ek == "pointed":
                tip = (base[0] + math.sin(math.radians(etilt)) * eh * sx, head_c[1] + 0.06, base[2] + eh)
                ear = cone(f"Ear{sx}", base, tip, ew, ear_mat)
                if a.get("ear_tips"):
                    paint_faces(ear, M["TP_Dark"], lambda x, y, z, b=base: z > b[2] + eh * 0.62)
                if a.get("ear_inner"):
                    parts.append(cone(f"EarIn{sx}", (base[0], base[1] - 0.03, base[2] + 0.01), (tip[0] * 0.97, tip[1] - 0.025, tip[2] - eh * 0.15), ew * 0.55, M["TP_Accent"]))
            elif ek == "round":
                c = (egap * sx * 1.25, head_c[1] + 0.02, head_c[2] + hr * 0.92)
                ear = ellipsoid(f"Ear{sx}", c, (eh, eh * 0.45, eh), ear_mat, rot=(0, -25 * sx, 0))
            elif ek == "long":
                c = (base[0] + math.sin(math.radians(etilt)) * eh * 0.5 * sx, head_c[1] + 0.05, base[2] + eh * 0.5)
                ear = ellipsoid(f"Ear{sx}", c, (ew, ew * 0.5, eh * 0.55), ear_mat, rot=(-8, -etilt * sx, 0))
                parts.append(ellipsoid(f"EarIn{sx}", (c[0], c[1] - ew * 0.3, c[2] + 0.01), (ew * 0.6, ew * 0.3, eh * 0.45), M["TP_Accent"], rot=(-8, -etilt * sx, 0)))
            else:  # floppy : oreilles tombant vers l'avant
                tip = (base[0] + 0.05 * sx, base[1] - 0.13, base[2] - 0.01)
                ear = cone(f"Ear{sx}", base, tip, ew, ear_mat)
            parts.append(ear)
            sleeve_tip = (base[0], base[1] + 0.005, base[2] + 0.05)
            parts.append(cone(f"EarSleeve{sx}", base, sleeve_tip, ew * 1.02 if ek != "round" else eh * 0.75, M["TP_Helmet"]))

    # Queue qui dépasse derrière le baquet.
    if a["tail"]:
        tk, tr = a["tail"]
        if tk == "bushy":
            t = skin_object("Tail", [(0, 0.12, 0.12), (0.08, 0.32, 0.18), (0.22, 0.5, 0.32), (0.34, 0.6, 0.5)], [(0, 1), (1, 2), (2, 3)],
                            [0.05, tr, tr * 0.95, tr * 0.3], M["TP_Fur"], levels=3)
            paint_faces(t, M["TP_Accent"], lambda x, y, z: z > 0.4)
        elif tk == "puff":
            t = ellipsoid("Tail", (0, 0.16 + tr * 0.5, 0.16), (tr, tr, tr), M["TP_Fur"])
        elif tk == "thin":
            t = skin_object("Tail", [(0, 0.12, 0.12), (0.06, 0.3, 0.16), (0.16, 0.42, 0.34), (0.14, 0.44, 0.52)], [(0, 1), (1, 2), (2, 3)],
                            [tr, tr, tr * 0.9, tr * 0.7], M["TP_Fur"], levels=2)
        else:  # curl : tire-bouchon
            pts = [(0.05 * math.sin(i * 1.2), 0.15 + 0.04 * i * 0.3 + 0.04 * math.cos(i * 1.2), 0.18 + 0.012 * i) for i in range(9)]
            t = skin_object("Tail", pts, [(i, i + 1) for i in range(8)], [tr] * 9, M["TP_Fur"], levels=2)
        parts.append(t)

    bpy.data.objects.remove(bpy.data.objects["HelmetCut"], do_unlink=True)
    for ob in parts:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    pilot = bpy.context.active_object
    pilot.name = f"Pilot_{animal}"
    return pilot


def boolean(target, cutter, op):
    mod = target.modifiers.new("Bool", "BOOLEAN")
    mod.operation = op
    mod.object = cutter
    mod.solver = "EXACT"
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.modifier_apply(modifier=mod.name)


# ------------------------------------------------------------------ aperçu et export

def preview(path):
    scene = bpy.context.scene
    cam_data = bpy.data.cameras.new("Cam")
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    cam.location = (-1.25, -2.0, 1.05)
    cam.rotation_euler = (math.radians(76), 0, math.radians(-32))
    cam_data.lens = 50
    scene.camera = cam
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
    sun.rotation_euler = (math.radians(50), 0, math.radians(-40))
    scene.collection.objects.link(sun)
    scene.render.engine = "BLENDER_WORKBENCH"
    shading = scene.display.shading
    shading.light = "STUDIO"
    shading.color_type = "MATERIAL"
    shading.show_shadows = True
    shading.show_cavity = True
    scene.render.resolution_x = 900
    scene.render.resolution_y = 900
    scene.render.film_transparent = False
    world = bpy.data.worlds.new("W")
    world.color = (0.95, 0.91, 0.81)
    scene.world = world
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def export(path):
    bpy.ops.object.select_all(action="DESELECT")
    for ob in bpy.data.objects:
        if ob.type == "MESH":
            ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE",
                             use_mesh_modifiers=True, add_leaf_bones=False, path_mode="AUTO")


def main():
    o = args()
    reset()
    pilot = build(o.animal)
    print(f"[pilote] {pilot.name} : {len(pilot.data.vertices)} sommets, {len(pilot.data.polygons)} faces")
    if o.preview:
        preview(o.preview)
    export(o.out)
    print(f"[pilote] exporté -> {o.out}")


main()

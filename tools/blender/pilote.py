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
    # snout : longueur du museau ; ears : (hauteur, largeur, écart, inclinaison) ; tail : rayon max (0 = pas de queue)
    "fox": dict(snout=0.15, snout_r=0.05, ears=(0.17, 0.075, 0.09, 14), ear_kind="pointed", tail=0.12, head_r=0.15,
                fur=(1.0, 0.5, 0.16), accent=(0.97, 0.94, 0.9), tail_tip=True),
}

PALETTE = dict(
    TP_Suit=(0.235, 0.561, 0.569), TP_Trim=(0.851, 0.302, 0.247), TP_Dark=(0.11, 0.13, 0.12), TP_Helmet=(0.235, 0.561, 0.569),
    TP_Visor=(0.05, 0.06, 0.07), TP_Eye=(0.06, 0.05, 0.04), TP_Nose=(0.04, 0.04, 0.04),
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
    M["TP_Accent"] = material("TP_Accent", a["accent"])
    parts = []

    # Combinaison : bassin, buste en V (épaules larges), bras tendus vers le volant, jambes vers les pédales.
    v = [
        (0, 0.0, 0.12),      # 0 bassin
        (0, -0.01, 0.30),    # 1 taille
        (0, -0.03, 0.50),    # 2 poitrine
        (0, -0.02, 0.70),    # 3 col
    ]
    r = [(0.19, 0.15), (0.155, 0.12), (0.235, 0.15), (0.1, 0.09)]
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
    paint_faces(suit, M["TP_Trim"], lambda x, y, z: abs(abs(x) - 0.235) < 0.035 and z > 0.5 and y > -0.12)
    # Col montant sombre.
    paint_faces(suit, M["TP_Dark"], lambda x, y, z: z > 0.64)
    parts.append(suit)

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

    # Tête : crâne + museau effilé (Skin), cou en fourrure.
    hr = a["head_r"]
    head_c = (0, -0.05, 0.85)
    snout_tip = (0, head_c[1] - hr - a["snout"], head_c[2] - 0.05)
    head = skin_object("Head", [(0, -0.01, 0.66), head_c, (0, head_c[1] - hr * 0.7, head_c[2] - 0.03), snout_tip],
                       [(0, 1), (1, 2), (2, 3)], [(0.075, 0.07), (hr, hr * 1.02), (hr * 0.62, hr * 0.5), (a["snout_r"] * 0.55, a["snout_r"] * 0.45)],
                       M["TP_Fur"], levels=3)
    # Dessous du museau et joues clairs.
    paint_faces(head, M["TP_Accent"], lambda x, y, z: z < head_c[2] - 0.045 and y < head_c[1] - 0.04)
    paint_faces(head, M["TP_Accent"], lambda x, y, z: abs(x) > hr * 0.55 and z < head_c[2] - 0.02 and y < head_c[1])
    parts.append(head)
    parts.append(ellipsoid("Nose", (snout_tip[0], snout_tip[1] - 0.005, snout_tip[2] + 0.012), (0.026, 0.02, 0.018), M["TP_Nose"]))

    # Yeux en amande, légèrement obliques (regard concentré), et arcades sombres.
    for sx in (-1, 1):
        ex, ey, ez = 0.058 * sx, head_c[1] - hr * 0.86, head_c[2] + 0.025
        parts.append(ellipsoid(f"Eye{sx}", (ex, ey, ez), (0.026, 0.012, 0.016), M["TP_Eye"], rot=(0, 18 * sx, 0)))
        parts.append(ellipsoid(f"Brow{sx}", (ex * 1.05, ey + 0.004, ez + 0.026), (0.034, 0.01, 0.008), M["TP_Dark"], rot=(0, -20 * sx, 0)))

    # Casque ouvert à la livrée : calotte + mentonnière, visière relevée sombre, manchons d'oreilles.
    helmet = ellipsoid("Helmet", (head_c[0], head_c[1] + 0.025, head_c[2] + 0.035), (hr * 1.1, hr * 1.12, hr * 1.04), M["TP_Helmet"], segments=40)
    cut = ellipsoid("HelmetCut", (0, head_c[1] - hr * 1.0, head_c[2] - 0.035), (hr * 0.95, hr * 0.95, hr * 0.8), M["TP_Helmet"])
    boolean(helmet, cut, "DIFFERENCE")
    paint_faces(helmet, M["TP_Trim"], lambda x, y, z: abs(x) < 0.03 and z > head_c[2] + 0.05)
    parts.append(helmet)

    eh, ew, egap, etilt = a["ears"]
    for sx in (-1, 1):
        base = (egap * sx * 1.2, head_c[1] + 0.03, head_c[2] + hr * 0.95)
        tip = (egap * sx * 1.2 + math.sin(math.radians(etilt)) * eh * sx, head_c[1] + 0.06, head_c[2] + hr * 0.95 + eh)
        ear = cone(f"Ear{sx}", base, tip, ew, M["TP_Fur"])
        paint_faces(ear, M["TP_Dark"], lambda x, y, z: z > base[2] + eh * 0.62)  # pointes sombres
        parts.append(ear)
        parts.append(cone(f"EarSleeve{sx}", base, (base[0] + (tip[0] - base[0]) * 0.35, base[1] + 0.01, base[2] + eh * 0.35), ew * 1.12, M["TP_Helmet"]))

    # Queue touffue qui dépasse derrière le baquet.
    if a["tail"] > 0:
        t = skin_object("Tail", [(0, 0.12, 0.12), (0.08, 0.32, 0.18), (0.22, 0.5, 0.32), (0.34, 0.6, 0.5)], [(0, 1), (1, 2), (2, 3)],
                        [0.05, a["tail"], a["tail"] * 0.95, a["tail"] * 0.3], M["TP_Fur"], levels=3)
        if a.get("tail_tip"):
            paint_faces(t, M["TP_Accent"], lambda x, y, z: z > 0.4)
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

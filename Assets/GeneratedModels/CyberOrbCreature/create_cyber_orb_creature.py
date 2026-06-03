import math
import os
from mathutils import Vector

import bpy


ROOT = os.path.dirname(os.path.abspath(__file__))
BLEND_PATH = os.path.join(ROOT, "CyberOrbCreature.blend")
FBX_PATH = os.path.join(ROOT, "CyberOrbCreature.fbx")
PREVIEW_PATH = os.path.join(ROOT, "CyberOrbCreature_preview.png")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_mat(name, color, roughness=0.45, metallic=0.0, alpha=1.0, emission=None, strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    rgba = (color[0], color[1], color[2], alpha)
    if bsdf:
        bsdf.inputs["Base Color"].default_value = rgba
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
        bsdf.inputs["Alpha"].default_value = alpha
        if emission:
            bsdf.inputs["Emission Color"].default_value = (emission[0], emission[1], emission[2], 1)
            bsdf.inputs["Emission Strength"].default_value = strength
    mat.diffuse_color = rgba
    if alpha < 1.0:
        mat.blend_method = "BLEND"
        mat.use_screen_refraction = True
    return mat


def smooth(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth()
    obj.select_set(False)
    obj.modifiers.new("Soft game normals", "WEIGHTED_NORMAL")
    return obj


def add_sphere(name, location, scale, material, segments=40, rings=20, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=segments,
        ring_count=rings,
        radius=1.0,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    smooth(obj)
    return obj


def add_cylinder_between(name, start, end, radius, material, vertices=18):
    start_v = Vector(start)
    end_v = Vector(end)
    mid = (start_v + end_v) * 0.5
    direction = end_v - start_v
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=direction.length, location=mid)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    obj.data.materials.append(material)
    smooth(obj)
    bevel = obj.modifiers.new("Soft segment caps", "BEVEL")
    bevel.width = radius * 0.16
    bevel.segments = 2
    return obj


def add_cone(name, location, radius1, radius2, depth, material, vertices=28, rotation=(0, 0, 0), scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius1,
        radius2=radius2,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    smooth(obj)
    return obj


def add_torus(name, location, major, minor, material, rotation=(0, 0, 0), scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_torus_add(
        major_segments=64,
        minor_segments=12,
        major_radius=major,
        minor_radius=minor,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    smooth(obj)
    return obj


def add_panel_line(name, location, scale, material, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    bevel = obj.modifiers.new("Rounded neon strip", "BEVEL")
    bevel.width = min(scale) * 0.4
    bevel.segments = 3
    smooth(obj)
    return obj


def add_triangle_plate(name, center, size, thickness, material, rotation=(0, 0, 0)):
    h = size * math.sqrt(3) / 2
    verts = [
        (-size / 2, -h / 3, -thickness / 2),
        (size / 2, -h / 3, -thickness / 2),
        (0, 2 * h / 3, -thickness / 2),
        (-size / 2, -h / 3, thickness / 2),
        (size / 2, -h / 3, thickness / 2),
        (0, 2 * h / 3, thickness / 2),
    ]
    faces = [(0, 1, 2), (3, 5, 4), (0, 3, 4, 1), (1, 4, 5, 2), (2, 5, 3, 0)]
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = center
    obj.rotation_euler = rotation
    obj.data.materials.append(material)
    smooth(obj)
    return obj


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def create_creature():
    navy = make_mat("CyberOrb_Deep_Navy", (0.04, 0.045, 0.16), 0.38, 0.15)
    navy_dark = make_mat("CyberOrb_Dark_Panels", (0.018, 0.02, 0.07), 0.48, 0.08)
    silver = make_mat("CyberOrb_Silver_Rim", (0.86, 0.90, 0.84), 0.26, 0.45)
    glass = make_mat("CyberOrb_Green_Glass", (0.0, 0.76, 0.58), 0.20, 0.0, alpha=0.72, emission=(0.0, 0.55, 0.42), strength=0.15)
    cyan = make_mat("CyberOrb_Cyan_Glow", (0.0, 1.0, 0.86), 0.32, 0.0, emission=(0.0, 1.0, 0.86), strength=1.2)
    pink = make_mat("CyberOrb_Pink_Glow", (1.0, 0.25, 0.70), 0.35, 0.0, emission=(1.0, 0.18, 0.72), strength=0.9)
    white = make_mat("CyberOrb_Eye_White", (0.96, 0.98, 0.95), 0.3)
    black = make_mat("CyberOrb_Eye_Black", (0.02, 0.025, 0.03), 0.35)
    teal = make_mat("CyberOrb_Teal_Crystal", (0.0, 0.88, 0.78), 0.22, 0.0, emission=(0.0, 0.8, 0.7), strength=0.35)

    objects = []

    objects.append(add_sphere("CyberOrb_Body", (0, 0, 1.25), (0.88, 0.80, 0.92), navy, 56, 28))

    # Front face assembly. The character faces negative Y in Unity.
    objects.append(add_sphere("CyberOrb_GlassFace", (0, -0.68, 1.24), (0.56, 0.055, 0.42), glass, 40, 18))
    objects.append(add_torus("CyberOrb_FaceSilverRing", (0, -0.705, 1.24), 0.49, 0.055, silver, rotation=(math.radians(90), 0, 0), scale=(1.17, 1.0, 1.0)))
    objects.append(add_torus("CyberOrb_CyanNeckRing", (0, -0.04, 2.03), 0.47, 0.027, cyan, rotation=(0, 0, 0), scale=(1.0, 0.94, 1.0)))

    for x in (-0.24, 0.24):
        objects.append(add_sphere(f"CyberOrb_EyeWhite_{x:+.2f}", (x, -0.74, 1.28), (0.16, 0.024, 0.20), white, 28, 14))
        objects.append(add_torus(f"CyberOrb_EyeCyanRing_{x:+.2f}", (x, -0.766, 1.27), 0.075, 0.015, cyan, rotation=(math.radians(90), 0, 0), scale=(0.82, 1.15, 1.0)))
        objects.append(add_sphere(f"CyberOrb_Pupil_{x:+.2f}", (x, -0.785, 1.25), (0.050, 0.012, 0.068), black, 20, 10))
        objects.append(add_sphere(f"CyberOrb_EyeHighlight_{x:+.2f}", (x - 0.045, -0.798, 1.34), (0.032, 0.008, 0.046), white, 16, 8))

    objects.append(add_sphere("CyberOrb_TopDome", (0.02, -0.06, 2.13), (0.35, 0.30, 0.23), navy, 32, 16))
    objects.append(add_cone("CyberOrb_AntennaHorn", (-0.34, -0.21, 2.38), 0.13, 0.025, 0.72, silver, 24, rotation=(math.radians(73), math.radians(-30), math.radians(-46)), scale=(1.0, 0.75, 1.0)))
    objects.append(add_panel_line("CyberOrb_HornCyanStripe", (-0.39, -0.28, 2.39), (0.018, 0.022, 0.25), cyan, rotation=(math.radians(46), math.radians(-34), math.radians(-48))))
    objects.append(add_panel_line("CyberOrb_HornPinkEdge", (-0.31, -0.29, 2.44), (0.014, 0.018, 0.31), pink, rotation=(math.radians(48), math.radians(-34), math.radians(-48))))

    # Decorative shell panel lines on the body.
    objects.append(add_panel_line("CyberOrb_LeftPanelLine", (-0.56, -0.37, 1.28), (0.018, 0.015, 0.40), silver, rotation=(math.radians(18), math.radians(-15), math.radians(-35))))
    objects.append(add_panel_line("CyberOrb_RightPanelLine", (0.58, -0.34, 1.42), (0.018, 0.015, 0.36), silver, rotation=(math.radians(-12), math.radians(16), math.radians(30))))
    objects.append(add_panel_line("CyberOrb_BackPanelLine", (0.00, 0.68, 1.24), (0.018, 0.015, 0.42), navy_dark, rotation=(math.radians(90), 0, 0)))

    # Tail made of mesh segments so the FBX imports cleanly in Unity.
    tail_points = [
        (-0.72, 0.30, 1.04),
        (-1.06, 0.34, 0.90),
        (-1.24, 0.12, 0.78),
        (-1.12, -0.12, 0.78),
        (-0.92, -0.18, 0.92),
    ]
    for i in range(len(tail_points) - 1):
        objects.append(add_cylinder_between(f"CyberOrb_TailSegment_{i}", tail_points[i], tail_points[i + 1], 0.055, navy_dark, 18))
        objects.append(add_sphere(f"CyberOrb_TailJoint_{i}", tail_points[i], (0.075, 0.075, 0.075), navy, 18, 8))
    objects.append(add_sphere("CyberOrb_TailGlowJoint", tail_points[-1], (0.095, 0.095, 0.095), cyan, 18, 8))
    objects.append(add_triangle_plate("CyberOrb_TailArrowSilver", (-1.31, -0.20, 0.90), 0.50, 0.055, silver, rotation=(math.radians(18), math.radians(8), math.radians(93))))
    objects.append(add_triangle_plate("CyberOrb_TailArrowPinkCore", (-1.31, -0.205, 0.90), 0.31, 0.062, pink, rotation=(math.radians(18), math.radians(8), math.radians(93))))

    # Small hovering fins/feet.
    for name, x, zrot in (("Left", -0.38, 22), ("Right", 0.38, -22)):
        objects.append(add_sphere(f"CyberOrb_{name}FootPod", (x, -0.15, 0.38), (0.20, 0.30, 0.14), navy, 26, 12, rotation=(0, math.radians(10 if x < 0 else -10), math.radians(zrot))))
        objects.append(add_sphere(f"CyberOrb_{name}FootCrystal", (x, -0.35, 0.42), (0.075, 0.025, 0.13), teal, 18, 8, rotation=(0, 0, math.radians(zrot))))

    objects.append(add_sphere("CyberOrb_RightSideFin", (0.88, -0.06, 1.01), (0.18, 0.33, 0.14), navy, 26, 12, rotation=(0, math.radians(-22), math.radians(-28))))
    objects.append(add_sphere("CyberOrb_RightSideCrystal", (0.98, -0.23, 1.03), (0.075, 0.026, 0.12), teal, 18, 8, rotation=(0, math.radians(-22), math.radians(-28))))

    return objects


def save_clean_asset(objects):
    bpy.context.preferences.filepaths.save_version = 0
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"MESH"},
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        path_mode="AUTO",
    )


def render_preview():
    preview = []
    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.color = (0.02, 0.09, 0.13)

    bpy.ops.object.light_add(type="AREA", location=(-3.0, -4.5, 4.5))
    key = bpy.context.object
    key.name = "Preview_KeyLight"
    key.data.energy = 650
    key.data.size = 5.0
    preview.append(key)

    bpy.ops.object.camera_add(location=(3.1, -5.4, 2.45))
    cam = bpy.context.object
    cam.name = "Preview_Camera"
    look_at(cam, (0, -0.1, 1.25))
    cam.data.lens = 58
    bpy.context.scene.camera = cam
    preview.append(cam)

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.studio_light = "studio.sl"
    scene.display.shading.color_type = "MATERIAL"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 800
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "Medium High Contrast"
    scene.render.filepath = PREVIEW_PATH
    bpy.ops.render.render(write_still=True)

    bpy.context.scene.camera = None
    for obj in preview:
        if obj.name in bpy.data.objects:
            bpy.data.objects.remove(obj, do_unlink=True)


def main():
    clear_scene()
    objects = create_creature()
    save_clean_asset(objects)
    render_preview()
    print(f"Created {BLEND_PATH}")
    print(f"Created {FBX_PATH}")
    print(f"Created {PREVIEW_PATH}")


if __name__ == "__main__":
    main()

import math
import os
from mathutils import Euler, Vector

import bpy


ROOT = os.path.dirname(os.path.abspath(__file__))
BLEND_PATH = os.path.join(ROOT, "ChibiCharacter.blend")
FBX_PATH = os.path.join(ROOT, "ChibiCharacter.fbx")
PREVIEW_PATH = os.path.join(ROOT, "ChibiCharacter_preview.png")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_mat(name, color, roughness=0.55):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
    mat.diffuse_color = color
    return mat


def smooth_object(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth()
    obj.select_set(False)
    obj.modifiers.new("Soft game normals", "WEIGHTED_NORMAL")
    return obj


def add_sphere(name, location, scale, material, segments=32, rings=16, rotation=(0, 0, 0)):
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
    smooth_object(obj)
    return obj


def add_cylinder(name, location, radius, depth, material, vertices=18, rotation=(0, 0, 0), scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    smooth_object(obj)
    bevel = obj.modifiers.new("Rounded edges", "BEVEL")
    bevel.width = 0.025
    bevel.segments = 2
    return obj


def add_cube(name, location, scale, material, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    bevel = obj.modifiers.new("Rounded game bevel", "BEVEL")
    bevel.width = 0.06
    bevel.segments = 4
    smooth_object(obj)
    return obj


def create_hair_cap(material):
    segments = 36
    rings = 8
    verts = []
    faces = []

    for i in range(rings + 1):
        theta = math.radians(18 + i * 76 / rings)
        z = math.cos(theta) * 0.72
        r = math.sin(theta)
        for j in range(segments):
            a = 2 * math.pi * j / segments
            x = math.cos(a) * r * 0.71
            y = math.sin(a) * r * 0.66
            # Keep the cap high on the forehead while staying fuller behind the head.
            if y < -0.18:
                z += 0.16 * abs(y)
            else:
                z -= 0.05 * y
            verts.append((x, y, z))

    for i in range(rings):
        for j in range(segments):
            a = i * segments + j
            b = i * segments + (j + 1) % segments
            c = (i + 1) * segments + (j + 1) % segments
            d = (i + 1) * segments + j
            faces.append((a, b, c, d))

    mesh = bpy.data.meshes.new("Chibi_HairCapMesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    obj = bpy.data.objects.new("Chibi_HairCap", mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = (0, -0.02, 2.28)
    obj.data.materials.append(material)
    smooth_object(obj)
    return obj


def create_bang(name, location, scale, material, rotation):
    bpy.ops.mesh.primitive_uv_sphere_add(
        segments=18,
        ring_count=10,
        radius=1.0,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    obj.data.materials.append(material)
    smooth_object(obj)
    return obj


def create_chibi():
    skin = make_mat("Chibi_Skin", (1.0, 0.73, 0.56, 1), 0.62)
    blush = make_mat("Chibi_Blush", (1.0, 0.40, 0.45, 1), 0.7)
    eye = make_mat("Chibi_Eye_Black", (0.03, 0.025, 0.025, 1), 0.38)
    eye_glint = make_mat("Chibi_Eye_Glint", (1.0, 0.96, 0.88, 1), 0.25)
    hair = make_mat("Chibi_Hair_Chestnut", (0.30, 0.12, 0.055, 1), 0.58)
    shirt = make_mat("Chibi_Hoodie_Blue", (0.08, 0.45, 0.95, 1), 0.54)
    shirt_dark = make_mat("Chibi_Hoodie_DarkBlue", (0.035, 0.18, 0.42, 1), 0.64)
    shorts = make_mat("Chibi_Shorts_Navy", (0.05, 0.07, 0.15, 1), 0.65)
    shoe = make_mat("Chibi_Shoes_White", (0.92, 0.91, 0.84, 1), 0.5)
    sole = make_mat("Chibi_Shoe_Sole", (0.12, 0.12, 0.13, 1), 0.7)

    objects = []

    objects.append(add_sphere("Chibi_Head", (0, 0, 2.18), (0.66, 0.61, 0.70), skin))
    objects.append(create_hair_cap(hair))

    objects.append(add_sphere("Chibi_BackHair", (0, 0.24, 2.13), (0.64, 0.30, 0.60), hair, 28, 14))
    objects.append(create_bang("Chibi_Bang_Left", (-0.27, -0.55, 2.48), (0.095, 0.030, 0.23), hair, (math.radians(8), math.radians(-14), math.radians(-28))))
    objects.append(create_bang("Chibi_Bang_Mid", (0.00, -0.57, 2.48), (0.115, 0.032, 0.27), hair, (math.radians(6), 0, math.radians(2))))
    objects.append(create_bang("Chibi_Bang_Right", (0.25, -0.55, 2.46), (0.095, 0.030, 0.22), hair, (math.radians(8), math.radians(14), math.radians(26))))

    objects.append(add_sphere("Chibi_Eye_L", (-0.23, -0.59, 2.18), (0.105, 0.026, 0.16), eye, 24, 12))
    objects.append(add_sphere("Chibi_Eye_R", (0.23, -0.59, 2.18), (0.105, 0.026, 0.16), eye, 24, 12))
    objects.append(add_sphere("Chibi_EyeGlint_L", (-0.265, -0.613, 2.245), (0.028, 0.008, 0.042), eye_glint, 16, 8))
    objects.append(add_sphere("Chibi_EyeGlint_R", (0.195, -0.613, 2.245), (0.028, 0.008, 0.042), eye_glint, 16, 8))
    objects.append(add_sphere("Chibi_Blush_L", (-0.36, -0.60, 2.00), (0.095, 0.014, 0.04), blush, 16, 8))
    objects.append(add_sphere("Chibi_Blush_R", (0.36, -0.60, 2.00), (0.095, 0.014, 0.04), blush, 16, 8))
    objects.append(add_sphere("Chibi_Mouth", (0, -0.615, 1.94), (0.065, 0.012, 0.022), eye, 16, 8))

    objects.append(add_sphere("Chibi_Body_Hoodie", (0, 0, 1.25), (0.42, 0.32, 0.48), shirt, 28, 14))
    objects.append(add_sphere("Chibi_Hood", (0, 0.16, 1.58), (0.46, 0.20, 0.25), shirt_dark, 24, 12))
    objects.append(add_cube("Chibi_Shorts", (0, -0.01, 0.82), (0.34, 0.28, 0.18), shorts))

    objects.append(add_cylinder("Chibi_Arm_L", (-0.48, -0.02, 1.22), 0.085, 0.58, skin, rotation=(math.radians(8), math.radians(0), math.radians(-24)), scale=(1.0, 1.0, 1.0)))
    objects.append(add_cylinder("Chibi_Arm_R", (0.48, -0.02, 1.22), 0.085, 0.58, skin, rotation=(math.radians(8), math.radians(0), math.radians(24)), scale=(1.0, 1.0, 1.0)))
    objects.append(add_sphere("Chibi_Hand_L", (-0.61, -0.05, 0.95), (0.10, 0.09, 0.10), skin, 18, 10))
    objects.append(add_sphere("Chibi_Hand_R", (0.61, -0.05, 0.95), (0.10, 0.09, 0.10), skin, 18, 10))

    objects.append(add_cylinder("Chibi_Leg_L", (-0.16, 0, 0.50), 0.095, 0.48, skin, rotation=(0, 0, 0), scale=(1.0, 0.88, 1.0)))
    objects.append(add_cylinder("Chibi_Leg_R", (0.16, 0, 0.50), 0.095, 0.48, skin, rotation=(0, 0, 0), scale=(1.0, 0.88, 1.0)))
    objects.append(add_sphere("Chibi_Shoe_L", (-0.17, -0.05, 0.22), (0.18, 0.25, 0.075), shoe, 20, 10))
    objects.append(add_sphere("Chibi_Shoe_R", (0.17, -0.05, 0.22), (0.18, 0.25, 0.075), shoe, 20, 10))
    objects.append(add_cube("Chibi_Sole_L", (-0.17, -0.10, 0.15), (0.18, 0.20, 0.025), sole))
    objects.append(add_cube("Chibi_Sole_R", (0.17, -0.10, 0.15), (0.18, 0.20, 0.025), sole))

    return objects


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


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
    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.color = (0.02, 0.12, 0.16)

    preview_objects = []
    bpy.ops.object.light_add(type="AREA", location=(-3.0, -4.0, 5.0))
    key = bpy.context.object
    key.name = "Preview_KeyLight"
    key.data.energy = 520
    key.data.size = 5.0
    preview_objects.append(key)

    bpy.ops.object.camera_add(location=(2.8, -5.1, 2.4))
    cam = bpy.context.object
    cam.name = "Preview_Camera"
    look_at(cam, (0, 0, 1.35))
    cam.data.lens = 55
    bpy.context.scene.camera = cam
    preview_objects.append(cam)

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.studio_light = "studio.sl"
    scene.display.shading.color_type = "MATERIAL"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 700
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "Medium High Contrast"
    scene.render.filepath = PREVIEW_PATH
    bpy.ops.render.render(write_still=True)

    bpy.context.scene.camera = None
    for obj in preview_objects:
        if obj.name in bpy.data.objects:
            bpy.data.objects.remove(obj, do_unlink=True)


def main():
    clear_scene()
    objects = create_chibi()
    save_clean_asset(objects)
    render_preview()
    print(f"Created {BLEND_PATH}")
    print(f"Created {FBX_PATH}")
    print(f"Created {PREVIEW_PATH}")


if __name__ == "__main__":
    main()

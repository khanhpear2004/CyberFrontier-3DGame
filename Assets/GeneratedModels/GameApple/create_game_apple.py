import math
import os
import random
from mathutils import Matrix, Vector, Euler

import bpy


ROOT = os.path.dirname(os.path.abspath(__file__))
BLEND_PATH = os.path.join(ROOT, "GameApple.blend")
FBX_PATH = os.path.join(ROOT, "GameApple.fbx")
PREVIEW_PATH = os.path.join(ROOT, "GameApple_preview.png")
TEXTURE_PATH = os.path.join(ROOT, "GameApple_BaseColor.png")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_mat(name, color, roughness=0.55, metallic=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
    mat.diffuse_color = color
    return mat


def smoothstep(edge0, edge1, x):
    t = min(1.0, max(0.0, (x - edge0) / (edge1 - edge0)))
    return t * t * (3.0 - 2.0 * t)


def mix_color(a, b, t):
    return tuple(a[i] * (1.0 - t) + b[i] * t for i in range(3))


def create_body_texture():
    width = 768
    height = 384
    random.seed(7)
    image = bpy.data.images.new("GameApple_BaseColor", width, height, alpha=True)
    pixels = []

    red = (0.92, 0.12, 0.07)
    orange = (1.0, 0.38, 0.14)
    yellow = (1.0, 0.72, 0.17)
    dark = (0.38, 0.035, 0.025)
    speckles = [random.random() for _ in range(width * height)]

    for y in range(height):
        v = y / (height - 1)
        for x in range(width):
            u = x / (width - 1)
            col = red

            top = smoothstep(0.58, 0.86, v)
            col = mix_color(col, yellow, top * 0.82)

            bottom = 1.0 - smoothstep(0.05, 0.23, v)
            col = mix_color(col, dark, bottom * 0.75)

            # Soft orange blush on the camera-facing side.
            du = min(abs(u - 0.74), 1.0 - abs(u - 0.74))
            blush = math.exp(-((du / 0.22) ** 2 + ((v - 0.50) / 0.23) ** 2))
            col = mix_color(col, orange, blush * 0.65)

            # Very subtle painterly fruit texture, kept game-stylized.
            noise = (speckles[y * width + x] - 0.5) * 0.045
            col = tuple(min(1.0, max(0.0, c + noise)) for c in col)
            pixels.extend((col[0], col[1], col[2], 1.0))

    image.pixels[:] = pixels
    image.filepath_raw = TEXTURE_PATH
    image.file_format = "PNG"
    image.save()
    return image


def make_texture_mat(name, image, roughness=0.52):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = image
    if bsdf:
        mat.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        bsdf.inputs["Roughness"].default_value = roughness
    mat.diffuse_color = (0.9, 0.16, 0.08, 1)
    return mat


def shade_game_smooth(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.shade_smooth()
    obj.select_set(False)

    weighted = obj.modifiers.new("Soft game normals", "WEIGHTED_NORMAL")
    weighted.keep_sharp = True
    return obj


def create_apple_body(body_mat):
    segments = 40
    rings = 22
    verts = []

    for i in range(rings + 1):
        theta = math.pi * i / rings
        z_base = math.cos(theta)
        r_base = math.sin(theta)

        # Round game-apple silhouette: fuller shoulders, smaller bottom,
        # and a shallow cup at the stem.
        shoulder = 1.0 + 0.12 * math.exp(-((z_base - 0.20) / 0.45) ** 2)
        bottom_taper = 1.0 - 0.08 * max(0.0, -z_base) ** 1.8
        r = r_base * shoulder * bottom_taper * 0.96
        z = z_base * 1.12
        z -= 0.28 * math.exp(-(r / 0.36) ** 2) * max(0.0, z_base) ** 1.5
        z += 0.05 * math.exp(-(r / 0.44) ** 2) * max(0.0, -z_base) ** 2

        for j in range(segments):
            angle = 2.0 * math.pi * j / segments
            # A tiny asymmetry keeps the asset from looking like a perfect ball.
            wobble = 1.0 + 0.025 * math.sin(angle * 3.0 + 0.7) * math.sin(theta)
            x = math.cos(angle) * r * wobble
            y = math.sin(angle) * r * (0.96 + 0.03 * math.cos(angle - 0.4))
            verts.append((x, y, z))

    faces = []
    for i in range(rings):
        for j in range(segments):
            a = i * segments + j
            b = i * segments + (j + 1) % segments
            c = (i + 1) * segments + (j + 1) % segments
            d = (i + 1) * segments + j
            faces.append((a, b, c, d))

    mesh = bpy.data.meshes.new("GameApple_BodyMesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    obj = bpy.data.objects.new("GameApple_Body", mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(body_mat)

    uv_layer = obj.data.uv_layers.new(name="GameApple_UV")
    for poly in obj.data.polygons:
        for loop_index in poly.loop_indices:
            vertex_index = obj.data.loops[loop_index].vertex_index
            ring = vertex_index // segments
            segment = vertex_index % segments
            uv_layer.data[loop_index].uv = (segment / segments, 1.0 - ring / rings)

    shade_game_smooth(obj)
    obj.modifiers.new("Rounded game surface", "SUBSURF").levels = 1
    return obj


def create_stem(mat_stem, mat_dark):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=12,
        radius=0.055,
        depth=0.42,
        location=(0.02, -0.02, 0.99),
        rotation=(math.radians(13), math.radians(-15), math.radians(6)),
    )
    stem = bpy.context.object
    stem.name = "GameApple_Stem"
    stem.data.materials.append(mat_stem)
    shade_game_smooth(stem)
    bevel = stem.modifiers.new("Rounded stem edges", "BEVEL")
    bevel.width = 0.015
    bevel.segments = 2

    for offset in (-0.028, 0.0, 0.028):
        curve = bpy.data.curves.new("GameApple_StemGrooveCurve", "CURVE")
        curve.dimensions = "3D"
        curve.resolution_u = 2
        curve.bevel_depth = 0.006
        curve.bevel_resolution = 1
        spline = curve.splines.new("POLY")
        spline.points.add(1)
        spline.points[0].co = (offset, -0.054, -0.17, 1)
        spline.points[1].co = (offset * 0.35 + 0.018, -0.054, 0.17, 1)
        groove = bpy.data.objects.new("GameApple_StemGroove", curve)
        bpy.context.collection.objects.link(groove)
        groove.location = stem.location
        groove.rotation_euler = stem.rotation_euler
        groove.data.materials.append(mat_dark)

    return stem


def create_leaf(mat_leaf, mat_vein):
    rows = 8
    verts = []
    faces = []
    for i in range(rows + 1):
        t = i / rows
        length = 0.78
        width = 0.19 * math.sin(math.pi * t) * (1.0 - 0.2 * t)
        x = t * length
        z = 0.07 * math.sin(math.pi * t)
        verts.extend([(x, -width, z), (x, width, z)])
        if i < rows:
            a = i * 2
            faces.append((a, a + 1, a + 3, a + 2))

    mesh = bpy.data.meshes.new("GameApple_LeafMesh")
    mesh.from_pydata(verts, [], faces)
    mesh.update()

    leaf = bpy.data.objects.new("GameApple_Leaf", mesh)
    bpy.context.collection.objects.link(leaf)
    leaf.data.materials.append(mat_leaf)
    leaf.location = (0.07, -0.02, 1.12)
    leaf.rotation_euler = Euler((math.radians(3), math.radians(-13), math.radians(26)), "XYZ")
    shade_game_smooth(leaf)
    solid = leaf.modifiers.new("Thin leaf thickness", "SOLIDIFY")
    solid.thickness = 0.012

    curve = bpy.data.curves.new("GameApple_LeafVeinCurve", "CURVE")
    curve.dimensions = "3D"
    curve.resolution_u = 8
    curve.bevel_depth = 0.01
    curve.bevel_resolution = 1
    spline = curve.splines.new("POLY")
    spline.points.add(rows)
    for i in range(rows + 1):
        t = i / rows
        spline.points[i].co = (t * 0.78, 0.0, 0.075 * math.sin(math.pi * t) + 0.008, 1)

    vein = bpy.data.objects.new("GameApple_LeafVein", curve)
    bpy.context.collection.objects.link(vein)
    vein.location = leaf.location
    vein.rotation_euler = leaf.rotation_euler
    vein.data.materials.append(mat_vein)
    return leaf


def create_top_dimple(mat_shadow):
    bpy.ops.mesh.primitive_torus_add(
        major_radius=0.18,
        minor_radius=0.018,
        major_segments=28,
        minor_segments=8,
        location=(0.0, -0.01, 0.78),
        rotation=(0, 0, 0),
    )
    dimple = bpy.context.object
    dimple.name = "GameApple_StemCupShadow"
    dimple.scale = (1.35, 0.82, 0.24)
    dimple.data.materials.append(mat_shadow)
    shade_game_smooth(dimple)
    return dimple


def create_highlight(mat_highlight):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, location=(-0.82, -0.46, 0.04))
    highlight = bpy.context.object
    highlight.name = "GameApple_PaintedHighlight"
    highlight.scale = (0.055, 0.018, 0.22)
    highlight.rotation_euler = (math.radians(5), math.radians(0), math.radians(-8))
    highlight.data.materials.append(mat_highlight)
    shade_game_smooth(highlight)
    return highlight


def look_at(obj, target):
    direction = Vector(target) - obj.location
    obj.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()


def setup_lighting_and_camera():
    preview_objects = []
    world = bpy.context.scene.world or bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.color = (0.02, 0.12, 0.16)

    bpy.ops.object.light_add(type="AREA", location=(-3.0, -4.0, 4.0))
    key = bpy.context.object
    key.name = "Preview_KeyLight"
    key.data.energy = 500
    key.data.size = 4.5
    preview_objects.append(key)

    bpy.ops.object.light_add(type="POINT", location=(2.8, 1.6, 2.3))
    fill = bpy.context.object
    fill.name = "Preview_FillLight"
    fill.data.energy = 55
    preview_objects.append(fill)

    bpy.ops.object.camera_add(location=(3.4, -5.8, 2.25))
    cam = bpy.context.object
    look_at(cam, (0.0, 0.0, 0.25))
    cam.data.lens = 45
    bpy.context.scene.camera = cam
    preview_objects.append(cam)

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 600
    scene.display.shading.light = "STUDIO"
    scene.display.shading.studio_light = "studio.sl"
    scene.display.shading.color_type = "TEXTURE"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = 0
    scene.view_settings.gamma = 1
    return preview_objects


def export_files(model_objects):
    bpy.context.preferences.filepaths.save_version = 0
    for obj in bpy.context.scene.objects:
        obj.select_set(False)
    for obj in model_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = model_objects[0]

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=True,
        apply_unit_scale=True,
        bake_space_transform=False,
        object_types={"MESH", "OTHER"},
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        path_mode="AUTO",
    )


def render_preview():
    preview_objects = setup_lighting_and_camera()

    bpy.context.scene.render.filepath = PREVIEW_PATH
    bpy.ops.render.render(write_still=True)
    bpy.context.scene.camera = None
    for obj in preview_objects:
        if obj.name in bpy.data.objects:
            bpy.data.objects.remove(obj, do_unlink=True)


def main():
    clear_scene()

    body_texture = create_body_texture()
    body_mat = make_texture_mat("Apple_Painted_BaseColor", body_texture)
    stem = make_mat("Stem_Wood", (0.48, 0.23, 0.09, 1), 0.7)
    stem_dark = make_mat("Stem_Dark_Groove", (0.22, 0.10, 0.035, 1), 0.82)
    leaf = make_mat("Leaf_Green", (0.20, 0.66, 0.12, 1), 0.58)
    vein = make_mat("Leaf_Darker_Vein", (0.08, 0.35, 0.055, 1), 0.62)

    body = create_apple_body(body_mat)
    stem_obj = create_stem(stem, stem_dark)
    leaf_obj = create_leaf(leaf, vein)

    model_objects = [
        body,
        stem_obj,
        leaf_obj,
        *[obj for obj in bpy.context.scene.objects if obj.name.startswith("GameApple_StemGroove")],
        *[obj for obj in bpy.context.scene.objects if obj.name.startswith("GameApple_LeafVein")],
    ]

    export_files(model_objects)
    render_preview()
    print(f"Created {BLEND_PATH}")
    print(f"Created {FBX_PATH}")
    print(f"Created {PREVIEW_PATH}")


if __name__ == "__main__":
    main()

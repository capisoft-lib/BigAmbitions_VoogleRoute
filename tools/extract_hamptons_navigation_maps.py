#!/usr/bin/env python3
"""Extract compact Hamptons house navigation metadata from an installed game.

The house prefabs contain the plot bounds, stairs, and four boundary NavMesh
elements. The Hamptons scene contains the baked agent-type-0 NavMeshData. This
script joins both sources without exporting copyrighted meshes or textures.
"""

from __future__ import annotations

import argparse
import gc
import json
import math
import re
from pathlib import Path
from typing import Any, Iterable

import UnityPy


DEFAULT_GAME_DATA = Path(
    r"C:\Program Files (x86)\Steam\steamapps\common\Big Ambitions\Big Ambitions_Data"
)
HOUSE_BUNDLE_PATTERN = re.compile(r"buildingstructuret(\d+)\.prefab_.*\.bundle$", re.I)
HOUSE_SCENE_PATTERN = re.compile(r"BuildingStructureT(\d+)CBC$")
NAV_DATA_PATTERN = re.compile(r"NavMesh-BuildingStructureT(\d+)CBC$")


def vector3(value: Any) -> list[float]:
    return [round(float(value.x), 6), round(float(value.y), 6), round(float(value.z), 6)]


def quaternion(value: Any) -> list[float]:
    return [
        round(float(value.x), 7),
        round(float(value.y), 7),
        round(float(value.z), 7),
        round(float(value.w), 7),
    ]


def multiply_quaternion(a: Any, b: Any) -> tuple[float, float, float, float]:
    ax, ay, az, aw = float(a[0]), float(a[1]), float(a[2]), float(a[3])
    bx, by, bz, bw = float(b[0]), float(b[1]), float(b[2]), float(b[3])
    return (
        aw * bx + ax * bw + ay * bz - az * by,
        aw * by - ax * bz + ay * bw + az * bx,
        aw * bz + ax * by - ay * bx + az * bw,
        aw * bw - ax * bx - ay * by - az * bz,
    )


def rotate_vector(q: Any, v: Any) -> tuple[float, float, float]:
    qx, qy, qz, qw = float(q[0]), float(q[1]), float(q[2]), float(q[3])
    vx, vy, vz = float(v[0]), float(v[1]), float(v[2])
    tx = 2.0 * (qy * vz - qz * vy)
    ty = 2.0 * (qz * vx - qx * vz)
    tz = 2.0 * (qx * vy - qy * vx)
    return (
        vx + qw * tx + (qy * tz - qz * ty),
        vy + qw * ty + (qz * tx - qx * tz),
        vz + qw * tz + (qx * ty - qy * tx),
    )


def read_ptr(pointer: Any) -> Any | None:
    try:
        return pointer.read()
    except Exception:
        return None


def transform_for(game_object: Any) -> Any | None:
    for component in game_object.m_Component:
        value = read_ptr(component.component)
        if value is not None and value.__class__.__name__ in ("Transform", "RectTransform"):
            return value
    return None


def game_object_for_transform(transform: Any) -> Any | None:
    return read_ptr(transform.m_GameObject)


def iter_hierarchy(root: Any) -> Iterable[tuple[str, Any, Any]]:
    def walk(game_object: Any, parent_path: str) -> Iterable[tuple[str, Any, Any]]:
        transform = transform_for(game_object)
        path = f"{parent_path}/{game_object.m_Name}" if parent_path else game_object.m_Name
        if transform is None:
            return
        yield path, game_object, transform
        for child_pointer in transform.m_Children:
            child_transform = read_ptr(child_pointer)
            child_object = game_object_for_transform(child_transform) if child_transform else None
            if child_object is not None:
                yield from walk(child_object, path)

    yield from walk(root, "")


def relative_pose(transform: Any) -> dict[str, list[float]]:
    return {
        "position": vector3(transform.m_LocalPosition),
        "rotation": quaternion(transform.m_LocalRotation),
        "scale": vector3(transform.m_LocalScale),
    }


def world_pose(transform: Any) -> dict[str, list[float]]:
    chain = []
    current = transform
    while current is not None:
        chain.append(current)
        father = current.m_Father
        current = read_ptr(father) if father is not None and father.path_id else None

    position = (0.0, 0.0, 0.0)
    rotation = (0.0, 0.0, 0.0, 1.0)
    scale = (1.0, 1.0, 1.0)
    for item in reversed(chain):
        local_position = vector3(item.m_LocalPosition)
        scaled = tuple(local_position[index] * scale[index] for index in range(3))
        rotated = rotate_vector(rotation, scaled)
        position = tuple(position[index] + rotated[index] for index in range(3))
        rotation = multiply_quaternion(rotation, quaternion(item.m_LocalRotation))
        local_scale = vector3(item.m_LocalScale)
        scale = tuple(scale[index] * local_scale[index] for index in range(3))

    return {
        "position": [round(value, 6) for value in position],
        "rotation": [round(value, 7) for value in rotation],
        "scale": [round(value, 6) for value in scale],
    }


def script_name(mono_behaviour: Any) -> str:
    script = read_ptr(mono_behaviour.m_Script)
    return str(getattr(script, "m_ClassName", "")) if script is not None else ""


def mono_behaviours(game_object: Any) -> Iterable[Any]:
    for component in game_object.m_Component:
        value = read_ptr(component.component)
        if value is not None and value.__class__.__name__ == "MonoBehaviour":
            yield value


def resolve_local_object(environment: Any, path_id: int) -> Any | None:
    for value in environment.objects:
        if value.path_id == path_id:
            return value
    return None


def extract_prefab(bundle_path: Path) -> dict[str, Any]:
    environment = UnityPy.load(str(bundle_path))
    root_object = next(iter(environment.container.values())).read()
    number = int(HOUSE_BUNDLE_PATTERN.match(bundle_path.name).group(1))

    result: dict[str, Any] = {
        "id": f"T{number}",
        "prefab": root_object.m_Name,
        "bundle": bundle_path.name,
        "plot_bounds": None,
        "navmesh_surface": None,
        "boundary_elements": [],
        "stairs": [],
        "outside_doors": [],
        "floors": [],
    }

    hamptons_tree = None
    for behaviour in mono_behaviours(root_object):
        name = script_name(behaviour)
        tree = behaviour.object_reader.read_typetree()
        if name == "HamptonsHouse":
            hamptons_tree = tree
        elif name == "NavMeshSurface":
            result["navmesh_surface"] = {
                "agent_type_id": int(tree["m_AgentTypeID"]),
                "collect_objects": int(tree["m_CollectObjects"]),
                "layer_mask": int(tree["m_LayerMask"]["m_Bits"]),
                "voxel_size": round(float(tree["m_VoxelSize"]), 6),
                "min_region_area": round(float(tree["m_MinRegionArea"]), 6),
            }

    if hamptons_tree is not None:
        pointer = hamptons_tree.get("plotBounds", {})
        path_id = int(pointer.get("m_PathID", 0))
        raw_object = resolve_local_object(environment, path_id)
        if raw_object is not None:
            bounds_component = raw_object.read()
            bounds_tree = raw_object.read_typetree()
            bounds_object = read_ptr(bounds_component.m_GameObject)
            bounds_transform = transform_for(bounds_object) if bounds_object is not None else None
            result["plot_bounds"] = {
                "size": [round(float(bounds_tree["size"][axis]), 6) for axis in ("x", "y", "z")],
                "pose": relative_pose(bounds_transform) if bounds_transform is not None else None,
            }

    for path, game_object, transform in iter_hierarchy(root_object):
        leaf = game_object.m_Name
        if path.startswith(f"{root_object.m_Name}/NavMeshElements/NavMeshElement"):
            result["boundary_elements"].append({"name": leaf, "pose": relative_pose(transform)})
        if "Stairs" in leaf and "/FirstFloor/" in path:
            result["stairs"].append({"name": leaf, "path": path, "pose": relative_pose(transform)})
        if leaf.startswith("OutsideDoor"):
            result["outside_doors"].append({"name": leaf, "path": path, "pose": relative_pose(transform)})
        if path in (
            f"{root_object.m_Name}/FirstFloor",
            f"{root_object.m_Name}/SecondFloor",
            f"{root_object.m_Name}/ThirdFloor",
        ):
            result["floors"].append({"name": leaf, "pose": relative_pose(transform)})

    del environment
    gc.collect()
    return result


def extract_scene(game_data: Path) -> tuple[dict[int, Any], dict[int, Any]]:
    environment = UnityPy.load(str(game_data / "level16"), str(game_data / "sharedassets16.assets"))
    roots: dict[int, Any] = {}
    nav_data: dict[int, Any] = {}

    for value in environment.objects:
        if value.type.name == "GameObject" and value.assets_file.name == "level16":
            try:
                game_object = value.read()
            except Exception:
                continue
            match = HOUSE_SCENE_PATTERN.fullmatch(game_object.m_Name)
            if match:
                transform = transform_for(game_object)
                if transform is not None:
                    roots[int(match.group(1))] = {
                        "name": game_object.m_Name,
                        "pose": world_pose(transform),
                    }
        elif value.type.name == "NavMeshData":
            data = value.read()
            match = NAV_DATA_PATTERN.fullmatch(str(data.m_Name))
            if not match:
                continue
            settings = data.m_NavMeshBuildSettings
            nav_data[int(match.group(1))] = {
                "name": str(data.m_Name),
                "serialized_position": vector3(data.m_Position),
                "serialized_rotation": quaternion(data.m_Rotation),
                "source_bounds": {
                    "center": vector3(data.m_SourceBounds.m_Center),
                    "extents": vector3(data.m_SourceBounds.m_Extent),
                },
                "agent_type_id": int(settings.agentTypeID),
                "agent_height": round(float(settings.agentHeight), 6),
                "agent_radius": round(float(settings.agentRadius), 6),
                "agent_climb": round(float(settings.agentClimb), 6),
                "agent_slope": round(float(settings.agentSlope), 6),
            }

    del environment
    gc.collect()
    return roots, nav_data


def build_document(game_data: Path) -> dict[str, Any]:
    bundle_root = game_data / "StreamingAssets" / "aa" / "StandaloneWindows64"
    bundle_paths = []
    for candidate in bundle_root.rglob("*.bundle"):
        match = HOUSE_BUNDLE_PATTERN.fullmatch(candidate.name)
        if match:
            bundle_paths.append((int(match.group(1)), candidate))
    bundle_paths.sort()

    if len(bundle_paths) != 16:
        raise RuntimeError(f"Expected 16 Hamptons house bundles, found {len(bundle_paths)}")

    scene_roots, scene_nav_data = extract_scene(game_data)
    houses = []
    for number, bundle_path in bundle_paths:
        house = extract_prefab(bundle_path)
        house["scene_root"] = scene_roots.get(number)
        house["scene_navmesh"] = scene_nav_data.get(number)
        houses.append(house)

    return {
        "schema_version": 1,
        "source": "Big Ambitions Beta 1.0 installed assets",
        "scene": "Assets/Scenes/SubScenes/TheHamptons/TheHamptons.unity",
        "house_count": len(houses),
        "houses": houses,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--game-data", type=Path, default=DEFAULT_GAME_DATA)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    document = build_document(args.game_data.resolve())
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
    print(f"Extracted {document['house_count']} Hamptons navigation maps to {args.output}")


if __name__ == "__main__":
    main()

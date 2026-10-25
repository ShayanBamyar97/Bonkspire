"""Builds the Core library and copies it (plus the catalog) into the Unity project.

  python -X utf8 Core/tools/sync_to_unity.py          # regenerates catalog.json from SPEC.md first
  python -X utf8 Core/tools/sync_to_unity.py --no-export

Run it after ANY change in Core/ or in the SPEC tables. Unity then re-imports the DLL automatically.
"""
import os, shutil, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
CORE = os.path.abspath(os.path.join(HERE, ".."))
ROOT = os.path.abspath(os.path.join(CORE, ".."))
UNITY = os.path.join(ROOT, "UnityClient", "Assets")


def run(cmd):
    print("$", " ".join(cmd))
    subprocess.run(cmd, check=True)


if "--no-export" not in sys.argv:
    run([sys.executable, "-X", "utf8", os.path.join(HERE, "export_catalog.py")])

proj = os.path.join(CORE, "TowerOfChaos.GameCore", "TowerOfChaos.GameCore.csproj")
run(["dotnet", "build", proj, "-c", "Release", "-nologo", "-v", "q"])

dll = os.path.join(CORE, "TowerOfChaos.GameCore", "bin", "Release", "netstandard2.1", "TowerOfChaos.GameCore.dll")
dst_dll = os.path.join(UNITY, "Plugins", "TowerOfChaos")
dst_cat = os.path.join(UNITY, "Resources")
os.makedirs(dst_dll, exist_ok=True); os.makedirs(dst_cat, exist_ok=True)
shutil.copy2(dll, dst_dll)
shutil.copy2(os.path.join(CORE, "Data", "catalog.json"), os.path.join(dst_cat, "catalog.json"))
print("copied:", os.path.join(dst_dll, "TowerOfChaos.GameCore.dll"), os.path.getsize(os.path.join(dst_dll, "TowerOfChaos.GameCore.dll")), "bytes")
print("copied:", os.path.join(dst_cat, "catalog.json"))

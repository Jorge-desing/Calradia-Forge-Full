import os
import shutil
import subprocess
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

def get_version(submodule_path):
    try:
        tree = ET.parse(submodule_path)
        root = tree.getroot()
        version_node = root.find("Version")
        if version_node is not None:
            return version_node.attrib.get("value", "1.0.0").replace("v", "")
    except:
        pass
    return "3.0.0"

def build_and_package():
    print("🚀 Starting Calradia Forge Release Build...")
    root_dir = Path(__file__).resolve().parent.parent
    sln_path = root_dir / "CalradiaForge.sln"
    module_dir = root_dir / "modules" / "CalradiaForge"
    dist_dir = root_dir / "dist"
    
    print("🧹 Cleaning previous builds...")
    subprocess.run(["dotnet", "clean", str(sln_path), "-c", "Release"], check=True)
    if dist_dir.exists():
        shutil.rmtree(dist_dir)
    dist_dir.mkdir(parents=True)
    
    print("🔨 Building solution in Release mode...")
    subprocess.run(["dotnet", "build", str(sln_path), "-c", "Release"], check=True)
    
    version = get_version(module_dir / "SubModule.xml")
    zip_name = f"CalradiaForge-v{version}.zip"
    zip_path = dist_dir / zip_name
    
    print(f"📦 Packaging module into {zip_name}...")
    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as zipf:
        for root, dirs, files in os.walk(module_dir):
            dirs[:] = [d for d in dirs if d not in (".git", ".vs", "obj")]
            for file in files:
                if file.endswith((".bat", ".ps1", ".pdb")):
                    continue
                file_path = Path(root) / file
                rel_path = file_path.relative_to(module_dir.parent.parent)
                zipf.write(file_path, rel_path)
                
    print(f"✅ Success! Release package created at: {zip_path}")

if __name__ == "__main__":
    build_and_package()


import os
import shutil
from pathlib import Path

def deploy():
    print("🚀 Deploying Calradia Forge to Bannerlord...")
    root_dir = Path(__file__).resolve().parent.parent
    source_dir = root_dir / "modules" / "CalradiaForge"
    
    # Common Steam installation paths
    game_dir = Path(os.environ.get("PROGRAMFILES(X86)", "C:\\Program Files (x86)")) / "Steam" / "steamapps" / "common" / "Mount & Blade II Bannerlord"
    target_dir = game_dir / "Modules" / "CalradiaForge"
    
    if not game_dir.exists():
        print(f"❌ Error: Could not find Bannerlord directory at {game_dir}")
        print("Please edit the script to point to your custom game installation path.")
        return
        
    print(f"📁 Copying files to: {target_dir}")
    if target_dir.exists():
        shutil.rmtree(target_dir)
        
    shutil.copytree(source_dir, target_dir, ignore=shutil.ignore_patterns(".git", "*.pdb", "obj"))
    print("✅ Deployment complete! You can now launch Bannerlord.")

if __name__ == "__main__":
    deploy()


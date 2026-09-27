import os
import subprocess
from pathlib import Path

def launch():
    print("🚀 Launching Bannerlord with Calradia Forge...")
    game_dir = Path(os.environ.get("PROGRAMFILES(X86)", "C:\\Program Files (x86)")) / "Steam" / "steamapps" / "common" / "Mount & Blade II Bannerlord"
    exe_path = game_dir / "bin" / "Win64_Shipping_Client" / "Bannerlord.exe"
    
    if not exe_path.exists():
        print(f"❌ Error: Could not find Bannerlord.exe at {exe_path}")
        return
        
    # The /singleplayer flag bypasses the launcher
    # The _MODULES_... flags force load the specific mod list
    args = [
        str(exe_path),
        "/singleplayer",
        "_MODULES_*Native*SandBoxCore*CustomBattle*SandBox*StoryMode*CalradiaForge*_MODULES_"
    ]
    
    print(f"▶ Executing: {' '.join(args)}")
    subprocess.Popen(args, cwd=str(exe_path.parent))

if __name__ == "__main__":
    launch()

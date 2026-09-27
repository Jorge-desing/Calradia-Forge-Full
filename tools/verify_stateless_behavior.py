#!/usr/bin/env python3
"""
Stateless CampaignBehavior & Architecture Acceptance Verifier
Validates compilation, statelessness, anti-shadowing, and SubModule registration.
"""
import os
import re
import subprocess
import sys

def check_compilation(workspace):
    print("[1/4] Verifying CalradiaForge.sln compiles in Release mode...")
    cmd = ["dotnet", "build", "CalradiaForge.sln", "-c", "Release", "-v:minimal"]
    try:
        res = subprocess.run(cmd, cwd=workspace, capture_output=True, text=True)
        if res.returncode != 0:
            print(f"FAILED: Compilation error:\n{res.stdout}\n{res.stderr}")
            return False
        print("  [OK] CalradiaForge.sln compiled successfully with 0 errors.")
        return True
    except Exception as ex:
        print(f"FAILED: Exception while executing build: {ex}")
        return False

def check_statelessness(mod_dir):
    print("[2/4] Verifying statelessness: 0 SaveableTypeDefiner & 0 SyncData fields...")
    # 1. No classes inherit from SaveableTypeDefiner across src/CalradiaForge.Mod
    violations = []
    for root, _, files in os.walk(mod_dir):
        for f in files:
            if f.endswith(".cs"):
                p = os.path.join(root, f)
                with open(p, "r", encoding="utf-8", errors="ignore") as src:
                    for line_no, line in enumerate(src, 1):
                        if re.search(r":\s*SaveableTypeDefiner\b", line):
                            violations.append(f"{p}:{line_no} inherits from SaveableTypeDefiner")
    if violations:
        print("FAILED: Found SaveableTypeDefiner inheritance:\n" + "\n".join(violations))
        return False

    # 2. Check SyncData in CampaignBehaviors (excluding legacy DataBehavior.cs)
    cb_dir = os.path.join(mod_dir, "CampaignBehaviors")
    if not os.path.exists(cb_dir):
        print(f"FAILED: CampaignBehaviors directory not found at {cb_dir}")
        return False

    behavior_files = [f for f in os.listdir(cb_dir) if f.endswith(".cs") and f != "DataBehavior.cs"]
    if not behavior_files:
        print(f"FAILED: No behavior files found in {cb_dir} (excluding DataBehavior.cs)")
        return False

    for f in behavior_files:
        p = os.path.join(cb_dir, f)
        with open(p, "r", encoding="utf-8", errors="ignore") as src:
            content = src.read()

        # Find SyncData method
        match = re.search(r"public\s+override\s+void\s+SyncData\s*\(\s*IDataStore\s+(\w+)\s*\)\s*\{([\s\S]*?)\}", content)
        if not match:
            print(f"FAILED: Could not find SyncData(IDataStore) override in {p}")
            return False

        param_name = match.group(1)
        body = match.group(2)

        # Check for dataStore.SyncData calls inside method body
        if re.search(rf"\b{param_name}\s*\.\s*SyncData\b", body):
            print(f"FAILED: SyncData in {p} calls dataStore.SyncData:\n{body.strip()}")
            return False

        # Check for [SaveableField] or [SaveableProperty] decorations
        if re.search(r"\[\s*Saveable(Field|Property)", content):
            print(f"FAILED: Found [SaveableField] or [SaveableProperty] in {p}")
            return False

    print(f"  [OK] Zero SaveableTypeDefiner and zero SyncData serialization confirmed across {len(behavior_files)} behavior file(s).")
    return True

def check_anti_shadowing(mod_dir):
    print("[3/4] Verifying GEMINI.md Anti-Shadowing constraints...")
    # 1. Check forbidden folder names
    for root, dirs, _ in os.walk(mod_dir):
        for d in dirs:
            if d.lower() == "campaign":
                print(f"FAILED: Forbidden folder named '{d}' found at {os.path.join(root, d)}")
                return False

    # 2. Check forbidden namespaces and class names in all C# source files
    for root, _, files in os.walk(mod_dir):
        for f in files:
            if f.endswith(".cs"):
                p = os.path.join(root, f)
                with open(p, "r", encoding="utf-8", errors="ignore") as src:
                    for line_no, line in enumerate(src, 1):
                        # Match "namespace ...Campaign" or "namespace Campaign"
                        if re.search(r"\bnamespace\s+([A-Za-z0-9_\.]*\.)?Campaign\b(\s*;|\s*\{|$)", line):
                            print(f"FAILED: Forbidden namespace ending in 'Campaign' at {p}:{line_no}")
                            return False
                        # Match "class Campaign"
                        if re.search(r"\bclass\s+Campaign\b", line):
                            print(f"FAILED: Forbidden class named 'Campaign' at {p}:{line_no}")
                            return False

    print("  [OK] Anti-shadowing verified: zero folders, namespaces, or classes named 'Campaign'.")
    return True

def check_submodule_registration(mod_dir):
    print("[4/4] Verifying MBSubModuleBase.OnGameStart registration...")
    submodule_path = os.path.join(mod_dir, "SubModule.cs")
    if not os.path.exists(submodule_path):
        print(f"FAILED: SubModule.cs not found at {submodule_path}")
        return False

    with open(submodule_path, "r", encoding="utf-8", errors="ignore") as f:
        content = f.read()

    # Match OnGameStart method
    match = re.search(r"protected\s+override\s+void\s+OnGameStart\s*\([^)]*\)\s*\{([\s\S]*?)\n\s*\}", content)
    if not match:
        print("FAILED: OnGameStart method not found in SubModule.cs")
        return False

    body = match.group(1)
    # Check for campaignStarter.AddBehavior(new ...Behavior())
    if not re.search(r"campaignStarter\.AddBehavior\s*\(\s*new\s+(?:[A-Za-z0-9_\.]+\.)?ClanCharacterProgressionBehavior\s*\(\s*\)\s*\);", body) and \
       not re.search(r"campaignStarter\.AddBehavior\s*\(\s*new\s+[\w\.]*Behavior\s*\(\s*\)\s*\);", body):
        print(f"FAILED: campaignStarter.AddBehavior(...) not found in SubModule.OnGameStart:\n{body}")
        return False

    print("  [OK] SubModule properly registers CampaignBehavior in OnGameStart via AddBehavior().")
    return True

def main():
    if len(sys.argv) > 1 and os.path.isdir(sys.argv[1]):
        workspace = os.path.abspath(sys.argv[1])
    else:
        workspace = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))

    print(f"Running Stateless CampaignBehavior Acceptance Verification against: {workspace}")
    mod_dir = os.path.join(workspace, "src", "CalradiaForge.Mod")
    if not os.path.exists(mod_dir):
        print(f"FAILED: CalradiaForge.Mod directory not found at {mod_dir}")
        sys.exit(1)

    success = (
        check_compilation(workspace) and
        check_statelessness(mod_dir) and
        check_anti_shadowing(mod_dir) and
        check_submodule_registration(mod_dir)
    )

    if success:
        print("\n=======================================================")
        print("SUCCESS: All architectural and acceptance criteria passed!")
        print("=======================================================")
        sys.exit(0)
    else:
        print("\n=======================================================")
        print("FAILURE: One or more acceptance criteria failed.")
        print("=======================================================")
        sys.exit(1)

if __name__ == "__main__":
    main()

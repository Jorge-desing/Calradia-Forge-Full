#!/usr/bin/env python3
"""Pack and smoke-test the isolated Calradia Forge .NET onboarding packages."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
import tarfile
import uuid
import zipfile
from datetime import datetime, timezone
from pathlib import Path
from typing import Any
from xml.etree import ElementTree


SDK_PROJECT = Path("src/CalradiaForge.Sdk/CalradiaForge.Sdk.csproj")
SDK_OVERLAYS = (
    Path("src/CalradiaForge.Sdk/ForgeTroopBuilder.cs"),
    Path("src/CalradiaForge.Sdk/ForgeItemBuilder.cs"),
    Path("src/CalradiaForge.Sdk/ForgeNoviceHub.cs"),
)
TEMPLATE_PROJECT = Path("templates/CalradiaForge.Mod.Template/CalradiaForge.Mod.Template.csproj")
TEMPLATE_SOURCE_FILES = (
    Path("CalradiaForge.Mod.Template.csproj"),
    Path("README.md"),
)
SDK_PACKAGE_README = Path("src/CalradiaForge.Sdk/README.md")
SDK_PACKAGE_ID = "CalradiaForge.Sdk"
TEMPLATE_PACKAGE_ID = "CalradiaForge.Mod.Template"


class SmokeFailure(RuntimeError):
    pass


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def run_logged(
    name: str,
    command: list[str],
    cwd: Path,
    env: dict[str, str],
    log_dir: Path,
    manifest: list[dict[str, Any]],
) -> subprocess.CompletedProcess[str]:
    log_path = log_dir / f"{name}.log"
    log_path.parent.mkdir(parents=True, exist_ok=True)
    result = subprocess.run(
        command,
        cwd=cwd,
        env=env,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )
    log_path.write_text(result.stdout, encoding="utf-8")
    manifest.append(
        {
            "name": name,
            "command": command,
            "cwd": str(cwd),
            "exitCode": result.returncode,
            "log": str(log_path),
        }
    )
    print(f"[{name}] exit={result.returncode}; log={log_path}")
    if result.stdout:
        lines = result.stdout.rstrip().splitlines()
        print("\n".join(lines[-18:]))
    if result.returncode != 0:
        raise SmokeFailure(f"Command '{name}' failed ({result.returncode}); see {log_path}")
    return result


def make_run_dir(repo: Path, requested_output: Path | None) -> Path:
    artifacts_root = (repo / "artifacts" / "sdk-evolution" / "onboarding").resolve()
    stamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    leaf = requested_output.resolve() if requested_output else artifacts_root / f"{stamp}-{uuid.uuid4().hex[:8]}"
    try:
        leaf.relative_to(artifacts_root)
    except ValueError as exc:
        raise SmokeFailure(f"Output must stay under ignored artifacts: {artifacts_root}") from exc
    leaf.mkdir(parents=True, exist_ok=False)
    return leaf


def git_text(repo: Path, *args: str) -> str:
    result = subprocess.run(
        ["git", *args], cwd=repo, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False
    )
    if result.returncode:
        raise SmokeFailure(f"git {' '.join(args)} failed: {result.stderr.strip()}")
    return result.stdout.strip()


def read_product_version(directory_build_props: Path) -> str:
    props = ElementTree.parse(directory_build_props).getroot()
    value = props.findtext("./PropertyGroup/CalradiaForgeVersion")
    if not value or not re.fullmatch(r"\d+\.\d+\.\d+", value.strip()):
        raise SmokeFailure(f"Directory.Build.props has no valid CalradiaForgeVersion: {directory_build_props}")
    return value.strip()


def stable_source_snapshot(repo: Path, snapshot: Path, report: dict[str, Any]) -> tuple[int, str, list[str]]:
    snapshot.mkdir(parents=True, exist_ok=False)
    head = git_text(repo, "rev-parse", "HEAD")
    archive_file = snapshot / "head-source.tar"
    with archive_file.open("wb") as stream:
        archive = subprocess.run(
            [
                "git",
                "archive",
                "--format=tar",
                "HEAD",
                "Directory.Build.props",
                "src/CalradiaForge.Sdk",
            ],
            cwd=repo,
            stdout=stream,
            stderr=subprocess.PIPE,
            check=False,
        )
    if archive.returncode:
        raise SmokeFailure(f"git archive failed: {archive.stderr.decode(errors='replace').strip()}")
    with tarfile.open(archive_file, "r:") as tar:
        root = snapshot.resolve()
        for member in tar.getmembers():
            target = (snapshot / member.name).resolve()
            try:
                target.relative_to(root)
            except ValueError as exc:
                raise SmokeFailure(f"Unsafe path in source archive: {member.name}") from exc
        tar.extractall(snapshot, filter="data")
    archive_file.unlink()

    version_file = snapshot / "src/CalradiaForge.Sdk/Contracts.cs"
    contracts = version_file.read_text(encoding="utf-8")
    versions = re.findall(r"public\s+const\s+int\s+Version\s*=\s*(\d+)\s*;", contracts)
    if not versions or int(versions[0]) < 1:
        raise SmokeFailure("Archived stable SDK does not expose a readable positive ForgeApi.Version.")
    stable_api_version = int(versions[0])
    product_version = read_product_version(snapshot / "Directory.Build.props")
    archived_sdk_project = ElementTree.parse(snapshot / SDK_PROJECT).getroot()
    frameworks_text = archived_sdk_project.findtext("./PropertyGroup/TargetFrameworks")
    frameworks = [part.strip() for part in (frameworks_text or "").split(";") if part.strip()]
    if set(frameworks) != {"net472", "net8.0"}:
        raise SmokeFailure(f"Archived SDK must keep the game and portable TFMs; found {frameworks}.")

    overlays: list[dict[str, Any]] = []
    for relative in (SDK_PROJECT, *SDK_OVERLAYS):
        source = repo / relative
        target = snapshot / relative
        if not source.is_file():
            raise SmokeFailure(f"Approved snapshot overlay is missing: {source}")
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
        overlays.append(
            {
                "path": relative.as_posix(),
                "headBlob": git_text(repo, "rev-parse", f"HEAD:{relative.as_posix()}"),
                "overlaySha256": sha256(source),
            }
        )

    package_readme = repo / SDK_PACKAGE_README
    if not package_readme.is_file():
        raise SmokeFailure(f"SDK package readme is missing: {package_readme}")
    shutil.copy2(package_readme, snapshot / SDK_PACKAGE_README)
    package_assets = [{"path": SDK_PACKAGE_README.as_posix(), "sha256": sha256(package_readme)}]

    working_contract_path = repo / "src/CalradiaForge.Sdk/Contracts.cs"
    current = working_contract_path.read_text(encoding="utf-8")
    current_versions = re.findall(r"public\s+const\s+int\s+Version\s*=\s*(\d+)\s*;", current)
    changed_sdk_paths = {
        path.strip().replace("\\", "/")
        for path in git_text(repo, "diff", "--name-only", "HEAD", "--", "src/CalradiaForge.Sdk").splitlines()
        if path.strip()
    }
    changed_sdk_paths.update(
        path.strip().replace("\\", "/")
        for path in git_text(
            repo, "ls-files", "--others", "--exclude-standard", "--", "src/CalradiaForge.Sdk"
        ).splitlines()
        if path.strip()
    )
    overlay_paths = {relative.as_posix() for relative in (SDK_PROJECT, *SDK_OVERLAYS)}
    included_sdk_paths = overlay_paths | {SDK_PACKAGE_README.as_posix()}
    report.update(
        {
            "repositoryHead": head,
            "stableContractVersion": stable_api_version,
            "workingTreeContractVersion": int(current_versions[0]) if current_versions else None,
            "workingTreeApiChangesIncluded": False,
            "includedSdkWorkspaceFiles": sorted(included_sdk_paths),
            "excludedSdkWorkspaceChanges": sorted(path for path in changed_sdk_paths if path not in included_sdk_paths),
            "stableSdkTargetFrameworks": frameworks,
            "productVersion": product_version,
            "sourceComposition": (
                "HEAD SDK contract plus explicit current csproj/three builder file overlays; "
                "current SDK README copied as a package asset"
            ),
            "overlays": overlays,
            "packageAssets": package_assets,
        }
    )
    return stable_api_version, product_version, frameworks


def isolated_template_source(repo: Path, snapshot: Path, report: dict[str, Any]) -> Path:
    """Copy only template pack inputs to the ignored run snapshot, never build in the shared checkout."""
    source = repo / TEMPLATE_PROJECT.parent
    destination = snapshot / "template-input" / TEMPLATE_PROJECT.parent
    destination.mkdir(parents=True, exist_ok=False)
    copied: list[dict[str, str]] = []
    for relative in TEMPLATE_SOURCE_FILES:
        source_file = source / relative
        if not source_file.is_file():
            raise SmokeFailure(f"Template pack input is missing: {source_file}")
        target = destination / relative
        shutil.copy2(source_file, target)
        copied.append({"path": (TEMPLATE_PROJECT.parent / relative).as_posix(), "sha256": sha256(source_file)})

    content_source = source / "content"
    if not content_source.is_dir():
        raise SmokeFailure(f"Template content directory is missing: {content_source}")
    shutil.copytree(content_source, destination / "content")
    for path in sorted((destination / "content").rglob("*")):
        if path.is_file():
            relative = path.relative_to(destination).as_posix()
            copied.append({"path": f"{TEMPLATE_PROJECT.parent.as_posix()}/{relative}", "sha256": sha256(path)})

    snapshot_props = snapshot / "Directory.Build.props"
    if not snapshot_props.is_file():
        raise SmokeFailure(f"The archived Directory.Build.props is missing: {snapshot_props}")
    report["templateSource"] = {
        "composition": "current template pack inputs copied into the ignored per-run snapshot; bin/obj omitted",
        "project": (TEMPLATE_PROJECT.parent / TEMPLATE_PROJECT.name).as_posix(),
        "files": copied,
    }
    return destination / TEMPLATE_PROJECT.name


def package_environment(run_dir: Path) -> dict[str, str]:
    env = os.environ.copy()
    cli_home = run_dir / "dotnet-home"
    packages = run_dir / "nuget-packages"
    for path in (cli_home, packages):
        path.mkdir(parents=True, exist_ok=True)
    env["DOTNET_CLI_HOME"] = str(cli_home)
    env["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1"
    env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
    env["NUGET_PACKAGES"] = str(packages)
    return env


def build_packages(repo: Path, run_dir: Path, commands: list[dict[str, Any]]) -> tuple[Path, Path, dict[str, Any], str]:
    feed = run_dir / "feed"
    snapshot = run_dir / "stable-sdk-source"
    feed.mkdir(parents=True, exist_ok=False)
    report: dict[str, Any] = {
        "schemaVersion": 1,
        "createdUtc": datetime.now(timezone.utc).isoformat(),
        "packages": [],
    }
    _, product_version, frameworks = stable_source_snapshot(repo, snapshot, report)
    isolated_template_project = isolated_template_source(repo, snapshot, report)
    env = package_environment(run_dir)

    run_logged(
        "pack-sdk",
        [
            "dotnet",
            "pack",
            str(snapshot / SDK_PROJECT),
            "--configuration",
            "Release",
            "--output",
            str(feed),
            "--nologo",
            f"-p:PackageVersion={product_version}",
        ],
        cwd=snapshot,
        env=env,
        log_dir=run_dir / "logs",
        manifest=commands,
    )
    run_logged(
        "pack-template",
        [
            "dotnet",
            "pack",
            str(isolated_template_project),
            "--configuration",
            "Release",
            "--output",
            str(feed),
            "--nologo",
            f"-p:PackageVersion={product_version}",
        ],
        cwd=isolated_template_project.parent,
        env=env,
        log_dir=run_dir / "logs",
        manifest=commands,
    )

    sdk_package = feed / f"{SDK_PACKAGE_ID}.{product_version}.nupkg"
    template_package = feed / f"{TEMPLATE_PACKAGE_ID}.{product_version}.nupkg"
    for package in (sdk_package, template_package):
        if not package.is_file():
            raise SmokeFailure(f"Expected package was not produced: {package}")
    template_default_version = read_template_default_version(template_package)
    if template_default_version != product_version:
        raise SmokeFailure(
            f"Template SDK version default {template_default_version!r} does not match product/package version {product_version!r}."
        )
    report["packages"] = [
        {"id": SDK_PACKAGE_ID, "version": product_version, "path": str(sdk_package), "sha256": sha256(sdk_package)},
        {
            "id": TEMPLATE_PACKAGE_ID,
            "version": product_version,
            "path": str(template_package),
            "sha256": sha256(template_package),
        },
    ]
    validate_packages(sdk_package, template_package, frameworks)
    return feed, template_package, report, product_version


def read_template_default_version(template_package: Path) -> str:
    with zipfile.ZipFile(template_package) as archive:
        config = json.loads(archive.read("content/.template.config/template.json"))
    return str(config.get("symbols", {}).get("forgeSdkVersion", {}).get("defaultValue", ""))


def validate_packages(sdk_package: Path, template_package: Path, frameworks: list[str]) -> None:
    with zipfile.ZipFile(sdk_package) as archive:
        names = set(archive.namelist())
        if "README.md" not in names:
            raise SmokeFailure("SDK NuGet is missing its package readme.")
        for framework in frameworks:
            for suffix in ("dll", "xml"):
                required = f"lib/{framework}/CalradiaForge.Sdk.{suffix}"
                if required not in names:
                    raise SmokeFailure(f"SDK NuGet is missing asset {required}")
        forbidden = [name for name in names if Path(name).name.startswith("TaleWorlds.") and name.lower().endswith(".dll")]
        if forbidden:
            raise SmokeFailure(f"SDK NuGet unexpectedly contains proprietary engine binaries: {forbidden}")

    with zipfile.ZipFile(template_package) as archive:
        names = set(archive.namelist())
        expected = {
            "README.md",
            "content/.template.config/template.json",
            "content/CalradiaForge.ModTemplate/CalradiaForge.ModTemplate.csproj",
            "content/CalradiaForge.ModTemplate/SubModule.cs",
            "content/CalradiaForge.ModTemplate/SubModule.xml",
        }
        missing = expected - names
        if missing:
            raise SmokeFailure(f"Template NuGet is missing content: {sorted(missing)}")
        binaries = [name for name in names if name.lower().endswith((".dll", ".exe"))]
        if binaries:
            raise SmokeFailure(f"Template package must contain source only, found: {binaries}")
        config = json.loads(archive.read("content/.template.config/template.json"))
        if config.get("shortName") != "calradiaforge-mod":
            raise SmokeFailure("Template package identity/short name does not match the supported command.")


def ensure_inside(parent: Path, child: Path) -> None:
    try:
        child.resolve().relative_to(parent.resolve())
    except ValueError as exc:
        raise SmokeFailure(f"Generated path escaped the temporary smoke directory: {child}") from exc


def validate_generated_module(project_dir: Path, module_id: str, product_version: str) -> Path:
    csproj = project_dir / f"{module_id}.csproj"
    manifest = project_dir / "SubModule.xml"
    source = project_dir / "SubModule.cs"
    if not all(path.is_file() for path in (csproj, manifest, source)):
        raise SmokeFailure("Template generation did not produce the expected csproj, SubModule.cs, and SubModule.xml.")
    xml = ElementTree.parse(manifest).getroot()
    def xml_value(node: ElementTree.Element | None) -> str | None:
        return node.get("value") if node is not None else None

    actual_id = xml_value(xml.find("Id"))
    if actual_id != module_id:
        raise SmokeFailure(f"Generated manifest Id is {actual_id!r}, expected {module_id!r}.")
    if xml_value(xml.find("Name")) != module_id:
        raise SmokeFailure("Generated manifest display name does not follow the chosen module name.")
    dependencies = {item.attrib.get("Id") for item in xml.findall("./DependedModules/DependedModule")}
    if "CalradiaForge" not in dependencies:
        raise SmokeFailure("Generated manifest does not depend on CalradiaForge.")
    dll_name = xml_value(xml.find("./SubModules/SubModule/DLLName"))
    class_type = xml_value(xml.find("./SubModules/SubModule/SubModuleClassType"))
    if dll_name != f"{module_id}.dll" or class_type != f"{module_id}.SubModule":
        raise SmokeFailure(f"Generated manifest assembly/class metadata is inconsistent: {dll_name} / {class_type}")
    project_xml = ElementTree.parse(csproj).getroot()
    framework = project_xml.findtext("./PropertyGroup/TargetFramework")
    sdk_version = project_xml.findtext("./PropertyGroup/CalradiaForgeSdkVersion")
    sdk = project_xml.find("./ItemGroup/PackageReference[@Include='CalradiaForge.Sdk']")
    if (
        framework != "net472"
        or sdk is None
        or sdk.attrib.get("Version") != "$(CalradiaForgeSdkVersion)"
        or sdk_version != product_version
    ):
        raise SmokeFailure("Generated project must target net472 and reference CalradiaForge.Sdk at the product version.")
    if sdk.attrib.get("ExcludeAssets") != "runtime":
        raise SmokeFailure("Generated SDK reference must be compile-time-only to avoid shipping a duplicate SDK assembly.")
    if "$(GameBin)" not in "".join(csproj.read_text(encoding="utf-8").splitlines()):
        raise SmokeFailure("Generated project is missing local Bannerlord GameBin references.")
    generated_source = source.read_text(encoding="utf-8")
    if f'private const string ModuleId = "{module_id}";' not in generated_source:
        raise SmokeFailure("Generated SubModule must retain the selected module ID for owned SDK registrations.")
    if "ForgeApi.RegisterWhenAvailable(RegisterForge);" not in generated_source:
        raise SmokeFailure("Generated SubModule must defer SDK auto-registration until Forge is available.")
    if "ForgeApi.UnregisterWhenAvailable(RegisterForge);" not in generated_source:
        raise SmokeFailure("Generated SubModule must release its pending SDK availability subscription on unload.")
    if "ForgeApi.UnregisterUiPages(ModuleId);" not in generated_source:
        raise SmokeFailure("Generated SubModule must remove its registered UI pages on unload.")
    if "ForgeApi.AutoRegister(Assembly.GetExecutingAssembly(), ModuleId);" not in generated_source:
        raise SmokeFailure("Generated availability callback must auto-register this assembly under its module ID.")
    load_body = generated_source.partition("protected override void OnSubModuleLoad()")[2].partition(
        "protected override void OnSubModuleUnloaded()"
    )[0]
    if not load_body or "RegisterWhenAvailable(RegisterForge);" not in load_body or "AutoRegister(" in load_body:
        raise SmokeFailure("Generated SubModule must not attempt AutoRegister before Forge connects.")
    return csproj


def validate_assets(assets_path: Path, feed: Path, packages_root: Path, product_version: str) -> dict[str, Any]:
    assets = json.loads(assets_path.read_text(encoding="utf-8"))
    library_key = f"{SDK_PACKAGE_ID}/{product_version}"
    if library_key not in assets.get("libraries", {}):
        raise SmokeFailure(f"Restore did not resolve {library_key} from the produced local feed.")
    target_candidates = [
        (key, value)
        for key, value in assets.get("targets", {}).items()
        if key.lower() in {"net472", ".netframework,version=v4.7.2"}
    ]
    if not target_candidates:
        raise SmokeFailure("Restore assets do not contain the net472 framework target.")
    framework_target, target = target_candidates[0]
    library = target.get(library_key)
    if library is None:
        raise SmokeFailure(f"Restored framework target is missing {library_key}.")
    compile_assets = library.get("compile", {})
    if "lib/net472/CalradiaForge.Sdk.dll" not in compile_assets:
        raise SmokeFailure("SDK compile asset was not selected for net472.")
    runtime_assets = library.get("runtime", {})
    runtime_dll_assets = [path for path in runtime_assets if path.lower().endswith(".dll")]
    if runtime_dll_assets:
        raise SmokeFailure(f"SDK runtime DLLs must be excluded from generated mods: {sorted(runtime_dll_assets)}")
    package_root = packages_root / SDK_PACKAGE_ID.lower() / product_version.lower()
    package_metadata_path = package_root / ".nupkg.metadata"
    if not package_metadata_path.is_file():
        raise SmokeFailure(f"NuGet did not materialize package provenance metadata: {package_metadata_path}")
    package_metadata = json.loads(package_metadata_path.read_text(encoding="utf-8"))
    package_source = package_metadata.get("source")
    if not package_source or Path(package_source).resolve() != feed.resolve():
        raise SmokeFailure(
            f"SDK package was not restored from the generated local feed; NuGet recorded source={package_source!r}."
        )
    return {
        "compileAssets": sorted(compile_assets),
        "runtimeAssets": sorted(runtime_assets),
        "target": framework_target,
        "packageSource": package_source,
        "packageMetadata": str(package_metadata_path),
    }


def detect_game_path(argument: str | None) -> Path | None:
    candidates = [argument, os.environ.get("BANNERLORD_GAME_PATH")]
    candidates.append(r"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord")
    for candidate in candidates:
        if candidate:
            root = Path(candidate)
            required = root / "bin" / "Win64_Shipping_Client" / "TaleWorlds.MountAndBlade.dll"
            if required.is_file():
                return root.resolve()
    return None


def smoke(repo: Path, run_dir: Path, portable_only: bool, explicit_game_path: str | None) -> dict[str, Any]:
    commands: list[dict[str, Any]] = []
    feed, template_package, provenance, product_version = build_packages(repo, run_dir, commands)
    smoke_root = run_dir / "isolated-smoke"
    hive = smoke_root / "template-hive"
    module_output_root = smoke_root / "temporary-module"
    module_dir = module_output_root / "OnboardingSmokeMod"
    module_output_root.mkdir(parents=True, exist_ok=False)
    for path in (hive, smoke_root / "dotnet-home", smoke_root / "nuget-packages"):
        path.mkdir(parents=True, exist_ok=True)
    env = package_environment(smoke_root)
    env["DOTNET_CLI_HOME"] = str(smoke_root / "dotnet-home")
    env["NUGET_PACKAGES"] = str(smoke_root / "nuget-packages")
    cli = ["dotnet", "new"]
    run_logged(
        "install-template-isolated",
        cli + ["install", str(template_package), "--debug:custom-hive", str(hive)],
        cwd=repo,
        env=env,
        log_dir=run_dir / "logs",
        manifest=commands,
    )
    module_id = "OnboardingSmokeMod"
    run_logged(
        "generate-module",
        cli
        + [
            "calradiaforge-mod",
            "--name",
            module_id,
            "--output",
            str(module_output_root),
            "--debug:custom-hive",
            str(hive),
        ],
        cwd=repo,
        env=env,
        log_dir=run_dir / "logs",
        manifest=commands,
    )
    ensure_inside(smoke_root, module_output_root)
    if not module_dir.is_dir():
        raise SmokeFailure(f"Template engine did not create its preferred name directory: {module_dir}")
    project = validate_generated_module(module_dir, module_id, product_version)
    nuget_config = smoke_root / "NuGet.Config"
    config = ElementTree.Element("configuration")
    package_sources = ElementTree.SubElement(config, "packageSources")
    ElementTree.SubElement(package_sources, "clear")
    ElementTree.SubElement(package_sources, "add", {"key": "calradia-forge-local", "value": str(feed)})
    ElementTree.SubElement(
        package_sources,
        "add",
        {"key": "nuget.org", "value": "https://api.nuget.org/v3/index.json"},
    )
    ElementTree.ElementTree(config).write(nuget_config, encoding="utf-8", xml_declaration=True)
    run_logged(
        "restore-generated-module",
        [
            "dotnet",
            "restore",
            str(project),
            "--configfile",
            str(nuget_config),
            "--packages",
            str(smoke_root / "nuget-packages"),
            "--verbosity",
            "minimal",
        ],
        cwd=module_dir,
        env=env,
        log_dir=run_dir / "logs",
        manifest=commands,
    )
    asset_report = validate_assets(
        module_dir / "obj/project.assets.json",
        feed,
        smoke_root / "nuget-packages",
        product_version,
    )
    game_path = None if portable_only else detect_game_path(explicit_game_path)
    build_mode = "portable-generation-only"
    if game_path:
        build_mode = "local-gamebin-module-build"
        run_logged(
            "build-generated-module",
            [
                "dotnet",
                "build",
                str(project),
                "--configuration",
                "Release",
                "--no-restore",
                "--nologo",
                f"-p:GamePath={game_path}",
            ],
            cwd=module_dir,
            env=env,
            log_dir=run_dir / "logs",
            manifest=commands,
        )
        output = module_dir / "bin/Release/net472"
        if any(path.name.startswith("TaleWorlds.") and path.suffix.lower() == ".dll" for path in output.glob("*.dll")):
            raise SmokeFailure("Generated module output copied proprietary TaleWorlds binaries despite Private=false.")
        if (output / "CalradiaForge.Sdk.dll").exists():
            raise SmokeFailure("Generated module output duplicated CalradiaForge.Sdk.dll at runtime.")
    else:
        print("[mode] portable generation-only; module compilation and live load are not verified (no valid local GameBin).")

    provenance.update(
        {
            "buildMode": build_mode,
            "gamePath": str(game_path) if game_path else None,
            "generatedModulePath": str(module_dir),
            "generatedProject": str(project),
            "projectAssets": asset_report,
            "commands": commands,
            "limitations": ["NuGet package/template smoke does not prove IDE UI, game load, or in-game behavior."],
        }
    )
    return provenance


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("pack", "smoke"))
    parser.add_argument("--repo", type=Path, required=True)
    parser.add_argument("--output", type=Path, help="Unique output directory under artifacts/sdk-evolution/onboarding")
    parser.add_argument("--portable-only", action="store_true", help="Do not compile against a local game installation")
    parser.add_argument("--game-path", help="Bannerlord root containing bin/Win64_Shipping_Client")
    parser.add_argument("--no-pause", action="store_true", help="Accepted for BAT wrapper compatibility")
    args = parser.parse_args()
    repo = args.repo.resolve()
    run_dir = make_run_dir(repo, args.output)
    print(f"Onboarding output: {run_dir}")
    try:
        if args.mode == "pack":
            commands: list[dict[str, Any]] = []
            _, _, report, _ = build_packages(repo, run_dir, commands)
            report["commands"] = commands
        else:
            report = smoke(repo, run_dir, args.portable_only, args.game_path)
        report_path = run_dir / "source-package-report.json"
        report_path.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        print(f"PASS: {args.mode}; report={report_path}")
        if args.mode == "smoke" and report.get("buildMode") == "portable-generation-only":
            print("NOTE: portable generation is verified; full local GameBin build was intentionally not run.")
        return 0
    except (SmokeFailure, OSError, ValueError, zipfile.BadZipFile, ElementTree.ParseError) as exc:
        error_path = run_dir / "failure.txt"
        error_path.write_text(f"{type(exc).__name__}: {exc}\n", encoding="utf-8")
        print(f"FAIL: {exc}\nEvidence: {run_dir}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())

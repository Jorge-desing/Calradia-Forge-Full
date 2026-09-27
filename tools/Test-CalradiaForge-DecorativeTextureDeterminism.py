#!/usr/bin/env python3
"""Check ImageGen texture preparation determinism without writing project files.

The PowerShell preparation script renders into two isolated temporary folders
for ``-Check``. Its reported hashes are compared with the checked-in prepared
sprites, so this test also catches source assets that have not been refreshed.
"""

from __future__ import annotations

import hashlib
import re
import shutil
import struct
import subprocess
import sys
from pathlib import Path


TEXTURES = {
    "forge_war_table_cloth_v2.png": ((1024, 128), 24, False, False),
    "forge_rail_cartographic_field_v1.png": ((256, 256), 36, False, False),
    "forge_heraldic_overlay.png": ((256, 48), 112, True, True),
    "forge_patina_brass.png": ((128, 16), 88, False, False),
    "forge_pine_felt.png": ((128, 32), 40, False, False),
    "forge_header_summary_v1.png": ((128, 64), 112, True, False),
    "forge_header_modules_v1.png": ((128, 64), 112, True, False),
    "forge_header_logs_v1.png": ((128, 64), 112, True, False),
    "forge_header_inspector_v1.png": ((128, 64), 112, True, False),
    "forge_header_tests_v1.png": ((128, 64), 112, True, False),
    "forge_header_metrics_v1.png": ((128, 64), 112, True, False),
    "forge_header_framework_v1.png": ((128, 64), 112, True, False),
    "forge_header_extensions_v1.png": ((128, 64), 112, True, False),
}
RETIRED_TEXTURES = {"forge_dark_wood.png", "forge_inkwash.png", "forge_war_table_cloth.png"}
PREPARER = Path("tools/Prepare-CalradiaForge-ImageGenTextures.ps1")
VALIDATOR = Path("tools/Validate-CalradiaForge-DecorativeSprites.py")
ARCHIVE = Path("assets/gauntlet-imagegen/archive/2026-09-25")


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def png_dimensions(data: bytes, label: str) -> tuple[int, int]:
    if len(data) < 24 or data[:8] != b"\x89PNG\r\n\x1a\n" or data[12:16] != b"IHDR":
        raise AssertionError(f"Not a valid PNG header: {label}")
    return struct.unpack(">II", data[16:24])


def verify_archive(root: Path) -> None:
    manifest = root / ARCHIVE / "SHA256SUMS.txt"
    if not manifest.is_file():
        raise FileNotFoundError(f"Retired texture SHA-256 archive manifest is missing: {manifest}")
    required = {
        "pine_woodgrain.png",
        "inkwash_vellum.png",
        "war_table_illustrated_cloth_v1.png",
        "prepared-forge_dark_wood.png",
        "prepared-forge_inkwash.png",
        "prepared-forge_war_table_cloth.png",
        "spriteparts-forge_war_table_cloth.png",
    }
    observed: set[str] = set()
    for line in manifest.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        match = re.fullmatch(r"([0-9A-F]{64})  ([^ ]+)  \[source=([^\]]+)\]", line)
        if match is None:
            raise AssertionError(f"Malformed archived texture integrity row: {line}")
        digest, name, _source = match.groups()
        archive_path = root / ARCHIVE / name
        if not archive_path.is_file() or sha256(archive_path.read_bytes()) != digest:
            raise AssertionError(f"Archived texture SHA-256 mismatch: {archive_path}")
        observed.add(name)
    if not required <= observed:
        raise AssertionError("Texture archive omitted required historical file(s): " + ", ".join(sorted(required - observed)))


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    preparer = root / PREPARER
    if not preparer.is_file():
        raise FileNotFoundError(f"ImageGen texture preparation script is missing: {preparer}")
    powershell = shutil.which("powershell.exe") or shutil.which("powershell")
    if not powershell:
        raise RuntimeError("Windows PowerShell was not found; run this check from its .bat wrapper on Windows.")

    prepared_directory = root / "assets/gauntlet-imagegen/prepared"
    sprite_directory = root / "modules/CalradiaForge/GUI/SpriteParts/ui_calradiaforge"
    verify_archive(root)
    import importlib.util
    validator_spec = importlib.util.spec_from_file_location(
        "calradiaforge_decorative_validator", root / VALIDATOR
    )
    if validator_spec is None or validator_spec.loader is None:
        raise RuntimeError("Could not load the decorative PNG validator for alpha/edge checks.")
    validator = importlib.util.module_from_spec(validator_spec)
    sys.modules[validator_spec.name] = validator
    validator_spec.loader.exec_module(validator)
    originals: dict[str, bytes] = {}
    for name, (dimensions, max_alpha, require_transparency, require_edge_content) in TEXTURES.items():
        path = prepared_directory / name
        if not path.is_file():
            raise FileNotFoundError(f"Canonical prepared ImageGen texture is missing: {path}")
        content = path.read_bytes()
        observed_dimensions = png_dimensions(content, str(path))
        if observed_dimensions != dimensions:
            raise AssertionError(
                f"{name} has dimensions {observed_dimensions}; expected {dimensions}."
            )
        facts = validator.inspect_png(path)
        if facts.color_type != 6 or facts.bit_depth != 8:
            raise AssertionError(f"{name} must use RGBA8 pixels.")
        if facts.alpha_max == 0 or facts.alpha_max > max_alpha:
            raise AssertionError(f"{name} exceeds its alpha cap {max_alpha} or is invisible.")
        if require_transparency and (facts.alpha_min != 0 or facts.alpha_min == facts.alpha_max):
            raise AssertionError(f"{name} must retain transparent and visible pixels.")
        if require_edge_content and (
            facts.left_edge_visible_pixels == 0 or facts.right_edge_visible_pixels == 0
        ):
            raise AssertionError(f"{name} must preserve visible artwork at both horizontal ends.")
        originals[name] = content

        sprite_path = sprite_directory / name
        if not sprite_path.is_file():
            raise FileNotFoundError(f"Generated SpriteParts texture is missing: {sprite_path}")
        if sprite_path.read_bytes() != content:
            raise AssertionError(f"SpriteParts/{name} differs from its canonical prepared texture.")
    stale = sorted(name for name in RETIRED_TEXTURES if (sprite_directory / name).exists())
    if stale:
        raise AssertionError("Retired Gauntlet texture source(s) remain in SpriteParts: " + ", ".join(stale))
    stale_prepared = sorted(name for name in RETIRED_TEXTURES if (prepared_directory / name).exists())
    if stale_prepared:
        raise AssertionError("Retired Gauntlet texture source(s) remain in prepared/: " + ", ".join(stale_prepared))

    command = [
        powershell,
        "-NoLogo",
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        str(preparer),
        "-Check",
    ]
    result = subprocess.run(
        command,
        cwd=root,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        check=False,
    )
    print(result.stdout, end="" if result.stdout.endswith("\n") else "\n")
    if result.returncode != 0:
        raise RuntimeError(f"ImageGen preparation check failed with exit code {result.returncode}.")
    if "PASS: both isolated preparations produced identical RGBA PNG hashes" not in result.stdout:
        raise AssertionError("Preparation script did not confirm identical hashes for its two isolated renders.")

    reported: dict[str, tuple[tuple[int, int], int, str]] = {}
    report_pattern = re.compile(
        r"^(?P<name>forge_[a-z0-9_]+\.png):\s+(?P<width>\d+)x(?P<height>\d+),\s+"
        r"alpha\s+(?P<alpha_min>\d+)-(?P<alpha_max>\d+),\s+visible\s+\d+,\s+"
        r"SHA256\s+(?P<sha>[0-9A-Fa-f]{64})$",
        re.MULTILINE,
    )
    for match in report_pattern.finditer(result.stdout):
        name = match.group("name")
        if name in TEXTURES:
            reported[name] = (
                (int(match.group("width")), int(match.group("height"))),
                int(match.group("alpha_max")),
                match.group("sha").upper(),
            )

    if set(reported) != set(TEXTURES):
        missing = sorted(set(TEXTURES) - set(reported))
        raise AssertionError("Preparation output omitted texture report(s): " + ", ".join(missing))

    for name, (expected_dimensions, expected_alpha, _require_transparency, require_edge_content) in TEXTURES.items():
        dimensions, alpha_max, prepared_hash = reported[name]
        if dimensions != expected_dimensions:
            raise AssertionError(f"{name} preparation reports {dimensions}; expected {expected_dimensions}.")
        if alpha_max <= 0 or alpha_max > expected_alpha:
            raise AssertionError(f"{name} preparation reports max alpha {alpha_max}; expected 1..{expected_alpha}.")
        if sha256(originals[name]) != prepared_hash:
            raise AssertionError(
                f"Checked-in {name} does not match the deterministic prepared output hash {prepared_hash}."
            )
        if require_edge_content:
            edge_match = re.search(
                r"^\s+visible art in left/right edge bands: (?P<left>\d+)/(?P<right>\d+)$",
                result.stdout,
                re.MULTILINE,
            )
            if edge_match is None or int(edge_match.group("left")) <= 0 or int(edge_match.group("right")) <= 0:
                raise AssertionError(f"Preparation did not confirm art at both horizontal ends of {name}.")

    changed = [
        name for name, before in originals.items()
        if not (prepared_directory / name).is_file()
        or (prepared_directory / name).read_bytes() != before
        or not (sprite_directory / name).is_file()
        or (sprite_directory / name).read_bytes() != before
    ]
    if changed:
        raise AssertionError("Preparation check modified project textures: " + ", ".join(changed))

    print("PASS: thirteen ImageGen-derived textures match deterministic preparation hashes and RGBA8 contracts.")
    print("PASS: the heraldic overlay retains transparent pixels and artwork at both horizontal ends.")
    print("PASS: dimensions and alpha maxima match the preparation report; source PNGs remained unchanged.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:  # Keep the .bat output concise and actionable.
        print(f"FAIL: {error}", file=sys.stderr)
        raise SystemExit(1)

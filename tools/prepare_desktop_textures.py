#!/usr/bin/env python3
"""Create compact WPF texture resources from the retained full-size artwork."""

from __future__ import annotations

import argparse
import hashlib
import io
import os
import sys
import tempfile
from pathlib import Path

try:
    from PIL import Image
except ImportError as error:  # pragma: no cover - exercised by the command wrapper
    raise SystemExit("Pillow is required only to regenerate Desktop texture resources.") from error


ROOT = Path(__file__).resolve().parents[1]
SOURCE_DIR = ROOT / "src/CalradiaForge.Desktop/Resources/Textures"
OUTPUT_DIR = SOURCE_DIR / "Optimized"
LANCZOS = getattr(Image, "Resampling", Image).LANCZOS
MAX_PACKAGE_BYTES = 8_950_119  # Previous package cap plus 999,999 approved additional bytes.
MAX_DECODED_RGBA_BYTES = 16_810_046  # Previous decoded cap plus 999,999 approved additional bytes.

# Source dimensions are assertions: if an approved master changes, regeneration must
# be intentional and the packaged-resource limits in the render tests must be reviewed.
SPECS = {
    "war-table-illustrated-cloth.png": ((1254, 1254), None, None, "RGB"),
    "parchment-cartographic-paper.png": ((1254, 1254), None, None, "RGB"),
    # These title-bar brushes only display narrow source-image bands. Crop them before
    # WPF decodes the bitmap to avoid decoding millions of pixels that can never show.
    "titlebar-embroidered-cloth.png": ((2048, 768), (0, 338, 2048, 400), None, "RGB"),
    "titlebar-botanical-band-v1.png": ((2172, 724), (0, 246, 2172, 344), None, "RGBA"),
    # Rev072 commissioned panoramic banner: crop to the titlebar's narrow
    # 32:1 viewport before WPF decodes it and preserve the complete horizontal
    # source width for crisp rendering on wide/high-DPI desktops.
    "titlebar-cartographic-panorama-rev072.png": ((2172, 724), (0, 328, 2172, 395), None, "RGB"),
    # Small ornaments are downsampled only as far as their rendered size permits.
    "titlebar-heraldic-corner.png": ((1254, 1254), None, (96, 96), "RGBA"),
    "header-heraldic-compass-v2.png": ((1254, 1254), None, (128, 128), "RGBA"),
    "header-corner-engraving-v1.png": ((2172, 724), None, (192, 64), "RGBA"),
    "evidence-ledger-empty-v1.png": ((1254, 1254), None, (96, 96), "RGBA"),
    "rail-field-compass-v1.png": ((1254, 1254), None, (96, 96), "RGBA"),
    "parchment-field-journal-ornament-v1.png": ((1254, 1254), None, (96, 96), "RGBA"),
    "workbench-cartographic-board-v1.png": ((1672, 941), None, (448, 252), "RGB"),
    # Rev064 raises edge-engraving contrast for the rail while retaining the
    # transparent center and 1:2 frame composition at the 320x640 display size.
    "workbench-heraldic-rail-portrait-rev064.png": ((887, 1774), None, (320, 640), "RGBA"),
    "workbench-heraldic-shield-v1.png": ((1254, 1254), None, (80, 80), "RGBA"),
    # The transparent corner master is displayed only inside the optional
    # reference drawer; 384x256 retains crisp 2x detail for its 192x128 DIP slot.
    "dossier-corner-ornament-rev057.png": ((1536, 1024), None, (384, 256), "RGBA"),
}

# These authoring masters remain available, but their old derivatives are no
# longer referenced by WPF. Do not silently package stale files via the csproj
# resource glob or let them consume the decoded-texture budget.
RETIRED_VARIANTS = {
    "tool-card-top-corners-v1.png",
    "desktop-titlebar-heraldic-frame-v1.png",
    "desktop-rail-etched-field-v1.png",
    "desktop-card-corners-botanical-v1.png",
    "titlebar-cartographic-engraving-v2.png",
    "workbench-heraldic-rail-band-v1.png",
    "workbench-heraldic-rail-portrait-rev063.png",
    "titlebar-cartographic-panorama-rev057.png",
}


def encode_texture(name: str) -> bytes:
    expected_size, crop_box, target_size, mode = SPECS[name]
    source_path = SOURCE_DIR / name
    with Image.open(source_path) as source:
        if source.size != expected_size:
            raise ValueError(
                f"{name}: expected approved source size {expected_size[0]}x{expected_size[1]}, "
                f"found {source.width}x{source.height}"
            )
        image = source.convert(mode)

    if crop_box is not None:
        image = image.crop(crop_box)
    if target_size is not None:
        source_ratio = image.width / image.height
        target_ratio = target_size[0] / target_size[1]
        if abs(source_ratio - target_ratio) > 0.01:
            raise ValueError(
                f"{name}: resizing {image.width}x{image.height} to {target_size[0]}x{target_size[1]} "
                "would distort the approved artwork; choose a matching crop"
            )
        image = image.resize(target_size, LANCZOS)

    encoded = io.BytesIO()
    image.save(encoded, format="PNG", compress_level=9, optimize=False)
    return encoded.getvalue()


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--check",
        action="store_true",
        help="regenerate in memory and compare with packaged variants without writing files",
    )
    args = parser.parse_args()

    total_source_bytes = 0
    total_output_bytes = 0
    total_source_rgba_bytes = 0
    total_output_rgba_bytes = 0
    generated: dict[str, bytes] = {}
    try:
        if args.check:
            actual = {path.name for path in OUTPUT_DIR.glob("*.png")}
            expected = set(SPECS)
            missing = expected - actual
            unexpected = actual - expected - RETIRED_VARIANTS
            if missing or unexpected:
                raise ValueError(
                    "optimized resource inventory differs from the runtime manifest: "
                    f"missing={sorted(missing)}, unexpected={sorted(unexpected)}"
                )
        for name, (source_size, crop_box, target_size, mode) in SPECS.items():
            source_path = SOURCE_DIR / name
            output = encode_texture(name)
            output_path = OUTPUT_DIR / name
            if args.check:
                if not output_path.is_file() or output_path.read_bytes() != output:
                    raise ValueError(f"{name}: optimized resource is stale or missing; rerun the .bat without --check")
            else:
                generated[name] = output

            with Image.open(source_path) as source:
                source_pixels = source.width * source.height * 4
                total_source_bytes += source_path.stat().st_size
                total_source_rgba_bytes += source_pixels
            with Image.open(io.BytesIO(output)) as optimized:
                optimized_pixels = optimized.width * optimized.height * 4
                total_output_bytes += len(output)
                total_output_rgba_bytes += optimized_pixels
                print(
                    f"{name}|{source.width}x{source.height}->{optimized.width}x{optimized.height}|"
                    f"{source_path.stat().st_size}->{len(output)} bytes|"
                    f"sha256={hashlib.sha256(output).hexdigest().upper()}"
                )

        if total_output_bytes > MAX_PACKAGE_BYTES:
            raise ValueError(
                f"optimized Desktop textures exceed the {MAX_PACKAGE_BYTES}-byte package budget: "
                f"{total_output_bytes} bytes"
            )
        if total_output_rgba_bytes > MAX_DECODED_RGBA_BYTES:
            raise ValueError(
                f"optimized Desktop textures exceed the {MAX_DECODED_RGBA_BYTES}-byte decoded RGBA budget: "
                f"{total_output_rgba_bytes} bytes"
            )

        if not args.check:
            OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
            with tempfile.TemporaryDirectory(prefix="desktop-textures-", dir=str(OUTPUT_DIR.parent)) as temporary:
                staging = Path(temporary)
                for name, data in generated.items():
                    (staging / name).write_bytes(data)
                for name in generated:
                    output_path = OUTPUT_DIR / name
                    if output_path.is_file() and output_path.read_bytes() == generated[name]:
                        continue
                    staged_path = staging / name
                    if output_path.exists():
                        os.replace(staged_path, output_path)
                    else:
                        # Creating a new package resource directly avoids a
                        # Windows scanner denying the temp-file rename; an
                        # existing resource still uses an atomic replacement.
                        with output_path.open("xb") as destination:
                            destination.write(generated[name])
                            destination.flush()
                            os.fsync(destination.fileno())
            for name in RETIRED_VARIANTS:
                try:
                    (OUTPUT_DIR / name).unlink()
                except FileNotFoundError:
                    pass
                except PermissionError:
                    # A running process or security scanner may hold an old derivative
                    # open. The project embeds only the explicit active manifest, so a
                    # locked historical file cannot enter the runtime package.
                    print(f"RETIRED_LOCKED|{name}|not embedded by the project resource manifest")

    except (OSError, ValueError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print(f"SOURCE_TOTAL|{total_source_bytes} bytes|{total_source_rgba_bytes} decoded bytes")
    print(f"PACKAGE_TOTAL|{total_output_bytes} bytes|{total_output_rgba_bytes} decoded bytes")
    print("PASS: Desktop texture variants are deterministic." if args.check else "PASS: optimized Desktop textures written.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

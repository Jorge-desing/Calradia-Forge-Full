#!/usr/bin/env python3
"""
Unit tests for Calradia Forge High-Quality Image Asset Pipeline
==============================================================
"""

import json
import shutil
import tempfile
import unittest
from pathlib import Path

from PIL import Image, ImageDraw

# Add repository root to path
ROOT = Path(__file__).resolve().parent.parent
import sys
sys.path.insert(0, str(ROOT))

from tools.process_high_quality_asset import (
    VALID_POT_SIZES,
    apply_color_bleed,
    compute_sha256,
    process_asset,
    remove_background,
    scale_to_dimensions,
    scale_to_pot,
)


class TestHighQualityImagePipeline(unittest.TestCase):
    def setUp(self):
        self.temp_dir = Path(tempfile.mkdtemp(prefix="cf_img_test_"))

    def tearDown(self):
        shutil.rmtree(self.temp_dir, ignore_errors=True)

    def create_synthetic_image(self, width: int = 300, height: int = 200, bg_color=(15, 15, 15)) -> Path:
        img_path = self.temp_dir / f"synthetic_{width}x{height}.png"
        img = Image.new("RGB", (width, height), bg_color)
        draw = ImageDraw.Draw(img)
        # Draw a golden sword blade in the center, well away from the corners
        mid_x, mid_y = width // 2, height // 2
        draw.line((mid_x - width // 4, mid_y, mid_x + width // 4, mid_y), fill=(212, 175, 55), width=max(4, height // 20))
        draw.rectangle((mid_x - 10, mid_y - 15, mid_x + 10, mid_y + 15), fill=(180, 140, 30))
        img.save(img_path, format="PNG")
        return img_path

    def test_pot_scaling_aspect_ratio_preserved(self):
        input_path = self.create_synthetic_image(300, 150)
        with Image.open(input_path) as src:
            result = scale_to_pot(src, 512, preserve_aspect=True)
            self.assertEqual(result.size, (512, 512))
            self.assertEqual(result.mode, "RGBA")

    def test_invalid_pot_size_raises_value_error(self):
        input_path = self.create_synthetic_image(100, 100)
        with Image.open(input_path) as src:
            with self.assertRaises(ValueError):
                scale_to_pot(src, 500)

    def test_background_removal_and_alpha(self):
        input_path = self.create_synthetic_image(100, 100, bg_color=(10, 10, 10))
        with Image.open(input_path) as src:
            keyed = remove_background(src, bg_threshold=20)
            self.assertEqual(keyed.mode, "RGBA")
            pixels = keyed.load()
            # Corner should be transparent
            self.assertEqual(pixels[0, 0][3], 0)
            # Center of the blade should be opaque or semi-opaque
            self.assertGreater(pixels[50, 50][3], 200)

    def test_color_bleed_padding(self):
        # Create 10x10 transparent image with single 2x2 opaque red block in center
        img = Image.new("RGBA", (10, 10), (0, 0, 0, 0))
        pixels = img.load()
        pixels[5, 5] = (255, 0, 0, 255)
        pixels[5, 6] = (255, 0, 0, 255)

        dilated = apply_color_bleed(img, pad_pixels=1)
        dilated_pixels = dilated.load()

        # The neighbor should have red color RGB, but alpha remains 0
        neighbor = dilated_pixels[4, 5]
        self.assertEqual(neighbor[0], 255)
        self.assertEqual(neighbor[3], 0)

    def test_full_pipeline_process_asset_with_report(self):
        input_path = self.create_synthetic_image(400, 300)
        output_path = self.temp_dir / "output_sword.png"
        report_path = self.temp_dir / "output_sword.audit.json"

        report = process_asset(
            input_path=input_path,
            output_path=output_path,
            mode="icon",
            pot_size=512,
            remove_bg=True,
            bg_threshold=25,
            pad_pixels=2,
            report_path=report_path,
        )

        self.assertTrue(output_path.exists())
        self.assertTrue(report_path.exists())
        self.assertEqual(report["output_size"], [512, 512])
        self.assertEqual(report["status"], "SUCCESS")
        self.assertEqual(report["sha256"], compute_sha256(output_path))

        with open(report_path, "r", encoding="utf-8") as f:
            data = json.load(f)
            self.assertEqual(data["sha256"], report["sha256"])
            self.assertEqual(data["mode"], "icon")


if __name__ == "__main__":
    unittest.main()

"""
Procedural generator for the Imperial War Table Astrolabe Dial / Compass Rose (Rev086).
Generates a high-resolution 512x512 master image in sRGB with rich tactical detail.
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_MASTER = ROOT / "artifacts" / "calradia_astrolabe_dial_master.png"

def create_astrolabe_dial():
    size = 512
    cx, cy = size / 2, size / 2
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Outer decorative ring & drop shadow
    outer_r = 240
    # Soft drop shadow
    for offset, alpha in [(4, 25), (3, 40), (2, 60), (1, 90)]:
        draw.ellipse([cx - outer_r + offset, cy - outer_r + offset,
                      cx + outer_r + offset, cy + outer_r + offset],
                     fill=(15, 12, 10, alpha))

    # Outer bezel ring (Deep Bronze #3D2D1D)
    draw.ellipse([cx - outer_r, cy - outer_r, cx + outer_r, cy + outer_r],
                 fill=(61, 45, 29, 255), outline=(197, 160, 89, 255), width=3)

    # Secondary decorative ring (Patinated Brass #8C6D37)
    r2 = 232
    draw.ellipse([cx - r2, cy - r2, cx + r2, cy + r2],
                 fill=(40, 32, 24, 255), outline=(140, 109, 55, 255), width=2)

    # Dial plate background (Dark Coal / Deep Pine blend #141A17)
    r_plate = 224
    draw.ellipse([cx - r_plate, cy - r_plate, cx + r_plate, cy + r_plate],
                 fill=(20, 26, 23, 255), outline=(212, 175, 55, 200), width=2)

    # 2. Degree ticks (360 degrees: major every 30°, medium every 10°, minor every 5°)
    for deg in range(360):
        rad = math.radians(deg)
        cos_a = math.cos(rad)
        sin_a = math.sin(rad)
        if deg % 30 == 0:
            # Major tick
            r_in = r_plate - 16
            draw.line([(cx + r_in * cos_a, cy + r_in * sin_a),
                       (cx + r_plate * cos_a, cy + r_plate * sin_a)],
                      fill=(225, 190, 80, 255), width=3)
        elif deg % 10 == 0:
            # Medium tick
            r_in = r_plate - 10
            draw.line([(cx + r_in * cos_a, cy + r_in * sin_a),
                       (cx + r_plate * cos_a, cy + r_plate * sin_a)],
                      fill=(180, 150, 70, 220), width=2)
        elif deg % 5 == 0:
            # Minor tick
            r_in = r_plate - 6
            draw.line([(cx + r_in * cos_a, cy + r_in * sin_a),
                       (cx + r_plate * cos_a, cy + r_plate * sin_a)],
                      fill=(140, 120, 60, 160), width=1)

    # Inner calibration ring
    r3 = 202
    draw.ellipse([cx - r3, cy - r3, cx + r3, cy + r3],
                 outline=(180, 150, 70, 180), width=1)

    # 3. Intermediate Astrolabe Ring with Verdigris Accents
    r4 = 175
    draw.ellipse([cx - r4, cy - r4, cx + r4, cy + r4],
                 outline=(78, 154, 120, 190), width=2) # Verdigris touch

    r5 = 145
    draw.ellipse([cx - r5, cy - r5, cx + r5, cy + r5],
                 outline=(212, 175, 55, 150), width=1)

    # 4. Sixteen-point faceted compass star
    # 8 minor points (radius 105), 4 medium intercardinal (radius 140), 4 major cardinal (radius 195)
    points_config = [
        # (angle, outer_radius, width_half)
        # Cardinals: N=270, E=0, S=90, W=180 in screen coords
        (270, 195, 22), (0, 195, 22), (90, 195, 22), (180, 195, 22),
        # Intercardinals: 45, 135, 225, 315
        (45, 140, 16), (135, 140, 16), (225, 140, 16), (315, 140, 16),
        # Minor points: 22.5, 67.5, 112.5, 157.5, 202.5, 247.5, 292.5, 337.5
        (22.5, 100, 10), (67.5, 100, 10), (112.5, 100, 10), (157.5, 100, 10),
        (202.5, 100, 10), (247.5, 100, 10), (292.5, 100, 10), (337.5, 100, 10),
    ]

    # Draw minor points first
    for angle, tip_r, base_w in points_config[8:]:
        rad = math.radians(angle)
        rad_perp = rad + math.pi / 2
        tip_x = cx + tip_r * math.cos(rad)
        tip_y = cy + tip_r * math.sin(rad)
        base_r = 30
        p_base1 = (cx + base_r * math.cos(rad) + base_w * math.cos(rad_perp),
                   cy + base_r * math.sin(rad) + base_w * math.sin(rad_perp))
        p_base2 = (cx + base_r * math.cos(rad) - base_w * math.cos(rad_perp),
                   cy + base_r * math.sin(rad) - base_w * math.sin(rad_perp))
        # Left facet (darker bronze)
        draw.polygon([(cx, cy), p_base1, (tip_x, tip_y)], fill=(120, 95, 45, 230))
        # Right facet (lighter gold)
        draw.polygon([(cx, cy), (tip_x, tip_y), p_base2], fill=(185, 150, 70, 230))

    # Draw intercardinal points
    for angle, tip_r, base_w in points_config[4:8]:
        rad = math.radians(angle)
        rad_perp = rad + math.pi / 2
        tip_x = cx + tip_r * math.cos(rad)
        tip_y = cy + tip_r * math.sin(rad)
        base_r = 38
        p_base1 = (cx + base_r * math.cos(rad) + base_w * math.cos(rad_perp),
                   cy + base_r * math.sin(rad) + base_w * math.sin(rad_perp))
        p_base2 = (cx + base_r * math.cos(rad) - base_w * math.cos(rad_perp),
                   cy + base_r * math.sin(rad) - base_w * math.sin(rad_perp))
        # Left facet (deep brass)
        draw.polygon([(cx, cy), p_base1, (tip_x, tip_y)], fill=(155, 120, 50, 245))
        # Right facet (bright imperial gold)
        draw.polygon([(cx, cy), (tip_x, tip_y), p_base2], fill=(225, 185, 75, 245))

    # Draw major cardinal points (N, S, E, W)
    for angle, tip_r, base_w in points_config[0:4]:
        rad = math.radians(angle)
        rad_perp = rad + math.pi / 2
        tip_x = cx + tip_r * math.cos(rad)
        tip_y = cy + tip_r * math.sin(rad)
        base_r = 45
        p_base1 = (cx + base_r * math.cos(rad) + base_w * math.cos(rad_perp),
                   cy + base_r * math.sin(rad) + base_w * math.sin(rad_perp))
        p_base2 = (cx + base_r * math.cos(rad) - base_w * math.cos(rad_perp),
                   cy + base_r * math.sin(rad) - base_w * math.sin(rad_perp))
        # Left facet (rich antique bronze)
        draw.polygon([(cx, cy), p_base1, (tip_x, tip_y)], fill=(180, 140, 55, 255), outline=(212, 175, 55, 255))
        # Right facet (burnished gold highlight)
        draw.polygon([(cx, cy), (tip_x, tip_y), p_base2], fill=(245, 210, 95, 255), outline=(212, 175, 55, 255))

    # 5. Center Astrolabe Hub & Pivot
    r_hub = 42
    draw.ellipse([cx - r_hub, cy - r_hub, cx + r_hub, cy + r_hub],
                 fill=(45, 34, 22, 255), outline=(230, 195, 80, 255), width=3)

    r_hub_in = 32
    draw.ellipse([cx - r_hub_in, cy - r_hub_in, cx + r_hub_in, cy + r_hub_in],
                 fill=(26, 36, 30, 255), outline=(82, 160, 125, 240), width=2) # Verdigris inner

    # Center jewel / brass rivet
    r_center = 16
    draw.ellipse([cx - r_center, cy - r_center, cx + r_center, cy + r_center],
                 fill=(212, 175, 55, 255), outline=(130, 95, 35, 255), width=2)

    # Rivet highlight
    draw.ellipse([cx - 6, cy - 6, cx + 4, cy + 4],
                 fill=(255, 240, 160, 220))

    # 6. Cardinal lettering (N, E, S, W)
    try:
        font = ImageFont.truetype("georgia.ttf", 20)
    except IOError:
        font = ImageFont.load_default()

    draw.text((cx - 7, cy - 192), "N", fill=(255, 225, 120, 255), font=font)
    draw.text((cx + 178, cy - 12), "E", fill=(235, 200, 100, 255), font=font)
    draw.text((cx - 6, cy + 168), "S", fill=(235, 200, 100, 255), font=font)
    draw.text((cx - 194, cy - 12), "W", fill=(235, 200, 100, 255), font=font)

    OUTPUT_MASTER.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUTPUT_MASTER, "PNG")
    print(f"Master astrolabe dial generated: {OUTPUT_MASTER}")

if __name__ == "__main__":
    create_astrolabe_dial()

"""
Procedural generator for the Calradic Imperial Aquila War Seal (Rev087).
Synthesizes a master 512x512 PNG with alpha transparency, imperial porphyry wax bed,
golden laurel wreath, and a sculpted imperial aquila with central sapphire jewel.
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_MASTER = ROOT / "artifacts" / "calradia_aquila_seal_master.png"


def create_aquila_seal():
    size = 512
    cx, cy = size / 2.0, size / 2.0
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Soft Drop Shadow (multi-pass for depth)
    shadow_r = 238
    for offset_y, offset_x, alpha in [(6, 2, 35), (4, 1, 55), (2, 0, 85)]:
        draw.ellipse(
            [cx - shadow_r + offset_x, cy - shadow_r + offset_y,
             cx + shadow_r + offset_x, cy + shadow_r + offset_y],
            fill=(12, 10, 8, alpha)
        )

    # 2. Outer Golden Bezel & Laurel Wreath Base
    bezel_r = 232
    draw.ellipse(
        [cx - bezel_r, cy - bezel_r, cx + bezel_r, cy + bezel_r],
        fill=(55, 42, 28, 255), outline=(197, 160, 89, 255), width=3
    )

    # Laurel Wreath: 20 leaf pairs flanking left and right
    for i in range(24):
        # Angle from bottom (90°) curving up towards top (270°)
        # Left side: 95° to 265°
        angle_l = 95 + i * 7.2
        rad_l = math.radians(angle_l)
        r_leaf = 222
        lx = cx + r_leaf * math.cos(rad_l)
        ly = cy + r_leaf * math.sin(rad_l)
        # Draw leaf oval rotated along tangent
        tangent_l = rad_l + math.pi / 2
        leaf_len = 11
        leaf_w = 4.5
        tip_x = lx + leaf_len * math.cos(tangent_l)
        tip_y = ly + leaf_len * math.sin(tangent_l)
        draw.polygon([
            (lx - leaf_w * math.sin(tangent_l), ly + leaf_w * math.cos(tangent_l)),
            (tip_x, tip_y),
            (lx + leaf_w * math.sin(tangent_l), ly - leaf_w * math.cos(tangent_l)),
            (lx - leaf_len * 0.4 * math.cos(tangent_l), ly - leaf_len * 0.4 * math.sin(tangent_l))
        ], fill=(212, 175, 55, 230), outline=(245, 215, 120, 255))

        # Right side: 85° down through 0° to 275°
        angle_r = 85 - i * 7.2
        rad_r = math.radians(angle_r)
        rx = cx + r_leaf * math.cos(rad_r)
        ry = cy + r_leaf * math.sin(rad_r)
        tangent_r = rad_r - math.pi / 2
        tip_rx = rx + leaf_len * math.cos(tangent_r)
        tip_ry = ry + leaf_len * math.sin(tangent_r)
        draw.polygon([
            (rx - leaf_w * math.sin(tangent_r), ry + leaf_w * math.cos(tangent_r)),
            (tip_rx, tip_ry),
            (rx + leaf_w * math.sin(tangent_r), ry - leaf_w * math.cos(tangent_r)),
            (rx - leaf_len * 0.4 * math.cos(tangent_r), ry - leaf_len * 0.4 * math.sin(tangent_r))
        ], fill=(212, 175, 55, 230), outline=(245, 215, 120, 255))

    # Laurel Berry Clusters at quadrant points
    for berry_ang in [30, 60, 120, 150, 210, 240, 300, 330]:
        brad = math.radians(berry_ang)
        bx = cx + 212 * math.cos(brad)
        by = cy + 212 * math.sin(brad)
        draw.ellipse([bx - 3, by - 3, bx + 3, by + 3], fill=(240, 195, 75, 255), outline=(130, 95, 30, 255))

    # 3. Inner Beaded Brass Ring
    inner_bezel_r = 206
    draw.ellipse(
        [cx - inner_bezel_r, cy - inner_bezel_r, cx + inner_bezel_r, cy + inner_bezel_r],
        fill=(42, 30, 22, 255), outline=(160, 125, 60, 255), width=2
    )
    # Studded rivets around the rim
    for deg in range(0, 360, 15):
        r_deg = math.radians(deg)
        rvx = cx + 200 * math.cos(r_deg)
        rvy = cy + 200 * math.sin(r_deg)
        draw.ellipse([rvx - 2, rvy - 2, rvx + 2, rvy + 2], fill=(225, 195, 100, 240))

    # 4. Imperial Porphyry / Crimson Wax Seal Bed
    wax_r = 194
    # Concentric gradient shading for convex 3D wax look
    for wr in range(wax_r, 0, -2):
        factor = wr / wax_r
        # Deep burgundy edge #3A0C13 to rich imperial crimson #7B1A28 in center
        r_col = int(58 + (123 - 58) * (1.0 - factor * 0.6))
        g_col = int(12 + (26 - 12) * (1.0 - factor * 0.6))
        b_col = int(20 + (40 - 20) * (1.0 - factor * 0.6))
        draw.ellipse([cx - wr, cy - wr, cx + wr, cy + wr], fill=(r_col, g_col, b_col, 255))

    # Fine inner filigree ring on the wax bed
    draw.ellipse([cx - 168, cy - 168, cx + 168, cy + 168], outline=(180, 140, 60, 110), width=1)
    draw.ellipse([cx - 162, cy - 162, cx + 162, cy + 162], outline=(220, 180, 80, 140), width=1)

    # 5. Imperial Aquila (Sculpted Golden Eagle with spread wings)
    # Eagle center is at cx, cy - 4
    ecx = cx
    ecy = cy - 6

    # A. Tail Feathers (Fanned downward from ecy + 60 to ecy + 130)
    for tail_idx in range(-3, 4):
        spread = tail_idx * 13
        curve = abs(tail_idx) * 4
        poly_tail = [
            (ecx + spread * 0.4, ecy + 65),
            (ecx + spread * 1.2 - 6, ecy + 125 - curve),
            (ecx + spread * 1.2, ecy + 135 - curve),
            (ecx + spread * 1.2 + 6, ecy + 125 - curve),
            (ecx + spread * 0.4 + 4, ecy + 65)
        ]
        draw.polygon(poly_tail, fill=(185, 145, 55, 255), outline=(245, 210, 110, 255))

    # B. Majestic Wings (Tier 1: Primary flight feathers, Tier 2: Secondary, Tier 3: Coverts)
    def draw_wing(side):
        # side: -1 for left, +1 for right
        s = side
        # Primary outer feathers (7 major long pinions)
        primary_tips = [
            (65, -85), (95, -75), (120, -55), (138, -30),
            (148, 0), (145, 30), (130, 60)
        ]
        for idx, (px, py) in enumerate(primary_tips):
            tip = (ecx + s * px, ecy + py)
            base1 = (ecx + s * (px * 0.45 - 8), ecy + py * 0.5 + 10)
            base2 = (ecx + s * (px * 0.45 + 8), ecy + py * 0.5 + 20)
            feather_poly = [
                (ecx + s * 22, ecy + 5),
                base1,
                tip,
                base2,
                (ecx + s * 18, ecy + 30)
            ]
            shade = 175 + idx * 8
            draw.polygon(feather_poly, fill=(shade, int(shade * 0.78), int(shade * 0.3), 255),
                         outline=(245, 215, 115, 255))

        # Secondary middle feathers (layered over primaries)
        secondary_tips = [
            (50, -55), (75, -45), (95, -25), (108, 0), (105, 25), (90, 48)
        ]
        for idx, (sx, sy) in enumerate(secondary_tips):
            tip = (ecx + s * sx, ecy + sy)
            feather_poly = [
                (ecx + s * 25, ecy - 5),
                (ecx + s * (sx * 0.6 - 5), ecy + sy * 0.6 + 5),
                tip,
                (ecx + s * (sx * 0.6 + 5), ecy + sy * 0.6 + 12),
                (ecx + s * 22, ecy + 25)
            ]
            draw.polygon(feather_poly, fill=(205, 165, 65, 255), outline=(255, 230, 140, 255))

        # Wing Covert Shoulder Arch (Solid embossed shoulder)
        shoulder_poly = [
            (ecx + s * 14, ecy - 45),
            (ecx + s * 35, ecy - 65),
            (ecx + s * 65, ecy - 60),
            (ecx + s * 70, ecy - 40),
            (ecx + s * 45, ecy - 10),
            (ecx + s * 20, ecy + 15)
        ]
        draw.polygon(shoulder_poly, fill=(225, 185, 75, 255), outline=(255, 240, 160, 255))

    # Draw both wings
    draw_wing(-1)
    draw_wing(1)

    # C. Talons and Imperial Regalia
    # Left Talon holding Laurel Olive Branch
    draw.ellipse([ecx - 48, ecy + 68, ecx - 30, ecy + 82], fill=(220, 180, 70, 255), outline=(255, 225, 130, 255))
    draw.line([(ecx - 65, ecy + 85), (ecx - 20, ecy + 65)], fill=(120, 180, 90, 255), width=3)
    # Olive leaves on left talon
    for olx, oly in [(-60, 80), (-52, 73), (-42, 68), (-32, 63)]:
        draw.ellipse([ecx + olx - 4, ecy + oly - 2, ecx + olx + 4, ecy + oly + 2],
                     fill=(90, 160, 80, 255), outline=(190, 230, 180, 255))

    # Right Talon holding Bundle of Thunderbolts / Arrows
    draw.ellipse([ecx + 30, ecy + 68, ecx + 48, ecy + 82], fill=(220, 180, 70, 255), outline=(255, 225, 130, 255))
    # Lightning / arrow lines
    draw.line([(ecx + 20, ecy + 65), (ecx + 65, ecy + 85)], fill=(240, 200, 80, 255), width=3)
    draw.line([(ecx + 24, ecy + 80), (ecx + 62, ecy + 70)], fill=(240, 200, 80, 255), width=2)
    # Arrow heads
    draw.polygon([(ecx + 65, ecy + 85), (ecx + 58, ecy + 80), (ecx + 68, ecy + 80)], fill=(255, 220, 100, 255))
    draw.polygon([(ecx + 62, ecy + 70), (ecx + 55, ecy + 65), (ecx + 65, ecy + 65)], fill=(255, 220, 100, 255))

    # D. Torso & Muscled Breastplate
    torso_poly = [
        (ecx - 18, ecy - 35),
        (ecx + 18, ecy - 35),
        (ecx + 26, ecy + 5),
        (ecx + 20, ecy + 60),
        (ecx, ecy + 75),
        (ecx - 20, ecy + 60),
        (ecx - 26, ecy + 5)
    ]
    draw.polygon(torso_poly, fill=(215, 175, 65, 255), outline=(255, 230, 140, 255))

    # Segmented pectoral armor plates on torso
    draw.polygon([(ecx - 16, ecy - 28), (ecx - 2, ecy - 28), (ecx - 2, ecy - 8), (ecx - 20, ecy - 12)],
                 fill=(235, 195, 85, 255), outline=(160, 120, 40, 255))
    draw.polygon([(ecx + 2, ecy - 28), (ecx + 16, ecy - 28), (ecx + 20, ecy - 12), (ecx + 2, ecy - 8)],
                 fill=(235, 195, 85, 255), outline=(160, 120, 40, 255))

    # Lower abdominal cuirass segments
    draw.polygon([(ecx - 14, ecy + 8), (ecx + 14, ecy + 8), (ecx + 10, ecy + 25), (ecx - 10, ecy + 25)],
                 fill=(205, 165, 55, 255), outline=(140, 100, 30, 255))
    draw.polygon([(ecx - 10, ecy + 27), (ecx + 10, ecy + 27), (ecx + 6, ecy + 45), (ecx - 6, ecy + 45)],
                 fill=(195, 155, 45, 255), outline=(140, 100, 30, 255))

    # E. Double-Headed Imperial Eagle Heads with Beaks
    # Left Head (facing west)
    draw.polygon([
        (ecx - 8, ecy - 35),
        (ecx - 16, ecy - 52),
        (ecx - 26, ecy - 55),
        (ecx - 38, ecy - 48), # Beak tip left
        (ecx - 25, ecy - 42),
        (ecx - 12, ecy - 32)
    ], fill=(230, 190, 80, 255), outline=(255, 235, 150, 255))
    # Eye left
    draw.ellipse([ecx - 22, ecy - 52, ecx - 18, ecy - 48], fill=(40, 10, 15, 255))
    draw.ellipse([ecx - 21, ecy - 51, ecx - 19, ecy - 49], fill=(255, 255, 255, 255))

    # Right Head (facing east)
    draw.polygon([
        (ecx + 8, ecy - 35),
        (ecx + 16, ecy - 52),
        (ecx + 26, ecy - 55),
        (ecx + 38, ecy - 48), # Beak tip right
        (ecx + 25, ecy - 42),
        (ecx + 12, ecy - 32)
    ], fill=(230, 190, 80, 255), outline=(255, 235, 150, 255))
    # Eye right
    draw.ellipse([ecx + 18, ecy - 52, ecx + 22, ecy - 48], fill=(40, 10, 15, 255))
    draw.ellipse([ecx + 19, ecy - 51, ecx + 21, ecy - 49], fill=(255, 255, 255, 255))

    # F. Imperial Diadem / Crown Surmounting Heads
    crown_poly = [
        (ecx - 16, ecy - 55),
        (ecx - 20, ecy - 72),
        (ecx - 10, ecy - 64),
        (ecx, ecy - 78),      # High central spire
        (ecx + 10, ecy - 64),
        (ecx + 20, ecy - 72),
        (ecx + 16, ecy - 55)
    ]
    draw.polygon(crown_poly, fill=(240, 205, 95, 255), outline=(255, 245, 180, 255))
    # Crown jewels / pearls
    for cpx, cpy in [(-20, -72), (-10, -64), (0, -78), (10, -64), (20, -72)]:
        draw.ellipse([ecx + cpx - 2.5, ecy + cpy - 2.5, ecx + cpx + 2.5, ecy + cpy + 2.5],
                     fill=(255, 255, 240, 255), outline=(150, 120, 50, 255))

    # G. Central Cut Sapphire Jewel on the Breastplate
    # Faceted rhomboid jewel
    j_cx, j_cy = ecx, ecy - 2
    # Gold setting bezel
    draw.ellipse([j_cx - 15, j_cy - 18, j_cx + 15, j_cy + 18], fill=(160, 120, 40, 255), outline=(245, 215, 110, 255), width=2)
    # Deep sapphire body
    draw.polygon([
        (j_cx, j_cy - 14),
        (j_cx + 10, j_cy - 6),
        (j_cx + 10, j_cy + 6),
        (j_cx, j_cy + 14),
        (j_cx - 10, j_cy + 6),
        (j_cx - 10, j_cy - 6)
    ], fill=(18, 52, 118, 255), outline=(50, 110, 210, 255))
    # Inner facets
    draw.polygon([(j_cx, j_cy - 14), (j_cx + 5, j_cy - 4), (j_cx - 5, j_cy - 4)],
                 fill=(40, 100, 195, 255))
    draw.polygon([(j_cx - 5, j_cy - 4), (j_cx + 5, j_cy - 4), (j_cx + 4, j_cy + 6), (j_cx - 4, j_cy + 6)],
                 fill=(28, 75, 160, 255))
    draw.polygon([(j_cx - 4, j_cy + 6), (j_cx + 4, j_cy + 6), (j_cx, j_cy + 14)],
                 fill=(12, 38, 90, 255))
    # Specular white reflection dot
    draw.ellipse([j_cx - 5, j_cy - 9, j_cx - 1, j_cy - 5], fill=(255, 255, 255, 240))

    # 6. Latin Imperial Motto Ribbon below Eagle: "SENATUS CALRADIAE"
    # Curved banner ribbon
    ribbon_y = ecy + 146
    draw.arc([cx - 130, ribbon_y - 20, cx + 130, ribbon_y + 35], start=20, end=160,
             fill=(225, 190, 80, 255), width=8)
    draw.arc([cx - 130, ribbon_y - 18, cx + 130, ribbon_y + 37], start=22, end=158,
             fill=(65, 45, 25, 255), width=4)

    # 7. Subtle radial vignette on edges
    vignette = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    vdraw = ImageDraw.Draw(vignette)
    vdraw.ellipse([cx - bezel_r, cy - bezel_r, cx + bezel_r, cy + bezel_r],
                  outline=(15, 10, 8, 70), width=6)
    img = Image.alpha_composite(img, vignette)

    OUTPUT_MASTER.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUTPUT_MASTER, "PNG", optimize=True)
    print(f"Master image generated: {OUTPUT_MASTER} ({size}x{size})")


if __name__ == "__main__":
    create_aquila_seal()

"""
Procedural generator for the Calradic CoALA Cognitive Mind Medallion (Rev093).
Synthesizes a master 512x512 PNG with alpha transparency, midnight imperial pine enamel bed,
sacred geometry cognitive synapse nodes, burnished gold contemplative scholar profile,
procedural logic gears, and an illuminated emerald/verdigris cabochon of insight.
"""
import math
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
OUTPUT_TEXTURE = ROOT / "src" / "CalradiaForge.Desktop" / "Resources" / "Textures" / "calradia-mind-medallion-rev093.png"
OUTPUT_ARTIFACT = ROOT / "artifacts" / "calradia-mind-medallion-rev093.png"


def create_mind_medallion():
    size = 512
    cx, cy = size / 2.0, size / 2.0
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # 1. Soft Drop Shadow (multi-pass for tactile depth)
    shadow_r = 236
    for offset_y, offset_x, alpha in [(6, 2, 40), (4, 1, 60), (2, 0, 90)]:
        draw.ellipse(
            [cx - shadow_r + offset_x, cy - shadow_r + offset_y,
             cx + shadow_r + offset_x, cy + shadow_r + offset_y],
            fill=(10, 12, 14, alpha)
        )

    # 2. Outer Golden Bezel & Rim
    bezel_r = 230
    draw.ellipse(
        [cx - bezel_r, cy - bezel_r, cx + bezel_r, cy + bezel_r],
        fill=(48, 38, 24, 255), outline=(197, 160, 89, 255), width=4
    )

    # Decorative Guilloche / Interlocking studs around rim (36 points)
    for i in range(36):
        deg = i * 10
        rad = math.radians(deg)
        r_stud = 220
        sx = cx + r_stud * math.cos(rad)
        sy = cy + r_stud * math.sin(rad)
        # Alternate between golden studs and bronze/verdigris rivets
        if i % 2 == 0:
            draw.ellipse([sx - 3.5, sy - 3.5, sx + 3.5, sy + 3.5], fill=(230, 195, 90, 255), outline=(130, 95, 30, 255))
            draw.ellipse([sx - 1.5, sy - 1.5, sx + 0.5, sy + 0.5], fill=(255, 240, 180, 255))
        else:
            draw.ellipse([sx - 2.5, sy - 2.5, sx + 2.5, sy + 2.5], fill=(58, 120, 102, 230), outline=(30, 70, 60, 255))

    # Inner Bezel Ring
    inner_bezel_r = 208
    draw.ellipse(
        [cx - inner_bezel_r, cy - inner_bezel_r, cx + inner_bezel_r, cy + inner_bezel_r],
        fill=(32, 26, 18, 255), outline=(175, 138, 70, 255), width=2
    )

    # 3. Deep Midnight Pine / Azure Enamel Bed (Concentric Gradient)
    bed_r = 198
    for r in range(bed_r, 0, -2):
        factor = r / bed_r
        # Outer edge deep pine #0A1C18 -> inner teal/pine #183C34
        r_col = int(10 + (28 - 10) * (1.0 - factor * 0.7))
        g_col = int(28 + (68 - 28) * (1.0 - factor * 0.7))
        b_col = int(24 + (58 - 24) * (1.0 - factor * 0.7))
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(r_col, g_col, b_col, 255))

    # 4. Sacred Cognitive Geometry & Neural Synapse Orbitals
    # Thin golden orbital rings
    for orb_r in [170, 132, 92]:
        draw.ellipse(
            [cx - orb_r, cy - orb_r, cx + orb_r, cy + orb_r],
            outline=(195, 160, 85, 95), width=1
        )

    # 12 Cognitive Constellation Nodes
    nodes = []
    node_angles = [15, 45, 75, 105, 135, 165, 195, 225, 255, 285, 315, 345]
    node_radii = [170, 132, 170, 132, 170, 132, 170, 132, 170, 132, 170, 132]
    for ang, rad_dist in zip(node_angles, node_radii):
        rad = math.radians(ang)
        nx = cx + rad_dist * math.cos(rad)
        ny = cy + rad_dist * math.sin(rad)
        nodes.append((nx, ny))

    # Interconnect neural nodes with delicate golden lines
    for i in range(len(nodes)):
        # Connect to next and skip-two
        n1 = nodes[i]
        n2 = nodes[(i + 1) % len(nodes)]
        n3 = nodes[(i + 3) % len(nodes)]
        draw.line([n1, n2], fill=(215, 180, 95, 110), width=1)
        draw.line([n1, n3], fill=(180, 145, 75, 70), width=1)

    # Draw nodes as glowing brass/gold discs with verdigris centers
    for nx, ny in nodes:
        draw.ellipse([nx - 4.5, ny - 4.5, nx + 4.5, ny + 4.5], fill=(225, 190, 85, 230), outline=(130, 95, 30, 255))
        draw.ellipse([nx - 2, ny - 2, nx + 2, ny + 2], fill=(58, 175, 145, 255))

    # 5. Procedural Logic Gear Teeth at Lower Base (Angles 40° to 140° bottom arc)
    gear_r = 112
    for deg in range(35, 146, 10):
        rad = math.radians(deg)
        gx = cx + gear_r * math.cos(rad)
        gy = cy + gear_r * math.sin(rad)
        # Draw gear tooth trapezoid pointing outward
        t_len = 8
        t_w = 4
        tip_x = gx + t_len * math.cos(rad)
        tip_y = gy + t_len * math.sin(rad)
        perp = rad + math.pi / 2
        draw.polygon([
            (gx - t_w * math.cos(perp), gy - t_w * math.sin(perp)),
            (tip_x - (t_w - 1.5) * math.cos(perp), tip_y - (t_w - 1.5) * math.sin(perp)),
            (tip_x + (t_w - 1.5) * math.cos(perp), tip_y + (t_w - 1.5) * math.sin(perp)),
            (gx + t_w * math.cos(perp), gy + t_w * math.sin(perp))
        ], fill=(210, 170, 75, 240), outline=(245, 215, 120, 255))

    # 6. Central Sculpted Profile of Contemplative Scholar / Mind
    # Classical Roman/Byzantine profile facing right
    # Head center slightly left of cx for balanced composition
    hcx = cx - 8
    hcy = cy - 6

    # Head & Neck Base Fill
    # Profile polygon: forehead, nose bridge, nose tip, upper lip, mouth, chin, jawline, neck, back of head
    profile_pts = [
        (hcx + 8, hcy - 68),   # Top of crown
        (hcx + 36, hcy - 50),  # Upper forehead
        (hcx + 44, hcy - 28),  # Brow ridge
        (hcx + 38, hcy - 22),  # Eye indent / bridge
        (hcx + 58, hcy - 6),   # Nose tip
        (hcx + 44, hcy + 2),   # Under nose
        (hcx + 50, hcy + 10),  # Upper lip
        (hcx + 42, hcy + 15),  # Lip cleft
        (hcx + 48, hcy + 22),  # Lower lip
        (hcx + 40, hcy + 28),  # Under lip
        (hcx + 50, hcy + 42),  # Chin point
        (hcx + 36, hcy + 55),  # Jaw angle
        (hcx + 32, hcy + 82),  # Front neck
        (hcx - 14, hcy + 86),  # Neck base
        (hcx - 38, hcy + 74),  # Back neck
        (hcx - 52, hcy + 42),  # Nape
        (hcx - 62, hcy + 10),  # Back of skull lower
        (hcx - 64, hcy - 25),  # Back of skull mid
        (hcx - 48, hcy - 55),  # Occiput
        (hcx - 20, hcy - 68)   # Crown back
    ]

    # Sculpted Shadow & Highlight passes
    # Base gold body
    draw.polygon(profile_pts, fill=(215, 175, 75, 255), outline=(245, 215, 120, 255), width=2)

    # Shading on back of head / occiput
    back_pts = [
        (hcx - 20, hcy - 68),
        (hcx - 48, hcy - 55),
        (hcx - 64, hcy - 25),
        (hcx - 62, hcy + 10),
        (hcx - 52, hcy + 42),
        (hcx - 38, hcy + 74),
        (hcx - 20, hcy + 50),
        (hcx - 25, hcy + 10),
        (hcx - 18, hcy - 30)
    ]
    draw.polygon(back_pts, fill=(165, 130, 50, 255))

    # Classical Laurel Wreath Crown over Head
    for li in range(7):
        lang = -155 + li * 22
        lrad = math.radians(lang)
        lx = hcx + 42 * math.cos(lrad)
        ly = hcy - 36 + 32 * math.sin(lrad)
        # Laurel leaf pointing along brow
        draw.ellipse([lx - 5, ly - 3, lx + 5, ly + 3], fill=(235, 205, 95, 255), outline=(130, 95, 30, 255))
        draw.ellipse([lx - 2, ly - 1, lx + 2, ly + 1], fill=(255, 240, 160, 255))

    # Eye and Brow detailing
    # Closed contemplative eye
    draw.line([(hcx + 22, hcy - 16), (hcx + 34, hcy - 14)], fill=(120, 85, 30, 255), width=2)
    draw.line([(hcx + 20, hcy - 22), (hcx + 36, hcy - 22)], fill=(150, 110, 40, 255), width=2)
    # Ear curl
    draw.arc([hcx - 12, hcy - 6, hcx + 4, hcy + 16], start=80, end=280, fill=(140, 100, 35, 255), width=2)

    # 7. Brow / Third Eye Illuminated Emerald Cabochon Jewel of Insight
    j_cx, j_cy = hcx + 20, hcy - 38
    # Gold bezel setting
    draw.ellipse([j_cx - 10, j_cy - 10, j_cx + 10, j_cy + 10], fill=(185, 145, 55, 255), outline=(245, 215, 110, 255), width=2)
    # Deep emerald/verdigris cabochon
    for jr in range(8, 0, -1):
        j_fact = jr / 8.0
        r_j = int(20 + (50 - 20) * (1.0 - j_fact))
        g_j = int(140 + (225 - 140) * (1.0 - j_fact))
        b_j = int(115 + (185 - 115) * (1.0 - j_fact))
        draw.ellipse([j_cx - jr, j_cy - jr, j_cx + jr, j_cy + jr], fill=(r_j, g_j, b_j, 255))
    # Specular white reflection dot
    draw.ellipse([j_cx - 4, j_cy - 5, j_cx - 1, j_cy - 2], fill=(255, 255, 255, 240))

    # 8. Subtle Latin Inscription Arch on Bottom Rim: "COGNITIO · MEMORIA"
    motto_y = cy + 172
    draw.arc([cx - 110, motto_y - 25, cx + 110, motto_y + 25], start=30, end=150,
             fill=(225, 190, 80, 255), width=5)
    draw.arc([cx - 110, motto_y - 23, cx + 110, motto_y + 27], start=32, end=148,
             fill=(55, 40, 22, 255), width=2)

    # 9. Color Bleed Dilation (2px) to prevent dark halos on alpha edges
    # Extract alpha channel
    r, g, b, a = img.split()
    # Create dilated color base
    # Max filter expands non-zero pixels
    r_dil = r.filter(ImageFilter.MaxFilter(5))
    g_dil = g.filter(ImageFilter.MaxFilter(5))
    b_dil = b.filter(ImageFilter.MaxFilter(5))
    # Recomposite with original sharp alpha
    img_clean = Image.merge("RGBA", (r_dil, g_dil, b_dil, a))
    # Paste original on top for sharp detail
    img_final = Image.alpha_composite(img_clean, img)

    # Save to targets
    OUTPUT_TEXTURE.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT_ARTIFACT.parent.mkdir(parents=True, exist_ok=True)
    img_final.save(OUTPUT_TEXTURE, "PNG", optimize=True)
    img_final.save(OUTPUT_ARTIFACT, "PNG", optimize=True)
    print(f"CoALA Cognitive Mind Medallion successfully synthesized:")
    print(f" -> {OUTPUT_TEXTURE}")
    print(f" -> {OUTPUT_ARTIFACT}")


if __name__ == "__main__":
    create_mind_medallion()

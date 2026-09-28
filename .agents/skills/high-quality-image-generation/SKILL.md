---
name: high-quality-image-generation
description: End-to-end high-fidelity image generation, visual prompt engineering, and game asset pipeline for Mount & Blade II Bannerlord and Calradia Forge. Covers in-game assets (weapons, armors, faction heraldry, icons, UI textures), promotional marketing art (Nexus Mods, Steam Workshop banners 1920x1080 / 4K), multi-model prompt architectures (Flux.1 Dev/Pro, Midjourney v6.1, DALL-E 3, Gemini generate_image), and automated Python image processing (alpha channel clipping, POT scaling, sprite atlas packing).
---

# High-Quality Image Generation & Visual Asset Pipeline

This skill provides an advanced visual direction framework, multi-model prompt engineering methodology, and automated post-processing pipeline for generating professional-grade imagery and game assets for **Mount & Blade II: Bannerlord** and the **Calradia Forge** ecosystem.

---

## 1. Core Principles & Visual Intent Architecture

Generating high-fidelity visual assets requires decomposing artistic intent into **9 Orthogonal Dimensions of Prompt Architecture**. Vague qualitative descriptors like *"photorealistic"* or *"4k"* produce generic, plastic AI artifacts; precise technical, material, and optical specifications yield studio-grade results.

### The 9 Dimensions of Visual Intent

| Dimension | Description & Focus | Calradia Forge / Bannerlord Calibration |
| :--- | :--- | :--- |
| **1. Primary Subject & Semantics** | Central entity, silhouette, posture, historical period grounding. | 11th–13th century Calradic feudal lore: Vlandian chivalric knights, Imperial kataphraktoi, Sturgian huscarls, Battanian fian archers, Aserai mamelukes, Khuzait horse archers. |
| **2. Materiality & Surface Micro-details** | Exact tactile textures, wear, corrosion, and weave density. | Folded Damascus crucible steel, beaten wrought iron with peened rivets, oiled boiled cuirboilli leather, riveted 4-in-1 chainmail links, hand-loomed wool with herringbone weave, silk brocade trims, weathered vellum. |
| **3. Illumination & Lighting Ratios** | Key/fill/rim light, color temperature, and atmospheric light scatter. | Chiaroscuro high-contrast lighting; warm candlelit medieval war-room (`2700K`); golden hour low-angle sunlight cutting through battlefield dust and mist; sharp directional rim lighting (`5600K`) separating subject from dark backdrop. |
| **4. Color Palette & Harmonization** | Palette harmony, saturation constraints, and tonal grading. | Muted earthy medieval pigments: lampblack, raw umber, vermilion lacquer, lapis lazuli ultramarine, weathered verdigris bronze, burnished gold leaf, aged parchment sepia. Avoid neon or over-saturated tones. |
| **5. Composition & Dynamic Framing** | Focal placement, aspect ratio, depth layering, leading lines. | Isometric or orthographic flat-lay for inventory icons; 16:9 or 21:9 golden-ratio rule-of-thirds for panoramic battle banners; centered square (`1:1`) for heraldic faction crests and portraits. |
| **6. Camera & Optical Physics** | Sensor format, lens focal length, aperture, depth of field. | Hasselblad H6D-100c medium format simulation; 85mm $f/1.4$ prime lens for portraits/weapons (subtle bokeh without blur); 24mm $f/8$ tilt-shift for sweeping siege panoramas (edge-to-edge sharpness). |
| **7. Atmospheric Context & Environmental Storytelling** | Micro-elements that ground the subject in a living world. | Airborne smoke plumes, embers, bloodied battlefield mud, etched heraldic battle damage, chipped pommel stones, frayed banner fringes, tactical war-map pins. |
| **8. Stylistic Dialect & Rendering Engine** | Engine and medium cues (oil on canvas, dark fantasy, digital render). | Historic oil painting (Rembrandt / Caravaggio lighting), dark gritty historical realism, concept art matte painting, or crisp vector-etched heraldic woodblock. |
| **9. Negative Prompting & Artifact Exclusion** | Systematic purging of common neural generative failure modes. | Exclude: plastic skin, smooth silicone textures, melted metal, modern typography, watermarks, deformed hands/fingers, symmetrical CG reflections, oversaturated bloom, blur. |

---

## 2. Multi-Model Dialect Matrix

Different image generation foundation models interpret prompting syntax differently. The table below outlines how to format prompts across the primary generative engines:

```
                               ┌─── Multi-Model Prompt Gateway ───┐
                               │                                  │
            ┌──────────────────┼─────────────────┬────────────────┴──────────────────┐
            ▼                  ▼                 ▼                                   ▼
     Flux.1 (Dev/Pro)   Midjourney v6.1     Gemini / Imagen 3                     DALL-E 3
    (Natural Prose &    (Descriptor Tags &   (generate_image Tool &               (Semantic Grounding &
     Exact Typography)   CLI Parameters)      Structured Prompting)                Strict Safety Filter)
```

### Model Specifications & Syntax Rules

#### A. Flux.1 (Dev / Schnell / Pro)
- **Strengths:** Superb photorealism, exact hand/finger anatomy, accurate text rendering in double quotes, realistic lighting physics.
- **Syntax:** Natural descriptive sentences. Avoid comma soup. Specify camera and lens details explicitly.
- **Example Formulation:**
  ```text
  A historical 12th-century Vlandian arming sword resting on an aged oak table, captured with an 85mm f/2.0 medium format lens. The blade features folded Damascus steel with subtle wavy rippling patterns and a razor-sharp peened edge. The crossguard is crafted from dark hand-forged wrought iron with brass inlay rivets. Warm directional candlelight illuminates the blade bevel from the upper-left, casting soft realistic contact shadows. In the background, an out-of-focus tactical vellum war map is dimly visible. Photorealistic, 8k resolution, crisp texture fidelity, no modern elements.
  ```

#### B. Midjourney (v6.1)
- **Strengths:** Cinematic lighting, artistic aesthetics, dramatic atmosphere, strong stylization.
- **Syntax:** Dense comma-separated descriptors, followed by CLI parameters (`--ar`, `--stylize`, `--v 6.1`).
- **Example Formulation:**
  ```text
  cinematic historical close-up portrait of an Imperial Kataphraktos commander in ornate cataphract scale armor, engraved blackened steel with brass filigree trim, heavy mail aventail, dramatic chiaroscuro Rembrandt lighting, warm candle glow, grim tactical expression, shot on 70mm anamorphic lens, shallow depth of field, photorealistic, 8k --ar 16:9 --style raw --v 6.1 --stylize 120
  ```

#### C. Antigravity Native `generate_image` (Gemini / Imagen 3)
- **Strengths:** Built directly into the Antigravity agentic runtime. Supports programmatic aspect ratios (`'1:1'`, `'16:9'`, `'4:3'`, `'3:2'`, `'9:16'`).
- **Syntax:** Direct, concise, high-density physical descriptions.
- **Invocation Pattern:**
  ```json
  {
    "Prompt": "Historical medieval arming sword isolated on dark neutral studio background, 45-degree angle, folded damascus steel blade with authentic wavy pattern, blackened iron crossguard, wire-wrapped leather grip, studio rim lighting, 85mm prime lens photography, tack sharp focus, 8k",
    "ImageName": "vlandian_arming_sword",
    "AspectRatio": "1:1",
    "toolAction": "Generating sword asset",
    "toolSummary": "Generate high quality weapon icon"
  }
  ```

#### D. DALL-E 3
- **Strengths:** Conceptual alignment, prompt adherence, crisp isolation.
- **Syntax:** Explicitly state: "A photo of..." or "A detailed digital painting of...". Specify exact placement to prevent the engine from adding unwanted decorative items.

---

## 3. Calradia Forge Game Asset Catalog & Archetypes

When generating assets for Mount & Blade II: Bannerlord and Calradia Forge, use these pre-calibrated prompt templates:

### Archetype 1: Weapons & Inventory Item Icons (512x512 $\rightarrow$ 128x128)
- **Usage:** Game item icons for `ModuleData/items.xml`, Gauntlet UI item tooltips, and inventory screens.
- **Framing:** Centered, 45-degree diagonal or horizontal orientation, neutral solid black or dark charcoal background (`#0D0F12`) to facilitate clean alpha channel extraction.
- **Formula:**
  `[Item Name], historical [Culture] design, [Material details: forged steel, gilded brass, oiled wood], isolated on solid neutral charcoal background (#101010), high-contrast rim lighting, studio product photography, 85mm macro lens, tack-sharp edge-to-edge focus, zero clutter, no text, no pedestal`

### Archetype 2: Faction Crests & Kingdom Heraldry (1024x1024 / 1:1)
- **Usage:** Settlement banners, kingdom diplomacy seals, Gauntlet UI kingdom views.
- **Framing:** Centered shield or roundel shape, clean graphic lines with rich medieval pigment textures.
- **Formula:**
  `Authentic medieval heraldic crest for the [Kingdom/Faction], featuring a [Heraldic charge: crowned lion, double-headed eagle, charging wolf] embossed on a [Field color: crimson, azure, emerald] heraldic escutcheon shield, weathered gold leaf and silver thread embroidery, authentic medieval woodblock and parchment etching aesthetic, crisp sharp contours, flat graphic isolation, no modern gradients`

### Archetype 3: UI Parchment & Tactical Cartography Textures (2048x2048 / Seamless)
- **Usage:** Background surfaces for Gauntlet XML prefabs and Desktop WPF tactical views (`War Table`, `Parchment Light`).
- **Framing:** Flat orthographic top-down scan (`0-degree tilt`), uniform diffuse lighting with zero directional shadows.
- **Formula:**
  `Seamless high-resolution texture of 13th-century aged calfskin vellum parchment, subtle natural animal skin pores, tea-stained sepia aging marks, authentic deckled fibers, uniform diffuse archival illumination, top-down flat-lay scan, zero directional shadows, high dynamic range surface detail, tileable texture map`

### Archetype 4: Promotional Banners & Steam/Nexus Workshop Art (1920x1080 / 3840x2160 / 16:9)
- **Usage:** Nexus Mods cover images, Steam Workshop showcase banners, in-game loading screens.
- **Framing:** Wide cinematic aspect ratio, dramatic rule-of-thirds composition, dark negative space in top or left thirds for logo/text overlay.
- **Formula:**
  `Wide cinematic panoramic shot of a grim medieval castle siege at dusk, massive stone battlements illuminated by flaming catapult trebuchet projectiles, armored warriors clashing on the ramparts, heavy smoke and atmospheric ash drifting across the scene, dramatic volumetric god rays, dark brooding negative space in the upper third for branding, shot on 35mm anamorphic cinema camera, Panavision lens, color graded in muted teal and fiery ember orange, 8k resolution, ultra-detailed epic fantasy matte painting`

---

## 4. Automated Python Post-Processing Pipeline (`tools/process_high_quality_asset.py`)

Raw AI-generated images require technical normalization before they can be ingested by the TaleWorlds engine or packaged into Gauntlet UI sprite sheets:
1. **Alpha Channel Extraction:** Neural generators produce opaque backgrounds. The pipeline isolates the subject, calculates color-distance or edge-aware luminance masks, and removes backgrounds cleanly.
2. **Power-of-Two (POT) Scaling:** TaleWorlds TPAC compilers and GPU DirectX texture units require textures sized to powers of two ($64 \times 64$, $128 \times 128$, $256 \times 256$, $512 \times 512$, $1024 \times 1024$, $2048 \times 2048$, $4096 \times 512$).
3. **Edge Bleed / Padding:** Linear texture filtering in Gauntlet causes dark edges if transparent pixels are black RGB. The pipeline dilates border pixel colors into the transparent zone (color bleeding) to eliminate dark halos.
4. **Metadata & Format Hygiene:** Converted to sRGB 8-bit RGBA PNG, stripping unneeded ICC profiles and writing a JSON forensic audit log with SHA-256 digests.

### Utility Command Syntax

```bash
# Process an inventory icon with automatic dark-background removal and 512x512 POT scaling
py -3.12 tools/process_high_quality_asset.py --input "artifacts/vlandian_sword.png" --output "modules/CalradiaForge/GUI/Assets/Icons/vlandian_sword.png" --mode icon --pot-size 512 --remove-bg --pad 2

# Process a promotional banner scaled to 1920x1080 with sRGB normalization
py -3.12 tools/process_high_quality_asset.py --input "artifacts/siege_banner.png" --output "docs/assets/nexus_mod_banner.png" --mode banner --width 1920 --height 1080

# Process a seamless parchment texture scaled to 2048x2048 POT
py -3.12 tools/process_high_quality_asset.py --input "artifacts/parchment_raw.png" --output "modules/CalradiaForge/GUI/Assets/Textures/parchment_bg.png" --mode texture --pot-size 2048
```

---

## 5. Step-by-Step Production Workflow

1. **Step 1: Define Intent & Select Archetype**
   - Identify whether the target is an in-game icon, heraldic crest, UI texture, or promo banner.
2. **Step 2: Engineer Prompt via 9 Dimensions**
   - Combine subject, materiality, lighting, camera, and negative prompts.
3. **Step 3: Generate Initial Image**
   - Use `generate_image` tool directly, or formulate prompts for Flux.1/Midjourney/DALL-E 3.
4. **Step 4: Execute Automated Post-Processing**
   - Run `tools/process_high_quality_asset.py` with appropriate `--mode` and `--pot-size`.
5. **Step 5: Integrate into Engine / Prefabs**
   - For UI sprites: append to `tools/generate_assets.py` to pack into `ui_calradiaforge_1.png` sprite atlas.
   - For items: reference in `ModuleData/items.xml` using `<Item mesh="mesh_name" ...>`.
   - For documentation: link in `docs/` and verify English/Spanish synchronization.

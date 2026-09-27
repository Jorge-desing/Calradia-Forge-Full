# Calradia Forge Desktop ImageGen assets

This manifest records the original ImageGen artwork and the compact local variants packaged by the WPF desktop application. Full-resolution copies remain under `src/CalradiaForge.Desktop/Resources/Textures`; the assembly embeds only `Resources/Textures/Optimized`. Gauntlet sprite sources and atlas preparation remain separate. The artwork adds no runtime dependency or network request.

## Safe use

- Put material textures only in theme-controlled surface brushes behind the UI. Keep text, evidence, inputs, buttons, selection and focus indicators on clean, sufficiently opaque surfaces. `High Contrast` remains a solid-color theme.
- Use transparent ornaments only in reserved frame, titlebar or navigation margins. They must not cover text or controls and should be non-interactive (`IsHitTestVisible="False"`) when decorative. Prefer low opacity and honor the user's decorative-accents toggle.
- The header compass is a brand mark on its own accessible command button, not an overlay on another control. Keep its automation name and tooltip localized.
- Keep the empty-ledger journal beside its empty-state copy, and hide it when the ledger has entries, when accents are disabled, or when the active theme does not permit decoration.
- Do not use any art as an evidence background, as a replacement for a state/focus cue, or as a way to imply that an operation succeeded.

## Full-resolution artwork inputs

SHA-256 values below identify the retained full-resolution local copies as of 2026-09-24. These files are authoring inputs and are not embedded in the WPF assembly. Dimensions, PNG color mode and alpha extrema were checked by decoding the complete images; alpha is `255..255` for opaque RGB images.

| Artwork input | ImageGen source master | Dimensions / mode / alpha | SHA-256 | Safe placement |
| --- | --- | --- | --- | --- |
| `war-table-illustrated-cloth.png` | `assets/gauntlet-imagegen/war_table_illustrated_cloth_v1.png` | 1254×1254, RGB, 255..255 | `99B79039DF750E7441C47C971040D258D006A4E8F27ED557309172CA7D47320A` | Low-contrast War Table surface material, behind content |
| `parchment-cartographic-paper.png` | `assets/gauntlet-imagegen/parchment_cartographic_paper_v1.png` | 1254×1254, RGB, 255..255 | `2B86FAA914103519A949399FBDECB92AD4073132C72B7E9E8C76BB30BD2DB80F` | Parchment Light surface and titlebar material, behind content |
| `parchment-field-journal-ornament-v1.png` | `C:/Users/Alex/.codex/generated_images/01a0d35e-2407-7e62-8804-96ea1c46bcb5/exec-f2121764-99e5-4783-a1b9-d94f3395e682.png` | 1254×1254, RGBA, 0..255 | `B6EBC6EECA36266E53B53DE9BB372C07A655DF946B42B9D570714E417B163AD3` | Passive Parchment Light titlebar mark; hidden in High Contrast and when accents are disabled |
| `titlebar-embroidered-cloth.png` | `assets/gauntlet-imagegen/titlebar_embroidered_cloth_v1.png` | 2048×768, RGB, 255..255 | `722B23CE06322F2E7E2746BDFA539678E65F245B4E385F76F639E69D2DA3B155` | War Table titlebar background; keep the caption area readable |
| `titlebar-heraldic-corner.png` | `assets/gauntlet-imagegen/titlebar_heraldic_compass_v1.png` | 1254×1254, RGBA, 0..255 | `BEF2AAE01BD0D95B10D8E87884D1E4E7B8110B25A5D4A2045BD984569F44A0BC` | Small isolated titlebar corner mark, clear of title and caption buttons |
| `header-heraldic-compass-v2.png` | `C:/Users/Alex/.codex/generated_images/01a0729a-9f57-7173-9f44-d03d3737b50f/exec-dff31f7b-dc18-41c2-9e98-14bcbe2939dd.png` | 1254×1254, RGBA, 0..255 | `29D8F76FF79367405BE95E5CA1468190AC01E484A8B5F172ACC59D03EB9F1836` | Standalone header seal inside its accessible command button |
| `header-corner-engraving-v1.png` | `C:/Users/Alex/.codex/generated_images/01a0729a-9f57-7173-9f44-d03d3737b50f/exec-00d1814a-8d25-4297-94c4-414ca04dba56.png` | 2172×724, RGBA, 0..255 | `D61A91D9E9B0186E464E6CD451CACDD4F3EEB5239C3E1798048594495A9550CF` | Low-opacity header corner frame behind empty margins only |
| `tool-card-top-corners-v1.png` | `C:/Users/Alex/.codex/generated_images/01a0729a-9f57-7173-9f44-d03d3737b50f/exec-078fe291-3ca6-4c40-a9cf-8fbc4fa5564a.png` | 2172×724, RGBA, 0..255 | `4801E3E10269A7D49E358891D673039EC3BECAE6095F92EAA10DA092493A3F74` | Tool-card title frame, confined to the top border and clear of text |
| `evidence-ledger-empty-v1.png` | `C:/Users/Alex/.codex/generated_images/01a0729a-9f57-7173-9f44-d03d3737b50f/exec-5f692a76-c349-4d03-ab53-8cf8deb29e73.png` | 1254×1254, RGBA, 0..255 | `69C6CA93CEFCE9615D9F73D924236F042668909D64564DB39F9AABF83B9F5B2C` | Small adjacent empty-ledger vignette, never behind evidence text |
| `titlebar-botanical-band-v1.png` | `C:/Users/Alex/.codex/generated_images/01a0729a-9f57-7173-9f44-d03d3737b50f/exec-3f4da677-ae87-4bf8-b95a-b3a132f9eb60.png` | 2172×724, RGBA, 0..255 | `1807D0EB845FAFFD0F60F0422F7C774DC1E3BEF7C832B12CAABFC4E960E098A3` | Thin decorative title/header strip only where it cannot cross the title or caption buttons |
| `rail-field-compass-v1.png` | `C:/Users/Alex/.codex/generated_images/01a0729a-9f57-7173-9f44-d03d3737b50f/exec-5ead803b-a9c9-427b-9063-9051ac89a412.png` | 1254×1254, RGBA, 0..255 | `89EFE3EF9E836484EBA2EA13D9C9F02A776055DA5191B2577AC39B2A34E0AC43` | Isolated 24–32 DIP rail-heading ornament, outside the text and navigation hit targets |

## Packaged WPF variants

`tools/Prepare-CalradiaForge-Desktop-Textures.bat` generates these deterministic derivatives with Pillow at authoring time. Run it without arguments to refresh the local package files, or with `--check` to regenerate in memory and compare without writing. Pillow is not a build or runtime dependency. The original files stay unchanged as readable source inputs.

The first optimized package measured 6,901,545 bytes compressed and 11,489,464 decoded image bytes, down from 17,864,675 compressed and 64,474,344 decoded bytes for the full-size copies (about 61% less packaged data and 82% fewer decoded bytes). Small decorations are sized for their maximum reviewed 200% layout; long title-bar art is cropped to the exact visible bands. These figures describe the texture set, not total process memory or measured app startup time.

| Packaged variant | Dimensions / mode | SHA-256 |
| --- | --- | --- |
| `war-table-illustrated-cloth.png` | 1254×1254 RGB | `AD2F33639BC469C800BE268120C9EDF2A950B07740180DDB4B4938C640DFDAFF` |
| `parchment-cartographic-paper.png` | 1254×1254 RGB | `8B5629133E4F38BD21328E97A93EFFA24429B291773B1EF849AA59C1AAE765C1` |
| `titlebar-embroidered-cloth.png` | 2048×62 RGB | `13C3CD49E570BB888D8A480944E5FCB262E040F81A08D17CC28532C09589544C` |
| `titlebar-botanical-band-v1.png` | 2172×98 RGBA | `6FACCBB7CCAF9D9687F408AA9C5B3C20F78933D7F4E91C2B20F0AE528B4E9A84` |
| `titlebar-heraldic-corner.png` | 96×96 RGBA | `ED48407BFEB10FE2F441D826F369AD620FF5BBAB117756CA6F5105905AADB651` |
| `header-heraldic-compass-v2.png` | 128×128 RGBA | `F9EED4CB1A0907F5CEEDA03054B013420E1C29EAA81881864CAE1FA1B4790205` |
| `header-corner-engraving-v1.png` | 192×64 RGBA | `6B1E4BF7B30AB6F89C1B871E6AF95F27287CB87014BD59489D989BE31D74860D` |
| `tool-card-top-corners-v1.png` | 648×216 RGBA | `25C431CE427807821B1B8D55A9E8601332B5A48910C21FFD8E4CCE1727D1D353` |
| `evidence-ledger-empty-v1.png` | 96×96 RGBA | `1A3A8B6FBB6CCE7CAB2E1EED285AAED2C8481D1AA0D4ADAA61476319DD61DE08` |
| `rail-field-compass-v1.png` | 96×96 RGBA | `A5E1F4032BAFA603070980BC8A3355A01FA3D5543F5E7157838548D12CCA588B` |
| `parchment-field-journal-ornament-v1.png` | 96×96 RGBA | `57CBC28BA0E895A01CF2B9425911572CDDE582207A78D74EDF3649B35F6A14C1` |

## Recorded generation prompts

### War Table cloth

> Use case: stylized-concept. Asset type: original local background material texture for a native tactical desktop/game UI. Create a seamless square swatch of deep charcoal and pine-green field cloth, subtly aged and hand-woven, with natural uneven fibers and faint dark ink traces; distinctly tactile and illustrated, not geometric. Painterly material study, refined historical field-journal craft, restrained detail. Seamless tile on all four edges; uniform material value, no focal object, no border; calm enough to sit behind small UI labels. Diffuse matte low-contrast lighting. Charcoal, deep pine, restrained muted brass/verdigris threads. Fine woven linen, lightly worn dark cloth, ink-wash fibers. No text, logos, watermark, symbols, repeated geometric pattern, large objects, or uneven luminance that would impair label legibility.

### Parchment cartographic paper

> Use case: stylized-concept. Asset type: original local surface texture for the Parchment Light theme of a native desktop UI. Create a seamless square swatch of warm archival parchment with fine organic paper fibers and extremely faint hand-drawn cartographic contour marks fading near the outer edge; open quiet center. Illustrated historical mapmaker's paper surface, elegant and tactile, not a mockup. Seamless on all four edges, evenly lit, no sheet outline, no border, no central illustration; faint details only at the perimeter. Soft warm daylight. Warm ivory, old parchment, sparse muted sepia, a very small hint of antique brass. Fine natural paper grain, tiny fiber flecks, subtle age variation. No text, labels, logos, watermark, compass rose, geometric pattern, strong stains, or dark marks behind body text.

### Parchment field-journal ornament v1

> Use case: stylized-concept  
> Asset type: Small decorative WPF UI ornament for Calradia Forge Desktop, Parchment Light theme  
> Primary request: Create an original, refined botanical field-journal corner ornament / tiny mapmaker medallion for a light parchment surface.  
> Scene/backdrop: Isolated ornamental object on a genuinely transparent background; no scene.  
> Subject: A compact antique-brass compass medallion entwined with a delicate olive/field sprig and a small inked parchment curl, suggesting a medieval cartographer's journal.  
> Style/medium: High-quality hand-illustrated engraving with restrained etched linework and a few soft watercolor accents, organic and tactile rather than geometric.  
> Composition/framing: Square composition, centered, clear silhouette and bold enough details to remain legible when displayed at only 24–40 DIP; generous true-alpha transparent margins; no rectangular panel or backing.  
> Lighting/mood: Warm, calm, crafted, unobtrusive.  
> Color palette: Warm antique brass, muted olive, parchment sepia, tiny deep-ink accents; sufficient contrast against pale parchment.  
> Materials/textures: Finely engraved brass, softly aged paper, delicate botanical ink lines.  
> Constraints: True RGBA transparency around the ornament. Keep the medallion compact and readable at small UI size. Intended only for reserved margins, passive and non-hit-test; hidden for High Contrast and when decorative accents are disabled.  
> Avoid: Text, letters, numbers, logos, watermark, background, card/panel, hard border, geometric tessellation, bright glow, drop shadow, excessive micro-detail, busy scenery.

### WPF titlebar embroidered cloth

> Use case: stylized-concept. Asset type: original wide background texture for the custom WPF title bar of a tactical developer desktop app. Create a dark, illustrated field-journal material panel combining very fine pine-green woven cloth and dark stained leather, with faint hand-etched map contour impressions and sparse antique-brass thread work only at the extreme top and bottom edges. Refined historical craft illustration, organic material rendering, tactile but clean, consistent with an understated heraldic war-table interface. Panoramic horizontal banner, broad uninterrupted quiet center and right side for a window title and three caption buttons; edge accents remain faint and subordinate; no vignette or isolated centerpiece. Matte, subdued, even, readable. Charcoal black, deep pine, muted brass, a trace of verdigris. Soft leather grain, fine woven cloth, inked mapmaking marks; no visible rigid repeating pattern. No text, letters, logo, watermark, icons, objects, buttons, panel mockup, bright focal highlights, or decorative marks beneath the caption controls.

### WPF titlebar heraldic compass

> Use case: stylized-concept. Asset type: transparent decorative corner medallion for the left end of a custom WPF title bar. Create a compact hand-engraved brass compass medallion wrapped by two small oak leaves and a restrained Calradia-inspired heraldic knot, designed as a UI ornament rather than a functional icon. Delicate illustrated pen-and-ink engraving with subtle aged brass and pine-green enamel, refined historical field-journal craft. Near-square centered motif, thin crisp contours, transparent empty margin, simple enough to read at 24–32 pixels high; no full frame or ribbon. Restrained low-gloss highlights. Antique brass, pale muted gold, deep pine accents, a tiny verdigris detail. Lightly engraved metal with organic leaf veins. Genuine transparent background, no colored backdrop, text, logo, watermark, geometric tessellation, gradients that create a rectangular tile, or shapes resembling a window-control glyph.

### WPF header heraldic compass v2

> Use case: original application brand emblem for a native Windows desktop developer workbench, displayed at only 56–66 pixels square. Create one centered, compact, hand-engraved heraldic compass shield: a crisp eight-point star nested in a small round compass medallion, framed by two restrained oak leaves, with a tiny deep-pine enamel inset. The silhouette must remain instantly readable at small size; simplified outer contour, strong separated shapes, fine detail only inside the medallion. Refined illustrated medieval field-journal craft, not photorealistic, original Calradia-inspired identity without copying any existing logo. Balanced, symmetrical, premium and calm. Transparent background with generous clear margin; design alone, no tile or mockup. Antique brass and muted pale gold linework, deep pine green enamel, a trace of verdigris; dark outline for contrast on both dark charcoal and warm parchment UI. Clean edges suitable for a PNG placed in a 64-DIP WPF Image control. No text, letters, initials, wordmark, banner, watermark, rectangular background, geometric tessellation, gradients outside the emblem, or tiny illegible detail.

### WPF header corner engraving v1

> Use case: transparent passive decorative overlay for the top content header of a desktop tactical workbench UI, displayed as a wide, shallow image behind controls. Create a restrained original pair of hand-engraved historical field-journal corner ornaments connected by only a very thin bottom rule: delicate oak-leaf curls, fine mapmaker's linework, and a few antique-brass engraved strokes confined to the far left and far right edges. Leave the full center and upper middle completely open and transparent for title text, badges, selectors, and session controls. Horizontal panoramic layout, transparent background, low visual weight, graceful and asymmetrical natural leaf details balanced left/right. Designed to crop and remain subtle at roughly 20 percent opacity. Antique brass, muted pine and tiny verdigris accents, outlined not filled. Delicate engraved strokes with slight hand-drawn irregularity. No large central emblem, no enclosing solid frame, no text, no logo, no buttons, no objects behind controls, no texture tile, no geometric pattern, no watermark. Preserve true transparency.

### WPF ToolPage title-card top corners v1

> Use case: original transparent passive corner decoration for the title section of a native Windows desktop tool card. Create a very wide, shallow pair of hand-engraved medieval field-journal corner ornaments: restrained oak-leaf and fine brass scrollwork at the extreme upper-left and upper-right, connected by only a hairline antique-brass rule near the top edge. Keep the entire center and lower 80 percent fully transparent for title, status, inputs, commands, and evidence. Designed to sit behind only a title-card header, not as a full frame; low visual weight, graceful organic curves, delicate ink engraving, consistent with charcoal, deep pine, parchment, antique brass and verdigris UI. True transparent background, transparent empty center, clean small-scale linework, suitable for WPF Image overlay with IsHitTestVisible false at 15 percent opacity. No text, letters, logo, central emblem, buttons, objects, panel mockup, opaque backdrop, geometric pattern, heavy border, watermark, or marks crossing the clear center.

### WPF evidence-ledger empty-state illustration

> Use case: original transparent empty-state illustration for the Evidence Ledger panel of a tactical Windows desktop workbench. Create a compact, warmly illustrated open field journal with one small loose evidence scroll, a fine quill laid diagonally beside it, and a tiny compass medallion tucked near the lower corner. The composition should be calm, inviting, and readable at about 56 pixels square; simple strong silhouette with a few delicate engraved details, no tiny illegible writing. Historical field-note craft in Calradia-inspired palette: aged parchment, muted antique brass, deep pine-green leather, dark ink, restrained verdigris. Front-facing isolated vignette with generous transparent margin, no ground shadow or background. Designed to sit beside localized empty-ledger text at low opacity; no border or frame. True transparent PNG appearance. No text, letters, logos, watermark, banner, extra symbols, complex scene, hard rectangular shapes, or colored backdrop.

### WPF titlebar botanical band v1

> Create an original ornamental material asset for a refined medieval tactical Windows desktop workbench. Extremely wide horizontal 3:1 transparent PNG, designed to be layered at low opacity along the top edge of a WPF title/header region. A dark pine-green woven cloth ribbon with a restrained, fine antique-brass botanical vine and tiny heraldic compass fleuron at the center; small leaf curls near both ends; a thin engraved brass hairline under the cloth. Detailed hand-illustrated craft, tasteful and welcoming, sophisticated not fantasy-game noisy. Keep all decoration inside the horizontal strip and leave broad transparent margins above and below; edges fade softly into true alpha transparency. Works over charcoal, pine green and parchment themes; no filled rectangular background, no hard box or border, no text, letters, numbers, logos, watermark, gradients that obscure content, bright white, glow, or clutter.

### WPF rail field compass v1

> Create an original compact transparent PNG ornament for the top-right corner of a tactical desktop application's left navigation rail. Square 1:1 asset, isolated heraldic field compass: antique brass needle and engraved circular compass rose intertwined with one small pine sprig and a tiny verdigris enamel detail, highly legible silhouette, elegant hand-illustrated metal-and-leather craft. Calradia-inspired but no known franchise marks. Designed to render at 24–32 pixels beside a section heading, so prioritize clean silhouette and a few bold engraved lines. True alpha transparency around the object with generous transparent margin. No text, letters, numbers, logos, watermark, backdrop, rectangular panel, glow, drop shadow, busy scenery, or tiny illegible details.

## Rechecking packaged PNGs

From the repository root, the following PowerShell snippet checks the **packaged derivatives** for their file hash, dimensions, PNG mode and complete alpha range with Pillow installed. The unoptimized files beside this folder are retained authoring masters, not WPF runtime resources:

```powershell
python -c @'
from pathlib import Path
from PIL import Image
import hashlib
p=Path('src/CalradiaForge.Desktop/Resources/Textures/Optimized')
for f in sorted(p.glob('*.png')):
    im=Image.open(f)
    alpha=im.getchannel('A').getextrema() if 'A' in im.getbands() else (255,255)
    print('{}|{}x{}|{}|alpha {}..{}|{}'.format(f.name,im.width,im.height,im.mode,alpha[0],alpha[1],hashlib.sha256(f.read_bytes()).hexdigest().upper()))
'@
```

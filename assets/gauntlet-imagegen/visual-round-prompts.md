# Visual-round ImageGen source assets

Generated with the built-in ImageGen tool for the Calradia Forge 22.0.0 visual round on 2026-09-24. The generated originals are retained here as source masters; Desktop and Gauntlet consume separately packaged/prepared copies. No network fetch or runtime image service is used.

| Source master | Dimensions / alpha | SHA-256 | Intended use |
| --- | --- | --- | --- |
| `war_table_illustrated_cloth_v1.png` | 1254×1254, RGB | `99B79039DF750E7441C47C971040D258D006A4E8F27ED557309172CA7D47320A` | War Table texture and Gauntlet surface sprite |
| `parchment_cartographic_paper_v1.png` | 1254×1254, RGB | `2B86FAA914103519A949399FBDECB92AD4073132C72B7E9E8C76BB30BD2DB80F` | Parchment Light texture |
| `heraldic_field_journal_overlay_v1.png` | 2172×724, RGBA with transparency | `70E619A85CB15BB87BA8E87EE67BFEAEF28FB2F458C5559DA8431E4B71189E20` | Passive heraldic Gauntlet header ornament |
| `titlebar_embroidered_cloth_v1.png` | 2048×768, RGB | `722B23CE06322F2E7E2746BDFA539678E65F245B4E385F76F639E69D2DA3B155` | Textured WPF caption surface |
| `titlebar_heraldic_compass_v1.png` | 1254×1254, RGBA; sampled alpha 0–255 | `BEF2AAE01BD0D95B10D8E87884D1E4E7B8110B25A5D4A2045BD984569F44A0BC` | Transparent WPF titlebar medallion |
| `header_heraldic_compass_v2.png` | 1254×1254, RGBA, alpha 0–255 | `29D8F76FF79367405BE95E5CA1468190AC01E484A8B5F172ACC59D03EB9F1836` | New WPF header mark; intended for 56–66 DIP and clickable command-palette access |
| `header_corner_engraving_v1.png` | 2172×724, RGBA, alpha 0–255 | `D61A91D9E9B0186E464E6CD451CACDD4F3EEB5239C3E1798048594495A9550CF` | Passive WPF header-corner overlay; keep center open |
| `tool_card_top_corners_v1.png` | 2172×724, RGBA, alpha 0–255 | `4801E3E10269A7D49E358891D673039EC3BECAE6095F92EAA10DA092493A3F74` | Passive low-opacity ToolPage title-card top corners |

## Prompts

**War Table cloth**

> Use case: stylized-concept. Asset type: original local background material texture for a native tactical desktop/game UI. Create a seamless square swatch of deep charcoal and pine-green field cloth, subtly aged and hand-woven, with natural uneven fibers and faint dark ink traces; distinctly tactile and illustrated, not geometric. Painterly material study, refined historical field-journal craft, restrained detail. Seamless tile on all four edges; uniform material value, no focal object, no border; calm enough to sit behind small UI labels. Diffuse matte low-contrast lighting. Charcoal, deep pine, restrained muted brass/verdigris threads. Fine woven linen, lightly worn dark cloth, ink-wash fibers. No text, logos, watermark, symbols, repeated geometric pattern, large objects, or uneven luminance that would impair label legibility.

**Parchment cartographic paper**

> Use case: stylized-concept. Asset type: original local surface texture for the Parchment Light theme of a native desktop UI. Create a seamless square swatch of warm archival parchment with fine organic paper fibers and extremely faint hand-drawn cartographic contour marks fading near the outer edge; open quiet center. Illustrated historical mapmaker's paper surface, elegant and tactile, not a mockup. Seamless on all four edges, evenly lit, no sheet outline, no border, no central illustration; faint details only at the perimeter. Soft warm daylight. Warm ivory, old parchment, sparse muted sepia, a very small hint of antique brass. Fine natural paper grain, tiny fiber flecks, subtle age variation. No text, labels, logos, watermark, compass rose, geometric pattern, strong stains, or dark marks behind body text.

**Heraldic field-journal overlay**

> Use case: stylized-concept. Asset type: transparent passive decorative overlay for a native tactical UI header and rail. Create delicate original hand-engraved heraldic field-journal ornament: a small balanced leaf-and-compass cartouche with thin cartographic linework and subtle brass filigree extending outward into short, sparse corner flourishes. Refined historical pen-and-ink engraving, organic, elegant, restrained; match a tactical charcoal-and-pine interface. Wide horizontal transparent overlay, open center and generous negative space for UI text; visual weight confined to the extreme left/right margins and a thin lower rule; fine isolated ornaments rather than a full frame. Flat understated linework in muted antique brass with tiny verdigris accents. Delicate engraved strokes with slight hand-drawn irregularity. Genuine transparent background, no white or colored backdrop, text, logo, watermark, buttons, geometric tessellation, large central emblem, opaque panel, or element that would obscure text or controls.

**WPF titlebar embroidered cloth**

> Use case: stylized-concept. Asset type: original wide background texture for the custom WPF title bar of a tactical developer desktop app. Create a dark, illustrated field-journal material panel combining very fine pine-green woven cloth and dark stained leather, with faint hand-etched map contour impressions and sparse antique-brass thread work only at the extreme top and bottom edges. Refined historical craft illustration, organic material rendering, tactile but clean, consistent with an understated heraldic war-table interface. Panoramic horizontal banner, broad uninterrupted quiet center and right side for a window title and three caption buttons; edge accents remain faint and subordinate; no vignette or isolated centerpiece. Matte, subdued, even, readable. Charcoal black, deep pine, muted brass, a trace of verdigris. Soft leather grain, fine woven cloth, inked mapmaking marks; no visible rigid repeating pattern. No text, letters, logo, watermark, icons, objects, buttons, panel mockup, bright focal highlights, or decorative marks beneath the caption controls.

**WPF titlebar heraldic compass**

> Use case: stylized-concept. Asset type: transparent decorative corner medallion for the left end of a custom WPF title bar. Create a compact hand-engraved brass compass medallion wrapped by two small oak leaves and a restrained Calradia-inspired heraldic knot, designed as a UI ornament rather than a functional icon. Delicate illustrated pen-and-ink engraving with subtle aged brass and pine-green enamel, refined historical field-journal craft. Near-square centered motif, thin crisp contours, transparent empty margin, simple enough to read at 24–32 pixels high; no full frame or ribbon. Restrained low-gloss highlights. Antique brass, pale muted gold, deep pine accents, a tiny verdigris detail. Lightly engraved metal with organic leaf veins. Genuine transparent background, no colored backdrop, text, logo, watermark, geometric tessellation, gradients that create a rectangular tile, or shapes resembling a window-control glyph.

**WPF header heraldic compass v2**

> Use case: original application brand emblem for a native Windows desktop developer workbench, displayed at only 56–66 pixels square. Create one centered, compact, hand-engraved heraldic compass shield: a crisp eight-point star nested in a small round compass medallion, framed by two restrained oak leaves, with a tiny deep-pine enamel inset. The silhouette must remain instantly readable at small size; simplified outer contour, strong separated shapes, fine detail only inside the medallion. Refined illustrated medieval field-journal craft, not photorealistic, original Calradia-inspired identity without copying any existing logo. Balanced, symmetrical, premium and calm. Transparent background with generous clear margin; design alone, no tile or mockup. Antique brass and muted pale gold linework, deep pine green enamel, a trace of verdigris; dark outline for contrast on both dark charcoal and warm parchment UI. Clean edges suitable for a PNG placed in a 64-DIP WPF Image control. No text, letters, initials, wordmark, banner, watermark, rectangular background, geometric tessellation, gradients outside the emblem, or tiny illegible detail.

**WPF header corner engraving v1**

> Use case: transparent passive decorative overlay for the top content header of a desktop tactical workbench UI, displayed as a wide, shallow image behind controls. Create a restrained original pair of hand-engraved historical field-journal corner ornaments connected by only a very thin bottom rule: delicate oak-leaf curls, fine mapmaker's linework, and a few antique-brass engraved strokes confined to the far left and far right edges. Leave the full center and upper middle completely open and transparent for title text, badges, selectors, and session controls. Horizontal panoramic layout, transparent background, low visual weight, graceful and asymmetrical natural leaf details balanced left/right. Designed to crop and remain subtle at roughly 20 percent opacity. Antique brass, muted pine and tiny verdigris accents, outlined not filled. No large central emblem, no enclosing solid frame, no text, no logo, no buttons, no objects behind controls, no texture tile, no geometric pattern, no watermark. Preserve true transparency.

**WPF ToolPage title-card top corners v1**

> Use case: original transparent passive corner decoration for the title section of a native Windows desktop tool card. Create a very wide, shallow pair of hand-engraved medieval field-journal corner ornaments: restrained oak-leaf and fine brass scrollwork at the extreme upper-left and upper-right, connected by only a hairline antique-brass rule near the top edge. Keep the entire center and lower 80 percent fully transparent for title, status, inputs, commands, and evidence. Designed to sit behind only a title-card header, not as a full frame; low visual weight, graceful organic curves, delicate ink engraving, consistent with charcoal, deep pine, parchment, antique brass and verdigris UI. True transparent background, transparent empty center, clean small-scale linework, suitable for WPF Image overlay with IsHitTestVisible false at 15 percent opacity. No text, letters, logo, central emblem, buttons, objects, panel mockup, opaque backdrop, geometric pattern, heavy border, watermark, or marks crossing the clear center.

## WPF evidence-ledger empty state — appended 2026-09-24

| Source master | Dimensions / alpha | SHA-256 | Packaged resource | Use |
| --- | --- | --- | --- | --- |
| `exec-5f692a76-c349-4d03-ab53-8cf8deb29e73.png` (`C:/Users/Alex/.codex/generated_images/01a0729a-9f57-7173-9f44-d03d3737b50f/exec-5f692a76-c349-4d03-ab53-8cf8deb29e73.png`) | 1254×1254, RGBA, alpha 0–255 | `69C6CA93CEFCE9615D9F73D924236F042668909D64564DB39F9AABF83B9F5B2C` | `src/CalradiaForge.Desktop/Resources/Textures/evidence-ledger-empty-v1.png` | Passive image shown only when the evidence ledger is empty; hidden in High Contrast and with decorative accents disabled |

**WPF evidence-ledger empty-state illustration**

> Use case: original transparent empty-state illustration for the Evidence Ledger panel of a tactical Windows desktop workbench. Create a compact, warmly illustrated open field journal with one small loose evidence scroll, a fine quill laid diagonally beside it, and a tiny compass medallion tucked near the lower corner. The composition should be calm, inviting, and readable at about 56 pixels square; simple strong silhouette with a few delicate engraved details, no tiny illegible writing. Historical field-note craft in Calradia-inspired palette: aged parchment, muted antique brass, deep pine-green leather, dark ink, restrained verdigris. Front-facing isolated vignette with generous transparent margin, no ground shadow or background. Designed to sit beside localized empty-ledger text at low opacity; no border or frame. True transparent PNG appearance. No text, letters, logos, watermark, banner, extra symbols, complex scene, hard rectangular shapes, or colored backdrop.

## Gauntlet illustrated refresh — 2026-09-28

The original generation prompts for these seven masters were not preserved verbatim in the project text. The following are faithful visual specifications reconstructed from the checked-in PNGs; they document the intended subjects and composition without claiming to be exact prompt transcripts. The three selected source masters are intended for Gauntlet preparation; the four alternatives are provenance-only candidates and are not runtime resources.

### Selected source masters

**`forge_war_table_cloth_v3_master.png` — panoramic header cloth; 2172×724, RGB, opaque**

> Use case: illustrated material master for the Gauntlet header. Create a wide, shallow dark pine-green and charcoal field-cloth surface with fine woven fibers and subdued aged texture. Keep the broad central field calm and open for interface text. Place faint hand-drawn coastlines and map contours toward the outer areas, with partial antique-brass compass/astrolabe details cropped near the upper-right and lower-left corners. Use soft, low-contrast light, muted olive-gold and verdigris accents, and an organic historical map-table character. No text, lettering, invented logo, watermark, hard geometric pattern, or high-contrast mark through the center.

**`forge_heraldic_header_v3_master.png` — header overlay; 2048×768, RGBA, alpha 0–254**

> Use case: wide transparent passive ornament over the Gauntlet header. Place matching engraved compass medallions and restrained oak-leaf flourishes at the far left and right, joined by a fine antique-brass lower rule. Leave the central title area mostly open and transparent. Render crisp but subdued metal engraving in muted brass, pine enamel, and small verdigris accents, with a faint atmospheric edge glow. No words, labels, invented identity, watermark, opaque panel, or control-like shapes.

**`forge_heraldic_rail_v4_master.png` — navigation rail field; 887×1774, RGB, opaque**

> Use case: tall, narrow illustrated material for a Gauntlet navigation rail. Use a dark charcoal/pine woven cloth field with a quiet center, subtle low-contrast cartographic contours, and slim antique-brass rails along both vertical edges. Repeat restrained oak-leaf and acorn engravings symmetrically along those edges while keeping the text-bearing center dark and uncluttered. Flat frontal composition with even subdued illumination. No transparency, large central crest, text, invented logo, watermark, or high-contrast detail behind labels.

### Alternate masters retained for provenance; not runtime

**`forge_heraldic_header_v2_master.png` — centered-shield header alternative; 1855×848, RGBA, alpha 0–255**

> Wide transparent header ornament formed by a thin horizontal brass line, curled oak-leaf terminals, small metal studs, and a centered blank pine-green shield cartouche. Keep the rest transparent; use engraved antique brass and deep-pine enamel. No text, lettering, logo, or watermark.

**`forge_heraldic_rail_v2_master.png` — transparent paired-rail alternative; 887×1774, RGBA, alpha 0–255**

> Tall transparent frame made from two ornate vertical oak-and-acorn side rails. Add slim brass uprights, restrained leaf scrollwork, and small shield cartouches toward the lower portion; leave the center transparent for navigation content. No text, lettering, logo, backdrop, or watermark.

**`forge_heraldic_rail_v3_master.png` — opaque map-cloth rail alternative; 1024×1536, RGB, opaque**

> Tall woven pine-green and charcoal material with subtle gold cartographic coastlines around a calm center, slim vertical border lines, and larger heraldic bird/leaf flourishes cropped toward the outer upper edges. Keep texture low contrast and the text area legible. No labels, text, invented logo, watermark, or repeating geometric pattern.

**`forge_heraldic_frame_v2_master.png` — transparent corner-frame alternative; 2172×724, RGBA, alpha 0–255**

> Wide transparent frame alternative with oversized engraved compass rosettes and oak-leaf scrollwork confined to the far left and right corners. Join the corners with a fine lower brass rule and leave the middle open for content. Use aged brass, dark pine enamel, and restrained verdigris. No text, invented logo, watermark, opaque background, or details crossing the central text area.

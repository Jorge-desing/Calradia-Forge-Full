"""Rebuild font-independent WPF paths and per-icon Game-icons.net attribution."""
import json
import subprocess
import tempfile
from pathlib import Path
from xml.etree import ElementTree

root = Path(__file__).resolve().parents[1] / "src" / "CalradiaForge.Desktop" / "Resources" / "GameIcons"
output = root.parent / "GameIcons.xaml"
workspace = root.parents[3]
native_root = workspace / "modules" / "CalradiaForge" / "GUI" / "SpriteParts"
native_root.mkdir(parents=True, exist_ok=True)
native_source_root = workspace / "modules" / "CalradiaForge" / "GUI" / "IconSources" / "GameIcons"
svg_namespace = "{http://www.w3.org/2000/svg}"
credits = {
    "archery-target": ("Lorc", "https://game-icons.net/1x1/lorc/archery-target.html"),
    "compass": ("Lorc", "https://game-icons.net/1x1/lorc/compass.html"),
    "crossed-swords": ("Lorc", "https://game-icons.net/1x1/lorc/crossed-swords.html"),
    "open-book": ("Lorc", "https://game-icons.net/1x1/lorc/open-book.html"),
    "gear-hammer": ("Lorc", "https://game-icons.net/1x1/lorc/gear-hammer.html"),
    "gears": ("Lorc", "https://game-icons.net/1x1/lorc/gears.html"),
    "anvil": ("Lorc", "https://game-icons.net/1x1/lorc/anvil.html"),
    "test-tubes": ("Lorc", "https://game-icons.net/1x1/lorc/test-tubes.html"),
    "files": ("Delapouite", "https://game-icons.net/1x1/delapouite/files.html"),
    "eye-target": ("Delapouite", "https://game-icons.net/1x1/delapouite/eye-target.html"),
    "histogram": ("Delapouite", "https://game-icons.net/1x1/delapouite/histogram.html"),
    "knight-banner": ("Delapouite", "https://game-icons.net/1x1/delapouite/knight-banner.html"),
    "plug": ("Delapouite", "https://game-icons.net/1x1/delapouite/plug.html"),
    "puzzle": ("Delapouite", "https://game-icons.net/1x1/delapouite/puzzle.html"),
    "magnifying-glass": ("Lorc", "https://game-icons.net/1x1/lorc/magnifying-glass.html"),
    "scroll-unfurled": ("Lorc", "https://game-icons.net/1x1/lorc/scroll-unfurled.html"),
    "stopwatch": ("Lorc", "https://game-icons.net/1x1/lorc/stopwatch.html"),
}
sprite_names = {
    "archery-target": "calradiaforge_archery_target",
    "compass": "calradiaforge_compass",
    "crossed-swords": "calradiaforge_crossed_swords",
    "open-book": "calradiaforge_open_book",
    "gear-hammer": "calradiaforge_gear_hammer",
    "gears": "calradiaforge_gears",
    "anvil": "calradiaforge_anvil",
    "test-tubes": "calradiaforge_test_tubes",
    "files": "calradiaforge_files",
    "eye-target": "calradiaforge_eye_target",
    "histogram": "calradiaforge_histogram",
    "knight-banner": "calradiaforge_knight_banner",
    "plug": "calradiaforge_plug",
    "puzzle": "calradiaforge_puzzle",
    "magnifying-glass": "calradiaforge_magnifying_glass",
    "scroll-unfurled": "calradiaforge_scroll_unfurled",
    "stopwatch": "calradiaforge_stopwatch",
}
lines = [
    '<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" '
    'xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">'
]
attributions = []
native_icons = []
native_source_root.mkdir(parents=True, exist_ok=True)
icon_paths = sorted(root.glob("game-icons-*.svg"))
present_stems = {path.stem[len("game-icons-"):] for path in icon_paths}
if present_stems != set(credits) or len(icon_paths) != len(credits):
    missing = sorted(set(credits) - present_stems)
    unexpected = sorted(present_stems - set(credits))
    raise ValueError("Game-icons source set differs from its pinned manifest: missing={} unexpected={}".format(
        missing, unexpected))

for path in icon_paths:
    document = ElementTree.parse(path).getroot()
    stem = path.stem[len("game-icons-"):]
    if stem not in credits:
        raise ValueError(f"Add exact artist and upstream URL for {path.name} before including it.")
    if document.attrib.get("viewBox") != "0 0 512 512":
        raise ValueError(f"Unexpected SVG viewBox in {path.name}; review conversion before generating.")
    paths = document.findall(f"{svg_namespace}path")
    # The upstream files are white-on-black. Drop the background rectangle and
    # retain the foreground path so tactical brushes can recolor it in WPF.
    foreground = [item.attrib.get("d", "") for item in paths if item.attrib.get("fill") == "#fff"]
    if not foreground or any(not item.strip() for item in foreground):
        raise ValueError(f"No supported white foreground path found in {path.name}.")
    key = "GameIcon." + stem.replace("-", "_")
    lines.append(f'  <Geometry x:Key="{key}">{" ".join(foreground)}</Geometry>')
    artist, url = credits[stem]
    attributions.append({
        "file": path.name,
        "icon": stem,
        "artist": artist,
        "source": url,
        "license": "CC BY 3.0",
        "licenseUrl": "https://creativecommons.org/licenses/by/3.0/",
        "changes": "Removed the black background and converted the white foreground SVG path to a recolorable WPF Geometry.",
        "spriteName": sprite_names[stem],
        "moduleOutput": f"GUI/SpriteParts/ui_calradiaforge/{sprite_names[stem]}.png",
        "moduleSource": f"GUI/IconSources/GameIcons/{path.name}",
    })
    (native_source_root / path.name).write_bytes(path.read_bytes())
    native_icons.append({
        "name": sprite_names[stem],
        "geometry": " ".join(foreground),
        "output": str(native_root / "ui_calradiaforge" / f"{sprite_names[stem]}.png"),
    })
lines.append("</ResourceDictionary>")
output.write_text("\n".join(lines) + "\n", encoding="utf-8")
(root / "ATTRIBUTION.json").write_text(json.dumps(attributions, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
native_root.joinpath("ATTRIBUTION.json").write_text(json.dumps(attributions, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")

native_root.mkdir(parents=True, exist_ok=True)
category_root = native_root / "ui_calradiaforge"
category_root.mkdir(parents=True, exist_ok=True)
# Remove temporary unprefixed names from early integration passes. Keep all
# unrelated files in the category untouched.
for stale_name in (
    "archery-target.png", "compass.png", "crossed-swords.png", "gear-hammer.png",
    "gears.png", "knight-banner.png", "magnifying-glass.png", "scroll-unfurled.png", "stopwatch.png",
):
    stale_path = category_root / stale_name
    if stale_path.exists():
        stale_path.unlink()
config = native_root / "Config.xml"
config.write_text(
    '<?xml version="1.0" encoding="utf-8"?>\n'
    '<Config><SpriteCategory Name="ui_calradiaforge"><AlwaysLoad /></SpriteCategory></Config>\n',
    encoding="utf-8",
)
readme = native_root / "README.md"
readme.write_text(
    "# Calradia Forge icon source assets\n\n"
    "Generated PNG sprite parts live in `ui_calradiaforge/`. Their source SVGs and per-icon artist, URL, license, and modification records are in the Source-SDK archive at `src/CalradiaForge.Desktop/Resources/GameIcons/`.\n\n"
    "The `Config.xml` entry marks the sprite category as always loaded. The official SpriteSheetGenerator creates the atlas and sprite metadata; Bannerlord Resource Browser imports that atlas and creates `Assets/GauntletUI/ui_calradiaforge_1_tex.tpac`. TpacTool was removed after frequent reader errors; do not install it or use its legacy parser as an import, deployment, or packaging gate. File presence, hashes, and header/table checks do not decode texture payloads or prove rendering. Use Resource Browser's import/update flow, preserve and hash the imported Steam TPAC around code deployment, and verify the referenced sprites in the running game. Add `--installed` to inspect the Steam-installed module; set `BANNERLORD_GAME_DIR` first for a custom install location.\n\n"
    "This workflow is for Gauntlet sprite atlases. The installed Bannerlord managed assemblies and the official asset-import instructions inspected for this project do not expose a documented C# FBX batch-import API or a `.meta` sidecar format for driving Resource Browser imports. Do not use `.meta` conventions from another engine as if Bannerlord supported them; the official flow still requires importing the atlas in Resource Browser. No FBX importer or TPAC writer is claimed here.\n",
    encoding="utf-8",
)

with tempfile.NamedTemporaryFile("w", encoding="utf-8", suffix=".json", delete=False) as request:
    json.dump(native_icons, request, ensure_ascii=False)
    request_path = Path(request.name)
try:
    renderer = Path(__file__).with_name("render_game_icons.ps1")
    result = subprocess.run(
        ["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(renderer), "-Manifest", str(request_path)],
        check=False,
        capture_output=True,
        text=True,
    )
    if result.returncode != 0:
        raise RuntimeError("Native icon rendering failed: " + (result.stderr or result.stdout).strip())
    if result.stdout.strip():
        print(result.stdout.strip())
finally:
    if request_path.exists():
        request_path.unlink()

print(f"Generated {len(attributions)} WPF geometries, native PNG sprite parts, and per-icon attribution records.")

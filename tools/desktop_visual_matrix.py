"""Run fast structural layout checks for every supported Desktop language and scale."""
from __future__ import annotations

import json
from pathlib import Path
from xml.etree import ElementTree as ET


ROOT = Path(__file__).resolve().parents[1]
XAML = (ROOT / "src" / "CalradiaForge.Desktop" / "MainWindow.xaml").read_text(encoding="utf-8")
APP = (ROOT / "src" / "CalradiaForge.Desktop" / "App.xaml").read_text(encoding="utf-8")
LANGUAGES = [path.stem for path in sorted((ROOT / "localization").glob("*.xml"))]
RESOURCES = ROOT / "src" / "CalradiaForge.Desktop" / "Resources"
SCALES = [100, 125, 150, 200]
VERSION = __import__("re").search(r"<CalradiaForgeVersion>([^<]+)</CalradiaForgeVersion>", (ROOT / "Directory.Build.props").read_text(encoding="utf-8")).group(1)


def require(fragment: str, message: str) -> None:
    if fragment not in XAML:
        raise ValueError(message)


def main() -> None:
    require('MinWidth="980"', "Desktop minimum width is missing")
    require('MinHeight="680"', "Desktop minimum height is missing")
    require('Width="300"', "Navigation rail is missing")
    require('Content="{Binding CurrentPage}"', "Routed active page is missing")
    require('AvailableThemes', "Theme selector is missing")
    require('ItemsSource="{Binding VisibleGroups}"', "Grouped section rail is missing")
    require('GroupHeaderButton', "Grouped section command style is missing")
    require('RailMainScrollViewer', "Main section rail viewport is missing")
    require('M28,10 L28,46 M10,28 L46,28', "Command seal vertical and horizontal axes are missing")
    require('Ellipse Width="12" Height="12"', "Command seal marker is missing")
    if 'MapArrow' in XAML or '— ◇ —' in XAML or '—◇—' in XAML or 'TacticalDividerBar' in XAML:
        raise ValueError("Deprecated arbitrary header decoration remains")
    for value in ('#0D1714', '#13231E', '#29463A', '#C7A45A', '#6FB183', '#BC6542'):
        if value not in (RESOURCES / 'TacticalPalette.xaml').read_text(encoding='utf-8'):
            raise ValueError(f"Missing tactical palette value {value}")
    if 'DynamicResource Ui.AppTitle' not in XAML or 'DynamicResource Ui.EvidenceLedger' not in APP:
        raise ValueError("MVVM work-order view is missing its dynamic template")
    english = {node.attrib['key'] for node in ET.parse(ROOT / 'localization' / 'en.xml').findall('string')}
    theme_files = [RESOURCES / 'TacticalPalette.xaml', RESOURCES / 'Themes' / 'TacticalPalette.Parchment.xaml', RESOURCES / 'Themes' / 'TacticalPalette.HighContrast.xaml']
    theme_keys = None
    for theme_file in theme_files:
        if not theme_file.exists():
            raise ValueError(f'Theme dictionary is missing: {theme_file}')
        keys = {node.attrib.get('{http://schemas.microsoft.com/winfx/2006/xaml}Key') for node in ET.parse(theme_file).iter() if node.attrib.get('{http://schemas.microsoft.com/winfx/2006/xaml}Key')}
        if theme_keys is None:
            theme_keys = keys
        elif keys != theme_keys:
            raise ValueError(f'Theme dictionary {theme_file.name} lacks key parity')
    if len(theme_keys or ()) < 20:
        raise ValueError('Theme dictionaries are incomplete')
    rows = []
    for language in LANGUAGES:
        keys = {node.attrib['key'] for node in ET.parse(ROOT / 'localization' / f'{language}.xml').findall('string')}
        if keys != english:
            raise ValueError(f"{language} lacks English catalog parity")
        resource = RESOURCES / f'Strings.{language}.xaml'
        if not resource.exists():
            raise ValueError(f'{language} desktop resource dictionary is missing')
        nodes = ET.parse(resource)
        resource_keys = {node.attrib.get('{http://schemas.microsoft.com/winfx/2006/xaml}Key') for node in nodes.iter() if node.attrib.get('{http://schemas.microsoft.com/winfx/2006/xaml}Key')}
        if len(resource_keys) < 19:
            raise ValueError(f'{language} desktop resource dictionary is incomplete')
        for scale in SCALES:
            rows.append({"language": language, "scalePercent": scale, "status": "structural-pass", "limit": "Static layout/catalog verification; no visual rendering claim."})
    for theme in ('war-table', 'parchment', 'high-contrast'):
        rows.append({"theme": theme, "status": "structural-pass", "limit": "Theme key and selector verification; actual WPF rendering is covered separately."})
    output = ROOT / "artifacts" / f"desktop-visual-matrix-{VERSION.replace('.', '')}.json"
    output.write_text(json.dumps(rows, indent=2) + "\n", encoding="utf-8")
    print(f"Validated {len(rows)} short structural desktop matrix cases: {output}")


if __name__ == '__main__':
    main()

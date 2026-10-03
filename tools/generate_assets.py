"""Generate deterministic, static UI assets and module manifests (no game files modified)."""
from pathlib import Path
import sys
import re
import xml.etree.ElementTree as E
import struct, zlib, json, hashlib

ROOT = Path(__file__).resolve().parents[1]
VERSION = E.parse(ROOT/'Directory.Build.props').find('.//CalradiaForgeVersion').text
GAUNTLET_ONLY = '--gauntlet-only' in sys.argv
PREFAB_ONLY = '--prefab-only' in sys.argv
SUPPORTED_LOCALIZATION_LANGUAGES=('en','es','pt','de','fr','it','pl','ru','tr','zh-HANS','zh-HANT','ja','ko')
navigation_palette=json.loads((ROOT/'localization/navigation-palette.json').read_text(encoding='utf-8'))
for source,localized in navigation_palette.items():
    if localized.get('en')!=source or set(localized)!=set(SUPPORTED_LOCALIZATION_LANGUAGES) or any(not isinstance(value,str) or not value.strip() for value in localized.values()):
        raise ValueError('Incomplete navigation palette translation: '+source)
gauntlet_composer_source=json.loads((ROOT/'localization/gauntlet-composer.json').read_text(encoding='utf-8'))
composer_languages=gauntlet_composer_source.get('languages',[])
composer_entries=gauntlet_composer_source.get('entries',[])
if composer_languages!=list(SUPPORTED_LOCALIZATION_LANGUAGES) or not isinstance(composer_entries,list):
    raise ValueError('Gauntlet Composer translation locales or entry list differ from supported languages.')
gauntlet_composer={}
for entry in composer_entries:
    source=entry.get('key','')
    values=entry.get('values',[])
    if not isinstance(source,str) or not source.strip() or source in gauntlet_composer or not isinstance(values,list) or len(values)!=len(composer_languages):
        raise ValueError('Invalid or duplicate Gauntlet Composer translation entry: '+str(source))
    localized=dict(zip(composer_languages,values))
    if localized.get('en')!=source or any(not isinstance(value,str) or not value.strip() for value in localized.values()):
        raise ValueError('Incomplete Gauntlet Composer translation: '+source)
    if source in navigation_palette:
        raise ValueError('Gauntlet Composer translation duplicates navigation palette key: '+source)
    gauntlet_composer[source]=localized
if not gauntlet_composer:
    raise ValueError('Gauntlet Composer translation source is empty.')
panel_source=(ROOT/'src/CalradiaForge.Mod/PanelViewModel.cs').read_text(encoding='utf-8')
composer_required_texts=set()
for line in panel_source.splitlines():
    if 'GauntletComposer' in line or 'OutputHeading' in line:
        composer_required_texts.update(re.findall(r'T\("([^"\n]+)"\)',line))
    if '_gauntletComposerStatusKey =' in line or 'SetGauntletComposerStatus(' in line:
        composer_required_texts.update(re.findall(r'"([^"\n]+)"',line))
composer_required_texts.discard('Search / argument')
missing_composer_texts=composer_required_texts.difference(gauntlet_composer)
if missing_composer_texts:
    raise ValueError('Gauntlet Composer UI text lacks translations: '+', '.join(sorted(missing_composer_texts)))
rule_source=json.loads((ROOT/'localization/campaign-rule-builder.json').read_text(encoding='utf-8'))
if rule_source.get('languages')!=list(SUPPORTED_LOCALIZATION_LANGUAGES):
    raise ValueError('Campaign Rule Builder translation locales differ from supported languages.')
campaign_rule_texts={}
for entry in rule_source.get('entries',[]):
    source=entry.get('key','')
    values=entry.get('values',[])
    if not isinstance(source,str) or not source.strip() or source in campaign_rule_texts or not isinstance(values,list) or len(values)!=len(SUPPORTED_LOCALIZATION_LANGUAGES):
        raise ValueError('Invalid Campaign Rule Builder translation entry: '+str(source))
    localized=dict(zip(SUPPORTED_LOCALIZATION_LANGUAGES,values))
    if localized['en']!=source or any(not isinstance(value,str) or not value.strip() for value in localized.values()):
        raise ValueError('Incomplete Campaign Rule Builder translation: '+source)
    if source in navigation_palette or source in gauntlet_composer:
        raise ValueError('Campaign Rule Builder translation duplicates a prior key: '+source)
    campaign_rule_texts[source]=localized
if not campaign_rule_texts:
    raise ValueError('Campaign Rule Builder translation source is empty.')
rule_required_texts=set()
for line in panel_source.splitlines():
    if 'CampaignRuleBuilder' in line or 'NoviceCampaignRuleBuilder' in line:
        rule_required_texts.update(re.findall(r'T\("([^"\n]+)"\)',line))
    if '_campaignRuleBuilderStatusKey =' in line or 'SetCampaignRuleBuilderStatus(' in line:
        rule_required_texts.update(re.findall(r'"([^"\n]+)"',line))
missing_rule_texts=rule_required_texts.difference(campaign_rule_texts).difference(gauntlet_composer).difference(navigation_palette)
if missing_rule_texts:
    raise ValueError('Campaign Rule Builder UI text lacks translations: '+', '.join(sorted(missing_rule_texts)))
def write_xml(path, root, encoding='utf-8'):
    path.parent.mkdir(parents=True, exist_ok=True)
    # E.indent(root)
    if encoding == 'utf-8-sig':
        data = E.tostring(root, encoding='utf-8')
        path.write_bytes(b'\xef\xbb\xbf<?xml version="1.0" encoding="utf-8"?>\n' + data)
        return
    E.ElementTree(root).write(path, encoding=encoding, xml_declaration=True)

def manifest(module_id, dll, cls, dependencies):
    root=E.Element('Module')
    names={'CalradiaForge':'Calradia Forge','CalradiaForgeExamples':'Calradia Forge Examples','CalradiaForgePriceProvider':'Calradia Forge Price Provider','CalradiaForgePriceConsumer':'Calradia Forge Price Consumer'}
    for tag,value in [('Name',names[module_id]),('Id',module_id),('Version','v'+VERSION),('SingleplayerModule','true'),('MultiplayerModule','false')]: E.SubElement(root,tag,value=value)
    deps=E.SubElement(root,'DependedModules')
    for dep in dependencies:E.SubElement(deps,'DependedModule',Id=dep)
    sm=E.SubElement(E.SubElement(root,'SubModules'),'SubModule')
    for tag,value in [('Name',module_id),('DLLName',dll),('SubModuleClassType',cls)]:E.SubElement(sm,tag,value=value)
    tags=E.SubElement(sm,'Tags');E.SubElement(tags,'Tag',key='DedicatedServerType',value='none');E.SubElement(tags,'Tag',key='IsNoRenderModeElement',value='false')
    write_xml(ROOT/'modules'/module_id/'SubModule.xml',root)
if not GAUNTLET_ONLY and not PREFAB_ONLY:
    manifest('CalradiaForge','CalradiaForge.Mod.dll','CalradiaForge.Mod.SubModule',['Native','SandBoxCore'])
    manifest('CalradiaForgeExamples','CalradiaForge.Examples.dll','CalradiaForge.Examples.SubModule',['Native','SandBoxCore','CalradiaForge'])
    manifest('CalradiaForgePriceProvider','CalradiaForge.PriceProvider.dll','CalradiaForge.PriceProvider.SubModule',['Native','SandBoxCore','CalradiaForge'])
    manifest('CalradiaForgePriceConsumer','CalradiaForge.PriceConsumer.dll','CalradiaForge.PriceConsumer.SubModule',['Native','SandBoxCore','CalradiaForge','CalradiaForgePriceProvider'])

IMAGEGEN_TEXTURES = {
    'forge_war_table_cloth_v2.png': ((1024, 128), 24, False),
    'forge_rail_cartographic_field_v1.png': ((256, 256), 36, False),
    'forge_heraldic_overlay.png': ((256, 48), 112, True),
    'forge_heraldic_header_v2.png': ((512, 100), 88, True),
    'forge_heraldic_rail_v2.png': ((256, 504), 64, False),
    'forge_patina_brass.png': ((128, 16), 88, False),
    'forge_pine_felt.png': ((128, 32), 40, False),
}
RETIRED_IMAGEGEN_TEXTURES = (
    'forge_dark_wood.png',
    'forge_inkwash.png',
    'forge_war_table_cloth.png',
)
RETIRED_ROUTE_HEADER_TEXTURES = (
    'forge_header_summary_v1.png',
    'forge_header_modules_v1.png',
    'forge_header_logs_v1.png',
    'forge_header_inspector_v1.png',
    'forge_header_tests_v1.png',
    'forge_header_metrics_v1.png',
    'forge_header_framework_v1.png',
    'forge_header_extensions_v1.png',
)
PNG_SIGNATURE = b'\x89PNG\r\n\x1a\n'

def unfilter_png_row(filter_type, encoded, previous, bytes_per_pixel):
    row = bytearray(encoded)
    if filter_type == 0:
        return bytes(row)
    if filter_type not in (1, 2, 3, 4):
        raise ValueError('Prepared PNG uses an unsupported scanline filter')
    for index in range(len(row)):
        left = row[index - bytes_per_pixel] if index >= bytes_per_pixel else 0
        up = previous[index] if previous else 0
        upper_left = previous[index - bytes_per_pixel] if previous and index >= bytes_per_pixel else 0
        if filter_type == 1:
            predictor = left
        elif filter_type == 2:
            predictor = up
        elif filter_type == 3:
            predictor = (left + up) // 2
        else:
            estimate = left + up - upper_left
            pa, pb, pc = abs(estimate - left), abs(estimate - up), abs(estimate - upper_left)
            predictor = left if pa <= pb and pa <= pc else (up if pb <= pc else upper_left)
        row[index] = (row[index] + predictor) & 255
    return bytes(row)

def validate_prepared_texture(data, filename, expected_size, alpha_cap, require_transparency):
    if not data.startswith(PNG_SIGNATURE):
        raise ValueError(f'Prepared ImageGen texture is not a PNG: {filename}')
    offset, width, height, bit_depth, color_type = len(PNG_SIGNATURE), 0, 0, 0, 0
    compressed, saw_end = bytearray(), False
    while offset < len(data):
        if offset + 12 > len(data):
            raise ValueError(f'Prepared PNG has a truncated chunk header: {filename}')
        length = struct.unpack_from('>I', data, offset)[0]
        kind = data[offset + 4:offset + 8]
        end = offset + length + 12
        if end > len(data):
            raise ValueError(f'Prepared PNG has a truncated chunk body: {filename}')
        body = data[offset + 8:offset + 8 + length]
        stored_crc = struct.unpack_from('>I', data, offset + 8 + length)[0]
        if zlib.crc32(kind + body) & 0xffffffff != stored_crc:
            raise ValueError(f'Prepared PNG has an invalid chunk checksum: {filename}')
        if kind == b'IHDR':
            if offset != len(PNG_SIGNATURE) or len(body) != 13:
                raise ValueError(f'Prepared PNG has an invalid IHDR: {filename}')
            width, height, bit_depth, color_type, compression, filtering, interlace = struct.unpack('>IIBBBBB', body)
            if compression or filtering or interlace:
                raise ValueError(f'Prepared PNG uses unsupported encoding: {filename}')
        elif kind == b'IDAT':
            compressed.extend(body)
        elif kind == b'IEND':
            if length != 0:
                raise ValueError(f'Prepared PNG has an invalid IEND: {filename}')
            saw_end = True
            offset = end
            break
        offset = end
    if not saw_end or offset != len(data) or bit_depth != 8 or color_type != 6:
        raise ValueError(f'{filename} must be a complete RGBA8 PNG')
    if (width, height) != expected_size:
        raise ValueError(f'{filename} is {width}x{height}, expected RGBA8 {expected_size}')
    row_bytes = width * 4
    raw = zlib.decompress(compressed)
    if len(raw) != (row_bytes + 1) * height:
        raise ValueError(f'{filename} has an invalid decoded image length')
    previous = b''
    minimum_alpha, maximum_alpha, visible = 255, 0, 0
    left_edge_visible = right_edge_visible = 0
    visible_colors = set()
    cursor = 0
    for _ in range(height):
        filter_type = raw[cursor]
        cursor += 1
        row = unfilter_png_row(filter_type, raw[cursor:cursor + row_bytes], previous, 4)
        cursor += row_bytes
        previous = row
        for x in range(width):
            pixel = row[x * 4:x * 4 + 4]
            alpha = pixel[3]
            minimum_alpha = min(minimum_alpha, alpha)
            maximum_alpha = max(maximum_alpha, alpha)
            if alpha:
                visible += 1
                visible_colors.add(pixel[:3])
                if x < width // 4:
                    left_edge_visible += 1
                if x >= width * 3 // 4:
                    right_edge_visible += 1
    if visible == 0 or maximum_alpha == 0 or maximum_alpha > alpha_cap:
        raise ValueError(f'{filename} must contain visible pixels with alpha <= {alpha_cap}')
    if len(visible_colors) < 2:
        raise ValueError(f'{filename} must preserve visible color variation')
    if require_transparency and (minimum_alpha == maximum_alpha or minimum_alpha != 0):
        raise ValueError(f'{filename} must preserve its transparent background')
    if require_transparency and (left_edge_visible == 0 or right_edge_visible == 0):
        raise ValueError(f'{filename} must preserve visible artwork at both horizontal ends')

def copy_imagegen_textures():
    source_dir = ROOT / 'assets' / 'gauntlet-imagegen' / 'prepared'
    target_dir = ROOT / 'modules/CalradiaForge/GUI/SpriteParts/ui_calradiaforge'
    for filename, (expected, alpha_cap, require_transparency) in IMAGEGEN_TEXTURES.items():
        source = source_dir / filename
        if not source.is_file():
            raise FileNotFoundError(f'Missing prepared ImageGen texture: {source}')
        data = source.read_bytes()
        validate_prepared_texture(data, filename, expected, alpha_cap, require_transparency)
        target_dir.mkdir(parents=True, exist_ok=True)
        (target_dir / filename).write_bytes(data)
    for filename in RETIRED_IMAGEGEN_TEXTURES:
        retired_output = target_dir / filename
        canonical_old_output = source_dir / filename
        if retired_output.is_file():
            if canonical_old_output.is_file() and retired_output.read_bytes() != canonical_old_output.read_bytes():
                raise ValueError(f'Refusing to remove modified retired texture: {retired_output}')
            retired_output.unlink()

    # Retired route ornaments are removed only when the checked-in dated archive
    # and its unique checksum record prove the exact SpriteParts bytes are intact.
    # An already-absent destination is an idempotent no-op; missing or changed
    # archive evidence always aborts before any file is deleted.
    archive_dir = ROOT / 'assets' / 'gauntlet-imagegen' / 'archive' / '2026-09-28'
    archive_manifest = archive_dir / 'SHA256SUMS.txt'
    for filename in RETIRED_ROUTE_HEADER_TEXTURES:
        retired_output = target_dir / filename
        if not retired_output.exists():
            continue
        archived_output = archive_dir / f'spriteparts-{filename}'
        if not archived_output.is_file() or not archive_manifest.is_file():
            raise ValueError(f'Refusing to remove {retired_output}: dated SpriteParts backup or checksum manifest is missing')

        expected_entry = f'spriteparts-{filename}'
        checksum_rows = []
        for line in archive_manifest.read_text(encoding='utf-8-sig').splitlines():
            parts = line.split(None, 2)
            if len(parts) >= 2 and parts[1] == expected_entry:
                checksum_rows.append(parts)
        if len(checksum_rows) != 1 or len(checksum_rows[0]) != 3:
            raise ValueError(f'Refusing to remove {retired_output}: expected one checksum/source row for {expected_entry}')

        checksum, _entry, source_annotation = checksum_rows[0]
        expected_source = str(Path('modules') / 'CalradiaForge' / 'GUI' / 'SpriteParts' / 'ui_calradiaforge' / filename)
        recorded_source = source_annotation.strip()
        if not (recorded_source.startswith('[source=') and recorded_source.endswith(']')):
            raise ValueError(f'Refusing to remove {retired_output}: checksum row has no source annotation')
        recorded_source = recorded_source[len('[source='):-1].replace('/', '\\').casefold()
        if recorded_source != expected_source.replace('/', '\\').casefold():
            raise ValueError(f'Refusing to remove {retired_output}: checksum row points at an unexpected source')

        current_bytes = retired_output.read_bytes()
        archived_bytes = archived_output.read_bytes()
        expected_hash = checksum.upper()
        current_hash = hashlib.sha256(current_bytes).hexdigest().upper()
        archived_hash = hashlib.sha256(archived_bytes).hexdigest().upper()
        if current_bytes != archived_bytes or current_hash != expected_hash or archived_hash != expected_hash:
            raise ValueError(f'Refusing to remove {retired_output}: destination, dated archive, and checksum do not match')
        retired_output.unlink()

if not PREFAB_ONLY:
    copy_imagegen_textures()

root=E.Element('Prefab'); window=E.SubElement(root,'Window')
shade=E.SubElement(window,'Widget',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Sprite='BlankWhiteSquare_9',Color='#000000CC')
# Keep a centered tactical workbench, but let the available viewport size it down
# and cap its footprint on large displays. The 24-DIP margins protect the frame.
panel=E.SubElement(E.SubElement(shade,'Children'),'Widget',Id='ForgeWorkbenchShell',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',SuggestedWidth='1760',SuggestedHeight='1024',MaxWidth='1760',MaxHeight='1024',HorizontalAlignment='Center',VerticalAlignment='Center',MarginLeft='24',MarginRight='24',MarginTop='24',MarginBottom='24',Sprite='BlankWhiteSquare_9',Color='#0F1311FF')
children=E.SubElement(panel,'Children')

def frame(parent, id_value, x, y, width, height, color='#29463AFF', sprite='BlankWhiteSquare_9'):
    return E.SubElement(parent,'Widget',Id=id_value,DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth=str(width),SuggestedHeight=str(height),MarginLeft=str(x),MarginTop=str(y),Sprite=sprite,Color=color)

def scrollbar(parent, id_value, margin_top, margin_bottom, margin_right, width=10):
    handle_id=id_value+'Handle'
    bar=E.SubElement(parent,'ScrollbarWidget',Id=id_value,WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth=str(width),HorizontalAlignment='Right',VerticalAlignment='Top',MarginTop=str(margin_top),MarginBottom=str(margin_bottom),MarginRight=str(margin_right),AlignmentAxis='Vertical',Handle=handle_id,MaxValue='100',MinValue='0')
    bar_children=E.SubElement(bar,'Children')
    E.SubElement(bar_children,'Widget',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='2',HorizontalAlignment='Center',Sprite='BlankWhiteSquare_9',Color='#5A4033FF',AlphaFactor='0.2')
    E.SubElement(bar_children,'ImageWidget',Id=handle_id,WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedHeight='10',SuggestedWidth='4',HorizontalAlignment='Center',Brush='FaceGen.Scrollbar.Handle',**{'Brush.AlphaFactor':'0.7'})
    return bar

def text(parent, id_value, value, x, y, width, height, brush='GameTip.Text', **extra):
    return E.SubElement(parent,'TextWidget',Id=id_value,DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth=str(width),SuggestedHeight=str(height),MarginLeft=str(x),MarginTop=str(y),Brush=brush,Text=value,**extra)

def button(parent,cmd,label,width=155,icon=None,height=42,selected=None,brush='CalradiaForge.TacticalButton',margin_right=8,margin_bottom=0,hint=None):
    attrs={'Id':'Forge'+cmd,'IsFocusable':'true','DoNotPassEventsToChildren':'true','WidthSizePolicy':'Fixed','HeightSizePolicy':'Fixed','SuggestedWidth':str(width),'SuggestedHeight':str(height),'MarginRight':str(margin_right),'MarginBottom':str(margin_bottom),'Brush':brush,'Command.Click':'Execute'+cmd}
    if selected:
        attrs['IsSelected']='@'+selected
        attrs['ButtonType']='Radio'
    if hint:
        attrs['Hint.HintText']=hint
    b=E.SubElement(parent,'ButtonWidget',**attrs)
    bc=E.SubElement(b,'Children')
    text_brush='CalradiaForge.PrimaryText' if brush=='CalradiaForge.Primary' else ('CalradiaForge.CategoryTabText' if selected else 'CalradiaForge.ButtonText')
    if selected:
        E.SubElement(bc,'Widget',Id='Forge'+cmd+'SelectedMarker',IsVisible='@'+selected,DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='4',SuggestedHeight='24',MarginLeft='2',Sprite='BlankWhiteSquare_9',Color='#E1C177FF',VerticalAlignment='Center')
    if icon:
        E.SubElement(bc,'ImageWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='20',SuggestedHeight='20',MarginLeft='10',VerticalAlignment='Center',Sprite=icon,Color='#D7BA73FF')
        E.SubElement(bc,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='36',MarginRight='4',Brush=text_brush,Text=label,HorizontalAlignment='Center',VerticalAlignment='Center')
    else:
        E.SubElement(bc,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='5',MarginRight='5',Brush=text_brush,Text=label,HorizontalAlignment='Center',VerticalAlignment='Center')
    return b

def icon_button(parent,cmd,icon,hint,command=None,margin_right=6):
    attrs={'Id':'Forge'+cmd,'IsFocusable':'true','DoNotPassEventsToChildren':'true','WidthSizePolicy':'Fixed','HeightSizePolicy':'Fixed','SuggestedWidth':'42','SuggestedHeight':'42','MarginRight':str(margin_right),'MarginBottom':'0','Brush':'CalradiaForge.TacticalButton','Command.Click':command or 'Execute'+cmd,'Hint.HintText':hint}
    b=E.SubElement(parent,'ButtonWidget',**attrs)
    bc=E.SubElement(b,'Children')
    E.SubElement(bc,'ImageWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='24',SuggestedHeight='24',HorizontalAlignment='Center',VerticalAlignment='Center',Sprite=icon,Color='#D7BA73FF')
    return b

def horizontal_row(id_value, y=None, x=280, width=918, height=46, visibility=None, bottom=None, right=24):
    attrs={'Id':id_value,'WidthSizePolicy':'StretchToParent','HeightSizePolicy':'Fixed','SuggestedHeight':str(height),'MarginLeft':str(x),'MarginRight':str(right),'StackLayout.LayoutMethod':'HorizontalLeftToRight'}
    if bottom is None:
        attrs['MarginTop']=str(y or 0)
    else:
        attrs['VerticalAlignment']='Bottom'
        attrs['MarginBottom']=str(bottom)
    if visibility:
        attrs['IsVisible']='@'+visibility
    return E.SubElement(E.SubElement(children,'ListPanel',**attrs),'Children')

# Header: the illustration is width-capped and centered so it remains crisp on ultrawide layouts.
header=E.SubElement(children,'Widget',Id='ForgeHeader',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='76',MarginLeft='24',MarginRight='24',MarginTop='12',Sprite='BlankWhiteSquare_9',Color='#101512FF')
header_children=E.SubElement(header,'Children')
E.SubElement(header_children,'ImageWidget',Id='ForgeHeaderCloth',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='76',MaxWidth='1172',HorizontalAlignment='Center',Sprite='forge_war_table_cloth_v2',Color='#FFFFFFFF')
seal_frame=E.SubElement(header_children,'Widget',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='56',SuggestedHeight='56',MarginTop='10',MarginLeft='12',Sprite='BlankWhiteSquare_9',Color='#29463AFF')
E.SubElement(E.SubElement(seal_frame,'Children'),'ImageWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='44',SuggestedHeight='44',MarginTop='6',MarginLeft='6',Sprite='calradiaforge_knight_banner',Color='#FFFFFFFF')
E.SubElement(header_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='550',SuggestedHeight='38',MarginTop='10',MarginLeft='84',Brush='GameTip.Title.Text',Text='@Title')
E.SubElement(header_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='550',SuggestedHeight='23',MarginTop='46',MarginLeft='87',Brush='GameTip.Text',Text='@HeaderSubtitle',**{'Brush.FontSize':'15','Brush.FontColor':'#6FB183FF'})
E.SubElement(header_children,'ImageWidget',Id='ForgeHeaderHeraldicOverlay',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='220',SuggestedHeight='42',MarginTop='17',MarginLeft='650',Sprite='forge_heraldic_header_v2',Color='#FFFFFFFF')
E.SubElement(header_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='118',SuggestedHeight='22',MarginTop='11',MarginRight='16',Brush='CalradiaForge.Gold',Text='@VersionLabel',HorizontalAlignment='Right',**{'Brush.FontSize':'17'})
E.SubElement(header_children,'Widget',Id='ForgeSessionStatusMark',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='12',SuggestedHeight='12',MarginTop='48',MarginRight='235',Sprite='BlankWhiteSquare_9',Color='@SessionStatusColor',HorizontalAlignment='Right')
E.SubElement(header_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='223',SuggestedHeight='27',MarginTop='40',MarginRight='6',Brush='CalradiaForge.Muted',Text='@MemoryHealthText',HorizontalAlignment='Right',**{'Brush.FontSize':'13'})
E.SubElement(children,'ImageWidget',Id='ForgeTopBrassFrameRule',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='6',MarginLeft='24',MarginRight='24',MarginTop='91',Sprite='forge_patina_brass',Color='#FFFFFFFF')

# The eight supported routes become a real vertical rail. Radio selection drives the active visual state.
rail=frame(children,'ForgeNavigationRail',24,101,230,730,'#13231EFF')
rail.set('HeightSizePolicy','StretchToParent')
rail.set('MarginBottom','24')
rail.attrib.pop('DoNotPassEventsToChildren',None)
rail_children=E.SubElement(rail,'Children')
nav_attrs={'Id':'ForgeAreaNavigation','WidthSizePolicy':'Fixed','HeightSizePolicy':'Fixed','SuggestedWidth':'206','SuggestedHeight':'384','MarginLeft':'12','MarginTop':'20','StackLayout.LayoutMethod':'VerticalTopToBottom'}
nav=E.SubElement(E.SubElement(rail_children,'ListPanel',**nav_attrs),'Children')
routes=[('Summary','Summary','calradiaforge_open_book','IsSummaryActive'),('Modules','Modules','calradiaforge_puzzle','IsModulesActive'),('Logs','Logs','calradiaforge_files','IsLogsActive'),('Inspector','Inspector','calradiaforge_eye_target','IsInspectorActive'),('Tests','Tests','calradiaforge_test_tubes','IsTestsActive'),('Metrics','Metrics','calradiaforge_histogram','IsMetricsActive'),('Framework','Framework','calradiaforge_anvil','IsFrameworkActive'),('Extensions','Extensions','calradiaforge_plug','IsExtensionsActive')]
for cmd,label,icon,active in routes:
    button(nav,cmd,'@'+label+'Label',202,icon,height=42,selected=active,brush='CalradiaForge.CategoryTab',margin_right=0,margin_bottom=4)
E.SubElement(rail_children,'ImageWidget',Id='ForgeRailPineFelt',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='198',SuggestedHeight='8',MarginLeft='16',MarginTop='394',Sprite='forge_pine_felt',Color='#FFFFFFFF')
E.SubElement(rail_children,'ImageWidget',Id='ForgeRailCloth',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='128',MaxHeight='256',HorizontalAlignment='Center',MarginLeft='0',MarginTop='425',MarginBottom='5',Sprite='forge_heraldic_rail_v2',Color='#FFFFFFFF')
E.SubElement(rail_children,'TextWidget',Id='ForgeRailGuidance',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='198',MaxHeight='125',MarginLeft='16',MarginTop='433',MarginBottom='12',Brush='CalradiaForge.Muted',Text='@QuickGuide',**{'Brush.FontSize':'15'})

# Route selection and keyboard focus are communicated by the native selected
# button brush and its marker; avoid tiny secondary sprites in this 24-DIP gutter.

# Active route context and a compact, keyboard-focusable evidence toggle.
E.SubElement(children,'TextWidget',Id='ForgeCurrentSection',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='35',MaxWidth='512',MarginLeft='280',MarginRight='@WorkspaceRightMargin',MarginTop='98',Brush='CalradiaForge.HeaderGold',Text='@CurrentSectionLabel',HorizontalAlignment='Left',**{'Brush.FontSize':'28'})
# The 47 SectionHelpLabel strings in PanelViewModel currently have no entries in
# the 13 localization catalogs, so the game uses their English fallback. Bound
# this passive text lane 12 DIP before the evidence-toggle row and reserve three
# lines for the longest source string without moving the command host or ledger.
E.SubElement(children,'TextWidget',Id='ForgeSectionHelp',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='46',MaxWidth='520',MarginLeft='280',MarginRight='@WorkspaceRightMargin',MarginTop='134',Brush='CalradiaForge.Muted',Text='@SectionHelpLabel',HorizontalAlignment='Left',**{'Brush.FontSize':'12'})
focus=E.SubElement(E.SubElement(children,'ListPanel',Id='ForgeEvidenceToggle',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='336',SuggestedHeight='46',MarginTop='97',MarginRight='24',HorizontalAlignment='Right',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'}),'Children')
evidence_focus=icon_button(focus,'ToggleEvidenceFocus','calradiaforge_scroll_unfurled','@FocusEvidenceLabel')
evidence_focus.set('IsVisible','@IsEvidenceToggleVisible')
icon_button(focus,'SdkCatalogButton','calradiaforge_open_book','@SdkCatalogHint',command='ExecuteCategorySdk')
icon_button(focus,'CycleModderRoleButton','calradiaforge_knight_banner','@ActiveModderRoleHint',command='ExecuteCycleModderRole')
icon_button(focus,'ToggleDetailMode','calradiaforge_magnifying_glass','@DetailModeHint')
icon_button(focus,'ToggleCategoryCommandsButton','calradiaforge_files','@SuggestedCommandsHint',command='ExecuteToggleCategoryCommands')
icon_button(focus,'CategoryHelpButton','calradiaforge_compass','@CategoryHelpHint',command='ExecuteCategoryHelp',margin_right=0)
test_results_shortcut=icon_button(focus,'TestResultsExplorerShortcut','calradiaforge_test_tubes','@TestResultsExplorerOpenHint',command='ExecuteOpenTestResultsExplorer',margin_right=0)
test_results_shortcut.set('IsVisible','@ShowTestActions')

# Context cards are a single visual summary row and disappear with the command deck in evidence-focus mode.
briefing_row=E.SubElement(children,'ListPanel',Id='ForgeBriefingDeck',IsVisible='@ShowCommandDeck',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='58',MarginLeft='280',MarginRight='24',MarginTop='180',HorizontalAlignment='Center',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
for i,(label,value,icon,card_suffix) in enumerate([('ContextLabel','ContextValue','calradiaforge_compass','Context'),('TestingLabel','TestingValue','calradiaforge_archery_target','Testing'),('EvidenceCountLabel','EvidenceCountValue','calradiaforge_scroll_unfurled','Evidence'),('TestsLabel','TestCountValue','calradiaforge_crossed_swords','Tests')]):
    card=E.SubElement(E.SubElement(briefing_row,'Children'),'Widget',Id='Briefing'+value,IsVisible='@ShowCommandDeck',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='208',SuggestedHeight='58',MarginRight='12' if i < 3 else '0',Brush='CalradiaForge.BriefingCard')
    cc=E.SubElement(card,'Children')
    E.SubElement(cc,'ImageWidget',Id='ForgeBriefing'+card_suffix+'PineFelt',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='208',SuggestedHeight='3',Sprite='forge_pine_felt',Color='#FFFFFFFF')
    E.SubElement(cc,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='20',MarginTop='4',MarginLeft='12',MarginRight='42',Brush='GameTip.Text',Text='@'+label,**{'Brush.FontSize':'15','Brush.FontColor':'#C7A45AFF'})
    E.SubElement(cc,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='29',MarginTop='25',MarginLeft='12',MarginRight='42',Brush='GameTip.Text',Text='@'+value,**{'Brush.FontSize':'20'})
    E.SubElement(cc,'ImageWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='25',SuggestedHeight='25',HorizontalAlignment='Right',VerticalAlignment='Center',MarginRight='10',Sprite=icon,Color='#D7BA73FF')
    E.SubElement(cc,'ImageWidget',Id='ForgeBriefing'+card_suffix+'PatinaRule',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='208',SuggestedHeight='3',MarginTop='54',Sprite='forge_patina_brass',Color='#FFFFFFFF')

# One shared command/argument region hosts either a normal argument or the assembly workbench fields.
input_row=E.SubElement(children,'Widget',Id='ForgeInputRow',IsVisible='@ShowCommandDeck',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='46',MarginLeft='280',MarginRight='@WorkspaceRightMargin',MarginTop='238',Sprite='BlankWhiteSquare_9',Color='#17261FFF')
input_children=E.SubElement(input_row,'Children')
E.SubElement(input_children,'TextWidget',Id='ForgeInputLabel',IsVisible='@IsNormalInputVisible',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='103',SuggestedHeight='40',MarginLeft='7',MarginTop='3',Brush='CalradiaForge.Gold',Text='@InputLabel',**{'Brush.FontSize':'16'})
E.SubElement(input_children,'TextWidget',Id='ForgeAssemblyPathLabel',IsVisible='@IsAssemblyWorkbench',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='103',SuggestedHeight='40',MarginLeft='7',MarginTop='3',Brush='CalradiaForge.Gold',Text='@AssemblyPathLabel',**{'Brush.FontSize':'16'})
input_frame=E.SubElement(input_children,'Widget',Id='ForgeCommandInputFrame',IsVisible='@IsNormalInputVisible',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='40',MarginTop='3',MarginLeft='112',MarginRight='222',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
input_background=E.SubElement(E.SubElement(input_frame,'Children'),'Widget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0A0D0BFF')
argument=E.SubElement(E.SubElement(input_background,'Children'),'EditableTextWidget',Id='ForgeArgument',IsFocusable='true',UpdateTextOnTyping='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='10',MarginRight='10',Brush='GameTip.Text',Text='@Argument')
pin_btn=button(input_children,'PinCurrentCommand','@PinCurrentCommandLabel',96,height=42,brush='CalradiaForge.TacticalButton',margin_right=118,hint='@PinCurrentCommandHint')
pin_btn.set('HorizontalAlignment','Right')
pin_btn.set('VerticalAlignment','Center')
pin_btn.set('IsVisible','@IsNormalInputVisible')
pin_btn.set('Command.Click','ExecutePinCurrentCommand')
for label_widget in pin_btn.iter('TextWidget'):
    label_widget.set('Brush.FontSize','12')
history_toggle=button(input_children,'HistoryToggle','@HistoryToggleLabel',106,height=42,brush='CalradiaForge.TacticalButton',margin_right=8,hint='@HistoryToggleHint')
history_toggle.set('HorizontalAlignment','Right')
history_toggle.set('VerticalAlignment','Center')
history_toggle.set('IsVisible','@IsNormalInputVisible')
history_toggle.set('IsDisabled','@IsHistoryDisabled')
history_toggle.set('Command.Click','ExecuteToggleHistory')
assembly_path=E.SubElement(input_children,'Widget',Id='ForgeAssemblyPathFrame',IsVisible='@IsAssemblyWorkbench',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='40',MarginTop='3',MarginLeft='112',MarginRight='291',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
assembly_path_inner=E.SubElement(E.SubElement(assembly_path,'Children'),'Widget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0A0D0BFF')
E.SubElement(E.SubElement(assembly_path_inner,'Children'),'EditableTextWidget',Id='ForgeAssemblyPath',UpdateTextOnTyping='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='10',MarginRight='10',Brush='GameTip.Text',Text='@Argument')
E.SubElement(input_children,'TextWidget',Id='ForgeAssemblyVersionLabel',IsVisible='@IsAssemblyWorkbench',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='95',SuggestedHeight='38',MarginTop='4',MarginRight='185',Brush='CalradiaForge.Gold',Text='@AssemblyVersionLabel',HorizontalAlignment='Right',**{'Brush.FontSize':'16'})
version_frame=E.SubElement(input_children,'Widget',Id='ForgeAssemblyVersionFrame',IsVisible='@IsAssemblyWorkbench',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='167',SuggestedHeight='40',MarginTop='3',MarginRight='11',HorizontalAlignment='Right',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
version_inner=E.SubElement(E.SubElement(version_frame,'Children'),'Widget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0A0D0BFF')
E.SubElement(E.SubElement(version_inner,'Children'),'EditableTextWidget',Id='ForgeAssemblyVersion',UpdateTextOnTyping='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='7',MarginRight='7',Brush='GameTip.Text',Text='@AssemblyVersion')

quick_host=E.SubElement(children,'Widget',Id='ForgePrimaryCommandHost',IsVisible='@ShowCommandDeck',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='46',MarginLeft='280',MarginRight='@WorkspaceRightMargin',MarginTop='284')
quick_actions=E.SubElement(E.SubElement(quick_host,'Children'),'ListPanel',Id='ForgePrimaryCommandDeck',IsVisible='@IsRegularActionDeckVisible',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='46',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
quick_actions=E.SubElement(quick_actions,'Children')
for cmd,label in [('Refresh','Refresh'),('Scan','Scan'),('Pin','Pin'),('Compare','Compare')]:button(quick_actions,cmd,'@'+label+'Label','@PrimaryActionButtonWidth',height=42)
button(quick_actions,'Run','@RunLabel','@PrimaryActionButtonWidth',height=42,brush='CalradiaForge.Primary')

hook_host=E.SubElement(E.SubElement(quick_host,'Children'),'Widget',Id='ForgeHookWorkbench',IsVisible='@IsHookWorkbenchVisible',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent')
hook_children=E.SubElement(hook_host,'Children')
hook_actions=E.SubElement(E.SubElement(hook_children,'ListPanel',Id='ForgeHookActions',IsVisible='@HasNoHookPlan',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'}),'Children')
for cmd,label in [('HookInventory','HookStatus'),('HookSelect','HookSelect'),('HookVerify','HookVerify'),('HookApplyPlan','HookApply'),('HookRevertPlan','HookRevert')]:
    hook_button=button(hook_actions,cmd,'@'+label+'Label','@PrimaryActionButtonWidth',height=42)
    if cmd == 'HookSelect': hook_button.set('IsDisabled','@IsHookPickerDisabled')
    elif cmd != 'HookInventory': hook_button.set('IsDisabled','@IsHookSelectionDisabled')
hook_confirm=E.SubElement(E.SubElement(hook_children,'ListPanel',Id='ForgeHookConfirmation',IsVisible='@HasHookPlan',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'}),'Children')
hook_check=E.SubElement(hook_confirm,'ToggleButtonWidget',Id='ForgeHookApproval',IsSelected='@HookConfirmationChecked',IsFocusable='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='240',SuggestedHeight='42',Brush='CalradiaForge.TacticalButton')
E.SubElement(E.SubElement(hook_check,'Children'),'TextWidget',Text='@HookApprovalLabel',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.ButtonText',HorizontalAlignment='Center',VerticalAlignment='Center')
button(hook_confirm,'HookConfirm','@HookConfirmLabel','@PrimaryActionButtonWidth',height=42,brush='CalradiaForge.Primary').set('IsDisabled','@IsHookConfirmDisabled')
button(hook_confirm,'HookCancel','@HookCancelLabel','@PrimaryActionButtonWidth',height=42)

# Evidence ledger stays on an untextured, high-contrast surface; existing page navigation remains intact.
body_frame=E.SubElement(children,'Widget',Id='ForgeEvidenceFrame',IsVisible='@IsEvidenceFrameVisible',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='280',MarginRight='@WorkspaceRightMargin',MarginTop='@EvidenceTop',MarginBottom='178',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
body=E.SubElement(E.SubElement(body_frame,'Children'),'Widget',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginTop='1',MarginRight='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0A0D0BFF')
evidence_children=E.SubElement(body,'Children')
E.SubElement(evidence_children,'TextWidget',Id='ForgeEvidenceHeading',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='205',SuggestedHeight='31',MarginLeft='16',MarginTop='6',Brush='CalradiaForge.HeaderGold',Text='@OutputHeading',**{'Brush.FontSize':'22'})
E.SubElement(evidence_children,'TextWidget',Id='ForgeEvidencePage',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='135',SuggestedHeight='31',HorizontalAlignment='Right',MarginRight='11',MarginTop='6',Brush='CalradiaForge.Gold',Text='@PageLabel',**{'Brush.FontSize':'19'})
filter_row=E.SubElement(evidence_children,'Widget',Id='ForgeOutputFilterRow',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='32',MarginLeft='225',MarginTop='5',MarginRight='150')
filter_row_children=E.SubElement(filter_row,'Children')
filter_frame=E.SubElement(filter_row_children,'Widget',Id='ForgeOutputFilterFrame',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginRight='46',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
filter_inner=E.SubElement(E.SubElement(filter_frame,'Children'),'Widget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0A0D0BFF')
filter_input_children=E.SubElement(filter_inner,'Children')
E.SubElement(filter_input_children,'EditableTextWidget',Id='ForgeOutputFilterInput',IsFocusable='true',UpdateTextOnTyping='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='6',MarginRight='6',Brush='GameTip.Text',Text='@OutputFilterText',**{'Hint.HintText':'@FilterLinesHint'})
E.SubElement(filter_input_children,'TextWidget',Id='ForgeOutputFilterPlaceholder',IsVisible='@IsOutputFilterEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='6',MarginRight='6',Brush='CalradiaForge.Muted',Text='@OutputFilterPlaceholder',VerticalAlignment='Center',**{'Brush.FontSize':'15'})
filter_clear=E.SubElement(filter_row_children,'ButtonWidget',Id='ForgeOutputFilterClear',IsVisible='@HasOutputFilter',IsFocusable='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='40',HorizontalAlignment='Right',VerticalAlignment='Center',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteClearOutputFilter','Hint.HintText':'@ClearOutputFilterHint'})
E.SubElement(E.SubElement(filter_clear,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.ButtonText',Text='×',HorizontalAlignment='Center',VerticalAlignment='Center')
output_actions=E.SubElement(E.SubElement(evidence_children,'ListPanel',Id='ForgeOutputComparisonActions',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='32',MarginLeft='16',MarginTop='42',MarginRight='12',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'}),'Children')
button(output_actions,'PinOutputBaseline','@PinOutputBaselineLabel',174,height=32,hint='@PinOutputBaselineHint')
button(output_actions,'CompareOutput','@OutputComparisonActionLabel',154,height=32,hint='@OutputComparisonActionHint')
clear_baseline=button(output_actions,'ClearOutputBaseline','@ClearOutputBaselineLabel',174,height=32,hint='@ClearOutputBaselineHint')
clear_baseline.set('IsVisible','@IsClearOutputBaselineVisible')
E.SubElement(output_actions,'TextWidget',Id='ForgeOutputBaselinePinnedStatus',IsVisible='@IsOutputBaselineStatusVisible',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='30',MarginLeft='10',Brush='CalradiaForge.Muted',Text='@OutputComparisonStatus',VerticalAlignment='Center',**{'Brush.FontSize':'14'})
column_headers=E.SubElement(evidence_children,'ListPanel',Id='ForgeOutputComparisonColumnHeaders',IsVisible='@IsOutputComparisonActive',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='24',MarginLeft='18',MarginTop='78',MarginRight='28',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
column_children=E.SubElement(column_headers,'Children')
E.SubElement(column_children,'TextWidget',Id='ForgeOutputComparisonBaselineHeader',IsVisible='@IsOutputComparisonActive',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='8',Brush='CalradiaForge.Gold',Text='@OutputComparisonBaselineHeading',VerticalAlignment='Center',**{'Brush.FontSize':'15'})
E.SubElement(column_children,'TextWidget',Id='ForgeOutputComparisonCurrentHeader',IsVisible='@IsOutputComparisonActive',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='8',Brush='CalradiaForge.Gold',Text='@OutputComparisonCurrentHeading',VerticalAlignment='Center',**{'Brush.FontSize':'15'})
evidence_scroll=E.SubElement(evidence_children,'ScrollablePanel',Id='ForgeEvidenceScroll',IsVisible='@IsOutputComparisonInactive',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='18',MarginTop='78',MarginRight='16',MarginBottom='12',ClipRect='ForgeEvidenceClip',InnerPanel='ForgeEvidenceClip\\ForgeEvidenceContent',VerticalScrollbar='..\\ForgeEvidenceScrollBar')
evidence_scroll_children=E.SubElement(evidence_scroll,'Children')
evidence_clip=E.SubElement(evidence_scroll_children,'Widget',Id='ForgeEvidenceClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
evidence_clip_children=E.SubElement(evidence_clip,'Children')
E.SubElement(evidence_clip_children,'TextWidget',Id='ForgeEvidenceContent',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',Brush='CalradiaForge.TerminalText',Text='@Content',VerticalAlignment='Top',ClipContents='true',**{'Brush.FontSize':'@EvidenceFontSize'})
E.SubElement(evidence_clip_children,'TextWidget',Id='ForgeEmptyEvidence',IsVisible='@IsNormalContentEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='20',MarginRight='20',Brush='CalradiaForge.Muted',Text='@ContentPlaceholder',HorizontalAlignment='Center',VerticalAlignment='Center',ClipContents='true',**{'Brush.FontSize':'19'})
normal_scrollbar=scrollbar(evidence_children,'ForgeEvidenceScrollBar',78,12,16)
normal_scrollbar.set('IsVisible','@IsOutputComparisonInactive')
comparison_scroll=E.SubElement(evidence_children,'ScrollablePanel',Id='ForgeOutputComparisonScroll',IsVisible='@IsOutputComparisonActive',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='18',MarginTop='106',MarginRight='16',MarginBottom='12',ClipRect='ForgeOutputComparisonClip',InnerPanel='ForgeOutputComparisonClip\\ForgeOutputComparisonRows',VerticalScrollbar='..\\ForgeOutputComparisonScrollBar')
comparison_scroll_children=E.SubElement(comparison_scroll,'Children')
comparison_clip=E.SubElement(comparison_scroll_children,'Widget',Id='ForgeOutputComparisonClip',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
comparison_clip_children=E.SubElement(comparison_clip,'Children')
comparison_list=E.SubElement(comparison_clip_children,'ListPanel',Id='ForgeOutputComparisonRows',DataSource='{OutputComparisonRows}',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
comparison_template=E.SubElement(comparison_list,'ItemTemplate')
comparison_row=E.SubElement(comparison_template,'ListPanel',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='34',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
comparison_row_children=E.SubElement(comparison_row,'Children')
E.SubElement(comparison_row_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='32',MarginLeft='8',MarginRight='8',Brush='CalradiaForge.TerminalText',Text='@BaselineText',VerticalAlignment='Center',**{'Brush.FontSize':'16'})
E.SubElement(comparison_row_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='32',MarginLeft='8',MarginRight='8',Brush='CalradiaForge.TerminalText',Text='@CurrentText',VerticalAlignment='Center',**{'Brush.FontSize':'16'})
E.SubElement(comparison_clip_children,'TextWidget',Id='ForgeOutputComparisonStatus',IsVisible='@IsOutputComparisonStatusVisible',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='24',MarginRight='24',Brush='CalradiaForge.Muted',Text='@OutputComparisonStatus',HorizontalAlignment='Center',VerticalAlignment='Center',ClipContents='true',**{'Brush.FontSize':'17'})
comparison_scrollbar=scrollbar(evidence_children,'ForgeOutputComparisonScrollBar',106,12,16)
comparison_scrollbar.set('IsVisible','@IsOutputComparisonActive')

# The evidence frame ends at y=702 in both modes; keep a quiet brass rule below it.
E.SubElement(children,'ImageWidget',Id='ForgeEvidenceActionBrassRule',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='8',MarginLeft='280',MarginRight='@WorkspaceRightMargin',MarginBottom='158',VerticalAlignment='Bottom',Sprite='forge_patina_brass',Color='#FFFFFFFF')

regular_actions=horizontal_row('ForgeSecondaryActionDeck',visibility='IsRegularActionDeckVisible',bottom=99)
button(regular_actions,'Enable','@EnableLabel',152,height=42)
button(regular_actions,'Copy','@CopyLabel',210,height=42)
button(regular_actions,'Export','@ExportLabel',150,height=42)
button(regular_actions,'Batch','@BatchLabel',142,height=42)
button(regular_actions,'ClearOutput','@ClearOutputLabel',142,height=42,hint='@ClearOutputHint')
extension_actions=horizontal_row('ForgeExtensionActionDeck',visibility='IsExtensionsActionDeckVisible',bottom=99)
for cmd,label,width in [('OpenAssemblyWorkbench','OpenAssemblyWorkbenchLabel',270),('OpenExtensionPage','OpenExtensionPageLabel',270),('ContextHelp','ContextHelpLabel',270)]:button(extension_actions,cmd,'@'+label,width,height=42)
assembly_actions=horizontal_row('ForgeAssemblyActionDeck',visibility='IsAssemblyWorkbench',bottom=99)
for cmd,label,width in [('AssemblyList','AssemblyListLabel',164),('AssemblySelect','AssemblySelectLabel',164),('AssemblyInspect','AssemblyInspectLabel',164),('AssemblyPreview','AssemblyPreviewLabel',164),('AssemblyApply','AssemblyApplyLabel',164)]:button(assembly_actions,cmd,'@'+label,width,height=42)

page_actions=horizontal_row('ForgePaginationAndUtilityDeck',visibility='IsComposerPaginationVisible',bottom=47)
for cmd,label,width in [('Previous','PreviousLabel',112),('Next','NextLabel',112),('Snapshots','SnapshotsLabel',145),('Dependencies','DependenciesLabel',145),('Remove','RemoveLabel',155),('Close','CloseLabel',112)]:button(page_actions,cmd,'@'+label,width,height=42,margin_right=7)
button(page_actions,'ToggleKeyHelp','?',42,height=42,margin_right=0,hint='@KeyHelpHint')

# Gauntlet Page Composer: a local, data-bound editor surface. The preview uses
# only the component ViewModels below; it never loads generated XML or invokes
# game/campaign operations.
composer_workspace=E.SubElement(children,'Widget',Id='ForgeComposerWorkspace',IsVisible='@IsGauntletComposerWorkspaceVisible',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='280',MarginRight='24',MarginTop='180',MarginBottom='155',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
composer_surface=E.SubElement(E.SubElement(composer_workspace,'Children'),'Widget',Id='ForgeComposerSurface',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0A0D0BFF')
composer_children=E.SubElement(composer_surface,'Children')
E.SubElement(composer_children,'TextWidget',Id='ForgeComposerTitleLabel',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='220',SuggestedHeight='24',MarginLeft='12',MarginTop='7',Brush='CalradiaForge.Gold',Text='@GauntletComposerTitleLabel',**{'Brush.FontSize':'16'})
composer_title_frame=E.SubElement(composer_children,'Widget',Id='ForgeComposerTitleFrame',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='34',MarginLeft='12',MarginRight='12',MarginTop='31',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
composer_title_inner=E.SubElement(E.SubElement(composer_title_frame,'Children'),'Widget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
E.SubElement(E.SubElement(composer_title_inner,'Children'),'EditableTextWidget',Id='ForgeComposerPageTitle',IsFocusable='true',UpdateTextOnTyping='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='10',MarginRight='10',Brush='GameTip.Text',Text='@GauntletComposerTitle')

composer_left=E.SubElement(composer_children,'Widget',Id='ForgeComposerLeftColumn',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='316',MarginLeft='12',MarginTop='72',MarginBottom='12',Sprite='BlankWhiteSquare_9',Color='#14211CFF')
composer_left_children=E.SubElement(composer_left,'Children')
E.SubElement(composer_left_children,'TextWidget',Id='ForgeComposerCatalogHeading',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='26',MarginLeft='10',MarginRight='10',MarginTop='4',Brush='CalradiaForge.HeaderGold',Text='@GauntletComposerCatalogHeading',**{'Brush.FontSize':'18'})
composer_left_scroll=E.SubElement(composer_left_children,'ScrollablePanel',Id='ForgeComposerLeftScroll',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='8',MarginRight='12',MarginTop='32',MarginBottom='10',ClipRect='ForgeComposerLeftClip',InnerPanel='ForgeComposerLeftClip\\ForgeComposerLeftContent',VerticalScrollbar='..\\ForgeComposerLeftScrollBar')
scrollbar(composer_left_children,'ForgeComposerLeftScrollBar',32,10,12)
composer_left_scroll_children=E.SubElement(composer_left_scroll,'Children')
composer_left_clip=E.SubElement(composer_left_scroll_children,'Widget',Id='ForgeComposerLeftClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
composer_left_content=E.SubElement(E.SubElement(composer_left_clip,'Children'),'ListPanel',Id='ForgeComposerLeftContent',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
composer_left_content_children=E.SubElement(composer_left_content,'Children')
composer_catalog=E.SubElement(composer_left_content_children,'ListPanel',Id='ForgeComposerCatalog',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
composer_catalog_children=E.SubElement(composer_catalog,'Children')
for kind,label in [('Heading','GauntletComposerAddHeadingLabel'),('Text','GauntletComposerAddTextLabel'),('Field','GauntletComposerAddFieldLabel'),('Button','GauntletComposerAddButtonLabel'),('Metric','GauntletComposerAddMetricLabel'),('List','GauntletComposerAddListLabel'),('Toggle','GauntletComposerAddToggleLabel'),('Progress','GauntletComposerAddProgressLabel'),('Selector','GauntletComposerAddSelectorLabel')]:
    add_button=button(composer_catalog_children,'ComposerAdd'+kind,'@'+label,276,height=34,margin_right=0,margin_bottom=4)
    add_button.set('IsDisabled','@GauntletComposerAtCapacity')

E.SubElement(composer_left_content_children,'TextWidget',Id='ForgeComposerOrderHeading',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='30',MarginLeft='2',MarginRight='4',MarginTop='12',Brush='CalradiaForge.HeaderGold',Text='@GauntletComposerOrderHeading',**{'Brush.FontSize':'18'})
E.SubElement(composer_left_content_children,'TextWidget',Id='ForgeComposerCount',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='22',MarginLeft='2',MarginRight='4',Brush='CalradiaForge.Muted',Text='@GauntletComposerCountLabel',**{'Brush.FontSize':'13'})
composer_blocks=E.SubElement(composer_left_content_children,'ListPanel',Id='ForgeComposerBlocks',DataSource='{GauntletComposerBlocks}',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
composer_block_template=E.SubElement(composer_blocks,'ItemTemplate')
composer_block_button=E.SubElement(composer_block_template,'ButtonWidget',IsFocusable='true',IsSelected='@IsSelected',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='44',MarginBottom='4',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteSelect'})
composer_block_children=E.SubElement(composer_block_button,'Children')
E.SubElement(composer_block_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='20',MarginLeft='8',MarginRight='8',MarginTop='2',Brush='CalradiaForge.Gold',Text='@TypeLabel',**{'Brush.FontSize':'12'})
E.SubElement(composer_block_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='20',MarginLeft='8',MarginRight='8',MarginTop='21',Brush='CalradiaForge.ButtonText',Text='@Label',**{'Brush.FontSize':'14'})

composer_right=E.SubElement(composer_children,'Widget',Id='ForgeComposerRightColumn',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='340',MarginRight='12',MarginTop='72',MarginBottom='12',Sprite='BlankWhiteSquare_9',Color='#14211CFF')
composer_right_children=E.SubElement(composer_right,'Children')
E.SubElement(composer_right_children,'TextWidget',Id='ForgeComposerPropertiesHeading',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='26',MarginLeft='10',MarginRight='10',MarginTop='4',Brush='CalradiaForge.HeaderGold',Text='@GauntletComposerPropertiesHeading',**{'Brush.FontSize':'18'})
composer_right_scroll=E.SubElement(composer_right_children,'ScrollablePanel',Id='ForgeComposerRightScroll',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='10',MarginRight='12',MarginTop='32',MarginBottom='10',ClipRect='ForgeComposerRightClip',InnerPanel='ForgeComposerRightClip\\ForgeComposerRightContent',VerticalScrollbar='..\\ForgeComposerRightScrollBar')
scrollbar(composer_right_children,'ForgeComposerRightScrollBar',32,10,12)
composer_right_scroll_children=E.SubElement(composer_right_scroll,'Children')
composer_right_clip=E.SubElement(composer_right_scroll_children,'Widget',Id='ForgeComposerRightClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
composer_right_content=E.SubElement(E.SubElement(composer_right_clip,'Children'),'ListPanel',Id='ForgeComposerRightContent',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
composer_right_content_children=E.SubElement(composer_right_content,'Children')
E.SubElement(composer_right_content_children,'TextWidget',Id='ForgeComposerSelectedKind',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='24',MarginLeft='2',MarginRight='6',Brush='CalradiaForge.Gold',Text='@GauntletComposerSelectedKindLabel',**{'Brush.FontSize':'14'})

def composer_editable_row(parent, widget_id, label_binding, value_binding, visible=None):
    attrs={'Id':widget_id,'WidthSizePolicy':'StretchToParent','HeightSizePolicy':'Fixed','SuggestedHeight':'66','MarginBottom':'5'}
    if visible:
        attrs['IsVisible']='@'+visible
    row=E.SubElement(parent,'Widget',**attrs)
    row_children=E.SubElement(row,'Children')
    E.SubElement(row_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='20',Brush='CalradiaForge.Muted',Text='@'+label_binding,**{'Brush.FontSize':'13'})
    frame=E.SubElement(row_children,'Widget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='38',MarginTop='22',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
    inner=E.SubElement(E.SubElement(frame,'Children'),'Widget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
    E.SubElement(E.SubElement(inner,'Children'),'EditableTextWidget',IsFocusable='true',UpdateTextOnTyping='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='8',MarginRight='8',Brush='GameTip.Text',Text='@'+value_binding)

composer_editable_row(composer_right_content_children,'ForgeComposerLabelEditor','GauntletComposerLabelFieldLabel','GauntletComposerSelectedLabel','GauntletComposerHasSelection')
composer_editable_row(composer_right_content_children,'ForgeComposerTextEditor','GauntletComposerTextFieldLabel','GauntletComposerSelectedText','GauntletComposerHasSelection')
composer_editable_row(composer_right_content_children,'ForgeComposerOptionsEditor','GauntletComposerOptionsFieldLabel','GauntletComposerSelectedOptions','GauntletComposerOptionsVisible')
composer_editable_row(composer_right_content_children,'ForgeComposerProgressEditor','GauntletComposerProgressFieldLabel','GauntletComposerSelectedProgress','GauntletComposerProgressVisible')
composer_select_actions=E.SubElement(composer_right_content_children,'ListPanel',Id='ForgeComposerSelectionActions',IsVisible='@GauntletComposerHasSelection',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='38',MarginTop='2',MarginBottom='4',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
composer_select_children=E.SubElement(composer_select_actions,'Children')
for cmd,label,width,handler in [('ComposerPreviousBlock','GauntletComposerPreviousLabel',98,'ExecuteComposerPreviousBlock'),('ComposerNextBlock','GauntletComposerNextLabel',98,'ExecuteComposerNextBlock'),('ComposerMoveUp','GauntletComposerMoveUpLabel',82,'ExecuteComposerMoveUp'),('ComposerMoveDown','GauntletComposerMoveDownLabel',82,'ExecuteComposerMoveDown'),('ComposerRemove','GauntletComposerRemoveLabel',88,'ExecuteComposerRemove')]:
    control=button(composer_select_children,cmd,'@'+label,width,height=34,margin_right=4 if cmd!='ComposerRemove' else 0)
    control.set('Command.Click',handler)
E.SubElement(composer_right_content_children,'TextWidget',Id='ForgeComposerEmptyState',IsVisible='@GauntletComposerIsEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='46',MarginLeft='4',MarginRight='8',Brush='CalradiaForge.Muted',Text='@GauntletComposerEmptyLabel',VerticalAlignment='Center',ClipContents='true',**{'Brush.FontSize':'15'})
composer_sample_actions=E.SubElement(composer_right_content_children,'ListPanel',Id='ForgeComposerSampleActions',IsVisible='@GauntletComposerHasSelection',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='34',MarginTop='4',MarginBottom='6',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
composer_sample_children=E.SubElement(composer_sample_actions,'Children')
sample_button=button(composer_sample_children,'ComposerSampleButton','@GauntletComposerTryButtonLabel',128,height=32,margin_right=5)
sample_button.set('IsVisible','@GauntletComposerSampleButtonVisible')
sample_button.set('Command.Click','ExecuteComposerSampleButton')
sample_toggle=button(composer_sample_children,'ComposerSampleToggle','@GauntletComposerToggleSampleLabel',142,height=32,margin_right=5)
sample_toggle.set('IsVisible','@GauntletComposerSampleToggleVisible')
sample_toggle.set('Command.Click','ExecuteComposerSampleToggle')
sample_next=button(composer_sample_children,'ComposerSampleNextOption','@GauntletComposerNextSampleLabel',130,height=32,margin_right=0)
sample_next.set('IsVisible','@GauntletComposerSampleSelectorVisible')
sample_next.set('Command.Click','ExecuteComposerSampleNextOption')
E.SubElement(composer_right_content_children,'TextWidget',Id='ForgeComposerSampleValue',IsVisible='@GauntletComposerHasSelection',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='22',MarginLeft='3',MarginRight='8',Brush='CalradiaForge.Muted',Text='@GauntletComposerSampleValueLabel',**{'Brush.FontSize':'13'})
E.SubElement(composer_right_content_children,'TextWidget',Id='ForgeComposerPreviewHeading',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='25',MarginLeft='2',MarginRight='6',MarginTop='6',Brush='CalradiaForge.HeaderGold',Text='@GauntletComposerPreviewHeading',**{'Brush.FontSize':'17'})
E.SubElement(composer_right_content_children,'TextWidget',Id='ForgeComposerPreviewEmpty',IsVisible='@GauntletComposerIsEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='48',MarginLeft='4',MarginRight='8',Brush='CalradiaForge.Muted',Text='@GauntletComposerEmptyLabel',VerticalAlignment='Center',ClipContents='true',**{'Brush.FontSize':'15'})

composer_preview=E.SubElement(composer_right_content_children,'ListPanel',Id='ForgeComposerPreviewBlocks',DataSource='{GauntletComposerBlocks}',IsVisible='@GauntletComposerHasBlocks',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginTop='6',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
composer_preview_template=E.SubElement(composer_preview,'ItemTemplate')
composer_preview_item=E.SubElement(composer_preview_template,'ListPanel',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginBottom='8',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
composer_preview_children=E.SubElement(composer_preview_item,'Children')
E.SubElement(composer_preview_children,'TextWidget',IsVisible='@IsHeading',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='32',Brush='CalradiaForge.HeaderGold',Text='@Label',**{'Brush.FontSize':'21'})
E.SubElement(composer_preview_children,'TextWidget',IsVisible='@IsText',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginLeft='2',MarginRight='8',Brush='GameTip.Text',Text='@Text',ClipContents='true',**{'Brush.FontSize':'16'})
field_preview=E.SubElement(composer_preview_children,'Widget',IsVisible='@IsField',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='48')
field_preview_children=E.SubElement(field_preview,'Children')
E.SubElement(field_preview_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='145',Brush='CalradiaForge.Gold',Text='@Label',VerticalAlignment='Center',ClipContents='true',**{'Brush.FontSize':'14'})
E.SubElement(field_preview_children,'EditableTextWidget',IsFocusable='true',UpdateTextOnTyping='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='150',MarginRight='5',Brush='GameTip.Text',Text='@Text')
button_preview=E.SubElement(composer_preview_children,'ButtonWidget',IsVisible='@IsButton',IsFocusable='true',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='42',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteSampleAction'})
E.SubElement(E.SubElement(button_preview,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.ButtonText',Text='@SampleButtonLabel',HorizontalAlignment='Center',VerticalAlignment='Center')
metric_preview=E.SubElement(composer_preview_children,'ListPanel',IsVisible='@IsMetric',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='38',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
metric_children=E.SubElement(metric_preview,'Children')
E.SubElement(metric_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.Gold',Text='@Label',VerticalAlignment='Center')
E.SubElement(metric_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='140',Brush='CalradiaForge.TerminalText',Text='@ValueText',HorizontalAlignment='Right',VerticalAlignment='Center')
list_preview=E.SubElement(composer_preview_children,'ListPanel',Id='ForgeComposerPreviewList',IsVisible='@IsList',DataSource='{Options}',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
list_template=E.SubElement(list_preview,'ItemTemplate')
list_option=E.SubElement(list_template,'ButtonWidget',IsFocusable='true',IsSelected='@IsSelected',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='32',MarginBottom='3',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteSelect'})
E.SubElement(E.SubElement(list_option,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='8',MarginRight='8',Brush='CalradiaForge.ButtonText',Text='@Label',VerticalAlignment='Center')
toggle_preview=E.SubElement(composer_preview_children,'ListPanel',IsVisible='@IsToggle',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='42',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
toggle_preview_children=E.SubElement(toggle_preview,'Children')
toggle_button=E.SubElement(toggle_preview_children,'ButtonWidget',IsFocusable='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='42',SuggestedHeight='42',ButtonType='Toggle',IsSelected='@IsOn',ToggleIndicator='ForgeComposerPreviewToggleIndicator',Brush='SPOptions.Checkbox.Empty.Button')
E.SubElement(E.SubElement(toggle_button,'Children'),'ImageWidget',Id='ForgeComposerPreviewToggleIndicator',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='SPOptions.Checkbox.Full.Button')
E.SubElement(toggle_preview_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='10',Brush='CalradiaForge.ButtonText',Text='@Label',VerticalAlignment='Center')
progress_preview=E.SubElement(composer_preview_children,'ListPanel',IsVisible='@IsProgress',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='56',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
progress_children=E.SubElement(progress_preview,'Children')
E.SubElement(progress_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='20',Brush='CalradiaForge.Gold',Text='@Label',**{'Brush.FontSize':'14'})
preview_fill_bar=E.SubElement(progress_children,'FillBarWidget',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='260',SuggestedHeight='27',HorizontalAlignment='Left',VerticalAlignment='Center',ContainerWidget='ForgeComposerPreviewProgressContainer',FillWidget='ForgeComposerPreviewProgressFillParent\\ForgeComposerPreviewProgressFill',MaxAmountAsFloat='1',InitialAmountAsFloat='@ProgressAmount')
preview_fill_children=E.SubElement(preview_fill_bar,'Children')
progress_fill_parent=E.SubElement(preview_fill_children,'Widget',Id='ForgeComposerPreviewProgressFillParent',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='240',SuggestedHeight='14',MarginLeft='10',MarginRight='10',Sprite='BlankWhiteSquare_9',Color='#6FB183FF')
E.SubElement(E.SubElement(progress_fill_parent,'Children'),'Widget',Id='ForgeComposerPreviewProgressFill',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
E.SubElement(preview_fill_children,'Widget',Id='ForgeComposerPreviewProgressContainer',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Sprite='BlankWhiteSquare_9',Color='#27392FFF')
selector_preview=E.SubElement(composer_preview_children,'SelectorWidget',IsVisible='@IsSelector',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',CurrentSelectedIndex='@SelectedOptionIndex',Container='ForgeComposerPreviewSelectorGrid')
selector_children=E.SubElement(selector_preview,'Children')
selector_grid=E.SubElement(selector_children,'NavigatableGridWidget',Id='ForgeComposerPreviewSelectorGrid',DataSource='{Options}',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',ColumnCount='1',DefaultCellHeight='34',DefaultCellWidth='280')
selector_template=E.SubElement(selector_grid,'ItemTemplate')
selector_option=E.SubElement(selector_template,'ButtonWidget',IsFocusable='true',IsSelected='@IsSelected',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='32',MarginBottom='2',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteSelect'})
E.SubElement(E.SubElement(selector_option,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='8',MarginRight='8',Brush='CalradiaForge.ButtonText',Text='@Label',VerticalAlignment='Center')

composer_actions=horizontal_row('ForgeComposerActionDeck',visibility='IsGauntletComposerActive',bottom=99)
save_draft=button(composer_actions,'ComposerSave','@GauntletComposerSaveLabel',150,height=42,margin_right=8)
save_draft.set('IsVisible','@IsGauntletComposerWorkspaceVisible')
save_draft.set('Command.Click','ExecuteComposerSaveDraft')
generate_package=button(composer_actions,'ComposerGenerate','@GauntletComposerGenerateLabel',150,height=42,brush='CalradiaForge.Primary',margin_right=8)
generate_package.set('IsVisible','@IsGauntletComposerWorkspaceVisible')
generate_package.set('Command.Click','ExecuteComposerGenerate')
copy_package=button(composer_actions,'ComposerCopy','@GauntletComposerCopyLabel',166,height=42,margin_right=8)
copy_package.set('IsVisible','@IsGauntletComposerPackageVisible')
copy_package.set('Command.Click','ExecuteComposerCopyPackage')
edit_draft=button(composer_actions,'ComposerEdit','@GauntletComposerEditLabel',150,height=42,margin_right=8)
edit_draft.set('IsVisible','@IsGauntletComposerPackageVisible')
edit_draft.set('Command.Click','ExecuteComposerEditDraft')
E.SubElement(composer_actions,'TextWidget',Id='ForgeComposerStatus',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='4',MarginRight='4',Brush='CalradiaForge.Muted',Text='@GauntletComposerStatusLabel',VerticalAlignment='Center',ClipContents='true',**{'Brush.FontSize':'13'})

# Campaign Rule Builder keeps authoring controls in two independently scrolling
# columns. The preview is display-only and cannot execute campaign actions.
rule_workspace=E.SubElement(children,'Widget',Id='ForgeCampaignRuleWorkspace',IsVisible='@IsCampaignRuleBuilderWorkspaceVisible',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='280',MarginRight='24',MarginTop='180',MarginBottom='155',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
rule_surface=E.SubElement(E.SubElement(rule_workspace,'Children'),'Widget',Id='ForgeCampaignRuleSurface',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0A0D0BFF')
rule_children=E.SubElement(rule_surface,'Children')

rule_left=E.SubElement(rule_children,'Widget',Id='ForgeCampaignRuleLeftColumn',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='316',MarginLeft='12',MarginTop='12',MarginBottom='12',Sprite='BlankWhiteSquare_9',Color='#14211CFF')
rule_left_children=E.SubElement(rule_left,'Children')
E.SubElement(rule_left_children,'TextWidget',Id='ForgeCampaignRuleCatalogHeading',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='26',MarginLeft='10',MarginRight='10',MarginTop='4',Brush='CalradiaForge.HeaderGold',Text='@CampaignRuleBuilderRulesLabel',**{'Brush.FontSize':'18'})
rule_add=button(rule_left_children,'CampaignRuleAdd','@CampaignRuleBuilderAddRuleLabel',282,height=36,margin_right=0)
rule_add.set('MarginLeft','10')
rule_add.set('MarginTop','34')
rule_add.set('Command.Click','ExecuteCampaignRuleBuilderAdd')
rule_add.set('IsDisabled','@CampaignRuleBuilderAtCapacity')
E.SubElement(rule_left_children,'TextWidget',Id='ForgeCampaignRuleCount',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='20',MarginLeft='10',MarginRight='10',MarginTop='76',Brush='CalradiaForge.Muted',Text='@CampaignRuleBuilderCountLabel',**{'Brush.FontSize':'13'})
rule_left_scroll=E.SubElement(rule_left_children,'ScrollablePanel',Id='ForgeCampaignRuleLeftScroll',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='8',MarginRight='12',MarginTop='121',MarginBottom='52',ClipRect='ForgeCampaignRuleLeftClip',InnerPanel='ForgeCampaignRuleLeftClip\\ForgeCampaignRuleList',VerticalScrollbar='..\\ForgeCampaignRuleLeftScrollbar')
rule_left_clip=E.SubElement(E.SubElement(rule_left_scroll,'Children'),'Widget',Id='ForgeCampaignRuleLeftClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
rule_list=E.SubElement(E.SubElement(rule_left_clip,'Children'),'ListPanel',Id='ForgeCampaignRuleList',DataSource='{CampaignRuleBuilderRules}',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
rule_template=E.SubElement(rule_list,'ItemTemplate')
rule_entry=E.SubElement(rule_template,'ButtonWidget',Id='ForgeCampaignRuleRow',IsFocusable='true',IsSelected='@IsSelected',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='54',MarginBottom='4',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteSelect'})
rule_entry_children=E.SubElement(rule_entry,'Children')
E.SubElement(rule_entry_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='18',MarginLeft='8',MarginRight='8',MarginTop='2',Brush='CalradiaForge.Gold',Text='@RowIdentityLabel',**{'Brush.FontSize':'12'})
E.SubElement(rule_entry_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='30',MarginLeft='8',MarginRight='8',MarginTop='20',Brush='CalradiaForge.ButtonText',Text='@Summary',ClipContents='true',**{'Brush.FontSize':'13'})
scrollbar(rule_left_children,'ForgeCampaignRuleLeftScrollbar',121,52,12)
E.SubElement(rule_left_children,'TextWidget',Id='ForgeCampaignRuleEmpty',IsVisible='@CampaignRuleBuilderIsEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='44',MarginLeft='12',MarginRight='12',MarginTop='125',Brush='CalradiaForge.Muted',Text='@CampaignRuleBuilderEmptyLabel',ClipContents='true',**{'Brush.FontSize':'14'})
rule_order_actions=E.SubElement(E.SubElement(rule_left_children,'ListPanel',Id='ForgeCampaignRuleOrderActions',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='36',MarginLeft='8',MarginRight='8',MarginBottom='8',VerticalAlignment='Bottom',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'}),'Children')
for command,glyph,label,width,handler in [('CampaignRulePrevious','‹','CampaignRuleBuilderPreviousLabel',52,'ExecuteCampaignRuleBuilderSelectPrevious'),('CampaignRuleNext','›','CampaignRuleBuilderNextLabel',52,'ExecuteCampaignRuleBuilderSelectNext'),('CampaignRuleUp','↑','CampaignRuleBuilderMoveUpLabel',48,'ExecuteCampaignRuleBuilderMoveUp'),('CampaignRuleDown','↓','CampaignRuleBuilderMoveDownLabel',48,'ExecuteCampaignRuleBuilderMoveDown'),('CampaignRuleRemove','×','CampaignRuleBuilderRemoveRuleLabel',48,'ExecuteCampaignRuleBuilderRemove')]:
    control=button(rule_order_actions,command,glyph,width,height=32,margin_right=3,hint='@'+label)
    control.set('Command.Click',handler)
    control.set('IsDisabled','@CampaignRuleBuilderCannotSelect')

rule_right=E.SubElement(rule_children,'Widget',Id='ForgeCampaignRuleRightColumn',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='340',MarginRight='12',MarginTop='12',MarginBottom='12',Sprite='BlankWhiteSquare_9',Color='#14211CFF')
rule_right_children=E.SubElement(rule_right,'Children')
E.SubElement(rule_right_children,'TextWidget',Id='ForgeCampaignRulePropertiesHeading',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='26',MarginLeft='10',MarginRight='10',MarginTop='4',Brush='CalradiaForge.HeaderGold',Text='@CampaignRuleBuilderPropertiesLabel',**{'Brush.FontSize':'18'})
rule_right_scroll=E.SubElement(rule_right_children,'ScrollablePanel',Id='ForgeCampaignRuleRightScroll',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='10',MarginRight='12',MarginTop='32',MarginBottom='10',ClipRect='ForgeCampaignRuleRightClip',InnerPanel='ForgeCampaignRuleRightClip\\ForgeCampaignRuleRightContent',VerticalScrollbar='..\\ForgeCampaignRuleRightScrollbar')
rule_right_clip=E.SubElement(E.SubElement(rule_right_scroll,'Children'),'Widget',Id='ForgeCampaignRuleRightClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
rule_right_content=E.SubElement(E.SubElement(rule_right_clip,'Children'),'ListPanel',Id='ForgeCampaignRuleRightContent',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
rule_right_items=E.SubElement(rule_right_content,'Children')
E.SubElement(rule_right_items,'TextWidget',Id='ForgeCampaignRuleNoSelection',IsVisible='@CampaignRuleBuilderIsEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='42',MarginLeft='4',MarginRight='6',Brush='CalradiaForge.Muted',Text='@CampaignRuleBuilderEmptyLabel',ClipContents='true',**{'Brush.FontSize':'15'})

def rule_cycle_row(label_binding, value_binding, command, widget_id):
    is_event = widget_id == 'CampaignRuleEvent'
    row_height = '87' if is_event else '67'
    row=E.SubElement(rule_right_items,'Widget',Id=widget_id,IsVisible='@CampaignRuleBuilderHasSelection',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight=row_height,MarginBottom='3')
    row_children=E.SubElement(row,'Children')
    E.SubElement(row_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='20',MarginLeft='3',MarginRight='6',Brush='CalradiaForge.Muted',Text='@'+label_binding,**{'Brush.FontSize':'13'})
    value_label_binding = 'CampaignRuleBuilderSelectedEventDisplayLabel' if is_event else value_binding
    control=button(row_children,widget_id+'Cycle','@'+value_label_binding,460,height=38,margin_right=0)
    control.set('WidthSizePolicy','StretchToParent')
    control.set('MarginLeft','3')
    control.set('MarginRight','6')
    control.set('MarginTop','23')
    control.set('Command.Click',command)
    if is_event:
        E.SubElement(row_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='18',MarginLeft='3',MarginRight='6',MarginTop='64',Brush='CalradiaForge.Muted',Text='@CampaignRuleBuilderEventChangeHintLabel',ClipContents='true',**{'Brush.FontSize':'12'})

rule_cycle_row('CampaignRuleBuilderEventLabel','CampaignRuleBuilderSelectedEventLabel','ExecuteCampaignRuleBuilderCycleEvent','CampaignRuleEvent')
rule_cycle_row('CampaignRuleBuilderActionLabel','CampaignRuleBuilderSelectedActionLabel','ExecuteCampaignRuleBuilderCycleAction','CampaignRuleAction')
rule_cycle_row('CampaignRuleBuilderTargetLabel','CampaignRuleBuilderSelectedTargetLabel','ExecuteCampaignRuleBuilderCycleTarget','CampaignRuleTarget')
rule_amount=E.SubElement(rule_right_items,'Widget',Id='ForgeCampaignRuleAmountRow',IsVisible='@CampaignRuleBuilderHasSelection',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='67',MarginBottom='4')
rule_amount_children=E.SubElement(rule_amount,'Children')
E.SubElement(rule_amount_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='20',MarginLeft='3',MarginRight='6',Brush='CalradiaForge.Muted',Text='@CampaignRuleBuilderAmountLabel',**{'Brush.FontSize':'13'})
amount_frame=E.SubElement(rule_amount_children,'Widget',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='38',MarginLeft='3',MarginRight='6',MarginTop='23',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
amount_inner=E.SubElement(E.SubElement(amount_frame,'Children'),'Widget',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
E.SubElement(E.SubElement(amount_inner,'Children'),'EditableTextWidget',Id='ForgeCampaignRuleAmount',IsFocusable='true',UpdateTextOnTyping='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='8',MarginRight='8',Brush='GameTip.Text',Text='@CampaignRuleBuilderAmountText')

for group in ('A','B'):
    group_row=E.SubElement(rule_right_items,'Widget',Id='ForgeCampaignRuleGroup'+group,IsVisible='@CampaignRuleBuilderHasSelection',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginTop='6',MarginBottom='6')
    group_children=E.SubElement(group_row,'Children')
    heading=E.SubElement(group_children,'ListPanel',Id='ForgeCampaignRuleGroup'+group+'Heading',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='34',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
    heading_children=E.SubElement(heading,'Children')
    E.SubElement(heading_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='3',Brush='CalradiaForge.HeaderGold',Text='@CampaignRuleBuilderGroup'+group+'Label',VerticalAlignment='Center',**{'Brush.FontSize':'16'})
    add_condition=button(heading_children,'CampaignRuleAddCondition'+group,'@CampaignRuleBuilderAddConditionLabel',170,height=32,margin_right=5)
    add_condition.set('Command.Click','ExecuteCampaignRuleBuilderAddCondition'+group)
    add_condition.set('IsDisabled','@CampaignRuleBuilderCannotAddCondition'+group)
    list_panel=E.SubElement(group_children,'ListPanel',Id='ForgeCampaignRuleConditions'+group,DataSource='{CampaignRuleBuilderConditions'+group+'}',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginTop='38',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
    condition_template=E.SubElement(list_panel,'ItemTemplate')
    condition_row=E.SubElement(condition_template,'Widget',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='106',MarginBottom='5',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
    condition_children=E.SubElement(condition_row,'Children')
    E.SubElement(condition_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='25',MarginLeft='6',MarginRight='6',MarginTop='2',Brush='CalradiaForge.Gold',Text='@Summary',ClipContents='true',**{'Brush.FontSize':'13'})
    value_frame=E.SubElement(condition_children,'Widget',IsVisible='@IsNumeric',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='33',MarginLeft='6',MarginRight='6',MarginTop='28',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
    value_inner=E.SubElement(E.SubElement(value_frame,'Children'),'Widget',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
    E.SubElement(E.SubElement(value_inner,'Children'),'EditableTextWidget',Id='ForgeCampaignRuleConditionThreshold'+group,IsFocusable='true',UpdateTextOnTyping='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='6',MarginRight='6',Brush='GameTip.Text',Text='@Value')
    condition_actions=E.SubElement(E.SubElement(condition_children,'ListPanel',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='32',MarginLeft='6',MarginRight='6',MarginTop='68',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'}),'Children')
    for action,label,width,handler in [('Kind','CycleKindLabel',170,'ExecuteCycleKind'),('Remove','RemoveLabel',150,'ExecuteRemove')]:
        control=button(condition_actions,'CampaignRuleCondition'+group+action,'@'+label,width,height=30,margin_right=4)
        control.set('Command.Click',handler)

E.SubElement(rule_right_items,'TextWidget',Id='ForgeCampaignRulePreviewHeading',IsVisible='@CampaignRuleBuilderHasSelection',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='27',MarginLeft='3',MarginTop='4',Brush='CalradiaForge.HeaderGold',Text='@CampaignRuleBuilderPreviewHeadingLabel',**{'Brush.FontSize':'17'})
E.SubElement(rule_right_items,'TextWidget',Id='ForgeCampaignRulePreview',IsVisible='@CampaignRuleBuilderHasSelection',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginLeft='3',MarginRight='6',MarginTop='3',MarginBottom='10',Brush='GameTip.Text',Text='@CampaignRuleBuilderPreviewLabel',ClipContents='true',**{'Brush.FontSize':'15'})
scrollbar(rule_right_children,'ForgeCampaignRuleRightScrollbar',32,10,12)

rule_actions=horizontal_row('ForgeCampaignRuleActionDeck',visibility='IsCampaignRuleBuilderActive',bottom=99)
for command,label,width,handler in [('CampaignRuleSave','CampaignRuleBuilderSaveLabel',150,'ExecuteCampaignRuleBuilderSave'),('CampaignRuleValidate','CampaignRuleBuilderValidateLabel',150,'ExecuteCampaignRuleBuilderValidate'),('CampaignRuleGenerate','CampaignRuleBuilderGenerateLabel',150,'ExecuteCampaignRuleBuilderGenerate'),('CampaignRuleCopy','CampaignRuleBuilderCopyLabel',166,'ExecuteCampaignRuleBuilderCopy')]:
    control=button(rule_actions,command,'@'+label,width,height=42,brush='CalradiaForge.Primary' if command=='CampaignRuleGenerate' else 'CalradiaForge.TacticalButton',margin_right=8)
    control.set('Command.Click',handler)
    if command=='CampaignRuleGenerate':
        control.set('IsDisabled','@CampaignRuleBuilderCannotGenerate')
    elif command=='CampaignRuleCopy':
        control.set('IsDisabled','@CampaignRuleBuilderCannotCopy')
rule_status_group=E.SubElement(rule_actions,'ListPanel',Id='ForgeCampaignRuleStatusGroup',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='42',VerticalAlignment='Center',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
rule_status_children=E.SubElement(rule_status_group,'Children')
E.SubElement(rule_status_children,'TextWidget',Id='ForgeCampaignRuleDraftLoadState',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='20',MarginLeft='4',MarginRight='4',Brush='CalradiaForge.Muted',Text='@CampaignRuleBuilderDraftLoadStatusLabel',ClipContents='true',**{'Brush.FontSize':'12'})
E.SubElement(rule_status_children,'TextWidget',Id='ForgeCampaignRuleStatus',IsVisible='@CampaignRuleBuilderOperationStatusVisible',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='20',MarginLeft='4',MarginRight='4',Brush='CalradiaForge.Muted',Text='@CampaignRuleBuilderStatusLabel',ClipContents='true',**{'Brush.FontSize':'12'})
E.SubElement(children,'TextWidget',Id='ForgeNavigationFooter',IsVisible='@IsNavigationFooterVisible',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='27',MarginLeft='280',MarginRight='24',MarginBottom='14',VerticalAlignment='Bottom',Brush='CalradiaForge.Muted',Text='@NavigationLabel',**{'Brush.FontSize':'16'})
E.SubElement(children,'ImageWidget',Id='ForgeBottomBrassFrameRule',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='6',MarginLeft='24',MarginRight='24',MarginBottom='4',VerticalAlignment='Bottom',Sprite='forge_patina_brass',Color='#FFFFFFFF')

# Searchable SDK catalog. It shares the shell's left content column and has a
# viewport-aware height so it remains inside short game resolutions.
sdk_panel=E.SubElement(children,'Widget',Id='ForgeSdkCatalogPanel',IsVisible='@IsSdkCatalogOpen',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='520',MarginLeft='280',MarginTop='216',MarginBottom='180',HorizontalAlignment='Left',MaxHeight='640',Sprite='BlankWhiteSquare_9',Color='#17261FFF')
sdk_surface=E.SubElement(E.SubElement(sdk_panel,'Children'),'Widget',Id='ForgeSdkCatalogSurface',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
sdk_children=E.SubElement(sdk_surface,'Children')
sdk_header=E.SubElement(sdk_children,'Widget',Id='ForgeSdkCatalogHeader',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='44')
E.SubElement(E.SubElement(sdk_header,'Children'),'TextWidget',Id='ForgeSdkCatalogTitle',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='430',SuggestedHeight='30',MarginLeft='12',MarginTop='7',Brush='CalradiaForge.HeaderGold',Text='@SdkCatalogLabel',**{'Brush.FontSize':'22'})
sdk_close=button(E.SubElement(sdk_header,'Children'),'SdkCatalogClose','×',36,height=36,brush='CalradiaForge.TacticalButton',margin_right=8,hint='@SdkCatalogCloseHint')
sdk_close.set('HorizontalAlignment','Right')
sdk_close.set('VerticalAlignment','Center')
sdk_close.set('Command.Click','ExecuteCloseSdkCatalog')
search_frame=E.SubElement(sdk_children,'Widget',Id='ForgeSdkCatalogSearchFrame',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='40',MarginLeft='12',MarginRight='12',MarginTop='52',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
search_inner=E.SubElement(E.SubElement(search_frame,'Children'),'Widget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0A0D0BFF')
search_inner_children=E.SubElement(search_inner,'Children')
E.SubElement(search_inner_children,'EditableTextWidget',Id='ForgeSdkCatalogSearch',IsFocusable='true',UpdateTextOnTyping='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='10',MarginRight='10',Brush='GameTip.Text',Text='@SearchText',**{'Hint.HintText':'@SearchHint'})
E.SubElement(search_inner_children,'TextWidget',Id='ForgeSdkCatalogSearchPlaceholder',IsVisible='@IsSdkCatalogSearchEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='10',MarginRight='10',VerticalAlignment='Center',Brush='CalradiaForge.Muted',Text='@SdkCatalogSearchPlaceholder',**{'Brush.FontSize':'15'})
sdk_scroll=E.SubElement(sdk_children,'ScrollablePanel',Id='ForgeSdkCatalogScroll',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='12',MarginRight='12',MarginTop='104',MarginBottom='12',ClipRect='ForgeSdkCatalogClip',InnerPanel='ForgeSdkCatalogClip\\ForgeSdkCatalogTools',VerticalScrollbar='..\\ForgeSdkCatalogScrollBar')
sdk_scroll_children=E.SubElement(sdk_scroll,'Children')
sdk_clip=E.SubElement(sdk_scroll_children,'Widget',Id='ForgeSdkCatalogClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
sdk_list=E.SubElement(E.SubElement(sdk_clip,'Children'),'ListPanel',Id='ForgeSdkCatalogTools',DataSource='{SdkTools}',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
sdk_template=E.SubElement(sdk_list,'ItemTemplate')
sdk_entry=E.SubElement(sdk_template,'ButtonWidget',IsVisible='@IsVisible',IsSelected='@IsSelected',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='50',MarginBottom='4',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteSelect','Hint.HintText':'@Hint'})
sdk_entry_children=E.SubElement(sdk_entry,'Children')
E.SubElement(sdk_entry_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='23',MarginLeft='12',MarginRight='12',MarginTop='4',Brush='CalradiaForge.ButtonText',Text='@Title',**{'Brush.FontSize':'16'})
E.SubElement(sdk_entry_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='17',MarginLeft='12',MarginRight='12',MarginTop='27',Brush='CalradiaForge.Muted',Text='@Hint',**{'Brush.FontSize':'12'})
scrollbar(sdk_children,'ForgeSdkCatalogScrollBar',104,12,12)
sdk_empty=E.SubElement(sdk_children,'Widget',Id='ForgeSdkCatalogEmptyState',IsVisible='@IsSdkCatalogEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='12',MarginRight='12',MarginTop='104',MarginBottom='12')
E.SubElement(E.SubElement(sdk_empty,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='40',Brush='CalradiaForge.Muted',Text='@SdkCatalogEmptyLabel',HorizontalAlignment='Center',VerticalAlignment='Center',**{'Brush.FontSize':'16'})

# Command history sits over the primary action row. The selected entry only
# loads into the argument field; the user still chooses when to run it.
history_panel=E.SubElement(children,'Widget',Id='ForgeCommandHistoryPanel',IsVisible='@IsHistoryVisible',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='520',SuggestedHeight='210',MarginLeft='280',MarginTop='333',Sprite='BlankWhiteSquare_9',Color='#17261FFF')
history_surface=E.SubElement(E.SubElement(history_panel,'Children'),'Widget',Id='ForgeCommandHistorySurface',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
history_children=E.SubElement(history_surface,'Children')
history_header=E.SubElement(history_children,'Widget',Id='ForgeCommandHistoryHeader',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='44')
E.SubElement(E.SubElement(history_header,'Children'),'TextWidget',Id='ForgeCommandHistoryTitle',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='340',SuggestedHeight='30',MarginLeft='12',MarginTop='7',Brush='CalradiaForge.HeaderGold',Text='@HistoryTitleLabel',**{'Brush.FontSize':'19'})
clear_history=button(E.SubElement(history_header,'Children'),'ClearHistory','@ClearHistoryLabel',104,height=36,brush='CalradiaForge.TacticalButton',margin_right=52,hint='@ClearHistoryHint')
clear_history.set('HorizontalAlignment','Right')
clear_history.set('VerticalAlignment','Center')
close_history=button(E.SubElement(history_header,'Children'),'CommandHistoryClose','×',36,height=36,brush='CalradiaForge.TacticalButton',margin_right=8)
close_history.set('HorizontalAlignment','Right')
close_history.set('VerticalAlignment','Center')
close_history.set('Command.Click','ExecuteToggleHistory')
history_navigation=E.SubElement(history_children,'ListPanel',Id='ForgeHistoryNavigation',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='74',SuggestedHeight='36',MarginLeft='12',MarginTop='52',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
history_nav_children=E.SubElement(history_navigation,'Children')
history_prev=button(history_nav_children,'HistoryPrev','@HistoryPrevLabel',34,height=34,margin_right=6,hint='@HistoryPrevHint')
history_prev.set('Command.Click','ExecuteHistoryPrevious')
button(history_nav_children,'HistoryNext','@HistoryNextLabel',34,height=34,margin_right=0,hint='@HistoryNextHint')
history_scroll=E.SubElement(history_children,'ScrollablePanel',Id='ForgeCommandHistoryScroll',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='100',MarginRight='12',MarginTop='52',MarginBottom='12',ClipRect='ForgeCommandHistoryClip',InnerPanel='ForgeCommandHistoryClip\\ForgeCommandHistoryEntries',VerticalScrollbar='..\\ForgeCommandHistoryScrollBar')
history_scroll_children=E.SubElement(history_scroll,'Children')
history_clip=E.SubElement(history_scroll_children,'Widget',Id='ForgeCommandHistoryClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
history_list=E.SubElement(E.SubElement(history_clip,'Children'),'ListPanel',Id='ForgeCommandHistoryEntries',DataSource='{CommandHistoryList}',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
history_template=E.SubElement(history_list,'ItemTemplate')
history_entry=E.SubElement(history_template,'ButtonWidget',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='42',MarginBottom='4',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteLoad'})
E.SubElement(E.SubElement(history_entry,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='10',MarginRight='10',Brush='CalradiaForge.ButtonText',Text='@CommandText',VerticalAlignment='Center',**{'Brush.FontSize':'14'})
scrollbar(history_children,'ForgeCommandHistoryScrollBar',52,12,12)

# Category suggested commands flyout sits right over the action deck when open.
cmds_panel=E.SubElement(children,'Widget',Id='ForgeCategoryCommandsPanel',IsVisible='@IsCategoryCommandsOpen',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='660',SuggestedHeight='320',MarginLeft='280',MarginTop='333',Sprite='BlankWhiteSquare_9',Color='#17261FFF')
cmds_surface=E.SubElement(E.SubElement(cmds_panel,'Children'),'Widget',Id='ForgeCategoryCommandsSurface',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
cmds_children=E.SubElement(cmds_surface,'Children')
cmds_header=E.SubElement(cmds_children,'Widget',Id='ForgeCategoryCommandsHeader',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='38')
cmds_header_children=E.SubElement(cmds_header,'Children')
E.SubElement(cmds_header_children,'TextWidget',Id='ForgeCategoryCommandsTitle',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='440',SuggestedHeight='28',MarginLeft='12',MarginTop='5',Brush='CalradiaForge.HeaderGold',Text='@SuggestedCommandsTitle',**{'Brush.FontSize':'18'})
cmds_close=button(cmds_header_children,'CategoryCommandsClose','×',34,height=30,brush='CalradiaForge.TacticalButton',margin_right=8)
cmds_close.set('HorizontalAlignment','Right')
cmds_close.set('VerticalAlignment','Center')
cmds_close.set('Command.Click','ExecuteCloseCategoryCommands')

# Quick slots bar for direct execution
slots_bar=E.SubElement(cmds_children,'ListPanel',Id='ForgeQuickSlotsBar',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='30',MarginLeft='12',MarginRight='12',MarginTop='38',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
slots_bar_children=E.SubElement(slots_bar,'Children')
E.SubElement(slots_bar_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='80',Brush='CalradiaForge.Gold',Text='@QuickSlotsTitle',VerticalAlignment='Center',**{'Brush.FontSize':'12'})
for slot_num in (1,2,3):
    slot_btn=E.SubElement(slots_bar_children,'ButtonWidget',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='170',SuggestedHeight='28',MarginRight='6',Brush='CalradiaForge.TacticalButton',**{'Command.Click':f'ExecuteQuickSlot{slot_num}','Hint.HintText':f'@QuickSlot{slot_num}Hint'})
    E.SubElement(E.SubElement(slot_btn,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.ButtonText',Text=f'@QuickSlot{slot_num}Label',HorizontalAlignment='Center',VerticalAlignment='Center',**{'Brush.FontSize':'11'})

cmds_scroll=E.SubElement(cmds_children,'ScrollablePanel',Id='ForgeCategoryCommandsScroll',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='12',MarginRight='12',MarginTop='72',MarginBottom='8',ClipRect='ForgeCategoryCommandsClip',InnerPanel='ForgeCategoryCommandsClip\\ForgeCategoryCommandsEntries',VerticalScrollbar='..\\ForgeCategoryCommandsScrollBar')
cmds_scroll_children=E.SubElement(cmds_scroll,'Children')
cmds_clip=E.SubElement(cmds_scroll_children,'Widget',Id='ForgeCategoryCommandsClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
cmds_list=E.SubElement(E.SubElement(cmds_clip,'Children'),'ListPanel',Id='ForgeCategoryCommandsEntries',DataSource='{CategorySuggestedCommands}',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
cmds_template=E.SubElement(cmds_list,'ItemTemplate')
cmds_row=E.SubElement(cmds_template,'ListPanel',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='40',MarginBottom='3',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
cmds_row_children=E.SubElement(cmds_row,'Children')

cmd_pin_btn=E.SubElement(cmds_row_children,'ButtonWidget',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='34',SuggestedHeight='36',MarginRight='4',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteTogglePin','Hint.HintText':'@PinHint'})
E.SubElement(E.SubElement(cmd_pin_btn,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.Gold',Text='@PinLabel',HorizontalAlignment='Center',VerticalAlignment='Center',**{'Brush.FontSize':'17'})

cmd_slot_btn=E.SubElement(cmds_row_children,'ButtonWidget',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='34',SuggestedHeight='36',MarginRight='4',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteAssignSlot','Hint.HintText':'@SlotHint'})
E.SubElement(E.SubElement(cmd_slot_btn,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.Gold',Text='@SlotLabel',HorizontalAlignment='Center',VerticalAlignment='Center',**{'Brush.FontSize':'13'})

cmd_select_btn=E.SubElement(cmds_row_children,'ButtonWidget',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='36',MarginRight='4',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteSelect','Hint.HintText':'@HintText'})
cmd_select_children=E.SubElement(cmd_select_btn,'Children')
E.SubElement(cmd_select_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='148',MarginLeft='6',Brush='CalradiaForge.ButtonText',Text='@CommandText',VerticalAlignment='Center',**{'Brush.FontSize':'13'})
E.SubElement(cmd_select_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='56',MarginLeft='156',Brush='CalradiaForge.Gold',Text='@UtilityBadge',VerticalAlignment='Center',**{'Brush.FontSize':'11'})
E.SubElement(cmd_select_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='216',MarginRight='6',Brush='CalradiaForge.Muted',Text='@Description',VerticalAlignment='Center',**{'Brush.FontSize':'11'})

cmd_run_btn=E.SubElement(cmds_row_children,'ButtonWidget',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='58',SuggestedHeight='36',Brush='CalradiaForge.Primary',**{'Command.Click':'ExecuteRun'})
E.SubElement(E.SubElement(cmd_run_btn,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.PrimaryText',Text='@RunLabel',HorizontalAlignment='Center',VerticalAlignment='Center',**{'Brush.FontSize':'13'})

scrollbar(cmds_children,'ForgeCategoryCommandsScrollBar',72,8,12)

# A right-column help card stays clear of the SDK surface at the narrow profile.
key_help=E.SubElement(children,'Widget',Id='ForgeKeyHelpPanel',IsVisible='@IsKeyHelpOpen',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='320',SuggestedHeight='220',MarginRight='24',MarginTop='216',HorizontalAlignment='Right',Sprite='BlankWhiteSquare_9',Color='#17261FFF')
key_help_surface=E.SubElement(E.SubElement(key_help,'Children'),'Widget',Id='ForgeKeyHelpSurface',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
key_help_children=E.SubElement(key_help_surface,'Children')
E.SubElement(key_help_children,'TextWidget',Id='ForgeKeyHelpTitle',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='230',SuggestedHeight='30',MarginLeft='12',MarginTop='8',Brush='CalradiaForge.HeaderGold',Text='@KeyHelpLabel',**{'Brush.FontSize':'19'})
key_help_close=button(key_help_children,'KeyHelpClose','×',36,height=36,brush='CalradiaForge.TacticalButton',margin_right=8)
key_help_close.set('HorizontalAlignment','Right')
key_help_close.set('VerticalAlignment','Center')
key_help_close.set('MarginTop','5')
key_help_close.set('Command.Click','ExecuteToggleKeyHelp')
E.SubElement(key_help_children,'TextWidget',Id='ForgeKeyHelpPrimary',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='48',MarginLeft='12',MarginRight='12',MarginTop='56',Brush='CalradiaForge.Muted',Text='@KeyHelpPrimaryHint',**{'Brush.FontSize':'13'})
E.SubElement(key_help_children,'TextWidget',Id='ForgeKeyHelpSecondary',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='48',MarginLeft='12',MarginRight='12',MarginTop='112',Brush='CalradiaForge.Muted',Text='@KeyHelpSecondaryHint',**{'Brush.FontSize':'13'})

# Category Playbook & Troubleshooting Panel for detailed mode.
playbook_panel=E.SubElement(children,'Widget',Id='ForgePlaybookPanel',IsVisible='@IsPlaybookVisible',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='320',SuggestedHeight='390',MaxHeight='390',MarginRight='24',MarginTop='281',MarginBottom='166',HorizontalAlignment='Right',Sprite='BlankWhiteSquare_9',Color='#17261FFF')
playbook_surface=E.SubElement(E.SubElement(playbook_panel,'Children'),'Widget',Id='ForgePlaybookSurface',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
playbook_children=E.SubElement(playbook_surface,'Children')
playbook_header=E.SubElement(playbook_children,'Widget',Id='ForgePlaybookHeader',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='34')
playbook_header_children=E.SubElement(playbook_header,'Children')
playbook_close=button(playbook_header_children,'PlaybookClose','×',34,height=28,brush='CalradiaForge.TacticalButton',margin_right=8)
playbook_close.set('Command.Click','ExecuteToggleDetailMode')
playbook_close.set('HorizontalAlignment','Right')
playbook_close.set('VerticalAlignment','Center')

playbook_scroll=E.SubElement(playbook_children,'ScrollablePanel',Id='ForgePlaybookScroll',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='10',MarginTop='42',MarginRight='18',MarginBottom='10',ClipRect='ForgePlaybookClip',InnerPanel='ForgePlaybookClip\\ForgePlaybookContent',VerticalScrollbar='..\\ForgePlaybookScrollBar')
playbook_clip=E.SubElement(E.SubElement(playbook_scroll,'Children'),'Widget',Id='ForgePlaybookClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
playbook_content=E.SubElement(E.SubElement(playbook_clip,'Children'),'Widget',Id='ForgePlaybookContent',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',ClipContents='true')
playbook_flow=E.SubElement(E.SubElement(playbook_content,'Children'),'ListPanel',Id='ForgePlaybookFlow',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
playbook_content_children=E.SubElement(playbook_flow,'Children')
scrollbar(playbook_children,'ForgePlaybookScrollBar',42,10,10,8)
E.SubElement(playbook_content_children,'TextWidget',Id='ForgePlaybookTitle',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginLeft='12',MarginRight='12',MarginTop='4',Brush='CalradiaForge.HeaderGold',Text='@CategoryPlaybookTitle',**{'Brush.FontSize':'13'})
E.SubElement(playbook_content_children,'ImageWidget',Id='ForgePlaybookBrassRule1',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='3',MarginLeft='12',MarginRight='12',MarginTop='6',Sprite='forge_patina_brass',Color='#FFFFFFFF')

# Steps
E.SubElement(playbook_content_children,'TextWidget',Id='ForgePlaybookStep1',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginLeft='12',MarginRight='12',MarginTop='8',Brush='CalradiaForge.Gold',Text='@CategoryPlaybookStep1',**{'Brush.FontSize':'12'})
E.SubElement(playbook_content_children,'TextWidget',Id='ForgePlaybookStep2',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginLeft='12',MarginRight='12',MarginTop='6',Brush='GameTip.Text',Text='@CategoryPlaybookStep2',**{'Brush.FontSize':'12'})
E.SubElement(playbook_content_children,'TextWidget',Id='ForgePlaybookStep3',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginLeft='12',MarginRight='12',MarginTop='6',Brush='GameTip.Text',Text='@CategoryPlaybookStep3',**{'Brush.FontSize':'12'})

E.SubElement(playbook_content_children,'ImageWidget',Id='ForgePlaybookFeltRule',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='3',MarginLeft='12',MarginRight='12',MarginTop='8',Sprite='forge_pine_felt',Color='#FFFFFFFF')

# Troubleshooting
E.SubElement(playbook_content_children,'TextWidget',Id='ForgeTroubleshootingTitle',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginLeft='12',MarginRight='12',MarginTop='8',Brush='CalradiaForge.HeaderGold',Text='@CategoryTroubleshootingTitle',**{'Brush.FontSize':'12'})
E.SubElement(playbook_content_children,'TextWidget',Id='ForgeTroubleshootingAdvice',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginLeft='12',MarginRight='12',MarginTop='4',Brush='CalradiaForge.Muted',Text='@CategoryTroubleshootingAdvice',**{'Brush.FontSize':'11'})

E.SubElement(playbook_content_children,'ImageWidget',Id='ForgePlaybookBrassRule2',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='3',MarginLeft='12',MarginRight='12',MarginTop='8',Sprite='forge_patina_brass',Color='#FFFFFFFF')

# Macro section
E.SubElement(playbook_content_children,'TextWidget',Id='ForgeRecommendedMacro',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginLeft='12',MarginRight='12',MarginTop='8',Brush='CalradiaForge.Gold',Text='@CategoryRecommendedMacro',**{'Brush.FontSize':'11'})

macro_btn=E.SubElement(playbook_content_children,'ButtonWidget',Id='ForgeRunMacro',IsFocusable='true',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='36',MarginLeft='12',MarginRight='12',MarginTop='8',MarginBottom='12',Brush='CalradiaForge.Primary',**{'Command.Click':'ExecuteRunMacro','Hint.HintText':'@MacroActionHint'})
E.SubElement(E.SubElement(macro_btn,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.PrimaryText',Text='@MacroActionLabel',HorizontalAlignment='Center',VerticalAlignment='Center',**{'Brush.FontSize':'12'})

# The toast occupies the reserved footer slot while the footer itself is hidden.
toast=E.SubElement(children,'Widget',Id='ForgeStatusToast',IsVisible='@IsToastVisible',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='600',SuggestedHeight='34',HorizontalAlignment='Center',VerticalAlignment='Bottom',MarginLeft='280',MarginRight='24',MarginBottom='12',Sprite='BlankWhiteSquare_9',Color='#27392FFF')
E.SubElement(E.SubElement(toast,'Children'),'TextWidget',Id='ForgeStatusToastMessage',IsVisible='@IsToastVisible',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='12',MarginRight='12',Brush='CalradiaForge.Gold',Text='@ToastMessage',HorizontalAlignment='Center',VerticalAlignment='Center',**{'Brush.FontSize':'15'})

# Global route and SDK-tool navigation stays above the page content. The result
# list is keyboard-driven by the view model; the separate marker shows focus
# without reusing the active-route selection state.
# The structured test results overlay is presentation only; opening it or
# selecting a row never calls the test runner.
results_overlay=E.SubElement(children,'Widget',Id='TestResultsExplorerOverlay',IsVisible='@IsTestResultsExplorerOpen',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Sprite='BlankWhiteSquare_9',Color='#000000D8')
results_panel=E.SubElement(E.SubElement(results_overlay,'Children'),'Widget',Id='TestResultsExplorerPanel',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',SuggestedWidth='1120',SuggestedHeight='760',MaxWidth='1160',MaxHeight='820',HorizontalAlignment='Center',VerticalAlignment='Center',MarginLeft='30',MarginRight='30',MarginTop='30',MarginBottom='30',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
results_surface=E.SubElement(E.SubElement(results_panel,'Children'),'Widget',Id='TestResultsExplorerSurface',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
results_children=E.SubElement(results_surface,'Children')
results_header=E.SubElement(results_children,'Widget',Id='TestResultsExplorerHeader',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='42',MarginLeft='20',MarginRight='20',MarginTop='12')
results_header_children=E.SubElement(results_header,'Children')
E.SubElement(results_header_children,'TextWidget',Id='TestResultsExplorerHeading',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginRight='54',Brush='CalradiaForge.HeaderGold',Text='@TestResultsExplorerTitle',VerticalAlignment='Center',**{'Brush.FontSize':'25'})
results_close=E.SubElement(results_header_children,'ButtonWidget',Id='TestResultsExplorerClose',IsFocusable='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='40',SuggestedHeight='36',HorizontalAlignment='Right',VerticalAlignment='Center',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteCloseTestResultsExplorer','Hint.HintText':'@TestResultsExplorerCloseLabel'})
E.SubElement(E.SubElement(results_close,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.ButtonText',Text='×',HorizontalAlignment='Center',VerticalAlignment='Center')
E.SubElement(results_children,'TextWidget',Id='TestResultsExplorerSummary',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='30',MarginLeft='20',MarginRight='20',MarginTop='54',Brush='CalradiaForge.Muted',Text='@TestResultsExplorerSummary',VerticalAlignment='Top',**{'Brush.FontSize':'15'})

results_content=E.SubElement(results_children,'ListPanel',Id='TestResultsExplorerContent',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='20',MarginRight='20',MarginTop='92',MarginBottom='18',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
results_content_children=E.SubElement(results_content,'Children')
results_list_frame=E.SubElement(results_content_children,'Widget',Id='TestResultsExplorerListFrame',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='424',Sprite='BlankWhiteSquare_9',Color='#14211CFF')
results_list_children=E.SubElement(results_list_frame,'Children')
results_columns=E.SubElement(results_list_children,'ListPanel',Id='TestResultsExplorerColumns',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='28',MarginLeft='10',MarginRight='10',MarginTop='8',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
results_column_children=E.SubElement(results_columns,'Children')
E.SubElement(results_column_children,'TextWidget',Id='TestResultsExplorerTestIdHeader',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.Gold',Text='@TestResultsExplorerTestIdLabel',VerticalAlignment='Center',**{'Brush.FontSize':'13'})
E.SubElement(results_column_children,'TextWidget',Id='TestResultsExplorerStatusHeader',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='90',ClipContents='true',Brush='CalradiaForge.Gold',Text='@TestResultsExplorerStatusLabel',VerticalAlignment='Center',**{'Brush.FontSize':'13'})
E.SubElement(results_column_children,'TextWidget',Id='TestResultsExplorerDurationHeader',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='96',ClipContents='true',Brush='CalradiaForge.Gold',Text='@TestResultsExplorerDurationLabel',VerticalAlignment='Center',**{'Brush.FontSize':'13'})
results_scroll=E.SubElement(results_list_children,'ScrollablePanel',Id='TestResultsExplorerScroll',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='8',MarginRight='8',MarginTop='42',MarginBottom='8',ClipRect='TestResultsExplorerClip',InnerPanel='TestResultsExplorerClip\\TestResults',VerticalScrollbar='..\\TestResultsExplorerScrollBar')
results_scroll_children=E.SubElement(results_scroll,'Children')
results_clip=E.SubElement(results_scroll_children,'Widget',Id='TestResultsExplorerClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
results_list=E.SubElement(E.SubElement(results_clip,'Children'),'ListPanel',Id='TestResults',DataSource='{TestResults}',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
results_template=E.SubElement(results_list,'ItemTemplate')
results_row=E.SubElement(results_template,'ButtonWidget',IsFocusable='true',IsSelected='@IsSelected',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='54',MarginBottom='4',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteSelect'})
results_row_children=E.SubElement(results_row,'Children')
E.SubElement(results_row_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='24',MarginLeft='10',MarginRight='10',MarginTop='3',Brush='CalradiaForge.ButtonText',Text='@ResultId',VerticalAlignment='Center',**{'Brush.FontSize':'14'})
# Reserve a wider visual gutter between the potentially untrusted status text
# and the fixed duration column; ClipContents prevents long statuses bleeding.
E.SubElement(results_row_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='20',MarginLeft='10',MarginRight='200',MarginTop='28',Brush='CalradiaForge.Muted',Text='@Status',VerticalAlignment='Center',ClipContents='true',**{'Brush.FontSize':'12'})
E.SubElement(results_row_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='168',SuggestedHeight='20',MarginRight='10',MarginTop='28',HorizontalAlignment='Right',Brush='CalradiaForge.Gold',Text='@DurationText',VerticalAlignment='Center',ClipContents='true',**{'Brush.FontSize':'12'})
scrollbar(results_list_children,'TestResultsExplorerScrollBar',42,8,2)

results_detail=E.SubElement(results_content_children,'Widget',Id='TestResultsExplorerDetailFrame',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='14',Sprite='BlankWhiteSquare_9',Color='#14211CFF')
results_detail_children=E.SubElement(results_detail,'Children')
E.SubElement(results_detail_children,'TextWidget',Id='TestResultsExplorerSelectedHeading',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='36',MarginLeft='16',MarginRight='16',MarginTop='12',Brush='CalradiaForge.HeaderGold',Text='@SelectedTestResultHeading',VerticalAlignment='Top',**{'Brush.FontSize':'19'})
detail_scroll=E.SubElement(results_detail_children,'ScrollablePanel',Id='TestResultsExplorerDetailScroll',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='14',MarginRight='12',MarginTop='54',MarginBottom='12',ClipRect='TestResultsExplorerDetailClip',InnerPanel='TestResultsExplorerDetailClip\\TestResultsExplorerDetailContent',VerticalScrollbar='..\\TestResultsExplorerDetailScrollBar')
detail_scroll_children=E.SubElement(detail_scroll,'Children')
detail_clip=E.SubElement(detail_scroll_children,'Widget',Id='TestResultsExplorerDetailClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
detail_content=E.SubElement(E.SubElement(detail_clip,'Children'),'Widget',Id='TestResultsExplorerDetailContent',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren')
detail_content_children=E.SubElement(detail_content,'Children')
E.SubElement(detail_content_children,'TextWidget',Id='SelectedTestResultDetail',IsVisible='@HasTestResults',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',MarginLeft='4',MarginRight='8',Brush='GameTip.Text',Text='@SelectedTestResultDetail',ClipContents='true',**{'Brush.FontSize':'15'})
E.SubElement(results_detail_children,'TextWidget',Id='TestResultsExplorerDetailEmpty',IsVisible='@IsTestResultsExplorerEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='14',MarginRight='12',MarginTop='54',MarginBottom='12',Brush='CalradiaForge.Muted',Text='@TestResultsExplorerEmptyDetail',HorizontalAlignment='Center',VerticalAlignment='Center',ClipContents='true',**{'Brush.FontSize':'16'})
E.SubElement(results_list_children,'TextWidget',Id='TestResultsExplorerEmptyMessage',IsVisible='@IsTestResultsExplorerEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='8',MarginRight='8',MarginTop='42',MarginBottom='8',Brush='CalradiaForge.Gold',Text='@TestResultsExplorerEmptyLabel',HorizontalAlignment='Center',VerticalAlignment='Center',ClipContents='true',**{'Brush.FontSize':'18'})
scrollbar(results_detail_children,'TestResultsExplorerDetailScrollBar',54,12,4)


hook_picker_overlay=E.SubElement(children,'Widget',Id='ForgeHookPickerOverlay',IsVisible='@IsHookPickerOpen',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Sprite='BlankWhiteSquare_9',Color='#000000C8')
hook_picker_panel=E.SubElement(E.SubElement(hook_picker_overlay,'Children'),'Widget',Id='ForgeHookPickerPanel',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',SuggestedWidth='980',SuggestedHeight='780',MaxWidth='1100',MaxHeight='860',HorizontalAlignment='Center',VerticalAlignment='Center',MarginLeft='42',MarginRight='42',MarginTop='42',MarginBottom='42',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
hook_picker_surface=E.SubElement(E.SubElement(hook_picker_panel,'Children'),'Widget',Id='ForgeHookPickerSurface',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
hook_picker_children=E.SubElement(hook_picker_surface,'Children')
hook_picker_header=E.SubElement(hook_picker_children,'Widget',Id='ForgeHookPickerHeader',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='48',MarginLeft='22',MarginRight='22',MarginTop='16')
hook_picker_header_children=E.SubElement(hook_picker_header,'Children')
E.SubElement(hook_picker_header_children,'TextWidget',Id='ForgeHookPickerTitle',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginRight='216',Brush='CalradiaForge.HeaderGold',Text='@HookPickerTitleLabel',VerticalAlignment='Center',**{'Brush.FontSize':'25'})
hook_picker_next=E.SubElement(hook_picker_header_children,'ButtonWidget',Id='ForgeHookPickerNext',IsFocusable='true',IsDisabled='@IsHookSelectionDisabled',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='150',SuggestedHeight='42',HorizontalAlignment='Right',VerticalAlignment='Center',MarginRight='52',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteHookNext','Hint.HintText':'@HookNextLabel'})
E.SubElement(E.SubElement(hook_picker_next,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.ButtonText',Text='@HookNextLabel',HorizontalAlignment='Center',VerticalAlignment='Center',**{'Brush.FontSize':'14'})
hook_picker_close=E.SubElement(hook_picker_header_children,'ButtonWidget',Id='ForgeHookPickerClose',IsFocusable='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='46',SuggestedHeight='42',HorizontalAlignment='Right',VerticalAlignment='Center',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteHookClosePicker','Hint.HintText':'@HookCancelLabel'})
E.SubElement(E.SubElement(hook_picker_close,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.ButtonText',Text='×',HorizontalAlignment='Center',VerticalAlignment='Center',**{'Brush.FontSize':'22'})
hook_picker_scroll=E.SubElement(hook_picker_children,'ScrollablePanel',Id='ForgeHookPickerScroll',IsVisible='@IsHookPickerHasItems',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='22',MarginRight='32',MarginTop='78',MarginBottom='24',ClipRect='ForgeHookPickerClip',InnerPanel='ForgeHookPickerClip\\ForgeHookPickerItems',VerticalScrollbar='..\\ForgeHookPickerScrollBar')
hook_picker_scroll_children=E.SubElement(hook_picker_scroll,'Children')
hook_picker_clip=E.SubElement(hook_picker_scroll_children,'Widget',Id='ForgeHookPickerClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
hook_picker_list=E.SubElement(E.SubElement(hook_picker_clip,'Children'),'ListPanel',Id='ForgeHookPickerItems',DataSource='{HookPickerItems}',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
hook_picker_template=E.SubElement(hook_picker_list,'ItemTemplate')
hook_picker_row=E.SubElement(hook_picker_template,'ButtonWidget',IsFocusable='true',IsSelected='@IsSelected',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='66',MarginBottom='5',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteSelect'})
hook_picker_row_children=E.SubElement(hook_picker_row,'Children')
E.SubElement(hook_picker_row_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='27',MarginLeft='14',MarginRight='14',MarginTop='5',Brush='CalradiaForge.HeaderGold',Text='@Id',VerticalAlignment='Center',**{'Brush.FontSize':'17'})
E.SubElement(hook_picker_row_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='25',MarginLeft='14',MarginRight='14',MarginTop='34',Brush='CalradiaForge.Muted',Text='@Details',VerticalAlignment='Center',ClipContents='true',**{'Brush.FontSize':'13'})
scrollbar(hook_picker_children,'ForgeHookPickerScrollBar',78,24,22,10)
hook_picker_empty=E.SubElement(hook_picker_children,'Widget',Id='ForgeHookPickerEmptyState',IsVisible='@IsHookPickerEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='22',MarginRight='32',MarginTop='78',MarginBottom='24')
E.SubElement(E.SubElement(hook_picker_empty,'Children'),'TextWidget',Id='ForgeHookPickerEmptyMessage',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='56',Brush='CalradiaForge.Muted',Text='@HookPickerEmptyLabel',HorizontalAlignment='Center',VerticalAlignment='Center',**{'Brush.FontSize':'17'})

palette_overlay=E.SubElement(children,'Widget',Id='NavigationPaletteOverlay',IsVisible='@IsNavigationPaletteOpen',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Sprite='BlankWhiteSquare_9',Color='#000000B8')
palette_panel=E.SubElement(E.SubElement(palette_overlay,'Children'),'Widget',Id='NavigationPalettePanel',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',SuggestedWidth='920',SuggestedHeight='760',MaxWidth='980',MaxHeight='780',HorizontalAlignment='Center',VerticalAlignment='Center',MarginLeft='48',MarginRight='48',MarginTop='48',MarginBottom='48',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
palette_surface=E.SubElement(E.SubElement(palette_panel,'Children'),'Widget',Id='NavigationPaletteSurface',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0D1511FF')
palette_children=E.SubElement(palette_surface,'Children')
palette_header=E.SubElement(palette_children,'Widget',Id='NavigationPaletteHeader',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='38',MarginLeft='20',MarginRight='20',MarginTop='12')
palette_header_children=E.SubElement(palette_header,'Children')
E.SubElement(palette_header_children,'TextWidget',Id='NavigationPaletteHeading',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginRight='54',Brush='CalradiaForge.HeaderGold',Text='@NavigationPaletteTitleLabel',VerticalAlignment='Center',**{'Brush.FontSize':'25'})
palette_close=E.SubElement(palette_header_children,'ButtonWidget',Id='NavigationPaletteClose',IsFocusable='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='40',SuggestedHeight='36',MarginRight='0',HorizontalAlignment='Right',VerticalAlignment='Center',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteCloseNavigationPalette','Hint.HintText':'@NavigationPaletteNavigationHint'})
E.SubElement(E.SubElement(palette_close,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.ButtonText',Text='×',HorizontalAlignment='Center',VerticalAlignment='Center')
palette_context=E.SubElement(palette_children,'Widget',Id='NavigationPaletteContext',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='24',MarginLeft='20',MarginRight='20',MarginTop='54')
palette_context_children=E.SubElement(palette_context,'Children')
E.SubElement(palette_context_children,'TextWidget',Id='NavigationPaletteActiveRoute',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginRight='190',Brush='CalradiaForge.Muted',Text='@NavigationPaletteActiveRouteLabel',VerticalAlignment='Center',**{'Brush.FontSize':'15'})
E.SubElement(palette_context_children,'TextWidget',Id='NavigationPaletteActiveGroup',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='StretchToParent',SuggestedWidth='180',Brush='CalradiaForge.Gold',Text='@NavigationPaletteActiveGroupLabel',HorizontalAlignment='Right',VerticalAlignment='Center',**{'Brush.FontSize':'14'})
palette_search_frame=E.SubElement(palette_children,'Widget',Id='NavigationPaletteSearchFrame',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='46',MarginLeft='20',MarginRight='20',MarginTop='88',Sprite='BlankWhiteSquare_9',Color='#C7A45AFF')
palette_search_inner=E.SubElement(E.SubElement(palette_search_frame,'Children'),'Widget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='1',MarginRight='1',MarginTop='1',MarginBottom='1',Sprite='BlankWhiteSquare_9',Color='#0A0D0BFF')
palette_search_children=E.SubElement(palette_search_inner,'Children')
E.SubElement(palette_search_children,'EditableTextWidget',Id='ForgeNavigationPaletteSearch',IsFocusable='true',UpdateTextOnTyping='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='10',MarginRight='10',Brush='GameTip.Text',Text='@NavigationPaletteSearchText',**{'Hint.HintText':'@NavigationPaletteSearchPlaceholder'})
E.SubElement(palette_search_children,'TextWidget',Id='NavigationPaletteSearchPlaceholder',IsVisible='@IsNavigationPaletteSearchEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='10',MarginRight='10',Brush='CalradiaForge.Muted',Text='@NavigationPaletteSearchPlaceholder',VerticalAlignment='Center',**{'Brush.FontSize':'16'})
palette_scroll=E.SubElement(palette_children,'ScrollablePanel',Id='NavigationPaletteScroll',IsVisible='@NavigationPaletteHasResults',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',AutoHideScrollBars='true',MarginLeft='20',MarginRight='20',MarginTop='146',MarginBottom='50',ClipRect='NavigationPaletteClip',InnerPanel='NavigationPaletteClip\\NavigationPaletteItems',VerticalScrollbar='..\\NavigationPaletteScrollBar')
palette_scroll_children=E.SubElement(palette_scroll,'Children')
palette_clip=E.SubElement(palette_scroll_children,'Widget',Id='NavigationPaletteClip',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',ClipContents='true')
palette_list=E.SubElement(E.SubElement(palette_clip,'Children'),'ListPanel',Id='NavigationPaletteItems',DataSource='{NavigationPaletteResults}',WidthSizePolicy='StretchToParent',HeightSizePolicy='CoverChildren',**{'StackLayout.LayoutMethod':'VerticalTopToBottom'})
palette_template=E.SubElement(palette_list,'ItemTemplate')
palette_row=E.SubElement(palette_template,'ListPanel',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='72',MarginBottom='5',**{'StackLayout.LayoutMethod':'HorizontalLeftToRight'})
palette_row_children=E.SubElement(palette_row,'Children')
palette_result=E.SubElement(palette_row_children,'ButtonWidget',IsVisible='@IsVisible',IsFocusable='true',IsSelected='@IsSelected',DoNotPassEventsToChildren='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='68',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteSelect'})
palette_result_children=E.SubElement(palette_result,'Children')
E.SubElement(palette_result_children,'Widget',IsVisible='@IsKeyboardFocused',DoNotAcceptEvents='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='4',SuggestedHeight='42',MarginLeft='3',Sprite='BlankWhiteSquare_9',Color='#E1C177FF',VerticalAlignment='Center')
E.SubElement(palette_result_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='23',MarginLeft='15',MarginRight='10',MarginTop='5',Brush='CalradiaForge.HeaderGold',Text='@Title',VerticalAlignment='Center',**{'Brush.FontSize':'17'})
E.SubElement(palette_result_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='148',SuggestedHeight='27',MarginLeft='15',MarginTop='34',Brush='CalradiaForge.Gold',Text='@GroupLabel',VerticalAlignment='Center',**{'Brush.FontSize':'13'})
E.SubElement(palette_result_children,'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='27',MarginLeft='166',MarginRight='10',MarginTop='34',Brush='CalradiaForge.Muted',Text='@Description',VerticalAlignment='Center',**{'Brush.FontSize':'13'})
palette_favorite=E.SubElement(palette_row_children,'ButtonWidget',IsVisible='@CanFavorite',IsFocusable='true',DoNotPassEventsToChildren='true',WidthSizePolicy='Fixed',HeightSizePolicy='Fixed',SuggestedWidth='48',SuggestedHeight='68',MarginLeft='6',Brush='CalradiaForge.TacticalButton',**{'Command.Click':'ExecuteToggleFavorite','Hint.HintText':'@FavoriteHint'})
E.SubElement(E.SubElement(palette_favorite,'Children'),'TextWidget',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',Brush='CalradiaForge.Gold',Text='@FavoriteLabel',HorizontalAlignment='Center',VerticalAlignment='Center',**{'Brush.FontSize':'21'})
scrollbar(palette_children,'NavigationPaletteScrollBar',146,50,20)
palette_empty=E.SubElement(palette_children,'Widget',Id='NavigationPaletteEmptyState',IsVisible='@IsNavigationPaletteEmpty',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='StretchToParent',MarginLeft='20',MarginRight='20',MarginTop='146',MarginBottom='50')
E.SubElement(E.SubElement(palette_empty,'Children'),'TextWidget',Id='NavigationPaletteEmptyMessage',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='54',Brush='CalradiaForge.Muted',Text='@NavigationPaletteEmptyLabel',HorizontalAlignment='Center',VerticalAlignment='Center',**{'Brush.FontSize':'17'})
E.SubElement(palette_children,'TextWidget',Id='NavigationPaletteKeyboardHint',DoNotAcceptEvents='true',WidthSizePolicy='StretchToParent',HeightSizePolicy='Fixed',SuggestedHeight='28',MarginLeft='20',MarginRight='20',MarginBottom='12',VerticalAlignment='Bottom',Brush='CalradiaForge.Muted',Text='@NavigationPaletteNavigationHint',**{'Brush.FontSize':'14'})

write_xml(ROOT/'modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml',root)

if PREFAB_ONLY:
    print('Generated the Gauntlet prefab; refreshing generated language catalogs without touching sprite assets.')
elif GAUNTLET_ONLY:
    print('Generated Calradia Forge Gauntlet textures and prefab.')
else:
    print('Generated manifests and the Calradia Forge Gauntlet prefab.')

if GAUNTLET_ONLY:
    sys.exit(0)

# The embedded catalog and Bannerlord translation files share the same English keys.
catalogs=[]
english_catalog={entry.attrib['key']:entry.attrib['value'] for entry in E.parse(ROOT/'localization'/'en.xml').getroot()}
for language,folder,display in [('en','EN','English'),('es','SP','Español (LA)')]:
    catalog={entry.attrib['key']:entry.attrib['value'] for entry in E.parse(ROOT/'localization'/f'{language}.xml').getroot()}
    if set(catalog) != set(english_catalog):
        raise ValueError('Desktop source catalog does not match English keys: '+language)
    catalogs.append((language,folder,display,catalog))
menu=json.loads((ROOT/'localization/native-menu.json').read_text(encoding='utf-8'))
for language in menu['languages']:
    if len(language['values'])!=len(menu['keys']) or any(not value.strip() for value in language['values']):
        raise ValueError('Incomplete native menu: '+language['id'])
    catalog=dict(english_catalog)
    catalog.update(dict(zip(menu['keys'],language['values'])))
    catalogs.append((language['iso'],language['folder'],language['id'],catalog))
briefing=json.loads((ROOT/'localization/native-briefing.json').read_text(encoding='utf-8'))
hook_labels=json.loads((ROOT/'localization/hook-workbench.json').read_text(encoding='utf-8'))
for key,translations in hook_labels.items():
    if translations.get('en')!=key or set(translations)!=set(SUPPORTED_LOCALIZATION_LANGUAGES) or any(not value.strip() for value in translations.values()):
        raise ValueError('Incomplete hook workbench localization: '+key)
    english_catalog[key]=translations['en']
    for language,folder,display,catalog in catalogs:
        catalog[key]=translations[language]
english_catalog.update({key:translations['en'] for key,translations in briefing.items()})
for language,folder,display,catalog in catalogs:
    catalog.update({key:translations[language] for key,translations in briefing.items()})
game_help=json.loads((ROOT/'localization/in-game-help.json').read_text(encoding='utf-8'))
english_catalog.update({key:translations['en'] for key,translations in game_help.items()})
for language,folder,display,catalog in catalogs:
    catalog.update({key:translations.get(language,translations['en']) for key,translations in game_help.items()})
navigation_languages={language for language,folder,display,catalog in catalogs}
if navigation_languages!=set(SUPPORTED_LOCALIZATION_LANGUAGES):
    raise ValueError('Source catalogs do not match the supported localization languages.')
english_catalog.update({source:localized['en'] for source,localized in navigation_palette.items()})
english_catalog.update({source:localized['en'] for source,localized in gauntlet_composer.items()})
output_filter_keys=(
    'Evidence',
    'Filter current output without changing the tool argument.',
    'Filter output lines...',
    'Clear output filter.',
    'No output lines match this filter.',
    'Pin output baseline',
    'Compare outputs',
    'Clear output baseline',
    'Baseline output',
    'Current output',
    'Output baseline pinned.',
    'Pin an output baseline before comparing.',
    'No current output to compare.',
    'No output matches the filter on either side.',
    'Output comparison unavailable because a configured input or work limit was reached.',
    'Output exceeds comparison limits; the baseline was not changed.',
    'Show current output',
    'Pin current output as the comparison baseline.',
    'Show the baseline and current outputs side by side.',
    'Return to the current output without removing the baseline.',
    'Clear the pinned output baseline.',
    'Output baseline cleared.',
)
test_results_explorer_keys=(
    'Test results',
    'Recent test results',
    'Open test results',
    'Close test results',
    'No test results are available.',
    'Test result details',
    'Started',
    'Steps',
    'Cleanup error',
    'Showing {0} results from the latest test response.',
    'Select a result to inspect its details.',
    'Test ID',
    'Status',
    'Duration',
    'Seed',
    'Context',
    'Error',
)
for language,folder,display,catalog in catalogs:
    catalog.update({source:localized[language] for source,localized in navigation_palette.items()})
    catalog.update({source:localized[language] for source,localized in gauntlet_composer.items()})
    source_catalog={entry.attrib['key']:entry.attrib['value'] for entry in E.parse(ROOT/'localization'/f'{language}.xml').getroot()}
    for key in output_filter_keys:
        translated=source_catalog.get(key)
        if not translated or (language=='en' and translated!=key):
            raise ValueError(f'Incomplete in-game Gauntlet label translation for {language}: {key}')
        catalog[key]=translated
    for key in test_results_explorer_keys:
        translated=source_catalog.get(key)
        if not translated or (language=='en' and translated!=key):
            raise ValueError(f'Incomplete test-results explorer translation for {language}: {key}')
        catalog[key]=translated
    strings=E.Element('base',type='string')
    E.SubElement(E.SubElement(strings,'tags'),'tag',language=display)
    target=E.SubElement(strings,'strings')
    for key,value in catalog.items():
        identifier='forge_'+hashlib.sha256(english_catalog[key].encode()).hexdigest()[:12]
        E.SubElement(target,'string',id=identifier,text=value)
    base=ROOT/'modules/CalradiaForge/ModuleData/Languages'
    write_xml(base/folder/'forge_strings.xml',strings, encoding='utf-8-sig')
    data=E.Element('LanguageData',id=display,name=display,supported_iso=language,under_development='false')
    E.SubElement(data,'LanguageFile',xml_path=folder+'/forge_strings.xml')
    write_xml(base/folder/'language_data.xml',data, encoding='utf-8-sig')



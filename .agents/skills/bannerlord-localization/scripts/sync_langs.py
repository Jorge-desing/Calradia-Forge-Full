import os
import re

languages = {
    'BR': 'Português (BR)',
    'CNs': '简体中文',
    'CNt': '繁體中文',
    'DE': 'Deutsch',
    'EN': 'English',
    'FR': 'Français',
    'IT': 'Italiano',
    'JP': '日本語',
    'KO': '한국어',
    'PL': 'Polski',
    'RU': 'Русский',
    'SP': 'Español (LA)',
    'TR': 'Türkçe'
}

base_dir = r'c:\Users\Alex\Documents\Mod Desarrolladores\modules\CalradiaForge\ModuleData\Languages'

en_file = os.path.join(base_dir, 'EN', 'forge_strings.xml')
en_xml = open(en_file, 'r', encoding='utf-8').read()
en_strings = dict(re.findall(r'<string id="([^"]+)" text="([^"]+)" />', en_xml))

for code, lang_name in languages.items():
    lang_file = os.path.join(base_dir, code, 'forge_strings.xml')
    existing_strings = {}
    if os.path.exists(lang_file):
        existing_xml = open(lang_file, 'r', encoding='utf-8').read()
        existing_strings = dict(re.findall(r'<string id="([^"]+)" text="([^"]+)" />', existing_xml))
    
    out = [f"<?xml version='1.0' encoding='utf-8'?>", '<base type="string">', '  <tags>', f'    <tag language="{lang_name}" />', '  </tags>', '  <strings>']
    
    for string_id, en_text in en_strings.items():
        text = existing_strings.get(string_id, en_text)
        text = text.replace(r'\n', '&#10;')
        out.append(f'    <string id="{string_id}" text="{text}" />')
        
    out.append("  </strings>")
    out.append("</base>")
    
    # write with BOM
    os.makedirs(os.path.dirname(lang_file), exist_ok=True)
    with open(lang_file, 'w', encoding='utf-8-sig') as f:
        f.write('\n'.join(out))
    
    # ensure language_data.xml exists
    ld_file = os.path.join(base_dir, code, 'language_data.xml')
    if not os.path.exists(ld_file):
        with open(ld_file, 'w', encoding='utf-8-sig') as f:
            f.write(f"<?xml version='1.0' encoding='utf-8'?>\n<LanguageData id=\"{lang_name}\" name=\"{lang_name}\" supported_iso=\"\" under_development=\"false\"><LanguageFile xml_path=\"{code}/forge_strings.xml\" /></LanguageData>")

print("Synced all languages.")

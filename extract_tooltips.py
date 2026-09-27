import re
import json

xaml_path = r'src\CalradiaForge.Desktop\MainWindow.xaml'
with open(xaml_path, 'r', encoding='utf-8') as f:
    xaml = f.read()

matches = re.findall(r'TreeViewItem.*?Tag=\x22(.*?)\x22.*?ToolTip=\x22(.*?)\x22', xaml, re.IGNORECASE)

with open('tooltips_en.json', 'w', encoding='utf-8') as f:
    json.dump({tag: tooltip for tag, tooltip in matches}, f, indent=4)
print(f"Extracted {len(matches)} tooltips to tooltips_en.json")

import re
data = open('modules/CalradiaForge/ModuleData/Languages/SP/forge_strings.xml', 'r', encoding='utf-8').read()
ids = re.findall(r'id="([^"]+)"', data)
dups = set([x for x in ids if ids.count(x) > 1])
print('Duplicates:', dups)

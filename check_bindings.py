import re

with open('modules/CalradiaForge/GUI/Prefabs/CalradiaForge.xml', 'r', encoding='utf-8') as f:
    xml_content = f.read()

with open('src/CalradiaForge.Mod/PanelViewModel.cs', 'r', encoding='utf-8') as f:
    cs_content = f.read()

bindings = set(re.findall(r'"@([A-Za-z0-9_]+)"', xml_content))
commands = set(re.findall(r'Command\.[A-Za-z0-9_]+="([A-Za-z0-9_]+)"', xml_content))
curly_bindings = set(re.findall(r'"\{([A-Za-z0-9_]+)\}"', xml_content))

# Match any public property in PanelViewModel
cs_props = set(re.findall(r'public\s+[A-Za-z0-9_<>,\.\[\]]+\s+([A-Za-z0-9_]+)\s*[\{=]', cs_content))
# Also check for properties with DataSourceProperty
ds_props = set(re.findall(r'\[DataSourceProperty\]\s*public\s+[A-Za-z0-9_<>,\.\[\]]+\s+([A-Za-z0-9_]+)', cs_content))
cs_props.update(ds_props)

cs_methods = set(re.findall(r'public\s+void\s+([A-Za-z0-9_]+)\s*\(', cs_content))

# Also nested view models (like ToolItemVM, CommandHistoryItemVM)
nested_props = set(re.findall(r'class\s+([A-Za-z0-9_]+VM)', cs_content))

missing_props = [b for b in sorted(bindings) if b not in cs_props]
missing_commands = [c for c in sorted(commands) if c not in cs_methods]
missing_curly = [b for b in sorted(curly_bindings) if b not in cs_props]

print('Missing @bindings in PanelViewModel:', missing_props)
print('Missing Commands in PanelViewModel:', missing_commands)
print('Missing {curly} in PanelViewModel:', missing_curly)

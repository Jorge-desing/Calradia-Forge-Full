import re

tools = []
with open('tools/generated_props.txt', 'r', encoding='utf-8') as f:
    content = f.read()

pattern = r'\[DataSourceProperty\] public bool Is(\w+)Active => current == "([^"]+)";\s*\[DataSourceProperty\] public string \w+Label => T\("([^"]+)"\);\s*\[DataSourceProperty\] public string \w+Hint => T\("([^"]+)"\);'

matches = re.finditer(pattern, content)
for m in matches:
    name_id = m.group(1) # ForgeKingdomManager
    tag = m.group(2) # sdk-ForgeKingdomManager
    label = m.group(3)
    hint = m.group(4)
    tools.append((name_id, tag, label, hint))

print(f"Extracted {len(tools)} SDK tools.")

csharp_list = []
for t in tools:
    # Escape quotes if necessary
    label = t[2].replace('"', '\\"')
    hint = t[3].replace('"', '\\"')
    csharp_list.append(f'new ToolDefinition("{t[1]}", "{label}", "{hint}")')

csharp_array = "public static readonly ToolDefinition[] SdkTools = new[] {\n    " + ",\n    ".join(csharp_list) + "\n};"

with open('tools/tools_array.cs', 'w', encoding='utf-8') as f:
    f.write(csharp_array)

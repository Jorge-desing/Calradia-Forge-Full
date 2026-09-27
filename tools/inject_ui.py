import re
import os

wpf_path = r'src\CalradiaForge.Desktop\MainWindow.xaml'
vm_path = r'src\CalradiaForge.Mod\PanelViewModel.cs'
xml_path = r'modules\CalradiaForge\GUI\Prefabs\CalradiaForge.xml'

with open(wpf_path, 'r', encoding='utf-8') as f:
    wpf_content = f.read()

pattern = r'<TreeViewItem[^>]*Header="([^"]+)"[^>]*Tag="SDK_([^"]+)"'
matches = re.findall(pattern, wpf_content)

print(f"Total matches: {len(matches)}")

# Prepare XML
xml_buttons = []
# Prepare C# View Model properties
vm_properties = []
vm_executes = []
vm_switches = []
vm_suggestions = []
vm_notify = []
vm_actions = []

for header, tag in matches:
    # Handle cases where header has &amp; or similar. Just keep it simple.
    header = header.replace("&amp;", "&")
    xml_buttons.append(f'''                          <ButtonWidget Id="Forge{tag}" Command.Click="Execute{tag}" Hint.HintText="@{tag}Hint" IsSelected="@Is{tag}Active" IsVisible="@IsCategorySdkActive" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="Fixed" SuggestedHeight="42" WidthSizePolicy="StretchToParent" MarginBottom="8"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@{tag}Label" /></Children></ButtonWidget>''')
    
    vm_properties.append(f'        [DataSourceProperty] public bool Is{tag}Active => current == "sdk-{tag}";')
    vm_properties.append(f'        [DataSourceProperty] public string {tag}Label => T("{header}");')
    vm_properties.append(f'        [DataSourceProperty] public string {tag}Hint => T("SDK Builder: Generate C# scaffold for {header}");')
    
    vm_executes.append(f'        public void Execute{tag}() => SelectSection("sdk-{tag}");')
    
    vm_switches.append(f'                case "sdk-{tag}": currentCategory = "sdk"; break;')
    
    vm_suggestions.append(f'                    case "sdk-{tag}": return T("SDK Builder: {header} Scaffold");')
    
    vm_notify.append(f'nameof(Is{tag}Active)')
    vm_notify.append(f'nameof({tag}Label)')
    vm_notify.append(f'nameof({tag}Hint)')
    
    vm_actions.append(f'"sdk-{tag}"')

# Let's write the generated snippets to files so we can inspect them and apply them manually or programmatically
with open('tools/generated_xml.txt', 'w', encoding='utf-8') as f:
    f.write('\n'.join(xml_buttons))

with open('tools/generated_props.txt', 'w', encoding='utf-8') as f:
    f.write('\n'.join(vm_properties))

with open('tools/generated_executes.txt', 'w', encoding='utf-8') as f:
    f.write('\n'.join(vm_executes))

with open('tools/generated_switches.txt', 'w', encoding='utf-8') as f:
    f.write('\n'.join(vm_switches))
    
with open('tools/generated_suggestions.txt', 'w', encoding='utf-8') as f:
    f.write('\n'.join(vm_suggestions))
    
with open('tools/generated_notify.txt', 'w', encoding='utf-8') as f:
    f.write(', '.join(vm_notify))

with open('tools/generated_actions.txt', 'w', encoding='utf-8') as f:
    f.write(', '.join(vm_actions))

print("Generated snippets written to tools/generated_*.txt")

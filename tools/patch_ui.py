import re

vm_path = r'src\CalradiaForge.Mod\PanelViewModel.cs'
xml_path = r'modules\CalradiaForge\GUI\Prefabs\CalradiaForge.xml'

with open('tools/generated_xml.txt', 'r', encoding='utf-8') as f: xml_buttons = f.read()
with open('tools/generated_props.txt', 'r', encoding='utf-8') as f: vm_properties = f.read()
with open('tools/generated_executes.txt', 'r', encoding='utf-8') as f: vm_executes = f.read()
with open('tools/generated_switches.txt', 'r', encoding='utf-8') as f: vm_switches = f.read()
with open('tools/generated_suggestions.txt', 'r', encoding='utf-8') as f: vm_suggestions = f.read()
with open('tools/generated_notify.txt', 'r', encoding='utf-8') as f: vm_notify = f.read()
with open('tools/generated_actions.txt', 'r', encoding='utf-8') as f: vm_actions = f.read()

# XML Injection
with open(xml_path, 'r', encoding='utf-8') as f:
    xml_content = f.read()

# We need to add the Category Tab and the Category Container
category_button = '''                            <ButtonWidget Id="ForgeCategorySdk" Command.Click="ExecuteCategorySdk" Hint.HintText="@CategorySdkHint" IsSelected="@IsCategorySdkActive" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TabButton" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="142" MarginRight="6"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@CategorySdkLabel" /></Children></ButtonWidget>
'''
xml_content = xml_content.replace('<!-- END CATEGORY TABS -->', category_button + '                            <!-- END CATEGORY TABS -->')

category_container = f'''                          <!-- CATEGORY 8: ADVANCED SDK -->
{xml_buttons}
'''
# Insert after NOVICE MODDER HUB container
xml_content = xml_content.replace('<!-- END NAVIGATION BARS -->', category_container + '                        <!-- END NAVIGATION BARS -->')

with open(xml_path, 'w', encoding='utf-8') as f:
    f.write(xml_content)

# View Model Injection
with open(vm_path, 'r', encoding='utf-8') as f:
    vm_content = f.read()

# Add to arrays in NavigationLabel
# var actions = new[] { ... };
actions_pattern = r'var actions = new\[\] \{([^}]+)\};'
match = re.search(actions_pattern, vm_content)
if match:
    old_actions = match.group(1)
    new_actions = old_actions + ", " + vm_actions
    vm_content = vm_content.replace(match.group(0), f'var actions = new[] {{{new_actions}}};')

# Add to NotifyLayout
# foreach (var name in new[] { ... }) OnPropertyChanged(name);
notify_pattern = r'foreach \(var name in new\[\] \{([^}]+)\}\) OnPropertyChanged\(name\);'
matches = re.finditer(notify_pattern, vm_content)
# We take the second one (the one with IsNoviceWorkshopActive)
for i, match in enumerate(matches):
    if 'IsNoviceWorkshopActive' in match.group(0):
        old_notify = match.group(1)
        new_notify = old_notify + ", " + vm_notify + ", nameof(IsCategorySdkActive), nameof(CategorySdkLabel), nameof(CategorySdkHint)"
        vm_content = vm_content.replace(match.group(0), f'foreach (var name in new[] {{{new_notify}}}) OnPropertyChanged(name);')

# Add IsCategorySdkActive
cat_prop = '''        [DataSourceProperty] public bool IsCategorySdkActive => currentCategory == "sdk";
        [DataSourceProperty] public string CategorySdkLabel => T("ADVANCED SDK");
        [DataSourceProperty] public string CategorySdkHint => T("Browse all 110+ Advanced SDK tools");
        public void ExecuteCategorySdk() { currentCategory = "sdk"; SelectSection("sdk-ForgeKingdomManager"); }
'''
vm_content = vm_content.replace('[DataSourceProperty] public bool IsCategoryNoviceActive => currentCategory == "novice";', '[DataSourceProperty] public bool IsCategoryNoviceActive => currentCategory == "novice";\n' + cat_prop)

# Add properties
vm_content = vm_content.replace('[DataSourceProperty] public bool IsNoviceCombatActive => current == "novice-combat";', '[DataSourceProperty] public bool IsNoviceCombatActive => current == "novice-combat";\n' + vm_properties)

# Add executes
vm_content = vm_content.replace('public void ExecuteNoviceCombat() => SelectSection("novice-combat");', 'public void ExecuteNoviceCombat() => SelectSection("novice-combat");\n' + vm_executes)

# Add switches
vm_content = vm_content.replace('case "novice-combat":\n                    currentCategory = "novice";\n                    break;', 'case "novice-combat":\n                    currentCategory = "novice";\n                    break;\n' + vm_switches)

# Add suggestions
vm_content = vm_content.replace('case "novice-combat": return T("Combat AI Component: Generate AgentComponent + MissionLogic with deferred _initialized pattern.");', 'case "novice-combat": return T("Combat AI Component: Generate AgentComponent + MissionLogic with deferred _initialized pattern.");\n' + vm_suggestions)

with open(vm_path, 'w', encoding='utf-8') as f:
    f.write(vm_content)
print("Injected into CalradiaForge.xml and PanelViewModel.cs")

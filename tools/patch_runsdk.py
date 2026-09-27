import re

vm_path = r'src\CalradiaForge.Mod\PanelViewModel.cs'
with open(vm_path, 'r', encoding='utf-8') as f:
    vm_content = f.read()

# Add ExecuteRunSdk
run_novice = 'public void ExecuteRunNovice() { if (currentCategory == "novice") Send(current, Argument); }'
run_sdk = 'public void ExecuteRunSdk() { if (currentCategory == "sdk") Send(current, Argument); }'
vm_content = vm_content.replace(run_novice, run_novice + '\n          ' + run_sdk)

# Add ShowSdkActions
show_novice = '[DataSourceProperty] public bool ShowNoviceActions => currentCategory == "novice";'
show_sdk = '[DataSourceProperty] public bool ShowSdkActions => currentCategory == "sdk";'
vm_content = vm_content.replace(show_novice, show_novice + '\n        ' + show_sdk)

# Add RunSdkLabel and Hint
run_novice_label = '[DataSourceProperty] public string RunNoviceLabel => T("Run Novice Tool");'
run_sdk_label = '[DataSourceProperty] public string RunSdkLabel => T("Run SDK Tool");\n        [DataSourceProperty] public string RunSdkHint => T("Run the selected SDK feature");'
vm_content = vm_content.replace(run_novice_label, run_novice_label + '\n        ' + run_sdk_label)

# Add to NotifyLayout
vm_content = vm_content.replace('nameof(ShowNoviceActions)', 'nameof(ShowNoviceActions), nameof(ShowSdkActions), nameof(RunSdkLabel), nameof(RunSdkHint)')

with open(vm_path, 'w', encoding='utf-8') as f:
    f.write(vm_content)

xml_path = r'modules\CalradiaForge\GUI\Prefabs\CalradiaForge.xml'
with open(xml_path, 'r', encoding='utf-8') as f:
    xml_content = f.read()

# Add RunSdk Button
xml_run_novice = '<ButtonWidget Id="ForgeRunNovice" Command.Click="ExecuteRunNovice" IsVisible="@ShowNoviceActions" Hint.HintText="@RunNoviceHint" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="130"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@RunNoviceLabel" /></Children></ButtonWidget>'
xml_run_sdk = '<ButtonWidget Id="ForgeRunSdk" Command.Click="ExecuteRunSdk" IsVisible="@ShowSdkActions" Hint.HintText="@RunSdkHint" DoNotPassEventsToChildren="true" Brush="CalradiaForge.TacticalButton" HeightSizePolicy="StretchToParent" WidthSizePolicy="Fixed" SuggestedWidth="130" MarginLeft="6"><Children><TextWidget Brush="CalradiaForge.ButtonText" Brush.TextHorizontalAlignment="Center" Brush.TextVerticalAlignment="Center" HeightSizePolicy="StretchToParent" WidthSizePolicy="StretchToParent" Text="@RunSdkLabel" /></Children></ButtonWidget>'
xml_content = xml_content.replace(xml_run_novice, xml_run_novice + '\n                  ' + xml_run_sdk)

with open(xml_path, 'w', encoding='utf-8') as f:
    f.write(xml_content)

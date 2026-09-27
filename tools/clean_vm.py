import re

with open(r'src\CalradiaForge.Mod\PanelViewModel.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Remove the massive array update in NotifyLayout
notify_pattern = r'foreach \(var name in new\[\] \{([^}]+)\}\)\s*OnPropertyChanged\(name\);'
def repl_notify(m):
    return 'foreach (var name in new[] { nameof(NavigationLabel), nameof(OverviewLabel), nameof(DeveloperLabel), nameof(ExtensionsLabel), nameof(DiagnosticsLabel), nameof(ToolsLabel), nameof(RunLabel), nameof(SearchHint), nameof(FilterHint), nameof(IsCategoryOverviewActive), nameof(IsCategoryDeveloperActive), nameof(IsCategoryExtensionsActive), nameof(IsCategoryDiagnosticsActive), nameof(IsCategoryToolsActive), nameof(IsCategoryNoviceActive), nameof(CategoryNoviceLabel), nameof(CategoryNoviceHint), nameof(IsCategorySdkActive), nameof(CategorySdkLabel), nameof(CategorySdkHint) }) OnPropertyChanged(name);'

content = re.sub(notify_pattern, repl_notify, content)

# Remove the giant blocks of IsForgeXActive, ForgeXLabel, ForgeXHint, ExecuteForgeX
props_pattern = r'\[DataSourceProperty\] public bool IsForge.*?Active => current == ".*?";\s*\[DataSourceProperty\] public string Forge.*?Label => T\(".*?"\);\s*\[DataSourceProperty\] public string Forge.*?Hint => T\(".*?"\);'
content = re.sub(props_pattern, '', content)

exec_pattern = r'public void ExecuteForge.*?\(\) => SelectSection\(".*?"\);'
content = re.sub(exec_pattern, '', content)

switch_pattern = r'case "sdk-Forge.*?": currentCategory = "sdk"; break;'
content = re.sub(switch_pattern, '', content)

sugg_pattern = r'case "sdk-Forge.*?": return T\(".*?"\);'
content = re.sub(sugg_pattern, '', content)

# Clean up multiple newlines
content = re.sub(r'\n\s*\n', '\n', content)

with open(r'src\CalradiaForge.Mod\PanelViewModel.cs', 'w', encoding='utf-8') as f:
    f.write(content)

import re
with open(r'src\CalradiaForge.Mod\PanelViewModel.cs', 'r', encoding='utf-8') as f:
    content = f.read()

correct_notify = 'foreach (var name in new[] { nameof(NavigationLabel), nameof(IsCategoryOverviewActive), nameof(CategoryOverviewLabel), nameof(CategoryOverviewHint), nameof(IsCategoryInspectorActive), nameof(CategoryInspectorLabel), nameof(CategoryInspectorHint), nameof(IsCategoryToolkitActive), nameof(CategoryToolkitLabel), nameof(CategoryToolkitHint), nameof(IsCategoryWeaveActive), nameof(CategoryWeaveLabel), nameof(CategoryWeaveHint), nameof(IsCategorySimulateActive), nameof(CategorySimulateLabel), nameof(CategorySimulateHint), nameof(IsCategoryAuditActive), nameof(CategoryAuditLabel), nameof(CategoryAuditHint), nameof(IsCategoryNoviceActive), nameof(CategoryNoviceLabel), nameof(CategoryNoviceHint), nameof(IsCategorySdkActive), nameof(CategorySdkLabel), nameof(CategorySdkHint), nameof(SearchText), nameof(SdkTools) }) OnPropertyChanged(name);'

pattern = r'foreach \(var name in new\[\] \{[^}]+\}\)\s*OnPropertyChanged\(name\);'
content = re.sub(pattern, correct_notify, content)

with open(r'src\CalradiaForge.Mod\PanelViewModel.cs', 'w', encoding='utf-8') as f:
    f.write(content)

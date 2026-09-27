import re

with open(r'src\CalradiaForge.Mod\PanelViewModel.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# We need to add the properties and the filtering logic.
# Search for `public PanelViewModel(Runtime r, Action c) { ... }`
# We'll inject Initialization there.

init_injection = '''
        private MBBindingList<ToolItemVM> _sdkTools = new MBBindingList<ToolItemVM>();
        [DataSourceProperty] public MBBindingList<ToolItemVM> SdkTools => _sdkTools;

        private string _searchText = "";
        [DataSourceProperty]
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (value != _searchText)
                {
                    _searchText = value;
                    OnPropertyChangedWithValue(value, nameof(SearchText));
                    UpdateSearch();
                }
            }
        }

        private void InitializeTools()
        {
            _sdkTools.Clear();
            foreach (var tool in ToolDefinitionRegistry.SdkTools)
            {
                _sdkTools.Add(new ToolItemVM(tool.Tag, tool.Title, tool.Hint, OnToolSelected));
            }
        }

        private void OnToolSelected(ToolItemVM tool)
        {
            foreach (var t in _sdkTools) t.IsSelected = (t == tool);
            SelectSection(tool.Tag);
        }

        private void UpdateSearch()
        {
            string query = _searchText?.ToLower() ?? "";
            foreach (var tool in _sdkTools)
            {
                tool.IsVisible = string.IsNullOrEmpty(query) || tool.Title.ToLower().Contains(query) || tool.Tag.ToLower().Contains(query);
            }
        }
'''

content = content.replace('public PanelViewModel(Runtime r, Action c) { runtime = r; close = c; Labels(); ExecuteSummary(); }', 'public PanelViewModel(Runtime r, Action c) { runtime = r; close = c; Labels(); InitializeTools(); ExecuteSummary(); }' + init_injection)

# Also we need to make sure the ToolDefinition partial or static array is available.
# We generated `tools_array.cs` earlier. Let's append it to `PanelViewModel.cs` inside the namespace, or just create `ToolDefinitionRegistry.cs`.
with open(r'src\CalradiaForge.Mod\PanelViewModel.cs', 'w', encoding='utf-8') as f:
    f.write(content)

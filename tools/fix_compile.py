import re

vm_path = r'src\CalradiaForge.Mod\PanelViewModel.cs'
with open(vm_path, 'r', encoding='utf-8') as f:
    vm_content = f.read()

vm_content = vm_content.replace('nameof(ShowNoviceActions), nameof(ShowSdkActions), nameof(RunSdkLabel), nameof(RunSdkHint)', 'nameof(ShowNoviceActions)')
with open(vm_path, 'w', encoding='utf-8') as f:
    f.write(vm_content)

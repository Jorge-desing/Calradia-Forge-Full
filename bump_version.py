import os, glob

version = '1.4.0'
files = glob.glob('src/**/*.csproj', recursive=True) + glob.glob('modules/**/SubModule.xml', recursive=True)

for f in files:
    content = open(f, encoding='utf-8').read()
    
    # csproj version bump
    if f.endswith('.csproj'):
        import re
        content = re.sub(r'<Version>.*?</Version>', f'<Version>{version}</Version>', content)
        content = re.sub(r'<AssemblyVersion>.*?</AssemblyVersion>', f'<AssemblyVersion>{version}</AssemblyVersion>', content)
        content = re.sub(r'<FileVersion>.*?</FileVersion>', f'<FileVersion>{version}</FileVersion>', content)
    
    # SubModule.xml version bump
    if f.endswith('SubModule.xml'):
        import re
        content = re.sub(r'value="v[0-9\.]+"', f'value="v{version}"', content)
        
    open(f, 'w', encoding='utf-8').write(content)

print('Versions bumped to', version)

import os, glob, re
version = '1.5.6'
for f in glob.glob('src/**/*.csproj', recursive=True) + glob.glob('modules/**/SubModule.xml', recursive=True):
    content = open(f, encoding='utf-8').read()
    if f.endswith('.csproj'):
        content = re.sub(r'<Version>.*?</Version>', f'<Version>{version}</Version>', content)
        content = re.sub(r'<AssemblyVersion>.*?</AssemblyVersion>', f'<AssemblyVersion>{version}</AssemblyVersion>', content)
        content = re.sub(r'<FileVersion>.*?</FileVersion>', f'<FileVersion>{version}</FileVersion>', content)
    if f.endswith('SubModule.xml'):
        content = re.sub(r'value=\"v[0-9\.]+\"', f'value=\"v{version}\"', content)
    open(f, 'w', encoding='utf-8').write(content)
for f in ['Directory.Build.props']:
    if os.path.exists(f):
        content = open(f, encoding='utf-8').read()
        content = re.sub(r'<Version>.*?</Version>', f'<Version>{version}</Version>', content)
        open(f, 'w', encoding='utf-8').write(content)
for f in ['src/CalradiaForge.Core/SuiteInfo.cs']:
    if os.path.exists(f):
        content = open(f, encoding='utf-8').read()
        content = re.sub(r'public const string Version = \".*?\";', f'public const string Version = \"{version}\";', content)
        open(f, 'w', encoding='utf-8').write(content)
print('Bumped to', version)

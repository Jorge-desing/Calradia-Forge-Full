import re

with open(r'src\CalradiaForge.Desktop\MainWindow.xaml', 'r', encoding='utf-8') as f:
    xaml = f.read()

toolbars = re.findall(r'<WrapPanel x:Name="([A-Za-z0-9_]+Toolbar)"[^>]*>(.*?)</WrapPanel>', xaml, re.DOTALL)
print(f'Total toolbars found: {len(toolbars)}')
for name, content in toolbars:
    buttons = re.findall(r'<Button [^>]*Content="([^"]+)"', content)
    print(f'{name}: {buttons}')

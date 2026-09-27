with open(r'src\CalradiaForge.Desktop\MainWindow.xaml.cs', 'r', encoding='utf-8') as f:
    text = f.read()

start = text.find('private void ApplyLanguage(string lang)')
end = text.find('// --- COMMAND DECK & FILTER CHIPS LOGIC ---', start)
print(f'ApplyLanguage starts at {start} and ends around {end}')

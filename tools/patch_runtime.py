import re

rt_path = r'src\CalradiaForge.Mod\Runtime.cs'
with open(rt_path, 'r', encoding='utf-8') as f:
    content = f.read()

injection = '''                        if (s.Action.StartsWith("sdk-"))
                        {
                            var tag = s.Action.Substring(4);
                            data = CalradiaForge.Sdk.ScaffoldEngine.Generate(tag, s.Argument);
                            break;
                        }
                        default:'''

content = content.replace("default:", injection)
with open(rt_path, 'w', encoding='utf-8') as f:
    f.write(content)

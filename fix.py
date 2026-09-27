import re
content = open('src/CalradiaForge.Core/SessionLog.cs', 'r', encoding='utf-8').read()
content = re.sub(r'public sealed class SessionLog.*?public sealed class SessionLog', 'public sealed class SessionLog', content, flags=re.DOTALL)
open('src/CalradiaForge.Core/SessionLog.cs', 'w', encoding='utf-8').write(content)

import re
content = open('.agents/rules/calradia_forge_ui.md', 'r', encoding='utf-8').read()

new_rule = '''
- **Text Wrapping Prevention:** In Gauntlet, if a TextWidget has a Brush.FontSize that exceeds its parent container's width (e.g. large watermarks), Gauntlet will auto-wrap the text (often injecting hyphens, turning "CF" into "C- \n F"). To prevent this, always set WidthSizePolicy="CoverChildren" on the TextWidget or ensure the font size is small enough to fit.
- **Watermarks:** Always add DoNotAcceptEvents="true" and DoNotPassEventsToChildren="true" to watermark widgets so they do not block mouse interactions with underlying buttons.
'''

if 'Text Wrapping Prevention' not in content:
    content = content.replace('## Scaling & Implicit Controls (Added from Learning)', '## Scaling & Implicit Controls (Added from Learning)\n' + new_rule)
    open('.agents/rules/calradia_forge_ui.md', 'w', encoding='utf-8').write(content)

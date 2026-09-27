from pathlib import Path
import re
from xml.sax.saxutils import escape
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, PageBreak
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.colors import HexColor
from reportlab.lib.enums import TA_LEFT
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont

ROOT = Path(__file__).resolve().parent
SOURCE = ROOT / 'bannerlord-importacion-fbx-2026-09-23.md'
OUTPUT = ROOT.parent / 'pdf' / 'bannerlord-importacion-fbx-2026-09-23.pdf'
OUTPUT.parent.mkdir(parents=True, exist_ok=True)
pdfmetrics.registerFont(TTFont('Research', 'C:/Windows/Fonts/arial.ttf'))
pdfmetrics.registerFont(TTFont('ResearchBold', 'C:/Windows/Fonts/arialbd.ttf'))
pdfmetrics.registerFontFamily('Research', normal='Research', bold='ResearchBold', italic='Research', boldItalic='ResearchBold')
styles = getSampleStyleSheet()
styles.add(ParagraphStyle(name='TextR', fontName='Research', fontSize=10.2, leading=14.5, spaceAfter=9, textColor=HexColor('#253344')))
styles.add(ParagraphStyle(name='TitleR', fontName='ResearchBold', fontSize=25, leading=29, spaceAfter=13, textColor=HexColor('#162B40')))
styles.add(ParagraphStyle(name='SectionR', fontName='ResearchBold', fontSize=17, leading=22, spaceAfter=17, textColor=HexColor('#162B40')))
styles.add(ParagraphStyle(name='MetaR', fontName='Research', fontSize=9, leading=12, textColor=HexColor('#5D7082'), spaceAfter=24))
styles.add(ParagraphStyle(name='BulletR', parent=styles['TextR'], leftIndent=12, firstLineIndent=-10))

def inline(s):
    s = escape(s)
    s = re.sub(r'\[([^\]]+)\]\((https?://[^)]+)\)', r'<link href="\2" color="#1A6476"><u>\1</u></link>', s)
    s = re.sub(r'\*\*(.+?)\*\*', r'<b>\1</b>', s)
    s = re.sub(r'`([^`]+)`', r'<font color="#205D70">\1</font>', s)
    return s

story=[]
section=0
for block in SOURCE.read_text(encoding='utf-8').strip().split('\n\n'):
    if block.startswith('# '):
        story.append(Paragraph(inline(block[2:]), styles['TitleR']))
    elif block.startswith('## '):
        section += 1
        if section > 1:
            story.append(PageBreak())
        story.append(Paragraph(inline(block[3:]), styles['SectionR']))
    elif block.startswith('Investigación técnica'):
        story.append(Paragraph(inline(block), styles['MetaR']))
    elif block.startswith('- ') or re.match(r'^\d+\. ', block):
        for line in block.splitlines():
            if line.startswith('- '):
                line = '• ' + line[2:]
            story.append(Paragraph(inline(line), styles['BulletR']))
    else:
        story.append(Paragraph(inline(block.replace('\n',' ')), styles['TextR']))

def decorate(canvas, doc):
    canvas.saveState()
    canvas.setStrokeColor(HexColor('#C5D0DA'))
    canvas.line(48, 751, 564, 751)
    canvas.setFont('Research', 8)
    canvas.setFillColor(HexColor('#5D7082'))
    canvas.drawString(48, 764, 'CALRADIA FORGE  /  INVESTIGACIÓN TÉCNICA')
    canvas.line(48, 43, 564, 43)
    canvas.drawString(48, 29, 'Bannerlord | Importación FBX | 23 septiembre 2026')
    canvas.drawRightString(564, 29, str(doc.page))
    canvas.restoreState()

doc = SimpleDocTemplate(str(OUTPUT), pagesize=(612,792), leftMargin=48, rightMargin=48, topMargin=58, bottomMargin=57, title='Automatización de importación FBX en Bannerlord', author='Investigación técnica para Calradia Forge')
doc.build(story, onFirstPage=decorate, onLaterPages=decorate)
print(OUTPUT)

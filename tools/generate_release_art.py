"""Create original static release artwork from reproducible geometric motifs."""
from pathlib import Path
import math, random
from PIL import Image, ImageDraw, ImageFont

root=Path(__file__).resolve().parents[1]
import re
VERSION = re.search(r'<CalradiaForgeVersion>([^<]+)</CalradiaForgeVersion>', (root/'Directory.Build.props').read_text(encoding='utf-8')).group(1)
random.seed(148)
size=1024
image=Image.new('RGB',(size,size),'#162B25')
draw=ImageDraw.Draw(image)
gold='#D9B878';muted='#8B987A';cream='#F4E8C8'
for n in range(4500):
    x=random.randrange(size);y=random.randrange(size)
    draw.point((x,y),fill=random.choice(['#1B3029','#20342B','#182D26']))
for inset,width in [(28,2),(39,1),(61,1)]:draw.rectangle((inset,inset,size-inset,size-inset),outline=gold,width=width)
for cx,cy in [(61,61),(963,61),(61,963),(963,963)]:
    for angle in range(0,360,45):
        a=math.radians(angle)
        points=[(cx+math.cos(a)*8,cy+math.sin(a)*8),(cx+math.cos(a+.24)*34,cy+math.sin(a+.24)*34),(cx+math.cos(a)*43,cy+math.sin(a)*43),(cx+math.cos(a-.24)*34,cy+math.sin(a-.24)*34)]
        draw.polygon(points,outline=gold)
# Paired harmonic curves suggest manuscript linework without copying historical art.
for sign in [-1,1]:
    points=[(x,825+sign*12*math.sin((x-130)*math.pi/64)) for x in range(130,895)]
    draw.line(points,fill=muted,width=2)
draw.ellipse((280,150,744,614),outline=muted,width=1)
draw.ellipse((295,165,729,599),outline=gold,width=3)
for i in range(32):
    a=i*2*math.pi/32
    draw.line([(512+239*math.cos(a),382+239*math.sin(a)),(512+248*math.cos(a),382+248*math.sin(a))],fill=gold,width=2)
# An original shield and anvil emblem built only from polygons and lines.
draw.polygon([(380,245),(644,245),(636,429),(606,485),(512,546),(418,485),(388,429)],fill='#203B30',outline=gold,width=4)
draw.polygon([(402,346),(626,346),(655,369),(613,397),(562,397),(559,425),(584,447),(584,462),(447,462),(447,447),(473,425),(470,397),(445,393)],fill=gold)
draw.line((485,329,526,288),fill=cream,width=14)
draw.polygon([(511,281),(529,263),(566,299),(548,318)],fill=cream)

def centered(text,y,font_name,font_size,color):
    font=ImageFont.truetype('C:/Windows/Fonts/'+font_name,font_size)
    bounds=draw.textbbox((0,0),text,font=font)
    draw.text(((size-(bounds[2]-bounds[0]))/2,y),text,font=font,fill=color)

base=image.copy()
for language in ['en','es']:
    image=base.copy();draw=ImageDraw.Draw(image)
    centered('CALRADIA FORGE' if language=='en' else 'FORJA DE CALRADIA',645,'georgiab.ttf',54 if language=='en' else 45,cream)
    centered('BANNERLORD DEVELOPER TOOLS' if language=='en' else 'HERRAMIENTAS PARA DESARROLLADORES',725,'arial.ttf',23 if language=='en' else 21,gold)
    centered('INSPECT  /  TEST  /  UNDERSTAND' if language=='en' else 'INSPECCIONA  /  PRUEBA  /  COMPRENDE',864,'arial.ttf',22,cream)
    centered(f'DEVELOPER PREVIEW {VERSION}' if language=='en' else f'VERSION PRELIMINAR {VERSION}',911,'arial.ttf',16,muted)
    output=root/'assets'/('release-cover.png' if language=='en' else 'release-cover.es.png')
    image.save(output,optimize=True)
    assert output.stat().st_size<1024*1024
    print(output)



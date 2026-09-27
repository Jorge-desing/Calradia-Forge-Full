import re
from html.parser import HTMLParser

class TextExtractor(HTMLParser):
    def __init__(self):
        super().__init__()
        self.text = []

    def handle_data(self, data):
        data = data.strip()
        if data:
            self.text.append(data)

parser = TextExtractor()
parser.feed(open('nexusmods.html', 'r', encoding='utf-8').read())
print(' '.join(parser.text)[:3000])

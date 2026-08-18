import os
with open('d:/okemsocial/Views/Shared/_Layout.cshtml', 'r', encoding='utf-8-sig', errors='ignore') as f:
    text = f.read()

text = text.replace(r'\"U\"', '"U"')

with open('d:/okemsocial/Views/Shared/_Layout.cshtml', 'w', encoding='utf-8-sig') as f:
    f.write(text)

import os
import sys
sys.stdout.reconfigure(encoding='utf-8')
with open('d:/okemsocial/Views/Posts/Feed.cshtml', 'r', encoding='utf-8-sig', errors='ignore') as f:
    text = f.read()

text = text.replace(r"\`<button", "`<button")
text = text.replace(r"</button>\`", "</button>`")

with open('d:/okemsocial/Views/Posts/Feed.cshtml', 'w', encoding='utf-8-sig') as f:
    f.write(text)
print('Fixed backticks!')

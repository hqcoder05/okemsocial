import os
import re

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    original = content

    # Replace "LinkUp" with "Okem"
    content = content.replace("LinkUp", "Okem")
    # Replace "linkup" with "okem"
    content = content.replace("linkup", "okem")
    # Replace "LINKUP" with "OKEM"
    content = content.replace("LINKUP", "OKEM")

    if original != content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Updated {filepath}")

for root, dirs, files in os.walk('d:/okemsocial/'):
    if '.git' in root or 'bin' in root or 'obj' in root or 'node_modules' in root or '.vs' in root:
        continue
    for file in files:
        if file.endswith(('.cshtml', '.cs', '.js', '.json', '.html', '.css')):
            process_file(os.path.join(root, file))

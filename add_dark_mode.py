import os
import re

def process_file(filepath):
    with open(filepath, 'r', encoding='utf-8') as f:
        content = f.read()

    original = content

    # Add dark:bg-black where bg-white is present
    content = re.sub(r'(class="[^"]*?\b)bg-white(\b[^"]*?")', lambda m: m.group(0) + ' dark:bg-black' if 'dark:bg-black' not in m.group(0) and 'dark:bg-neutral' not in m.group(0) else m.group(0), content)
    
    # Add dark:bg-neutral-900 where bg-slate-50 is present
    content = re.sub(r'(class="[^"]*?\b)bg-slate-50(\b[^"]*?")', lambda m: m.group(0) + ' dark:bg-neutral-900' if 'dark:bg-neutral' not in m.group(0) else m.group(0), content)

    # Add dark:border-white/10 where border-slate-100 or border-slate-200 is present
    content = re.sub(r'(class="[^"]*?\b)border-slate-[12]00(\b[^"]*?")', lambda m: m.group(0) + ' dark:border-white/10' if 'dark:border-white/10' not in m.group(0) else m.group(0), content)

    # Add dark:text-slate-100 where text-slate-800 or 900 or 950 is present
    content = re.sub(r'(class="[^"]*?\b)text-slate-(800|900|950)(\b[^"]*?")', lambda m: m.group(0) + ' dark:text-slate-100' if 'dark:text-white' not in m.group(0) and 'dark:text-slate-100' not in m.group(0) else m.group(0), content)

    # Add dark:text-slate-400 where text-slate-500 or 600 is present
    content = re.sub(r'(class="[^"]*?\b)text-slate-(500|600)(\b[^"]*?")', lambda m: m.group(0) + ' dark:text-slate-400' if 'dark:text-slate-400' not in m.group(0) else m.group(0), content)

    # Add dark:hover:bg-white/10 where hover:bg-slate-50 or 100 is present
    content = re.sub(r'(class="[^"]*?\b)hover:bg-slate-(50|100)(\b[^"]*?")', lambda m: m.group(0) + ' dark:hover:bg-white/10' if 'dark:hover:bg-white/10' not in m.group(0) else m.group(0), content)

    # Add dark:bg-white dark:text-black where bg-slate-950 text-white is present (submit buttons)
    content = re.sub(r'(class="[^"]*?\b)(bg-slate-950[^"]*?text-white|text-white[^"]*?bg-slate-950)(\b[^"]*?")', lambda m: m.group(0) + ' dark:bg-white dark:text-black' if 'dark:bg-white' not in m.group(0) else m.group(0), content)

    if original != content:
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"Updated {filepath}")

for root, dirs, files in os.walk('d:/okemsocial/Views/'):
    for file in files:
        if file.endswith('.cshtml'):
            process_file(os.path.join(root, file))

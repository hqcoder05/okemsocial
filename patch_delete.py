import os
with open('d:/okemsocial/Views/Posts/Feed.cshtml', 'r', encoding='utf-8-sig', errors='ignore') as f:
    text = f.read()

text = text.replace('c.userId === currentUserId', 'c.user?.id === currentUserId')
text = text.replace('class="opacity-0 group-hover:opacity-100 transition absolute right-2 top-2 text-slate-400 hover:text-red-500"', 'class="absolute right-2 top-2 text-slate-400 hover:text-red-500 bg-white/80 dark:bg-black/80 rounded-full w-6 h-6 flex items-center justify-center shadow-sm"')

with open('d:/okemsocial/Views/Posts/Feed.cshtml', 'w', encoding='utf-8-sig') as f:
    f.write(text)

with open('d:/okemsocial/Views/Profile/Me.cshtml', 'r', encoding='utf-8-sig', errors='ignore') as f:
    text2 = f.read()

text2 = text2.replace('c.userId === currentUserId', 'c.user?.id === currentUserId')
with open('d:/okemsocial/Views/Profile/Me.cshtml', 'w', encoding='utf-8-sig') as f:
    f.write(text2)
print("Done fixing delete buttons!")

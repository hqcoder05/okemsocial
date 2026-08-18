import os
with open('d:/okemsocial/Views/Profile/Me.cshtml', 'r', encoding='utf-8-sig', errors='ignore') as f:
    lines = f.readlines()

for i, line in enumerate(lines):
    if "const name = (c.user?.fullName" in line:
        for j in range(i, i+15):
            print(lines[j], end='')
        break

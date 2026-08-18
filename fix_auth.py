import os
import re

# Fix Login.cshtml
with open('d:/okemsocial/Views/Account/Login.cshtml', 'r', encoding='utf-8-sig', errors='ignore') as f:
    text = f.read()

text = re.sub(r'@model.*?\n', '', text)
text = text.replace('asp-for="Email"', 'name="email" id="email" required')
text = text.replace('asp-for="Password"', 'name="password" id="password" required')
text = text.replace('asp-for="RememberMe"', 'name="rememberMe" id="rememberMe"')
text = re.sub(r'<span asp-validation-for=".*?".*?>.*?</span>', '', text)

with open('d:/okemsocial/Views/Account/Login.cshtml', 'w', encoding='utf-8-sig') as f:
    f.write(text)

# Fix Register.cshtml
with open('d:/okemsocial/Views/Account/Register.cshtml', 'r', encoding='utf-8-sig', errors='ignore') as f:
    text2 = f.read()

text2 = re.sub(r'@model.*?\n', '', text2)
text2 = text2.replace('asp-for="FullName"', 'name="fullName" id="fullName" required')
text2 = text2.replace('asp-for="Email"', 'name="email" id="email" required')
text2 = text2.replace('asp-for="Password"', 'name="password" id="password" required')
text2 = text2.replace('asp-for="ConfirmPassword"', 'name="confirmPassword" id="confirmPassword" required')
text2 = re.sub(r'<span asp-validation-for=".*?".*?>.*?</span>', '', text2)

with open('d:/okemsocial/Views/Account/Register.cshtml', 'w', encoding='utf-8-sig') as f:
    f.write(text2)

print('Done fixing Auth views!')

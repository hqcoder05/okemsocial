import os
with open('d:/okemsocial/Views/Profile/Me.cshtml', 'r', encoding='utf-8-sig', errors='ignore') as f:
    text = f.read()

target = '''            const content = document.createElement('div');
            content.className = "text-sm text-slate-700 whitespace-pre-wrap dark:text-slate-300";
            content.textContent = p.caption || "";
            card.appendChild(content);

            if (p.imageUrl) {
                const imgWrap = document.createElement('div');
                imgWrap.className = "mt-4 rounded-xl overflow-hidden bg-slate-50";
                const img = document.createElement('img');
                img.src = p.imageUrl;
                img.className = "w-full max-h-[300px] object-cover";
                imgWrap.appendChild(img);
                card.appendChild(imgWrap);
            }'''

replacement = '''            const content = document.createElement('div');
            content.className = "text-sm text-slate-700 whitespace-pre-wrap dark:text-slate-300";
            content.textContent = p.caption || "";
            card.appendChild(content);

            if (p.imageUrl) {
                const imgWrap = document.createElement('div');
                imgWrap.className = "mt-4 rounded-xl overflow-hidden bg-slate-50";
                const img = document.createElement('img');
                img.src = p.imageUrl;
                img.className = "w-full max-h-[300px] object-cover";
                imgWrap.appendChild(img);
                card.appendChild(imgWrap);
            }

            if (p.originalPost) {
                const op = p.originalPost;
                const opName = (op.user?.fullName || op.user?.email || "Người dùng").trim();
                const opAvatarL = opName[0].toUpperCase();
                
                const qBlock = document.createElement('div');
                qBlock.className = "mt-3 mb-2 rounded-2xl border border-slate-200 bg-slate-50/50 p-4 dark:border-white/10 dark:bg-neutral-800";
                
                const qHeader = document.createElement('div');
                qHeader.className = "flex items-center gap-2 mb-2";
                qHeader.innerHTML = `
                    <div class="w-5 h-5 rounded-full bg-slate-800 text-white flex items-center justify-center text-[10px] font-bold">${opAvatarL}</div>
                    <span class="text-xs font-bold text-slate-600 dark:text-slate-300">${opName}</span>
                    <span class="text-[10px] text-slate-400">${new Date(op.createdAt).toLocaleDateString()}</span>
                `;
                qBlock.appendChild(qHeader);

                if (op.caption) {
                    const qCaption = document.createElement('p');
                    qCaption.className = "text-[14px] leading-relaxed text-slate-700 whitespace-pre-wrap dark:text-slate-300 mb-2";
                    qCaption.textContent = op.caption;
                    qBlock.appendChild(qCaption);
                }

                if (op.imageUrl) {
                    const qImgWrap = document.createElement('div');
                    qImgWrap.className = "overflow-hidden rounded-xl bg-slate-200 dark:bg-neutral-900";
                    qImgWrap.innerHTML = `<img src="${op.imageUrl}" class="max-h-[200px] w-full object-cover" />`;
                    qBlock.appendChild(qImgWrap);
                }
                card.appendChild(qBlock);
            }'''

text = text.replace(target, replacement)
with open('d:/okemsocial/Views/Profile/Me.cshtml', 'w', encoding='utf-8-sig') as f:
    f.write(text)
print('Done patching Me.cshtml')

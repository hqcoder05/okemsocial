import os
import sys
sys.stdout.reconfigure(encoding='utf-8')
with open('d:/okemsocial/Views/Posts/Feed.cshtml', 'r', encoding='utf-8-sig', errors='ignore') as f:
    text = f.read()

# Add currentUserId to JS
if 'const currentUserId = @currentUserId;' not in text:
    text = text.replace('document.addEventListener("DOMContentLoaded", function () {', 'const currentUserId = @currentUserId;\n        document.addEventListener("DOMContentLoaded", function () {')

# Add delete button to comment html
old_html = '''const html = `
                        <div class="flex gap-2.5">
                            ${avatarHtml}
                            <div class="flex-1 rounded-2xl bg-slate-50 px-3.5 py-2 dark:bg-neutral-900">
                                <div class="text-xs font-bold text-slate-900 dark:text-white">${authorName}</div>
                                <div class="text-xs text-slate-700 mt-0.5 dark:text-slate-300">${content}</div>
                            </div>
                        </div>
                    `;'''
new_html = '''const html = `
                        <div class="flex gap-2.5 group relative">
                            ${avatarHtml}
                            <div class="flex-1 rounded-2xl bg-slate-50 px-3.5 py-2 dark:bg-neutral-900">
                                <div class="text-xs font-bold text-slate-900 dark:text-white">${authorName}</div>
                                <div class="text-xs text-slate-700 mt-0.5 dark:text-slate-300 whitespace-pre-wrap">${content}</div>
                            </div>
                            ${c.userId === currentUserId ? \`<button onclick="deleteComment(${c.id}, this)" class="opacity-0 group-hover:opacity-100 transition absolute right-2 top-2 text-slate-400 hover:text-red-500"><i class="fa-solid fa-trash text-xs"></i></button>\` : ''}
                        </div>
                    `;'''
text = text.replace(old_html, new_html)

# Add sharePost and deleteComment functions
funcs = '''
        async function sharePost(postId, btn) {
            if(!confirm("Chia sẻ bài viết này về trang cá nhân của bạn?")) return;
            btn.disabled = true;
            try {
                const res = await fetch(`/api/posts/${postId}/share`, { method: "POST" });
                if(res.ok) {
                    alert("Đã chia sẻ bài viết thành công!");
                    window.location.reload();
                } else {
                    alert("Lỗi khi chia sẻ.");
                }
            } catch(e) { console.error(e); }
            btn.disabled = false;
        }

        async function deleteComment(commentId, btn) {
            if(!confirm("Bạn có chắc chắn muốn xóa bình luận này?")) return;
            try {
                const res = await fetch(`/api/comments/${commentId}`, { method: "DELETE" });
                if(res.ok) {
                    btn.closest('.flex').remove();
                } else {
                    alert("Không thể xóa bình luận.");
                }
            } catch(e) { console.error(e); }
        }
'''
if 'async function deleteComment' not in text:
    text = text.replace('async function submitComment(postId, button) {', funcs + '\n        async function submitComment(postId, button) {')

with open('d:/okemsocial/Views/Posts/Feed.cshtml', 'w', encoding='utf-8-sig') as f:
    f.write(text)
print('Done!')

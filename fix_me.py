import os
import sys
sys.stdout.reconfigure(encoding='utf-8')
with open('d:/okemsocial/Views/Profile/Me.cshtml', 'r', encoding='utf-8-sig', errors='ignore') as f:
    text = f.read()

# Add currentUserId
if 'const currentUserId = @userId;' not in text:
    text = text.replace('const userId = @userId;', 'const userId = @userId;\n            const currentUserId = @userId;')

# Fix mojibake
text = text.replace('Lá»—i khi táº£i bÃ i viáº¿t.', 'Lỗi khi tải bài viết.')
text = text.replace('NgÆ°á»\x8di dÃ¹ng', 'Người dùng')
text = text.replace('ChÆ°a cÃ³ ngÆ°á»\x8di báº¡n nÃ\xa0o.', 'Chưa có người bạn nào.')
text = text.replace('Xem táº¥t cáº£', 'Xem tất cả')
text = text.replace('Ä\x90ang táº£i...', 'Đang tải...')
text = text.replace('ChÆ°a cÃ³ bÃ¬nh luáº\xadn nÃ\xa0o.', 'Chưa có bình luận nào.')

# Add delete button to comment html
old_html = '''<div class="flex items-center justify-between mb-1.5">
                                <span class="font-bold text-slate-900 text-sm" dark:text-slate-100>${name}</span>
                                <span class="text-[11px] font-medium text-slate-500" dark:text-slate-400>${c.createdAt ? new Date(c.createdAt).toLocaleDateString() : ""}</span>
                            </div>'''
new_html = '''<div class="flex items-center justify-between mb-1.5">
                                <span class="font-bold text-slate-900 text-sm" dark:text-slate-100>${name}</span>
                                <div class="flex items-center gap-2">
                                    <span class="text-[11px] font-medium text-slate-500" dark:text-slate-400>${c.createdAt ? new Date(c.createdAt).toLocaleDateString() : ""}</span>
                                    ${c.userId === currentUserId ? `<button onclick="deleteComment(${c.id}, this)" class="text-slate-400 hover:text-red-500 transition"><i class="fa-solid fa-trash text-xs"></i></button>` : ''}
                                </div>
                            </div>'''
text = text.replace(old_html, new_html)

# Add deleteComment func
funcs = '''
        async function deleteComment(commentId, btn) {
            if(!confirm("Bạn có chắc chắn muốn xóa bình luận này?")) return;
            try {
                const res = await fetch(`/api/comments/${commentId}`, { method: "DELETE" });
                if(res.ok) {
                    btn.closest('.flex.gap-3').remove();
                } else {
                    alert("Không thể xóa bình luận.");
                }
            } catch(e) { console.error(e); }
        }
        
        async function deletePost(postId) {
            if(!confirm("Bạn có chắc chắn muốn xóa bài viết này?")) return;
            try {
                const res = await fetch(`/api/posts/${postId}`, { method: "DELETE" });
                if(res.ok) {
                    alert("Đã xóa bài viết.");
                    window.location.reload();
                } else {
                    alert("Lỗi khi xóa bài viết.");
                }
            } catch(e) { console.error(e); }
        }
'''
if 'async function deleteComment' not in text:
    text = text.replace('async function loadProfileStats', funcs + '\n        async function loadProfileStats')

# Add delete post to modal
if 'deletePost(' not in text:
    text = text.replace('<button type="button" id="closePostModal"', '''<button type="button" id="deletePostBtn" class="hidden text-red-500 hover:bg-red-50 transition-colors w-8 h-8 flex items-center justify-center rounded-full" title="Xóa bài viết"><i class="fa-solid fa-trash"></i></button>
              <button type="button" id="closePostModal"''')
    
    text = text.replace('document.getElementById("postModalLikes").textContent = post.likesCount ?? 0;', '''document.getElementById("postModalLikes").textContent = post.likesCount ?? 0;
            const delBtn = document.getElementById("deletePostBtn");
            if(post.userId === currentUserId) {
                delBtn.classList.remove('hidden');
                delBtn.onclick = () => deletePost(post.id);
            } else {
                delBtn.classList.add('hidden');
            }''')

with open('d:/okemsocial/Views/Profile/Me.cshtml', 'w', encoding='utf-8-sig') as f:
    f.write(text)
print('Done Me.cshtml!')

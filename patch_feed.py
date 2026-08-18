import os
with open('d:/okemsocial/Views/Posts/Feed.cshtml', 'r', encoding='utf-8-sig', errors='ignore') as f:
    text = f.read()

target = '''                        @if (!string.IsNullOrEmpty(post.VideoUrl))
                        {
                            <div class="mb-4 overflow-hidden rounded-2xl bg-slate-50 dark:bg-neutral-900">
                                <video src="@post.VideoUrl" controls class="max-h-[500px] w-full bg-black"></video>
                            </div>
                        }'''

replacement = target + '''
                        <!-- Original Post (Quote Share) -->
                        @if (post.OriginalPost != null)
                        {
                            var op = post.OriginalPost;
                            var opUser = op.User;
                            var opName = opUser?.FullName ?? opUser?.Email ?? "Người dùng";
                            var opAvatar = opUser?.AvatarUrl;
                            var opAvatarLetter = !string.IsNullOrWhiteSpace(opName) ? opName.Trim()[0].ToString().ToUpper() : "U";
                            <div class="mb-4 rounded-2xl border border-slate-200 bg-slate-50/50 p-4 dark:border-white/10 dark:bg-neutral-800">
                                <div class="flex items-center gap-2 mb-2">
                                    @if(!string.IsNullOrEmpty(opAvatar)) {
                                        <img src="@opAvatar" class="w-5 h-5 rounded-full object-cover" />
                                    } else {
                                        <div class="w-5 h-5 rounded-full bg-slate-800 text-white flex items-center justify-center text-[10px] font-bold">@opAvatarLetter</div>
                                    }
                                    <span class="text-xs font-bold text-slate-600 dark:text-slate-300">@opName</span>
                                    <span class="text-[10px] text-slate-400">@op.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy")</span>
                                </div>
                                @if (!string.IsNullOrEmpty(op.Caption)) {
                                    <p class="text-[14px] leading-relaxed text-slate-700 whitespace-pre-wrap dark:text-slate-300 mb-3">@op.Caption</p>
                                }
                                @if (!string.IsNullOrEmpty(op.ImageUrl)) {
                                    <div class="overflow-hidden rounded-xl bg-slate-200 dark:bg-neutral-900 mb-2">
                                        <img src="@op.ImageUrl" class="max-h-[300px] w-full object-cover" />
                                    </div>
                                }
                            </div>
                        }
'''
text = text.replace(target, replacement)

with open('d:/okemsocial/Views/Posts/Feed.cshtml', 'w', encoding='utf-8-sig') as f:
    f.write(text)
print('Done patching Feed.cshtml')

// wwwroot/js/chat.js

let chatConnection = null;
let currentConversationId = 0;
let currentUserId = 0;

// Helper: flag boolean từ server
function isFlagTrue(v) {
    return v === true || v === "true" || v === "True";
}

// Helper: xác định tin nhắn có phải của user hiện tại
function isMyMessage(msg) {
    if (!msg) return false;

    if (isFlagTrue(msg.isMine)) return true;

    let senderId = null;
    if (msg.senderId != null) senderId = msg.senderId;
    else if (msg.sender && msg.sender.id != null) senderId = msg.sender.id;
    else if (msg.userId != null) senderId = msg.userId;

    if (senderId != null) {
        const idNum = parseInt(senderId, 10);
        if (!Number.isNaN(idNum) && idNum === currentUserId) return true;
    }

    return false;
}

// Khởi động SignalR
async function startChatConnection() {
    if (chatConnection) return;

    chatConnection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/chat")
        .withAutomaticReconnect()
        .build();

    chatConnection.on("ReceiveMessage", (msg) => {
        console.log("ReceiveMessage:", msg);
        handleReceiveMessage(msg);
    });

    chatConnection.on("UserTyping", (data) => {
        console.log("UserTyping:", data);
    });

    chatConnection.on("MessageSeen", (data) => {
        console.log("MessageSeen:", data);
    });

    try {
        await chatConnection.start();
        console.log("SignalR connected");

        if (currentConversationId) {
            await joinConversationGroup(currentConversationId);
        }
    } catch (err) {
        console.error("Error starting SignalR connection:", err);
    }
}

async function joinConversationGroup(conversationId) {
    if (!chatConnection) return;
    try {
        await chatConnection.invoke("JoinConversation", conversationId);
        console.log("Joined conversation group:", conversationId);
    } catch (err) {
        console.error("JoinConversation error:", err);
    }
}

// Khi nhận tin nhắn realtime từ server
function handleReceiveMessage(msg) {
    if (!msg || !msg.conversationId) return;

    // QUAN TRỌNG: Bỏ qua tin nhắn của chính mình để tránh duplicate
    // vì đã có local echo khi gửi
    if (isMyMessage(msg)) {
        console.log("Skipping own message (already shown via local echo)");

        // Chỉ cập nhật last message trong conversation list
        const convItem = document.querySelector(
            `[data-conversation-id="${msg.conversationId}"] .conv-last-message`
        );
        if (convItem) {
            convItem.textContent = msg.content || "";
        }
        return;
    }

    // Chỉ hiển thị tin nhắn của người khác
    if (msg.conversationId === currentConversationId) {
        appendMessageToUi(msg);
    }

    // Cập nhật last message
    const convItem = document.querySelector(
        `[data-conversation-id="${msg.conversationId}"] .conv-last-message`
    );
    if (convItem) {
        convItem.textContent = msg.content || "";
    }
}

// Vẽ 1 bubble ra UI
function appendMessageToUi(msg) {
    const container = document.getElementById("chatMessages");
    if (!container) return;

    const mine = isMyMessage(msg);
    const wrapper = document.createElement("div");
    wrapper.className = `message-item ${mine ? "sent" : "received"}`;
    wrapper.dataset.messageId = msg.id ?? "";

    if (mine) {
        // Tin nhắn của mình (bên phải)
        wrapper.innerHTML = `
            <div class="flex justify-end">
                <div class="message-bubble sent-bubble max-w-[75%] sm:max-w-[65%] rounded-2xl rounded-br-sm bg-slate-950 text-white px-4 py-2.5 shadow-sm" dark:bg-white dark:text-black>
                    <p class="message-text text-sm leading-relaxed text-white break-words"></p>
                    <small class="message-time block text-[10px] text-slate-400 mt-1 text-right"></small>
                </div>
            </div>
        `;
    } else {
        // Tin nhắn của người kia (bên trái)
        const fullName =
            (msg.sender && (msg.sender.fullName || msg.sender.email)) ||
            msg.senderFullName ||
            msg.senderName ||
            "Người dùng";
        const initial = fullName ? fullName.charAt(0).toUpperCase() : "?";
        const avatarUrl = msg.sender?.avatarUrl;
        
        const avatarHtml = avatarUrl
            ? `<img src="${avatarUrl}" alt="${fullName}" class="avatar-circle avatar-sm h-8 w-8 rounded-full object-cover shrink-0 mt-0.5" />`
            : `<div class="avatar-circle avatar-sm flex h-8 w-8 items-center justify-center rounded-full bg-gradient-to-br from-slate-800 to-slate-600 text-white text-xs font-bold shrink-0 mt-0.5">
                   ${initial}
               </div>`;

        wrapper.innerHTML = `
            <div class="flex items-start gap-2.5 justify-start">
                ${avatarHtml}
                <div class="message-bubble received-bubble max-w-[75%] sm:max-w-[65%] rounded-2xl rounded-bl-sm bg-white border border-slate-100 text-slate-900 px-4 py-2.5 shadow-card" dark:text-slate-100 dark:border-white/10 dark:bg-black>
                    <div class="text-[11px] font-bold text-slate-900 mb-0.5" dark:text-slate-100>${fullName}</div>
                    <p class="message-text text-sm leading-relaxed text-slate-800 break-words" dark:text-slate-100></p>
                    <small class="message-time text-muted block text-[10px] text-slate-400 mt-1"></small>
                </div>
            </div>
        `;
    }

    const contentEl = wrapper.querySelector(".message-text");
    const timeEl = wrapper.querySelector(".message-time");

    if (contentEl) {
        contentEl.textContent = msg.content || "";
    }

    if (timeEl && msg.createdAt) {
        try {
            const d = new Date(msg.createdAt);
            if (!isNaN(d.getTime())) {
                const hh = d.getHours().toString().padStart(2, "0");
                const mm = d.getMinutes().toString().padStart(2, "0");
                timeEl.textContent = `${hh}:${mm}`;
            }
        } catch {
            // ignore
        }
    }

    container.appendChild(wrapper);
    container.scrollTop = container.scrollHeight;
}

// Gửi tin nhắn (có local echo giống Zalo/Messenger)
async function sendMessageRealtime(content, attachmentUrl = null) {
    if (!chatConnection) return;
    if (!currentConversationId) return;
    if (!content && !attachmentUrl) return;

    // local echo: hiển thị ngay bên phải
    const tempId = "local-" + Date.now();
    appendMessageToUi({
        id: tempId,
        conversationId: currentConversationId,
        content,
        createdAt: new Date().toISOString(),
        isMine: true
    });

    try {
        await chatConnection.invoke(
            "SendMessage",
            currentConversationId,
            content,
            attachmentUrl
        );
    } catch (err) {
        console.error("SendMessage error:", err);
    }
}

// Gắn event cho form gửi tin
function wireChatFormEvents() {
    const chatForm = document.getElementById("formSendMessage");
    const messagesBox = document.getElementById("chatMessages");

    if (!chatForm || !messagesBox) return;

    const textarea = chatForm.querySelector("textarea[name='content']");
    if (!textarea) return;

    chatForm.addEventListener("submit", async (e) => {
        e.preventDefault();

        const text = textarea.value.trim();
        if (!text) return;

        await sendMessageRealtime(text, null);
        textarea.value = "";
        textarea.focus();
    });
}

// Load 1 cuộc trò chuyện bằng AJAX
async function loadConversation(conversationId, url) {
    const rightPanel = document.getElementById("chatPanel");
    if (!rightPanel) return;

    try {
        rightPanel.innerHTML = `
            <div class="d-flex justify-content-center align-items-center h-100">
                <div class="spinner-border" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>`;

        const res = await fetch(url, {
            headers: { "X-Requested-With": "XMLHttpRequest" }
        });

        if (!res.ok) {
            rightPanel.innerHTML = `<div class="alert alert-danger m-3">Không thể tải đoạn chat.</div>`;
            return;
        }

        const html = await res.text();
        rightPanel.innerHTML = html;

        const chatRoot = rightPanel.querySelector("[data-chat-root='1']");
        if (chatRoot) {
            currentConversationId = parseInt(
                chatRoot.getAttribute("data-conversation-id") || "0",
                10
            );
            currentUserId = parseInt(
                chatRoot.getAttribute("data-current-user-id") || "0",
                10
            );
            console.log("Loaded conversation", {
                currentConversationId,
                currentUserId
            });
        }

        if (currentConversationId) {
            await joinConversationGroup(currentConversationId);
        }

        wireChatFormEvents();

        const messagesBox = document.getElementById("chatMessages");
        if (messagesBox) {
            messagesBox.scrollTop = messagesBox.scrollHeight;
        }
    } catch (err) {
        console.error("loadConversation error:", err);
        rightPanel.innerHTML = `<div class="alert alert-danger m-3">Lỗi khi tải đoạn chat.</div>`;
    }
}

// Event click trên danh sách conversation / friend
function wireIndexEvents() {
    document.addEventListener("click", (e) => {
        const convItem = e.target.closest("[data-open-conversation-url]");
        if (convItem) {
            const convId = parseInt(
                convItem.getAttribute("data-conversation-id") || "0",
                10
            );
            const url = convItem.getAttribute("data-open-conversation-url");
            if (convId && url) {
                loadConversation(convId, url);
            }
        }
    });

    document.addEventListener("click", (e) => {
        const friendItem = e.target.closest("[data-chat-with-url]");
        if (friendItem) {
            const friendId = parseInt(
                friendItem.getAttribute("data-friend-id") || "0",
                10
            );
            const url = friendItem.getAttribute("data-chat-with-url");
            if (friendId && url) {
                loadConversation(0, url);   
            }
        }
    });
}

// Khởi tạo
document.addEventListener("DOMContentLoaded", () => {
    console.log("Initializing chat interface...");
    startChatConnection();
    wireIndexEvents();
});
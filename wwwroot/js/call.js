// wwwroot/js/call.js
// Chức năng gọi thoại/video giống Messenger/Instagram

// ============================================================
// 🛑 FIX QUAN TRỌNG: NGĂN CHẶN LOAD FILE 2 LẦN
// ============================================================
if (window.isCallJsLoaded) {
    console.warn("⚠️ call.js đã chạy trước đó. Hủy bỏ lần khởi tạo này.");
    throw new Error("CALL_SCRIPT_ALREADY_LOADED");
}
window.isCallJsLoaded = true;
// ============================================================

console.log("📞 call.js loaded successfully!");

let callConnection = null;
let peerConnection = null;
let localStream = null;
let remoteStream = null;
let currentCallTargetUserId = null;
let currentCallIsVideo = false;
let callStartTime = null;
let callDurationInterval = null;
let isAudioEnabled = true;
let isVideoEnabled = true;

// ICE servers configuration (STUN/TURN)
const iceServers = {
    iceServers: [
        { urls: "stun:stun.l.google.com:19302" },
        { urls: "stun:stun1.l.google.com:19302" },
        { urls: "stun:stun2.l.google.com:19302" },
    ]
};

// --- [MỚI] HÀM CƯỠNG CHẾ ẨN LOADING (GIỮ LOGIC AVATAR CHO VOICE CALL) ---
function forceHideLoading() {
    console.log("⚡ Force hiding loading overlay...");

    // Tìm overlay theo cả ID và Class để chắc chắn tìm thấy
    const overlay = document.getElementById('loadingOverlay') || document.querySelector('.call-overlay');
    const statusText = document.getElementById('callStatusText');

    if (overlay) {
        if (currentCallIsVideo) {
            // VIDEO CALL: Ẩn hoàn toàn overlay để lộ Video
            overlay.style.display = 'none';
            overlay.classList.add('hidden'); // Thêm class hidden để CSS transition nếu có
        } else {
            // VOICE CALL: Giữ overlay (để hiện Avatar) nhưng ẨN CÁI XOAY XOAY (Spinner)
            overlay.style.display = 'flex'; // Đảm bảo overlay hiện
            overlay.classList.remove('hidden');

            const spinner = overlay.querySelector('.spinner-border');
            if (spinner) {
                spinner.style.display = 'none'; // Chỉ ẩn cái vòng xoay
            }
        }
    }

    // Nếu chưa bắt đầu đếm giờ thì bắt đầu ngay (UX trick)
    if (statusText && statusText.innerText === "Đang kết nối...") {
        statusText.innerText = "00:00";
    }
}
// ---------------------------------------

// Ringtone audio
let ringtoneAudio = null;

function playRingtone() {
    try {
        if (!ringtoneAudio) {
            ringtoneAudio = new Audio('data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBTGH0fPTgjMGHm7A7+OZSA0PVqzn77BdGAg+ltryxnMpBSl+zPLaizsIGGS57OihUBALTqXh8bllHAU2jdXz0H8wBSF1xe/glEILElyx6+ytWBUIQ5zd8sFuJAUuhM/z24Y7CBlpvO3nn1EMDVG n6/C2YxwGN4/X88p3KwUld8rx3Y9AChRfsunrp1QUCkef4PK+bCAFMIjR89OCMgYfb8Tv45lIDQ9Xq+fwsF0YCD6W2vLGcykFKX7M8tqLOwgYZLns6KFQEAtOpOHxuWUcBTaN1fPQfzAFIXXF7+CUQgsRXLHr7K1YFQhDnN3ywW4kBS6Ez/PbhjsIGWm87eefUQwNUajr8LZjHAY3j9fyyncqBSV3yvHdj0AKFFyx6eqmVBQKR5/g8r1rIAUxiNHz04IyBh9vxO/jmUgND1er5/CwXRgIPpba8sZzKQUpfszyTOSFZU2wYAACAASURBVHhe7J0HYNv...');
            ringtoneAudio.loop = true;
        }
        ringtoneAudio.play().catch(e => console.log('Cannot play ringtone:', e));
    } catch (e) {
        console.log('Ringtone error:', e);
    }
}

function stopRingtone() {
    if (ringtoneAudio) {
        ringtoneAudio.pause();
        ringtoneAudio.currentTime = 0;
    }
}

// Lấy các element trong DOM
function getCallElements() {
    return {
        callContainer: document.getElementById("callContainer"),
        localVideo: document.getElementById("localVideo"),
        remoteVideo: document.getElementById("remoteVideo"),
        callStatusText: document.getElementById("callStatusText"),
        callTitle: document.getElementById("callTitle"),
    };
}

// Khởi tạo SignalR connection
async function initCallConnection() {
    if (callConnection && (callConnection.state === signalR.HubConnectionState.Connected || callConnection.state === signalR.HubConnectionState.Connecting)) {
        return;
    }

    callConnection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/call")
        .withAutomaticReconnect([0, 2000, 5000, 10000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Event handlers
    callConnection.on("IncomingCall", onIncomingCall);
    callConnection.on("CallAnswered", onCallAnswered);
    callConnection.on("IceCandidateReceived", onIceCandidateReceived);
    callConnection.on("CallEnded", onCallEnded);
    callConnection.on("CallRejected", onCallRejected);
    callConnection.on("CallError", onCallError);
    callConnection.on("PeerVideoToggled", onPeerVideoToggled);
    callConnection.on("PeerAudioToggled", onPeerAudioToggled);

    // Reconnection handlers
    callConnection.onreconnecting((error) => {
        console.log("CallHub reconnecting...", error);
        updateCallStatus("Đang kết nối lại...");
    });

    callConnection.onreconnected((connectionId) => {
        console.log("CallHub reconnected:", connectionId);
        if (currentCallTargetUserId) {
            updateCallStatus("Đã kết nối");
        }
    });

    callConnection.onclose((error) => {
        console.log("CallHub connection closed", error);
        if (currentCallTargetUserId) {
            showNotification("Mất kết nối. Cuộc gọi đã kết thúc.", "error");
            cleanupCall(false);
        }
    });

    try {
        await callConnection.start();
        console.log("CallHub connected successfully");
    } catch (err) {
        console.error("CallHub connection error:", err);
        showNotification("Không thể kết nối dịch vụ gọi", "error");
    }
}

// 🚀 AUTO-INIT: Khởi tạo CallHub ngay lập tức
console.log("🎬 Starting auto-initialization...");
setTimeout(() => {
    console.log("🔌 Connecting to CallHub...");
    initCallConnection()
        .then(() => console.log("✅ CallHub ready!"))
        .catch(err => console.warn("⚠️ CallHub init failed:", err.message));
}, 500);

// Tạo RTCPeerConnection
function createPeerConnection(targetUserId) {
    try {
        peerConnection = new RTCPeerConnection(iceServers);

        // Handle ICE candidates
        peerConnection.onicecandidate = (event) => {
            if (event.candidate) {
                callConnection.invoke("SendIceCandidate", targetUserId, event.candidate)
                    .catch(err => console.error("SendIceCandidate error:", err));
            }
        };

        // Handle remote track
        peerConnection.ontrack = (event) => {
            console.log("Received remote track:", event.track.kind);
            const [stream] = event.streams;
            remoteStream = stream;

            const { remoteVideo } = getCallElements();

            // 1. Gắn Video Stream
            if (remoteVideo) {
                remoteVideo.srcObject = remoteStream;
                remoteVideo.onloadedmetadata = () => {
                    remoteVideo.play().catch(e => console.warn("Remote video play error:", e));
                    // Vẫn gọi ở đây để chắc chắn
                    forceHideLoading();
                };
            }

            // 2. Gắn Audio Stream (Nếu có thẻ audio ẩn cho voice call)
            const remoteAudio = document.getElementById('remoteAudio');
            if (remoteAudio) {
                remoteAudio.srcObject = remoteStream;
                remoteAudio.play().catch(e => console.warn("Remote audio play error:", e));
            }

            updateCallStatus("Đã kết nối");
            startCallDuration();

            // ⚡ [FIX] Có Stream là ẩn loading NGAY LẬP TỨC
            forceHideLoading();
        };

        // Handle connection state changes
        peerConnection.onconnectionstatechange = () => {
            console.log("Connection state:", peerConnection.connectionState);

            switch (peerConnection.connectionState) {
                case "connected":
                    updateCallStatus("Đã kết nối");
                    // ⚡ [FIX] Đã kết nối P2P là ẩn loading NGAY LẬP TỨC (Không chờ video)
                    forceHideLoading();
                    break;
                case "disconnected":
                    updateCallStatus("Mất kết nối");
                    break;
                case "failed":
                    showNotification("Kết nối thất bại", "error");
                    cleanupCall(true);
                    break;
                case "closed":
                    cleanupCall(false);
                    break;
            }
        };

        // Handle ICE connection state
        peerConnection.oniceconnectionstatechange = () => {
            if (peerConnection.iceConnectionState === "failed") {
                peerConnection.restartIce();
            }
        };

        return peerConnection;
    } catch (err) {
        console.error("Error creating peer connection:", err);
        showNotification("Không thể tạo kết nối P2P", "error");
        throw err;
    }
}

// Lấy media stream (audio/video)
async function getLocalMedia(isVideo) {
    try {
        const constraints = {
            audio: {
                echoCancellation: true,
                noiseSuppression: true,
                autoGainControl: true
            },
            video: isVideo ? {
                width: { ideal: 1280 },
                height: { ideal: 720 },
                facingMode: "user"
            } : false
        };

        localStream = await navigator.mediaDevices.getUserMedia(constraints);

        const { localVideo } = getCallElements();
        if (localVideo) {
            localVideo.srcObject = localStream;
            localVideo.onloadedmetadata = () => {
                localVideo.play().catch(e => console.warn("Local video play error:", e));
            };
        }

        isAudioEnabled = true;
        isVideoEnabled = isVideo;

        return localStream;
    } catch (err) {
        console.error("Error getting media:", err);
        let errorMessage = "Không thể truy cập camera/microphone";
        if (err.name === "NotAllowedError") errorMessage = "❌ Quyền truy cập bị từ chối";
        else if (err.name === "NotFoundError") errorMessage = "❌ Không tìm thấy thiết bị";
        else if (err.name === "NotReadableError") errorMessage = "❌ Thiết bị đang bận";

        alert(errorMessage);
        showNotification(errorMessage, "error");
        throw err;
    }
}

// Bắt đầu cuộc gọi (caller)
async function startCall(targetUserId, isVideo) {
    if (!targetUserId) {
        showNotification("Không xác định người nhận", "error");
        return;
    }
    try { await initCallConnection(); } catch (err) { return; }

    const elements = getCallElements();
    if (!elements.callContainer) return;

    currentCallTargetUserId = targetUserId.toString();
    currentCallIsVideo = isVideo;

    // Hiện UI
    elements.callContainer.classList.remove("d-none");

    // Reset Overlay: Hiện lại Spinner để báo đang gọi
    const overlay = document.getElementById('loadingOverlay') || document.querySelector('.call-overlay');
    if (overlay) {
        overlay.style.display = 'flex';
        overlay.classList.remove('hidden');
        const spinner = overlay.querySelector('.spinner-border');
        if (spinner) spinner.style.display = 'inline-block';
    }

    updateCallStatus("Đang gọi...");

    // Ẩn/Hiện Video Local/Remote tùy loại cuộc gọi
    if (elements.localVideo) elements.localVideo.style.display = isVideo ? "block" : "none";
    if (elements.remoteVideo) elements.remoteVideo.style.display = isVideo ? "block" : "none";

    try {
        await getLocalMedia(isVideo);
        createPeerConnection(currentCallTargetUserId);
        localStream.getTracks().forEach(track => peerConnection.addTrack(track, localStream));

        const offer = await peerConnection.createOffer({
            offerToReceiveAudio: true,
            offerToReceiveVideo: isVideo
        });

        await peerConnection.setLocalDescription(offer);
        await callConnection.invoke("CallUser", currentCallTargetUserId, offer, isVideo);
    } catch (err) {
        console.error("Error starting call:", err);
        cleanupCall(false);
    }
}

// Nhận cuộc gọi đến (callee)
async function onIncomingCall(payload) {
    const { fromUserId, offer, isVideo } = payload;

    if (currentCallTargetUserId) {
        await callConnection.invoke("RejectCall", fromUserId);
        return;
    }

    playRingtone();

    // Custom Notification UI
    const notifId = 'call-notif-' + Date.now();
    const notif = document.createElement('div');
    notif.id = notifId;
    notif.style.cssText = `
        position: fixed; top: 20px; right: 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white; padding: 20px 30px; border-radius: 12px; box-shadow: 0 10px 40px rgba(0,0,0,0.3);
        z-index: 10000; min-width: 320px; font-family: system-ui, sans-serif; animation: slideIn 0.3s ease-out;
    `;

    notif.innerHTML = `
        <style>@keyframes slideIn { from { transform: translateX(400px); opacity: 0; } to { transform: translateX(0); opacity: 1; } }</style>
        <div style="font-size: 24px; margin-bottom: 8px;">🔔 Cuộc gọi ${isVideo ? 'Video' : 'Thoại'}</div>
        <div style="font-size: 14px; opacity: 0.9; margin-bottom: 16px;">Bạn có muốn nhận không?</div>
        <div style="display: flex; gap: 12px;">
            <button id="btn-accept-${notifId}" style="flex: 1; background: #10b981; border: none; color: white; padding: 12px; border-radius: 8px; font-weight: 600; cursor: pointer;">✅ Nhận</button>
            <button id="btn-reject-${notifId}" style="flex: 1; background: #ef4444; border: none; color: white; padding: 12px; border-radius: 8px; font-weight: 600; cursor: pointer;">❌ Từ chối</button>
        </div>
    `;

    document.body.appendChild(notif);

    const removeNotif = () => {
        stopRingtone();
        if (document.body.contains(notif)) document.body.removeChild(notif);
    };

    // Timeout 30s
    const timeoutId = setTimeout(async () => {
        if (document.body.contains(notif)) {
            removeNotif();
            await callConnection.invoke("RejectCall", fromUserId);
        }
    }, 30000);

    document.getElementById(`btn-accept-${notifId}`).onclick = async () => {
        clearTimeout(timeoutId);
        removeNotif();
        await handleAcceptCall(fromUserId, offer, isVideo);
    };

    document.getElementById(`btn-reject-${notifId}`).onclick = async () => {
        clearTimeout(timeoutId);
        removeNotif();
        await callConnection.invoke("RejectCall", fromUserId);
    };
}

// Xử lý chấp nhận cuộc gọi
async function handleAcceptCall(fromUserId, offer, isVideo) {
    try { await initCallConnection(); } catch (err) { return; }

    const elements = getCallElements();
    if (!elements.callContainer) return;

    currentCallTargetUserId = fromUserId.toString();
    currentCallIsVideo = isVideo;

    // Hiện UI
    elements.callContainer.classList.remove("d-none");

    // Reset Overlay: Hiện Spinner để báo đang kết nối
    const overlay = document.getElementById('loadingOverlay') || document.querySelector('.call-overlay');
    if (overlay) {
        overlay.style.display = 'flex';
        overlay.classList.remove('hidden');
        const spinner = overlay.querySelector('.spinner-border');
        if (spinner) spinner.style.display = 'inline-block';
    }

    updateCallStatus("Đang kết nối...");

    // Ẩn/Hiện Video
    if (elements.localVideo) elements.localVideo.style.display = isVideo ? "block" : "none";
    if (elements.remoteVideo) elements.remoteVideo.style.display = isVideo ? "block" : "none";

    try {
        await getLocalMedia(isVideo);
        createPeerConnection(currentCallTargetUserId);
        localStream.getTracks().forEach(track => peerConnection.addTrack(track, localStream));
        await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
        const answer = await peerConnection.createAnswer();
        await peerConnection.setLocalDescription(answer);
        await callConnection.invoke("AnswerCall", currentCallTargetUserId, answer);
    } catch (err) {
        console.error("Error answering call:", err);
        cleanupCall(false);
    }
}

async function onCallAnswered(payload) {
    if (!peerConnection) return;
    try {
        await peerConnection.setRemoteDescription(new RTCSessionDescription(payload.answer));
        updateCallStatus("Đang kết nối...");
    } catch (err) {
        cleanupCall(true);
    }
}

async function onIceCandidateReceived(payload) {
    if (!peerConnection) return;
    try {
        await peerConnection.addIceCandidate(new RTCIceCandidate(payload.candidate));
    } catch (err) { console.error("ICE Error:", err); }
}

function onCallEnded(payload) {
    showNotification(payload.reason === "disconnected" ? "Người dùng ngắt kết nối" : "Cuộc gọi kết thúc", "info");
    cleanupCall(false);
}

function onCallRejected(payload) {
    showNotification("Cuộc gọi bị từ chối", "warning");
    cleanupCall(false);
}

function onCallError(payload) {
    showNotification(payload.message === "User is busy" ? "Người dùng đang bận" : "Có lỗi xảy ra", "error");
    cleanupCall(false);
}

// Toggle Mic/Cam
async function toggleAudio() {
    if (!localStream) return;
    const track = localStream.getAudioTracks()[0];
    if (track) {
        isAudioEnabled = !isAudioEnabled;
        track.enabled = isAudioEnabled;
        if (callConnection) await callConnection.invoke("ToggleAudio", currentCallTargetUserId, isAudioEnabled);
        showNotification(isAudioEnabled ? "Đã bật mic" : "Đã tắt mic", "info");
    }
}

async function toggleVideo() {
    if (!localStream || !currentCallIsVideo) return;
    const track = localStream.getVideoTracks()[0];
    if (track) {
        isVideoEnabled = !isVideoEnabled;
        track.enabled = isVideoEnabled;
        if (callConnection) await callConnection.invoke("ToggleVideo", currentCallTargetUserId, isVideoEnabled);
        showNotification(isVideoEnabled ? "Đã bật camera" : "Đã tắt camera", "info");
    }
}

// Peer Toggle Event Handler (có thể mở rộng để update UI icon mic/cam của đối phương)
function onPeerVideoToggled(payload) { console.log("Peer video:", payload.enabled); }
function onPeerAudioToggled(payload) { console.log("Peer audio:", payload.enabled); }

function updateCallStatus(status) {
    const { callStatusText } = getCallElements();
    if (callStatusText) callStatusText.textContent = status;
}

function startCallDuration() {
    if (callDurationInterval) clearInterval(callDurationInterval);
    callStartTime = Date.now();
    callDurationInterval = setInterval(() => {
        const elapsed = Date.now() - callStartTime;
        const minutes = Math.floor(elapsed / 60000);
        const seconds = Math.floor((elapsed % 60000) / 1000);
        updateCallStatus(`${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`);
    }, 1000);
}

async function cleanupCall(needNotifyOtherSide) {
    if (callDurationInterval) { clearInterval(callDurationInterval); callDurationInterval = null; }
    callStartTime = null;

    if (peerConnection) { peerConnection.close(); peerConnection = null; }
    if (localStream) { localStream.getTracks().forEach(t => t.stop()); localStream = null; }
    if (remoteStream) { remoteStream.getTracks().forEach(t => t.stop()); remoteStream = null; }

    const elements = getCallElements();
    if (elements.localVideo) elements.localVideo.srcObject = null;
    if (elements.remoteVideo) elements.remoteVideo.srcObject = null;
    if (elements.callContainer) elements.callContainer.classList.add("d-none");

    // Reset Audio element nếu có
    const remoteAudio = document.getElementById('remoteAudio');
    if (remoteAudio) remoteAudio.srcObject = null;

    if (needNotifyOtherSide && callConnection && currentCallTargetUserId) {
        try { await callConnection.invoke("HangUp", currentCallTargetUserId); } catch (e) { }
    }

    currentCallTargetUserId = null;
    currentCallIsVideo = false;
}

async function hangUp() { await cleanupCall(true); }
function showNotification(msg, type) { console.log(`[${type}] ${msg}`); }

// Event Delegation cho các nút bấm
document.addEventListener("click", async (e) => {
    const btn = e.target.closest("[data-call]");
    if (!btn) return;

    // Tránh submit form nếu nút nằm trong form
    e.preventDefault();

    const action = btn.getAttribute("data-call");
    const uid = btn.getAttribute("data-target-user-id");

    if (action === 'voice') await startCall(uid, false);
    else if (action === 'video') await startCall(uid, true);
    else if (action === 'hangup') await hangUp();
    else if (action === 'toggle-audio') await toggleAudio();
    else if (action === 'toggle-video') await toggleVideo();
});
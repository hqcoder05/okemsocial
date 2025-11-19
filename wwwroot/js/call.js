// wwwroot/js/call.js

console.log("📞 call.js loaded!");

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

const iceServers = {
    iceServers: [
        { urls: "stun:stun.l.google.com:19302" },
        { urls: "stun:stun1.l.google.com:19302" }
    ]
};

let ringtoneAudio = null;
function playRingtone() {
    try {
        if (!ringtoneAudio) {
            ringtoneAudio = new Audio('data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBTGH0fPTgjMGHm7A7+OZSA0PVqzn77BdGAg+ltryxnMpBSl+zPLaizsIGGS57OihUBALTqXh8bllHAU2jdXz0H8wBSF1xe/glEILElyx6+ytWBUIQ5zd8sFuJAUuhM/z24Y7CBlpvO3nn1EMDVGn6/C2YxwGN4/X88p3KwUld8rx3Y9AChRfsunrp1QUCkef4PK+bCAFMIjR89OCMgYfb8Tv45lIDQ9Xq+fwsF0YCD6W2vLGcykFKX7M8tqLOwgYZLns6KFQEAtOpOHxuWUcBTaN1fPQfzAFIXXF7+CUQgsRXLHr7K1YFQhDnN3ywW4kBS6Ez/PbhjsIGWm87eefUQwNUajr8LZjHAY3j9fyyncqBSV3yvHdj0AKFFyx6eqmVBQKR5/g8r1rIAUxiNHz04IyBh9vxO/jmUgND1er5/CwXRgIPpba8sZzKQUpfszyTOSFZU2wYAACAASURBVHhe7J0HYNv...');
            ringtoneAudio.loop = true;
        }
        ringtoneAudio.play().catch(e => { });
    } catch (e) { }
}
function stopRingtone() {
    if (ringtoneAudio) { ringtoneAudio.pause(); ringtoneAudio.currentTime = 0; }
}

// Ensure a remote audio element exists so audio-only streams have a real target
function ensureRemoteAudioExists() {
    let a = document.getElementById('remoteAudio');
    if (!a) {
        a = document.createElement('audio');
        a.id = 'remoteAudio';
        a.autoplay = true;
        a.style.display = 'none';
        const container = document.getElementById('callContainer') || document.body;
        container.appendChild(a);
    }
    return a;
}

function getCallElements() {
    return {
        callContainer: document.getElementById("callContainer"),
        localVideo: document.getElementById("localVideo"),
        remoteVideo: document.getElementById("remoteVideo"),
        remoteAudio: document.getElementById("remoteAudio") || ensureRemoteAudioExists(),
        callStatusText: document.getElementById("callStatusText"),
        callTitle: document.getElementById("callTitle"),
    };
}

// Helper: unified overlay show/hide (use .call-overlay from Chat.cshtml)
function setOverlayVisible(show) {
    const overlay = document.querySelector('.call-overlay') || document.getElementById('loadingOverlay');
    if (!overlay) return;
    if (show) {
        overlay.classList.remove('hidden');
        overlay.style.display = 'flex';
    } else {
        overlay.classList.add('hidden');
        setTimeout(() => { if (overlay.classList.contains('hidden')) overlay.style.display = 'none'; }, 300);
    }
}

async function initCallConnection() {
    if (callConnection && callConnection.state === signalR.HubConnectionState.Connected) return;
    if (!callConnection) {
        callConnection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/call")
            .withAutomaticReconnect([0, 2000, 5000])
            .configureLogging(signalR.LogLevel.Information)
            .build();

        callConnection.on("IncomingCall", onIncomingCall);
        callConnection.on("CallAnswered", onCallAnswered);
        callConnection.on("IceCandidateReceived", onIceCandidateReceived);
        callConnection.on("CallEnded", onCallEnded);
        callConnection.on("CallRejected", onCallRejected);
        callConnection.on("CallError", onCallError);
        callConnection.on("PeerVideoToggled", onPeerVideoToggled);
        callConnection.on("PeerAudioToggled", onPeerAudioToggled);
    }

    try {
        await callConnection.start();
        console.log("CallHub connected");
    } catch (err) {
        console.error("CallHub error (init):", err);
        throw err; // propagate so caller can handle (prevents later invoke() on null)
    }
}

setTimeout(() => {
    // fire-and-forget, but errors will be logged
    initCallConnection().catch(e => console.warn("initCallConnection early error:", e));
}, 500);

function createPeerConnection(targetUserId) {
    try {
        peerConnection = new RTCPeerConnection(iceServers);

        peerConnection.onicecandidate = (event) => {
            try {
                if (event.candidate && callConnection && callConnection.state === signalR.HubConnectionState.Connected) {
                    callConnection.invoke("SendIceCandidate", targetUserId, event.candidate).catch(console.error);
                }
            } catch (e) { console.error("onicecandidate error:", e); }
        };

        peerConnection.ontrack = (event) => {
            try {
                console.log("Remote stream received", event);
                const [stream] = event.streams;
                remoteStream = stream;

                const { remoteVideo, remoteAudio } = getCallElements();

                if (stream.getVideoTracks && stream.getVideoTracks().length > 0) {
                    // video + audio
                    if (remoteAudio) { remoteAudio.srcObject = null; }
                    if (remoteVideo) {
                        if (remoteVideo.srcObject !== stream) remoteVideo.srcObject = stream;
                        remoteVideo.muted = false;
                        remoteVideo.play().catch(e => console.log("Autoplay remoteVideo:", e));
                        const onPlaying = () => { setOverlayVisible(false); updateCallStatus("Đang trong cuộc gọi"); remoteVideo.removeEventListener('playing', onPlaying); };
                        remoteVideo.addEventListener('playing', onPlaying);
                        if (remoteVideo.readyState >= HTMLMediaElement.HAVE_ENOUGH_DATA) { setTimeout(() => setOverlayVisible(false), 150); updateCallStatus("Đang trong cuộc gọi"); }
                    }
                } else {
                    // audio-only
                    if (remoteVideo) remoteVideo.srcObject = null;
                    if (remoteAudio) {
                        if (remoteAudio.srcObject !== stream) remoteAudio.srcObject = stream;
                        remoteAudio.muted = false;
                        remoteAudio.play().then(() => {
                            setOverlayVisible(false);
                            updateCallStatus("Đang trong cuộc gọi");
                        }).catch(e => {
                            console.log("Autoplay remoteAudio blocked:", e);
                            // UI still usable — hide overlay after small delay
                            setTimeout(() => { setOverlayVisible(false); updateCallStatus("Đang trong cuộc gọi"); }, 300);
                        });
                    }
                }
            } catch (ex) {
                console.error("Error in ontrack handler:", ex);
                setOverlayVisible(false);
            }
        };

        peerConnection.onconnectionstatechange = () => {
            console.log("Peer connection state:", peerConnection.connectionState);
            if (peerConnection.connectionState === "connected") {
                setOverlayVisible(false);
                updateCallStatus("Đang trong cuộc gọi");
                startCallDuration();
            }
            if (peerConnection.connectionState === "failed") {
                showNotification("Kết nối thất bại", "error");
                cleanupCall(true);
            }
        };

        peerConnection.oniceconnectionstatechange = () => {
            try { if (peerConnection.iceConnectionState === "failed") peerConnection.restartIce(); } catch(e) { console.warn(e); }
        };

        return peerConnection;
    } catch (err) {
        console.error("Err create peer:", err);
        throw err;
    }
}

async function getLocalMedia(isVideo) {
    try {
        const constraints = { audio: { echoCancellation: true }, video: isVideo ? { width: { ideal: 1280 }, height: { ideal: 720 } } : false };
        localStream = await navigator.mediaDevices.getUserMedia(constraints);
        const { localVideo } = getCallElements();
        if (localVideo) {
            localVideo.srcObject = localStream;
            localVideo.onloadedmetadata = () => localVideo.play().catch(console.warn);
        }
        isAudioEnabled = true; isVideoEnabled = !!isVideo;
        return localStream;
    } catch (err) {
        console.error("getLocalMedia error:", err);
        alert("Lỗi: Không thể truy cập Camera/Mic hoặc quyền bị từ chối.");
        throw err;
    }
}

async function startCall(targetUserId, isVideo) {
    try {
        // ensure signalling connected
        await initCallConnection();
    } catch (e) {
        alert("Không thể kết nối tới server cuộc gọi. Vui lòng thử lại.");
        return;
    }

    const elements = getCallElements();
    if (!elements.callContainer) return;
    currentCallTargetUserId = targetUserId?.toString() ?? null;
    currentCallIsVideo = !!isVideo;

    elements.callContainer.classList.remove("d-none");
    setOverlayVisible(true);
    updateCallStatus("Đang gọi...");
    if (!isVideo && elements.localVideo) elements.localVideo.style.display = 'none';

    try {
        await getLocalMedia(isVideo);

        createPeerConnection(currentCallTargetUserId);

        // attach tracks defensively (log but don't throw)
        if (localStream && peerConnection) {
            localStream.getTracks().forEach(t => {
                try { peerConnection.addTrack(t, localStream); }
                catch (err) { console.warn("addTrack failed:", err); }
            });
        }

        const offer = await peerConnection.createOffer({ offerToReceiveAudio: true, offerToReceiveVideo: isVideo });
        await peerConnection.setLocalDescription(offer);

        if (!callConnection || callConnection.state !== signalR.HubConnectionState.Connected) {
            await initCallConnection();
        }

        try {
            await callConnection.invoke("CallUser", currentCallTargetUserId, offer, isVideo);
        } catch (invokeErr) {
            console.error("CallUser invoke failed:", invokeErr);
            showNotification("Lỗi khi gọi. Vui lòng thử lại.");
            cleanupCall(false);
        }
    } catch (err) {
        console.error("startCall error:", err);
        cleanupCall(false);
    }
}

async function onIncomingCall(payload) {
    const { fromUserId, offer, isVideo } = payload ?? {};
    if (currentCallTargetUserId) { if (callConnection) await callConnection.invoke("RejectCall", fromUserId); return; }

    playRingtone();
    if (confirm(`Cuộc gọi ${isVideo ? 'video' : 'thoại'} đến. Nhận?`)) {
        stopRingtone();
        await handleAcceptCall(fromUserId, offer, isVideo);
    } else {
        stopRingtone();
        if (callConnection) await callConnection.invoke("RejectCall", fromUserId);
    }
}

async function handleAcceptCall(fromUserId, offer, isVideo) {
    try {
        await initCallConnection();
    } catch (e) {
        alert("Không thể kết nối tới server cuộc gọi.");
        return;
    }

    const elements = getCallElements();
    currentCallTargetUserId = fromUserId?.toString() ?? null;
    currentCallIsVideo = !!isVideo;

    elements.callContainer.classList.remove("d-none");
    setOverlayVisible(true);
    updateCallStatus("Đang kết nối...");
    if (!isVideo && elements.localVideo) elements.localVideo.style.display = 'none';

    try {
        await getLocalMedia(isVideo);

        createPeerConnection(currentCallTargetUserId);

        if (localStream && peerConnection) {
            localStream.getTracks().forEach(t => {
                try { peerConnection.addTrack(t, localStream); } catch(e) { console.warn("addTrack failed:", e); }
            });
        }

        await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
        const answer = await peerConnection.createAnswer();
        await peerConnection.setLocalDescription(answer);

        if (!callConnection || callConnection.state !== signalR.HubConnectionState.Connected) await initCallConnection();
        await callConnection.invoke("AnswerCall", currentCallTargetUserId, answer);
    } catch (err) {
        console.error("handleAcceptCall error:", err);
        cleanupCall(false);
    }
}

async function onCallAnswered(payload) {
    try {
        if (!peerConnection || !payload) return;
        await peerConnection.setRemoteDescription(new RTCSessionDescription(payload.answer));
    } catch (e) {
        console.error("onCallAnswered error:", e);
    }
}

async function onIceCandidateReceived(payload) {
    try {
        if (peerConnection && payload && payload.candidate) {
            await peerConnection.addIceCandidate(new RTCIceCandidate(payload.candidate));
        }
    } catch (e) {
        console.error("onIceCandidateReceived error:", e);
    }
}

function onCallEnded() { showNotification("Cuộc gọi kết thúc"); cleanupCall(false); }
function onCallRejected() { showNotification("Người dùng bận/từ chối"); cleanupCall(false); }
function onCallError(p) { showNotification(p?.message ?? "Lỗi cuộc gọi"); cleanupCall(false); }
function onPeerVideoToggled(p) { console.log("Peer video:", p?.enabled); }
function onPeerAudioToggled(p) { console.log("Peer audio:", p?.enabled); }

async function toggleAudio() {
    if (!localStream) return;
    const t = localStream.getAudioTracks()[0];
    if (t) { isAudioEnabled = !isAudioEnabled; t.enabled = isAudioEnabled; if (callConnection) await callConnection.invoke("ToggleAudio", currentCallTargetUserId, isAudioEnabled); }
}

async function toggleVideo() {
    if (!localStream) return;
    const t = localStream.getVideoTracks()[0];
    if (t) { isVideoEnabled = !isVideoEnabled; t.enabled = isVideoEnabled; if (callConnection) await callConnection.invoke("ToggleVideo", currentCallTargetUserId, isVideoEnabled); }
}

function updateCallStatus(text) { const el = document.getElementById("callStatusText"); if (el) el.innerText = text; }

function startCallDuration() {
    if (callDurationInterval) clearInterval(callDurationInterval);
    callStartTime = Date.now();
    callDurationInterval = setInterval(() => {
        const diff = Date.now() - callStartTime;
        const m = Math.floor(diff / 60000).toString().padStart(2, '0');
        const s = Math.floor((diff % 60000) / 1000).toString().padStart(2, '0');
        updateCallStatus(`${m}:${s}`);
    }, 1000);
}

async function cleanupCall(notify) {
    if (callDurationInterval) clearInterval(callDurationInterval);
    try { if (peerConnection) peerConnection.close(); } catch(e) { console.warn(e); }
    if (localStream) localStream.getTracks().forEach(t => t.stop());
    if (remoteStream) remoteStream.getTracks().forEach(t => t.stop());

    const el = getCallElements();
    if (el.callContainer) el.callContainer.classList.add('d-none');
    if (el.localVideo) el.localVideo.srcObject = null;
    if (el.remoteVideo) el.remoteVideo.srcObject = null;
    if (el.remoteAudio) el.remoteAudio.srcObject = null;

    setOverlayVisible(true);

    if (notify && callConnection && currentCallTargetUserId) await callConnection.invoke("HangUp", currentCallTargetUserId).catch(console.warn);
    currentCallTargetUserId = null;
}

function showNotification(m) { console.log("NOTIF:", m); } // Thay bằng toast nếu muốn

function wireCallButtons() {
    document.addEventListener("click", async (e) => {
        const btn = e.target.closest("[data-call]");
        if (!btn) return;
        e.preventDefault();
        const action = btn.getAttribute("data-call");
        const target = btn.getAttribute("data-target-user-id");
        try {
            if (action === "voice") await startCall(target, false);
            if (action === "video") await startCall(target, true);
            if (action === "hangup") await cleanupCall(true);
            if (action === "toggle-audio") await toggleAudio();
            if (action === "toggle-video") await toggleVideo();
        } catch (err) {
            console.error("Action handler error:", err);
            showNotification("Lỗi nội bộ khi thực hiện thao tác.");
        }
    });
}

document.addEventListener("DOMContentLoaded", wireCallButtons);
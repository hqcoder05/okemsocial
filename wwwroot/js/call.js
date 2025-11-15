// wwwroot/js/call.js
// Chức năng gọi thoại/video giống Messenger/Instagram

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
    if (callConnection && callConnection.state === signalR.HubConnectionState.Connected) {
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
        showNotification("Không thể kết nối đến server cuộc gọi", "error");
        throw err;
    }
}

// Tạo RTCPeerConnection
function createPeerConnection(targetUserId) {
    try {
        peerConnection = new RTCPeerConnection(iceServers);

        // Handle ICE candidates
        peerConnection.onicecandidate = (event) => {
            if (event.candidate) {
                console.log("Sending ICE candidate");
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
            if (remoteVideo) {
                remoteVideo.srcObject = remoteStream;
                remoteVideo.onloadedmetadata = () => {
                    remoteVideo.play().catch(e => console.warn("Remote video play error:", e));
                };
            }

            updateCallStatus("Đã kết nối");
            startCallDuration();
        };

        // Handle connection state changes
        peerConnection.onconnectionstatechange = () => {
            console.log("Connection state:", peerConnection.connectionState);

            switch (peerConnection.connectionState) {
                case "connected":
                    updateCallStatus("Đã kết nối");
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
            console.log("ICE connection state:", peerConnection.iceConnectionState);

            if (peerConnection.iceConnectionState === "failed") {
                // Restart ICE nếu thất bại
                peerConnection.restartIce();
            }
        };

        return peerConnection;
    } catch (err) {
        console.error("Error creating peer connection:", err);
        showNotification("Không thể tạo kết nối", "error");
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

        // Reset trạng thái audio/video
        isAudioEnabled = true;
        isVideoEnabled = isVideo;

        return localStream;
    } catch (err) {
        console.error("Error getting media:", err);

        let errorMessage = "Không thể truy cập camera/microphone";
        if (err.name === "NotAllowedError" || err.name === "PermissionDeniedError") {
            errorMessage = "Vui lòng cấp quyền truy cập camera/microphone";
        } else if (err.name === "NotFoundError" || err.name === "DevicesNotFoundError") {
            errorMessage = "Không tìm thấy camera/microphone";
        }

        showNotification(errorMessage, "error");
        throw err;
    }
}

// Bắt đầu cuộc gọi (caller)
async function startCall(targetUserId, isVideo) {
    if (!targetUserId) {
        showNotification("Không xác định được người nhận", "error");
        return;
    }

    try {
        await initCallConnection();
    } catch (err) {
        return;
    }

    const elements = getCallElements();
    if (!elements.callContainer) {
        console.warn("Không tìm thấy callContainer trong DOM");
        return;
    }

    currentCallTargetUserId = targetUserId.toString();
    currentCallIsVideo = isVideo;

    // Hiện UI cuộc gọi ngay
    elements.callContainer.classList.remove("d-none");
    updateCallStatus("Đang gọi...");

    // Ẩn video nếu là cuộc gọi thoại
    if (!isVideo && elements.localVideo && elements.remoteVideo) {
        elements.localVideo.style.display = "none";
        elements.remoteVideo.style.display = "none";
    }

    try {
        // Lấy media
        await getLocalMedia(isVideo);

        // Tạo peer connection
        createPeerConnection(currentCallTargetUserId);

        // Thêm tracks vào peer connection
        localStream.getTracks().forEach(track => {
            peerConnection.addTrack(track, localStream);
        });

        // Tạo offer
        const offer = await peerConnection.createOffer({
            offerToReceiveAudio: true,
            offerToReceiveVideo: isVideo
        });

        await peerConnection.setLocalDescription(offer);

        // Gửi offer qua SignalR
        await callConnection.invoke("CallUser", currentCallTargetUserId, offer, isVideo);

        console.log("Call initiated successfully");
    } catch (err) {
        console.error("Error starting call:", err);
        cleanupCall(false);
    }
}

// Nhận cuộc gọi đến (callee)
async function onIncomingCall(payload) {
    console.log("IncomingCall:", payload);

    const fromUserId = payload.fromUserId;
    const offer = payload.offer;
    const isVideo = payload.isVideo;

    // Nếu đang trong cuộc gọi khác, từ chối
    if (currentCallTargetUserId) {
        await callConnection.invoke("RejectCall", fromUserId);
        return;
    }

    // Hiển thị popup xác nhận
    const callType = isVideo ? "video" : "thoại";
    const accept = confirm(`Có cuộc gọi ${callType} đến. Bạn có muốn nhận không?`);

    if (!accept) {
        await callConnection.invoke("RejectCall", fromUserId);
        return;
    }

    try {
        await initCallConnection();
    } catch (err) {
        return;
    }

    const elements = getCallElements();
    if (!elements.callContainer) return;

    currentCallTargetUserId = fromUserId.toString();
    currentCallIsVideo = isVideo;

    // Hiện UI
    elements.callContainer.classList.remove("d-none");
    updateCallStatus("Đang kết nối...");

    // Ẩn video nếu là cuộc gọi thoại
    if (!isVideo && elements.localVideo && elements.remoteVideo) {
        elements.localVideo.style.display = "none";
        elements.remoteVideo.style.display = "none";
    }

    try {
        // Lấy media
        await getLocalMedia(isVideo);

        // Tạo peer connection
        createPeerConnection(currentCallTargetUserId);

        // Thêm tracks
        localStream.getTracks().forEach(track => {
            peerConnection.addTrack(track, localStream);
        });

        // Set remote description
        await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));

        // Tạo answer
        const answer = await peerConnection.createAnswer();
        await peerConnection.setLocalDescription(answer);

        // Gửi answer
        await callConnection.invoke("AnswerCall", currentCallTargetUserId, answer);

        console.log("Call answered successfully");
    } catch (err) {
        console.error("Error answering call:", err);
        cleanupCall(false);
    }
}

// Caller nhận answer từ callee
async function onCallAnswered(payload) {
    console.log("CallAnswered:", payload);

    if (!peerConnection) {
        console.warn("No peer connection found");
        return;
    }

    try {
        const answer = new RTCSessionDescription(payload.answer);
        await peerConnection.setRemoteDescription(answer);

        updateCallStatus("Đang kết nối...");
        console.log("Answer set successfully");
    } catch (err) {
        console.error("Error setting remote description:", err);
        cleanupCall(true);
    }
}

// Nhận ICE candidate
async function onIceCandidateReceived(payload) {
    if (!peerConnection) {
        console.warn("Received ICE candidate but no peer connection");
        return;
    }

    try {
        const candidate = new RTCIceCandidate(payload.candidate);
        await peerConnection.addIceCandidate(candidate);
        console.log("ICE candidate added successfully");
    } catch (err) {
        console.error("Error adding ICE candidate:", err);
    }
}

// Cuộc gọi bị kết thúc từ phía bên kia
function onCallEnded(payload) {
    console.log("CallEnded:", payload);

    const reason = payload.reason || "hangup";
    let message = "Cuộc gọi đã kết thúc";

    if (reason === "disconnected") {
        message = "Người dùng đã ngắt kết nối";
    }

    showNotification(message, "info");
    cleanupCall(false);
}

// Cuộc gọi bị từ chối
function onCallRejected(payload) {
    console.log("CallRejected:", payload);
    showNotification("Cuộc gọi bị từ chối", "warning");
    cleanupCall(false);
}

// Lỗi từ server
function onCallError(payload) {
    console.log("CallError:", payload);

    let message = payload.message || "Có lỗi xảy ra";
    if (message === "User is busy") {
        message = "Người dùng đang bận";
    }

    showNotification(message, "error");
    cleanupCall(false);
}

// Peer toggle video
function onPeerVideoToggled(payload) {
    console.log("Peer video toggled:", payload.enabled);
    // Có thể hiển thị thông báo cho user biết
}

// Peer toggle audio
function onPeerAudioToggled(payload) {
    console.log("Peer audio toggled:", payload.enabled);
    // Có thể hiển thị thông báo cho user biết
}

// Toggle microphone
async function toggleAudio() {
    if (!localStream) return;

    const audioTrack = localStream.getAudioTracks()[0];
    if (audioTrack) {
        isAudioEnabled = !isAudioEnabled;
        audioTrack.enabled = isAudioEnabled;

        // Thông báo cho peer
        if (callConnection && currentCallTargetUserId) {
            await callConnection.invoke("ToggleAudio", currentCallTargetUserId, isAudioEnabled);
        }

        showNotification(
            isAudioEnabled ? "Đã bật microphone" : "Đã tắt microphone",
            "info"
        );
    }
}

// Toggle camera
async function toggleVideo() {
    if (!localStream || !currentCallIsVideo) return;

    const videoTrack = localStream.getVideoTracks()[0];
    if (videoTrack) {
        isVideoEnabled = !isVideoEnabled;
        videoTrack.enabled = isVideoEnabled;

        // Thông báo cho peer
        if (callConnection && currentCallTargetUserId) {
            await callConnection.invoke("ToggleVideo", currentCallTargetUserId, isVideoEnabled);
        }

        showNotification(
            isVideoEnabled ? "Đã bật camera" : "Đã tắt camera",
            "info"
        );
    }
}

// Cập nhật trạng thái cuộc gọi
function updateCallStatus(status) {
    const { callStatusText } = getCallElements();
    if (callStatusText) {
        callStatusText.textContent = status;
    }
}

// Bắt đầu đếm thời gian cuộc gọi
function startCallDuration() {
    if (callDurationInterval) {
        clearInterval(callDurationInterval);
    }

    callStartTime = Date.now();

    callDurationInterval = setInterval(() => {
        const elapsed = Date.now() - callStartTime;
        const minutes = Math.floor(elapsed / 60000);
        const seconds = Math.floor((elapsed % 60000) / 1000);

        updateCallStatus(`${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`);
    }, 1000);
}

// Dọn dẹp khi kết thúc cuộc gọi
async function cleanupCall(needNotifyOtherSide) {
    console.log("Cleaning up call...");

    const elements = getCallElements();

    // Dừng interval
    if (callDurationInterval) {
        clearInterval(callDurationInterval);
        callDurationInterval = null;
    }
    callStartTime = null;

    // Đóng peer connection
    if (peerConnection) {
        peerConnection.close();
        peerConnection = null;
    }

    // Dừng local stream
    if (localStream) {
        localStream.getTracks().forEach(track => {
            track.stop();
        });
        localStream = null;
    }

    // Dừng remote stream
    if (remoteStream) {
        remoteStream.getTracks().forEach(track => {
            track.stop();
        });
        remoteStream = null;
    }

    // Clear video elements
    if (elements.localVideo) {
        elements.localVideo.srcObject = null;
        elements.localVideo.style.display = "";
    }
    if (elements.remoteVideo) {
        elements.remoteVideo.srcObject = null;
        elements.remoteVideo.style.display = "";
    }

    // Ẩn UI
    if (elements.callContainer) {
        elements.callContainer.classList.add("d-none");
    }

    // Thông báo cho bên kia nếu cần
    if (needNotifyOtherSide && callConnection && currentCallTargetUserId) {
        try {
            await callConnection.invoke("HangUp", currentCallTargetUserId);
        } catch (err) {
            console.warn("HangUp error:", err);
        }
    }

    // Reset state
    currentCallTargetUserId = null;
    currentCallIsVideo = false;
    isAudioEnabled = true;
    isVideoEnabled = true;
}

// Kết thúc cuộc gọi
async function hangUp() {
    await cleanupCall(true);
}

// Hiển thị thông báo
function showNotification(message, type = "info") {
    // Có thể dùng toast notification library hoặc alert đơn giản
    console.log(`[${type.toUpperCase()}] ${message}`);

    // Tạm thời dùng alert cho demo
    if (type === "error") {
        alert(message);
    }
}

// Gắn event listeners cho các nút
function wireCallButtons() {
    document.addEventListener("click", async (e) => {
        // Nút gọi thoại
        const voiceBtn = e.target.closest("[data-call='voice']");
        if (voiceBtn) {
            e.preventDefault();
            const targetUserId = voiceBtn.getAttribute("data-target-user-id");
            if (targetUserId) {
                await startCall(targetUserId, false);
            }
            return;
        }

        // Nút gọi video
        const videoBtn = e.target.closest("[data-call='video']");
        if (videoBtn) {
            e.preventDefault();
            const targetUserId = videoBtn.getAttribute("data-target-user-id");
            if (targetUserId) {
                await startCall(targetUserId, true);
            }
            return;
        }

        // Nút kết thúc
        const hangBtn = e.target.closest("[data-call='hangup']");
        if (hangBtn) {
            e.preventDefault();
            await hangUp();
            return;
        }

        // Nút tắt/bật mic
        const muteBtn = e.target.closest("[data-call='toggle-audio']");
        if (muteBtn) {
            e.preventDefault();
            await toggleAudio();
            return;
        }

        // Nút tắt/bật camera
        const camBtn = e.target.closest("[data-call='toggle-video']");
        if (camBtn) {
            e.preventDefault();
            await toggleVideo();
            return;
        }
    });
}

// Khởi tạo khi DOM ready
document.addEventListener("DOMContentLoaded", () => {
    console.log("Initializing call.js...");
    wireCallButtons();
});
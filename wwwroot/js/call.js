// wwwroot/js/call.js - Okem Realtime Voice & Video Calling (WebRTC + SignalR)

if (!window.isCallJsLoaded) {
    window.isCallJsLoaded = true;

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
    let ringtoneAudio = null;

    const iceServers = {
        iceServers: [
            { urls: "stun:stun.l.google.com:19302" },
            { urls: "stun:stun1.l.google.com:19302" },
            { urls: "stun:stun2.l.google.com:19302" },
        ]
    };

    function getCallElements() {
        return {
            callContainer: document.getElementById("callOverlay") || document.getElementById("callContainer"),
            localVideo: document.getElementById("localVideo"),
            remoteVideo: document.getElementById("remoteVideo"),
            remoteAudio: document.getElementById("remoteAudio"),
            callStatusText: document.getElementById("callStatus") || document.getElementById("callStatusText"),
            callTitle: document.getElementById("callPeerName") || document.getElementById("callTitle"),
            callLoading: document.getElementById("callLoading"),
            callLoadingText: document.getElementById("callLoadingText"),
            callAvatar: document.getElementById("callAvatar"),
            callHeading: document.getElementById("callHeading"),
        };
    }

    function playRingtone() {
        try {
            if (!ringtoneAudio) {
                ringtoneAudio = new Audio('data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBTGH0fPTgjMGHm7A7+OZSA0PVqzn77BdGAg+ltryxnMpBSl+zPLaizsIGGS57OihUBALTqXh8bllHAU2jdXz0H8wBSF1xe/glEILElyx6+ytWBUIQ5zd8sFuJAUuhM/z24Y7CBlpvO3nn1EMDVGn6/C2YxwGN4/X88p3KwUld8rx3Y9AChRfsunrp1QUCkef4PK+bCAFMIjR89OCMgYfb8Tv45lIDQ9Xq+fwsF0YCD6W2vLGcykFKX7M8tqLOwgYZLns6KFQEAtOpOHxuWUcBTaN1fPQfzAFIXXF7+CUQgsRXLHr7K1YFQhDnN3ywW4kBS6Ez/PbhjsIGWm87eefUQwNUajr8LZjHAY3j9fyyncqBSV3yvHdj0AKFFyx6eqmVBQKR5/g8r1rIAUxiNHz04IyBh9vxO/jmUgND1er5/CwXRgIPpba8sZzKQUpfszyTOSFZU2wYAACA...');
                ringtoneAudio.loop = true;
            }
            ringtoneAudio.play().catch(e => console.log('Cannot play ringtone:', e));
        } catch (e) {}
    }

    function stopRingtone() {
        if (ringtoneAudio) {
            ringtoneAudio.pause();
            ringtoneAudio.currentTime = 0;
        }
    }

    async function initCallConnection() {
        if (callConnection && (callConnection.state === signalR.HubConnectionState.Connected || callConnection.state === signalR.HubConnectionState.Connecting)) {
            return;
        }

        callConnection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/call")
            .withAutomaticReconnect([0, 2000, 5000, 10000])
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

        callConnection.on("UserOnline", (userId) => {
            const dot = document.getElementById(`online-dot-${userId}`);
            if (dot) {
                dot.classList.remove("hidden");
                const text = dot.closest('button')?.querySelector('.font-medium');
                if (text) text.innerText = "Đang hoạt động";
            }
        });

        callConnection.on("UserOffline", (userId) => {
            const dot = document.getElementById(`online-dot-${userId}`);
            if (dot) {
                dot.classList.add("hidden");
                const text = dot.closest('button')?.querySelector('.font-medium');
                if (text) text.innerText = "Ngoại tuyến";
            }
        });

        try {
            await callConnection.start();
            console.log("CallHub connected successfully");
            
            callConnection.invoke("GetOnlineUsers").then((users) => {
                users.forEach(userId => {
                    const dot = document.getElementById(`online-dot-${userId}`);
                    if (dot) {
                        dot.classList.remove("hidden");
                        const text = dot.closest('button')?.querySelector('.font-medium');
                        if (text) text.innerText = "Đang hoạt động";
                    }
                });
            }).catch(console.error);
        } catch (err) {
            console.error("CallHub connection error:", err);
        }
    }

    function createPeerConnection(targetUserId) {
        if (peerConnection) {
            peerConnection.close();
            peerConnection = null;
        }

        peerConnection = new RTCPeerConnection(iceServers);

        peerConnection.onicecandidate = (event) => {
            if (event.candidate && callConnection) {
                callConnection.invoke("SendIceCandidate", targetUserId, event.candidate).catch(console.error);
            }
        };

        peerConnection.ontrack = (event) => {
            const elements = getCallElements();
            remoteStream = event.streams[0] || new MediaStream([event.track]);

            if (elements.remoteVideo && currentCallIsVideo) {
                elements.remoteVideo.srcObject = remoteStream;
                elements.remoteVideo.play().catch(e => console.warn("Remote video play error:", e));
            }
            if (elements.remoteAudio) {
                elements.remoteAudio.srcObject = remoteStream;
                elements.remoteAudio.play().catch(e => console.warn("Remote audio play error:", e));
            }

            // Hide loading or update text
            if (elements.callLoading) {
                if (currentCallIsVideo) {
                    elements.callLoading.style.display = 'none';
                } else {
                    if (elements.callLoadingText) {
                        elements.callLoadingText.innerHTML = '<i class="fa-solid fa-phone text-emerald-400"></i> Đang trong cuộc gọi thoại...';
                    }
                }
            }
        };

        peerConnection.onconnectionstatechange = () => {
            console.log("Peer connection state:", peerConnection.connectionState);
            const elements = getCallElements();
            if (peerConnection.connectionState === "connected") {
                if (elements.callStatusText) elements.callStatusText.innerText = "Đã kết nối";
                
                if (elements.callLoading) {
                    if (currentCallIsVideo) {
                        elements.callLoading.style.display = 'none';
                    } else {
                        if (elements.callLoadingText) {
                            elements.callLoadingText.innerHTML = '<i class="fa-solid fa-phone text-emerald-400"></i> Đang trong cuộc gọi thoại...';
                        }
                    }
                }

                startCallTimer();
            } else if (peerConnection.connectionState === "disconnected" || peerConnection.connectionState === "failed") {
                cleanupCall(false);
            }
        };

        return peerConnection;
    }

    async function getLocalMedia(isVideo) {
        try {
            const constraints = {
                audio: { echoCancellation: true, noiseSuppression: true, autoGainControl: true },
                video: isVideo ? { width: { ideal: 1280 }, height: { ideal: 720 }, facingMode: "user" } : false
            };

            localStream = await navigator.mediaDevices.getUserMedia(constraints);

            const { localVideo } = getCallElements();
            if (localVideo) {
                localVideo.srcObject = localStream;
                localVideo.style.display = isVideo ? "block" : "none";
                localVideo.play().catch(console.warn);
            }

            isAudioEnabled = true;
            isVideoEnabled = isVideo;
            return localStream;
        } catch (err) {
            console.error("Media access error:", err);
            alert("Không thể truy cập camera/micro của thiết bị.");
            throw err;
        }
    }

    async function startCall(targetUserId, isVideo, targetName) {
        if (!targetUserId) return;

        const callType = isVideo ? "video" : "thoại";
        const peerName = targetName || `Người dùng #${targetUserId}`;
        if (!confirm(`Bạn có chắc chắn muốn bắt đầu cuộc gọi ${callType} với ${peerName}?`)) {
            return;
        }

        try { await initCallConnection(); } catch (err) { return; }

        currentCallTargetUserId = targetUserId.toString();
        currentCallIsVideo = isVideo;

        const elements = getCallElements();
        if (elements.callContainer) {
            elements.callContainer.classList.remove("hidden");
            elements.callContainer.style.display = "flex";
        }
        if (elements.callTitle) elements.callTitle.innerText = targetName || `Người dùng #${targetUserId}`;
        if (elements.callHeading) elements.callHeading.innerText = targetName || `Người dùng #${targetUserId}`;
        if (elements.callAvatar) elements.callAvatar.innerText = (targetName || 'U')[0].toUpperCase();
        if (elements.callStatusText) elements.callStatusText.innerText = isVideo ? "Đang gọi Video..." : "Đang gọi Thoại...";
        if (elements.callLoading) elements.callLoading.style.display = "flex";
        if (elements.callLoadingText) elements.callLoadingText.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin text-blue-400"></i> Đang kết nối...';

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
            console.error("Start call error:", err);
            cleanupCall(false);
        }
    }

    async function onIncomingCall(payload) {
        const { fromUserId, offer, isVideo } = payload;
        if (currentCallTargetUserId) {
            await callConnection.invoke("RejectCall", fromUserId);
            return;
        }

        playRingtone();

        const notifId = 'call-notif-' + Date.now();
        const notif = document.createElement('div');
        notif.id = notifId;
        notif.className = "fixed top-6 right-6 z-[100] rounded-3xl border border-slate-200 bg-white/95 p-6 shadow-2xl backdrop-blur-xl animate-scaleIn dark:border-white/10 dark:bg-slate-900";
        notif.innerHTML = `
            <div class="flex items-center gap-4 mb-4">
                <div class="flex h-12 w-12 items-center justify-center rounded-2xl bg-blue-600 text-white font-bold text-lg animate-pulse">
                    <i class="fa-solid ${isVideo ? 'fa-video' : 'fa-phone'}"></i>
                </div>
                <div>
                    <h4 class="text-sm font-extrabold text-slate-900 dark:text-white">Cuộc gọi đến (${isVideo ? 'Video' : 'Thoại'})</h4>
                    <p class="text-xs text-slate-500 dark:text-slate-400">Từ người dùng #${fromUserId}</p>
                </div>
            </div>
            <div class="flex gap-3">
                <button id="btn-accept-${notifId}" class="flex-1 rounded-full bg-emerald-600 py-2.5 text-xs font-bold text-white hover:bg-emerald-700 active:scale-95 transition-all">
                    <i class="fa-solid fa-phone me-1.5"></i> Trả lời
                </button>
                <button id="btn-reject-${notifId}" class="flex-1 rounded-full bg-red-600 py-2.5 text-xs font-bold text-white hover:bg-red-700 active:scale-95 transition-all">
                    <i class="fa-solid fa-phone-slash me-1.5"></i> Từ chối
                </button>
            </div>
        `;
        document.body.appendChild(notif);

        document.getElementById(`btn-accept-${notifId}`).onclick = async () => {
            stopRingtone();
            notif.remove();
            await answerCall(fromUserId, offer, isVideo);
        };

        document.getElementById(`btn-reject-${notifId}`).onclick = async () => {
            stopRingtone();
            notif.remove();
            await callConnection.invoke("RejectCall", fromUserId);
        };
    }

    async function answerCall(fromUserId, offer, isVideo) {
        currentCallTargetUserId = fromUserId.toString();
        currentCallIsVideo = isVideo;

        const elements = getCallElements();
        if (elements.callContainer) {
            elements.callContainer.classList.remove("hidden");
            elements.callContainer.style.display = "flex";
        }
        if (elements.callTitle) elements.callTitle.innerText = `Người dùng #${fromUserId}`;
        if (elements.callStatusText) elements.callStatusText.innerText = "Đang kết nối...";
        if (elements.callLoading) elements.callLoading.style.display = "flex";
        if (elements.callLoadingText) elements.callLoadingText.innerHTML = '<i class="fa-solid fa-circle-notch fa-spin text-blue-400"></i> Đang kết nối...';

        try {
            await getLocalMedia(isVideo);
            createPeerConnection(currentCallTargetUserId);
            localStream.getTracks().forEach(track => peerConnection.addTrack(track, localStream));

            await peerConnection.setRemoteDescription(new RTCSessionDescription(offer));
            const answer = await peerConnection.createAnswer();
            await peerConnection.setLocalDescription(answer);

            await callConnection.invoke("AnswerCall", currentCallTargetUserId, answer);
        } catch (err) {
            console.error("Answer call error:", err);
            cleanupCall(false);
        }
    }

    async function onCallAnswered(payload) {
        const { answer } = payload;
        try {
            if (peerConnection && answer) {
                await peerConnection.setRemoteDescription(new RTCSessionDescription(answer));
            }
        } catch (err) {
            console.error("CallAnswered error:", err);
        }
    }

    async function onIceCandidateReceived(payload) {
        const { candidate } = payload;
        try {
            if (peerConnection && candidate) {
                await peerConnection.addIceCandidate(new RTCIceCandidate(candidate));
            }
        } catch (err) {
            console.error("IceCandidate error:", err);
        }
    }

    function onCallEnded() {
        stopRingtone();
        cleanupCall(false);
    }

    function onCallRejected() {
        stopRingtone();
        alert("Người nhận đã từ chối cuộc gọi.");
        cleanupCall(false);
    }

    function onCallError(payload) {
        alert(payload?.message || "Có lỗi xảy ra trong cuộc gọi.");
        cleanupCall(false);
    }

    function onPeerVideoToggled(payload) {
        console.log("Peer video toggled:", payload?.enabled);
    }

    function onPeerAudioToggled(payload) {
        console.log("Peer audio toggled:", payload?.enabled);
    }

    function startCallTimer() {
        if (callDurationInterval) clearInterval(callDurationInterval);
        callStartTime = Date.now();
        callDurationInterval = setInterval(() => {
            const elapsed = Date.now() - callStartTime;
            const minutes = Math.floor(elapsed / 60000);
            const seconds = Math.floor((elapsed % 60000) / 1000);
            const elements = getCallElements();
            if (elements.callStatusText) {
                elements.callStatusText.innerText = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
            }
        }, 1000);
    }

    async function cleanupCall(needNotify) {
        if (callDurationInterval) { clearInterval(callDurationInterval); callDurationInterval = null; }
        callStartTime = null;

        if (peerConnection) { peerConnection.close(); peerConnection = null; }
        if (localStream) { localStream.getTracks().forEach(t => t.stop()); localStream = null; }
        if (remoteStream) { remoteStream.getTracks().forEach(t => t.stop()); remoteStream = null; }

        const elements = getCallElements();
        if (elements.localVideo) elements.localVideo.srcObject = null;
        if (elements.remoteVideo) elements.remoteVideo.srcObject = null;
        if (elements.remoteAudio) elements.remoteAudio.srcObject = null;
        if (elements.callContainer) {
            elements.callContainer.classList.add("hidden");
            elements.callContainer.style.display = "none";
        }

        if (needNotify && callConnection && currentCallTargetUserId) {
            try { await callConnection.invoke("HangUp", currentCallTargetUserId); } catch (e) {}
        }

        currentCallTargetUserId = null;
        currentCallIsVideo = false;
    }

    // Expose global window helper functions for UI
    window.startVoiceCall = (peerId, name) => startCall(peerId, false, name);
    window.startVideoCall = (peerId, name) => startCall(peerId, true, name);
    window.endCall = () => cleanupCall(true);
    window.minimizeCall = () => {
        const elements = getCallElements();
        if (elements.callContainer) {
            elements.callContainer.classList.toggle("call-minimized");
        }
    };
    window.toggleMic = () => {
        if (localStream) {
            isAudioEnabled = !isAudioEnabled;
            localStream.getAudioTracks().forEach(t => t.enabled = isAudioEnabled);
            const btn = document.getElementById('btnMic');
            if (btn) btn.classList.toggle('bg-red-600', !isAudioEnabled);
            if (callConnection && currentCallTargetUserId) {
                callConnection.invoke("ToggleAudio", currentCallTargetUserId, isAudioEnabled).catch(console.error);
            }
        }
    };
    window.toggleCam = () => {
        if (localStream) {
            isVideoEnabled = !isVideoEnabled;
            localStream.getVideoTracks().forEach(t => t.enabled = isVideoEnabled);
            const btn = document.getElementById('btnCam');
            if (btn) btn.classList.toggle('bg-red-600', !isVideoEnabled);
            if (callConnection && currentCallTargetUserId) {
                callConnection.invoke("ToggleVideo", currentCallTargetUserId, isVideoEnabled).catch(console.error);
            }
        }
    };

    // Auto-init call connection
    setTimeout(initCallConnection, 600);
}
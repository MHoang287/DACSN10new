(() => {
    if (window.__teacherLiveInit) return; window.__teacherLiveInit = true;

    const cfgEl = document.getElementById('live-config');
    if (!cfgEl) { console.error('[Teacher] Missing #live-config'); return; }
    const API_BASE = (cfgEl.dataset.apiBase || '').replace(/\/$/, '');
    const ROOM_ID = cfgEl.dataset.roomId;
    const MY_ID = cfgEl.dataset.myId;
    const TEACHER_PID = cfgEl.dataset.teacherId || MY_ID; // ưu tiên teacherParticipantId nếu có
    const DISPLAY_NAME = cfgEl.dataset.displayName || 'Teacher';
    const WS_ENDPOINT = API_BASE + '/ws';

    // ===== Presence (Teacher heartbeat) + viewerCount =====
    const HEARTBEAT_MS = 15000;
    const VIEWERS_POLL_MS = 10000;
    let __hbTimer = null;
    let __viewerTimer = null;

    async function sendHeartbeat(live) {
        if (!ROOM_ID) return;
        const url = `${API_BASE}/api/rooms/${ROOM_ID}/teacher/heartbeat?live=${live ? 'true' : 'false'}${TEACHER_PID ? `&participantId=${encodeURIComponent(TEACHER_PID)}` : ''}`;
        try { await fetch(url, { method: 'POST', keepalive: !live }); } catch { }
    }
    function startHeartbeat() {
        clearInterval(__hbTimer);
        sendHeartbeat(true);
        __hbTimer = setInterval(() => sendHeartbeat(true), HEARTBEAT_MS);
    }
    function stopHeartbeat() {
        clearInterval(__hbTimer); __hbTimer = null;
        try {
            const url = `${API_BASE}/api/rooms/${ROOM_ID}/teacher/heartbeat?live=false${TEACHER_PID ? `&participantId=${encodeURIComponent(TEACHER_PID)}` : ''}`;
            if (navigator.sendBeacon) navigator.sendBeacon(url);
            else fetch(url, { method: 'POST', keepalive: true }).catch(() => { });
        } catch { }
    }

    async function fetchViewerCount() {
        if (!ROOM_ID) return;
        const url = `${API_BASE}/api/rooms/${ROOM_ID}/viewer-count`;
        try {
            const resp = await fetch(url, { headers: { 'Accept': 'application/json' } });
            if (!resp.ok) throw new Error(`viewer-count ${resp.status}`);
            const data = await resp.json();
            const n = Number(data?.viewerCount ?? 0);
            const el = document.getElementById('viewer-count');
            if (el) el.textContent = isFinite(n) ? String(n) : '0';
        } catch {
            const el = document.getElementById('viewer-count');
            if (el) el.textContent = '--';
        }
    }
    function startViewerPolling() {
        clearInterval(__viewerTimer);
        fetchViewerCount();
        __viewerTimer = setInterval(fetchViewerCount, VIEWERS_POLL_MS);
    }
    function stopViewerPolling() { clearInterval(__viewerTimer); __viewerTimer = null; }

    // Status UI
    function setStatus(txt) {
        const el = document.getElementById('live-status');
        if (el) el.textContent = txt;
    }

    // ===== WebRTC (Teacher publishes) =====
    const defaultIce = [{ urls: 'stun:stun.l.google.com:19302' }];
    const extraIce = Array.isArray(window.LIVE_TURN) ? window.LIVE_TURN : [];
    const rtcConfig = { iceServers: [...defaultIce, ...extraIce] };

    const pcs = new Map();
    const pendingIce = new Map();
    let stompClient = null;

    let previewStream = new MediaStream();
    let sendVideoTrack = null;
    let sendAudioTrack = null;

    let camStream = null;
    let screenStream = null;
    let micStream = null;

    let audioCtx = null, mixDest = null, micSource = null, sysSource = null;

    function logState(pc, label) {
        console.log(`[${label}] connection=${pc.connectionState} ice=${pc.iceConnectionState} signaling=${pc.signalingState}`);
    }
    function stopTrackSafe(t) { try { t.stop(); } catch { } }
    function stopStreamSafe(s) { if (!s) return; s.getTracks().forEach(stopTrackSafe); }

    function setPreviewElement() {
        const v = document.getElementById('localVideo');
        if (v && v.srcObject !== previewStream) v.srcObject = previewStream;
    }
    function updatePreviewTracks() {
        previewStream.getTracks().forEach(t => previewStream.removeTrack(t));
        if (sendVideoTrack) previewStream.addTrack(sendVideoTrack);
        if (sendAudioTrack) previewStream.addTrack(sendAudioTrack);
        setPreviewElement();
        // update status based on having tracks
        if (sendVideoTrack || sendAudioTrack) setStatus('LIVE'); else setStatus('READY');
    }
    async function applyTracksToAllPeers() {
        for (const [sid, pc] of pcs) {
            const vs = pc.getSenders().find(s => s.track && s.track.kind === 'video');
            if (sendVideoTrack) { if (vs) await vs.replaceTrack(sendVideoTrack); else pc.addTrack(sendVideoTrack, previewStream); }
            const as = pc.getSenders().find(s => s.track && s.track.kind === 'audio');
            if (sendAudioTrack) { if (as) await as.replaceTrack(sendAudioTrack); else pc.addTrack(sendAudioTrack, previewStream); }
        }
    }

    async function initCameraDefault() {
        console.log('[Teacher] getUserMedia start');
        camStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });
        sendVideoTrack = camStream.getVideoTracks()[0] || null;
        sendAudioTrack = camStream.getAudioTracks()[0] || null;
        micStream = new MediaStream(camStream.getAudioTracks());
        updatePreviewTracks();
        await applyTracksToAllPeers();
        console.log('[Teacher] getUserMedia OK');
    }

    function resetMixing() { if (audioCtx) { try { audioCtx.close(); } catch { } } audioCtx = null; mixDest = null; micSource = null; sysSource = null; }
    function stopScreenIfAny() { if (screenStream) { screenStream.getTracks().forEach(t => t.onended = null); stopStreamSafe(screenStream); screenStream = null; } }

    async function switchToCamera() {
        if (!camStream) { await initCameraDefault(); return; }
        stopScreenIfAny(); resetMixing();
        sendVideoTrack = camStream.getVideoTracks()[0] || null;
        sendAudioTrack = camStream.getAudioTracks()[0] || null;
        updatePreviewTracks(); await applyTracksToAllPeers();
    }

    async function startScreenShare() {
        try {
            if (!navigator.mediaDevices?.getDisplayMedia) { alert('Trình duyệt không hỗ trợ chia sẻ màn hình'); return; }
            screenStream = await navigator.mediaDevices.getDisplayMedia({ video: true, audio: true });
            const screenVideo = screenStream.getVideoTracks()[0] || null;
            const systemAudio = screenStream.getAudioTracks()[0] || null;
            if (screenVideo) { screenVideo.onended = () => switchToCamera(); }

            if (!micStream || !micStream.getAudioTracks().length) {
                try { micStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false }); } catch { }
            }

            const mic = micStream?.getAudioTracks()[0] || null;
            if (mic && systemAudio) {
                try {
                    if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)();
                    mixDest = audioCtx.createMediaStreamDestination();
                    const micS = new MediaStream([mic]); micSource = audioCtx.createMediaStreamSource(micS); micSource.connect(mixDest);
                    const sysS = new MediaStream([systemAudio]); sysSource = audioCtx.createMediaStreamSource(sysS); sysSource.connect(mixDest);
                    sendAudioTrack = mixDest.stream.getAudioTracks()[0] || mic;
                } catch { sendAudioTrack = mic; }
            } else {
                sendAudioTrack = mic || systemAudio || null;
            }

            sendVideoTrack = screenVideo;
            updatePreviewTracks(); await applyTracksToAllPeers();
        } catch (e) { alert('Không thể chia sẻ màn hình: ' + e); }
    }

    async function useOBSVirtualCamera() {
        try {
            if (!camStream) { try { await navigator.mediaDevices.getUserMedia({ video: true, audio: false }); } catch { } }
            const devices = await navigator.mediaDevices.enumerateDevices();
            const obs = devices.find(d => d.kind === 'videoinput' && /obs.*virtual.*camera/i.test(d.label || ''));
            if (!obs) { alert('Không tìm thấy "OBS Virtual Camera".'); return; }
            const obsStream = await navigator.mediaDevices.getUserMedia({ video: { deviceId: { exact: obs.deviceId } }, audio: false });
            const video = obsStream.getVideoTracks()[0] || null;

            if (!micStream || !micStream.getAudioTracks().length) {
                try { micStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false }); } catch { }
            }

            stopScreenIfAny(); resetMixing();
            sendVideoTrack = video;
            sendAudioTrack = micStream?.getAudioTracks()[0] || null;

            updatePreviewTracks(); await applyTracksToAllPeers();
        } catch (e) { alert('Không thể dùng OBS: ' + e); }
    }

    function createPcForStudent(studentId) {
        const pc = new RTCPeerConnection(rtcConfig);
        if (sendVideoTrack) pc.addTrack(sendVideoTrack, previewStream);
        if (sendAudioTrack) pc.addTrack(sendAudioTrack, previewStream);

        pc.onicecandidate = (ev) => {
            if (ev.candidate) {
                sendSignal({ type: 'CANDIDATE', roomId: ROOM_ID, from: MY_ID, to: studentId, candidate: JSON.stringify(ev.candidate) });
            }
        };
        pc.onconnectionstatechange = () => logState(pc, `Teacher->${studentId}`);
        pc.oniceconnectionstatechange = () => logState(pc, `Teacher->${studentId}`);
        pc.onsignalingstatechange = () => logState(pc, `Teacher->${studentId}`);

        pcs.set(studentId, pc);
        return pc;
    }
    function drainPendingIce(studentId, pc) {
        const q = pendingIce.get(studentId);
        if (q?.length) { q.forEach(async c => { try { await pc.addIceCandidate(c); } catch (e) { console.warn(e); } }); pendingIce.delete(studentId); }
    }
    function sendSignal(payload) { try { stompClient?.publish({ destination: '/app/signal', body: JSON.stringify(payload) }); } catch (e) { console.warn('[Teacher] publish failed', e); } }

    async function handleSignal(msg) {
        try {
            if (!msg?.type) return;
            if (msg.roomId && msg.roomId !== ROOM_ID) return;

            if (msg.type === 'OFFER' && (!msg.to || msg.to === MY_ID)) {
                const studentId = msg.from;
                let pc = pcs.get(studentId) || createPcForStudent(studentId);
                await pc.setRemoteDescription(new RTCSessionDescription(JSON.parse(msg.sdp)));
                const answer = await pc.createAnswer(); await pc.setLocalDescription(answer);
                sendSignal({ type: 'ANSWER', roomId: ROOM_ID, from: MY_ID, to: studentId, sdp: JSON.stringify(pc.localDescription) });
                drainPendingIce(studentId, pc);
            } else if (msg.type === 'CANDIDATE' && msg.candidate && (!msg.to || msg.to === MY_ID)) {
                const studentId = msg.from;
                let pc = pcs.get(studentId);
                const cand = JSON.parse(msg.candidate);
                if (!pc) { pc = createPcForStudent(studentId); pendingIce.set(studentId, [cand]); return; }
                if (!pc.remoteDescription) { const q = pendingIce.get(studentId) || []; q.push(cand); pendingIce.set(studentId, q); return; }
                try { await pc.addIceCandidate(cand); } catch (e) { console.warn(e); }
            }
        } catch (e) { console.warn('[Teacher] handleSignal error', e); }
    }

    function appendChat(from, text, mine = false) {
        const box = document.getElementById('chat-messages'); if (!box) return;
        const row = document.createElement('div');
        if (mine) row.classList.add('mine');
        row.innerHTML = `<strong>${from}:</strong> ${text}`;
        box.appendChild(row);
        box.scrollTop = box.scrollHeight;
    }
    function setupChatSend() {
        const sendBtn = document.getElementById('chat-send');
        const input = document.getElementById('chat-input');
        if (!sendBtn || !input) return;
        const send = () => {
            const text = (input.value || '').trim(); if (!text) return;
            stompClient.publish({ destination: '/app/chat', body: JSON.stringify({ roomId: ROOM_ID, from: MY_ID, sender: DISPLAY_NAME, text }) });
            appendChat(DISPLAY_NAME, text, true);
            input.value = '';
        };
        sendBtn.addEventListener('click', send);
        input.addEventListener('keydown', e => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(); } });
    }

    function connectWs() {
        if (typeof SockJS === 'undefined' || typeof StompJs === 'undefined') { console.error('SockJS/StompJs not loaded'); return; }
        const socket = new SockJS(WS_ENDPOINT);
        stompClient = new StompJs.Client({ webSocketFactory: () => socket, reconnectDelay: 3000 });
        stompClient.onConnect = () => {
            stompClient.subscribe('/topic/room.' + ROOM_ID, (f) => handleSignal(JSON.parse(f.body)));
            stompClient.subscribe('/topic/chat.' + ROOM_ID, (f) => {
                const m = JSON.parse(f.body);
                if (m && m.from !== MY_ID) appendChat(m.sender || 'User', m.text || '');
            });
            setupChatSend();
        };
        stompClient.onStompError = (f) => console.error('STOMP error', f);
        stompClient.activate();
    }

    // Boot
    document.addEventListener('DOMContentLoaded', () => {
        startHeartbeat();
        startViewerPolling();
        setStatus('READY');
    });
    window.addEventListener('beforeunload', () => {
        stopViewerPolling();
        stopHeartbeat();
    });

    (async function start() {
        await initCameraDefault();
        document.getElementById('btnUseCamera')?.addEventListener('click', (e) => { e.preventDefault(); switchToCamera(); });
        document.getElementById('btnShareScreen')?.addEventListener('click', (e) => { e.preventDefault(); startScreenShare(); });
        document.getElementById('btnUseOBS')?.addEventListener('click', (e) => { e.preventDefault(); useOBSVirtualCamera(); });
        connectWs();
    })();
})();
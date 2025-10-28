(() => {
    if (window.__studentLiveInit) return; window.__studentLiveInit = true;

    const cfgEl = document.getElementById('live-config');
    if (!cfgEl) { console.error('[Student] Missing #live-config'); return; }

    const API_BASE = (cfgEl.dataset.apiBase || '').replace(/\/$/, '');
    const ROOM_ID = cfgEl.dataset.roomId;
    const MY_ID = cfgEl.dataset.myId;
    const DISPLAY_NAME = cfgEl.dataset.displayName || 'Student';
    const WS_ENDPOINT = API_BASE + '/ws';

    // ===== Presence + viewerCount =====
    const HEARTBEAT_MS = 15000;
    const VIEWERS_POLL_MS = 10000;
    let __hbTimer = null;
    let __viewerTimer = null;

    async function sendStudentHeartbeat(live) {
        if (!ROOM_ID || !MY_ID) return;
        const url = `${API_BASE}/api/rooms/${ROOM_ID}/student/heartbeat?participantId=${encodeURIComponent(MY_ID)}&live=${live ? 'true' : 'false'}`;
        try {
            await fetch(url, { method: 'POST', keepalive: !live });
        } catch { /* silent */ }
    }
    function startHeartbeat() {
        clearInterval(__hbTimer);
        sendStudentHeartbeat(true);
        __hbTimer = setInterval(() => sendStudentHeartbeat(true), HEARTBEAT_MS);
    }
    function stopHeartbeat() {
        clearInterval(__hbTimer); __hbTimer = null;
        try {
            const url = `${API_BASE}/api/rooms/${ROOM_ID}/student/heartbeat?participantId=${encodeURIComponent(MY_ID)}&live=false`;
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
        } catch (e) {
            // Optionally show "--" on failure
            const el = document.getElementById('viewer-count');
            if (el) el.textContent = '--';
        }
    }
    function startViewerPolling() {
        clearInterval(__viewerTimer);
        fetchViewerCount();
        __viewerTimer = setInterval(fetchViewerCount, VIEWERS_POLL_MS);
    }
    function stopViewerPolling() {
        clearInterval(__viewerTimer);
        __viewerTimer = null;
    }

    // ===== WebRTC playback =====
    const defaultIce = [{ urls: 'stun:stun.l.google.com:19302' }];
    const extraIce = Array.isArray(window.LIVE_TURN) ? window.LIVE_TURN : [];
    const rtcConfig = { iceServers: [...defaultIce, ...extraIce] };

    const pc = new RTCPeerConnection(rtcConfig);
    const remoteVideo = document.getElementById('remoteVideo');
    const pendingIce = [];

    function setStatus(txt) {
        const el = document.getElementById('student-live-status');
        if (el) el.textContent = txt;
    }
    function logState() {
        console.log(`[Student] connection=${pc.connectionState} ice=${pc.iceConnectionState} signaling=${pc.signalingState}`);
        if (pc.connectionState === 'connected') setStatus('LIVE');
        else if (pc.connectionState === 'connecting') setStatus('CONNECTING');
        else if (pc.connectionState === 'failed') setStatus('FAILED');
        else if (pc.connectionState === 'disconnected') setStatus('DISCONNECTED');
        else if (pc.connectionState === 'closed') setStatus('CLOSED');
    }

    pc.ontrack = (ev) => {
        if (!remoteVideo.srcObject) {
            remoteVideo.srcObject = ev.streams?.[0] || new MediaStream([ev.track]);
        }
        const p = remoteVideo.play();
        if (p?.catch) p.catch(err => console.warn('auto-play blocked', err));
        setStatus('LIVE');
    };

    let stompClient = null;

    pc.onicecandidate = (ev) => {
        if (ev.candidate) {
            stompClient?.publish({
                destination: '/app/signal',
                body: JSON.stringify({
                    type: 'CANDIDATE', roomId: ROOM_ID, from: MY_ID,
                    candidate: JSON.stringify(ev.candidate)
                })
            });
        }
    };
    pc.onconnectionstatechange = logState;
    pc.oniceconnectionstatechange = logState;
    pc.onsignalingstatechange = logState;

    function sendSignal(payload) {
        try { stompClient?.publish({ destination: '/app/signal', body: JSON.stringify(payload) }); }
        catch (e) { console.warn('[Student] publish failed', e); }
    }

    async function startOffer() {
        try {
            pc.addTransceiver('video', { direction: 'recvonly' });
            pc.addTransceiver('audio', { direction: 'recvonly' });
            const offer = await pc.createOffer();
            await pc.setLocalDescription(offer);
            console.log('[Student] sends OFFER (no to) for room', ROOM_ID);
            sendSignal({ type: 'OFFER', roomId: ROOM_ID, from: MY_ID, sdp: JSON.stringify(pc.localDescription) });
            setStatus('CONNECTING');
        } catch (e) { console.error('[Student] startOffer error', e); setStatus('ERROR'); }
    }

    async function handleSignal(msg) {
        try {
            if (!msg?.type) return;
            if (msg.roomId && msg.roomId !== ROOM_ID) return;

            if (msg.type === 'ANSWER' && msg.to === MY_ID) {
                const answer = JSON.parse(msg.sdp);
                await pc.setRemoteDescription(new RTCSessionDescription(answer));
                while (pendingIce.length) {
                    const cand = pendingIce.shift();
                    try { await pc.addIceCandidate(cand); } catch (e) { console.warn('[Student] addICE (drain) failed', e); }
                }
            } else if (msg.type === 'CANDIDATE' && msg.to === MY_ID && msg.candidate) {
                const cand = JSON.parse(msg.candidate);
                if (!pc.remoteDescription) { pendingIce.push(cand); return; }
                try { await pc.addIceCandidate(cand); } catch (e) { console.warn('[Student] addICE failed', e); }
            }
        } catch (e) { console.warn('[Student] handleSignal error', e); }
    }

    // ===== CHAT =====
    function appendChat(from, text, mine = false) {
        const box = document.getElementById('chat-messages');
        if (!box) return;
        const row = document.createElement('div');
        if (mine) row.classList.add('mine');
        row.innerHTML = `<strong>${from}:</strong> ${text}`;
        box.appendChild(row);
        box.scrollTop = box.scrollHeight;
    }
    function setupChat() {
        const sendBtn = document.getElementById('chat-send');
        const input = document.getElementById('chat-input');
        if (!sendBtn || !input) return;

        const send = () => {
            const text = (input.value || '').trim();
            if (!text) return;
            stompClient.publish({
                destination: '/app/chat',
                body: JSON.stringify({ roomId: ROOM_ID, from: MY_ID, sender: DISPLAY_NAME, text })
            });
            appendChat(DISPLAY_NAME, text, true);
            input.value = '';
        };
        sendBtn.onclick = send;
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(); }
        });
    }
    // =================

    function connectWsAndOffer() {
        if (typeof SockJS === 'undefined' || typeof StompJs === 'undefined') {
            console.error('SockJS/StompJs not loaded'); return;
        }
        const socket = new SockJS(WS_ENDPOINT);
        stompClient = new StompJs.Client({ webSocketFactory: () => socket, reconnectDelay: 3000 });
        stompClient.onConnect = async () => {
            console.log('WS connected (Student). Subscribe room topic...');
            stompClient.subscribe('/topic/room.' + ROOM_ID, (frame) => handleSignal(JSON.parse(frame.body)));
            stompClient.subscribe('/topic/chat.' + ROOM_ID, (frame) => {
                const m = JSON.parse(frame.body);
                if (!m) return;
                if (m.from !== MY_ID) appendChat(m.sender || 'Teacher', m.text || '');
            });

            setupChat();
            await startOffer();
        };
        stompClient.onStompError = (f) => console.error('STOMP error', f);
        stompClient.onWebSocketClose = () => console.warn('[Student] WS closed, will auto-reconnect');
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

    connectWsAndOffer();
})();
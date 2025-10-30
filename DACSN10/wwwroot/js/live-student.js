(() => {
    if (window.__studentLiveInit) return; window.__studentLiveInit = true;

    const cfgEl = document.getElementById('live-config');
    if (!cfgEl) { console.error('[Student] Missing #live-config'); return; }

    const API_BASE = (cfgEl.dataset.apiBase || '').replace(/\/$/, '');
    const ROOM_ID = cfgEl.dataset.roomId;
    const MY_ID = cfgEl.dataset.myId;
    const DISPLAY_NAME = (cfgEl.dataset.displayName || 'Student');
    const WS_ENDPOINT = API_BASE + '/ws';

    // ===== Presence + viewerCount =====
    const HEARTBEAT_MS = 15000;
    const VIEWERS_POLL_MS = 10000;
    let hbTimer = null, viewerTimer = null;

    async function sendStudentHeartbeat(live) {
        if (!ROOM_ID || !MY_ID) return;
        const url = `${API_BASE}/api/rooms/${ROOM_ID}/student/heartbeat?participantId=${encodeURIComponent(MY_ID)}&live=${live ? 'true' : 'false'}`;
        try { await fetch(url, { method: 'POST', keepalive: !live }); } catch { }
    }
    function startHeartbeat() {
        clearInterval(hbTimer);
        sendStudentHeartbeat(true);
        hbTimer = setInterval(() => sendStudentHeartbeat(true), HEARTBEAT_MS);
    }
    function stopHeartbeat() {
        clearInterval(hbTimer); hbTimer = null;
        try {
            const url = `${API_BASE}/api/rooms/${ROOM_ID}/student/heartbeat?participantId=${encodeURIComponent(MY_ID)}&live=false`;
            if (navigator.sendBeacon) navigator.sendBeacon(url);
            else fetch(url, { method: 'POST', keepalive: true }).catch(() => { });
        } catch { }
    }
    async function fetchViewerCount() {
        if (!ROOM_ID) return;
        try {
            const resp = await fetch(`${API_BASE}/api/rooms/${ROOM_ID}/viewer-count`, { headers: { Accept: 'application/json' } });
            const data = resp.ok ? await resp.json() : null;
            const n = Number(data?.viewerCount ?? 0);
            const el = document.getElementById('viewer-count');
            if (el) el.textContent = isFinite(n) ? String(n) : '--';
        } catch {
            const el = document.getElementById('viewer-count'); if (el) el.textContent = '--';
        }
    }
    function startViewerPolling() {
        clearInterval(viewerTimer);
        fetchViewerCount();
        viewerTimer = setInterval(fetchViewerCount, VIEWERS_POLL_MS);
    }
    function stopViewerPolling() { clearInterval(viewerTimer); viewerTimer = null; }

    // ===== WebRTC + autoplay Promise-safe + TURN fallback =====
    const defaultIce = [{ urls: 'stun:stun.l.google.com:19302' }];
    const extraIce = Array.isArray(window.LIVE_TURN) ? window.LIVE_TURN : [];
    const ICE_SERVERS_BASE = [...defaultIce, ...extraIce];
    const FORCE_TURN_PARAM = !!window.FORCE_TURN;

    console.log('[LIVE] ICE servers (student):', ICE_SERVERS_BASE);
    console.log('[LIVE] FORCE_TURN (student):', FORCE_TURN_PARAM);

    const videoEl = document.getElementById('remoteVideo');
    const remoteStream = new MediaStream();
    if (videoEl) {
        videoEl.srcObject = remoteStream; // gắn một lần
        videoEl.playsInline = true;
        // Cho phép click trực tiếp để phát nếu autoplay bị chặn
        videoEl.addEventListener('click', () => tryPlay('video-click'));
        // Khi sẵn sàng, thử play lại (không pause sau play)
        ['loadedmetadata', 'canplay', 'resize'].forEach(ev => videoEl.addEventListener(ev, () => tryPlay('video:' + ev)));
    }

    let stompClient = null;
    let pc = null;
    let pendingIce = [];
    let relayOnly = FORCE_TURN_PARAM;          // trạng thái hiện tại (relay-only hay normal)
    let connectTimer = null;
    const CONNECT_TIMEOUT_MS = 8000;           // sau 8s chưa connected -> đảo chiều và re-offer một lần
    let switchedOnce = false;

    function setStatus(txt) {
        const el = document.getElementById('student-live-status');
        if (el) el.textContent = txt;
    }
    function logState() {
        if (!pc) return;
        console.log(`[Student] relayOnly=${relayOnly} connection=${pc.connectionState} ice=${pc.iceConnectionState} signaling=${pc.signalingState}`);
        if (pc.connectionState === 'connected') setStatus('LIVE');
        else if (pc.connectionState === 'connecting') setStatus('CONNECTING');
        else if (pc.connectionState === 'failed') setStatus('FAILED');
        else if (pc.connectionState === 'disconnected') setStatus('DISCONNECTED');
        else if (pc.connectionState === 'closed') setStatus('CLOSED');
    }

    // Theo blog Chrome: luôn bắt Promise của play(), không pause ngay sau play()
    function tryPlay(reason = '') {
        if (!videoEl) return;
        const p = videoEl.play();
        if (p !== undefined) {
            p.then(() => {
                console.log('[Student] play() OK', reason);
                removeGestureUnlock();
            }).catch(err => {
                if (err?.name === 'NotAllowedError') {
                    console.warn('[Student] autoplay prevented — waiting for user gesture');
                    addGestureUnlock();
                } else if (err?.name === 'AbortError') {
                    console.warn('[Student] play() aborted — retry soon');
                    setTimeout(() => tryPlay('retry-after-abort'), 200);
                } else {
                    console.warn('[Student] play() failed:', err?.name || err);
                }
            });
        }
    }

    // Dùng “user gesture” bất kỳ trên trang để gọi play() một lần
    function onUserGesture() { tryPlay('user-gesture'); }
    function addGestureUnlock() {
        document.addEventListener('pointerdown', onUserGesture, { once: true, capture: true });
        document.addEventListener('keydown', onUserGesture, { once: true, capture: true });
        document.addEventListener('touchend', onUserGesture, { once: true, capture: true });
    }
    function removeGestureUnlock() {
        document.removeEventListener('pointerdown', onUserGesture, true);
        document.removeEventListener('keydown', onUserGesture, true);
        document.removeEventListener('touchend', onUserGesture, true);
    }

    // Gắn track: thêm track mới trước, xóa track cũ sau một nhịp để tránh “rỗng” video track gây AbortError
    function attachTrack(t) {
        if (!remoteStream) return;
        if (t.kind === 'video') {
            const old = remoteStream.getVideoTracks()[0];
            if (!old || old.id !== t.id) {
                remoteStream.addTrack(t);
                if (old && old.id !== t.id) setTimeout(() => remoteStream.removeTrack(old), 50);
            }
        } else if (t.kind === 'audio') {
            const old = remoteStream.getAudioTracks()[0];
            if (!old || old.id !== t.id) {
                remoteStream.addTrack(t);
                if (old && old.id !== t.id) setTimeout(() => remoteStream.removeTrack(old), 50);
            }
        }
        tryPlay('attach-' + t.kind);
    }

    // Log selected candidate pair để chẩn đoán (in một lần rồi dừng)
    function logSelectedPairOnce(_pc, label) {
        let stop = false;
        const iv = setInterval(async () => {
            if (stop || !_pc) { clearInterval(iv); return; }
            try {
                const stats = await _pc.getStats();
                let pair = null, lc = null, rc = null;
                stats.forEach(r => {
                    if (r.type === 'transport' && r.selectedCandidatePairId) {
                        pair = stats.get(r.selectedCandidatePairId);
                    } else if (r.type === 'candidate-pair' && (r.selected || r.nominated) && r.state === 'succeeded') {
                        pair = r;
                    }
                });
                if (pair) {
                    lc = stats.get(pair.localCandidateId);
                    rc = stats.get(pair.remoteCandidateId);
                    console.log(`[${label}] SELECTED PAIR state=${pair.state} local=${lc?.candidateType}/${lc?.protocol}/${lc?.ip}:${lc?.port} remote=${rc?.candidateType}/${rc?.protocol}/${rc?.ip}:${rc?.port}`);
                    stop = true; clearInterval(iv);
                }
            } catch { }
        }, 1000);
    }

    function buildRtcConfig(relay) {
        if (relay) {
            // relay-only: chỉ TURN
            const turnsOnly = ICE_SERVERS_BASE.filter(s => String(s.urls).startsWith('turn'));
            return { iceServers: turnsOnly, iceTransportPolicy: 'relay' };
        }
        // normal: STUN + TURN
        return { iceServers: ICE_SERVERS_BASE };
    }

    function createPeer(relay) {
        if (pc) { try { pc.close(); } catch { } }
        pendingIce = [];
        const cfg = buildRtcConfig(relay);
        pc = new RTCPeerConnection(cfg);
        relayOnly = relay;

        pc.ontrack = (ev) => {
            const tracks = ev.streams && ev.streams[0] ? ev.streams[0].getTracks() : [ev.track];
            tracks.forEach(t => {
                console.log('[Student] ontrack kind=', t.kind);
                attachTrack(t);
            });
            setStatus('LIVE');
        };

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

        pc.onconnectionstatechange = () => {
            logState();
            if (pc.connectionState === 'connected') {
                if (connectTimer) { clearTimeout(connectTimer); connectTimer = null; }
                tryPlay('ice-connected');
            } else if (pc.connectionState === 'failed') {
                fallbackReoffer();
            }
        };
        pc.oniceconnectionstatechange = () => {
            logState();
            if (pc.iceConnectionState === 'failed') {
                fallbackReoffer();
            }
        };
        pc.onsignalingstatechange = logState;

        // Log selected-pair (một lần)
        logSelectedPairOnce(pc, relay ? 'Student/relay' : 'Student/normal');

        // Watchdog: nếu sau X giây vẫn chưa connected -> thử chế độ ngược lại
        if (connectTimer) clearTimeout(connectTimer);
        connectTimer = setTimeout(() => {
            if (!pc || pc.connectionState === 'connected') return;
            console.warn('[Student] timeout no connection — switching relayOnly=', !relayOnly);
            fallbackReoffer();
        }, CONNECT_TIMEOUT_MS);

        return pc;
    }

    function sendSignal(payload) {
        try { stompClient?.publish({ destination: '/app/signal', body: JSON.stringify(payload) }); }
        catch (e) { console.warn('[Student] publish failed', e); }
    }

    async function startOffer(relay) {
        try {
            const peer = createPeer(relay);
            // Nhận video/audio
            peer.addTransceiver('video', { direction: 'recvonly' });
            peer.addTransceiver('audio', { direction: 'recvonly' });

            const offer = await peer.createOffer();
            await peer.setLocalDescription(offer);
            console.log('[Student] sends OFFER relayOnly=' + relay + ' room=', ROOM_ID);
            sendSignal({ type: 'OFFER', roomId: ROOM_ID, from: MY_ID, sdp: JSON.stringify(peer.localDescription) });
            setStatus('CONNECTING');
        } catch (e) {
            console.error('[Student] startOffer error', e);
            setStatus('ERROR');
        }
    }

    // Gửi lại OFFER với chế độ ngược lại (normal <-> relay-only) chỉ 1 lần mỗi “lần kết nối”
    function fallbackReoffer() {
        if (switchedOnce) return;
        switchedOnce = true;
        const nextRelay = !relayOnly;
        console.warn('[Student] Fallback re-offer — switch to relayOnly=', nextRelay);
        startOffer(nextRelay);
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
                if (!pc || !pc.remoteDescription) { pendingIce.push(cand); return; }
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
        input.addEventListener('keydown', (e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send(); } });
    }

    function connectWsAndOffer() {
        if (typeof SockJS === 'undefined' || typeof StompJs === 'undefined') {
            console.error('SockJS/StompJs not loaded'); return;
        }
        console.log('[Student] connecting WS via', WS_ENDPOINT);
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
            switchedOnce = false;
            // Bắt đầu theo tham số URL (forceTurn), nếu không kết nối sẽ tự đảo chiều một lần
            startOffer(FORCE_TURN_PARAM);
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
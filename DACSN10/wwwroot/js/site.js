// === AI Chatbot Widget ===
(function () {
    const box = document.getElementById("chatbot-widget");
    const openBtn = document.getElementById("chatbot-open-btn");
    const closeBtn = document.getElementById("chatbot-close-btn");
    const msgBox = document.getElementById("chatbot-messages");
    const input = document.getElementById("chatbot-input");
    const sendBtn = document.getElementById("chatbot-send-btn");
    const quickBtns = document.querySelectorAll(".chatbot-quick-btn");

    function appendUserMessage(text) {
        const wrap = document.createElement("div");
        wrap.style.marginBottom = "8px";
        wrap.style.textAlign = "right";

        const bubble = document.createElement("div");
        bubble.style.display = "inline-block";
        bubble.style.background = "#4f46e5";
        bubble.style.color = "#fff";
        bubble.style.borderRadius = "8px";
        bubble.style.padding = "8px 10px";
        bubble.style.fontSize = "13px";
        bubble.style.maxWidth = "85%";
        bubble.textContent = text;

        wrap.appendChild(bubble);
        msgBox.appendChild(wrap);
        msgBox.scrollTop = msgBox.scrollHeight;
    }

    function appendBotMessage(html) {
        const wrap = document.createElement("div");
        wrap.style.marginBottom = "8px";

        const bubble = document.createElement("div");
        bubble.style.background = "#fff";
        bubble.style.border = "1px solid #ddd";
        bubble.style.borderRadius = "8px";
        bubble.style.padding = "8px 10px";
        bubble.style.fontSize = "13px";
        bubble.style.maxWidth = "85%";
        bubble.innerHTML = html;

        wrap.appendChild(bubble);
        msgBox.appendChild(wrap);
        msgBox.scrollTop = msgBox.scrollHeight;
    }

    async function callChatbot(question) {
        appendUserMessage(question);
        input.value = "";

        const loadingId = "bot-loading-" + Date.now();
        appendBotMessage(`<span id="${loadingId}">Đang trả lời...</span>`);

        try {
            const resp = await fetch("/Chatbot/Ask", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({ question })
            });

            const data = await resp.json();

            const loadingEl = document.getElementById(loadingId);
            if (loadingEl) loadingEl.parentElement.parentElement.remove();

            if (data.error) {
                appendBotMessage("Xin lỗi, hệ thống AI đang lỗi. Hãy thử lại sau.");
                return;
            }

            appendBotMessage(data.answer.replace(/\n/g, "<br/>"));

        } catch (err) {
            console.error(err);
            const loadingEl = document.getElementById(loadingId);
            if (loadingEl) loadingEl.parentElement.parentElement.remove();
            appendBotMessage("Không kết nối được tới AI. Thử lại sau.");
        }
    }

    sendBtn?.addEventListener("click", () => {
        const q = input.value.trim();
        if (q.length > 0) callChatbot(q);
    });

    input?.addEventListener("keydown", e => {
        if (e.key === "Enter") {
            e.preventDefault();
            const q = input.value.trim();
            if (q.length > 0) callChatbot(q);
        }
    });

    quickBtns?.forEach(btn => {
        btn.addEventListener("click", () => {
            const q = btn.getAttribute("data-question");
            if (q) callChatbot(q);
        });
    });

    closeBtn?.addEventListener("click", () => {
        box.style.display = "none";
        openBtn.style.display = "block";
    });

    openBtn?.addEventListener("click", () => {
        box.style.display = "flex";
        openBtn.style.display = "none";
    });
})();

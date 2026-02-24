(function () {
  if (!document.getElementById("chat-fab")) return;

  const chatButton = document.getElementById("chat-fab");
  const chatPopUp = document.getElementById("chat-panel");
  const popUpClose = document.getElementById("chat-panel-close");
  const addBtn = document.getElementById("chat-add-btn");
  const doctorList = document.getElementById("chat-doctor-list");
  const searchInput = document.getElementById("chat-search");

  const newConvPanel = document.getElementById("chat-new-conv");
  const newConvBack = document.getElementById("chat-new-back");
  const newConvClose = document.getElementById("chat-new-close");
  const newDoctorList = document.getElementById("chat-new-doctor-list");
  const newSearchInput = document.getElementById("chat-new-search");

  const convPanel = document.getElementById("chat-conversation");
  const convBack = document.getElementById("chat-back");
  const convClose = document.getElementById("chat-conv-close");
  const convName = document.getElementById("chat-conv-name");
  const messagesDiv = document.getElementById("chat-messages");
  const chatInput = document.getElementById("chat-input");
  const sendBtn = document.getElementById("chat-send");

  let activeDoctorId = null;
  let activeDoctorName = "";
  let allDoctors = [];

  // ── SignalR connection ──
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .withAutomaticReconnect()
    .build();

  connection
    .start()
    .catch((err) => console.error("SignalR connect failed:", err));

  // ── Helpers ──
  function hideAll() {
    chatPopUp.style.display = "none";
    newConvPanel.style.display = "none";
    convPanel.style.display = "none";
  }

  function getInitials(name) {
    return name
      .split(" ")
      .map((w) => w[0])
      .join("")
      .toUpperCase()
      .substring(0, 2);
  }

  function formatTime(isoString) {
    const d = new Date(isoString);
    return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
  }

  // show conversations list ──
  chatButton.addEventListener("click", () => {
    if (chatPopUp.style.display !== "none") {
      hideAll();
      return;
    }
    hideAll();
    chatPopUp.style.display = "flex";
    loadConversations();
  });

  popUpClose.addEventListener("click", hideAll);

  // ── 2. Load existing conversations ──
  function loadConversations() {
    fetch("/Chat/GetConversations")
      .then((r) => r.json())
      .then((convos) => {
        doctorList.innerHTML = "";

        if (convos.length === 0) {
          doctorList.innerHTML = `
                        <div class="chat-empty-state">
                            <i class="fa-regular fa-comment-dots"></i>
                            <p>No conversations yet.<br/>Click + to start one.</p>
                        </div>`;
          return;
        }

        convos.forEach((c) => {
          if (!c.doctorId) return;
          const btn = document.createElement("button");
          btn.className = "chat-doctor-item";
          btn.dataset.doctorId = c.doctorId;
          btn.dataset.doctorName = c.doctorName;

          const preview = c.lastMessage ? c.lastMessage.message : "";
          const time = c.lastMessage ? formatTime(c.lastMessage.timestamp) : "";

          btn.innerHTML = `
                        <div class="chat-doctor-avatar">${getInitials(c.doctorName)}</div>
                        <div class="chat-doctor-info">
                            <span class="chat-doctor-name">${c.doctorName}</span>
                            <span class="chat-doctor-preview">${preview}</span>
                        </div>
                        <div class="chat-doctor-meta">
                            <span class="chat-doctor-time">${time}</span>
                        </div>`;

          btn.addEventListener("click", () =>
            openConversation(c.doctorId, c.doctorName),
          );
          doctorList.appendChild(btn);
        });
      })
      .catch((err) => console.error("Failed to load conversations:", err));
  }

  // ── 3. Search filter on conversations list ──
  searchInput.addEventListener("input", () => {
    const q = searchInput.value.toLowerCase();
    doctorList.querySelectorAll(".chat-doctor-item").forEach((item) => {
      const name = item
        .querySelector(".chat-doctor-name")
        .textContent.toLowerCase();
      item.style.display = name.includes(q) ? "" : "none";
    });
  });

  // ── 4. "+" button → show new conversation picker ──
  addBtn.addEventListener("click", () => {
    hideAll();
    newConvPanel.style.display = "flex";
    loadNewConvDoctors();
  });

  newConvBack.addEventListener("click", () => {
    hideAll();
    chatPopUp.style.display = "flex";
    loadConversations();
  });

  newConvClose.addEventListener("click", hideAll);

  function loadNewConvDoctors() {
    fetch("/Chat/GetMyDoctors")
      .then((r) => r.json())
      .then((doctors) => {
        allDoctors = doctors;
        // Only show doctors the user has NOT chatted with yet
        const newDoctors = doctors.filter((d) => !d.hasConversation);
        newDoctorList.innerHTML = "";

        if (newDoctors.length === 0) {
          newDoctorList.innerHTML = `
                        <div class="chat-empty-state">
                            <i class="fa-regular fa-calendar"></i>
                            <p>You already have conversations with all your doctors.</p>
                        </div>`;
          return;
        }

        newDoctors.forEach((doc) => {
          const btn = document.createElement("button");
          btn.className = "chat-doctor-item";
          btn.dataset.doctorId = doc.doctorId;
          btn.innerHTML = `
                        <div class="chat-doctor-avatar">${getInitials(doc.name)}</div>
                        <div class="chat-doctor-info">
                            <span class="chat-doctor-name">${doc.name}</span>
                            <span class="chat-doctor-preview">Start a conversation…</span>
                        </div>`;
          btn.addEventListener("click", () =>
            openConversation(doc.doctorId, doc.name),
          );
          newDoctorList.appendChild(btn);
        });
      })
      .catch((err) => console.error("Failed to load doctors:", err));
  }

  // Search filter on new conversation list
  newSearchInput.addEventListener("input", () => {
    const q = newSearchInput.value.toLowerCase();
    newDoctorList.querySelectorAll(".chat-doctor-item").forEach((item) => {
      const name = item
        .querySelector(".chat-doctor-name")
        .textContent.toLowerCase();
      item.style.display = name.includes(q) ? "" : "none";
    });
  });

  // ── 5. Open a conversation ──
  function openConversation(doctorId, doctorName) {
    activeDoctorId = doctorId;
    activeDoctorName = doctorName;
    convName.textContent = doctorName;
    messagesDiv.innerHTML = "";

    hideAll();
    convPanel.style.display = "flex";

    // Load existing messages
    fetch(`/Chat/GetMessages?doctorId=${doctorId}`)
      .then((r) => r.json())
      .then((messages) => {
        messages.forEach((m) =>
          appendMessage(m.message, m.isMine, m.timestamp),
        );
        scrollToBottom();
      })
      .catch((err) => console.error("Failed to load messages:", err));

    // Mark messages as read
    connection.invoke("MarkAsRead", doctorId).catch(() => {});
  }

  convBack.addEventListener("click", () => {
    activeDoctorId = null;
    hideAll();
    chatPopUp.style.display = "flex";
    loadConversations();
  });

  convClose.addEventListener("click", () => {
    activeDoctorId = null;
    hideAll();
  });

  // ── 6. Send a message ──
  function sendMessage() {
    const text = chatInput.value.trim();
    if (!text || !activeDoctorId) return;

    connection
      .invoke("SendMessage", activeDoctorId, text)
      .catch((err) => console.error("Send failed:", err));

    chatInput.value = "";
    chatInput.focus();
  }

  sendBtn.addEventListener("click", sendMessage);
  chatInput.addEventListener("keydown", (e) => {
    if (e.key === "Enter") sendMessage();
  });

  // ── 7. Receive messages via SignalR ──
  connection.on("ReceiveMessage", (data) => {
    // If we're viewing this doctor's conversation, show it
    if (data.doctorId === activeDoctorId) {
      appendMessage(data.message, false, data.timestamp);
      scrollToBottom();
      connection.invoke("MarkAsRead", data.doctorId).catch(() => {});
    }
  });

  connection.on("MessageSent", (data) => {
    // Echo of our own message — show it in the chat
    if (data.doctorId === activeDoctorId) {
      appendMessage(data.message, true, data.timestamp);
      scrollToBottom();
    }
  });

  // ── 8. DOM helpers ──
  function appendMessage(text, isMine, timestamp) {
    const wrapper = document.createElement("div");
    wrapper.className = `chat-msg ${isMine ? "chat-msg--sent" : "chat-msg--received"}`;
    wrapper.innerHTML = `
            <div class="chat-msg-bubble">${escapeHtml(text)}</div>
            <span class="chat-msg-time">${formatTime(timestamp)}</span>`;
    messagesDiv.appendChild(wrapper);
  }

  function scrollToBottom() {
    messagesDiv.scrollTop = messagesDiv.scrollHeight;
  }

  function escapeHtml(str) {
    const div = document.createElement("div");
    div.textContent = str;
    return div.innerHTML;
  }
})();

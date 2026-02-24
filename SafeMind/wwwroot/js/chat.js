/**
 * SafeMind – Unified Chat (Patient ↔ Doctor)
 *
 * Detects the user's role from the data-role attribute on #chat-fab
 * and adapts all API calls and SignalR methods accordingly.
 */
(function () {
  const chatButton = document.getElementById("chat-fab");
  if (!chatButton) return;

  // ── Role detection ──
  const ROLE = chatButton.dataset.role; // "patient" or "doctor"
  const isDoctor = ROLE === "doctor";

  // ── DOM refs ──
  const chatPopUp = document.getElementById("chat-panel");
  const popUpClose = document.getElementById("chat-panel-close");
  const addBtn = document.getElementById("chat-add-btn");
  const contactList = document.getElementById("chat-contact-list");
  const searchInput = document.getElementById("chat-search");

  const newConvPanel = document.getElementById("chat-new-conv");
  const newConvBack = document.getElementById("chat-new-back");
  const newConvClose = document.getElementById("chat-new-close");
  const newContactList = document.getElementById("chat-new-contact-list");
  const newSearchInput = document.getElementById("chat-new-search");

  const convPanel = document.getElementById("chat-conversation");
  const convBack = document.getElementById("chat-back");
  const convClose = document.getElementById("chat-conv-close");
  const convName = document.getElementById("chat-conv-name");
  const messagesDiv = document.getElementById("chat-messages");
  const chatInput = document.getElementById("chat-input");
  const sendBtn = document.getElementById("chat-send");

  // Active conversation state — works for both roles
  // For patients:  activeContactId = doctorId (int)
  // For doctors:   activeContactId = patientId (string)
  let activeContactId = null;
  let activeContactName = "";

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
    if (!name) return "?";
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

  function escapeHtml(str) {
    const div = document.createElement("div");
    div.textContent = str;
    return div.innerHTML;
  }

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

  // ── Filter helper for search inputs ──
  function filterList(container, query) {
    container.querySelectorAll(".chat-doctor-item").forEach((item) => {
      const name = item
        .querySelector(".chat-doctor-name")
        .textContent.toLowerCase();
      item.style.display = name.includes(query) ? "" : "none";
    });
  }

  // =====================================================
  //  PANEL 1 – Conversations list
  // =====================================================

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

  function loadConversations() {
    // The server's GetConversations auto-routes by role
    fetch("/Chat/GetConversations")
      .then((r) => r.json())
      .then((convos) => {
        contactList.innerHTML = "";

        if (!convos || convos.length === 0) {
          contactList.innerHTML = `
            <div class="chat-empty-state">
              <i class="fa-regular fa-comment-dots"></i>
              <p>No conversations yet.<br/>Click + to start one.</p>
            </div>`;
          return;
        }

        convos.forEach((c) => {
          // Patient convos have doctorId/doctorName; doctor convos have patientId/patientName
          const contactId = isDoctor ? c.patientId : c.doctorId;
          const contactName = isDoctor ? c.patientName : c.doctorName;
          if (!contactId) return;

          const btn = document.createElement("button");
          btn.className = "chat-doctor-item";
          btn.dataset.contactId = contactId;
          btn.dataset.contactName = contactName;

          const preview = c.lastMessage ? c.lastMessage.message : "";
          const time = c.lastMessage
            ? formatTime(c.lastMessage.timestamp)
            : "";

          btn.innerHTML = `
            <div class="chat-doctor-avatar">${getInitials(contactName)}</div>
            <div class="chat-doctor-info">
              <span class="chat-doctor-name">${escapeHtml(contactName)}</span>
              <span class="chat-doctor-preview">${escapeHtml(preview)}</span>
            </div>
            <div class="chat-doctor-meta">
              <span class="chat-doctor-time">${time}</span>
            </div>`;

          btn.addEventListener("click", () =>
            openConversation(contactId, contactName),
          );
          contactList.appendChild(btn);
        });
      })
      .catch((err) => console.error("Failed to load conversations:", err));
  }

  searchInput.addEventListener("input", () =>
    filterList(contactList, searchInput.value.toLowerCase()),
  );

  // =====================================================
  //  PANEL 2 – New conversation picker
  // =====================================================

  addBtn.addEventListener("click", () => {
    hideAll();
    newConvPanel.style.display = "flex";
    loadNewContacts();
  });

  newConvBack.addEventListener("click", () => {
    hideAll();
    chatPopUp.style.display = "flex";
    loadConversations();
  });

  newConvClose.addEventListener("click", hideAll);

  function loadNewContacts() {
    // Patient fetches doctors; doctor fetches patients
    const url = isDoctor ? "/Chat/GetMyPatients" : "/Chat/GetMyDoctors";
    fetch(url)
      .then((r) => r.json())
      .then((contacts) => {
        // Only show those the user has NOT chatted with yet
        const fresh = contacts.filter((c) => !c.hasConversation);
        newContactList.innerHTML = "";

        if (fresh.length === 0) {
          const who = isDoctor ? "patients" : "doctors";
          newContactList.innerHTML = `
            <div class="chat-empty-state">
              <i class="fa-regular fa-calendar"></i>
              <p>You already have conversations with all your ${who}.</p>
            </div>`;
          return;
        }

        fresh.forEach((item) => {
          const contactId = isDoctor ? item.patientId : item.doctorId;
          const contactName = item.name;

          const btn = document.createElement("button");
          btn.className = "chat-doctor-item";
          btn.dataset.contactId = contactId;
          btn.innerHTML = `
            <div class="chat-doctor-avatar">${getInitials(contactName)}</div>
            <div class="chat-doctor-info">
              <span class="chat-doctor-name">${escapeHtml(contactName)}</span>
              <span class="chat-doctor-preview">Start a conversation…</span>
            </div>`;
          btn.addEventListener("click", () =>
            openConversation(contactId, contactName),
          );
          newContactList.appendChild(btn);
        });
      })
      .catch((err) => console.error("Failed to load contacts:", err));
  }

  newSearchInput.addEventListener("input", () =>
    filterList(newContactList, newSearchInput.value.toLowerCase()),
  );

  // =====================================================
  //  PANEL 3 – Conversation view
  // =====================================================

  function openConversation(contactId, contactName) {
    activeContactId = contactId;
    activeContactName = contactName;
    convName.textContent = contactName;
    messagesDiv.innerHTML = "";

    hideAll();
    convPanel.style.display = "flex";

    // Fetch message history
    const url = isDoctor
      ? `/Chat/GetPatientMessages?patientId=${encodeURIComponent(contactId)}`
      : `/Chat/GetMessages?doctorId=${contactId}`;

    fetch(url)
      .then((r) => r.json())
      .then((messages) => {
        messages.forEach((m) =>
          appendMessage(m.message, m.isMine, m.timestamp),
        );
        scrollToBottom();
      })
      .catch((err) => console.error("Failed to load messages:", err));

    // Mark unread messages as read
    if (isDoctor) {
      connection
        .invoke("MarkPatientAsRead", String(contactId))
        .catch(() => {});
    } else {
      connection.invoke("MarkAsRead", contactId).catch(() => {});
    }
  }

  convBack.addEventListener("click", () => {
    activeContactId = null;
    hideAll();
    chatPopUp.style.display = "flex";
    loadConversations();
  });

  convClose.addEventListener("click", () => {
    activeContactId = null;
    hideAll();
  });

  // =====================================================
  //  Send a message
  // =====================================================

  function sendMessage() {
    const text = chatInput.value.trim();
    if (!text || activeContactId === null) return;

    if (isDoctor) {
      connection
        .invoke("SendMessageToPatient", String(activeContactId), text)
        .catch((err) => console.error("Send failed:", err));
    } else {
      connection
        .invoke("SendMessage", activeContactId, text)
        .catch((err) => console.error("Send failed:", err));
    }

    chatInput.value = "";
    chatInput.focus();
  }

  sendBtn.addEventListener("click", sendMessage);
  chatInput.addEventListener("keydown", (e) => {
    if (e.key === "Enter") sendMessage();
  });

  // =====================================================
  //  SignalR receive handlers (shared for both roles)
  // =====================================================

  connection.on("ReceiveMessage", (data) => {
    // data has: doctorId, patientId, message, timestamp
    // Determine if this message belongs to the currently open conversation
    const relevantId = isDoctor ? data.patientId : data.doctorId;

    if (relevantId === activeContactId) {
      appendMessage(data.message, false, data.timestamp);
      scrollToBottom();

      // Auto-mark as read since we're viewing it
      if (isDoctor) {
        connection
          .invoke("MarkPatientAsRead", String(data.patientId))
          .catch(() => {});
      } else {
        connection.invoke("MarkAsRead", data.doctorId).catch(() => {});
      }
    }
    // TODO: increment unread badge when conversation is not open
  });

  connection.on("MessageSent", (data) => {
    // Echo of our own sent message
    const relevantId = isDoctor ? data.patientId : data.doctorId;

    if (relevantId === activeContactId) {
      appendMessage(data.message, true, data.timestamp);
      scrollToBottom();
    }
  });
})();

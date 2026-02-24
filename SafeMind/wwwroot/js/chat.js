(function () {
    if (!document.getElementById("chat-fab")) return;

    const chatButton = document.getElementById("chat-fab");
    const closeButton = document.getElementById("chat-panel-close");
    const chatPopup = document.getElementById("chat-panel");

    var connectionChatMessages = new signalR.HubConnectionBuilder()
        .withUrl("/chathub")
        .build();

    chatButton.addEventListener("click", () => {
        if (chatPopup.style.display === "none") {
            chatPopup.style.display = "flex";
            chatButton.classList.add("active");
        } else {
            chatPopup.style.display = "none";
            chatButton.classList.remove("active");
        }
    });

    closeButton.addEventListener("click", () => {
        chatPopup.style.display = "none";
        chatButton.classList.remove("active");
    });
    connectionChatMessages.start().then((function () {
        console.log("Connected to ChatHub");
    }
    ));
})();

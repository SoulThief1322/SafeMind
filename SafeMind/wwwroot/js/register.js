const signUpBtn = document.getElementById("signUpBtn");
const registerModal = document.getElementById("registerModal");
const registerOverlay = document.getElementById("registerOverlay");
const registerCloseBtn = document.getElementById("registerCloseBtn");

function openRegister() {
  registerModal.classList.remove("hidden");
  registerOverlay.classList.remove("hidden");
  registerModal.setAttribute("aria-hidden", "false");
  registerOverlay.setAttribute("aria-hidden", "false");
  document.body.style.overflow = "hidden";

  const firstInput = registerModal.querySelector("input");
  firstInput?.focus();
}

function closeRegister() {
  registerModal.classList.add("hidden");
  registerOverlay.classList.add("hidden");
  registerModal.setAttribute("aria-hidden", "true");
  registerOverlay.setAttribute("aria-hidden", "true");
  document.body.style.overflow = "";
}

signUpBtn?.addEventListener("click", e => {
  e.preventDefault();
  closeLogin();
  openRegister();
});

registerOverlay?.addEventListener("click", closeRegister);
registerCloseBtn?.addEventListener("click", closeRegister);
document.getElementById("switchToRegister")?.addEventListener("click", e => {
  e.preventDefault();
  closeLogin();
  openRegister();
});

document.getElementById("switchToLogin")?.addEventListener("click", e => {
  e.preventDefault();
  closeRegister();
  openLogin();
});
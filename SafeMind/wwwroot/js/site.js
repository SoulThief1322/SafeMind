// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

const qs = new URLSearchParams(location.search);
const openLoginModal = () => {
    if (typeof openLogin === "function") {
        openLogin();
    } else if (typeof openAuthModal === "function") {
        openAuthModal("login");
    }
};

if (qs.get("auth") === "login") {
    openLoginModal();
    history.replaceState({}, "", location.pathname + location.hash);
}

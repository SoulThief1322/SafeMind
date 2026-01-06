const signInBtn = document.getElementById("signInBtn");
const loginModal = document.getElementById("loginModal");
const loginOverlay = document.getElementById("loginOverlay");
const loginCloseBtn = document.getElementById("loginCloseBtn");

function openLogin() {
  if (!loginModal || !loginOverlay) return;
  loginModal.classList.remove("hidden");
  loginOverlay.classList.remove("hidden");
  loginModal.setAttribute('aria-hidden', 'false');
  loginOverlay.setAttribute('aria-hidden', 'false');
  document.body.style.overflow = "hidden";
  const firstInput = loginModal.querySelector('input, button, [tabindex]:not([tabindex="-1"])');
  if (firstInput) firstInput.focus();
}

function closeLogin() {
  if (!loginModal || !loginOverlay) return;
  loginModal.classList.add("hidden");
  loginOverlay.classList.add("hidden");
  loginModal.setAttribute('aria-hidden', 'true');
  loginOverlay.setAttribute('aria-hidden', 'true');
  document.body.style.overflow = "";
  if (signInBtn) signInBtn.focus();
}

if (signInBtn) {
  signInBtn.addEventListener("click", e => {
    e.preventDefault();
    if (!loginModal) {
      window.location.href = '/Identity/Account/Login';
      return;
    }
    openLogin();
  });
}

loginOverlay?.addEventListener("click", closeLogin);
loginCloseBtn?.addEventListener("click", closeLogin);

const loginForm = loginModal ? loginModal.querySelector('form.login-form') : null;
const loginErrors = loginModal ? loginModal.querySelector('.login-errors') : null;

function showLoginErrors(messages) {
  if (!loginErrors) return;
  if (!messages || messages.length === 0) {
    loginErrors.innerHTML = '';
    loginErrors.classList.remove('visible');
    return;
  }
  loginErrors.innerHTML = '<ul>' + messages.map(m => '<li>' + m + '</li>').join('') + '</ul>';
  loginErrors.classList.add('visible');
}

if (loginForm) {
  loginForm.addEventListener('submit', async (e) => {
    e.preventDefault();
    showLoginErrors([]);

    const submitBtn = loginForm.querySelector('button[type="submit"]');
    if (submitBtn) submitBtn.disabled = true;

    try {
      const formData = new FormData(loginForm);
      const resp = await fetch(loginForm.action, {
        method: 'POST',
        body: formData,
        headers: {
          'X-Requested-With': 'XMLHttpRequest'
        },
        credentials: 'same-origin'
      });

      const contentType = resp.headers.get('content-type') || '';
      if (resp.ok && contentType.includes('application/json')) {
        const data = await resp.json();
        if (data.succeeded) {
          closeLogin();
          if (data.redirect) {
            window.location.href = data.redirect;
          } else {
            window.location.reload();
          }
          return;
        } else {
          if (data.errors && data.errors.length) {
            showLoginErrors(data.errors);
          } else if (data.reason) {
            showLoginErrors([data.reason]);
          } else {
            showLoginErrors(['Login failed.']);
          }
        }
      } else {
        if (resp.status === 400 && contentType.includes('application/json')) {
          const data = await resp.json();
          if (data.errors && data.errors.length) {
            showLoginErrors(data.errors);
          } else if (data.reason) {
            showLoginErrors([data.reason]);
          } else {
            showLoginErrors(['Invalid login attempt.']);
          }
        } else {
          window.location.href = '/Identity/Account/Login';
        }
      }
    } catch (err) {
      console.error('Login request failed', err);
      showLoginErrors(['Network error - please try again.']);
    } finally {
      if (submitBtn) submitBtn.disabled = false;
    }
  });
}

document.addEventListener("keydown", e => {
  if (e.key === "Escape" && loginModal && !loginModal.classList.contains("hidden")) {
    closeLogin();
  }
});

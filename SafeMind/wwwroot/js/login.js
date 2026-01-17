document.addEventListener("DOMContentLoaded", () => {
  const modal = document.getElementById("loginModal");
  const overlay = document.getElementById("loginOverlay");

  window.openLogin = () => {
    modal.classList.remove("hidden");
    overlay.classList.remove("hidden");
    document.body.style.overflow = "hidden";
  };

  window.closeLogin = () => {
    modal.classList.add("hidden");
    overlay.classList.add("hidden");
    document.body.style.overflow = "";
  };

  document.getElementById("loginCloseBtn")?.addEventListener("click", closeLogin);
  overlay?.addEventListener("click", closeLogin);

  document.getElementById("switchToRegister")?.addEventListener("click", e => {
    e.preventDefault();
    closeLogin();
    window.openRegister();
  });

  document.addEventListener("keydown", e => {
    if (e.key === "Escape" && !modal.classList.contains("hidden")) {
      closeLogin();
    }
  });

  document.getElementById("signInBtn")?.addEventListener("click", e => {
    e.preventDefault();
    openLogin();
  });

  const form = modal?.querySelector('form.login-form');
  const errors = modal?.querySelector('.login-errors');

  const showErrors = msgs => {
    if (!errors) return;
    if (!msgs || msgs.length === 0) {
      errors.innerHTML = '';
      errors.classList.remove('visible');
      return;
    }
    errors.innerHTML = '<ul>' + msgs.map(m => `<li>${m}</li>`).join('') + '</ul>';
    errors.classList.add('visible');
  };

  form?.addEventListener('submit', async e => {
    e.preventDefault();
    showErrors([]);
    const submitBtn = form.querySelector('button[type="submit"]');
    if (submitBtn) submitBtn.disabled = true;
    try {
      const formData = new FormData(form);
      const resp = await fetch(form.action, {
        method: 'POST',
        body: formData,
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin',
        redirect: 'manual'
      });

      const ct = resp.headers.get('content-type') || '';
      const isRedirect = resp.type === 'opaqueredirect' || resp.redirected || (resp.status >= 300 && resp.status < 400);

      if (isRedirect) {
        const loc = resp.headers.get('Location');
        if (loc) {
          window.location.href = loc;
        } else {
          window.location.reload();
        }
      } else if (resp.ok && ct.includes('application/json')) {
        const data = await resp.json();
        if (data.succeeded) {
          closeLogin();
          if (data.redirect) window.location.href = data.redirect; else window.location.reload();
        } else if (data.errors?.length) {
          showErrors(data.errors);
        } else if (data.reason) {
          showErrors([data.reason]);
        } else {
          showErrors(['Login failed.']);
        }
      } else if (resp.ok && ct.includes('text/html')) {
        showErrors(['Invalid login attempt.']);
      } else if (resp.status === 400 && ct.includes('application/json')) {
        const data = await resp.json();
        if (data.errors?.length) showErrors(data.errors); else if (data.reason) showErrors([data.reason]); else showErrors(['Invalid login attempt.']);
      } else {
        showErrors(['Invalid login attempt.']);
      }
    } catch (err) {
      console.error('Login request failed', err);
      showErrors(['Network error - please try again.']);
    } finally {
      if (submitBtn) submitBtn.disabled = false;
    }
  });
});

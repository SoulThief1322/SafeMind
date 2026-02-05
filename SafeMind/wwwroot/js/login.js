// Sets up login modal behavior and AJAX submission.
document.addEventListener("DOMContentLoaded", () => {
  const modal = document.getElementById("loginModal");
  const overlay = document.getElementById("loginOverlay");

  if (!modal || !overlay) return;

  // Opens the login modal.
  window.openLogin = () => {
    modal.classList.remove("hidden");
    overlay.classList.remove("hidden");
    document.body.style.overflow = "hidden";
  };

  // Closes the login modal.
  window.closeLogin = () => {
    modal.classList.add("hidden");
    overlay.classList.add("hidden");
    document.body.style.overflow = "";
  };

  document.getElementById("loginCloseBtn")?.addEventListener("click", closeLogin);
  overlay?.addEventListener("click", closeLogin);

  document.getElementById("switchToRegister")?.addEventListener("click", event => {
    event.preventDefault();
    closeLogin();
    window.openRegister();
  });

  document.addEventListener("keydown", event => {
    if (event.key === "Escape" && !modal.classList.contains("hidden")) {
      closeLogin();
    }
  });

  document.getElementById("signInBtn")?.addEventListener("click", event => {
    event.preventDefault();
    openLogin();
  });

  const form = modal?.querySelector('form.login-form');
  const errors = modal?.querySelector('.login-errors');

  // Renders validation messages inside the modal.
  const showErrors = messages => {
    if (!errors) return;
    if (!messages || messages.length === 0) {
      errors.innerHTML = '';
      errors.classList.remove('visible');
      return;
    }
    errors.innerHTML = '<ul>' + messages.map(message => `<li>${message}</li>`).join('') + '</ul>';
    errors.classList.add('visible');
  };

  // Handles AJAX login submission and error display.
  form?.addEventListener('submit', async event => {
    event.preventDefault();
    showErrors([]);
    const submitBtn = form.querySelector('button[type="submit"]');
    if (submitBtn) submitBtn.disabled = true;
    try {
      const formData = new FormData(form);
      const response = await fetch(form.action, {
        method: 'POST',
        body: formData,
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
        credentials: 'same-origin',
        redirect: 'manual'
      });

      const contentType = response.headers.get('content-type') || '';
      const isRedirect = response.type === 'opaqueredirect' || response.redirected || (response.status >= 300 && response.status < 400);

      if (isRedirect) {
        const redirectLocation = response.headers.get('Location');
        if (redirectLocation) {
          window.location.href = redirectLocation;
        } else {
          window.location.reload();
        }
      } else if (response.ok && contentType.includes('application/json')) {
        const data = await response.json();
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
      } else if (response.ok && contentType.includes('text/html')) {
        showErrors(['Invalid login attempt.']);
      } else if (response.status === 400 && contentType.includes('application/json')) {
        const data = await response.json();
        if (data.errors?.length) showErrors(data.errors); else if (data.reason) showErrors([data.reason]); else showErrors(['Invalid login attempt.']);
      } else {
        showErrors(['Invalid login attempt.']);
      }
    } catch (error) {
      console.error('Login request failed', error);
      showErrors(['Network error - please try again.']);
    } finally {
      if (submitBtn) submitBtn.disabled = false;
    }
  });
});

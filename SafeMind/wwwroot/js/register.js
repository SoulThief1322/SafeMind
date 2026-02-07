document.addEventListener("DOMContentLoaded", () => {
  const modal = document.getElementById("registerModal");
  const overlay = document.getElementById("registerOverlay");

  if (modal && overlay) {
    const close = () => {
      modal.classList.add("hidden");
      overlay.classList.add("hidden");
      document.body.style.overflow = "";
    };

    const open = () => {
      modal.classList.remove("hidden");
      overlay.classList.remove("hidden");
      document.body.style.overflow = "hidden";
    };

    window.openRegister = open;
    window.closeRegister = close;

    overlay.addEventListener("click", close);
    document.getElementById("registerCloseBtn")?.addEventListener("click", close);

    document.getElementById("switchToLogin")?.addEventListener("click", e => {
      e.preventDefault();
      close();
      window.openLogin?.();
    });

    document.addEventListener("keydown", e => {
      if (e.key === "Escape" && !modal.classList.contains("hidden")) close();
    });

    const form = modal.querySelector("form.register-form");
    const errors = modal.querySelector(".register-errors");

    const showErrors = list => {
      if (!errors) return;
      if (!list?.length) {
        errors.innerHTML = "";
        errors.classList.remove("visible");
        return;
      }
      errors.innerHTML = `<ul>${list.map(e => `<li>${e}</li>`).join("")}</ul>`;
      errors.classList.add("visible");
    };

    form?.addEventListener("submit", async e => {
      e.preventDefault();
      showErrors([]);
      const btn = form.querySelector('[type="submit"]');
      if (btn) btn.disabled = true;

      try {
        const res = await fetch(form.action, {
          method: "POST",
          body: new FormData(form),
          headers: { "X-Requested-With": "XMLHttpRequest" },
          credentials: "same-origin",
          redirect: "manual"
        });

        const type = res.headers.get("content-type") || "";

        if (res.redirected || (res.status >= 300 && res.status < 400)) {
          window.location.href = res.headers.get("Location") || window.location.href;
          return;
        }

        if (type.includes("application/json")) {
          const data = await res.json();
          if (data.succeeded) {
            close();
            window.location.href = data.redirect || window.location.href;
          } else {
            showErrors(data.errors || [data.reason || "Registration failed."]);
          }
          return;
        }

        showErrors(["Invalid registration attempt."]);
      } catch {
        showErrors(["Network error. Please try again."]);
      } finally {
        if (btn) btn.disabled = false;
      }
    });
  }

  const pickers = document.querySelectorAll(".time-picker");
  if (!pickers.length) return;

  const closeAll = () => {
    pickers.forEach(p => {
      p.classList.remove("is-open");
      p.querySelector(".time-panel")?.setAttribute("aria-hidden", "true");
      p.querySelector(".time-display")?.setAttribute("aria-expanded", "false");
    });
  };

  const pad = n => String(n).padStart(2, "0");

  pickers.forEach(picker => {
    const input = picker.querySelector('input[type="time"]');
    const display = picker.querySelector(".time-display");
    const panel = picker.querySelector(".time-panel");
    if (!input || !display || !panel) return;

    const step = Math.max(1, Math.round(Number(input.step || 900) / 60));
    let h = null;
    let m = null;
    let confirmMinute = true;

    const col = type =>
      Object.assign(document.createElement("div"), { className: "time-column" });

    const hourCol = col("hour");
    const minCol = col("minute");

    for (let i = 0; i < 24; i++) {
      const b = document.createElement("button");
      b.type = "button";
      b.textContent = pad(i);
      b.onclick = () => {
        h = i;
        confirmMinute = true;
        render();
      };
      hourCol.appendChild(b);
    }

    for (let i = 0; i < 60; i += step) {
      const b = document.createElement("button");
      b.type = "button";
      b.textContent = pad(i);
      b.onclick = () => {
        m = i;
        if (h !== null) {
          confirmMinute = false;
          commit();
          closeAll();
        }
        render();
      };
      minCol.appendChild(b);
    }

    panel.append(hourCol, minCol);

    const render = () => {
      display.textContent = `${h === null ? "--" : pad(h)}:${m === null || confirmMinute ? "--" : pad(m)}`;
    };

    const commit = () => {
      if (h !== null && m !== null && !confirmMinute) {
        input.value = `${pad(h)}:${pad(m)}`;
        display.textContent = input.value;
      }
    };

    display.onclick = () => {
      const open = picker.classList.contains("is-open");
      closeAll();
      if (!open) {
        picker.classList.add("is-open");
        panel.setAttribute("aria-hidden", "false");
        display.setAttribute("aria-expanded", "true");
      }
    };

    input.addEventListener("input", () => {
      const [hh, mm] = input.value.split(":").map(Number);
      if (!Number.isNaN(hh) && !Number.isNaN(mm)) {
        h = hh;
        m = mm;
        confirmMinute = false;
      } else {
        h = m = null;
        confirmMinute = true;
      }
      render();
    });

    if (input.value) {
      const [hh, mm] = input.value.split(":").map(Number);
      if (!Number.isNaN(hh) && !Number.isNaN(mm)) {
        h = hh;
        m = mm;
        confirmMinute = false;
      }
    }

    render();
  });

  document.addEventListener("click", e => {
    if (!e.target.closest(".time-picker")) closeAll();
  });

  document.addEventListener("keydown", e => {
    if (e.key === "Escape") closeAll();
  });
});
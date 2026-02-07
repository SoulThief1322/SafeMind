document.addEventListener("DOMContentLoaded", () => {
  const modal = document.getElementById("registerModal");
  const overlay = document.getElementById("registerOverlay");

  if (modal && overlay) {
    const open = () => {
      modal.classList.remove("hidden");
      overlay.classList.remove("hidden");
      document.body.style.overflow = "hidden";
    };

    const close = () => {
      modal.classList.add("hidden");
      overlay.classList.add("hidden");
      document.body.style.overflow = "";
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

    const startInput = form?.querySelector("#workStart");
    const endInput = form?.querySelector("#workEnd");

    const isValidTimeRange = () => {
      if (!startInput || !endInput) return true;
      if (!startInput.value || !endInput.value) return true;
      return endInput.value > startInput.value;
    };

    form?.addEventListener("submit", async e => {
      e.preventDefault();
      showErrors([]);

      if (!isValidTimeRange()) {
        showErrors(["End time must be after start time."]);
        return;
      }

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
        showErrors(["Network error - please try again."]);
      } finally {
        if (btn) btn.disabled = false;
      }
    });
  }

  const pickers = document.querySelectorAll(".time-picker");
  if (!pickers.length) return;

  const pad = n => String(n).padStart(2, "0");

  const closeAll = () => {
    pickers.forEach(p => {
      p.classList.remove("is-open");
      p.querySelector(".time-panel")?.setAttribute("aria-hidden", "true");
      p.querySelector(".time-display")?.setAttribute("aria-expanded", "false");
    });
  };

  pickers.forEach(picker => {
    const input = picker.querySelector('input[type="time"]');
    const display = picker.querySelector(".time-display");
    const panel = picker.querySelector(".time-panel");
    if (!input || !display || !panel) return;

    const step = Math.max(1, Math.round(Number(picker.dataset.step || input.step || 900) / 60));

    let hour = null;
    let minute = null;
    let confirmMinute = true;

    const hourCol = document.createElement("div");
    hourCol.className = "time-column";

    const minuteCol = document.createElement("div");
    minuteCol.className = "time-column";

    for (let h = 0; h < 24; h++) {
      const b = document.createElement("button");
      b.type = "button";
      b.className = "time-option";
      b.dataset.value = h;
      b.textContent = pad(h);
      b.onclick = () => {
        hour = h;
        confirmMinute = true;
        render(false);
      };
      hourCol.appendChild(b);
    }

    for (let m = 0; m < 60; m += step) {
      const b = document.createElement("button");
      b.type = "button";
      b.className = "time-option";
      b.dataset.value = m;
      b.textContent = pad(m);
      b.onclick = () => {
        minute = m;
        if (hour !== null) {
          confirmMinute = false;
          render(true);
          closeAll();
        } else {
          render(false);
        }
      };
      minuteCol.appendChild(b);
    }

    panel.append(hourCol, minuteCol);

    const updateActive = () => {
      panel.querySelectorAll(".time-option").forEach(opt => {
        const val = Number(opt.dataset.value);
        const isHour = opt.parentElement === hourCol;
        opt.classList.toggle(
          "is-active",
          isHour ? val === hour : val === minute
        );
      });
    };

    const render = commit => {
      const hText = hour === null ? "--" : pad(hour);
      const mText = minute === null || confirmMinute ? "--" : pad(minute);
      display.textContent = `${hText}:${mText}`;
      updateActive();

      if (commit && hour !== null && minute !== null && !confirmMinute) {
        input.value = `${pad(hour)}:${pad(minute)}`;
        display.textContent = input.value;
        input.dispatchEvent(new Event("input", { bubbles: true }));
      }
    };

    display.addEventListener("click", () => {
      const open = picker.classList.contains("is-open");
      closeAll();
      if (!open) {
        picker.classList.add("is-open");
        panel.setAttribute("aria-hidden", "false");
        display.setAttribute("aria-expanded", "true");
      }
    });

    input.addEventListener("input", () => {
      const [h, m] = input.value.split(":").map(Number);
      if (!Number.isNaN(h) && !Number.isNaN(m)) {
        hour = h;
        minute = m;
        confirmMinute = false;
      } else {
        hour = minute = null;
        confirmMinute = true;
      }
      render(false);
    });

    if (input.value || input.defaultValue) {
      const [h, m] = (input.value || input.defaultValue).split(":").map(Number);
      if (!Number.isNaN(h) && !Number.isNaN(m)) {
        hour = h;
        minute = m;
        confirmMinute = false;
      }
    }

    render(false);
  });

  document.addEventListener("click", e => {
    if (!e.target.closest(".time-picker")) closeAll();
  });

  document.addEventListener("keydown", e => {
    if (e.key === "Escape") closeAll();
  });
});

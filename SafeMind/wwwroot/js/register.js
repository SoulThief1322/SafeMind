document.addEventListener("DOMContentLoaded", () => {
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

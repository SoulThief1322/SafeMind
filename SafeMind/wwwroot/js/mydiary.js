document.addEventListener("DOMContentLoaded", () => {
	const weekRow = document.querySelector("[data-week-row]");
	const weekLabel = document.querySelector("[data-week-label]");
	const calendarBody = document.querySelector("[data-calendar-body]");
	const monthLabel = document.querySelector("[data-month-label]");
	const prevMonthBtn = document.querySelector("[data-cal-prev]");
	const nextMonthBtn = document.querySelector("[data-cal-next]");
	if (!weekRow || !weekLabel) return;

	const dayNames = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];

	const today = new Date();
	const startOfWeek = (() => {
		const d = new Date(today);
		const day = d.getDay(); // 0 = Sun, 1 = Mon
		const offset = day === 0 ? -6 : 1 - day; // shift so Monday is start
		d.setDate(d.getDate() + offset);
		d.setHours(0, 0, 0, 0);
		return d;
	})();

	const formatISO = (date) => date.toISOString().split("T")[0];
	const formatDom = (date) => date.toLocaleDateString(undefined, { day: "2-digit" });
	const formatLabel = (date) => date.toLocaleDateString(undefined, { month: "short", day: "numeric" });

	const endOfWeek = new Date(startOfWeek);
	endOfWeek.setDate(startOfWeek.getDate() + 6);
	weekLabel.textContent = `This week · ${formatLabel(startOfWeek)} – ${formatLabel(endOfWeek)}`;

	weekRow.innerHTML = "";

	for (let i = 0; i < 7; i++) {
		const current = new Date(startOfWeek);
		current.setDate(startOfWeek.getDate() + i);

		const pill = document.createElement("button");
		pill.type = "button";
		pill.className = "day-pill";
		pill.setAttribute("role", "listitem");
		pill.dataset.date = formatISO(current);

		const dow = document.createElement("span");
		dow.className = "dow";
		dow.textContent = dayNames[i];

		const dom = document.createElement("span");
		dom.className = "dom";
		dom.textContent = formatDom(current);

		pill.appendChild(dow);
		pill.appendChild(dom);

		const isToday = current.toDateString() === today.toDateString();
		if (isToday) {
			pill.classList.add("active");
			pill.setAttribute("aria-current", "date");
		}

		weekRow.appendChild(pill);
	}

	if (calendarBody && monthLabel && prevMonthBtn && nextMonthBtn) {
		let monthOffset = 0;

		const renderCalendar = () => {
			const base = new Date(today);
			base.setDate(1);
			base.setMonth(base.getMonth() + monthOffset);
			const baseMonth = base.getMonth();
			const baseYear = base.getFullYear();

			monthLabel.textContent = base.toLocaleDateString(undefined, {
				month: "long",
				year: "numeric"
			});

			const startDay = base.getDay();
			const offset = startDay === 0 ? -6 : 1 - startDay; // align to Monday
			const gridStart = new Date(base);
			gridStart.setDate(base.getDate() + offset);

			calendarBody.innerHTML = "";

			const setSelectedDate = (dateStr) => {
				calendarBody.querySelectorAll(".mc-day").forEach((btn) => {
					btn.classList.remove("mc-selected");
					btn.removeAttribute("aria-current");
				});
				const match = calendarBody.querySelector(`[data-date='${dateStr}']`);
				if (match) {
					match.classList.add("mc-selected");
					match.setAttribute("aria-current", "date");
				}
			};

			let initialSelectable = null;

			for (let week = 0; week < 6; week++) {
				const row = document.createElement("div");
				row.className = "mc-row";
				row.setAttribute("role", "row");

				for (let day = 0; day < 7; day++) {
					const current = new Date(gridStart);
					current.setDate(gridStart.getDate() + week * 7 + day);

					const btn = document.createElement("button");
					btn.type = "button";
					btn.className = "mc-day";
					btn.setAttribute("role", "gridcell");
					btn.dataset.date = formatISO(current);
					btn.textContent = current.getDate();

					const isCurrentMonth = current.getMonth() === baseMonth;
					if (!isCurrentMonth) {
						btn.disabled = true;
						btn.setAttribute("aria-disabled", "true");
					}

					const isToday = current.toDateString() === today.toDateString();
					if (isToday && isCurrentMonth && monthOffset === 0) {
						btn.classList.add("mc-today");
						initialSelectable = formatISO(current);
					}

					if (isCurrentMonth) {
						if (!initialSelectable) initialSelectable = formatISO(current);
						btn.addEventListener("click", () => {
							setSelectedDate(btn.dataset.date);
						});
					}

					row.appendChild(btn);
				}

				calendarBody.appendChild(row);
			}

			if (initialSelectable) {
				setSelectedDate(initialSelectable);
			}
		};

		prevMonthBtn.addEventListener("click", () => {
			monthOffset -= 1;
			renderCalendar();
		});

		nextMonthBtn.addEventListener("click", () => {
			monthOffset += 1;
			renderCalendar();
		});

		renderCalendar();
	}
});

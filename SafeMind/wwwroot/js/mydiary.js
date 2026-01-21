document.addEventListener("DOMContentLoaded", () => {
	const weekRow = document.querySelector("[data-week-row]");
	const weekLabel = document.querySelector("[data-week-label]");
	const calendarBody = document.querySelector("[data-calendar-body]");
	const monthLabel = document.querySelector("[data-month-label]");
	const prevMonthBtn = document.querySelector("[data-cal-prev]");
	const nextMonthBtn = document.querySelector("[data-cal-next]");
	const chipGroups = document.querySelectorAll("[data-chip-group]");
	const saveBtn = document.getElementById("saveCheckinBtn");
	const noteInput = document.getElementById("diaryText");
	const checkinForm = document.getElementById("checkinForm");
	const promptTiles = document.querySelectorAll("[data-prompt]");
	const noteCounter = document.getElementById("diaryCount");

	const dayNames = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
	const today = new Date();
	const formatISO = (date) => date.toISOString().split("T")[0];
	const formatDom = (date) => date.toLocaleDateString(undefined, { day: "2-digit" });
	const formatLabel = (date) => date.toLocaleDateString(undefined, { month: "short", day: "numeric" });

	if (weekRow && weekLabel) {
		const startOfWeek = (() => {
			const d = new Date(today);
			const day = d.getDay();
			const offset = day === 0 ? -6 : 1 - day;
			d.setDate(d.getDate() + offset);
			d.setHours(0, 0, 0, 0);
			return d;
		})();

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
			const offset = startDay === 0 ? -6 : 1 - startDay;
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

	chipGroups.forEach((group) => {
		const applyActive = (btn) => {
			group.querySelectorAll(".chip").forEach((c) => {
				c.classList.remove("chip-active");
				c.setAttribute("aria-pressed", "false");
			});
			btn.classList.add("chip-active");
			btn.setAttribute("aria-pressed", "true");
			group.dataset.selected = btn.dataset.value;
		};

		const preselected = group.querySelector(".chip-active");
		if (preselected) {
			preselected.setAttribute("aria-pressed", "true");
			group.dataset.selected = preselected.dataset.value;
		}

		group.addEventListener("click", (event) => {
			const btn = event.target.closest(".chip");
			if (!btn || !group.contains(btn)) return;
			applyActive(btn);
		});
	});

	const getSelectedValue = (key) => {
		const group = document.querySelector(`[data-chip-group='${key}']`);
		return group ? group.dataset.selected : null;
	};

	const getAntiForgery = () => checkinForm?.querySelector("input[name='__RequestVerificationToken']")?.value;

	if (saveBtn) {
		saveBtn.addEventListener("click", async () => {
			const mood = getSelectedValue("mood");
			const energy = getSelectedValue("energy");
			const sleep = getSelectedValue("sleep");
			const stress = getSelectedValue("stress");
			const notes = noteInput?.value?.trim() ?? "";
			const token = getAntiForgery();

			if (!mood || !energy || !sleep || !stress) {
				alert("Please select mood, energy, sleep, and stress.");
				return;
			}

			if (!token) {
				alert("Unable to save right now. Please refresh the page.");
				return;
			}

			saveBtn.disabled = true;
			saveBtn.textContent = "Saving...";

			try {
				const resp = await fetch("/MyDiary/SaveCheck", {
					method: "POST",
					headers: {
						"Content-Type": "application/x-www-form-urlencoded;charset=UTF-8",
						"RequestVerificationToken": token
					},
					body: new URLSearchParams({ mood, energy, sleep, stress, notes }).toString()
				});

				if (resp.status === 409) {
					alert("You already checked in today.");
					return location.reload();
				}

				if (!resp.ok) throw new Error("Save failed");
				const data = await resp.json();
				if (!data?.success) throw new Error("Save failed");

				alert("Check-in saved.");
				location.reload();
			} catch (err) {
				console.error(err);
				alert("Could not save your check-in. Please try again.");
			} finally {
				saveBtn.disabled = false;
				saveBtn.textContent = "Save check-in";
			}
		});
	}

	if (noteInput && promptTiles.length > 0) {
		promptTiles.forEach((tile) => {
			tile.addEventListener("click", () => {
				noteInput.value = tile.dataset.prompt || "";
				noteInput.dispatchEvent(new Event("input"));
				noteInput.focus();
			});
		});
	}

	if (noteInput && noteCounter) {
		const max = parseInt(noteInput.getAttribute("maxLength"), 10) || 500;
		const updateCount = () => {
			noteCounter.textContent = `${noteInput.value.length}/${max}`;
		};
		noteInput.addEventListener("input", updateCount);
		updateCount();
	}
});

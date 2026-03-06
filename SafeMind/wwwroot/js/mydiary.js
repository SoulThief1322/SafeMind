// Initializes the diary calendar, mood chips, and check-in submission.
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
	// Formats a Date as an ISO yyyy-MM-dd string.
	const formatISO = (date) => date.toISOString().split("T")[0];
	// Formats a Date for day-of-month display.
	const formatDom = (date) => date.toLocaleDateString(undefined, { day: "2-digit" });
	// Formats a Date for month and day labels.
	const formatLabel = (date) => date.toLocaleDateString(undefined, { month: "short", day: "numeric" });

	if (weekRow && weekLabel) {
		// Determines Monday-based week start from today.
		const startOfWeek = (() => {
			const startDate = new Date(today);
			const weekDay = startDate.getDay();
			const offset = weekDay === 0 ? -6 : 1 - weekDay;
			startDate.setDate(startDate.getDate() + offset);
			startDate.setHours(0, 0, 0, 0);
			return startDate;
		})();

		const endOfWeek = new Date(startOfWeek);
		endOfWeek.setDate(startOfWeek.getDate() + 6);
		weekLabel.textContent = `This week · ${formatLabel(startOfWeek)} – ${formatLabel(endOfWeek)}`;

		weekRow.innerHTML = "";

		for (let dayIndex = 0; dayIndex < 7; dayIndex++) {
			const current = new Date(startOfWeek);
			current.setDate(startOfWeek.getDate() + dayIndex);

			const pill = document.createElement("button");
			pill.type = "button";
			pill.className = "day-pill";
			pill.setAttribute("role", "listitem");
			pill.dataset.date = formatISO(current);

			const dow = document.createElement("span");
			dow.className = "dow";
			dow.textContent = dayNames[dayIndex];

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
		let entryDateCache = {};

		// Fetches journal and check-in dates for a given month from the server.
		const fetchEntryDates = async (year, month) => {
			const key = `${year}-${month}`;
			if (entryDateCache[key]) return entryDateCache[key];

			try {
				const resp = await fetch(`/MyDiary/GetEntryDates?year=${year}&month=${month}`);
				if (!resp.ok) return { journalDates: [], checkDates: [] };
				const data = await resp.json();
				entryDateCache[key] = data;
				return data;
			} catch {
				return { journalDates: [], checkDates: [] };
			}
		};

		// Renders the month calendar grid and highlights dates with entries.
		const renderCalendar = async () => {
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

			const entryData = await fetchEntryDates(baseYear, baseMonth + 1);
			const journalSet = new Set(entryData.journalDates || []);
			const checkSet = new Set(entryData.checkDates || []);

			for (let weekIndex = 0; weekIndex < 6; weekIndex++) {
				const row = document.createElement("div");
				row.className = "mc-row";
				row.setAttribute("role", "row");

				for (let dayIndex = 0; dayIndex < 7; dayIndex++) {
					const current = new Date(gridStart);
					current.setDate(gridStart.getDate() + weekIndex * 7 + dayIndex);

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

					const dateStr = formatISO(current);
					const isToday = current.toDateString() === today.toDateString();
					const hasJournal = journalSet.has(dateStr);
					const hasCheck = checkSet.has(dateStr);

					if (isToday && isCurrentMonth && monthOffset === 0) {
						btn.classList.add("mc-today");
					}

					if (isCurrentMonth && hasJournal && hasCheck) {
						btn.classList.add("mc-has-both");
						btn.title = "Journal & Check-in";
					} else if (isCurrentMonth && hasJournal) {
						btn.classList.add("mc-has-journal");
						btn.title = "Journal entry";
					} else if (isCurrentMonth && hasCheck) {
						btn.classList.add("mc-has-check");
						btn.title = "Check-in";
					}

					row.appendChild(btn);
				}

				calendarBody.appendChild(row);
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
		// Marks a chip as selected within a group and stores its value.
		const applyActive = (btn) => {
			group.querySelectorAll(".chip").forEach((chipButton) => {
				chipButton.classList.remove("chip-active");
				chipButton.setAttribute("aria-pressed", "false");
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

	// Retrieves the selected chip value for a given key.
	const getSelectedValue = (key) => {
		const group = document.querySelector(`[data-chip-group='${key}']`);
		return group ? group.dataset.selected : null;
	};

	// Pulls the antiforgery token from the form.
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
			} catch (error) {
				console.error(error);
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
		const maxLength = parseInt(noteInput.getAttribute("maxLength"), 10) || 500;
		// Updates the live character counter for the diary note.
		const updateCount = () => {
			noteCounter.textContent = `${noteInput.value.length}/${maxLength}`;
		};
		noteInput.addEventListener("input", updateCount);
		updateCount();
	}
});

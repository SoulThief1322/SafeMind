// Sets up appointment date selection, slot picking, and handoff to checkout.
(() => {
    const weekGrid = document.getElementById('weekGrid');
    const weekTitle = document.getElementById('weekTitle');
    const prevWeek = document.getElementById('prevWeek');
    const nextWeek = document.getElementById('nextWeek');
    const selectedDateLabel = document.getElementById('selectedDateLabel');
    const sessionGrid = document.getElementById('sessionGrid');
    const sessionEmpty = document.getElementById('sessionEmpty');
    const continueContainer = document.getElementById('continueContainer');
    const hiddenInput = document.getElementById('selectedSlotsJson');
    const doctorId = document.querySelector('input[name="doctorId"]')?.value;

    if (!weekGrid || !selectedDateLabel || !doctorId) return;

    const selectedSlotsByDate = new Map();
    const availabilityCache = new Map(); // dateIso -> slots array
    let rebuildLock = false;
    const today = normalizeDate(new Date());
    const initialDate = parseLocalIso(selectedDateLabel.dataset?.date) || today;
    let currentSelectedDate = initialDate < today ? today : initialDate;
    let currentWeekStart = today;

    // Normalizes a date to midnight for consistent comparisons.
    function normalizeDate(dateValue) {
        const copy = new Date(dateValue);
        copy.setHours(0, 0, 0, 0);
        return copy;
    }

    // Parses a yyyy-MM-dd string into a Date in local time.
    function parseLocalIso(value) {
        if (!value) return null;
        const [year, month, dayOfMonth] = value.split('-').map(Number);
        const date = new Date(year, month - 1, dayOfMonth);
        return isNaN(date) ? null : normalizeDate(date);
    }

    // Converts a Date to yyyy-MM-dd string (UTC) and local date variant.
    function toIso(date) {
        return date.toISOString().slice(0, 10);
    }

    function toLocalIsoDate(dateValue = new Date()) {
        const date = new Date(dateValue);
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    // Gets the Monday-starting week anchor for a given date.
    function startOfWeek(date) {
        const startDate = normalizeDate(date);
        const dayIndex = (startDate.getDay() + 6) % 7; // Monday = 0
        startDate.setDate(startDate.getDate() - dayIndex);
        return startDate;
    }

    // Checks if two dates represent the same calendar day.
    function isSameDay(firstDate, secondDate) {
        return firstDate.getFullYear() === secondDate.getFullYear() &&
            firstDate.getMonth() === secondDate.getMonth() &&
            firstDate.getDate() === secondDate.getDate();
    }

    // Checks if two dates fall in the same week.
    function isSameWeek(firstDate, secondDate) {
        return isSameDay(startOfWeek(firstDate), startOfWeek(secondDate));
    }

    // Builds label parts for a day tile.
    function formatDay(date) {
        return {
            dow: date.toLocaleDateString(undefined, { weekday: 'short' }),
            label: date.getDate()
        };
    }

    // Formats the label text for the visible week range.
    function formatWeekTitle(weekStartDate) {
        const weekEndDate = new Date(weekStartDate);
        weekEndDate.setDate(weekEndDate.getDate() + 6);
        const range = `${weekStartDate.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} - ${weekEndDate.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}`;
        return isSameWeek(weekStartDate, today) ? 'This Week' : `Week of ${range}`;
    }

    // Updates the header label for the current week.
    function updateWeekTitle() {
        if (weekTitle) weekTitle.textContent = formatWeekTitle(currentWeekStart);
    }

    // Builds the week day pills and wires click behavior.
    function buildWeekGrid() {
        const weekSnapshot = new Date(currentWeekStart);
        weekGrid.innerHTML = '';
        updateWeekTitle();
        for (let dayOffset = 0; dayOffset < 7; dayOffset++) {
            const date = new Date(currentWeekStart);
            date.setDate(currentWeekStart.getDate() + dayOffset);
            const isoDate = toLocalIsoDate(date);
            const { dow, label } = formatDay(date);
            const isPast = date < today;
            const cached = availabilityCache.get(isoDate);
            const hasKnownSlots = Array.isArray(cached) && cached.length > 0;
            const isKnownEmpty = Array.isArray(cached) && cached.length === 0;
            const dayElement = document.createElement('div');
            dayElement.className = 'day';
            if (isPast) dayElement.classList.add('disabled');
            if (isKnownEmpty) dayElement.classList.add('disabled');
            if (isSameDay(date, currentSelectedDate)) dayElement.classList.add('selected');
            dayElement.dataset.date = isoDate;
            dayElement.innerHTML = `<span class="dow">${dow}</span><span class="date">${label}</span>`;
            if (!isPast && !isKnownEmpty) dayElement.addEventListener('click', (event) => {
                event.preventDefault();
                event.stopPropagation();
                currentSelectedDate = date;
                setSelectedDateLabel(date);
                loadSessions(isoDate);
                
                // Update selected state without rebuilding
                weekGrid.querySelectorAll('.day').forEach(el => el.classList.remove('selected'));
                dayElement.classList.add('selected');
            });
            weekGrid.appendChild(dayElement);
        }

        ensureWeekAvailability(weekSnapshot);
    }

    // Updates the visible date label for the session list.
    function setSelectedDateLabel(date) {
        selectedDateLabel.textContent = date.toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric' });
        selectedDateLabel.dataset.date = toLocalIsoDate(date);
    }

    // Serializes selected slots (and doctor) into the hidden input for form submission.
    function updateHiddenInput() {
        const slotList = [];
        selectedSlotsByDate.forEach((times, dateValue) => {
            times.forEach(time => slotList.push({ date: dateValue, time }));
        });

        const payload = {
            doctorId: Number(doctorId) || 0,
            slots: slotList
        };

        hiddenInput.value = JSON.stringify(payload);
    }

    // Shows/hides the continue CTA and refreshes the hidden payload.
    function updateContinueCta() {
        const total = Array.from(selectedSlotsByDate.values()).reduce((slotCount, slotSet) => slotCount + slotSet.size, 0);
        continueContainer.style.display = total > 0 ? 'flex' : 'none';
        updateHiddenInput();
    }

    // Toggles a session slot selection and updates state/UI.
    function toggleSelection(dateIso, time, sessionElement) {
        let slotSet = selectedSlotsByDate.get(dateIso);
        if (!slotSet) {
            slotSet = new Set();
            selectedSlotsByDate.set(dateIso, slotSet);
        }

        const limit = window._safeSlotLimit || 0; // 0 = unlimited
        const alreadySelected = sessionElement.classList.contains('selected');

        if (!alreadySelected && limit > 0) {
            // Count total selected across all dates
            const totalSelected = Array.from(selectedSlotsByDate.values())
                .reduce((sum, s) => sum + s.size, 0);
            if (totalSelected >= limit) {
                // Deselect all existing selections first
                selectedSlotsByDate.forEach((set, key) => set.clear());
                sessionGrid.querySelectorAll('.session.selected')
                    .forEach(el => el.classList.remove('selected'));
            }
        }

        const isSelected = sessionElement.classList.toggle('selected');
        if (isSelected) slotSet.add(time); else slotSet.delete(time);
        updateContinueCta();
    }

    const sessionDescriptor = document.querySelector('.sessions-head .text-muted')?.textContent?.trim() || 'Online / In-person';

    // Renders available sessions for a given date.
    function renderSessions(dateIso, slots) {
        sessionGrid.innerHTML = '';
        if (!slots || slots.length === 0) {
            sessionEmpty.style.display = 'block';
            return;
        }
        sessionEmpty.style.display = 'none';
        const selectedForDay = selectedSlotsByDate.get(dateIso) || new Set();
        slots.forEach(time => {
            const session = document.createElement('div');
            session.className = 'session';
            if (selectedForDay.has(time)) session.classList.add('selected');
            session.innerHTML = `
                <strong>${time}</strong>
                <small>${sessionDescriptor}</small>
                <span class="cta">Select</span>
            `;
            session.addEventListener('click', (event) => {
                event.preventDefault();
                event.stopPropagation();
                toggleSelection(dateIso, time, session);
            });
            sessionGrid.appendChild(session);
        });
    }

    function timeToMinutes(timeValue) {
        const [h, m] = timeValue.split(':').map(Number);
        return (h * 60) + (m || 0);
    }

    function currentLocalMinutes() {
        const now = new Date();
        return now.getHours() * 60 + now.getMinutes();
    }

    async function fetchSlots(dateIso) {
        try {
            const response = await fetch(`/Book/AvailableSessions?id=${doctorId}&date=${dateIso}`);
            if (!response.ok) return [];
            const data = await response.json();
            return Array.isArray(data.slots) ? data.slots : [];
        } catch {
            return [];
        }
    }

    function filterFutureSlots(dateIso, slots) {
        const todayIso = toLocalIsoDate();
        return dateIso === todayIso
            ? slots.filter(t => timeToMinutes(t) > currentLocalMinutes())
            : slots;
    }

    async function findNextAvailableDate(startDateIso, searchDays = 30) {
        let probeDate = parseLocalIso(startDateIso);
        if (!probeDate) return null;

        for (let dayOffset = 0; dayOffset < searchDays; dayOffset++) {
            if (dayOffset > 0) {
                probeDate.setDate(probeDate.getDate() + 1);
            }

            const probeIso = toLocalIsoDate(probeDate);
            const slots = filterFutureSlots(probeIso, await fetchSlots(probeIso));
            if (slots.length > 0) {
                return { date: new Date(probeDate), dateIso: probeIso, slots };
            }
        }
        return null;
    }

    // Loads sessions from the server for a date and renders them.
    async function loadSessions(dateIso, allowAdvance = false) {
        sessionGrid.innerHTML = '';
        sessionEmpty.style.display = 'none';

        const slots = filterFutureSlots(dateIso, await fetchSlots(dateIso));

        if (slots.length === 0 && allowAdvance) {
            const next = await findNextAvailableDate(dateIso);
            if (next) {
                currentSelectedDate = normalizeDate(next.date);
                currentWeekStart = startOfWeek(currentSelectedDate);
                setSelectedDateLabel(currentSelectedDate);
                buildWeekGrid();
                renderSessions(next.dateIso, next.slots);
                return;
            }
        }

        renderSessions(dateIso, slots);
    }

    async function ensureWeekAvailability(weekStartSnapshot) {
        const tasks = [];
        for (let dayOffset = 0; dayOffset < 7; dayOffset++) {
            const date = new Date(weekStartSnapshot);
            date.setDate(weekStartSnapshot.getDate() + dayOffset);
            if (date < today) continue;
            const isoDate = toLocalIsoDate(date);
            if (availabilityCache.has(isoDate)) continue;
            const fetchTask = fetchSlots(isoDate).then(slots => {
                const filtered = filterFutureSlots(isoDate, slots);
                availabilityCache.set(isoDate, filtered);
            });
            tasks.push(fetchTask);
        }

        if (tasks.length === 0) return;

        await Promise.all(tasks);

        if (isSameDay(weekStartSnapshot, currentWeekStart) && !rebuildLock) {
            rebuildLock = true;
            buildWeekGrid();
            rebuildLock = false;
        }
    }

    // Move the visible week without causing form submissions or page reloads.
    function shiftWeek(days) {
        currentWeekStart.setDate(currentWeekStart.getDate() + days);
        buildWeekGrid();
    }

    // Wires up existing session elements on initial load (first render).
    prevWeek?.addEventListener('click', (event) => {
        event.preventDefault();
        event.stopPropagation();
        shiftWeek(-7);
    });

    nextWeek?.addEventListener('click', (event) => {
        event.preventDefault();
        event.stopPropagation();
        shiftWeek(7);
    });

    setSelectedDateLabel(currentSelectedDate);
    buildWeekGrid();
    loadSessions(toLocalIsoDate(currentSelectedDate), true);
    updateContinueCta();
})();

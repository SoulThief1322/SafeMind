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
    const today = normalize(new Date());
    const initial = parseIso(selectedDateLabel.dataset?.date) || today;
    let currentSelectedDate = initial < today ? today : initial;
    let currentWeekStart = today;

    function normalize(d) {
        const copy = new Date(d);
        copy.setHours(0, 0, 0, 0);
        return copy;
    }

    function parseIso(value) {
        if (!value) return null;
        const [y, m, d] = value.split('-').map(Number);
        const date = new Date(Date.UTC(y, m - 1, d));
        return isNaN(date) ? null : normalize(date);
    }

    function toIso(date) {
        return date.toISOString().slice(0, 10);
    }

    function startOfWeek(date) {
        const d = normalize(date);
        const day = (d.getDay() + 6) % 7; // Monday = 0
        d.setDate(d.getDate() - day);
        return d;
    }

    function isSameDay(a, b) {
        return a.getFullYear() === b.getFullYear() &&
            a.getMonth() === b.getMonth() &&
            a.getDate() === b.getDate();
    }

    function isSameWeek(a, b) {
        return isSameDay(startOfWeek(a), startOfWeek(b));
    }

    function formatDay(date) {
        return {
            dow: date.toLocaleDateString(undefined, { weekday: 'short' }),
            label: date.getDate()
        };
    }

    function formatWeekTitle(start) {
        const end = new Date(start);
        end.setDate(end.getDate() + 6);
        const range = `${start.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} - ${end.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}`;
        return isSameWeek(start, today) ? 'This Week' : `Week of ${range}`;
    }

    function updateWeekTitle() {
        if (weekTitle) weekTitle.textContent = formatWeekTitle(currentWeekStart);
    }

    function buildWeekGrid() {
        weekGrid.innerHTML = '';
        updateWeekTitle();
        for (let i = 0; i < 7; i++) {
            const date = new Date(currentWeekStart);
            date.setDate(currentWeekStart.getDate() + i);
            const iso = toIso(date);
            const { dow, label } = formatDay(date);
            const isPast = date < today;
            const dayEl = document.createElement('div');
            dayEl.className = 'day';
            if (isPast) dayEl.classList.add('disabled');
            if (isSameDay(date, currentSelectedDate)) dayEl.classList.add('selected');
            dayEl.dataset.date = iso;
            dayEl.innerHTML = `<span class="dow">${dow}</span><span class="date">${label}</span>`;
            dayEl.addEventListener('click', () => {
                if (isPast) return;
                currentSelectedDate = date;
                setSelectedDateLabel(date);
                loadSessions(iso);
                buildWeekGrid();
            });
            weekGrid.appendChild(dayEl);
        }

        // Keep the selected day visible in the center when scrolling horizontally
        const selectedEl = weekGrid.querySelector('.day.selected');
        selectedEl?.scrollIntoView({ behavior: 'instant', block: 'nearest', inline: 'center' });
    }

    function setSelectedDateLabel(date) {
        selectedDateLabel.textContent = date.toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric' });
        selectedDateLabel.dataset.date = toIso(date);
    }

    function updateHiddenInput() {
        const payload = [];
        selectedSlotsByDate.forEach((times, date) => {
            times.forEach(time => payload.push({ date, time }));
        });
        hiddenInput.value = JSON.stringify(payload);
    }

    function updateContinueCta() {
        const total = Array.from(selectedSlotsByDate.values()).reduce((sum, set) => sum + set.size, 0);
        continueContainer.style.display = total > 0 ? 'flex' : 'none';
        updateHiddenInput();
    }

    function toggleSelection(dateIso, time, sessionEl) {
        let set = selectedSlotsByDate.get(dateIso);
        if (!set) {
            set = new Set();
            selectedSlotsByDate.set(dateIso, set);
        }
        const isSelected = sessionEl.classList.toggle('selected');
        if (isSelected) set.add(time); else set.delete(time);
        updateContinueCta();
    }

    const sessionDescriptor = document.querySelector('.sessions-head .text-muted')?.textContent?.trim() || 'Online / In-person';

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
            session.addEventListener('click', () => toggleSelection(dateIso, time, session));
            sessionGrid.appendChild(session);
        });
    }

    function loadSessions(dateIso) {
        sessionGrid.innerHTML = '';
        sessionEmpty.style.display = 'none';
        fetch(`/Book/AvailableSessions?id=${doctorId}&date=${dateIso}`)
            .then(r => r.ok ? r.json() : Promise.reject())
            .then(data => renderSessions(dateIso, data.slots))
            .catch(() => renderSessions(dateIso, []));
    }

    function attachInitialSessions() {
        const dateIso = selectedDateLabel.dataset.date || toIso(today);
        const sessions = sessionGrid.querySelectorAll('.session');
        sessions.forEach(session => {
            const time = session.querySelector('strong')?.textContent?.trim();
            if (!time) return;
            session.addEventListener('click', () => toggleSelection(dateIso, time, session));
        });
    }

    prevWeek?.addEventListener('click', () => {
        currentWeekStart.setDate(currentWeekStart.getDate() - 7);
        buildWeekGrid();
    });

    nextWeek?.addEventListener('click', () => {
        currentWeekStart.setDate(currentWeekStart.getDate() + 7);
        buildWeekGrid();
    });

    setSelectedDateLabel(currentSelectedDate);
    buildWeekGrid();
    loadSessions(toIso(currentSelectedDate));
    attachInitialSessions();
    updateContinueCta();
})();

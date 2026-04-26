// Book-session: month calendar + time-slot picker
(() => {
    const calGrid          = document.getElementById('calGrid');
    const monthTitle       = document.getElementById('monthTitle');
    const prevMonthBtn     = document.getElementById('prevMonth');
    const nextMonthBtn     = document.getElementById('nextMonth');
    const selectedDayName  = document.getElementById('selectedDayName');
    const sessionGrid      = document.getElementById('sessionGrid');
    const sessionEmpty     = document.getElementById('sessionEmpty');
    const continueContainer= document.getElementById('continueContainer');
    const hiddenInput      = document.getElementById('selectedSlotsJson');
    const selectedDateLabel= document.getElementById('selectedDateLabel');
    const fmt12Btn         = document.getElementById('fmt12');
    const fmt24Btn         = document.getElementById('fmt24');
    const doctorId         = document.querySelector('input[name="doctorId"]')?.value;
    const stickyBar        = document.getElementById('stickyBar');
    const stickyLabel      = document.getElementById('stickyLabel');
    const stickyBtn        = document.getElementById('stickyBtn');

    if (!calGrid || !doctorId) return;

    let use12h = true;
    const selectedSlotsByDate = new Map();
    const availabilityCache   = new Map();

    // ── Sticky bar visibility ──────────────────────────────────
    let continueInView = true;

    const continueObserver = new IntersectionObserver(entries => {
        continueInView = entries[0].isIntersecting;
        syncStickyBar();
    }, { threshold: 0.1 });

    if (continueContainer) continueObserver.observe(continueContainer);

    stickyBtn?.addEventListener('click', () => {
        continueContainer?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });

    function syncStickyBar() {
        const total = Array.from(selectedSlotsByDate.values()).reduce((s, v) => s + v.size, 0);
        const show  = total > 0 && !continueInView;
        if (stickyBar) stickyBar.style.display = show ? 'flex' : 'none';
        if (stickyLabel) {
            stickyLabel.textContent = `${total} session${total !== 1 ? 's' : ''} selected`;
        }
    }

    const today = normalizeDate(new Date());
    const initialDate = parseLocalIso(selectedDateLabel?.dataset?.date) || today;
    let currentSelectedDate = initialDate < today ? today : initialDate;
    let viewYear  = currentSelectedDate.getFullYear();
    let viewMonth = currentSelectedDate.getMonth();

    // ── Helpers ────────────────────────────────────────────────

    function normalizeDate(d) {
        const c = new Date(d);
        c.setHours(0, 0, 0, 0);
        return c;
    }

    function parseLocalIso(val) {
        if (!val) return null;
        const [y, m, d] = val.split('-').map(Number);
        const date = new Date(y, m - 1, d);
        return isNaN(date.getTime()) ? null : normalizeDate(date);
    }

    function toLocalIsoDate(d = new Date()) {
        const year  = d.getFullYear();
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const day   = String(d.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    function isSameDay(a, b) {
        return a.getFullYear() === b.getFullYear() &&
               a.getMonth()    === b.getMonth()    &&
               a.getDate()     === b.getDate();
    }

    function formatDayName(date) {
        return date.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric' });
    }

    function to12h(time24) {
        const [h, m] = time24.split(':').map(Number);
        const period = h < 12 ? 'am' : 'pm';
        const hour   = h === 0 ? 12 : h > 12 ? h - 12 : h;
        return `${hour}:${String(m).padStart(2, '0')}${period}`;
    }

    function formatTime(time24) {
        return use12h ? to12h(time24) : time24;
    }

    // ── Calendar ───────────────────────────────────────────────

    function buildCalendar() {
        calGrid.innerHTML = '';

        const firstOfMonth    = new Date(viewYear, viewMonth, 1);
        const daysInMonth     = new Date(viewYear, viewMonth + 1, 0).getDate();
        const firstDow        = (firstOfMonth.getDay() + 6) % 7; // Monday = 0
        const todayFirstOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);

        monthTitle.textContent = firstOfMonth.toLocaleDateString(undefined, {
            month: 'long', year: 'numeric'
        });

        prevMonthBtn.disabled = firstOfMonth <= todayFirstOfMonth;

        // Leading empty cells
        for (let i = 0; i < firstDow; i++) {
            const empty = document.createElement('div');
            empty.className = 'bs-cal-day bs-cal-empty';
            calGrid.appendChild(empty);
        }

        for (let d = 1; d <= daysInMonth; d++) {
            const date   = new Date(viewYear, viewMonth, d);
            const iso    = toLocalIsoDate(date);
            const isPast = date < today;
            const isToday    = isSameDay(date, today);
            const isSelected = isSameDay(date, currentSelectedDate);
            const cached   = availabilityCache.get(iso);
            const isEmpty  = Array.isArray(cached) && cached.length === 0;

            const el = document.createElement('div');
            el.className = 'bs-cal-day';
            if (isPast || isEmpty) el.classList.add('bs-cal-past');
            if (isToday)    el.classList.add('bs-cal-today');
            if (isSelected) el.classList.add('bs-cal-selected');
            el.textContent = d;
            el.dataset.date = iso;

            if (!isPast && !isEmpty) {
                el.addEventListener('click', () => {
                    currentSelectedDate = normalizeDate(date);
                    selectedDayName.textContent = formatDayName(currentSelectedDate);
                    buildCalendar();
                    loadSessions(iso);
                });
            }

            calGrid.appendChild(el);
        }

        prefetchMonth();
    }

    async function prefetchMonth() {
        const daysInMonth = new Date(viewYear, viewMonth + 1, 0).getDate();
        const tasks = [];

        for (let d = 1; d <= daysInMonth; d++) {
            const date = new Date(viewYear, viewMonth, d);
            if (date < today) continue;
            const iso = toLocalIsoDate(date);
            if (availabilityCache.has(iso)) continue;

            const task = fetchSlots(iso).then(slots => {
                availabilityCache.set(iso, filterFutureSlots(iso, slots));
            });
            tasks.push(task);
        }

        if (tasks.length === 0) return;
        await Promise.all(tasks);
        buildCalendar();
    }

    // ── Slots ──────────────────────────────────────────────────

    async function fetchSlots(iso) {
        try {
            const res  = await fetch(`/Book/AvailableSessions?id=${doctorId}&date=${iso}`);
            if (!res.ok) return [];
            const data = await res.json();
            return Array.isArray(data.slots) ? data.slots : [];
        } catch { return []; }
    }

    function timeToMinutes(t) {
        const [h, m] = t.split(':').map(Number);
        return h * 60 + (m || 0);
    }

    function currentLocalMinutes() {
        const now = new Date();
        return now.getHours() * 60 + now.getMinutes();
    }

    function filterFutureSlots(iso, slots) {
        return iso === toLocalIsoDate()
            ? slots.filter(t => timeToMinutes(t) > currentLocalMinutes())
            : slots;
    }

    function renderSessions(iso, slots) {
        sessionGrid.innerHTML = '';

        if (!slots || slots.length === 0) {
            sessionEmpty.style.display = 'block';
            return;
        }

        sessionEmpty.style.display = 'none';
        const selectedForDay = selectedSlotsByDate.get(iso) || new Set();

        slots.forEach(time => {
            const el = document.createElement('button');
            el.type = 'button';
            el.className = 'bs-slot';
            if (selectedForDay.has(time)) el.classList.add('bs-slot-selected');
            el.textContent  = formatTime(time);
            el.dataset.time = time;
            el.addEventListener('click', () => toggleSlot(iso, time, el));
            sessionGrid.appendChild(el);
        });
    }

    function rerenderSlotLabels() {
        sessionGrid.querySelectorAll('.bs-slot').forEach(el => {
            el.textContent = formatTime(el.dataset.time);
        });
    }

    function toggleSlot(iso, time, el) {
        let set = selectedSlotsByDate.get(iso);
        if (!set) { set = new Set(); selectedSlotsByDate.set(iso, set); }

        const limit = window._safeSlotLimit || 0;
        const alreadySelected = el.classList.contains('bs-slot-selected');

        if (!alreadySelected && limit > 0) {
            const total = Array.from(selectedSlotsByDate.values()).reduce((s, v) => s + v.size, 0);
            if (total >= limit) {
                selectedSlotsByDate.forEach(s => s.clear());
                sessionGrid.querySelectorAll('.bs-slot-selected').forEach(e => e.classList.remove('bs-slot-selected'));
            }
        }

        if (el.classList.toggle('bs-slot-selected')) {
            set.add(time);
        } else {
            set.delete(time);
        }

        updateContinue();
    }

    function updateHidden() {
        const slots = [];
        selectedSlotsByDate.forEach((times, date) => {
            times.forEach(time => slots.push({ date, time }));
        });
        hiddenInput.value = JSON.stringify({ doctorId: Number(doctorId), slots });
    }

    function updateContinue() {
        const total = Array.from(selectedSlotsByDate.values()).reduce((s, v) => s + v.size, 0);
        continueContainer.style.display = total > 0 ? 'flex' : 'none';
        updateHidden();
        syncStickyBar();
    }

    async function loadSessions(iso) {
        sessionGrid.innerHTML = '';
        sessionEmpty.style.display = 'none';

        const slots = filterFutureSlots(iso, await fetchSlots(iso));
        availabilityCache.set(iso, slots);
        renderSessions(iso, slots);
        buildCalendar();
    }

    // ── Format toggle ──────────────────────────────────────────

    fmt12Btn?.addEventListener('click', () => {
        use12h = true;
        fmt12Btn.classList.add('bs-fmt-active');
        fmt24Btn.classList.remove('bs-fmt-active');
        rerenderSlotLabels();
    });

    fmt24Btn?.addEventListener('click', () => {
        use12h = false;
        fmt24Btn.classList.add('bs-fmt-active');
        fmt12Btn.classList.remove('bs-fmt-active');
        rerenderSlotLabels();
    });

    // ── Month navigation ───────────────────────────────────────

    prevMonthBtn?.addEventListener('click', e => {
        e.preventDefault();
        viewMonth--;
        if (viewMonth < 0) { viewMonth = 11; viewYear--; }
        buildCalendar();
    });

    nextMonthBtn?.addEventListener('click', e => {
        e.preventDefault();
        viewMonth++;
        if (viewMonth > 11) { viewMonth = 0; viewYear++; }
        buildCalendar();
    });

    // ── Init ───────────────────────────────────────────────────

    selectedDayName.textContent = formatDayName(currentSelectedDate);
    buildCalendar();
    loadSessions(toLocalIsoDate(currentSelectedDate));
    updateContinue();
})();

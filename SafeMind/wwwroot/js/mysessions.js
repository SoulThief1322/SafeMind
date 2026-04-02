(function () {
    const tabs = document.getElementById('sessionTabs');
    if (!tabs) return;

    const panels = document.querySelectorAll('[data-panel]');

    tabs.addEventListener('click', (e) => {
        const btn = e.target.closest('[data-target]');
        if (!btn) return;

        const target = btn.getAttribute('data-target');
        tabs.querySelectorAll('.pill').forEach(p => p.classList.remove('active'));
        btn.classList.add('active');

        panels.forEach(panel => {
            panel.style.display = panel.getAttribute('data-panel') === target ? 'block' : 'none';
        });
    });
})();

// ── Star rating interaction ────────────────────────────────────────────────
(function () {
    document.addEventListener('click', function (e) {
        const btn = e.target.closest('.star-btn');
        if (!btn) return;

        const form = btn.closest('.star-form');
        if (!form) return;

        const value = parseInt(btn.getAttribute('data-value'), 10);
        const starsInput = form.querySelector('.stars-value');
        const submitBtn = form.querySelector('.rating-submit');
        const allBtns = form.querySelectorAll('.star-btn');

        starsInput.value = value;
        submitBtn.disabled = false;

        allBtns.forEach(b => {
            const v = parseInt(b.getAttribute('data-value'), 10);
            b.classList.toggle('selected', v <= value);
            b.classList.remove('hovered');
        });
    });

    document.addEventListener('mouseover', function (e) {
        const btn = e.target.closest('.star-btn');
        if (!btn) return;
        const form = btn.closest('.star-form');
        if (!form) return;
        const hoverVal = parseInt(btn.getAttribute('data-value'), 10);
        form.querySelectorAll('.star-btn').forEach(b => {
            const v = parseInt(b.getAttribute('data-value'), 10);
            b.classList.toggle('hovered', v <= hoverVal);
        });
    });

    document.addEventListener('mouseout', function (e) {
        const btn = e.target.closest('.star-btn');
        if (!btn) return;
        const form = btn.closest('.star-form');
        if (!form) return;
        form.querySelectorAll('.star-btn').forEach(b => b.classList.remove('hovered'));
    });

    document.addEventListener('submit', async function (e) {
        const form = e.target.closest('.star-form');
        if (!form) return;
        e.preventDefault();

        const starsInput = form.querySelector('.stars-value');
        const stars = parseInt(starsInput.value, 10);
        if (!stars || stars < 1 || stars > 5) return;

        const sessionId = parseInt(form.getAttribute('data-session-id'), 10);
        const token = form.querySelector('input[name="__RequestVerificationToken"]').value;

        const submit = form.querySelector('.rating-submit');
        submit.disabled = true;
        submit.textContent = 'Saving…';

        try {
            const body = new URLSearchParams({
                sessionId,
                stars,
                __RequestVerificationToken: token
            });
            const resp = await fetch(form.action, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: body.toString()
            });
            const data = await resp.json();

            if (data.success) {
                // Replace the form with a readonly display
                const row = form.closest('.rating-row');
                if (row) {
                    const filled = Array.from({ length: 5 }, (_, i) =>
                        `<span class="star ${i < stars ? 'star-filled' : 'star-empty'}">&#9733;</span>`
                    ).join('');
                    row.innerHTML = `<div class="stars-display" title="Your rating: ${stars}/5">${filled}</div><span class="rating-label">Your rating</span>`;
                }
            } else {
                submit.disabled = false;
                submit.textContent = submit.textContent.replace('Saving…', 'Rate Dr.');
                alert(data.error || 'Could not save rating. Please try again.');
            }
        } catch {
            submit.disabled = false;
            submit.textContent = 'Rate';
        }
    });
})();

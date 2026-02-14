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

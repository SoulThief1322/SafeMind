(() => {
    const form = document.getElementById('searchForm');
    const specialtySelect = document.getElementById('specialtySelect');

    document.querySelectorAll('.chip, .popular-chip').forEach(btn => {
        btn.addEventListener('click', () => btn.classList.toggle('active'));
    });

    document.querySelectorAll('.book-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            btn.classList.add('active');
            setTimeout(() => btn.classList.remove('active'), 300);
        });
    });

    document.querySelectorAll('.popular-chip').forEach(btn => {
        btn.addEventListener('click', () => {
            const target = btn.getAttribute('data-specialty');
            if (!target || !specialtySelect) return;
            const match = Array.from(specialtySelect.options)
                .find(opt => opt.text.localeCompare(target, undefined, { sensitivity: 'accent' }) === 0);
            if (!match) return;
            match.selected = true;
            form?.submit();
        });
    });
})();

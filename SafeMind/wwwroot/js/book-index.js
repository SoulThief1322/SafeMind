// Initializes the doctor search filters and chips on the book index page.
(() => {
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
            const targetSpecialty = btn.getAttribute('data-specialty');
            if (!targetSpecialty || !specialtySelect) return;
            const matchingOption = Array.from(specialtySelect.options)
                .find(option => option.text.localeCompare(targetSpecialty, undefined, { sensitivity: 'accent' }) === 0);
            if (!matchingOption) return;
            matchingOption.selected = true;
            const searchForm = document.getElementById('searchForm');
            searchForm?.submit();
        });
    });
})();

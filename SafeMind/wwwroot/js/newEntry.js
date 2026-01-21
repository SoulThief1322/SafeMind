document.addEventListener("DOMContentLoaded", () => {
			const setHidden = (key, value) => {
				const hidden = document.getElementById(key.charAt(0).toUpperCase() + key.slice(1));
				if (hidden) hidden.value = value;
			};

			document.querySelectorAll('[data-chip-group]').forEach(group => {
				const type = group.dataset.chipGroup;
				group.addEventListener('click', (e) => {
					const btn = e.target.closest('.chip');
					if (!btn) return;
					group.querySelectorAll('.chip').forEach(c => c.classList.remove('chip-active'));
					btn.classList.add('chip-active');
					setHidden(type, btn.dataset.value);
				});
			});
		});
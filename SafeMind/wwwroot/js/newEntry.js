// Sets hidden form values based on chip selection for new diary entries.
document.addEventListener("DOMContentLoaded", () => {
			// Updates the hidden input that pairs with a chip group selection.
			const setHidden = (key, value) => {
				const hidden = document.getElementById(key.charAt(0).toUpperCase() + key.slice(1));
				if (hidden) hidden.value = value;
			};

			document.querySelectorAll('[data-chip-group]').forEach(group => {
				const type = group.dataset.chipGroup;
				group.addEventListener('click', (event) => {
					const chipButton = event.target.closest('.chip');
					if (!chipButton) return;
					group.querySelectorAll('.chip').forEach(chip => chip.classList.remove('chip-active'));
					chipButton.classList.add('chip-active');
					setHidden(type, chipButton.dataset.value);
				});
			});
		});
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

			// Client-side validation before form submit
			const form = document.querySelector('form.entry-stack');
			if (form) {
				const titleInput = document.getElementById('Title');
				const contentInput = document.getElementById('Content');

				const showError = (input, message) => {
					let err = input.nextElementSibling;
					if (!err || !err.classList.contains('entry-field-error')) {
						err = document.createElement('span');
						err.className = 'entry-field-error text-danger';
						err.style.cssText = 'display:block;font-size:0.85em;margin-top:4px;';
						input.insertAdjacentElement('afterend', err);
					}
					err.textContent = message;
					input.setAttribute('aria-invalid', 'true');
				};

				const clearError = (input) => {
					const err = input.nextElementSibling;
					if (err && err.classList.contains('entry-field-error')) err.textContent = '';
					input.removeAttribute('aria-invalid');
				};

				[titleInput, contentInput].forEach(input => {
					if (input) input.addEventListener('input', () => {
						if (input.value.trim()) clearError(input);
					});
				});

				form.addEventListener('submit', (e) => {
					let valid = true;
					if (!titleInput || !titleInput.value.trim()) {
						showError(titleInput, 'Title is required.');
						valid = false;
					}
					if (!contentInput || !contentInput.value.trim()) {
						showError(contentInput, 'Content is required.');
						valid = false;
					}
					if (!valid) {
						e.preventDefault();
						const firstInvalid = form.querySelector('[aria-invalid]');
						if (firstInvalid) firstInvalid.focus();
					}
				});
			}
		});
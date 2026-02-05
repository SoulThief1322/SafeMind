// Handles navbar toggle for mobile view when present.
const toggler = document.querySelector('.navbar-toggler');
const navLinks = document.querySelector('.nav-links');

if (toggler && navLinks) {
    toggler.addEventListener('click', () => {
        navLinks.classList.toggle('active');
    });
}
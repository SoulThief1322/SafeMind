const toggler = document.querySelector('.navbar-toggler');
const navLinks = document.querySelector('.nav-links');

toggler.addEventListener('click', () => {
    navLinks.classList.toggle('active');
});
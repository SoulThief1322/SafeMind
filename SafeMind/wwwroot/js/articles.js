document.addEventListener("DOMContentLoaded", () => {
  const carousel = document.querySelector(".carousel-editorial");
  if (!carousel) return;

  const track = carousel.querySelector(".carousel-track");
  const slides = [...carousel.querySelectorAll(".carousel-slide")];
  const prevBtn = carousel.querySelector(".carousel-btn.prev");
  const nextBtn = carousel.querySelector(".carousel-btn.next");

  let index = 0;

  track.addEventListener("click", () => {
    window.location.href = "http://localhost:5019/Articles/#";
  });

  function updateCarousel() {
    slides.forEach((slide, i) => {
      slide.classList.remove("is-active", "is-prev", "is-next");

      if (i === index) slide.classList.add("is-active");
      if (i === index - 1) slide.classList.add("is-prev");
      if (i === index + 1) slide.classList.add("is-next");
    });

    const active = slides[index];
    const offset =
      active.offsetLeft -
      track.clientWidth / 2 +
      active.clientWidth / 2;

    track.scrollTo({
      left: offset,
      behavior: "smooth"
    });
  }

  nextBtn.addEventListener("click", () => {
    index = (index + 1) % slides.length;
    updateCarousel();
  });

  prevBtn.addEventListener("click", () => {
    index = (index - 1 + slides.length) % slides.length;
    updateCarousel();
  });

  updateCarousel();
});

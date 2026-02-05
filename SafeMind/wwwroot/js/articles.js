document.addEventListener("DOMContentLoaded", () => {
  // Rotates the hero banner content automatically and on user selection.
  const hero = document.querySelector("[data-hero-rotator]");
  if (!hero) return;

  const heroBg = hero.querySelector(".hero-bg");
  const eyebrowEl = hero.querySelector("[data-hero-eyebrow]");
  const titleEl = hero.querySelector("[data-hero-title]");
  const ctaEl = hero.querySelector("[data-hero-cta]");
  const summaryEl = hero.querySelector("[data-hero-summary]");
  const categoryEl = hero.querySelector("[data-hero-category]");
  const linkEl = hero.querySelector("[data-hero-link]");
  const dotsEl = hero.querySelector("[data-hero-dots]");
  const sideLinks = [...hero.querySelectorAll(".side-link")];

  if (!heroBg || !eyebrowEl || !titleEl || !ctaEl || !summaryEl || !categoryEl || !linkEl || !dotsEl) {
    return;
  }

  const heroArticles = [
    {
      topic: "Mind",
      category: "Mindfulness",
      eyebrow: "Expansive Exploration",
      title: "Quick Breathing Reset",
      summary: "30-second calm routine to reset before your day.",
      cta: "Read article",
      link: "#mind",
      background: "/images/feat1.jpg"
    },
    {
      topic: "Wellness",
      category: "Wellness",
      eyebrow: "Tiny Steps",
      title: "Tiny Steps, Big Calm",
      summary: "Stack small habits for steady progress.",
      cta: "Try the steps",
      link: "#wellness",
      background: "/images/feat3.jpg"
    },
    {
      topic: "Sleep",
      category: "Sleep",
      eyebrow: "Nightly Rituals",
      title: "Better Night Routines",
      summary: "Wind-down cues to help your body rest.",
      cta: "Build your routine",
      link: "#sleep",
      background: "/images/feat2.jpg"
    },
    {
      topic: "Therapy",
      category: "Therapy",
      eyebrow: "Therapist Notes",
      title: "What Therapists Wish You Knew",
      summary: "Prep for your first session with less nerves.",
      cta: "See prep list",
      link: "#therapy",
      background: "/images/a1.jpg"
    },
    {
      topic: "Insights",
      category: "Insights",
      eyebrow: "Data-backed Check-ins",
      title: "Your Weekly Grounding",
      summary: "Micro-reflections to keep you steady all week.",
      cta: "Start check-in",
      link: "#insights",
      background: "/images/a2.jpg"
    }
  ];

  let index = 0;
  let timerId;
  let isSwapping = false;

  // Builds the pagination dots and wires them to swap the hero slide.
  const createDots = () => {
    dotsEl.innerHTML = "";
    heroArticles.forEach((article, articleIndex) => {
      const dot = document.createElement("button");
      dot.type = "button";
      dot.className = "hero-dot";
      dot.setAttribute("role", "tab");
      dot.setAttribute("aria-label", `Show ${article.title}`);
      dot.addEventListener("click", () => setIndex(articleIndex, true));
      dotsEl.appendChild(dot);
    });
  };

  // Restarts the auto-advance timer for the hero rotator.
  const restartTimer = () => {
    clearInterval(timerId);
    timerId = window.setInterval(() => setIndex(index + 1), 7000);
  };

  // Applies the active article data to the hero UI.
  const applyHero = () => {
    const item = heroArticles[index];
    if (!item) return;

    heroBg.style.backgroundImage = `url('${item.background}')`;
    eyebrowEl.textContent = item.eyebrow;
    titleEl.innerHTML = item.title;
    summaryEl.textContent = item.summary;
    categoryEl.textContent = item.category;
    ctaEl.textContent = item.cta;
    ctaEl.href = item.link;
    linkEl.textContent = "Read more";
    linkEl.href = item.link;

    sideLinks.forEach((link) => {
      const { heroTopic } = link.dataset;
      link.classList.toggle("active", heroTopic === item.topic);
    });

    [...dotsEl.children].forEach((dot, dotIndex) => {
      const isActive = dotIndex === index;
      dot.classList.toggle("is-active", isActive);
      dot.setAttribute("aria-selected", isActive ? "true" : "false");
      dot.tabIndex = isActive ? 0 : -1;
    });
  };

  // Swaps to a specific hero slide, optionally triggered by user interaction.
  const setIndex = (nextIndex, isManual = false) => {
    if (isSwapping) return;
    const target = (nextIndex + heroArticles.length) % heroArticles.length;
    if (target === index && !isManual) return;

    isSwapping = true;
    hero.classList.add("is-fading");

    window.setTimeout(() => {
      index = target;
      applyHero();
      hero.classList.remove("is-fading");
      hero.classList.add("is-entering");

      window.setTimeout(() => {
        hero.classList.remove("is-entering");
        isSwapping = false;
      }, 380);
    }, 220);

    if (isManual) {
      restartTimer();
    }
  };

  sideLinks.forEach((link) => {
    link.addEventListener("click", (event) => {
      event.preventDefault();
      const { heroTopic } = link.dataset;
      const targetIndex = heroArticles.findIndex(
        (article) => article.topic === heroTopic
      );

      if (targetIndex >= 0) {
        setIndex(targetIndex, true);
      }
    });
  });

  createDots();
  applyHero();
  restartTimer();
});

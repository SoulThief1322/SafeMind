document.addEventListener("DOMContentLoaded", () => {
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

  const heroArticles = (window.__heroArticles || []).map(a => ({
    topic: a.headline,
    category: a.categories && a.categories.length > 0 ? a.categories[0] : "",
    eyebrow: a.categories && a.categories.length > 0 ? a.categories[0] : "Article",
    title: a.headline,
    summary: a.content,
    cta: "Read article",
    link: `/Articles/SelectedArticle/${a.id}`
  }));

  if (heroArticles.length === 0) return;

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

  // ── Category filtering + client-side pagination ──
  const categoryGrid = document.querySelector("[data-category-grid]");
  const articlesList = document.querySelector("[data-articles-list]");

  if (categoryGrid && articlesList) {
    const tiles = [...categoryGrid.querySelectorAll("[data-category-filter]")];
    const cards = [...articlesList.querySelectorAll("[data-categories]")];
    const paginationNav = document.getElementById("articlesPagination");
    const pageSize = 10;
    let currentFilter = "all";
    let currentPage = 1;

    // Auto-select category from ?category= query param
    const urlCategory = new URLSearchParams(window.location.search).get("category");
    if (urlCategory) {
      const match = tiles.find(t => t.dataset.categoryFilter === urlCategory);
      if (match) {
        currentFilter = urlCategory;
        tiles.forEach(t => t.classList.remove("is-active"));
        match.classList.add("is-active");
      }
    }

    function getVisibleCards() {
      return cards.filter(card => {
        if (currentFilter === "all") return true;
        const cats = card.dataset.categories.split(",").map(c => c.trim()).filter(Boolean);
        return cats.includes(currentFilter);
      });
    }

    function render() {
      const visible = getVisibleCards();
      const totalPages = Math.max(1, Math.ceil(visible.length / pageSize));
      if (currentPage > totalPages) currentPage = totalPages;

      const start = (currentPage - 1) * pageSize;
      const end = start + pageSize;
      const pageSet = new Set(visible.slice(start, end));

      cards.forEach(card => {
        card.style.display = pageSet.has(card) ? "" : "none";
      });

      renderPagination(totalPages);
    }

    function renderPagination(totalPages) {
      if (!paginationNav) return;
      paginationNav.innerHTML = "";
      if (totalPages <= 1) return;

      if (currentPage > 1) {
        const prev = document.createElement("a");
        prev.className = "page-link";
        prev.href = "#";
        prev.innerHTML = "&laquo; Prev";
        prev.addEventListener("click", e => { e.preventDefault(); currentPage--; render(); });
        paginationNav.appendChild(prev);
      }

      for (let i = 1; i <= totalPages; i++) {
        const link = document.createElement("a");
        link.className = "page-link" + (i === currentPage ? " active" : "");
        link.href = "#";
        link.textContent = i;
        link.addEventListener("click", e => { e.preventDefault(); currentPage = i; render(); });
        paginationNav.appendChild(link);
      }

      if (currentPage < totalPages) {
        const next = document.createElement("a");
        next.className = "page-link";
        next.href = "#";
        next.innerHTML = "Next &raquo;";
        next.addEventListener("click", e => { e.preventDefault(); currentPage++; render(); });
        paginationNav.appendChild(next);
      }
    }

    tiles.forEach(tile => {
      tile.addEventListener("click", e => {
        e.preventDefault();
        currentFilter = tile.dataset.categoryFilter;
        currentPage = 1;
        tiles.forEach(t => t.classList.remove("is-active"));
        tile.classList.add("is-active");
        render();
      });
    });

    render();
  }
});

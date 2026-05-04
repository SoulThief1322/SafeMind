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

  // ── Daily Insight widget ──────────────────────────────────────
  const insightWidget  = document.querySelector('.block-insight');
  const insightTextEl  = document.getElementById('insightText');
  const insightTopicEl = document.getElementById('insightTopic');
  const insightDayEl   = document.getElementById('insightDay');
  const insightDotsEl  = document.getElementById('insightDots');
  const insightNextBtn = document.getElementById('insightNext');

  if (insightTextEl) {
    // ── Insight pools by mood ────────────────────────────────────
    const insightsByMood = {
      'Great': [
        { text: "You're feeling great — use this energy to tackle one thing you've been putting off. Positive affect boosts creative problem-solving.", topic: "Momentum" },
        { text: "Good moods are contagious. Send a kind, specific message to someone today — it compounds their wellbeing and deepens your bond.", topic: "Connection" },
        { text: "When you feel this good, write down what contributed. Sleep? Exercise? A conversation? Knowing the formula helps you recreate it.", topic: "Self-Awareness" },
        { text: "High energy is a great time to strengthen a habit — not just do tasks. Link something you want to build to how you feel right now.", topic: "Habits" },
        { text: "Gratitude hits harder when you're already in a good mood. Name three specific things — not just 'health' but the exact moment that made you smile.", topic: "Gratitude" },
        { text: "This is a good day to have a conversation you've been avoiding. Emotional safety is higher when you're regulated and resourced.", topic: "Relationships" },
      ],
      'Okay': [
        { text: "Steady is underrated. 'Okay' means your nervous system is regulated — a perfect state for focused, quality work.", topic: "Focus" },
        { text: "Step outside for 5 minutes. A brief change of environment resets attention and quietly lifts neutral moods without effort.", topic: "Reset" },
        { text: "When you're in the middle, small actions matter most. A 10-minute walk, a glass of water, or one completed task can shift the day.", topic: "Momentum" },
        { text: "Neutral moods are ideal for reflection. Ask yourself: what would make today feel meaningful? Then do one small piece of it.", topic: "Intention" },
        { text: "Breathing out longer than you breathe in activates your parasympathetic system. Try 4 in, 6 out for 2 minutes — it's backed by research.", topic: "Calm" },
        { text: "You don't need to feel great to make progress. Consistent effort on ordinary days is what actually builds a life you're proud of.", topic: "Growth" },
      ],
      'Not great': [
        { text: "Name what you feel before trying to fix it. Labelling an emotion activates the prefrontal cortex and reduces its intensity immediately.", topic: "Emotional Regulation" },
        { text: "Place a hand on your chest and take 3 slow breaths. You're not broken — your nervous system is just asking for safety right now.", topic: "Grounding" },
        { text: "Rough days pass. You don't need to fix everything today — just identify one small thing that would make the next hour slightly better.", topic: "Self-Care" },
        { text: "Feeling low is data, not a verdict. It often means you need rest, connection, or to release an expectation you've been holding too tightly.", topic: "Compassion" },
        { text: "On hard days, lower the bar on purpose. Done and imperfect beats perfect and undone — especially when your resources are depleted.", topic: "Resilience" },
        { text: "You don't have to explain your feelings to anyone today. Protecting your energy is a valid and healthy choice.", topic: "Boundaries" },
      ],
      'default': [
        { text: "Name what you feel before trying to fix it. Labelling an emotion activates the prefrontal cortex and reduces its intensity.", topic: "Emotional Regulation" },
        { text: "A 5-minute walk outside shifts mood more reliably than scrolling. Your brain needs movement to process stress hormones.", topic: "Stress Relief" },
        { text: "Sleep is the most underrated mental health intervention. Even one extra hour can measurably improve mood, focus, and resilience.", topic: "Sleep & Recovery" },
        { text: "You don't need a large social circle. Two or three relationships where you feel genuinely seen are enough to protect your mental health.", topic: "Connection" },
        { text: "Anxiety often feels like a fact, but it's a signal. Try 'I notice I'm feeling anxious' instead of 'I am anxious' — the distance helps.", topic: "Anxiety" },
        { text: "Progress rarely looks like a straight line. Two steps forward, one back is still net progress. Track the trend, not each day.", topic: "Growth" },
        { text: "Breathing out longer than you breathe in activates the parasympathetic system. Try 4 in, 6 out for 2 minutes.", topic: "Calm" },
        { text: "Setting one boundary this week is more protective for your mental health than any supplement or productivity hack.", topic: "Boundaries" },
        { text: "Perfectionism is a form of anxiety. Done and imperfect moves you forward. Perfect and unfinished keeps you stuck.", topic: "Mindset" },
        { text: "Grief doesn't follow a schedule. Feeling something old resurface doesn't mean you're not healing — it means you're human.", topic: "Grief" },
      ]
    };

    const days = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];
    const today = new Date();
    const dayOfYear = Math.floor((today - new Date(today.getFullYear(), 0, 0)) / 86400000);

    let current = 0;
    let pool = insightsByMood['default'];

    function getCookie(n) {
      return document.cookie.split(';').map(c => c.trim())
        .filter(c => c.startsWith(n + '=')).map(c => decodeURIComponent(c.split('=')[1]))[0];
    }

    function initWidget(mood) {
      pool = insightsByMood[mood] || insightsByMood['default'];
      const label = mood && mood !== 'default'
        ? { 'Great': 'For you · Feeling great', 'Okay': 'For you · Feeling okay', 'Not great': 'For you · Rough day' }[mood]
        : null;

      const eyebrow = insightWidget?.querySelector('.insight-eyebrow');
      if (eyebrow && label) eyebrow.textContent = label;

      current = dayOfYear % pool.length;
      insightDayEl.textContent = days[today.getDay()];
      buildDots();
      showInsight(current, false);
    }

    function buildDots() {
      insightDotsEl.innerHTML = '';
      pool.forEach((_, i) => {
        const dot = document.createElement('button');
        dot.className = 'insight-dot' + (i === current ? ' active' : '');
        dot.setAttribute('aria-label', `Insight ${i + 1}`);
        dot.addEventListener('click', () => showInsight(i));
        insightDotsEl.appendChild(dot);
      });
    }

    function showInsight(index, animate = true) {
      current = (index + pool.length) % pool.length;
      if (animate) {
        insightTextEl.classList.add('fading');
        setTimeout(() => {
          insightTextEl.textContent = pool[current].text;
          insightTopicEl.textContent = pool[current].topic;
          insightTextEl.classList.remove('fading');
          buildDots();
        }, 220);
      } else {
        insightTextEl.textContent = pool[current].text;
        insightTopicEl.textContent = pool[current].topic;
        buildDots();
      }
    }

    insightNextBtn?.addEventListener('click', () => showInsight(current + 1));

    // ── Resolve mood then init ────────────────────────────────────
    const isAuth = insightWidget?.dataset?.authenticated === 'true';
    if (isAuth) {
      fetch('/Home/GetMood')
        .then(r => r.ok ? r.json() : { mood: null })
        .then(data => initWidget(data?.mood || 'default'))
        .catch(() => initWidget('default'));
    } else {
      const savedMood = getCookie('safemindMood');
      initWidget(savedMood || 'default');
    }
  }
});

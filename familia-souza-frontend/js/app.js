(() => {
  const S = window.FSShared;
  if (!S.requireAuthOrRedirect()) return;

  const user = S.getUser();
  document.getElementById("profileName").textContent = S.firstName(user.name);

  const state = {
    favorites: S.getFavorites(),
    selectedTitle: null,
    // Cache local de tudo que já veio da API, pra abrir o modal sem precisar buscar de novo.
    allTitles: []
  };

  const movieRow = document.getElementById("movieRow");
  const seriesRow = document.getElementById("seriesRow");
  const listRow = document.getElementById("listRow");
  const emptyList = document.getElementById("emptyList");
  const listCount = document.getElementById("listCount");

  init();
  bindNavigation();
  S.bindProfileMenu();
  S.bindLogoutButtons();
  S.bindSettingsModal();
  S.bindModalClose("movieModal");
  bindSearch();

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      S.closeModal("movieModal");
      S.closeModal("settingsModal");
      document.getElementById("searchPanel").hidden = true;
    }
  });

  window.addEventListener("scroll", () => {
    document.getElementById("navbar").classList.toggle("scrolled", window.scrollY > 15);
  });

  async function init() {
    const [moviesPage, seriesPage] = await Promise.all([
      S.fetchTitles({ type: "Movie", page: 1, pageSize: 20 }),
      S.fetchTitles({ type: "Series", page: 1, pageSize: 20 })
    ]);

    const movies = moviesPage.items;
    const series = seriesPage.items;

    cacheTitles([...movies, ...series]);
    renderAll(movies, series);
    renderHero(movies, series);
  }

  function cacheTitles(titles) {
    for (const title of titles) {
      const index = state.allTitles.findIndex((t) => t.id === title.id);
      if (index >= 0) state.allTitles[index] = title;
      else state.allTitles.push(title);
    }
  }

  // ---------------- Hero (destaque) ----------------

  function renderHero(movies, series) {
    // Escolhe aleatoriamente entre os títulos mais bem avaliados, pra variar a cada visita
    // em vez de sempre mostrar o mesmo destaque fixo.
    const pool = [...movies, ...series].slice(0, 8);
    if (pool.length === 0) {
      document.getElementById("heroTitle").textContent = "Bem-vindo à Família Souza";
      document.getElementById("heroDescription").textContent = "Seu catálogo ainda está sendo preparado.";
      return;
    }

    const featured = pool[Math.floor(Math.random() * pool.length)];
    state.featuredTitle = featured;

    document.getElementById("heroTitle").textContent = featured.name;
    document.getElementById("heroDescription").textContent = featured.description || "";

    const year = featured.releaseDate ? new Date(featured.releaseDate).getFullYear() : null;
    const rating = featured.rating ? `⭐ ${featured.rating.toFixed(1)}` : null;
    const genres = featured.genres && featured.genres.length ? featured.genres.join(" · ") : null;
    document.getElementById("heroMeta").innerHTML = [year, rating, genres]
      .filter(Boolean)
      .map((item) => `<span>${S.escapeHtml(String(item))}</span>`)
      .join("");
  }

  // ---------------- Renderização das fileiras ----------------

  function renderAll(movies, series) {
    const list = state.allTitles.filter((title) => state.favorites.includes(title.id));

    renderRow(movieRow, movies);
    renderRow(seriesRow, series);
    renderRow(listRow, list);

    listCount.textContent = `${list.length} ${list.length === 1 ? "título" : "títulos"}`;
    emptyList.hidden = list.length > 0;
    listRow.hidden = list.length === 0;
  }

  function renderRow(container, titles) {
    container.innerHTML = titles.map((title) => S.cardTemplate(title, state.favorites.includes(title.id))).join("");

    container.querySelectorAll(".movie-card").forEach((card) => {
      card.addEventListener("click", () => openTitle(Number(card.dataset.id)));
    });
    container.querySelectorAll("[data-favorite]").forEach((button) => {
      button.addEventListener("click", (event) => {
        event.stopPropagation();
        toggleFavorite(Number(button.dataset.favorite));
      });
    });
    container.querySelectorAll("[data-play]").forEach((button) => {
      button.addEventListener("click", (event) => {
        event.stopPropagation();
        openTitle(Number(button.dataset.play));
      });
    });
  }

  function openTitle(id) {
    const title = state.allTitles.find((item) => item.id === id);
    if (!title) return;
    state.selectedTitle = title;
    S.openTitleModal(title, state.favorites, toggleFavorite);
  }

  function toggleFavorite(id) {
    state.favorites = state.favorites.includes(id)
      ? state.favorites.filter((titleId) => titleId !== id)
      : [...state.favorites, id];
    S.setFavorites(state.favorites);

    const movies = state.allTitles.filter((t) => t.type === "Movie");
    const series = state.allTitles.filter((t) => t.type === "Series");
    renderAll(movies, series);

    if (state.selectedTitle) openTitle(state.selectedTitle.id);
    S.showToast(state.favorites.includes(id) ? "Adicionado à sua lista." : "Removido da sua lista.");
  }

  // ---------------- Navegação / UI ----------------

  function bindNavigation() {
    document.getElementById("heroWatchButton").addEventListener("click", () => {
      const featured = state.featuredTitle;
      if (!featured) return;
      const withUrl = (featured.providers || []).find((p) => p.watchUrl);
      if (withUrl) {
        window.open(withUrl.watchUrl, "_blank", "noopener");
      } else {
        openTitle(featured.id);
      }
    });
    document.getElementById("heroInfoButton").addEventListener("click", () => {
      if (state.featuredTitle) openTitle(state.featuredTitle.id);
    });
  }

  function bindSearch() {
    const panel = document.getElementById("searchPanel");
    const input = document.getElementById("searchInput");
    const results = document.getElementById("searchResults");
    let searchTimer = null;

    document.getElementById("searchButton").addEventListener("click", () => {
      panel.hidden = false;
      input.value = "";
      results.innerHTML = "";
      setTimeout(() => input.focus(), 50);
    });

    document.getElementById("closeSearch").addEventListener("click", () => {
      panel.hidden = true;
    });

    input.addEventListener("input", () => {
      const query = input.value.trim();
      clearTimeout(searchTimer);

      if (!query) {
        results.innerHTML = "";
        return;
      }

      searchTimer = setTimeout(async () => {
        const page = await S.fetchTitles({ q: query, page: 1, pageSize: 9 });
        cacheTitles(page.items);

        results.innerHTML = page.items.length
          ? page.items
              .map(
                (title) => `
                  <article class="result-card" data-result-id="${title.id}">
                    <p>${title.type === "Movie" ? "Filme" : "Série"}${title.releaseDate ? " · " + new Date(title.releaseDate).getFullYear() : ""}</p>
                    <h3>${S.escapeHtml(title.name)}</h3>
                    <p>${S.escapeHtml((title.genres || []).join(", "))}</p>
                  </article>
                `
              )
              .join("")
          : `<p style="color:#777">Nenhum título encontrado.</p>`;

        results.querySelectorAll("[data-result-id]").forEach((card) =>
          card.addEventListener("click", () => {
            panel.hidden = true;
            openTitle(Number(card.dataset.resultId));
          })
        );
      }, 300);
    });
  }
})();
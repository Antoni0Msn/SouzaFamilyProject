(() => {
  const S = window.FSShared;
  if (!S.requireAuthOrRedirect()) return;

  const user = S.getUser();
  document.getElementById("profileName").textContent = S.firstName(user.name);

  const params = new URLSearchParams(window.location.search);

  const state = {
    favorites: S.getFavorites(),
    allTitles: [],
    type: params.get("type") || "",
    genre: params.get("genre") || "",
    sort: params.get("sort") || "",
    onlyMyProviders: false,
    page: 1,
    pageSize: 30,
    totalPages: 1
  };

  const grid = document.getElementById("catalogGrid");
  const emptyState = document.getElementById("catalogEmpty");
  const loadMoreButton = document.getElementById("loadMoreButton");

  S.bindProfileMenu();
  S.bindLogoutButtons();
  S.bindSettingsModal(refreshAfterSettings);
  S.bindModalClose("movieModal");
  bindFilters();
  bindGenreFilter();
  bindSortFilter();
  bindMyProvidersFilter();
  bindSearch();
  bindLoadMore();

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") {
      const settingsWasOpen = !document.getElementById("settingsModal").hidden;
      S.closeModal("movieModal");
      S.closeModal("settingsModal");
      document.getElementById("searchPanel").hidden = true;
      if (settingsWasOpen) refreshAfterSettings();
    }
  });

  loadPage(1, { replace: true });

  function refreshAfterSettings() {
    loadPage(1, { replace: true });
  }

  function bindFilters() {
    const buttons = {
      "": document.getElementById("filterAll"),
      Movie: document.getElementById("filterMovies"),
      Series: document.getElementById("filterSeries")
    };

    function updateActive() {
      Object.entries(buttons).forEach(([type, button]) => button.classList.toggle("active", type === state.type));
    }

    Object.entries(buttons).forEach(([type, button]) => {
      button.addEventListener("click", () => {
        state.type = type;
        updateActive();
        loadPage(1, { replace: true });
      });
    });

    updateActive();
  }

  async function bindGenreFilter() {
    const toggle = document.getElementById("genreDropdownToggle");
    const menu = document.getElementById("genreDropdownMenu");
    const label = document.getElementById("genreDropdownLabel");

    const genres = [{ name: "" , display: "Todos os gêneros" }, ...(await S.fetchGenres()).map((g) => ({ name: g.name, display: g.name }))];

    function renderMenu() {
      menu.innerHTML = genres
        .map(
          (g) => `<button type="button" data-genre="${S.escapeHtml(g.name)}" class="${g.name === state.genre ? "active" : ""}">${S.escapeHtml(g.display)}</button>`
        )
        .join("");

      menu.querySelectorAll("button").forEach((button) => {
        button.addEventListener("click", () => {
          state.genre = button.dataset.genre;
          label.textContent = button.textContent;
          closeMenu();
          loadPage(1, { replace: true });
        });
      });
    }

    function openMenu() {
      menu.hidden = false;
      toggle.setAttribute("aria-expanded", "true");
    }

    function closeMenu() {
      menu.hidden = true;
      toggle.setAttribute("aria-expanded", "false");
    }

    toggle.addEventListener("click", () => {
      menu.hidden ? openMenu() : closeMenu();
    });

    document.addEventListener("click", (event) => {
      if (!event.target.closest("#genreDropdown")) closeMenu();
    });

    const current = genres.find((g) => g.name === state.genre);
    if (current) label.textContent = current.display;

    renderMenu();
  }

  function bindSortFilter() {
    const toggle = document.getElementById("sortDropdownToggle");
    const menu = document.getElementById("sortDropdownMenu");
    const label = document.getElementById("sortDropdownLabel");

    const options = [
      { value: "", display: "Avaliação" },
      { value: "year", display: "Ano" },
      { value: "name", display: "A-Z" }
    ];

    function renderMenu() {
      menu.innerHTML = options
        .map(
          (opt) =>
            `<button type="button" data-sort="${opt.value}" class="${opt.value === state.sort ? "active" : ""}">${opt.display}</button>`
        )
        .join("");

      menu.querySelectorAll("button").forEach((button) => {
        button.addEventListener("click", () => {
          state.sort = button.dataset.sort;
          label.textContent = button.textContent;
          menu.querySelectorAll("button").forEach((b) => b.classList.toggle("active", b === button));
          closeMenu();
          loadPage(1, { replace: true });
        });
      });
    }

    function openMenu() {
      menu.hidden = false;
      toggle.setAttribute("aria-expanded", "true");
    }

    function closeMenu() {
      menu.hidden = true;
      toggle.setAttribute("aria-expanded", "false");
    }

    toggle.addEventListener("click", () => {
      menu.hidden ? openMenu() : closeMenu();
    });

    document.addEventListener("click", (event) => {
      if (!event.target.closest("#sortDropdown")) closeMenu();
    });

    const current = options.find((opt) => opt.value === state.sort);
    if (current) label.textContent = current.display;

    renderMenu();
  }

  function bindMyProvidersFilter() {
    const button = document.getElementById("filterMyProviders");
    if (!button) return;

    button.addEventListener("click", () => {
      state.onlyMyProviders = !state.onlyMyProviders;
      button.classList.toggle("active", state.onlyMyProviders);
      loadPage(1, { replace: true });
    });
  }

  async function loadPage(page, { replace = false } = {}) {
    const result = await S.fetchTitles({
      ...(state.type ? { type: state.type } : {}),
      ...(state.genre ? { genre: state.genre } : {}),
      ...(state.sort ? { sort: state.sort } : {}),
      ...(state.onlyMyProviders ? { onlyMyProviders: true } : {}),
      page,
      pageSize: state.pageSize
    });

    state.page = result.page;
    state.totalPages = result.totalPages;

    if (replace) {
      state.allTitles = [];
      grid.innerHTML = "";
    }

    cacheTitles(result.items);
    renderTitles(result.items, { append: !replace });

    updateHeader(result.totalCount);
    loadMoreButton.hidden = state.page >= state.totalPages;
  }

  function updateHeader(totalCount) {
    const baseLabel = state.type === "Movie" ? "Filmes" : state.type === "Series" ? "Séries" : "Catálogo completo";
    const label = state.genre ? `${baseLabel} · ${state.genre}` : baseLabel;
    document.getElementById("catalogTitle").textContent = label;
    document.getElementById("catalogSubtitle").textContent =
      totalCount > 0 ? `${totalCount} ${totalCount === 1 ? "título" : "títulos"}` : "Nenhum título encontrado.";
  }

  function cacheTitles(titles) {
    for (const title of titles) {
      const index = state.allTitles.findIndex((t) => t.id === title.id);
      if (index >= 0) state.allTitles[index] = title;
      else state.allTitles.push(title);
    }
  }

  function renderTitles(titles, { append }) {
    if (!append) grid.innerHTML = "";

    emptyState.hidden = state.allTitles.length > 0;

    const html = titles.map((title) => S.cardTemplate(title, state.favorites.includes(title.id))).join("");
    grid.insertAdjacentHTML("beforeend", html);

    grid.querySelectorAll(".movie-card:not([data-bound])").forEach((card) => {
      card.setAttribute("data-bound", "1");
      card.addEventListener("click", () => openTitle(Number(card.dataset.id)));
      card.querySelector("[data-favorite]")?.addEventListener("click", (event) => {
        event.stopPropagation();
        toggleFavorite(Number(card.dataset.id));
      });
      card.querySelector("[data-play]")?.addEventListener("click", (event) => {
        event.stopPropagation();
        openTitle(Number(card.dataset.id));
      });
    });
  }

  function openTitle(id) {
    const title = state.allTitles.find((item) => item.id === id);
    if (!title) return;
    S.openTitleModal(title, state.favorites, toggleFavorite);
  }

  function toggleFavorite(id) {
    state.favorites = state.favorites.includes(id)
      ? state.favorites.filter((titleId) => titleId !== id)
      : [...state.favorites, id];
    S.setFavorites(state.favorites);

    // Atualiza só o ícone do card e do modal, sem recarregar a grade inteira.
    document.querySelectorAll(`[data-id="${id}"] [data-favorite]`).forEach((button) => {
      const isFavorite = state.favorites.includes(id);
      button.textContent = isFavorite ? "✓" : "＋";
    });

    const title = state.allTitles.find((t) => t.id === id);
    if (title) S.openTitleModal(title, state.favorites, toggleFavorite);

    S.showToast(state.favorites.includes(id) ? "Adicionado à sua lista." : "Removido da sua lista.");
  }

  function bindLoadMore() {
    loadMoreButton.addEventListener("click", () => loadPage(state.page + 1));
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
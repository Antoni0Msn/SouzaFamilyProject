(() => {
  const getStored = (key) => sessionStorage.getItem(key) || localStorage.getItem(key);
  const token = getStored("fs_token");

  if (!token) {
    window.location.href = "index.html";
    return;
  }

  const user = (() => {
    try { return JSON.parse(getStored("fs_user") || "{}"); } catch (_) { return {}; }
  })();

  document.getElementById("profileName").textContent = firstName(user.name || "Souza");

  const state = {
    favorites: JSON.parse(localStorage.getItem("fs_favorites") || "[]"),
    selectedMovie: null
  };

  const continueRow = document.getElementById("continueRow");
  const movieRow = document.getElementById("movieRow");
  const seriesRow = document.getElementById("seriesRow");
  const listRow = document.getElementById("listRow");
  const emptyList = document.getElementById("emptyList");
  const listCount = document.getElementById("listCount");
  const modal = document.getElementById("movieModal");
  const toast = document.getElementById("toast");

  renderAll();
  bindNavigation();
  bindProfileMenu();
  bindSearch();
  bindModal();
  bindLogout();

  window.addEventListener("scroll", () => {
    document.getElementById("navbar").classList.toggle("scrolled", window.scrollY > 15);
  });

  function renderAll() {
    const continued = MOVIES.filter((movie) => movie.progress > 0);
    const films = MOVIES.filter((movie) => movie.type === "Filme").slice(0, 10);
    const series = MOVIES.filter((movie) => movie.type === "Série").slice(0, 10);
    const list = MOVIES.filter((movie) => state.favorites.includes(movie.id));

    renderRow(continueRow, continued, true);
    renderRow(movieRow, films);
    renderRow(seriesRow, series);
    renderRow(listRow, list);

    listCount.textContent = `${list.length} ${list.length === 1 ? "título" : "títulos"}`;
    emptyList.hidden = list.length > 0;
    listRow.hidden = list.length === 0;
  }

  function renderRow(container, movies, showProgress = false) {
    container.innerHTML = movies.map((movie) => cardTemplate(movie, showProgress)).join("");
    container.querySelectorAll(".movie-card").forEach((card) => {
      card.addEventListener("click", () => openMovie(Number(card.dataset.id)));
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
        openMovie(Number(button.dataset.play));
      });
    });
  }

  function cardTemplate(movie, showProgress) {
    const isFavorite = state.favorites.includes(movie.id);
    return `
      <article class="movie-card" data-id="${movie.id}" title="${escapeHtml(movie.title)}">
        <div class="movie-art" style="--c1:${movie.c1};--c2:${movie.c2};">
          <span class="poster-title">${escapeHtml(movie.title)}</span>
        </div>
        <span class="card-badge">${movie.age}</span>
        <div class="card-actions">
          <button class="card-mini-btn" data-play="${movie.id}" aria-label="Abrir ${escapeHtml(movie.title)}">▶</button>
          <button class="card-mini-btn" data-favorite="${movie.id}" aria-label="${isFavorite ? "Remover da lista" : "Adicionar à lista"}">${isFavorite ? "✓" : "＋"}</button>
        </div>
        ${showProgress && movie.progress > 0 ? `<div class="progress-bar"><span style="width:${movie.progress}%"></span></div>` : ""}
      </article>
    `;
  }

  function openMovie(id) {
    const movie = MOVIES.find((item) => item.id === id);
    if (!movie) return;
    state.selectedMovie = movie;

    document.getElementById("modalArt").style.background = `radial-gradient(circle at 72% 26%, rgba(255,255,255,.18), transparent 23%), linear-gradient(125deg, ${movie.c1}, ${movie.c2} 76%)`;
    document.getElementById("modalType").textContent = movie.type.toUpperCase();
    document.getElementById("modalTitle").textContent = movie.title;
    document.getElementById("modalMeta").textContent = `${movie.year} · ${movie.age} · ${movie.duration} · ${movie.genre}`;
    document.getElementById("modalDescription").textContent = movie.description;
    document.getElementById("modalList").innerHTML = state.favorites.includes(movie.id) ? "✓ &nbsp; Na minha lista" : "＋ &nbsp; Minha lista";

    modal.hidden = false;
    document.body.style.overflow = "hidden";
  }

  function closeModal() {
    modal.hidden = true;
    document.body.style.overflow = "";
  }

  function bindModal() {
    document.querySelectorAll("[data-close-modal]").forEach((element) => element.addEventListener("click", closeModal));
    document.getElementById("modalWatch").addEventListener("click", () => {
      showToast(`Abrindo “${state.selectedMovie?.title || "título"}”…`);
      closeModal();
    });
    document.getElementById("modalList").addEventListener("click", () => {
      if (state.selectedMovie) toggleFavorite(state.selectedMovie.id);
    });
    document.addEventListener("keydown", (event) => {
      if (event.key === "Escape") {
        closeModal();
        document.getElementById("searchPanel").hidden = true;
      }
    });
  }

  function toggleFavorite(id) {
    state.favorites = state.favorites.includes(id)
      ? state.favorites.filter((movieId) => movieId !== id)
      : [...state.favorites, id];
    localStorage.setItem("fs_favorites", JSON.stringify(state.favorites));
    renderAll();
    if (state.selectedMovie) openMovie(state.selectedMovie.id);
    showToast(state.favorites.includes(id) ? "Adicionado à sua lista." : "Removido da sua lista.");
  }

  function bindNavigation() {
    document.getElementById("heroWatchButton").addEventListener("click", () => openMovie(1));
    document.getElementById("heroInfoButton").addEventListener("click", () => openMovie(1));
    document.querySelectorAll("[data-scroll-target]").forEach((button) => {
      button.addEventListener("click", () => document.getElementById(button.dataset.scrollTarget)?.scrollIntoView({ behavior: "smooth" }));
    });
  }

  function bindProfileMenu() {
    const button = document.getElementById("profileButton");
    const menu = document.getElementById("profileMenu");
    button.addEventListener("click", () => {
      const expanded = button.getAttribute("aria-expanded") === "true";
      button.setAttribute("aria-expanded", String(!expanded));
      menu.hidden = expanded;
    });
    document.addEventListener("click", (event) => {
      if (!event.target.closest(".profile-menu-wrap")) {
        menu.hidden = true;
        button.setAttribute("aria-expanded", "false");
      }
    });
  }

  function bindSearch() {
    const panel = document.getElementById("searchPanel");
    const input = document.getElementById("searchInput");
    const results = document.getElementById("searchResults");

    document.getElementById("searchButton").addEventListener("click", () => {
      panel.hidden = false;
      input.value = "";
      results.innerHTML = "";
      setTimeout(() => input.focus(), 50);
    });

    document.getElementById("closeSearch").addEventListener("click", () => { panel.hidden = true; });

    input.addEventListener("input", () => {
      const query = input.value.trim().toLowerCase();
      if (!query) { results.innerHTML = ""; return; }
      const matches = MOVIES.filter((movie) => `${movie.title} ${movie.genre} ${movie.type}`.toLowerCase().includes(query)).slice(0, 9);
      results.innerHTML = matches.length ? matches.map((movie) => `
        <article class="result-card" data-result-id="${movie.id}">
          <p>${movie.type} · ${movie.year}</p>
          <h3>${escapeHtml(movie.title)}</h3>
          <p>${escapeHtml(movie.genre)}</p>
        </article>
      `).join("") : `<p style="color:#777">Nenhum título encontrado.</p>`;
      results.querySelectorAll("[data-result-id]").forEach((card) => card.addEventListener("click", () => {
        panel.hidden = true;
        openMovie(Number(card.dataset.resultId));
      }));
    });
  }

  function bindLogout() {
    document.getElementById("logoutButton").addEventListener("click", () => {
      sessionStorage.removeItem("fs_token");
      sessionStorage.removeItem("fs_user");
      localStorage.removeItem("fs_token");
      localStorage.removeItem("fs_user");
      window.location.href = "index.html";
    });
  }

  function showToast(message) {
    toast.textContent = message;
    toast.classList.add("show");
    clearTimeout(showToast.timer);
    showToast.timer = setTimeout(() => toast.classList.remove("show"), 2300);
  }

  function firstName(name) { return String(name).trim().split(/\s+/)[0] || "Souza"; }
  function escapeHtml(value) { return String(value).replace(/[&<>'"]/g, (char) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" }[char])); }
})();

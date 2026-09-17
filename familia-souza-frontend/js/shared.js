// ==============================
// FUNÇÕES COMPARTILHADAS (Home + Catálogo)
// ==============================
window.FSShared = (() => {
  function getStored(key) {
    return sessionStorage.getItem(key) || localStorage.getItem(key);
  }

  function getToken() {
    return getStored("fs_token");
  }

  function getUser() {
    try { return JSON.parse(getStored("fs_user") || "{}"); } catch (_) { return {}; }
  }

  function requireAuthOrRedirect() {
    if (!getToken()) {
      window.location.href = "index.html";
      return false;
    }
    return true;
  }

  function authHeaders() {
    return { Authorization: `Bearer ${getToken()}` };
  }

  function logout() {
    sessionStorage.removeItem("fs_token");
    sessionStorage.removeItem("fs_user");
    localStorage.removeItem("fs_token");
    localStorage.removeItem("fs_user");
    window.location.href = "index.html";
  }

  function firstName(name) {
    return String(name || "Souza").trim().split(/\s+/)[0] || "Souza";
  }

  function escapeHtml(value) {
    return String(value).replace(
      /[&<>'"]/g,
      (char) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "'": "&#39;", '"': "&quot;" }[char])
    );
  }

  function getFavorites() {
    return JSON.parse(localStorage.getItem("fs_favorites") || "[]");
  }

  function setFavorites(ids) {
    localStorage.setItem("fs_favorites", JSON.stringify(ids));
  }

  /**
   * Busca títulos paginados da API. Retorna { items, page, pageSize, totalCount, totalPages }.
   * Em caso de 401, desloga automaticamente. Em outros erros, retorna uma página vazia.
   */
  async function fetchTitles(params) {
    const query = new URLSearchParams(params).toString();

    try {
      const response = await fetch(
        `${window.APP_CONFIG.API_BASE_URL}${window.APP_CONFIG.endpoints.titles}?${query}`,
        { headers: authHeaders() }
      );

      if (response.status === 401) {
        logout();
        return emptyPage();
      }

      if (!response.ok) throw new Error(`Falha ao buscar catálogo (${response.status})`);

      return await response.json();
    } catch (error) {
      console.error(error);
      return emptyPage();
    }
  }

  function emptyPage() {
    return { items: [], page: 1, pageSize: 0, totalCount: 0, totalPages: 0 };
  }

  async function fetchMe() {
    try {
      const response = await fetch(
        `${window.APP_CONFIG.API_BASE_URL}/auth/me`,
        { headers: authHeaders() }
      );
      if (!response.ok) return null;
      return await response.json();
    } catch (error) {
      console.error(error);
      return null;
    }
  }

  async function fetchGenres() {
    try {
      const response = await fetch(
        `${window.APP_CONFIG.API_BASE_URL}/genres`,
        { headers: authHeaders() }
      );
      if (!response.ok) return [];
      return await response.json();
    } catch (error) {
      console.error(error);
      return [];
    }
  }

  function cardTemplate(title, isFavorite) {
    const artStyle = title.posterUrl
      ? `background-image:url('${title.posterUrl}');background-size:cover;background-position:center;`
      : `--c1:#2a2a2a;--c2:#0c0c0c;`;

    return `
      <article class="movie-card" data-id="${title.id}" title="${escapeHtml(title.name)}">
        <div class="movie-art" style="${artStyle}">
          ${!title.posterUrl ? `<span class="poster-title">${escapeHtml(title.name)}</span>` : ""}
        </div>
        <span class="card-badge">${title.type === "Movie" ? "FILME" : "SÉRIE"}</span>
        <div class="card-actions">
          <button class="card-mini-btn" data-play="${title.id}" aria-label="Abrir ${escapeHtml(title.name)}">▶</button>
          <button class="card-mini-btn" data-favorite="${title.id}" aria-label="${isFavorite ? "Remover da lista" : "Adicionar à lista"}">${isFavorite ? "✓" : "＋"}</button>
        </div>
      </article>
    `;
  }

  /**
   * Preenche e abre o modal de detalhes de um título. Precisa que o HTML da página
   * tenha os elementos com esses IDs (movieModal, modalArt, modalType, modalTitle,
   * modalMeta, modalDescription, modalWatchButtons, modalList).
   */
  function openTitleModal(title, favorites, onToggleFavorite) {
    const modal = document.getElementById("movieModal");
    if (!modal) return;

    const backdrop = title.backdropUrl || title.posterUrl;
    document.getElementById("modalArt").style.background = backdrop
      ? `linear-gradient(180deg, rgba(0,0,0,.15), rgba(0,0,0,.85)), url('${backdrop}') center/cover no-repeat`
      : `linear-gradient(120deg, #3a1118, #0c0c0c 75%)`;

    document.getElementById("modalType").textContent = title.type === "Movie" ? "FILME" : "SÉRIE";
    document.getElementById("modalTitle").textContent = title.name;

    const year = title.releaseDate ? new Date(title.releaseDate).getFullYear() : null;
    const rating = title.rating ? `⭐ ${title.rating.toFixed(1)}` : null;
    const genres = title.genres && title.genres.length ? title.genres.join(" · ") : null;
    document.getElementById("modalMeta").textContent = [year, rating, genres].filter(Boolean).join(" · ");

    document.getElementById("modalDescription").textContent = title.description || "Sem descrição disponível.";

    const isFavorite = favorites.includes(title.id);
    document.getElementById("modalList").innerHTML = isFavorite ? "✓ &nbsp; Na minha lista" : "＋ &nbsp; Minha lista";
    document.getElementById("modalList").onclick = () => onToggleFavorite(title.id);

    renderWatchButtons(title.providers || []);

    modal.hidden = false;
    document.body.style.overflow = "hidden";
  }

  function renderWatchButtons(providers) {
    const container = document.getElementById("modalWatchButtons");
    if (!container) return;

    const withUrl = providers.filter((p) => p.watchUrl);

    if (withUrl.length === 0) {
      container.innerHTML = `<p style="color:#888;font-size:.82rem;margin:0;">Ainda não sabemos onde assistir esse título.</p>`;
      return;
    }

    container.innerHTML = withUrl
      .map(
        (provider) => `
          <button class="primary-button" data-watch-url="${escapeHtml(provider.watchUrl)}">
            ▶ &nbsp; Assistir na ${escapeHtml(provider.name)}
          </button>
        `
      )
      .join("");

    container.querySelectorAll("[data-watch-url]").forEach((button) => {
      button.addEventListener("click", () => {
        const url = button.getAttribute("data-watch-url");
        if (url) window.open(url, "_blank", "noopener");
      });
    });
  }

  function closeModal(id) {
    const modal = document.getElementById(id);
    if (!modal) return;
    modal.hidden = true;
    document.body.style.overflow = "";
  }

  function bindModalClose(modalId) {
    const modal = document.getElementById(modalId);
    if (!modal) return;
    modal.querySelectorAll("[data-close-modal]").forEach((el) =>
      el.addEventListener("click", () => closeModal(modalId))
    );
  }

  function showToast(message) {
    const toast = document.getElementById("toast");
    if (!toast) return;
    toast.textContent = message;
    toast.classList.add("show");
    clearTimeout(showToast.timer);
    showToast.timer = setTimeout(() => toast.classList.remove("show"), 2300);
  }

  function bindProfileMenu() {
    const button = document.getElementById("profileButton");
    const menu = document.getElementById("profileMenu");
    if (!button || !menu) return;

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

  function bindLogoutButtons() {
    document.getElementById("logoutButton")?.addEventListener("click", logout);
    document.getElementById("settingsLogout")?.addEventListener("click", logout);
  }

  async function bindSettingsModal() {
    const settingsButton = document.getElementById("settingsButton");
    if (!settingsButton) return;

    bindModalClose("settingsModal");

    settingsButton.addEventListener("click", async () => {
      const me = await fetchMe();
      const user = getUser();
      document.getElementById("settingsName").textContent = me?.name || user.name || "—";
      document.getElementById("settingsEmail").textContent = me?.email || user.email || "—";
      document.getElementById("settingsModal").hidden = false;
      document.body.style.overflow = "hidden";
    });
  }

  return {
    getToken,
    getUser,
    requireAuthOrRedirect,
    authHeaders,
    logout,
    firstName,
    escapeHtml,
    getFavorites,
    setFavorites,
    fetchTitles,
    fetchMe,
    fetchGenres,
    cardTemplate,
    openTitleModal,
    renderWatchButtons,
    closeModal,
    bindModalClose,
    showToast,
    bindProfileMenu,
    bindLogoutButtons,
    bindSettingsModal
  };
})();
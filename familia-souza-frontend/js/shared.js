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

  /**
   * Formata dígitos como telefone brasileiro conforme a quantidade digitada:
   * (DD, (DD) NNNN, (DD) NNNN-NNNN (fixo) ou (DD) NNNNN-NNNN (celular).
   */
  function maskPhoneBR(value) {
    const digits = String(value).replace(/\D/g, "").slice(0, 11);
    const len = digits.length;

    if (len === 0) return "";
    if (len <= 2) return `(${digits}`;
    if (len <= 6) return `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
    if (len <= 10) return `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
    return `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
  }

  /** Aplica a máscara em tempo real a um <input>, sempre que o usuário digitar. */
  function bindPhoneMask(input) {
    if (!input) return;
    input.addEventListener("input", () => {
      input.value = maskPhoneBR(input.value);
    });
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

  async function fetchAllProviders() {
    try {
      const response = await fetch(
        `${window.APP_CONFIG.API_BASE_URL}/providers`,
        { headers: authHeaders() }
      );
      if (!response.ok) return [];
      return await response.json();
    } catch (error) {
      console.error(error);
      return [];
    }
  }

  async function fetchMyProviders() {
    try {
      const response = await fetch(
        `${window.APP_CONFIG.API_BASE_URL}/providers/me`,
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

    const nameInput = document.getElementById("settingsNameInput");
    const emailInput = document.getElementById("settingsEmailInput");
    const phoneInput = document.getElementById("settingsPhoneInput");
    bindPhoneMask(phoneInput);

    const profileMessage = document.getElementById("settingsProfileMessage");
    const saveProfileButton = document.getElementById("settingsSaveProfile");
    const providersContainer = document.getElementById("settingsProviders");
    const saveProvidersButton = document.getElementById("settingsSaveProviders");

    settingsButton.addEventListener("click", async () => {
      document.getElementById("settingsModal").hidden = false;
      document.body.style.overflow = "hidden";
      setMessage(profileMessage, "");

      const me = await fetchMe();
      if (me) {
        nameInput.value = me.name || "";
        emailInput.value = me.email || "";
        phoneInput.value = maskPhoneBR(me.phoneNumber || "");
      }

      if (providersContainer) await loadProviderCheckboxes(providersContainer);
    });

    saveProfileButton?.addEventListener("click", async () => {
      setMessage(profileMessage, "");
      saveProfileButton.disabled = true;

      try {
        const response = await fetch(`${window.APP_CONFIG.API_BASE_URL}/auth/me`, {
          method: "PUT",
          headers: { "Content-Type": "application/json", ...authHeaders() },
          body: JSON.stringify({
            displayName: nameInput.value.trim(),
            phoneNumber: phoneInput.value.trim()
          })
        });

        const payload = await response.json().catch(() => null);

        if (!response.ok) {
          setMessage(profileMessage, payload?.message || "Não foi possível salvar.", true);
          return;
        }

        const currentUser = getUser();
        currentUser.name = payload.name;
        const storage = sessionStorage.getItem("fs_token") ? sessionStorage : localStorage;
        storage.setItem("fs_user", JSON.stringify(currentUser));

        const profileNameEl = document.getElementById("profileName");
        if (profileNameEl) profileNameEl.textContent = firstName(payload.name);

        showToast("Dados atualizados.");
      } finally {
        saveProfileButton.disabled = false;
      }
    });

    saveProvidersButton?.addEventListener("click", async () => {
      const selected = Array.from(providersContainer.querySelectorAll("input[type=checkbox]:checked")).map((el) =>
        Number(el.value)
      );

      saveProvidersButton.disabled = true;
      try {
        const response = await fetch(`${window.APP_CONFIG.API_BASE_URL}/providers/me`, {
          method: "PUT",
          headers: { "Content-Type": "application/json", ...authHeaders() },
          body: JSON.stringify({ providerIds: selected })
        });

        showToast(response.ok ? "Streamings salvos." : "Não foi possível salvar os streamings.");
      } finally {
        saveProvidersButton.disabled = false;
      }
    });
  }

  async function loadProviderCheckboxes(container) {
    const [allProviders, myProviderIds] = await Promise.all([fetchAllProviders(), fetchMyProviders()]);

    container.innerHTML = allProviders.length
      ? allProviders
          .map(
            (provider) => `
              <label class="provider-check">
                <input type="checkbox" value="${provider.id}" ${myProviderIds.includes(provider.id) ? "checked" : ""} />
                ${escapeHtml(provider.name)}
              </label>
            `
          )
          .join("")
      : `<p style="color:#777;font-size:.8rem;margin:0;">Nenhum streaming cadastrado ainda.</p>`;
  }

  function setMessage(el, text, isError) {
    if (!el) return;
    if (!text) {
      el.hidden = true;
      el.textContent = "";
      return;
    }
    el.hidden = false;
    el.textContent = text;
    el.classList.toggle("error", Boolean(isError));
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
    maskPhoneBR,
    bindPhoneMask,
    fetchTitles,
    fetchMe,
    fetchGenres,
    fetchAllProviders,
    fetchMyProviders,
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
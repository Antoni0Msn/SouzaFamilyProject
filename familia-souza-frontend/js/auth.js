(() => {
  const form = document.getElementById("loginForm");
  if (!form) return;

  if (sessionStorage.getItem("fs_token")) {
    window.location.href = "home.html";
    return;
  }

  const passwordInput = document.getElementById("password");
  const togglePassword = document.getElementById("togglePassword");
  const errorBox = document.getElementById("loginError");
  const rememberMe = document.getElementById("rememberMe");
  const forgotPassword = document.getElementById("forgotPassword");

  togglePassword.addEventListener("click", () => {
    const hidden = passwordInput.type === "password";
    passwordInput.type = hidden ? "text" : "password";
    togglePassword.textContent = hidden ? "◌" : "◉";
    togglePassword.setAttribute("aria-label", hidden ? "Ocultar senha" : "Mostrar senha");
  });

  forgotPassword.addEventListener("click", (event) => {
    event.preventDefault();
    showError("Recuperação de senha será conectada à sua API em uma próxima etapa.");
  });

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    clearError();

    const email = document.getElementById("email").value.trim();
    const password = passwordInput.value;

    if (!email || !email.includes("@")) {
      showError("Digite um e-mail válido.");
      return;
    }

    if (!password || password.length < 6) {
      showError("A senha precisa ter pelo menos 6 caracteres.");
      return;
    }

    try {
      const result = await login(email, password);
      const storage = rememberMe.checked ? localStorage : sessionStorage;
      storage.setItem("fs_token", result.token);
      storage.setItem("fs_user", JSON.stringify(result.user || { name: "Souza", email }));

      // Remove token salvo anteriormente no storage oposto.
      (rememberMe.checked ? sessionStorage : localStorage).removeItem("fs_token");
      (rememberMe.checked ? sessionStorage : localStorage).removeItem("fs_user");

      window.location.href = "home.html";
    } catch (error) {
      showError(error.message || "Não foi possível entrar. Tente novamente.");
    }
  });

  async function login(email, password) {
    if (window.APP_CONFIG.DEMO_MODE) {
      // Login de demonstração para você abrir o frontend sem backend.
      await new Promise((resolve) => setTimeout(resolve, 450));
      return {
        token: "demo-token-familia-souza",
        user: { id: 1, name: "Família Souza", email }
      };
    }

    const response = await fetch(`${window.APP_CONFIG.API_BASE_URL}${window.APP_CONFIG.endpoints.login}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password })
    });

    let payload = null;
    try { payload = await response.json(); } catch (_) {}

    if (!response.ok) {
      throw new Error(payload?.message || payload?.error || "E-mail ou senha inválidos.");
    }

    // Ajuste aqui se seu C# retornar nomes diferentes.
    return {
      token: payload.token || payload.accessToken,
      user: payload.user || { name: payload.name || "Família Souza", email }
    };
  }

  function showError(message) {
    errorBox.hidden = false;
    errorBox.textContent = message;
  }

  function clearError() {
    errorBox.hidden = true;
    errorBox.textContent = "";
  }
})();
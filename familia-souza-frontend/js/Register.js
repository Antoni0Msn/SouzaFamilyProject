(() => {
  const form = document.getElementById("registerForm");
  if (!form) return;

  const getStored = (key) => sessionStorage.getItem(key) || localStorage.getItem(key);
  if (getStored("fs_token")) {
    window.location.href = "home.html";
    return;
  }

  const passwordInput = document.getElementById("password");
  const confirmPasswordInput = document.getElementById("confirmPassword");
  const togglePassword = document.getElementById("togglePassword");
  const errorBox = document.getElementById("registerError");

  togglePassword.addEventListener("click", () => {
    const hidden = passwordInput.type === "password";
    passwordInput.type = hidden ? "text" : "password";
    confirmPasswordInput.type = hidden ? "text" : "password";
    togglePassword.textContent = hidden ? "◌" : "◉";
    togglePassword.setAttribute("aria-label", hidden ? "Ocultar senha" : "Mostrar senha");
  });

  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    clearError();

    const displayName = document.getElementById("displayName").value.trim();
    const email = document.getElementById("email").value.trim();
    const password = passwordInput.value;
    const confirmPassword = confirmPasswordInput.value;

    if (!displayName) {
      showError("Digite seu nome.");
      return;
    }

    if (!email || !email.includes("@")) {
      showError("Digite um e-mail válido.");
      return;
    }

    if (!password || password.length < 6) {
      showError("A senha precisa ter pelo menos 6 caracteres.");
      return;
    }

    if (password !== confirmPassword) {
      showError("As senhas não coincidem.");
      return;
    }

    const submitButton = form.querySelector("button[type='submit']");
    submitButton.disabled = true;

    try {
      const result = await register(email, password, displayName);

      // Cadastro já entra logado, igual ao login (guarda em sessionStorage por padrão).
      sessionStorage.setItem("fs_token", result.token);
      sessionStorage.setItem("fs_user", JSON.stringify(result.user));

      window.location.href = "home.html";
    } catch (error) {
      showError(error.message || "Não foi possível criar sua conta. Tente novamente.");
      submitButton.disabled = false;
    }
  });

  async function register(email, password, displayName) {
    const response = await fetch(`${window.APP_CONFIG.API_BASE_URL}${window.APP_CONFIG.endpoints.register}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password, displayName })
    });

    let payload = null;
    try { payload = await response.json(); } catch (_) {}

    if (!response.ok) {
      throw new Error(payload?.message || "Não foi possível criar sua conta.");
    }

    return {
      token: payload.token,
      user: payload.user
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
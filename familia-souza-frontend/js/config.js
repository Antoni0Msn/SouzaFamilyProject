// ==============================
// CONFIGURAÇÃO DO FRONTEND
// ==============================
// Troque pela URL da sua API REST em C# hospedada no Render.
// Exemplo: https://familia-souza-api.onrender.com/api
const API_BASE_URL = "https://SEU-BACKEND-RENDER.onrender.com/api";

// true = usa o login demonstrativo enquanto a API ainda não estiver conectada.
// Mude para false quando sua API estiver pronta.
const DEMO_MODE = true;

window.APP_CONFIG = {
  API_BASE_URL,
  DEMO_MODE,
  endpoints: {
    login: "/auth/login",
    movies: "/movies",
    search: "/movies/search",
    favorites: "/favorites"
  }
};

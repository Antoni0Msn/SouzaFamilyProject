// ==============================
// CONFIGURAÇÃO DO FRONTEND
// ==============================
// Troque pela URL da sua API REST em C# hospedada no Render.
// Ex: "https://familia-souza-api.onrender.com/api" (produção)
// Ex: "https://localhost:7101/api" (rodando local, junto com o backend)
const API_BASE_URL = "https://localhost:7101/api";

// false = usa a API C# de verdade em vez do login demonstrativo.
const DEMO_MODE = false;

window.APP_CONFIG = {
  API_BASE_URL,
  DEMO_MODE,
  endpoints: {
    login: "/auth/login",
    register: "/auth/register",
    movies: "/movies",
    search: "/movies/search",
    favorites: "/favorites"
  }
};
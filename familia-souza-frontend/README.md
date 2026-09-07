# Família Souza — Frontend

Frontend inicial de um serviço de streaming privado, inspirado na organização visual de plataformas como Netflix.

## Estrutura

- `index.html` — tela de login
- `home.html` — catálogo principal
- `css/style.css` — estilos responsivos
- `js/config.js` — URL e endpoints da API C#
- `js/auth.js` — autenticação
- `js/data.js` — catálogo demonstrativo
- `js/app.js` — interface, busca, favoritos, modal e sessão

## Rodar localmente

Você pode abrir `index.html` diretamente no navegador. Para uma experiência mais próxima de produção, use uma extensão como Live Server no VS Code ou qualquer servidor estático.

## Login demonstrativo

Por padrão, `DEMO_MODE = true` em `js/config.js`. Assim, qualquer e-mail válido e senha com 4+ caracteres entram no catálogo.

Quando sua API C# estiver pronta:

1. Troque `API_BASE_URL` pela URL pública do Render.
2. Defina `DEMO_MODE = false`.
3. Confirme o endpoint `POST /auth/login`.
4. Ajuste o mapeamento de `token/accessToken` e `user` em `js/auth.js` caso sua resposta tenha nomes diferentes.

## Contrato sugerido da API

### POST /api/auth/login

Request:

```json
{
  "email": "familia@email.com",
  "password": "123456"
}
```

Response sugerida:

```json
{
  "token": "jwt-aqui",
  "user": {
    "id": 1,
    "name": "Família Souza",
    "email": "familia@email.com"
  }
}
```

### GET /api/movies

O frontend pode futuramente substituir os dados de `js/data.js` por dados dessa rota.

### GET /api/movies/search?q=...

Busca de filmes e séries.

### GET /api/favorites

Lista de favoritos do usuário autenticado.

### POST /api/favorites

Adiciona um título à lista.

### DELETE /api/favorites/{movieId}

Remove um título da lista.

## Observação

Os posters desta primeira versão são artes geradas em CSS para deixar o projeto independente de imagens externas. Você pode trocar o conteúdo por posters reais, TMDB, armazenamento próprio etc.

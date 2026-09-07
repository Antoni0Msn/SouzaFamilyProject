# ---------- Base (imagem de runtime, usada no final) ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

# ---------- Build ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia só o csproj primeiro para aproveitar cache de camadas do Docker
COPY ["WebApplication1.csproj", "./"]
RUN dotnet restore "WebApplication1.csproj"

# Agora copia o resto do código
COPY . .
RUN dotnet build "WebApplication1.csproj" -c Release -o /app/build

# ---------- Publish ----------
FROM build AS publish
RUN dotnet publish "WebApplication1.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- Final (imagem enxuta que roda em produção) ----------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApplication1.dll"]

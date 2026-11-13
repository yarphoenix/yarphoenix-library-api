# Один образ на весь монолит (ТЗ §11.2). Контекст сборки — корень репозитория:
#   docker build -f src/YarPhoenix.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# Сначала только манифесты — слой restore кэшируется между сборками.
COPY Directory.Build.props ./
COPY src/YarPhoenix.Api/YarPhoenix.Api.csproj src/YarPhoenix.Api/
COPY src/YarPhoenix.Shared/YarPhoenix.Shared.csproj src/YarPhoenix.Shared/
COPY src/YarPhoenix.Catalog/YarPhoenix.Catalog.csproj src/YarPhoenix.Catalog/
COPY src/YarPhoenix.Auth/YarPhoenix.Auth.csproj src/YarPhoenix.Auth/
COPY src/YarPhoenix.Storage/YarPhoenix.Storage.csproj src/YarPhoenix.Storage/
COPY src/YarPhoenix.Ai/YarPhoenix.Ai.csproj src/YarPhoenix.Ai/
RUN dotnet restore src/YarPhoenix.Api/YarPhoenix.Api.csproj

COPY src/ src/
RUN dotnet publish src/YarPhoenix.Api/YarPhoenix.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl нужен для HEALTHCHECK.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "YarPhoenix.Api.dll"]

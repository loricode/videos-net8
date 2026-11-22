# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY DomainCore/DomainCore.csproj DomainCore/
COPY InfrastructureCore/InfrastructureCore.csproj InfrastructureCore/
COPY ApplicationCore/ApplicationCore.csproj ApplicationCore/
COPY WebApiCore/WebApiCore.csproj WebApiCore/
RUN dotnet restore WebApiCore/WebApiCore.csproj

COPY . .
RUN dotnet publish WebApiCore/WebApiCore.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# FFmpeg para generar HLS + curl para healthcheck
RUN apt-get update \
 && apt-get install -y --no-install-recommends ffmpeg curl \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    FFmpeg__PathOrigen=/usr/bin/ffmpeg \
    FFmpeg__PathVideos=/data/ \
    Storage__BasePath=/data/images

VOLUME ["/data"]
EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=60s --retries=10 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "WebApiCore.dll"]

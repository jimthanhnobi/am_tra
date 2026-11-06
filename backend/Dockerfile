FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/TeaPremium.Api/TeaPremium.Api.csproj --locked-mode
RUN dotnet publish src/TeaPremium.Api/TeaPremium.Api.csproj \
    --configuration Release --no-restore --output /app/publish \
    -p:UseAppHost=false

# Explicit local migration command only; the API image never runs migrations on startup.
FROM build AS migrate
RUN dotnet tool install --global dotnet-ef --version 10.0.14
ENV PATH="/root/.dotnet/tools:${PATH}"
CMD ["dotnet", "ef", "database", "update", "--project", "src/TeaPremium.Infrastructure", "--startup-project", "src/TeaPremium.Api", "--context", "AppDbContext"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0.14 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /home/app/.aspnet
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .
ENV ASPNETCORE_HTTP_PORTS=5080
EXPOSE 5080
USER app
ENTRYPOINT ["dotnet", "TeaPremium.Api.dll"]

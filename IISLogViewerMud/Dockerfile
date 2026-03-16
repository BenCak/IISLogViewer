# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies first (layer caching)
COPY IISLogViewer.csproj ./
RUN dotnet restore

# Copy everything else and publish
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create the LogFiles directory that the app expects at runtime
RUN mkdir -p /app/LogFiles

COPY --from=build /app/publish .

# Expose HTTP port (ASP.NET Core default in containers)
EXPOSE 8080

# Mount point for IIS log files from the host
VOLUME ["/app/LogFiles"]

ENTRYPOINT ["dotnet", "IISLogViewer.dll"]

# ── Development Container (with SDK) ──────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS development
WORKDIR /app

# Copy project files
COPY . .

# Restore and build (optional, for faster startup)
RUN dotnet restore
RUN dotnet build

# Expose port for development
EXPOSE 5000

# Default command for development
CMD ["dotnet", "run"]

# Expose HTTP port (ASP.NET Core default in containers)
EXPOSE 8080

# Mount point for IIS log files from the host
VOLUME ["/app/LogFiles"]

ENTRYPOINT ["dotnet", "IISLogViewer.dll"]

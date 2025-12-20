# Task: T109 [P] Create Dockerfile with multi-stage build, BuildKit secrets, app user, health check
# Per Constitution X and Maliev guidelines Section 3

# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Maliev.ReceiptService.sln", "./"]
COPY ["Maliev.ReceiptService.Api/Maliev.ReceiptService.Api.csproj", "Maliev.ReceiptService.Api/"]
COPY ["nuget.config", "./"]

# Restore dependencies using BuildKit secrets for NuGet authentication
RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_password \
    export NUGET_USERNAME=$(cat /run/secrets/nuget_username) && \
    export NUGET_PASSWORD=$(cat /run/secrets/nuget_password) && \
    dotnet restore "ReceiptService.Api/ReceiptService.Api.csproj"

# Copy source code
COPY ["Maliev.ReceiptService.Api/", "Maliev.ReceiptService.Api/"]

# Build and publish
WORKDIR "/Maliev.ReceiptService.Api"
RUN dotnet publish "Maliev.ReceiptService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user per Constitution X
RUN groupadd -r app && useradd -r -g app app

# Copy published application
COPY --from=build /app/publish .

# Set ownership to app user
RUN chown -R app:app /app

# Switch to non-root user
USER app

# Expose port (default ASP.NET Core port)
EXPOSE 8080

# Health check per Constitution X
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/receipt/health || exit 1

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "Maliev.ReceiptService.Api.dll"]

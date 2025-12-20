# syntax=docker/dockerfile:1.4
# Multi-stage build for Receipt Service

# Base stage for runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy solution and project files
COPY ["Maliev.ReceiptService.sln", "./"]
COPY ["Maliev.ReceiptService.Api/Maliev.ReceiptService.Api.csproj", "Maliev.ReceiptService.Api/"]
COPY ["Maliev.ReceiptService.Data/Maliev.ReceiptService.Data.csproj", "Maliev.ReceiptService.Data/"]
COPY ["nuget.config", "."]

# Restore dependencies with BuildKit secrets
RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_password \
    NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
    NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
    dotnet restore "Maliev.ReceiptService.Api/Maliev.ReceiptService.Api.csproj"

# Copy remaining source code
COPY . .
WORKDIR "/src/Maliev.ReceiptService.Api"
RUN dotnet build "Maliev.ReceiptService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Maliev.ReceiptService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Ensure 'app' owns the workdir (app user already exists in ASP.NET runtime image)
RUN chown -R app:app /app

# Switch to non-root user
USER app

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/receipt/health || exit 1

ENTRYPOINT ["dotnet", "Maliev.ReceiptService.Api.dll"]
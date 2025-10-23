# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for restore
COPY ["src/OrderTrace.Api/OrderTrace.Api.csproj", "OrderTrace.Api/"]
COPY ["src/OrderTrace.Core/OrderTrace.Core.csproj", "OrderTrace.Core/"]
COPY ["src/OrderTrace.Infrastructure/OrderTrace.Infrastructure.csproj", "OrderTrace.Infrastructure/"]
COPY ["src/OrderTrace.Observability/OrderTrace.Observability.csproj", "OrderTrace.Observability/"]

# Restore dependencies
RUN dotnet restore "OrderTrace.Api/OrderTrace.Api.csproj"

# Copy source code
COPY src/ .

# Build
WORKDIR "/src/OrderTrace.Api"
RUN dotnet build "OrderTrace.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "OrderTrace.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS final
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create logs directory
RUN mkdir -p /app/logs && chmod 777 /app/logs

# Copy published app
COPY --from=publish /app/publish .

# Copy entrypoint script
COPY src/OrderTrace.Api/docker-entrypoint.sh /app/
RUN chmod +x /app/docker-entrypoint.sh

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Environment variables (can be overridden in docker-compose)
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["/app/docker-entrypoint.sh"]
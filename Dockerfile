# syntax=docker/dockerfile:1

# ---------- Build layer: restore dependencies and publish all services ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
ARG BUILD_CONFIGURATION=Release

# Copy project files first to maximize Docker layer caching when dependencies do not change.
COPY dotnet/dotnet.csproj dotnet/
COPY chat/chat.csproj chat/
COPY gateway/gateway.csproj gateway/

# Restore dependencies per service so each project can be built independently.
RUN dotnet restore dotnet/dotnet.csproj \
    && dotnet restore chat/chat.csproj \
    && dotnet restore gateway/gateway.csproj

# Copy the remainder of the repository and publish release builds for each service.
COPY . .
RUN dotnet publish dotnet/dotnet.csproj -c $BUILD_CONFIGURATION -o /out/dotnet \
    && dotnet publish chat/chat.csproj -c $BUILD_CONFIGURATION -o /out/chat \
    && dotnet publish gateway/gateway.csproj -c $BUILD_CONFIGURATION -o /out/gateway

# ---------- Runtime layer: install Supervisor and run multi-process container ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install Supervisor (lightweight init system) to orchestrate the three ASP.NET Core processes.
RUN apt-get update \
    && apt-get install -y --no-install-recommends supervisor \
    && rm -rf /var/lib/apt/lists/*

# Copy the published outputs produced by the build stage.
COPY --from=build /out/dotnet ./dotnet
COPY --from=build /out/chat ./chat
COPY --from=build /out/gateway ./gateway

# Provide Supervisor with the process definitions.
COPY supervisord.conf /etc/supervisor/conf.d/supervisord.conf

# Default internal networking used by the services (override via environment at runtime as needed).
ENV DOTNET_GRPC=http://127.0.0.1:5100 \
    CHAT_GRPC=http://127.0.0.1:5296 \
    GRPC__CHATURL=http://127.0.0.1:5296 \
    GRPC__AUTHURL=http://127.0.0.1:5100 \
    PORT=5200

# Only the public gateway port is exposed — gRPC services stay internal.
EXPOSE 5200

# Supervisor keeps the container alive and restarts crashed services automatically.
ENTRYPOINT ["supervisord", "-c", "/etc/supervisor/conf.d/supervisord.conf"]

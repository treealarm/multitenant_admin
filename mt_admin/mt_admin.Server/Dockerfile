FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# ===============================
# SDK + Node
# ===============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS with-node
RUN apt-get update
RUN apt-get install curl
RUN curl -sL https://deb.nodesource.com/setup_20.x | bash
RUN apt-get -y install nodejs

# ===============================
# Build
# ===============================
FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["mt_admin.Server/mt_admin.Server.csproj", "mt_admin.Server/"]
COPY ["mt_admin.client/mt_admin.client.esproj", "mt_admin.client/"]
COPY ["DbAdmin/DbAdmin.csproj", "DbAdmin/"]
COPY ["KeycloakAdmin/KeycloakAdmin.csproj", "KeycloakAdmin/"]
COPY ["KuberAdmin/KuberAdmin.csproj", "KuberAdmin/"]

RUN dotnet restore "mt_admin.Server/mt_admin.Server.csproj"

COPY . .
WORKDIR "/src/mt_admin.Server"
ARG BUILD_CONFIGURATION=Release
RUN dotnet build mt_admin.Server.csproj -c $BUILD_CONFIGURATION -o /app/build

RUN node --version && npm --version

# ===============================
# Publish
# ===============================
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish mt_admin.Server.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ===============================
# Final
# ===============================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "mt_admin.Server.dll"]

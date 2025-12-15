# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS with-node
RUN apt-get update
RUN apt-get install curl
RUN curl -sL https://deb.nodesource.com/setup_20.x | bash
RUN apt-get -y install nodejs


# This stage is used to build the service project
FROM with-node AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["mt_admin.client/nuget.config", "mt_admin.client/"]
COPY ["mt_admin.Server/mt_admin.Server.csproj", "mt_admin.Server/"]
COPY ["DbAdmin/DbAdmin.csproj", "DbAdmin/"]
COPY ["KeycloakAdmin/KeycloakAdmin.csproj", "KeycloakAdmin/"]
COPY ["KuberAdmin/KuberAdmin.csproj", "KuberAdmin/"]
COPY ["mt_admin.client/mt_admin.client.esproj", "mt_admin.client/"]
RUN dotnet restore "./mt_admin.Server/mt_admin.Server.csproj"
COPY . .
WORKDIR "/src/mt_admin.Server"
RUN dotnet build "./mt_admin.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./mt_admin.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mt_admin.Server.dll"]
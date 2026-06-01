# Multi-stage Dockerfile for Nexus API
# We use Alpine-based images for smaller footprint and security.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install ICU for globalization support in Alpine
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY ["backend/src/Nexus.Api/Nexus.Api.csproj", "backend/src/Nexus.Api/"]
COPY ["backend/src/Nexus.Application/Nexus.Application.csproj", "backend/src/Nexus.Application/"]
COPY ["backend/src/Nexus.Domain/Nexus.Domain.csproj", "backend/src/Nexus.Domain/"]
COPY ["backend/src/Nexus.Infrastructure/Nexus.Infrastructure.csproj", "backend/src/Nexus.Infrastructure/"]
COPY ["backend/src/Nexus.SharedKernel/Nexus.SharedKernel.csproj", "backend/src/Nexus.SharedKernel/"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

RUN dotnet restore "backend/src/Nexus.Api/Nexus.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/backend/src/Nexus.Api"
RUN dotnet build "Nexus.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Nexus.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexus.Api.dll"]
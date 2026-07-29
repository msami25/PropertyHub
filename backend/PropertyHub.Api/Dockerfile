FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props PropertyHub.sln ./
COPY backend/PropertyHub.Domain/PropertyHub.Domain.csproj backend/PropertyHub.Domain/
COPY backend/PropertyHub.Application/PropertyHub.Application.csproj backend/PropertyHub.Application/
COPY backend/PropertyHub.Infrastructure/PropertyHub.Infrastructure.csproj backend/PropertyHub.Infrastructure/
COPY backend/PropertyHub.Api/PropertyHub.Api.csproj backend/PropertyHub.Api/
RUN dotnet restore backend/PropertyHub.Api/PropertyHub.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/PropertyHub.Api/PropertyHub.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/uploads
EXPOSE 8080
ENTRYPOINT ["dotnet", "PropertyHub.Api.dll"]

# Multi-stage build — Host API (Fase 1)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props SocioTorcedor.sln ./
COPY src/ ./src/

RUN dotnet restore src/Host/SocioTorcedor.Api/SocioTorcedor.Api.csproj
RUN dotnet publish src/Host/SocioTorcedor.Api/SocioTorcedor.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    --no-self-contained

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SocioTorcedor.Api.dll"]

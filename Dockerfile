# ==============================================================================
# Stage 1: Build & Publish (.NET 10 SDK)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar primero los archivos de proyecto para maximizar la caché de NuGet
COPY src/PergamonLens.Domain/PergamonLens.Domain.csproj src/PergamonLens.Domain/
COPY src/PergamonLens.Application/PergamonLens.Application.csproj src/PergamonLens.Application/
COPY src/PergamonLens.Infrastructure/PergamonLens.Infrastructure.csproj src/PergamonLens.Infrastructure/
COPY src/PergamonLens.Web/PergamonLens.Web.csproj src/PergamonLens.Web/

RUN dotnet restore src/PergamonLens.Web/PergamonLens.Web.csproj

# Copiar el código fuente y publicar en configuración Release
COPY src/ src/
RUN dotnet publish src/PergamonLens.Web/PergamonLens.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ==============================================================================
# Stage 2: Runtime ligero y seguro (.NET 10 ASP.NET Core, usuario non-root)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

# Seguridad: ejecutar con el usuario non-root integrado en las imágenes .NET 8+
USER app

EXPOSE 8080

ENTRYPOINT ["dotnet", "PergamonLens.Web.dll"]

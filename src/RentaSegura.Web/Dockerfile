# ============================================================================
#  Dockerfile (multi-stage) para RentaSegura.Web
#  build -> publish con el SDK de .NET 9; runtime ligero con ASP.NET 9.
# ============================================================================

# --- Etapa de build ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restaurar primero (mejor uso de la caché de capas).
COPY src/RentaSegura.Web/RentaSegura.Web.csproj src/RentaSegura.Web/
RUN dotnet restore src/RentaSegura.Web/RentaSegura.Web.csproj

# Copiar el resto y publicar.
COPY . .
RUN dotnet publish src/RentaSegura.Web/RentaSegura.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# --- Etapa de runtime ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Carpeta para la bóveda de documentos cifrados (privacidad KYC).
RUN mkdir -p /app/vault

COPY --from=build /app/publish .

# Escucha en el puerto 8080 dentro del contenedor.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RentaSegura.Web.dll"]

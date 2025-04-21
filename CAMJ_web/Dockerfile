# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar todo y restaurar dependencias
COPY . . 
RUN dotnet restore

# Compilar y publicar la app
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Especificar el archivo DLL de arranque
ENTRYPOINT ["dotnet", "CAMJ_web.dll"]

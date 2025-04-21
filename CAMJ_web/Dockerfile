# Imagen base de .NET SDK para compilar
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copiamos los archivos del proyecto
COPY . ./

# Restauramos dependencias
RUN dotnet restore

# Publicamos la app en modo Release
RUN dotnet publish -c Release -o out

# Imagen base para ejecutar la app
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out .

# Expone el puerto que usará la app
EXPOSE 80

# Comando para ejecutar la app
ENTRYPOINT ["dotnet", "CAMJ_web.dll"]

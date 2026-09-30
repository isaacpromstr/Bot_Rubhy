# Etapa 1: Compilación
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar csproj y restaurar paquetes NuGet
COPY Plantillabot.csproj ./
RUN dotnet restore Plantillabot.csproj

# Copiar el resto del código y publicar
COPY . ./
RUN dotnet publish Plantillabot.csproj -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagen final para ejecución
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Puerto por defecto para Render (Render inyecta la variable $PORT)
ENV PORT=10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "Plantillabot.dll"]

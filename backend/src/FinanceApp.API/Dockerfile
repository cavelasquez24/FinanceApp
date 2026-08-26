FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/FinanceApp.API/FinanceApp.API.csproj
RUN dotnet publish src/FinanceApp.API/FinanceApp.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
# Render limita las instancias de inotify (128 por usuario). El reload de
# configuracion por archivo crea un FileSystemWatcher por cada appsettings*.json
# y hace fallar el arranque. En un contenedor inmutable no aporta nada.
ENV DOTNET_hostBuilder__reloadConfigOnChange=false
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
EXPOSE 8080
ENTRYPOINT ["dotnet", "FinanceApp.API.dll"]

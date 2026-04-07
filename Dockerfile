FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore
COPY src/EDO.Server/EDO.Server.csproj EDO.Server/
COPY src/EDO.Client/EDO.Client.csproj EDO.Client/
RUN dotnet restore EDO.Server/EDO.Server.csproj
RUN dotnet restore EDO.Client/EDO.Client.csproj

# Copy source
COPY src/EDO.Server/ EDO.Server/
COPY src/EDO.Client/ EDO.Client/

# Build & publish client (Blazor WASM → static files)
RUN dotnet publish EDO.Client/EDO.Client.csproj -c Release -o /app/client

# Build & publish server
RUN dotnet publish EDO.Server/EDO.Server.csproj -c Release -o /app/server

# Copy client static files into server wwwroot
RUN cp -r /app/client/wwwroot/* /app/server/wwwroot/ 2>/dev/null || true

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/server .

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "EDO.Server.dll"]

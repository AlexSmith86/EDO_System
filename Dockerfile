FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Встановлюємо Python, який критично необхідний для збірки WebAssembly (wasm-tools)
RUN apt-get update && apt-get install -y python3

# Install wasm-tools workload for Blazor AOT/trimming
RUN dotnet workload install wasm-tools

WORKDIR /src

# Copy csproj files and restore (layer cache)
COPY src/EDO.Client/EDO.Client.csproj EDO.Client/
COPY src/EDO.Server/EDO.Server.csproj EDO.Server/
RUN dotnet restore EDO.Server/EDO.Server.csproj

# Copy all source
COPY src/EDO.Client/ EDO.Client/
COPY src/EDO.Server/ EDO.Server/

# Single publish — builds both server and client (via ProjectReference)
# Client WASM files land in /app/wwwroot/_framework/ automatically
RUN dotnet publish EDO.Server/EDO.Server.csproj -c Release -o /app

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080
ENTRYPOINT ["dotnet", "EDO.Server.dll"]
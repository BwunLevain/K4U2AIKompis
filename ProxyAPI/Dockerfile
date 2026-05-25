FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ProxyAPI/ProxyAPI.csproj", "ProxyAPI/"]
RUN dotnet restore "ProxyAPI/ProxyAPI.csproj"
COPY . .
WORKDIR "/src/ProxyAPI"
RUN dotnet publish "ProxyAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProxyAPI.dll"]

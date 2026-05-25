FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ContentAPI/ContentAPI.csproj", "ContentAPI/"]
RUN dotnet restore "ContentAPI/ContentAPI.csproj"
COPY . .
WORKDIR "/src/ContentAPI"
RUN dotnet publish "ContentAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ContentAPI.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SurvivalS2API/SurvivalS2API.csproj", "SurvivalS2API/"]
RUN dotnet restore "SurvivalS2API/SurvivalS2API.csproj"
COPY . .
WORKDIR "/src/SurvivalS2API"
RUN dotnet build "SurvivalS2API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SurvivalS2API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SurvivalS2API.dll"]

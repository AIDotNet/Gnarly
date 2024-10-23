FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Gnarly.WebApi/Gnarly.WebApi.csproj", "src/Gnarly.WebApi/"]
COPY ["src/Gnarly.Data/Gnarly.Data.csproj", "src/Gnarly.Data/"]
COPY ["src/Gnarly.Application/Gnarly.Application.csproj", "src/Gnarly.Application/"]
COPY ["src/Gnarly/Gnarly.csproj", "src/Gnarly/"]
RUN dotnet restore "src/Gnarly.WebApi/Gnarly.WebApi.csproj"
COPY . .
WORKDIR "/src/src/Gnarly.WebApi"
RUN dotnet build "Gnarly.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Gnarly.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gnarly.WebApi.dll"]

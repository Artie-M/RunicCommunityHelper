FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM node:22-alpine AS web
WORKDIR /web
COPY ["src/RunicCommunityHelper.Web/package.json", "src/RunicCommunityHelper.Web/package-lock.json", "./"]
RUN npm ci --no-audit --no-fund
COPY src/RunicCommunityHelper.Web/ .
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/RunicCommunityHelper.App/RunicCommunityHelper.App.csproj", "src/RunicCommunityHelper.App/"]
COPY ["src/RunicCommunityHelper.Common/RunicCommunityHelper.Common.csproj", "src/RunicCommunityHelper.Common/"]
COPY ["src/RunicCommunityHelper.Discord/RunicCommunityHelper.Discord.csproj", "src/RunicCommunityHelper.Discord/"]
COPY ["src/RunicCommunityHelper.Implementations/RunicCommunityHelper.Implementations.csproj", "src/RunicCommunityHelper.Implementations/"]
RUN dotnet restore "src/RunicCommunityHelper.App/RunicCommunityHelper.App.csproj"
COPY . . 
WORKDIR "/src/src/RunicCommunityHelper.App"
RUN dotnet build "RunicCommunityHelper.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "RunicCommunityHelper.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=web /web/dist ./wwwroot
ENTRYPOINT ["dotnet", "RunicCommunityHelper.App.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

COPY ["nuget.config", "."]
COPY ["NexusMods.ArticleViewer/Server/NexusMods.ArticleViewer.Server.csproj", "NexusMods.ArticleViewer/Server/"]
COPY ["NexusMods.ArticleViewer/Shared/NexusMods.ArticleViewer.Shared.csproj", "NexusMods.ArticleViewer/Shared/"]

RUN dotnet restore "NexusMods.ArticleViewer/Server/NexusMods.ArticleViewer.Server.csproj"

COPY . .

WORKDIR "/src/NexusMods.ArticleViewer/Server"
RUN dotnet build "NexusMods.ArticleViewer.Server.csproj" -c Release -o /app/build


FROM build AS publish
RUN dotnet publish "NexusMods.ArticleViewer.Server.csproj" -c Release -o /app/publish


FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

LABEL org.opencontainers.image.source="https://github.com/BUTR/NexusMods.ArticleViewer"
EXPOSE 8080/tcp
ENTRYPOINT ["dotnet", "NexusMods.ArticleViewer.Server.dll"]

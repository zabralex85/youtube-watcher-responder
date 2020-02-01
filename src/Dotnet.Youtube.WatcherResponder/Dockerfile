#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["Dotnet.Youtube.WatcherResponder/Dotnet.Youtube.WatcherResponder.csproj", "Dotnet.Youtube.WatcherResponder/"]
RUN dotnet restore "Dotnet.Youtube.WatcherResponder/Dotnet.Youtube.WatcherResponder.csproj"
COPY . .
WORKDIR "/src/Dotnet.Youtube.WatcherResponder"
RUN dotnet build "Dotnet.Youtube.WatcherResponder.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Dotnet.Youtube.WatcherResponder.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dotnet.Youtube.WatcherResponder.dll"]
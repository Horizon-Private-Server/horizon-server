#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["Server.Dme/Server.Dme.csproj", "Server.Dme/"]
COPY ["RT.Cryptography/RT.Cryptography.csproj", "RT.Cryptography/"]
COPY ["HighResolutionTimer/HighResolutionTimer.csproj", "HighResolutionTimer/"]
COPY ["Server.Plugins/Server.Plugins.csproj", "Server.Plugins/"]
COPY ["RT.Common/RT.Common.csproj", "RT.Common/"]
COPY ["Server.Common/Server.Common.csproj", "Server.Common/"]
COPY ["Server.Pipeline/Server.Pipeline.csproj", "Server.Pipeline/"]
COPY ["RT.Models/RT.Models.csproj", "RT.Models/"]
RUN dotnet restore "Server.Dme/Server.Dme.csproj"
COPY . .
WORKDIR "/src/Server.Dme"
RUN dotnet build "Server.Dme.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Server.Dme.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Server.Dme.dll"]
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ApexVotingApi/ApexVotingApi.csproj", "ApexVotingApi/"]
COPY ["ApexVoting.ServiceDefaults/ApexVoting.ServiceDefaults.csproj", "ApexVoting.ServiceDefaults/"]
RUN dotnet restore "ApexVotingApi/ApexVotingApi.csproj"
COPY . .
WORKDIR "/src/ApexVotingApi"
RUN dotnet build "./ApexVotingApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ApexVotingApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApexVotingApi.dll"]

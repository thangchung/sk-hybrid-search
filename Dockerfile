# Test Dockerfile for HyDE Search application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["HydeSearch.csproj", "."]
RUN dotnet restore "./HydeSearch.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "HydeSearch.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HydeSearch.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HydeSearch.dll"]

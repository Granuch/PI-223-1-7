FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5010

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AccountController/AccountService.csproj", "AccountController/"]
COPY ["BLL/BLL.csproj", "BLL/"]
COPY ["PI-223-1-7/DAL.csproj", "PI-223-1-7/"]
COPY ["Mapping/Mapping.csproj", "Mapping/"]
RUN dotnet restore "AccountController/AccountService.csproj"
COPY . .
WORKDIR "/src/AccountController"
RUN dotnet build "AccountService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AccountService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AccountService.dll"]

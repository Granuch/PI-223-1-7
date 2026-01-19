FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["UI/UI.csproj", "UI/"]
COPY ["BLL/BLL.csproj", "BLL/"]
COPY ["PI-223-1-7/DAL.csproj", "PI-223-1-7/"]
COPY ["Mapping/Mapping.csproj", "Mapping/"]
RUN dotnet restore "UI/UI.csproj"
COPY . .
WORKDIR "/src/UI"
RUN dotnet build "UI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UI.dll"]

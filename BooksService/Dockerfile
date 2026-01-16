FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["BooksService/BooksService.csproj", "BooksService/"]
COPY ["BLL/BLL.csproj", "BLL/"]
COPY ["PI-223-1-7/DAL.csproj", "PI-223-1-7/"]
COPY ["Mapping/Mapping.csproj", "Mapping/"]
RUN dotnet restore "BooksService/BooksService.csproj"
COPY . .
WORKDIR "/src/BooksService"
RUN dotnet build "BooksService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BooksService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BooksService.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Movora.WebAPI/Movora.WebAPI.csproj", "Movora.WebAPI/"]
RUN dotnet restore "Movora.WebAPI/Movora.WebAPI.csproj"
COPY . .
WORKDIR "/src/Movora.WebAPI"
RUN dotnet build "Movora.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Movora.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Movora.WebAPI.dll"]
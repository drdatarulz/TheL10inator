ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY ["src/TheL10inator.Migrator/TheL10inator.Migrator.csproj", "src/TheL10inator.Migrator/"]
RUN dotnet restore "src/TheL10inator.Migrator/TheL10inator.Migrator.csproj"

COPY ["src/TheL10inator.Migrator/", "src/TheL10inator.Migrator/"]
RUN dotnet publish "src/TheL10inator.Migrator/TheL10inator.Migrator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:${DOTNET_VERSION} AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "TheL10inator.Migrator.dll"]

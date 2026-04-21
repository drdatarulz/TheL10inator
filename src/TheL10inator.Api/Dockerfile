ARG DOTNET_VERSION=10.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

COPY ["src/TheL10inator.Api/TheL10inator.Api.csproj", "src/TheL10inator.Api/"]
COPY ["src/TheL10inator.Infrastructure/TheL10inator.Infrastructure.csproj", "src/TheL10inator.Infrastructure/"]
COPY ["src/TheL10inator.Domain/TheL10inator.Domain.csproj", "src/TheL10inator.Domain/"]
RUN dotnet restore "src/TheL10inator.Api/TheL10inator.Api.csproj"

COPY . .
RUN dotnet publish "src/TheL10inator.Api/TheL10inator.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TheL10inator.Api.dll"]

# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the layer is cached while only source files change.
COPY Directory.Build.props Sgo.sln ./
COPY src/Sgo.Domain/Sgo.Domain.csproj src/Sgo.Domain/
COPY src/Sgo.Application/Sgo.Application.csproj src/Sgo.Application/
COPY src/Sgo.Infrastructure/Sgo.Infrastructure.csproj src/Sgo.Infrastructure/
COPY src/Sgo.Api/Sgo.Api.csproj src/Sgo.Api/
RUN dotnet restore src/Sgo.Api/Sgo.Api.csproj

COPY src/ src/
RUN dotnet publish src/Sgo.Api/Sgo.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Sgo.Api.dll"]

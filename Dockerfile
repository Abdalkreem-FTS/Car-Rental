FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source

COPY Directory.Build.props Directory.Packages.props .editorconfig ./
COPY *.slnx ./
COPY src/CarRental.Domain/*.csproj src/CarRental.Domain/
COPY src/CarRental.Application/*.csproj src/CarRental.Application/
COPY src/CarRental.Infrastructure/*.csproj src/CarRental.Infrastructure/
COPY src/CarRental.Api/*.csproj src/CarRental.Api/
RUN dotnet restore src/CarRental.Api/CarRental.Api.csproj

COPY src/ src/
RUN dotnet publish src/CarRental.Api/CarRental.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

RUN adduser --disabled-password --no-create-home --uid 64198 carrental \
    && chown -R carrental /app
USER carrental

COPY --from=build --chown=carrental /app .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=1

EXPOSE 8080

HEALTHCHECK --interval=15s --timeout=5s --start-period=20s --retries=5 \
    CMD wget --quiet --spider http://localhost:8080/api/health || exit 1

ENTRYPOINT ["dotnet", "CarRental.Api.dll"]
